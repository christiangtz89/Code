using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Domain.Enums;
using pcms.Application.Receptions.Exceptions;

namespace pcms.Infrastructure.Services;

public partial class ReceptionService : IReceptionService
{
    private const decimal WeightCorrectionTolerance = 0.10m;
    private static readonly Guid ProtectedAdminRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly AppDbContext _context;
    private readonly ICremationPricingService _cremationPricingService;

    public ReceptionService(
        AppDbContext context,
        ICremationPricingService cremationPricingService)
    {
        _context = context;
        _cremationPricingService = cremationPricingService;
    }

    public async Task<ReceptionDto> CreateAsync(
    CreateReceptionDto dto,
    Guid receivedByUserId)
    {
        if (dto.PetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar una mascota válida.");
        }

        if (receivedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que realiza la recepción.");
        }

        if (dto.VerifiedWeightKg <= 0)
        {
            throw new ArgumentException(
                "El peso verificado debe ser mayor que cero.");
        }

        if (decimal.Round(dto.VerifiedWeightKg, 2) !=
            dto.VerifiedWeightKg)
        {
            throw new ArgumentException(
                "El peso verificado no puede tener más de dos decimales.");
        }

        if (dto.HasPersonalBelongings &&
            string.IsNullOrWhiteSpace(
                dto.PersonalBelongingsDescription))
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var pet = CustomerPetWorkflowRules.RequireEligiblePet(
            await _context.Pets
            .Include(p => p.Customer)
            .Include(p => p.Reception)
            .FirstOrDefaultAsync(p => p.Id == dto.PetId));

        if (pet.Reception != null)
        {
            throw new InvalidOperationException(
                "La mascota ya tiene una recepción registrada.");
        }

        var hasActiveCollection =
            await _context.Collections
                .AsNoTracking()
                .AnyAsync(collection =>
                    collection.PetId == pet.Id &&
                    collection.IsActive &&
                    collection.Status != CollectionStatus.Received &&
                    collection.Status != CollectionStatus.Cancelled);

        if (hasActiveCollection)
        {
            throw new InvalidOperationException(
                "Esta mascota tiene una recolección activa. Complete la recepción desde la recolección existente.");
        }

        var receivedByUser = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == receivedByUserId &&
                u.IsActive);

        if (receivedByUser == null)
        {
            throw new ArgumentException(
                "El usuario que realiza la recepción no existe o está inactivo.");
        }

        string qrCode;

        do
        {
            qrCode =
                $"REC-{Guid.NewGuid():N}"
                    .ToUpperInvariant();
        }
        while (await _context.Receptions
            .AnyAsync(r => r.QrCode == qrCode));

        var referral =
        await ValidateReferralSourceAsync(
            dto.VeterinaryClinicId,
            dto.ReferringVeterinarianId,
            dto.ReferralNotes);



        VeterinaryClinic? veterinaryClinic = null;
        Veterinarian? referringVeterinarian = null;

        if (dto.VeterinaryClinicId.HasValue)
        {
            veterinaryClinic = await _context.VeterinaryClinics
                .FirstOrDefaultAsync(v =>
                    v.Id == dto.VeterinaryClinicId.Value &&
                    v.IsActive);

            if (veterinaryClinic == null)
            {
                throw new InvalidOperationException(
                    "Active veterinary clinic not found.");
            }
        }

        if (dto.ReferringVeterinarianId.HasValue)
        {
            referringVeterinarian = await _context.Veterinarians
                .FirstOrDefaultAsync(v =>
                    v.Id == dto.ReferringVeterinarianId.Value &&
                    v.IsActive);

            if (referringVeterinarian == null)
            {
                throw new InvalidOperationException(
                    "Active referring veterinarian not found.");
            }

            if (referringVeterinarian.VeterinaryClinicId !=
                dto.VeterinaryClinicId)
            {
                throw new InvalidOperationException(
                    "The referring veterinarian does not belong to the selected veterinary clinic.");
            }
        }

        var currentTime = DateTime.UtcNow;
        var identitySnapshot =
            CustomerPetWorkflowRules.CaptureReceptionIdentity(pet);

        var reception = new Reception
        {
            Id = Guid.NewGuid(),
            PetId = pet.Id,
            PetNameSnapshot = identitySnapshot.PetName,
            CustomerNameSnapshot = identitySnapshot.CustomerName,
            ReceivedByUserId = receivedByUser.Id,
            VeterinaryClinicId =
        referral.Clinic?.Id,

            ReferringVeterinarianId =
        referral.Veterinarian?.Id,
            ReferralNotes = string.IsNullOrWhiteSpace(dto.ReferralNotes)
                ? null
                : dto.ReferralNotes.Trim(),
            ReceivedAt = currentTime,
            QrCode = qrCode,
            VerifiedWeightKg = dto.VerifiedWeightKg,
            HasPersonalBelongings =
                dto.HasPersonalBelongings,
            PersonalBelongingsDescription =
                dto.HasPersonalBelongings
                    ? dto.PersonalBelongingsDescription?.Trim()
                    : null,
            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim(),
            IsActive = true,
            CreatedAt = currentTime
        };

        _context.Receptions.Add(reception);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new ReceptionDto
        {
            Id = reception.Id,
            PetId = reception.PetId,
            PetName = reception.PetNameSnapshot,
            CustomerId = pet.CustomerId,
            CustomerName = reception.CustomerNameSnapshot,
            ReceivedByUserId =
                reception.ReceivedByUserId,
            ReceivedByUserName =
                receivedByUser.FirstName + " " +
                receivedByUser.LastName,
            VeterinaryClinicId = reception.VeterinaryClinicId,

            VeterinaryClinicName = veterinaryClinic?.Name,

            ReferringVeterinarianId =
                reception.ReferringVeterinarianId,

            ReferringVeterinarianName =
                referringVeterinarian == null
                ? null
                : referringVeterinarian.FirstName + " " +
                referringVeterinarian.LastName,

            IsVeterinaryRequestOrigin = false,
            VeterinaryRequestId = null,
            IsCollectionOrigin = false,

            ReceivedAt = reception.ReceivedAt,
            QrCode = reception.QrCode,
            VerifiedWeightKg =
                reception.VerifiedWeightKg,
            HasPersonalBelongings =
                reception.HasPersonalBelongings,
            PersonalBelongingsDescription =
                reception.PersonalBelongingsDescription,
            ReferralNotes = reception.ReferralNotes,
            Notes = reception.Notes,
            IsActive = reception.IsActive,
            CreatedAt = reception.CreatedAt
        };


    }

    public async Task<PagedReceptionsDto> GetAllAsync(
    int page,
    int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "La página debe ser mayor que cero.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "El tamaño de página debe estar entre 1 y 100.");
        }

        var query = _context.Receptions
            .AsNoTracking()
            .Where(r => r.IsActive);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.PetNameSnapshot,

                CustomerId = r.Pet.CustomerId,
                CustomerName = r.CustomerNameSnapshot,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.Collection != null
                        ? r.Collection.ReceivedByUserNameSnapshot ??
                          r.ReceivedByUser.FirstName + " " + r.ReceivedByUser.LastName
                        : r.ReceivedByUser.FirstName + " " +
                          r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicId
                    : r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicNameSnapshot
                    : r.Collection != null
                        ? r.Collection.VeterinaryClinicNameSnapshot
                    : r.VeterinaryClinic != null
                        ? r.VeterinaryClinic.Name
                        : null,

                ReferringVeterinarianId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianId
                        : r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianNameSnapshot
                        : r.Collection != null
                            ? r.Collection.ReferringVeterinarianNameSnapshot
                        : r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                            r.ReferringVeterinarian.LastName
                            : null,

                IsVeterinaryRequestOrigin =
                    r.VeterinaryRequest != null,

                VeterinaryRequestId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.Id
                        : null,

                IsCollectionOrigin =
                    r.CollectionId.HasValue,

                ReceivedAt = r.ReceivedAt,
                QrCode = r.QrCode,
                VerifiedWeightKg =
                    r.VerifiedWeightKg,

                HasPersonalBelongings =
                    r.HasPersonalBelongings,

                PersonalBelongingsDescription =
                    r.PersonalBelongingsDescription,


                ReferralNotes = r.ReferralNotes,
                Notes = r.Notes,

                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        await EnrichReceptionDtosAsync(items);

        return new PagedReceptionsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize)
        };
    }

    public async Task<ReceptionDto?> GetByIdAsync(Guid id)
    {
        var reception = await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.Id == id &&
                r.IsActive)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.PetNameSnapshot,

                CustomerId = r.Pet.CustomerId,
                CustomerName = r.CustomerNameSnapshot,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.Collection != null
                        ? r.Collection.ReceivedByUserNameSnapshot ??
                          r.ReceivedByUser.FirstName + " " + r.ReceivedByUser.LastName
                        : r.ReceivedByUser.FirstName + " " +
                          r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicId
                    : r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicNameSnapshot
                    : r.Collection != null
                        ? r.Collection.VeterinaryClinicNameSnapshot
                    : r.VeterinaryClinic != null
                        ? r.VeterinaryClinic.Name
                        : null,

                ReferringVeterinarianId = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.ReferringVeterinarianId
                    : r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianNameSnapshot
                        : r.Collection != null
                            ? r.Collection.ReferringVeterinarianNameSnapshot
                        : r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                            r.ReferringVeterinarian.LastName
                            : null,

                IsVeterinaryRequestOrigin =
                    r.VeterinaryRequest != null,

                VeterinaryRequestId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.Id
                        : null,

                IsCollectionOrigin =
                    r.CollectionId.HasValue,

                ReceivedAt = r.ReceivedAt,
                QrCode = r.QrCode,
                VerifiedWeightKg =
                    r.VerifiedWeightKg,

                HasPersonalBelongings =
                    r.HasPersonalBelongings,
                PersonalBelongingsDescription =
                    r.PersonalBelongingsDescription,

                ReferralNotes = r.ReferralNotes,
                Notes = r.Notes,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (reception is not null)
        {
            await EnrichReceptionDtosAsync([reception]);
        }

        return reception;
    }

    public async Task<ReceptionDto?> GetByQrCodeAsync(
     string qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode))
        {
            return null;
        }

        var normalizedQrCode = qrCode
            .Trim()
            .ToUpperInvariant();

        var reception = await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.QrCode == normalizedQrCode &&
                r.IsActive)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.PetNameSnapshot,

                CustomerId = r.Pet.CustomerId,
                CustomerName = r.CustomerNameSnapshot,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.Collection != null
                        ? r.Collection.ReceivedByUserNameSnapshot ??
                          r.ReceivedByUser.FirstName + " " + r.ReceivedByUser.LastName
                        : r.ReceivedByUser.FirstName + " " +
                          r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicId
                    : r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicNameSnapshot
                    : r.Collection != null
                        ? r.Collection.VeterinaryClinicNameSnapshot
                    : r.VeterinaryClinic != null
                        ? r.VeterinaryClinic.Name
                        : null,

                ReferringVeterinarianId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianId
                        : r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianNameSnapshot
                        : r.Collection != null
                            ? r.Collection.ReferringVeterinarianNameSnapshot
                        : r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                            r.ReferringVeterinarian.LastName
                            : null,

                IsVeterinaryRequestOrigin =
                    r.VeterinaryRequest != null,

                VeterinaryRequestId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.Id
                        : null,

                IsCollectionOrigin =
                    r.CollectionId.HasValue,


                ReceivedAt = r.ReceivedAt,
                QrCode = r.QrCode,
                VerifiedWeightKg =
                    r.VerifiedWeightKg,

                HasPersonalBelongings =
                    r.HasPersonalBelongings,
                PersonalBelongingsDescription =
                    r.PersonalBelongingsDescription,

                ReferralNotes = r.ReferralNotes,
                Notes = r.Notes,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (reception is not null)
        {
            await EnrichReceptionDtosAsync([reception]);
        }

        return reception;
    }


    public async Task<ReceptionDto?> UpdateAsync(
        Guid id,
        UpdateReceptionDto dto,
        Guid actorUserId)
    {
        Exception? concurrencyException = null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await UpdateCoreAsync(id, dto, actorUserId);
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

    private async Task<ReceptionDto?> UpdateCoreAsync(
    Guid id,
    UpdateReceptionDto dto,
    Guid actorUserId)
    {
        if (dto.VerifiedWeightKg <= 0)
        {
            throw new ArgumentException(
                "El peso verificado debe ser mayor que cero.");
        }

        if (decimal.Round(dto.VerifiedWeightKg, 2) !=
            dto.VerifiedWeightKg)
        {
            throw new ArgumentException(
                "El peso verificado no puede tener más de dos decimales.");
        }

        if (dto.HasPersonalBelongings &&
            string.IsNullOrWhiteSpace(
                dto.PersonalBelongingsDescription))
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que realiza la edición.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (!await LockActiveReceptionAsync(id))
        {
            return null;
        }

        var linkedCremationId =
            await LockCremationByReceptionAsync(id);

        var reception = await _context.Receptions
            .Include(r => r.Pet)
                .ThenInclude(p => p.Customer)
            .Include(r => r.ReceivedByUser)
            .Include(r => r.VeterinaryRequest)
            .Include(r => r.Collection)
            .Include(r => r.VeterinaryClinic)
            .Include(r => r.ReferringVeterinarian)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.IsActive);

        if (reception == null)
        {
            return null;
        }

        var hasLinkedCremation = linkedCremationId.HasValue;

        var actor = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == actorUserId &&
                user.IsActive);

        if (actor is null)
        {
            throw new ArgumentException(
                "El usuario que realiza la edición no existe o está inactivo.");
        }

        var actorRoleSnapshot =
            await ResolveActorRoleSnapshotAsync(actor);

        var normalizedReferralNotes =
            NormalizeOptionalText(dto.ReferralNotes);

        var normalizedBelongingsDescription =
            dto.HasPersonalBelongings
                ? NormalizeOptionalText(
                    dto.PersonalBelongingsDescription)
                : null;

        var normalizedNotes =
            NormalizeOptionalText(dto.Notes);

        var importantFieldChanged =
            dto.VerifiedWeightKg != reception.VerifiedWeightKg ||
            dto.VeterinaryClinicId != reception.VeterinaryClinicId ||
            dto.ReferringVeterinarianId != reception.ReferringVeterinarianId ||
            dto.HasPersonalBelongings != reception.HasPersonalBelongings ||
            normalizedBelongingsDescription !=
                reception.PersonalBelongingsDescription ||
            normalizedReferralNotes != reception.ReferralNotes;

        if (hasLinkedCremation && importantFieldChanged)
        {
            throw new InvalidOperationException(
                "La recepción ya está vinculada a una cremación. " +
                "Los campos operativos deben corregirse mediante una enmienda.");
        }

        if (hasLinkedCremation &&
            normalizedNotes != reception.Notes)
        {
            throw new InvalidOperationException(
                "La recepción ya está vinculada a una cremación. " +
                "Agregue la información mediante el flujo de aclaraciones.");
        }

        var originalWeightKg =
            reception.VerifiedWeightKg;

        var originalVeterinaryClinicId =
            reception.VeterinaryClinicId;

        var originalVeterinaryClinicName =
            reception.VeterinaryClinic?.Name;

        var originalReferringVeterinarianId =
            reception.ReferringVeterinarianId;

        var originalReferringVeterinarianName =
            GetVeterinarianFullName(
                reception.ReferringVeterinarian);

        var originalHasPersonalBelongings =
            reception.HasPersonalBelongings;

        var originalPersonalBelongingsDescription =
            reception.PersonalBelongingsDescription;

        var originalReferralNotes =
            reception.ReferralNotes;

        var weightChanged =
            Math.Abs(
                dto.VerifiedWeightKg -
                originalWeightKg) >= 0.01m;

        if (weightChanged)
        {
            var cremation =
                await _context.Cremations
                    .FirstOrDefaultAsync(c =>
                        c.ReceptionId == reception.Id &&
                        c.IsActive);

            await ApplyOperationalWeightChangeAsync(
                reception,
                cremation,
                dto.VerifiedWeightKg,
                dto.ConfirmWeightRangeChange);
        }

        VeterinaryClinic? veterinaryClinic = null;
        Veterinarian? referringVeterinarian = null;

        if (reception.VeterinaryRequest is { } veterinaryRequest)
        {
            if (dto.VeterinaryClinicId !=
                    veterinaryRequest.VeterinaryClinicId ||
                dto.ReferringVeterinarianId !=
                    veterinaryRequest.ReferringVeterinarianId)
            {
                throw new InvalidOperationException(
                    "La fuente de una recepción originada por solicitud veterinaria no puede modificarse.");
            }

            reception.VeterinaryClinicId =
                veterinaryRequest.VeterinaryClinicId;

            reception.ReferringVeterinarianId =
                veterinaryRequest.ReferringVeterinarianId;
        }
        else if (reception.Collection is { } originCollection)
        {
            if (dto.VeterinaryClinicId != originCollection.VeterinaryClinicId ||
                dto.ReferringVeterinarianId != originCollection.ReferringVeterinarianId)
            {
                throw new InvalidOperationException(
                    "La fuente de una recepción originada por recolección no puede modificarse.");
            }

            reception.VeterinaryClinicId = originCollection.VeterinaryClinicId;
            reception.ReferringVeterinarianId = originCollection.ReferringVeterinarianId;
        }
        else
        {
            var referral =
                await ValidateReferralSourceAsync(
                    dto.VeterinaryClinicId,
                    dto.ReferringVeterinarianId,
                    dto.ReferralNotes);

            veterinaryClinic = referral.Clinic;
            referringVeterinarian = referral.Veterinarian;

            reception.VeterinaryClinicId =
                referral.Clinic?.Id;

            reception.ReferringVeterinarianId =
                referral.Veterinarian?.Id;
        }

        var historyChanges =
            new List<ReceptionHistoryChange>();

        if (dto.VerifiedWeightKg != originalWeightKg)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field = ReceptionHistoryField.VerifiedWeightKg,
                OriginalValue =
                    originalWeightKg.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture),
                NewValue =
                    dto.VerifiedWeightKg.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture)
            });
        }

        if (reception.VeterinaryClinicId !=
            originalVeterinaryClinicId)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field = ReceptionHistoryField.VeterinaryClinicId,
                OriginalValue =
                    originalVeterinaryClinicId?.ToString("D"),
                NewValue =
                    reception.VeterinaryClinicId?.ToString("D"),
                OriginalDisplayValue =
                    originalVeterinaryClinicName,
                NewDisplayValue =
                    veterinaryClinic?.Name
            });
        }

        if (reception.ReferringVeterinarianId !=
            originalReferringVeterinarianId)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field = ReceptionHistoryField.ReferringVeterinarianId,
                OriginalValue =
                    originalReferringVeterinarianId?.ToString("D"),
                NewValue =
                    reception.ReferringVeterinarianId?.ToString("D"),
                OriginalDisplayValue =
                    originalReferringVeterinarianName,
                NewDisplayValue =
                    GetVeterinarianFullName(
                        referringVeterinarian)
            });
        }

        if (dto.HasPersonalBelongings !=
            originalHasPersonalBelongings)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field = ReceptionHistoryField.HasPersonalBelongings,
                OriginalValue =
                    originalHasPersonalBelongings
                        ? "true"
                        : "false",
                NewValue =
                    dto.HasPersonalBelongings
                        ? "true"
                        : "false"
            });
        }

        if (normalizedBelongingsDescription !=
            originalPersonalBelongingsDescription)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field =
                    ReceptionHistoryField.PersonalBelongingsDescription,
                OriginalValue =
                    originalPersonalBelongingsDescription,
                NewValue =
                    normalizedBelongingsDescription
            });
        }

        if (normalizedReferralNotes != originalReferralNotes)
        {
            historyChanges.Add(new ReceptionHistoryChange
            {
                Id = Guid.NewGuid(),
                Field = ReceptionHistoryField.ReferralNotes,
                OriginalValue = originalReferralNotes,
                NewValue = normalizedReferralNotes
            });
        }

        if (!hasLinkedCremation && historyChanges.Count > 0)
        {
            var currentSequenceNumber =
                await _context.ReceptionHistoryEvents
                    .Where(historyEvent =>
                        historyEvent.ReceptionId == reception.Id)
                    .MaxAsync(historyEvent =>
                        (long?)historyEvent.SequenceNumber) ?? 0;

            _context.ReceptionHistoryEvents.Add(
                new ReceptionHistoryEvent
                {
                    Id = Guid.NewGuid(),
                    ReceptionId = reception.Id,
                    SequenceNumber = currentSequenceNumber + 1,
                    EventKind =
                        ReceptionHistoryEventKind.PreLockEditAudit,
                    ReceptionStage =
                        ReceptionHistoryStage.BeforeCremation,
                    CreatedByUserId = actor.Id,
                    CreatedByUserNameSnapshot =
                        BuildUserNameSnapshot(actor),
                    CreatedByRoleSnapshot = actorRoleSnapshot,
                    CreatedAt = DateTime.UtcNow,
                    Changes = historyChanges
                });
        }

        reception.ReferralNotes =
            normalizedReferralNotes;

        reception.VerifiedWeightKg =
            dto.VerifiedWeightKg;

        reception.HasPersonalBelongings =
            dto.HasPersonalBelongings;

        reception.PersonalBelongingsDescription =
            normalizedBelongingsDescription;

        reception.Notes =
            normalizedNotes;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var result = new ReceptionDto
        {
            Id = reception.Id,

            PetId = reception.PetId,
            PetName = reception.PetNameSnapshot,

            CustomerId =
                reception.Pet.CustomerId,

            CustomerName = reception.CustomerNameSnapshot,

            ReceivedByUserId =
                reception.ReceivedByUserId,

            ReceivedByUserName =
                reception.Collection?.ReceivedByUserNameSnapshot ??
                reception.ReceivedByUser.FirstName + " " +
                reception.ReceivedByUser.LastName,

            VeterinaryClinicId =
                reception.VeterinaryRequest?.VeterinaryClinicId ??
                reception.VeterinaryClinicId,

            VeterinaryClinicName =
                reception.VeterinaryRequest is { } request
                    ? request.VeterinaryClinicNameSnapshot
                    : reception.Collection?.VeterinaryClinicNameSnapshot ??
                      veterinaryClinic?.Name,

            ReferringVeterinarianId =
                reception.VeterinaryRequest?.ReferringVeterinarianId ??
                reception.ReferringVeterinarianId,

            ReferringVeterinarianName =
                reception.VeterinaryRequest is { } originRequest
                    ? originRequest.ReferringVeterinarianNameSnapshot
                    : reception.Collection is { } sourceCollection
                    ? sourceCollection.ReferringVeterinarianNameSnapshot
                    : referringVeterinarian == null
                    ? null
                    : referringVeterinarian.FirstName +
                      " " +
                      referringVeterinarian.LastName,

            IsVeterinaryRequestOrigin =
                reception.VeterinaryRequest is not null,

            VeterinaryRequestId =
                reception.VeterinaryRequest?.Id,

            IsCollectionOrigin =
                reception.CollectionId.HasValue,

            ReceivedAt =
                reception.ReceivedAt,

            QrCode =
                reception.QrCode,

            VerifiedWeightKg =
                reception.VerifiedWeightKg,

            HasPersonalBelongings =
                reception.HasPersonalBelongings,

            PersonalBelongingsDescription =
                reception.PersonalBelongingsDescription,

            ReferralNotes =
                reception.ReferralNotes,

            Notes =
                reception.Notes,

            IsActive =
                reception.IsActive,

            CreatedAt =
                reception.CreatedAt
        };

        await EnrichReceptionDtosAsync([result]);

        return result;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var reception = await _context.Receptions
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.IsActive);

        if (reception == null)
        {
            return false;
        }

        reception.IsActive = false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var reception = await _context.Receptions
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                !r.IsActive);

        if (reception == null)
        {
            return false;
        }

        reception.IsActive = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ReceptionDto>> SearchAsync(
    string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Array.Empty<ReceptionDto>();
        }

        var normalizedSearch = search
            .Trim()
            .ToLower();

        var receptions = await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                (
                    r.QrCode.ToLower()
                        .Contains(normalizedSearch) ||

                    r.PetNameSnapshot.ToLower()
                        .Contains(normalizedSearch) ||

                    r.CustomerNameSnapshot.ToLower()
                        .Contains(normalizedSearch) ||

                    r.ReceivedByUser.FirstName.ToLower()
                        .Contains(normalizedSearch) ||

                    r.ReceivedByUser.LastName.ToLower()
                        .Contains(normalizedSearch)
                ))
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.PetNameSnapshot,

                CustomerId = r.Pet.CustomerId,
                CustomerName = r.CustomerNameSnapshot,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.Collection != null
                        ? r.Collection.ReceivedByUserNameSnapshot ??
                          r.ReceivedByUser.FirstName + " " + r.ReceivedByUser.LastName
                        : r.ReceivedByUser.FirstName + " " +
                          r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicId
                    : r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryRequest != null
                    ? r.VeterinaryRequest.VeterinaryClinicNameSnapshot
                    : r.Collection != null
                        ? r.Collection.VeterinaryClinicNameSnapshot
                    : r.VeterinaryClinic != null
                        ? r.VeterinaryClinic.Name
                        : null,

                ReferringVeterinarianId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianId
                        : r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.ReferringVeterinarianNameSnapshot
                        : r.Collection != null
                            ? r.Collection.ReferringVeterinarianNameSnapshot
                        : r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                            r.ReferringVeterinarian.LastName
                            : null,

                IsVeterinaryRequestOrigin =
                    r.VeterinaryRequest != null,

                VeterinaryRequestId =
                    r.VeterinaryRequest != null
                        ? r.VeterinaryRequest.Id
                        : null,

                IsCollectionOrigin =
                    r.CollectionId.HasValue,

                ReceivedAt = r.ReceivedAt,
                QrCode = r.QrCode,
                VerifiedWeightKg =
                    r.VerifiedWeightKg,

                HasPersonalBelongings =
                    r.HasPersonalBelongings,
                PersonalBelongingsDescription =
                    r.PersonalBelongingsDescription,

                ReferralNotes = r.ReferralNotes,
                Notes = r.Notes,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        await EnrichReceptionDtosAsync(receptions);

        return receptions;
    }

    private async Task<string> ResolveActorRoleSnapshotAsync(
        User actor)
    {
        if (actor.IsOwner)
        {
            return "Owner";
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
                candidate.RoleId == ProtectedAdminRoleId
                    ? 0
                    : 1)
            .ThenBy(candidate => candidate.NormalizedName)
            .ThenBy(candidate => candidate.RoleId)
            .FirstOrDefaultAsync();

        if (role is null)
        {
            throw new InvalidOperationException(
                "El usuario autenticado no tiene un rol activo " +
                "con permiso para administrar recepciones.");
        }

        return role.RoleId == ProtectedAdminRoleId
            ? "Admin"
            : role.Name;
    }

    private static string BuildUserNameSnapshot(User user)
    {
        var name = string.Join(
            " ",
            new[]
            {
                user.FirstName,
                user.LastName
            }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));

        return name.Length <= 200
            ? name
            : name[..200];
    }

    private static string? GetVeterinarianFullName(
        Veterinarian? veterinarian)
    {
        if (veterinarian is null)
        {
            return null;
        }

        return string.Join(
            " ",
            new[]
            {
                veterinarian.FirstName,
                veterinarian.LastName,
                veterinarian.SecondLastName
            }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record WeightRangeInfo(
    int Index,
    decimal MinimumWeightKg,
    decimal MaximumWeightKg);

    private static WeightRangeInfo GetWeightRange(
        decimal weightKg,
        WeightPricingInterval interval)
    {
        var intervalKg = (decimal)(int)interval;

        var index =
            (int)Math.Ceiling(weightKg / intervalKg);

        if (index < 1)
        {
            index = 1;
        }

        var maximumWeightKg =
            index * intervalKg;

        var minimumWeightKg =
            index == 1
                ? 0.01m
                : ((index - 1) * intervalKg) + 0.01m;

        return new WeightRangeInfo(
            index,
            minimumWeightKg,
            maximumWeightKg);
    }

    private async Task<
        (
            VeterinaryClinic? Clinic,
            Veterinarian? Veterinarian
        )>
        ValidateReferralSourceAsync(
            Guid? veterinaryClinicId,
            Guid? veterinarianId,
            string? referralNotes)
    {
        if (!veterinaryClinicId.HasValue &&
            !veterinarianId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(
                referralNotes))
            {
                throw new ArgumentException(
                    "Debe seleccionar una veterinaria o un veterinario referente cuando se registran notas de referencia.");
            }

            return (null, null);
        }

        VeterinaryClinic? clinic = null;
        Veterinarian? veterinarian = null;

        if (veterinaryClinicId.HasValue)
        {
            clinic = await _context.VeterinaryClinics
                .FirstOrDefaultAsync(c =>
                    c.Id == veterinaryClinicId.Value &&
                    c.IsActive);

            if (clinic == null)
            {
                throw new InvalidOperationException(
                    "La veterinaria seleccionada no existe o está inactiva.");
            }
        }

        if (veterinarianId.HasValue)
        {
            veterinarian = await _context.Veterinarians
                .FirstOrDefaultAsync(v =>
                    v.Id == veterinarianId.Value &&
                    v.IsActive);

            if (veterinarian == null)
            {
                throw new InvalidOperationException(
                    "El veterinario referente no existe o está inactivo.");
            }

            if (veterinarian.VeterinaryClinicId.HasValue)
            {
                if (clinic == null)
                {
                    throw new InvalidOperationException(
                        "El veterinario seleccionado pertenece a una veterinaria; seleccione también la veterinaria asociada.");
                }

                if (veterinarian.VeterinaryClinicId.Value !=
                    clinic.Id)
                {
                    throw new InvalidOperationException(
                        "El veterinario referente no pertenece a la veterinaria seleccionada.");
                }
            }
            else if (clinic != null)
            {
                throw new InvalidOperationException(
                    "El veterinario seleccionado es independiente y no pertenece a la veterinaria seleccionada.");
            }
        }

        return (clinic, veterinarian);
    }
}
