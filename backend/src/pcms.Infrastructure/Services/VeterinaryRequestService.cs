using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.VeterinaryRequests.DTOs;
using pcms.Application.VeterinaryRequests.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class VeterinaryRequestService
    : IVeterinaryRequestService
{
    private readonly AppDbContext _context;

    public VeterinaryRequestService(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<VeterinaryRequestDto> CreateAsync(
        CreateVeterinaryRequestDto dto,
        Guid submittedByUserId)
    {
        var submittedByUser =
            await GetActiveUserAsync(
                submittedByUserId,
                "No se encontró un usuario activo para registrar la solicitud.");

        ValidateRequestData(
            dto.OwnerFirstName,
            dto.OwnerLastName,
            dto.OwnerPhone,
            dto.PetName,
            dto.Species,
            dto.Breed,
            dto.Sex,
            dto.Color,
            dto.ApproximateWeightKg,
            dto.DateOfDeath,
            dto.RequestedCremationType);

        var referral =
            await ValidateReferralSourceAsync(
                dto.VeterinaryClinicId,
                dto.ReferringVeterinarianId);

        var currentTime = DateTime.UtcNow;

        var request = new VeterinaryRequest
        {
            Id = Guid.NewGuid(),

            VeterinaryClinicId =
                referral.Clinic?.Id,

            ReferringVeterinarianId =
                referral.Veterinarian?.Id,

            SubmittedByUserId =
                submittedByUser.Id,

            Status =
                VeterinaryRequestStatus.Submitted,

            OwnerFirstName =
                dto.OwnerFirstName.Trim(),

            OwnerLastName =
                dto.OwnerLastName.Trim(),

            OwnerSecondLastName =
                NormalizeOptional(
                    dto.OwnerSecondLastName),

            OwnerPhone =
                dto.OwnerPhone.Trim(),

            OwnerEmail =
                NormalizeEmail(dto.OwnerEmail),

            PetName =
                dto.PetName.Trim(),

            Species =
                dto.Species.Trim(),

            Breed =
                dto.Breed.Trim(),

            Sex =
                dto.Sex.Trim(),

            Color =
                dto.Color.Trim(),

            ApproximateWeightKg =
                dto.ApproximateWeightKg,

            AgeYears =
                dto.AgeYears,

            DateOfDeath =
                NormalizeToUtc(dto.DateOfDeath),

            RequestedCremationType =
                dto.RequestedCremationType,

            RequestedPackageName =
                NormalizeOptional(
                    dto.RequestedPackageName),

            RequestNotes =
                NormalizeOptional(
                    dto.RequestNotes),

            SubmittedAt = currentTime,
            CreatedAt = currentTime
        };

        _context.VeterinaryRequests.Add(request);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(request.Id)
            ?? throw new InvalidOperationException(
                "No fue posible recuperar la solicitud creada.");
    }

    public async Task<PagedVeterinaryRequestsDto>
        GetAllAsync(
            int page,
            int pageSize,
            VeterinaryRequestStatus? status)
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

        var query = _context.VeterinaryRequests
            .AsNoTracking()
            .AsQueryable();

        query = ApplyStatusFilter(
            query,
            status);

        var totalItems =
            await query.CountAsync();

        var ids = await query
            .OrderByDescending(vr =>
                vr.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(vr => vr.Id)
            .ToListAsync();

        var items =
            await GetDtosByIdsAsync(ids);

        return new PagedVeterinaryRequestsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,

            TotalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)pageSize)
        };
    }

    public async Task<VeterinaryRequestDto?>
        GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var items =
            await GetDtosByIdsAsync(
                new[] { id });

        return items.FirstOrDefault();
    }

    public async Task<VeterinaryRequestDto?>
        UpdateAsync(
            Guid id,
            UpdateVeterinaryRequestDto dto)
    {
        var request =
            await _context.VeterinaryRequests
                .FirstOrDefaultAsync(vr =>
                    vr.Id == id);

        if (request == null)
        {
            return null;
        }

        if (request.Status !=
                VeterinaryRequestStatus.Submitted &&
            request.Status !=
                VeterinaryRequestStatus.UnderReview)
        {
            throw new InvalidOperationException(
                "Solo se pueden editar solicitudes recibidas o en revisión.");
        }

        ValidateRequestData(
            dto.OwnerFirstName,
            dto.OwnerLastName,
            dto.OwnerPhone,
            dto.PetName,
            dto.Species,
            dto.Breed,
            dto.Sex,
            dto.Color,
            dto.ApproximateWeightKg,
            dto.DateOfDeath,
            dto.RequestedCremationType);

        var referral =
            await ValidateReferralSourceAsync(
                dto.VeterinaryClinicId,
                dto.ReferringVeterinarianId);

        request.VeterinaryClinicId =
            referral.Clinic?.Id;

        request.ReferringVeterinarianId =
            referral.Veterinarian?.Id;

        request.OwnerFirstName =
            dto.OwnerFirstName.Trim();

        request.OwnerLastName =
            dto.OwnerLastName.Trim();

        request.OwnerSecondLastName =
            NormalizeOptional(
                dto.OwnerSecondLastName);

        request.OwnerPhone =
            dto.OwnerPhone.Trim();

        request.OwnerEmail =
            NormalizeEmail(dto.OwnerEmail);

        request.PetName =
            dto.PetName.Trim();

        request.Species =
            dto.Species.Trim();

        request.Breed =
            dto.Breed.Trim();

        request.Sex =
            dto.Sex.Trim();

        request.Color =
            dto.Color.Trim();

        request.ApproximateWeightKg =
            dto.ApproximateWeightKg;

        request.AgeYears =
            dto.AgeYears;

        request.DateOfDeath =
            NormalizeToUtc(dto.DateOfDeath);

        request.RequestedCremationType =
            dto.RequestedCremationType;

        request.RequestedPackageName =
            NormalizeOptional(
                dto.RequestedPackageName);

        request.RequestNotes =
            NormalizeOptional(
                dto.RequestNotes);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(request.Id);
    }

    public async Task<VeterinaryRequestDto?>
        ChangeStatusAsync(
            Guid id,
            ChangeVeterinaryRequestStatusDto dto,
            Guid reviewedByUserId)
    {
        if (!Enum.IsDefined(
                typeof(VeterinaryRequestStatus),
                dto.Status))
        {
            throw new ArgumentException(
                "El estado de la solicitud no es válido.");
        }

        if (dto.Status ==
            VeterinaryRequestStatus.Converted)
        {
            throw new InvalidOperationException(
                "Utilice la conversión de solicitud para cambiar al estado Convertida.");
        }

        var reviewer =
            await GetActiveUserAsync(
                reviewedByUserId,
                "No se encontró un usuario activo para revisar la solicitud.");

        var request =
            await _context.VeterinaryRequests
                .FirstOrDefaultAsync(vr =>
                    vr.Id == id);

        if (request == null)
        {
            return null;
        }

        if (!IsValidTransition(
                request.Status,
                dto.Status))
        {
            throw new InvalidOperationException(
                $"No se puede cambiar la solicitud de {request.Status} a {dto.Status}.");
        }

        if (dto.Status ==
                VeterinaryRequestStatus.Rejected &&
            string.IsNullOrWhiteSpace(
                dto.RejectionReason))
        {
            throw new ArgumentException(
                "Debe proporcionar un motivo de rechazo.");
        }

        var currentTime = DateTime.UtcNow;

        request.Status = dto.Status;

        if (dto.InternalNotes != null)
        {
            request.InternalNotes =
                NormalizeOptional(
                    dto.InternalNotes);
        }

        if (dto.Status ==
            VeterinaryRequestStatus.Rejected)
        {
            request.RejectionReason =
                dto.RejectionReason!.Trim();
        }
        else
        {
            request.RejectionReason = null;
        }

        request.ReviewedByUserId =
            reviewer.Id;

        request.ReviewedAt =
            currentTime;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(request.Id);
    }

    public async Task<VeterinaryRequestDto?>
        ConvertAsync(
            Guid id,
            ConvertVeterinaryRequestDto dto,
            Guid receivedByUserId)
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

        var receivedByUser =
            await GetActiveUserAsync(
                receivedByUserId,
                "El usuario que realiza la recepción no existe o está inactivo.");

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable);

        var request =
            await _context.VeterinaryRequests
                .Include(vr => vr.Reception)
                .FirstOrDefaultAsync(vr =>
                    vr.Id == id);

        if (request == null)
        {
            return null;
        }

        if (request.Status !=
            VeterinaryRequestStatus.Approved)
        {
            throw new InvalidOperationException(
                "Solo una solicitud aprobada puede convertirse en recepción.");
        }

        if (request.ReceptionId.HasValue ||
            request.Reception != null)
        {
            throw new InvalidOperationException(
                "La solicitud ya fue convertida en una recepción.");
        }

        var referral =
            await ValidateReferralSourceAsync(
                request.VeterinaryClinicId,
                request.ReferringVeterinarianId);

        Customer customer;
        Pet pet;

        if (dto.ExistingPetId.HasValue)
        {
            pet = await _context.Pets
                .Include(p => p.Customer)
                .Include(p => p.Reception)
                .FirstOrDefaultAsync(p =>
                    p.Id ==
                        dto.ExistingPetId.Value &&
                    p.IsActive &&
                    p.Customer.IsActive)
                ?? throw new InvalidOperationException(
                    "No se encontró una mascota activa válida.");

            if (pet.Reception != null)
            {
                throw new InvalidOperationException(
                    "La mascota seleccionada ya tiene una recepción registrada.");
            }

            customer = pet.Customer;

            if (dto.ExistingCustomerId.HasValue &&
                pet.CustomerId !=
                    dto.ExistingCustomerId.Value)
            {
                throw new InvalidOperationException(
                    "La mascota seleccionada no pertenece al cliente seleccionado.");
            }
        }
        else
        {
            if (!dto.ExistingCustomerId.HasValue &&
    string.IsNullOrWhiteSpace(request.OwnerEmail))
            {
                throw new InvalidOperationException(
                    "El correo electrónico del propietario es obligatorio para crear un nuevo cliente.");
            }

            if (dto.ExistingCustomerId.HasValue)
            {
                customer =
                    await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.Id ==
                                dto.ExistingCustomerId.Value &&
                            c.IsActive)
                    ?? throw new InvalidOperationException(
                        "No se encontró un cliente activo válido.");
            }
            else
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),

                    FirstName =
                        request.OwnerFirstName,

                    LastName =
                        request.OwnerLastName,

                    SecondLastName =
                        request.OwnerSecondLastName,

                    Phone =
                        request.OwnerPhone,

                    Email = request.OwnerEmail!,

                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
            }

            pet = new Pet
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,

                Name =
                    request.PetName,

                Species =
                    request.Species,

                Breed =
                    request.Breed,

                Sex =
                    request.Sex,

                Color =
                    request.Color,

                WeightKg =
                    request.ApproximateWeightKg,

                AgeYears =
                    request.AgeYears,

                DateOfDeath =
                    request.DateOfDeath,

                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Pets.Add(pet);
        }

        string qrCode;

        do
        {
            qrCode =
                $"REC-{Guid.NewGuid():N}"
                    .ToUpperInvariant();
        }
        while (await _context.Receptions
            .AnyAsync(r =>
                r.QrCode == qrCode));

        var currentTime = DateTime.UtcNow;

        var reception = new Reception
        {
            Id = Guid.NewGuid(),

            PetId = pet.Id,

            ReceivedByUserId =
                receivedByUser.Id,

            VeterinaryClinicId =
                referral.Clinic?.Id,

            ReferringVeterinarianId =
                referral.Veterinarian?.Id,

            ReceivedAt = currentTime,

            QrCode = qrCode,

            VerifiedWeightKg =
                dto.VerifiedWeightKg,

            HasPersonalBelongings =
                dto.HasPersonalBelongings,

            PersonalBelongingsDescription =
                dto.HasPersonalBelongings
                    ? NormalizeOptional(
                        dto.PersonalBelongingsDescription)
                    : null,

            ReferralNotes =
                NormalizeOptional(
                    dto.ReferralNotes),

            Notes =
                NormalizeOptional(dto.Notes),

            IsActive = true,
            CreatedAt = currentTime
        };

        _context.Receptions.Add(reception);

        request.ReceptionId =
            reception.Id;

        request.Status =
            VeterinaryRequestStatus.Converted;

        request.ConvertedAt =
            currentTime;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetByIdAsync(request.Id);
    }

    public async Task<IEnumerable<VeterinaryRequestDto>>
        SearchAsync(
            string search,
            VeterinaryRequestStatus? status)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Array.Empty<VeterinaryRequestDto>();
        }

        var term = search.Trim();
        var pattern = $"%{term}%";

        var query =
            _context.VeterinaryRequests
                .AsNoTracking()
                .Where(vr =>
                    EF.Functions.ILike(
                        vr.OwnerFirstName,
                        pattern) ||

                    EF.Functions.ILike(
                        vr.OwnerLastName,
                        pattern) ||

                    (
                        vr.OwnerSecondLastName != null &&
                        EF.Functions.ILike(
                            vr.OwnerSecondLastName,
                            pattern)
                    ) ||

                    EF.Functions.ILike(
                        vr.OwnerPhone,
                        pattern) ||

                    (
                        vr.OwnerEmail != null &&
                        EF.Functions.ILike(
                            vr.OwnerEmail,
                            pattern)
                    ) ||

                    EF.Functions.ILike(
                        vr.PetName,
                        pattern) ||

                    EF.Functions.ILike(
                        vr.Species,
                        pattern) ||

                    EF.Functions.ILike(
                        vr.Breed,
                        pattern) ||

                    (
                        vr.VeterinaryClinic != null &&
                        EF.Functions.ILike(
                            vr.VeterinaryClinic.Name,
                            pattern)
                    ) ||

                    (
                        vr.ReferringVeterinarian != null &&
                        (
                            EF.Functions.ILike(
                                vr.ReferringVeterinarian
                                    .FirstName,
                                pattern) ||

                            EF.Functions.ILike(
                                vr.ReferringVeterinarian
                                    .LastName,
                                pattern) ||

                            (
                                vr.ReferringVeterinarian
                                    .SecondLastName != null &&
                                EF.Functions.ILike(
                                    vr.ReferringVeterinarian
                                        .SecondLastName,
                                    pattern)
                            )
                        )
                    ) ||

                    (
                        vr.RequestedPackageName != null &&
                        EF.Functions.ILike(
                            vr.RequestedPackageName,
                            pattern)
                    ));

        query =
            ApplyStatusFilter(
                query,
                status);

        var ids = await query
            .OrderByDescending(vr =>
                vr.SubmittedAt)
            .Select(vr => vr.Id)
            .ToListAsync();

        return await GetDtosByIdsAsync(ids);
    }

    private async Task<
        (
            VeterinaryClinic? Clinic,
            Veterinarian? Veterinarian
        )>
        ValidateReferralSourceAsync(
            Guid? veterinaryClinicId,
            Guid? veterinarianId)
    {
        if (!veterinaryClinicId.HasValue &&
            !veterinarianId.HasValue)
        {
            throw new ArgumentException(
                "Debe seleccionar una veterinaria o un veterinario referente.");
        }

        VeterinaryClinic? clinic = null;
        Veterinarian? veterinarian = null;

        if (veterinaryClinicId.HasValue)
        {
            clinic =
                await _context.VeterinaryClinics
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id ==
                            veterinaryClinicId.Value &&
                        c.IsActive);

            if (clinic == null)
            {
                throw new InvalidOperationException(
                    "La veterinaria seleccionada no existe o está inactiva.");
            }
        }

        if (veterinarianId.HasValue)
        {
            veterinarian =
                await _context.Veterinarians
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v =>
                        v.Id ==
                            veterinarianId.Value &&
                        v.IsActive);

            if (veterinarian == null)
            {
                throw new InvalidOperationException(
                    "El veterinario referente no existe o está inactivo.");
            }

            if (veterinarian
                .VeterinaryClinicId
                .HasValue)
            {
                if (clinic == null)
                {
                    throw new InvalidOperationException(
                        "El veterinario seleccionado pertenece a una veterinaria; seleccione también la veterinaria asociada.");
                }

                if (veterinarian
                        .VeterinaryClinicId.Value !=
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

    private async Task<User> GetActiveUserAsync(
        Guid userId,
        string errorMessage)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                errorMessage);
        }

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.IsActive)
            ?? throw new ArgumentException(
                errorMessage);
    }

    private static void ValidateRequestData(
        string ownerFirstName,
        string ownerLastName,
        string ownerPhone,
        string petName,
        string species,
        string breed,
        string sex,
        string color,
        decimal approximateWeightKg,
        DateTime dateOfDeath,
        CremationType? requestedCremationType)
    {
        if (string.IsNullOrWhiteSpace(
            ownerFirstName))
        {
            throw new ArgumentException(
                "El nombre del propietario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            ownerLastName))
        {
            throw new ArgumentException(
                "El apellido paterno del propietario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            ownerPhone))
        {
            throw new ArgumentException(
                "El teléfono del propietario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(petName))
        {
            throw new ArgumentException(
                "El nombre de la mascota es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            throw new ArgumentException(
                "La especie es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            throw new ArgumentException(
                "La raza es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(sex))
        {
            throw new ArgumentException(
                "El sexo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(color))
        {
            throw new ArgumentException(
                "El color es obligatorio.");
        }

        if (approximateWeightKg <= 0)
        {
            throw new ArgumentException(
                "El peso aproximado debe ser mayor que cero.");
        }

        if (dateOfDeath == default)
        {
            throw new ArgumentException(
                "La fecha de fallecimiento es obligatoria.");
        }

        var dateOfDeathUtc =
            NormalizeToUtc(dateOfDeath);

        if (dateOfDeathUtc > DateTime.UtcNow)
        {
            throw new ArgumentException(
                "La fecha de fallecimiento no puede estar en el futuro.");
        }

        if (requestedCremationType.HasValue &&
            !Enum.IsDefined(
                typeof(CremationType),
                requestedCremationType.Value))
        {
            throw new ArgumentException(
                "El tipo de cremación solicitado no es válido.");
        }
    }

    private static bool IsValidTransition(
        VeterinaryRequestStatus current,
        VeterinaryRequestStatus next)
    {
        return current switch
        {
            VeterinaryRequestStatus.Submitted =>
                next is
                    VeterinaryRequestStatus.UnderReview
                    or VeterinaryRequestStatus.Cancelled,

            VeterinaryRequestStatus.UnderReview =>
                next is
                    VeterinaryRequestStatus.Approved
                    or VeterinaryRequestStatus.Rejected
                    or VeterinaryRequestStatus.Cancelled,

            VeterinaryRequestStatus.Approved =>
                next is
                    VeterinaryRequestStatus.Cancelled,

            VeterinaryRequestStatus.Rejected =>
                false,

            VeterinaryRequestStatus.Converted =>
                false,

            VeterinaryRequestStatus.Cancelled =>
                false,

            _ => false
        };
    }

    private static IQueryable<VeterinaryRequest>
        ApplyStatusFilter(
            IQueryable<VeterinaryRequest> query,
            VeterinaryRequestStatus? status)
    {
        if (!status.HasValue)
        {
            return query;
        }

        return query.Where(vr =>
            vr.Status == status.Value);
    }

    private async Task<List<VeterinaryRequestDto>>
        GetDtosByIdsAsync(
            IEnumerable<Guid> ids)
    {
        var orderedIds = ids.ToList();

        if (orderedIds.Count == 0)
        {
            return new List<VeterinaryRequestDto>();
        }

        var requests =
            await _context.VeterinaryRequests
                .AsNoTracking()
                .Where(vr =>
                    orderedIds.Contains(vr.Id))
                .Include(vr =>
                    vr.VeterinaryClinic)
                .Include(vr =>
                    vr.ReferringVeterinarian)
                .Include(vr =>
                    vr.SubmittedByUser)
                .Include(vr =>
                    vr.ReviewedByUser)
                .Include(vr =>
                    vr.Reception)
                .ToListAsync();

        var dictionary =
            requests.ToDictionary(
                vr => vr.Id);

        return orderedIds
            .Where(dictionary.ContainsKey)
            .Select(id =>
                MapRequest(dictionary[id]))
            .ToList();
    }

    private static VeterinaryRequestDto MapRequest(
        VeterinaryRequest request)
    {
        return new VeterinaryRequestDto
        {
            Id = request.Id,

            VeterinaryClinicId =
                request.VeterinaryClinicId,

            VeterinaryClinicName =
                request.VeterinaryClinic?.Name,

            ReferringVeterinarianId =
                request.ReferringVeterinarianId,

            ReferringVeterinarianName =
                request.ReferringVeterinarian == null
                    ? null
                    : BuildPersonName(
                        request.ReferringVeterinarian
                            .FirstName,
                        request.ReferringVeterinarian
                            .LastName,
                        request.ReferringVeterinarian
                            .SecondLastName),

            SubmittedByUserId =
                request.SubmittedByUserId,

            SubmittedByUserName =
                BuildPersonName(
                    request.SubmittedByUser.FirstName,
                    request.SubmittedByUser.LastName,
                    null),

            ReviewedByUserId =
                request.ReviewedByUserId,

            ReviewedByUserName =
                request.ReviewedByUser == null
                    ? null
                    : BuildPersonName(
                        request.ReviewedByUser.FirstName,
                        request.ReviewedByUser.LastName,
                        null),

            ReceptionId =
                request.ReceptionId,

            ReceptionQrCode =
                request.Reception?.QrCode,

            Status = request.Status,

            OwnerFirstName =
                request.OwnerFirstName,

            OwnerLastName =
                request.OwnerLastName,

            OwnerSecondLastName =
                request.OwnerSecondLastName,

            OwnerPhone =
                request.OwnerPhone,

            OwnerEmail =
                request.OwnerEmail,

            PetName =
                request.PetName,

            Species =
                request.Species,

            Breed =
                request.Breed,

            Sex =
                request.Sex,

            Color =
                request.Color,

            ApproximateWeightKg =
                request.ApproximateWeightKg,

            AgeYears =
                request.AgeYears,

            DateOfDeath =
                request.DateOfDeath,

            RequestedCremationType =
                request.RequestedCremationType,

            RequestedPackageName =
                request.RequestedPackageName,

            RequestNotes =
                request.RequestNotes,

            InternalNotes =
                request.InternalNotes,

            RejectionReason =
                request.RejectionReason,

            SubmittedAt =
                request.SubmittedAt,

            ReviewedAt =
                request.ReviewedAt,

            ConvertedAt =
                request.ConvertedAt,

            CreatedAt =
                request.CreatedAt
        };
    }

    private static string BuildPersonName(
        string firstName,
        string lastName,
        string? secondLastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                lastName,
                secondLastName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value =>
                value!.Trim()));
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeEmail(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static DateTime NormalizeToUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }
}