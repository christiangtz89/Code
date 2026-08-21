using Microsoft.EntityFrameworkCore;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Domain.Enums;
using pcms.Application.Receptions.Exceptions;

namespace pcms.Infrastructure.Services;

public class ReceptionService : IReceptionService
{
    private const decimal WeightCorrectionTolerance = 0.10m;
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

        if (dto.HasPersonalBelongings &&
            string.IsNullOrWhiteSpace(
                dto.PersonalBelongingsDescription))
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }

        var pet = await _context.Pets
            .Include(p => p.Customer)
            .Include(p => p.Reception)
            .FirstOrDefaultAsync(p =>
                p.Id == dto.PetId &&
                p.IsActive);

        if (pet == null)
        {
            throw new ArgumentException(
                "La mascota no existe o está inactiva.");
        }

        if (pet.Reception != null)
        {
            throw new InvalidOperationException(
                "La mascota ya tiene una recepción registrada.");
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

        var reception = new Reception
        {
            Id = Guid.NewGuid(),
            PetId = pet.Id,
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

        return new ReceptionDto
        {
            Id = reception.Id,
            PetId = reception.PetId,
            PetName = pet.Name,
            CustomerId = pet.CustomerId,
            CustomerName =
                pet.Customer.FirstName + " " +
                pet.Customer.LastName,
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
                PetName = r.Pet.Name,

                CustomerId = r.Pet.CustomerId,
                CustomerName =
                    r.Pet.Customer.FirstName + " " +
                    r.Pet.Customer.LastName,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.ReceivedByUser.FirstName + " " +
                    r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryClinic != null
                    ? r.VeterinaryClinic.Name
                    : null,

                ReferringVeterinarianId =
                    r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                        r.ReferringVeterinarian.LastName
                        : null,

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
        return await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.Id == id &&
                r.IsActive)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.Pet.Name,

                CustomerId = r.Pet.CustomerId,
                CustomerName =
                    r.Pet.Customer.FirstName + " " +
                    r.Pet.Customer.LastName,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.ReceivedByUser.FirstName + " " +
                    r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryClinic != null
                    ? r.VeterinaryClinic.Name
                    : null,

                ReferringVeterinarianId = r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                        r.ReferringVeterinarian.LastName
                        : null,

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

        return await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.QrCode == normalizedQrCode &&
                r.IsActive)
            .Select(r => new ReceptionDto
            {
                Id = r.Id,

                PetId = r.PetId,
                PetName = r.Pet.Name,

                CustomerId = r.Pet.CustomerId,
                CustomerName =
                    r.Pet.Customer.FirstName + " " +
                    r.Pet.Customer.LastName,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.ReceivedByUser.FirstName + " " +
                    r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryClinic != null
                    ? r.VeterinaryClinic.Name
                    : null,

                ReferringVeterinarianId =
                    r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                         r.ReferringVeterinarian.LastName
                        : null,


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
    }


    public async Task<ReceptionDto?> UpdateAsync(
    Guid id,
    UpdateReceptionDto dto)
    {
        if (dto.VerifiedWeightKg <= 0)
        {
            throw new ArgumentException(
                "El peso verificado debe ser mayor que cero.");
        }

        if (dto.HasPersonalBelongings &&
            string.IsNullOrWhiteSpace(
                dto.PersonalBelongingsDescription))
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }

        var reception = await _context.Receptions
            .Include(r => r.Pet)
                .ThenInclude(p => p.Customer)
            .Include(r => r.ReceivedByUser)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.IsActive);

        if (reception == null)
        {
            return null;
        }

        var originalWeightKg =
            reception.VerifiedWeightKg;

        var weightChanged =
            Math.Abs(
                dto.VerifiedWeightKg -
                originalWeightKg) >= 0.01m;

        if (weightChanged)
        {
            var minimumAllowedWeightKg =
                originalWeightKg *
                (1m - WeightCorrectionTolerance);

            var maximumAllowedWeightKg =
                originalWeightKg *
                (1m + WeightCorrectionTolerance);

            if (dto.VerifiedWeightKg <
                    minimumAllowedWeightKg ||
                dto.VerifiedWeightKg >
                    maximumAllowedWeightKg)
            {
                throw new InvalidOperationException(
                    $"Verifica que sea la mascota correcta. " +
                    $"El peso ingresado " +
                    $"({dto.VerifiedWeightKg:F2} kg) " +
                    $"está fuera de la tolerancia permitida " +
                    $"de ±10% respecto al peso registrado " +
                    $"({originalWeightKg:F2} kg). " +
                    $"El rango permitido es de " +
                    $"{minimumAllowedWeightKg:F2} a " +
                    $"{maximumAllowedWeightKg:F2} kg.");
            }

            var pricingConfiguration =
                await _context.CremationPricingConfigurations
                    .AsNoTracking()
                    .OrderBy(configuration =>
                        configuration.CreatedAt)
                    .FirstOrDefaultAsync();

            if (pricingConfiguration is null)
            {
                throw new InvalidOperationException(
                    "No existe una configuración activa de rangos de peso.");
            }

            var currentRange =
                GetWeightRange(
                    originalWeightKg,
                    pricingConfiguration.WeightInterval);

            var newRange =
                GetWeightRange(
                    dto.VerifiedWeightKg,
                    pricingConfiguration.WeightInterval);

            var rangeDifference =
                Math.Abs(
                    newRange.Index -
                    currentRange.Index);

            if (rangeDifference > 1)
            {
                throw new InvalidOperationException(
                    "Verifica que sea la mascota correcta. " +
                    "El nuevo peso provocaría un cambio de dos o más " +
                    "rangos de precio. No se realizó ningún cambio.");
            }

            var cremation =
                await _context.Cremations
                    .FirstOrDefaultAsync(c =>
                        c.ReceptionId == reception.Id &&
                        c.IsActive);

            if (cremation is not null &&
                cremation.Status != CremationStatus.Pending &&
                cremation.Status != CremationStatus.Scheduled)
            {
                throw new InvalidOperationException(
                    "El peso verificado no puede modificarse " +
                    "mediante el flujo normal porque la cremación " +
                    "ya fue iniciada. La corrección requiere una " +
                    "revisión administrativa.");
            }

            decimal? previousPrice = null;
            decimal? newPrice = null;

            pcms.Application.CremationPricing.DTOs
                .CremationPriceQuoteDto? newQuote = null;

            if (cremation?.CremationPackageId.HasValue == true)
            {
                newQuote =
                    await _cremationPricingService
                        .GetQuoteAsync(
                            cremation.CremationPackageId.Value,
                            dto.VerifiedWeightKg,
                            cremation.CremationType);

                previousPrice =
                    cremation.QuotedPrice;

                newPrice =
                    newQuote.Price;
            }

            var rangeChanged =
                rangeDifference == 1;

            if (rangeChanged &&
                !dto.ConfirmWeightRangeChange)
            {
                throw new WeightRangeChangeConfirmationRequiredException(
                    originalWeightKg,
                    dto.VerifiedWeightKg,
                    currentRange.MinimumWeightKg,
                    currentRange.MaximumWeightKg,
                    newRange.MinimumWeightKg,
                    newRange.MaximumWeightKg,
                    previousPrice,
                    newPrice);
            }

            if (cremation is not null)
            {
                if (!rangeChanged)
                {
                    if (cremation.CremationPackageId.HasValue)
                    {
                        // Same bracket:
                        // update only the verified quote weight.
                        // Preserve historical price and payment total.
                        cremation.QuotedWeightKg =
                            dto.VerifiedWeightKg;
                    }
                }
                else if (newQuote is not null)
                {
                    var paymentAccount =
                        await _context.PaymentAccounts
                            .Include(account =>
                                account.Payments)
                            .FirstOrDefaultAsync(account =>
                                account.CremationId ==
                                cremation.Id);

                    if (paymentAccount is not null)
                    {
                        var amountPaid =
                            paymentAccount.Payments.Sum(
                                payment =>
                                    payment.Amount);

                        if (newQuote.Price < amountPaid)
                        {
                            throw new InvalidOperationException(
                                $"La nueva cotización " +
                                $"({newQuote.Price:C2}) " +
                                $"no puede ser menor que el monto " +
                                $"ya pagado ({amountPaid:C2}). " +
                                "La corrección requiere una " +
                                "revisión administrativa.");
                        }

                        paymentAccount.ServiceTotal =
                            newQuote.Price;

                        paymentAccount.UpdatedAt =
                            DateTime.UtcNow;
                    }

                    cremation.QuotedPrice =
                        newQuote.Price;

                    cremation.QuotedWeightKg =
                        newQuote.WeightKg;

                    cremation.QuotedMinimumWeightKg =
                        newQuote.MinimumWeightKg;

                    cremation.QuotedMaximumWeightKg =
                        newQuote.MaximumWeightKg;
                }
            }
        }

        var referral =
            await ValidateReferralSourceAsync(
                dto.VeterinaryClinicId,
                dto.ReferringVeterinarianId,
                dto.ReferralNotes);

        var veterinaryClinic =
            referral.Clinic;

        var referringVeterinarian =
            referral.Veterinarian;

        reception.VeterinaryClinicId =
            referral.Clinic?.Id;

        reception.ReferringVeterinarianId =
            referral.Veterinarian?.Id;

        reception.ReferralNotes =
            string.IsNullOrWhiteSpace(
                dto.ReferralNotes)
                ? null
                : dto.ReferralNotes.Trim();

        reception.VerifiedWeightKg =
            dto.VerifiedWeightKg;

        reception.HasPersonalBelongings =
            dto.HasPersonalBelongings;

        reception.PersonalBelongingsDescription =
            dto.HasPersonalBelongings
                ? dto.PersonalBelongingsDescription?.Trim()
                : null;

        reception.Notes =
            string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim();

        await _context.SaveChangesAsync();

        return new ReceptionDto
        {
            Id = reception.Id,

            PetId = reception.PetId,
            PetName = reception.Pet.Name,

            CustomerId =
                reception.Pet.CustomerId,

            CustomerName =
                reception.Pet.Customer.FirstName + " " +
                reception.Pet.Customer.LastName,

            ReceivedByUserId =
                reception.ReceivedByUserId,

            ReceivedByUserName =
                reception.ReceivedByUser.FirstName + " " +
                reception.ReceivedByUser.LastName,

            VeterinaryClinicId =
                reception.VeterinaryClinicId,

            VeterinaryClinicName =
                veterinaryClinic?.Name,

            ReferringVeterinarianId =
                reception.ReferringVeterinarianId,

            ReferringVeterinarianName =
                referringVeterinarian == null
                    ? null
                    : referringVeterinarian.FirstName +
                      " " +
                      referringVeterinarian.LastName,

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

        return await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                (
                    r.QrCode.ToLower()
                        .Contains(normalizedSearch) ||

                    r.Pet.Name.ToLower()
                        .Contains(normalizedSearch) ||

                    r.Pet.Customer.FirstName.ToLower()
                        .Contains(normalizedSearch) ||

                    r.Pet.Customer.LastName.ToLower()
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
                PetName = r.Pet.Name,

                CustomerId = r.Pet.CustomerId,
                CustomerName =
                    r.Pet.Customer.FirstName + " " +
                    r.Pet.Customer.LastName,

                ReceivedByUserId =
                    r.ReceivedByUserId,
                ReceivedByUserName =
                    r.ReceivedByUser.FirstName + " " +
                    r.ReceivedByUser.LastName,

                VeterinaryClinicId = r.VeterinaryClinicId,

                VeterinaryClinicName = r.VeterinaryClinic != null
                    ? r.VeterinaryClinic.Name
                    : null,

                ReferringVeterinarianId =
                    r.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    r.ReferringVeterinarian != null
                        ? r.ReferringVeterinarian.FirstName + " " +
                         r.ReferringVeterinarian.LastName
                        : null,

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