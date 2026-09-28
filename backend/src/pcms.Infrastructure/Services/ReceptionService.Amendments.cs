using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Auth;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Exceptions;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class ReceptionService
{
    public async Task<ReceptionHistoryEventDto?> CreateCorrectionAsync(
        Guid id,
        CreateReceptionCorrectionDto dto,
        Guid actorUserId)
    {
        Exception? concurrencyException = null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await CreateCorrectionCoreAsync(
                    id,
                    dto,
                    actorUserId);
            }
            catch (Exception exception)
                when (IsReceptionConcurrencyConflict(exception))
            {
                concurrencyException = exception;
                _context.ChangeTracker.Clear();
            }
        }

        throw ReceptionConcurrencyConflict(concurrencyException!);
    }

    private async Task<ReceptionHistoryEventDto?> CreateCorrectionCoreAsync(
        Guid id,
        CreateReceptionCorrectionDto dto,
        Guid actorUserId)
    {
        ValidateCorrectionRequest(dto, actorUserId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (!await LockActiveReceptionAsync(id))
        {
            return null;
        }

        var lockedCremationId =
            await LockCremationByReceptionAsync(id);

        var reception = await _context.Receptions
            .Include(item => item.VeterinaryClinic)
            .Include(item => item.ReferringVeterinarian)
            .Include(item => item.VeterinaryRequest)
            .Include(item => item.Collection)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.IsActive);

        if (reception is null)
        {
            return null;
        }

        var cremation = await _context.Cremations
            .FirstOrDefaultAsync(item =>
                item.Id == lockedCremationId);

        if (cremation is null)
        {
            throw new InvalidOperationException(
                "La recepción todavía no está vinculada a una cremación.");
        }

        var actor = await ResolveCorrectionActorAsync(actorUserId);

        var existingEvent = await _context.ReceptionHistoryEvents
            .AsNoTracking()
            .Include(historyEvent => historyEvent.Changes)
            .FirstOrDefaultAsync(historyEvent =>
                historyEvent.ReceptionId == reception.Id &&
                historyEvent.RequestId == dto.RequestId);

        if (existingEvent is not null)
        {
            EnsureMatchingCorrectionRetry(
                existingEvent,
                dto);

            await transaction.CommitAsync();
            return MapHistoryEvent(existingEvent);
        }

        var reason = dto.Reason.Trim();
        var stage = GetHistoryStage(cremation.Status);
        var changes = new List<ReceptionHistoryChange>();

        await BuildWeightCorrectionAsync(
            reception,
            cremation,
            dto,
            changes);

        var normalizedReferralNotes = dto.ReferralNotes is null
            ? reception.ReferralNotes
            : NormalizeOptionalText(dto.ReferralNotes.Value);

        var requestedClinicId = dto.VeterinaryClinicId is null
            ? reception.VeterinaryClinicId
            : dto.VeterinaryClinicId.Value;

        var requestedVeterinarianId = dto.ReferringVeterinarianId is null
            ? reception.ReferringVeterinarianId
            : dto.ReferringVeterinarianId.Value;

        var referralCorrectionRequested =
            dto.VeterinaryClinicId is not null ||
            dto.ReferringVeterinarianId is not null ||
            dto.ReferralNotes is not null;

        var previouslyCorrectedReferralFields = referralCorrectionRequested
            ? await _context.ReceptionHistoryEvents
                .AsNoTracking()
                .Where(historyEvent =>
                    historyEvent.ReceptionId == reception.Id &&
                    historyEvent.EventKind ==
                        ReceptionHistoryEventKind.Correction)
                .SelectMany(historyEvent => historyEvent.Changes)
                .Where(change =>
                    change.Field ==
                        ReceptionHistoryField.VeterinaryClinicId ||
                    change.Field ==
                        ReceptionHistoryField.ReferringVeterinarianId)
                .Select(change => change.Field)
                .Distinct()
                .ToListAsync()
            : [];

        var referral = referralCorrectionRequested
            ? await ValidateReferralSourceAsync(
                requestedClinicId,
                requestedVeterinarianId,
                normalizedReferralNotes)
            : (
                Clinic: reception.VeterinaryClinic,
                Veterinarian: reception.ReferringVeterinarian);

        AddGuidCorrection(
            dto.VeterinaryClinicId,
            ReceptionHistoryField.VeterinaryClinicId,
            reception.VeterinaryClinicId,
            requestedClinicId,
            GetEffectiveClinicDisplayName(
                reception,
                previouslyCorrectedReferralFields.Contains(
                    ReceptionHistoryField.VeterinaryClinicId)),
            referral.Clinic?.Name,
            changes);

        AddGuidCorrection(
            dto.ReferringVeterinarianId,
            ReceptionHistoryField.ReferringVeterinarianId,
            reception.ReferringVeterinarianId,
            requestedVeterinarianId,
            GetEffectiveVeterinarianDisplayName(
                reception,
                previouslyCorrectedReferralFields.Contains(
                    ReceptionHistoryField.ReferringVeterinarianId)),
            GetVeterinarianFullName(referral.Veterinarian),
            changes);

        AddStringCorrection(
            dto.ReferralNotes,
            ReceptionHistoryField.ReferralNotes,
            reception.ReferralNotes,
            normalizedReferralNotes,
            changes);

        var requestedHasBelongings = dto.HasPersonalBelongings is null
            ? reception.HasPersonalBelongings
            : dto.HasPersonalBelongings.Value;

        var requestedBelongingsDescription =
            dto.PersonalBelongingsDescription is null
                ? reception.PersonalBelongingsDescription
                : NormalizeOptionalText(
                    dto.PersonalBelongingsDescription.Value);

        ValidateBelongingsCorrection(
            reception,
            dto,
            requestedHasBelongings,
            requestedBelongingsDescription);

        AddBooleanCorrection(
            dto.HasPersonalBelongings,
            ReceptionHistoryField.HasPersonalBelongings,
            reception.HasPersonalBelongings,
            requestedHasBelongings,
            changes);

        AddStringCorrection(
            dto.PersonalBelongingsDescription,
            ReceptionHistoryField.PersonalBelongingsDescription,
            reception.PersonalBelongingsDescription,
            requestedBelongingsDescription,
            changes);

        if (changes.Count == 0)
        {
            throw new InvalidOperationException(
                "La corrección no contiene cambios efectivos.");
        }

        reception.VeterinaryClinicId = requestedClinicId;
        reception.ReferringVeterinarianId = requestedVeterinarianId;
        reception.ReferralNotes = normalizedReferralNotes;
        reception.HasPersonalBelongings = requestedHasBelongings;
        reception.PersonalBelongingsDescription =
            requestedBelongingsDescription;

        var historyEvent = new ReceptionHistoryEvent
        {
            Id = Guid.NewGuid(),
            ReceptionId = reception.Id,
            CremationId = cremation.Id,
            RequestId = dto.RequestId,
            SequenceNumber = await GetNextHistorySequenceAsync(
                reception.Id),
            EventKind = ReceptionHistoryEventKind.Correction,
            ReceptionStage = stage,
            CremationStatusSnapshot = cremation.Status,
            Reason = reason,
            CreatedByUserId = actor.User.Id,
            CreatedByUserNameSnapshot =
                BuildUserNameSnapshot(actor.User),
            CreatedByRoleSnapshot = actor.RoleSnapshot,
            CreatedAt = DateTime.UtcNow,
            Changes = changes
        };

        _context.ReceptionHistoryEvents.Add(historyEvent);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapHistoryEvent(historyEvent);
    }

    public async Task<ReceptionHistoryEventDto?> CreateClarificationAsync(
        Guid id,
        CreateReceptionClarificationDto dto,
        Guid actorUserId)
    {
        Exception? concurrencyException = null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await CreateClarificationCoreAsync(
                    id,
                    dto,
                    actorUserId);
            }
            catch (Exception exception)
                when (IsReceptionConcurrencyConflict(exception))
            {
                concurrencyException = exception;
                _context.ChangeTracker.Clear();
            }
        }

        throw ReceptionConcurrencyConflict(concurrencyException!);
    }

    private async Task<ReceptionHistoryEventDto?> CreateClarificationCoreAsync(
        Guid id,
        CreateReceptionClarificationDto dto,
        Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la solicitud es obligatorio.");
        }

        var text = NormalizeOptionalText(dto.Text);

        if (text is null)
        {
            throw new ArgumentException(
                "El texto de la aclaración es obligatorio.");
        }

        if (text.Length > 1000)
        {
            throw new ArgumentException(
                "El texto de la aclaración no puede exceder 1000 caracteres.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que realiza la aclaración.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (!await LockActiveReceptionAsync(id))
        {
            return null;
        }

        var lockedCremationId =
            await LockCremationByReceptionAsync(id);

        var receptionExists = await _context.Receptions
            .AnyAsync(item =>
                item.Id == id &&
                item.IsActive);

        if (!receptionExists)
        {
            return null;
        }

        var cremation = await _context.Cremations
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == lockedCremationId);

        if (cremation is null)
        {
            throw new InvalidOperationException(
                "La recepción todavía no está vinculada a una cremación.");
        }

        var actor = await ResolveManageActorAsync(actorUserId);

        var existingEvent = await _context.ReceptionHistoryEvents
            .AsNoTracking()
            .Include(historyEvent => historyEvent.Changes)
            .FirstOrDefaultAsync(historyEvent =>
                historyEvent.ReceptionId == id &&
                historyEvent.RequestId == dto.RequestId);

        if (existingEvent is not null)
        {
            if (existingEvent.EventKind !=
                    ReceptionHistoryEventKind.Clarification ||
                !string.Equals(
                    existingEvent.ClarificationText,
                    text,
                    StringComparison.Ordinal))
            {
                throw RequestIdConflict();
            }

            await transaction.CommitAsync();
            return MapHistoryEvent(existingEvent);
        }

        var historyEvent = new ReceptionHistoryEvent
        {
            Id = Guid.NewGuid(),
            ReceptionId = id,
            CremationId = cremation.Id,
            RequestId = dto.RequestId,
            SequenceNumber = await GetNextHistorySequenceAsync(id),
            EventKind = ReceptionHistoryEventKind.Clarification,
            ReceptionStage = GetHistoryStage(cremation.Status),
            CremationStatusSnapshot = cremation.Status,
            ClarificationText = text,
            CreatedByUserId = actor.User.Id,
            CreatedByUserNameSnapshot =
                BuildUserNameSnapshot(actor.User),
            CreatedByRoleSnapshot = actor.RoleSnapshot,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReceptionHistoryEvents.Add(historyEvent);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapHistoryEvent(historyEvent);
    }

    public async Task<IReadOnlyList<ReceptionHistoryEventDto>?>
        GetHistoryAsync(Guid id)
    {
        var receptionExists = await _context.Receptions
            .AsNoTracking()
            .AnyAsync(item =>
                item.Id == id &&
                item.IsActive);

        if (!receptionExists)
        {
            return null;
        }

        return await _context.ReceptionHistoryEvents
            .AsNoTracking()
            .Where(historyEvent =>
                historyEvent.ReceptionId == id)
            .OrderBy(historyEvent => historyEvent.SequenceNumber)
            .Select(historyEvent => new ReceptionHistoryEventDto
            {
                Id = historyEvent.Id,
                ReceptionId = historyEvent.ReceptionId,
                CremationId = historyEvent.CremationId,
                RequestId = historyEvent.RequestId,
                Sequence = historyEvent.SequenceNumber,
                EventKind = historyEvent.EventKind,
                Stage = historyEvent.ReceptionStage,
                CremationStatus =
                    historyEvent.CremationStatusSnapshot,
                Reason = historyEvent.Reason,
                ClarificationText =
                    historyEvent.ClarificationText,
                ActorUserId = historyEvent.CreatedByUserId,
                ActorUserName =
                    historyEvent.CreatedByUserNameSnapshot,
                ActorRole = historyEvent.CreatedByRoleSnapshot,
                CreatedAt = historyEvent.CreatedAt,
                Changes = historyEvent.Changes
                    .OrderBy(change => change.Field)
                    .Select(change => new ReceptionHistoryChangeDto
                    {
                        Field = change.Field,
                        OriginalValue = change.OriginalValue,
                        NewValue = change.NewValue,
                        OriginalDisplayValue =
                            change.OriginalDisplayValue,
                        NewDisplayValue = change.NewDisplayValue
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    private async Task EnrichReceptionDtosAsync(
        IReadOnlyCollection<ReceptionDto> receptions)
    {
        if (receptions.Count == 0)
        {
            return;
        }

        var receptionIds = receptions
            .Select(item => item.Id)
            .ToArray();

        var states = await _context.Receptions
            .AsNoTracking()
            .Where(item => receptionIds.Contains(item.Id))
            .Select(item => new
            {
                item.Id,
                CremationStatus = item.Cremation == null
                    ? (CremationStatus?)null
                    : item.Cremation.Status,
                item.VeterinaryClinicId,
                VeterinaryClinicName = item.VeterinaryClinic == null
                    ? null
                    : item.VeterinaryClinic.Name,
                item.ReferringVeterinarianId,
                VeterinarianFirstName =
                    item.ReferringVeterinarian == null
                        ? null
                        : item.ReferringVeterinarian.FirstName,
                VeterinarianLastName =
                    item.ReferringVeterinarian == null
                        ? null
                        : item.ReferringVeterinarian.LastName,
                VeterinarianSecondLastName =
                    item.ReferringVeterinarian == null
                        ? null
                        : item.ReferringVeterinarian.SecondLastName,
                HasClinicCorrection = item.HistoryEvents.Any(
                    historyEvent =>
                        historyEvent.EventKind ==
                            ReceptionHistoryEventKind.Correction &&
                        historyEvent.Changes.Any(change =>
                            change.Field ==
                            ReceptionHistoryField.VeterinaryClinicId)),
                HasVeterinarianCorrection = item.HistoryEvents.Any(
                    historyEvent =>
                        historyEvent.EventKind ==
                            ReceptionHistoryEventKind.Correction &&
                        historyEvent.Changes.Any(change =>
                            change.Field ==
                            ReceptionHistoryField.ReferringVeterinarianId))
            })
            .ToDictionaryAsync(item => item.Id);

        var historyOnlyWeightRows =
            await _context.ReceptionHistoryEvents
                .AsNoTracking()
                .Where(historyEvent =>
                    receptionIds.Contains(historyEvent.ReceptionId) &&
                    historyEvent.EventKind ==
                        ReceptionHistoryEventKind.Correction &&
                    (historyEvent.ReceptionStage ==
                        ReceptionHistoryStage.CremationStarted ||
                     historyEvent.ReceptionStage ==
                        ReceptionHistoryStage.CremationCompleted ||
                     historyEvent.CremationStatusSnapshot ==
                        CremationStatus.Cancelled))
                .SelectMany(
                    historyEvent => historyEvent.Changes
                        .Where(change =>
                            change.Field ==
                            ReceptionHistoryField.VerifiedWeightKg),
                    (historyEvent, change) => new
                    {
                        historyEvent.ReceptionId,
                        historyEvent.SequenceNumber,
                        change.NewValue
                    })
                .OrderByDescending(item => item.SequenceNumber)
                .ToListAsync();

        var latestReportedWeights = historyOnlyWeightRows
            .GroupBy(item => item.ReceptionId)
            .ToDictionary(
                group => group.Key,
                group => group.First().NewValue);

        foreach (var reception in receptions)
        {
            if (!states.TryGetValue(reception.Id, out var state))
            {
                continue;
            }

            reception.HasCremation = state.CremationStatus.HasValue;
            reception.CremationStatus = state.CremationStatus;
            reception.IsNormalEditLocked = state.CremationStatus.HasValue;
            reception.CurrentHistoryStage = state.CremationStatus.HasValue
                ? GetHistoryStage(state.CremationStatus.Value)
                : ReceptionHistoryStage.BeforeCremation;

            if (state.HasClinicCorrection)
            {
                reception.VeterinaryClinicId =
                    state.VeterinaryClinicId;
                reception.VeterinaryClinicName =
                    state.VeterinaryClinicName;
            }

            if (state.HasVeterinarianCorrection)
            {
                reception.ReferringVeterinarianId =
                    state.ReferringVeterinarianId;
                reception.ReferringVeterinarianName = string.Join(
                    " ",
                    new[]
                    {
                        state.VeterinarianFirstName,
                        state.VeterinarianLastName,
                        state.VeterinarianSecondLastName
                    }.Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

                if (string.IsNullOrWhiteSpace(
                        reception.ReferringVeterinarianName))
                {
                    reception.ReferringVeterinarianName = null;
                }
            }

            if (latestReportedWeights.TryGetValue(
                    reception.Id,
                    out var storedWeight) &&
                TryParseWeight(storedWeight, out var reportedWeight) &&
                reportedWeight != reception.VerifiedWeightKg)
            {
                reception.LatestReportedCorrectedWeightKg =
                    reportedWeight;
            }
        }
    }

    private async Task BuildWeightCorrectionAsync(
        Reception reception,
        Cremation cremation,
        CreateReceptionCorrectionDto dto,
        List<ReceptionHistoryChange> changes)
    {
        if (dto.VerifiedWeightKg is null)
        {
            return;
        }

        var correctedWeight = dto.VerifiedWeightKg.Value;
        ValidateWeight(correctedWeight);

        if (reception.CollectionId.HasValue)
        {
            throw new InvalidOperationException(
                "El peso verificado de esta recepción ya fue " +
                "finalizado desde la recolección y no puede corregirse.");
        }

        if (IsHistoryOnlyWeightStatus(cremation.Status))
        {
            var previousReportedWeight =
                await GetLatestHistoryOnlyWeightAsync(reception.Id) ??
                reception.VerifiedWeightKg;

            if (correctedWeight == previousReportedWeight)
            {
                throw new InvalidOperationException(
                    "El peso corregido es igual al último peso reportado.");
            }

            changes.Add(CreateWeightChange(
                previousReportedWeight,
                correctedWeight));

            return;
        }

        if (cremation.Status is not CremationStatus.Pending and
            not CremationStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "El estado de la cremación no permite corregir el peso.");
        }

        if (correctedWeight == reception.VerifiedWeightKg)
        {
            throw new InvalidOperationException(
                "El peso corregido es igual al peso operativo actual.");
        }

        var originalWeight = reception.VerifiedWeightKg;

        var operationalResult =
            await ApplyOperationalWeightChangeAsync(
                reception,
                cremation,
                correctedWeight,
                dto.ConfirmWeightRangeChange,
                dto.ExpectedCremationPriceId,
                dto.ExpectedNewPrice);

        reception.VerifiedWeightKg = correctedWeight;
        changes.Add(CreateWeightChange(
            originalWeight,
            correctedWeight,
            FormatWeightRange(operationalResult.OriginalRange),
            FormatWeightRange(operationalResult.NewRange)));
    }

    private async Task<OperationalWeightChangeResult>
        ApplyOperationalWeightChangeAsync(
        Reception reception,
        Cremation? cremation,
        decimal correctedWeight,
        bool confirmWeightRangeChange,
        Guid? expectedCremationPriceId = null,
        decimal? expectedNewPrice = null)
    {
        if (reception.CollectionId.HasValue)
        {
            throw new InvalidOperationException(
                "El peso verificado de esta recepción ya fue " +
                "finalizado desde la recolección y no puede corregirse.");
        }

        var originalWeight = reception.VerifiedWeightKg;
        var minimumAllowedWeight =
            originalWeight * (1m - WeightCorrectionTolerance);
        var maximumAllowedWeight =
            originalWeight * (1m + WeightCorrectionTolerance);

        if (correctedWeight < minimumAllowedWeight ||
            correctedWeight > maximumAllowedWeight)
        {
            throw new InvalidOperationException(
                $"Verifica que sea la mascota correcta. " +
                $"El peso ingresado ({correctedWeight:F2} kg) " +
                $"está fuera de la tolerancia permitida de ±10% " +
                $"respecto al peso registrado ({originalWeight:F2} kg). " +
                $"El rango permitido es de {minimumAllowedWeight:F2} a " +
                $"{maximumAllowedWeight:F2} kg.");
        }

        var pricingConfiguration =
            await _context.CremationPricingConfigurations
                .AsNoTracking()
                .OrderBy(configuration => configuration.CreatedAt)
                .FirstOrDefaultAsync();

        if (pricingConfiguration is null)
        {
            throw new InvalidOperationException(
                "No existe una configuración activa de rangos de peso.");
        }

        var currentRange = GetWeightRange(
            originalWeight,
            pricingConfiguration.WeightInterval);
        var newRange = GetWeightRange(
            correctedWeight,
            pricingConfiguration.WeightInterval);
        var rangeDifference = Math.Abs(
            newRange.Index - currentRange.Index);

        if (rangeDifference > 1)
        {
            throw new InvalidOperationException(
                "Verifica que sea la mascota correcta. " +
                "El nuevo peso provocaría un cambio de dos o más " +
                "rangos de precio. No se realizó ningún cambio.");
        }

        if (cremation is not null &&
            cremation.Status is not CremationStatus.Pending and
            not CremationStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "El peso verificado no puede modificarse porque la " +
                "cremación ya no está en una etapa operativa previa.");
        }

        pcms.Application.CremationPricing.DTOs.CremationPriceQuoteDto?
            newQuote = null;
        decimal? previousPrice = null;
        PaymentAccount? paymentAccount = null;
        decimal? amountPaid = null;

        if (cremation?.CremationPackageId.HasValue == true)
        {
            newQuote = await _cremationPricingService.GetQuoteAsync(
                cremation.CremationPackageId.Value,
                correctedWeight,
                cremation.CremationType);
            previousPrice = cremation.QuotedPrice;

            paymentAccount = await _context.PaymentAccounts
                .Include(account => account.Payments)
                .FirstOrDefaultAsync(account =>
                    account.CremationId == cremation.Id);
            amountPaid = paymentAccount?.Payments.Sum(payment =>
                payment.Amount);
        }

        var rangeChanged = rangeDifference == 1;
        var result = new OperationalWeightChangeResult(
            currentRange,
            newRange);

        var currentQuoteWasConfirmed = newQuote is null ||
            expectedCremationPriceId == newQuote.CremationPriceId &&
            expectedNewPrice == newQuote.Price;

        if (rangeChanged &&
            (!confirmWeightRangeChange || !currentQuoteWasConfirmed))
        {
            throw new WeightRangeChangeConfirmationRequiredException(
                originalWeight,
                correctedWeight,
                currentRange.MinimumWeightKg,
                currentRange.MaximumWeightKg,
                newRange.MinimumWeightKg,
                newRange.MaximumWeightKg,
                previousPrice,
                newQuote?.Price,
                amountPaid,
                newQuote?.CremationPriceId);
        }

        if (cremation is null)
        {
            return result;
        }

        if (!rangeChanged)
        {
            if (cremation.CremationPackageId.HasValue)
            {
                cremation.QuotedWeightKg = correctedWeight;
            }

            return result;
        }

        if (newQuote is null)
        {
            return result;
        }

        if (paymentAccount is not null)
        {
            var paid = amountPaid ?? 0m;

            if (newQuote.Price < paid)
            {
                throw new InvalidOperationException(
                    $"La nueva cotización ({newQuote.Price:C2}) " +
                    $"no puede ser menor que el monto ya pagado " +
                    $"({paid:C2}). La corrección requiere una " +
                    "revisión administrativa.");
            }

            paymentAccount.ServiceTotal = newQuote.Price;
            paymentAccount.UpdatedAt = DateTime.UtcNow;
        }

        cremation.QuotedPrice = newQuote.Price;
        cremation.QuotedWeightKg = newQuote.WeightKg;
        cremation.QuotedMinimumWeightKg =
            newQuote.MinimumWeightKg;
        cremation.QuotedMaximumWeightKg =
            newQuote.MaximumWeightKg;

        return result;
    }

    private static void EnsureMatchingCorrectionRetry(
        ReceptionHistoryEvent existingEvent,
        CreateReceptionCorrectionDto dto)
    {
        if (existingEvent.EventKind !=
            ReceptionHistoryEventKind.Correction)
        {
            throw RequestIdConflict();
        }

        var reason = dto.Reason.Trim();

        if (!string.Equals(
                existingEvent.Reason,
                reason,
                StringComparison.Ordinal))
        {
            throw RequestIdConflict();
        }

        var requestedValues = GetRequestedCorrectionValues(dto);

        if (requestedValues.Count != existingEvent.Changes.Count)
        {
            throw RequestIdConflict();
        }

        foreach (var requestedValue in requestedValues)
        {
            var existingChange = existingEvent.Changes
                .SingleOrDefault(change =>
                    change.Field == requestedValue.Key);

            if (existingChange is null ||
                !string.Equals(
                    existingChange.NewValue,
                    requestedValue.Value,
                    StringComparison.Ordinal))
            {
                throw RequestIdConflict();
            }
        }

        var weightChange = existingEvent.Changes
            .SingleOrDefault(change =>
                change.Field ==
                ReceptionHistoryField.VerifiedWeightKg);

        var confirmationWasMaterial =
            existingEvent.CremationStatusSnapshot is
                CremationStatus.Pending or CremationStatus.Scheduled &&
            weightChange is not null &&
            weightChange.OriginalDisplayValue is not null &&
            weightChange.NewDisplayValue is not null &&
            !string.Equals(
                weightChange.OriginalDisplayValue,
                weightChange.NewDisplayValue,
                StringComparison.Ordinal);

        if (confirmationWasMaterial &&
            !dto.ConfirmWeightRangeChange)
        {
            throw RequestIdConflict();
        }
    }

    private static Dictionary<ReceptionHistoryField, string?>
        GetRequestedCorrectionValues(CreateReceptionCorrectionDto dto)
    {
        var values = new Dictionary<ReceptionHistoryField, string?>();

        if (dto.VerifiedWeightKg is not null)
        {
            values[ReceptionHistoryField.VerifiedWeightKg] =
                FormatWeight(dto.VerifiedWeightKg.Value);
        }

        if (dto.VeterinaryClinicId is not null)
        {
            values[ReceptionHistoryField.VeterinaryClinicId] =
                dto.VeterinaryClinicId.Value?.ToString("D");
        }

        if (dto.ReferringVeterinarianId is not null)
        {
            values[ReceptionHistoryField.ReferringVeterinarianId] =
                dto.ReferringVeterinarianId.Value?.ToString("D");
        }

        if (dto.HasPersonalBelongings is not null)
        {
            values[ReceptionHistoryField.HasPersonalBelongings] =
                dto.HasPersonalBelongings.Value
                    ? "true"
                    : "false";
        }

        if (dto.PersonalBelongingsDescription is not null)
        {
            values[ReceptionHistoryField.PersonalBelongingsDescription] =
                NormalizeOptionalText(
                    dto.PersonalBelongingsDescription.Value);
        }

        if (dto.ReferralNotes is not null)
        {
            values[ReceptionHistoryField.ReferralNotes] =
                NormalizeOptionalText(dto.ReferralNotes.Value);
        }

        return values;
    }

    private async Task<ActorEvidence> ResolveCorrectionActorAsync(
        Guid actorUserId)
    {
        var actor = await GetActiveActorAsync(actorUserId);

        if (actor.IsOwner)
        {
            return new ActorEvidence(actor, "Owner");
        }

        var role = await _context.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == actor.Id &&
                userRole.Role.IsActive &&
                (userRole.RoleId == ProtectedAdminRoleId ||
                 userRole.Role.NormalizedName == "MANAGER") &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission.Code ==
                    PermissionCodes.ReceptionsAmend))
            .Select(userRole => new
            {
                userRole.RoleId,
                userRole.Role.NormalizedName
            })
            .OrderBy(candidate =>
                candidate.RoleId == ProtectedAdminRoleId ? 0 : 1)
            .ThenBy(candidate => candidate.RoleId)
            .FirstOrDefaultAsync();

        if (role is null)
        {
            throw new UnauthorizedAccessException(
                "La corrección requiere un propietario, administrador " +
                "protegido o Manager activo con permiso para enmendar " +
                "recepciones.");
        }

        return new ActorEvidence(
            actor,
            role.RoleId == ProtectedAdminRoleId
                ? "Admin"
                : "Manager");
    }

    private async Task<ActorEvidence> ResolveManageActorAsync(
        Guid actorUserId)
    {
        var actor = await GetActiveActorAsync(actorUserId);

        if (actor.IsOwner)
        {
            return new ActorEvidence(actor, "Owner");
        }

        var role = await _context.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == actor.Id &&
                userRole.Role.IsActive &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission.Code ==
                    PermissionCodes.ReceptionsManage))
            .Select(userRole => new
            {
                userRole.RoleId,
                userRole.Role.Name,
                userRole.Role.NormalizedName
            })
            .OrderBy(candidate =>
                candidate.RoleId == ProtectedAdminRoleId ? 0 : 1)
            .ThenBy(candidate => candidate.NormalizedName)
            .ThenBy(candidate => candidate.RoleId)
            .FirstOrDefaultAsync();

        if (role is null)
        {
            throw new UnauthorizedAccessException(
                "El usuario autenticado no conserva un rol activo con " +
                "permiso para administrar recepciones.");
        }

        return new ActorEvidence(
            actor,
            role.RoleId == ProtectedAdminRoleId
                ? "Admin"
                : role.Name);
    }

    private async Task<User> GetActiveActorAsync(Guid actorUserId)
    {
        var actor = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == actorUserId &&
                user.IsActive);

        return actor ?? throw new UnauthorizedAccessException(
            "El usuario autenticado no existe o está inactivo.");
    }

    private async Task<bool> LockActiveReceptionAsync(Guid id)
    {
        var lockedReceptionIds =
            await _context.Database
                .SqlQuery<Guid>($"""
                    SELECT "Id" AS "Value"
                    FROM "Recepciones"
                    WHERE "Id" = {id}
                      AND "Activo" = TRUE
                    FOR UPDATE
                    """)
                .ToListAsync();

        return lockedReceptionIds.Count > 0;
    }

    private async Task<Guid?> LockCremationByReceptionAsync(
        Guid receptionId)
    {
        var cremationIds = await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "Cremaciones"
                WHERE "RecepcionId" = {receptionId}
                FOR UPDATE
                """)
            .ToListAsync();

        return cremationIds.Count == 0
            ? null
            : cremationIds[0];
    }

    private static bool IsReceptionConcurrencyConflict(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is not PostgresException postgresException)
            {
                continue;
            }

            if (postgresException.SqlState is
                PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }

            if (postgresException.SqlState ==
                    PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName is
                    "IX_EventosHistorialRecepcion_RecepcionId_NumeroSecuencia" or
                    "IX_EventosHistorialRecepcion_RecepcionId_SolicitudId")
            {
                return true;
            }
        }

        return false;
    }

    private static InvalidOperationException ReceptionConcurrencyConflict(
        Exception innerException) =>
        new(
            "La recepción cambió por otra operación concurrente. " +
            "Intente nuevamente.",
            innerException);

    private async Task<long> GetNextHistorySequenceAsync(Guid receptionId)
    {
        var currentSequence = await _context.ReceptionHistoryEvents
            .Where(historyEvent =>
                historyEvent.ReceptionId == receptionId)
            .MaxAsync(historyEvent =>
                (long?)historyEvent.SequenceNumber) ?? 0;

        return currentSequence + 1;
    }

    private async Task<decimal?> GetLatestHistoryOnlyWeightAsync(
        Guid receptionId)
    {
        var storedWeight = await _context.ReceptionHistoryEvents
            .AsNoTracking()
            .Where(historyEvent =>
                historyEvent.ReceptionId == receptionId &&
                historyEvent.EventKind ==
                    ReceptionHistoryEventKind.Correction &&
                (historyEvent.ReceptionStage ==
                    ReceptionHistoryStage.CremationStarted ||
                 historyEvent.ReceptionStage ==
                    ReceptionHistoryStage.CremationCompleted ||
                 historyEvent.CremationStatusSnapshot ==
                    CremationStatus.Cancelled))
            .OrderByDescending(historyEvent =>
                historyEvent.SequenceNumber)
            .SelectMany(historyEvent => historyEvent.Changes)
            .Where(change =>
                change.Field == ReceptionHistoryField.VerifiedWeightKg)
            .Select(change => change.NewValue)
            .FirstOrDefaultAsync();

        return TryParseWeight(storedWeight, out var weight)
            ? weight
            : null;
    }

    private static void ValidateCorrectionRequest(
        CreateReceptionCorrectionDto dto,
        Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la solicitud es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new ArgumentException(
                "El motivo de la corrección es obligatorio.");
        }

        if (dto.Reason.Trim().Length > 1000)
        {
            throw new ArgumentException(
                "El motivo de la corrección no puede exceder 1000 caracteres.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que realiza la corrección.");
        }

        if (GetRequestedCorrectionValues(dto).Count == 0)
        {
            throw new ArgumentException(
                "Debe solicitar al menos un campo para corregir.");
        }

        if (dto.PersonalBelongingsDescription?.Value?.Length > 500)
        {
            throw new ArgumentException(
                "La descripción de los objetos personales no puede " +
                "exceder 500 caracteres.");
        }

        if (dto.ReferralNotes?.Value?.Length > 1000)
        {
            throw new ArgumentException(
                "Las notas de referencia no pueden exceder 1000 caracteres.");
        }
    }

    private static void ValidateWeight(decimal weight)
    {
        if (weight is <= 0 or > 999.99m)
        {
            throw new ArgumentException(
                "El peso verificado debe estar entre 0.01 y 999.99 kg.");
        }

        if (decimal.Round(weight, 2) != weight)
        {
            throw new ArgumentException(
                "El peso verificado no puede tener más de dos decimales.");
        }
    }

    private static void ValidateBelongingsCorrection(
        Reception reception,
        CreateReceptionCorrectionDto dto,
        bool hasBelongings,
        string? description)
    {
        if (hasBelongings && description is null)
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }

        if (!hasBelongings && description is not null)
        {
            throw new ArgumentException(
                "No puede conservar una descripción cuando la recepción " +
                "no tiene objetos personales.");
        }

        var hasBelongingsChanges =
            dto.HasPersonalBelongings is not null &&
            hasBelongings != reception.HasPersonalBelongings;

        if (hasBelongingsChanges &&
            dto.PersonalBelongingsDescription is null &&
            reception.PersonalBelongingsDescription != description)
        {
            throw new ArgumentException(
                "Incluya también la corrección de la descripción de los " +
                "objetos personales.");
        }
    }

    private static void AddGuidCorrection(
        CorrectionValueDto<Guid?>? requested,
        ReceptionHistoryField field,
        Guid? originalValue,
        Guid? newValue,
        string? originalDisplayValue,
        string? newDisplayValue,
        ICollection<ReceptionHistoryChange> changes)
    {
        if (requested is null)
        {
            return;
        }

        if (originalValue == newValue)
        {
            throw FieldNoOp(field);
        }

        changes.Add(new ReceptionHistoryChange
        {
            Id = Guid.NewGuid(),
            Field = field,
            OriginalValue = originalValue?.ToString("D"),
            NewValue = newValue?.ToString("D"),
            OriginalDisplayValue = originalDisplayValue,
            NewDisplayValue = newDisplayValue
        });
    }

    private static void AddBooleanCorrection(
        CorrectionValueDto<bool>? requested,
        ReceptionHistoryField field,
        bool originalValue,
        bool newValue,
        ICollection<ReceptionHistoryChange> changes)
    {
        if (requested is null)
        {
            return;
        }

        if (originalValue == newValue)
        {
            throw FieldNoOp(field);
        }

        changes.Add(new ReceptionHistoryChange
        {
            Id = Guid.NewGuid(),
            Field = field,
            OriginalValue = originalValue ? "true" : "false",
            NewValue = newValue ? "true" : "false"
        });
    }

    private static void AddStringCorrection(
        CorrectionValueDto<string?>? requested,
        ReceptionHistoryField field,
        string? originalValue,
        string? newValue,
        ICollection<ReceptionHistoryChange> changes)
    {
        if (requested is null)
        {
            return;
        }

        if (string.Equals(
                originalValue,
                newValue,
                StringComparison.Ordinal))
        {
            throw FieldNoOp(field);
        }

        changes.Add(new ReceptionHistoryChange
        {
            Id = Guid.NewGuid(),
            Field = field,
            OriginalValue = originalValue,
            NewValue = newValue
        });
    }

    private static ReceptionHistoryChange CreateWeightChange(
        decimal originalWeight,
        decimal correctedWeight,
        string? originalDisplayValue = null,
        string? newDisplayValue = null)
    {
        return new ReceptionHistoryChange
        {
            Id = Guid.NewGuid(),
            Field = ReceptionHistoryField.VerifiedWeightKg,
            OriginalValue = FormatWeight(originalWeight),
            NewValue = FormatWeight(correctedWeight),
            OriginalDisplayValue = originalDisplayValue,
            NewDisplayValue = newDisplayValue
        };
    }

    private static string FormatWeightRange(WeightRangeInfo range)
    {
        return $"{FormatWeight(range.MinimumWeightKg)}–" +
            $"{FormatWeight(range.MaximumWeightKg)} kg";
    }

    private static string? GetEffectiveClinicDisplayName(
        Reception reception,
        bool hasPriorCorrection)
    {
        if (hasPriorCorrection)
        {
            return reception.VeterinaryClinic?.Name;
        }

        return reception.VeterinaryRequest?.VeterinaryClinicNameSnapshot ??
            reception.Collection?.VeterinaryClinicNameSnapshot ??
            reception.VeterinaryClinic?.Name;
    }

    private static string? GetEffectiveVeterinarianDisplayName(
        Reception reception,
        bool hasPriorCorrection)
    {
        if (hasPriorCorrection)
        {
            return GetVeterinarianFullName(
                reception.ReferringVeterinarian);
        }

        return reception.VeterinaryRequest?
                .ReferringVeterinarianNameSnapshot ??
            reception.Collection?.ReferringVeterinarianNameSnapshot ??
            GetVeterinarianFullName(reception.ReferringVeterinarian);
    }

    private static ReceptionHistoryEventDto MapHistoryEvent(
        ReceptionHistoryEvent historyEvent)
    {
        return new ReceptionHistoryEventDto
        {
            Id = historyEvent.Id,
            ReceptionId = historyEvent.ReceptionId,
            CremationId = historyEvent.CremationId,
            RequestId = historyEvent.RequestId,
            Sequence = historyEvent.SequenceNumber,
            EventKind = historyEvent.EventKind,
            Stage = historyEvent.ReceptionStage,
            CremationStatus = historyEvent.CremationStatusSnapshot,
            Reason = historyEvent.Reason,
            ClarificationText = historyEvent.ClarificationText,
            ActorUserId = historyEvent.CreatedByUserId,
            ActorUserName =
                historyEvent.CreatedByUserNameSnapshot,
            ActorRole = historyEvent.CreatedByRoleSnapshot,
            CreatedAt = historyEvent.CreatedAt,
            Changes = historyEvent.Changes
                .OrderBy(change => change.Field)
                .Select(change => new ReceptionHistoryChangeDto
                {
                    Field = change.Field,
                    OriginalValue = change.OriginalValue,
                    NewValue = change.NewValue,
                    OriginalDisplayValue =
                        change.OriginalDisplayValue,
                    NewDisplayValue = change.NewDisplayValue
                })
                .ToList()
        };
    }

    private static ReceptionHistoryStage GetHistoryStage(
        CremationStatus status)
    {
        return status switch
        {
            CremationStatus.Pending or
            CremationStatus.Scheduled or
            CremationStatus.Cancelled =>
                ReceptionHistoryStage.CremationCreatedNotStarted,

            CremationStatus.InProgress or
            CremationStatus.Cooling or
            CremationStatus.ProcessingRemains =>
                ReceptionHistoryStage.CremationStarted,

            CremationStatus.Completed or
            CremationStatus.ReadyForDelivery or
            CremationStatus.Delivered =>
                ReceptionHistoryStage.CremationCompleted,

            _ => throw new InvalidOperationException(
                "El estado de la cremación no es válido.")
        };
    }

    private static bool IsHistoryOnlyWeightStatus(
        CremationStatus status)
    {
        return status is
            CremationStatus.Cancelled or
            CremationStatus.InProgress or
            CremationStatus.Cooling or
            CremationStatus.ProcessingRemains or
            CremationStatus.Completed or
            CremationStatus.ReadyForDelivery or
            CremationStatus.Delivered;
    }

    private static InvalidOperationException FieldNoOp(
        ReceptionHistoryField field)
    {
        return new InvalidOperationException(
            $"El campo {field} no contiene un cambio efectivo.");
    }

    private static InvalidOperationException RequestIdConflict()
    {
        return new InvalidOperationException(
            "El identificador de solicitud ya fue utilizado con un " +
            "contenido diferente.");
    }

    private static string FormatWeight(decimal weight)
    {
        return weight.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static bool TryParseWeight(
        string? value,
        out decimal weight)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out weight);
    }

    private sealed record ActorEvidence(
        User User,
        string RoleSnapshot);

    private sealed record OperationalWeightChangeResult(
        WeightRangeInfo OriginalRange,
        WeightRangeInfo NewRange);
}
