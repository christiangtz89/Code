using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    private const string RevalidationIneligibleMessage =
        "La validación del pago requerido solo puede realizarse antes de iniciar la cremación.";

    private const string RevalidationAlreadyAppliedMessage =
        "El pago requerido de esta cremación ya fue validado.";

    private const string RevalidationStaleMessage =
        "El precio o el pago requerido cambió desde la confirmación. Revísalos nuevamente.";

    private const string RevalidationPricingMessage =
        "No fue posible determinar de forma segura el pago requerido de esta cremación.";

    public async Task<RevalidateStartPaymentResultDto?>
        RevalidateStartPaymentAsync(
            Guid id,
            RevalidateStartPaymentRequestDto dto,
            Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
        {
            throw new ArgumentException("La solicitud debe tener un identificador válido.");
        }

        if (!dto.ConfirmPriceChange &&
            (dto.ExpectedCurrentQuotedPrice.HasValue ||
             dto.ExpectedNewQuotedPrice.HasValue ||
             dto.ExpectedCremationPriceId.HasValue ||
             dto.ExpectedRequiredStartPaymentAmount.HasValue ||
             dto.ExpectedCurrentPaymentAccountServiceTotal.HasValue))
        {
            throw new ArgumentException(
                "La solicitud sin confirmación no debe incluir valores esperados de cotización.");
        }

        if (dto.ConfirmPriceChange &&
            !dto.ExpectedRequiredStartPaymentAmount.HasValue)
        {
            throw new ArgumentException(
                "La confirmación debe incluir el pago requerido esperado.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "No se pudo identificar al usuario autenticado.");
        }

        Exception? concurrencyException = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await RevalidateStartPaymentCoreAsync(
                    id, dto, actorUserId);
            }
            catch (Exception exception)
                when (IsCremationConcurrencyConflict(exception) ||
                      IsRevalidationUniqueConflict(exception))
            {
                concurrencyException = exception;
                _context.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException(
            "La cremación cambió por otra operación concurrente. Intente nuevamente.",
            concurrencyException);
    }

    private async Task<RevalidateStartPaymentResultDto?>
        RevalidateStartPaymentCoreAsync(
            Guid id,
            RevalidateStartPaymentRequestDto dto,
            Guid actorUserId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var receptionId = await LockReceptionForCremationAsync(id);

        if (!receptionId.HasValue ||
            !await LockActiveCremationAsync(id, receptionId.Value))
        {
            return null;
        }

        var cremation = await _context.Cremations
            .Include(candidate => candidate.Reception)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == id && candidate.IsActive);

        if (cremation is null)
        {
            return null;
        }

        var actor = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == actorUserId && user.IsActive);

        if (actor is null)
        {
            throw new InvalidOperationException(
                "El usuario que confirma la validación no está activo.");
        }

        var existing = await _context.CremationStartPaymentRevalidations
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.CremationId == id);

        if (existing is not null)
        {
            return BuildRevalidationReplay(existing, dto, actorUserId);
        }

        if (cremation.Status is not CremationStatus.Pending and
            not CremationStatus.Scheduled ||
            cremation.StartedAt.HasValue)
        {
            throw new InvalidOperationException(
                RevalidationIneligibleMessage);
        }

        if (cremation.RequiredStartPaymentAmount.HasValue)
        {
            throw new InvalidOperationException(
                RevalidationAlreadyAppliedMessage);
        }

        if (!cremation.CremationPackageId.HasValue ||
            cremation.Reception is null ||
            cremation.Reception.VerifiedWeightKg <= 0m ||
            !Enum.IsDefined(cremation.CremationType))
        {
            throw new InvalidOperationException(
                RevalidationPricingMessage);
        }

        pcms.Application.CremationPricing.DTOs.CremationPriceQuoteDto quote;

        try
        {
            quote = await _cremationPricingService.GetQuoteAsync(
                cremation.CremationPackageId.Value,
                cremation.Reception.VerifiedWeightKg,
                cremation.CremationType);
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                RevalidationPricingMessage, exception);
        }

        if (quote.Price <= 0m ||
            quote.RequiredCollectionPaymentAmount is not decimal required ||
            required <= 0m ||
            required > quote.Price)
        {
            throw new InvalidOperationException(
                RevalidationPricingMessage);
        }

        // Catalog edits that race with this decision force a serializable retry.
        var selectedPriceIds = await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "PreciosCremacion"
                WHERE "Id" = {quote.CremationPriceId}
                FOR SHARE
                """)
            .ToListAsync();

        if (selectedPriceIds.Count != 1)
        {
            throw new InvalidOperationException(
                RevalidationPricingMessage);
        }

        // Keep the same Reception -> Cremation -> PaymentAccount lock order.
        var accountIds = await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "CuentasPago"
                WHERE "CremacionId" = {id}
                FOR UPDATE
                """)
            .ToListAsync();

        var account = accountIds.Count == 0
            ? null
            : await _context.PaymentAccounts
                .Include(paymentAccount => paymentAccount.Payments)
                .FirstOrDefaultAsync(paymentAccount =>
                    paymentAccount.Id == accountIds[0]);

        var requiresConfirmation =
            cremation.QuotedPrice != quote.Price ||
            account is not null &&
            (account.CremationPriceId != quote.CremationPriceId ||
             account.ServiceTotal != quote.Price);

        var previousQuote = cremation.QuotedPrice;
        decimal? previousAccountTotalForConfirmation =
            account is not null && account.ServiceTotal != quote.Price
                ? account.ServiceTotal
                : null;

        if (requiresConfirmation)
        {
            if (!dto.ConfirmPriceChange)
            {
                return new RevalidateStartPaymentResultDto
                {
                    CremationId = id,
                    Applied = false,
                    PriceConfirmationRequired = true,
                    SelectedCremationPriceId = quote.CremationPriceId,
                    PreviousQuotedPrice = cremation.QuotedPrice,
                    PreviousPaymentAccountServiceTotal =
                        previousAccountTotalForConfirmation,
                    NewQuotedPrice = quote.Price,
                    RequiredStartPaymentAmount = required,
                    Message = "La cotización o el total de la cuenta requiere confirmación. Revisa los importes antes de validar el pago requerido."
                };
            }

            if (dto.ExpectedCurrentQuotedPrice != cremation.QuotedPrice ||
                dto.ExpectedNewQuotedPrice != quote.Price ||
                dto.ExpectedCremationPriceId != quote.CremationPriceId ||
                dto.ExpectedRequiredStartPaymentAmount != required ||
                dto.ExpectedCurrentPaymentAccountServiceTotal !=
                    previousAccountTotalForConfirmation)
            {
                throw new InvalidOperationException(
                    RevalidationStaleMessage);
            }

            if (account is not null)
            {
                var amountPaid = account.Payments.Sum(payment =>
                    payment.Amount);

                if (amountPaid > quote.Price)
                {
                    throw new InvalidOperationException(
                        "La nueva cotización no puede ser menor que el monto ya pagado.");
                }

                if (account.CremationPriceId != quote.CremationPriceId ||
                    account.CremationPackageId != quote.CremationPackageId ||
                    account.PackageName != quote.CremationPackageName ||
                    account.CremationTypeSnapshot != quote.CremationType ||
                    account.WeightKgSnapshot != quote.WeightKg ||
                    account.MinimumWeightKgSnapshot != quote.MinimumWeightKg ||
                    account.MaximumWeightKgSnapshot != quote.MaximumWeightKg ||
                    account.ServiceTotal != quote.Price)
                {
                    account.CremationPriceId = quote.CremationPriceId;
                    account.CremationPackageId = quote.CremationPackageId;
                    account.PackageName = quote.CremationPackageName;
                    account.CremationTypeSnapshot = quote.CremationType;
                    account.WeightKgSnapshot = quote.WeightKg;
                    account.MinimumWeightKgSnapshot = quote.MinimumWeightKg;
                    account.MaximumWeightKgSnapshot = quote.MaximumWeightKg;
                    if (account.ServiceTotal != quote.Price)
                    {
                        account.ServiceTotal = quote.Price;
                    }

                    account.UpdatedAt = DateTime.UtcNow;
                }
            }

            cremation.QuotedPrice = quote.Price;
            cremation.QuotedWeightKg = quote.WeightKg;
            cremation.QuotedMinimumWeightKg = quote.MinimumWeightKg;
            cremation.QuotedMaximumWeightKg = quote.MaximumWeightKg;
        }
        else if (dto.ConfirmPriceChange &&
                 (dto.ExpectedCurrentQuotedPrice != cremation.QuotedPrice ||
                  dto.ExpectedNewQuotedPrice != quote.Price ||
                  dto.ExpectedCremationPriceId != quote.CremationPriceId ||
                  dto.ExpectedRequiredStartPaymentAmount != required ||
                  dto.ExpectedCurrentPaymentAccountServiceTotal !=
                      previousAccountTotalForConfirmation))
        {
            throw new InvalidOperationException(
                RevalidationStaleMessage);
        }

        cremation.RequiredStartPaymentAmount = required;

        _context.CremationStartPaymentRevalidations.Add(
            new CremationStartPaymentRevalidation
            {
                Id = Guid.NewGuid(),
                CremationId = id,
                ConfirmedByUserId = actor.Id,
                ConfirmedByUserNameSnapshot =
                    string.Join(" ", new[]
                    {
                        actor.FirstName.Trim(),
                        actor.LastName.Trim()
                    }.Where(part => part.Length > 0)),
                ConfirmedAt = DateTime.UtcNow,
                RequestId = dto.RequestId,
                PriceChangeConfirmed = dto.ConfirmPriceChange,
                SelectedCremationPriceId = quote.CremationPriceId,
                PreviousQuotedPrice = previousQuote,
                PreviousPaymentAccountServiceTotal =
                    previousAccountTotalForConfirmation,
                NewQuotedPrice = quote.Price,
                EstablishedRequiredStartPaymentAmount = required
            });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new RevalidateStartPaymentResultDto
        {
            CremationId = id,
            Applied = true,
            SelectedCremationPriceId = quote.CremationPriceId,
            PreviousQuotedPrice = previousQuote,
            PreviousPaymentAccountServiceTotal =
                previousAccountTotalForConfirmation,
            NewQuotedPrice = quote.Price,
            RequiredStartPaymentAmount = required
        };
    }

    private static RevalidateStartPaymentResultDto BuildRevalidationReplay(
        CremationStartPaymentRevalidation existing,
        RevalidateStartPaymentRequestDto dto,
        Guid actorUserId)
    {
        if (existing.RequestId != dto.RequestId ||
            existing.ConfirmedByUserId != actorUserId ||
            existing.PriceChangeConfirmed != dto.ConfirmPriceChange ||
            existing.PriceChangeConfirmed &&
                (dto.ExpectedCurrentQuotedPrice != existing.PreviousQuotedPrice ||
                 dto.ExpectedNewQuotedPrice != existing.NewQuotedPrice ||
                 dto.ExpectedCremationPriceId != existing.SelectedCremationPriceId ||
                 dto.ExpectedRequiredStartPaymentAmount !=
                     existing.EstablishedRequiredStartPaymentAmount ||
                 dto.ExpectedCurrentPaymentAccountServiceTotal !=
                     existing.PreviousPaymentAccountServiceTotal) ||
            !existing.PriceChangeConfirmed &&
                (dto.ExpectedCurrentQuotedPrice.HasValue ||
                 dto.ExpectedNewQuotedPrice.HasValue ||
                 dto.ExpectedCremationPriceId.HasValue ||
                 dto.ExpectedRequiredStartPaymentAmount.HasValue ||
                 dto.ExpectedCurrentPaymentAccountServiceTotal.HasValue))
        {
            throw new InvalidOperationException(
                RevalidationAlreadyAppliedMessage);
        }

        return new RevalidateStartPaymentResultDto
        {
            CremationId = existing.CremationId,
            Applied = true,
            Replayed = true,
            SelectedCremationPriceId = existing.SelectedCremationPriceId,
            PreviousQuotedPrice = existing.PreviousQuotedPrice,
            PreviousPaymentAccountServiceTotal =
                existing.PreviousPaymentAccountServiceTotal,
            NewQuotedPrice = existing.NewQuotedPrice,
            RequiredStartPaymentAmount =
                existing.EstablishedRequiredStartPaymentAmount
        };
    }

    private static bool IsRevalidationUniqueConflict(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgres.ConstraintName ==
                    "IX_RevalidacionesPagoInicioCremacion_CremacionId")
            {
                return true;
            }
        }

        return false;
    }
}
