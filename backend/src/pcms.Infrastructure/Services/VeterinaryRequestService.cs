using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Common;
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

        var ownerInput =
            CustomerPetInputRules.NormalizeVeterinaryRequestOwner(
                dto.OwnerFirstName,
                dto.OwnerLastName,
                dto.OwnerSecondLastName,
                dto.OwnerPhone,
                dto.OwnerEmail);

        var petInput = CustomerPetInputRules.NormalizePet(
            dto.PetName,
            dto.Species,
            dto.Breed,
            dto.Sex,
            dto.Color);

        ValidateRequestData(
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

            VeterinaryClinicNameSnapshot =
                referral.Clinic?.Name,

            ReferringVeterinarianNameSnapshot =
                referral.Veterinarian is null
                    ? null
                    : BuildPersonName(
                        referral.Veterinarian.FirstName,
                        referral.Veterinarian.LastName,
                        referral.Veterinarian.SecondLastName),

            SubmittedByUserId =
                submittedByUser.Id,

            Status =
                VeterinaryRequestStatus.Submitted,

            OwnerFirstName =
                ownerInput.FirstName,

            OwnerLastName =
                ownerInput.LastName,

            OwnerSecondLastName =
                ownerInput.SecondLastName,

            OwnerPhone =
                ownerInput.Phone,

            OwnerEmail =
                ownerInput.Email,

            PetName =
                petInput.Name,

            Species =
                petInput.Species,

            Breed =
                petInput.Breed,

            Sex =
                petInput.Sex,

            Color =
                petInput.Color,

            ApproximateWeightKg =
                dto.ApproximateWeightKg,

            AgeYears =
                dto.AgeYears,

            DateOfDeath =
                dto.DateOfDeath,

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

        var items = await ProjectListItems(query)
            .OrderByDescending(vr =>
                vr.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

        var ownerInput =
            CustomerPetInputRules.NormalizeVeterinaryRequestOwner(
                dto.OwnerFirstName,
                dto.OwnerLastName,
                dto.OwnerSecondLastName,
                dto.OwnerPhone,
                dto.OwnerEmail);

        var petInput = CustomerPetInputRules.NormalizePet(
            dto.PetName,
            dto.Species,
            dto.Breed,
            dto.Sex,
            dto.Color);

        ValidateRequestData(
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

        request.VeterinaryClinicNameSnapshot =
            referral.Clinic?.Name;

        request.ReferringVeterinarianNameSnapshot =
            referral.Veterinarian is null
                ? null
                : BuildPersonName(
                    referral.Veterinarian.FirstName,
                    referral.Veterinarian.LastName,
                    referral.Veterinarian.SecondLastName);

        request.OwnerFirstName =
            ownerInput.FirstName;

        request.OwnerLastName =
            ownerInput.LastName;

        request.OwnerSecondLastName =
            ownerInput.SecondLastName;

        request.OwnerPhone =
            ownerInput.Phone;

        request.OwnerEmail =
            ownerInput.Email;

        request.PetName =
            petInput.Name;

        request.Species =
            petInput.Species;

        request.Breed =
            petInput.Breed;

        request.Sex =
            petInput.Sex;

        request.Color =
            petInput.Color;

        request.ApproximateWeightKg =
            dto.ApproximateWeightKg;

        request.AgeYears =
            dto.AgeYears;

        request.DateOfDeath =
            dto.DateOfDeath;

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

        await ValidateHistoricalReferralSourceAsync(request);

        Customer customer;
        Pet pet;

        if (dto.ExistingPetId.HasValue)
        {
            pet = CustomerPetWorkflowRules.RequireEligiblePet(
                await _context.Pets
                .Include(p => p.Customer)
                .Include(p => p.Reception)
                .FirstOrDefaultAsync(p =>
                    p.Id ==
                        dto.ExistingPetId.Value));

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
                var customerInput =
                    CustomerPetInputRules.NormalizeCustomer(
                        request.OwnerFirstName,
                        request.OwnerLastName,
                        request.OwnerSecondLastName,
                        request.OwnerPhone,
                        request.OwnerEmail);

                customer = new Customer
                {
                    Id = Guid.NewGuid(),

                    FirstName =
                        customerInput.FirstName,

                    LastName =
                        customerInput.LastName,

                    SecondLastName =
                        customerInput.SecondLastName,

                    Phone =
                        customerInput.Phone,

                    Email = customerInput.Email,

                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
            }

            var petInput = CustomerPetInputRules.NormalizePet(
                request.PetName,
                request.Species,
                request.Breed,
                request.Sex,
                request.Color);

            pet = new Pet
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,

                Name =
                    petInput.Name,

                Species =
                    petInput.Species,

                Breed =
                    petInput.Breed,

                Sex =
                    petInput.Sex,

                Color =
                    petInput.Color,

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
        var identitySnapshot =
            CustomerPetWorkflowRules.CaptureReceptionIdentity(pet, customer);

        var reception = new Reception
        {
            Id = Guid.NewGuid(),

            PetId = pet.Id,

            PetNameSnapshot = identitySnapshot.PetName,

            CustomerNameSnapshot = identitySnapshot.CustomerName,

            ReceivedByUserId =
                receivedByUser.Id,

            VeterinaryClinicId =
                request.VeterinaryClinicId,

            ReferringVeterinarianId =
                request.ReferringVeterinarianId,

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

    public async Task<PagedVeterinaryRequestsDto>
        SearchAsync(
            string search,
            VeterinaryRequestStatus? status,
            int page,
            int pageSize)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new PagedVeterinaryRequestsDto
            {
                Page = page,
                PageSize = pageSize
            };
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
                        vr.VeterinaryClinicNameSnapshot != null &&
                        EF.Functions.ILike(
                            vr.VeterinaryClinicNameSnapshot,
                            pattern)
                    ) ||

                    (
                        vr.ReferringVeterinarianNameSnapshot != null &&
                        EF.Functions.ILike(
                            vr.ReferringVeterinarianNameSnapshot,
                            pattern)
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

        var totalItems = await query.CountAsync();

        var items = await ProjectListItems(query)
            .OrderByDescending(vr =>
                vr.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedVeterinaryRequestsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };
    }

    public async Task<
        PaginatedResult<VeterinaryRequestClinicOptionDto>>
        GetClinicOptionsAsync(
            string? search,
            int page,
            int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.VeterinaryClinics
            .AsNoTracking()
            .Where(clinic => clinic.IsActive);

        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(clinic =>
                EF.Functions.ILike(clinic.Name, pattern));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(clinic => clinic.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(clinic => new VeterinaryRequestClinicOptionDto
            {
                Id = clinic.Id,
                DisplayName = clinic.Name
            })
            .ToListAsync();

        return CreateLookupResult(
            items,
            page,
            pageSize,
            totalItems);
    }

    public async Task<
        PaginatedResult<VeterinaryRequestVeterinarianOptionDto>>
        GetVeterinarianOptionsAsync(
            Guid? veterinaryClinicId,
            string? search,
            int page,
            int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Veterinarians
            .AsNoTracking()
            .Where(veterinarian =>
                veterinarian.IsActive &&
                (veterinaryClinicId.HasValue
                    ? veterinarian.VeterinaryClinicId ==
                        veterinaryClinicId.Value &&
                      veterinarian.VeterinaryClinic!.IsActive
                    : veterinarian.VeterinaryClinicId == null));

        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(veterinarian =>
                EF.Functions.ILike(veterinarian.FirstName, pattern) ||
                EF.Functions.ILike(veterinarian.LastName, pattern) ||
                (veterinarian.SecondLastName != null &&
                    EF.Functions.ILike(
                        veterinarian.SecondLastName,
                        pattern)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(veterinarian => veterinarian.LastName)
            .ThenBy(veterinarian => veterinarian.SecondLastName)
            .ThenBy(veterinarian => veterinarian.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(veterinarian =>
                new VeterinaryRequestVeterinarianOptionDto
                {
                    Id = veterinarian.Id,
                    DisplayName =
                        veterinarian.FirstName + " " +
                        veterinarian.LastName +
                        (veterinarian.SecondLastName == null
                            ? string.Empty
                            : " " + veterinarian.SecondLastName),
                    VeterinaryClinicId =
                        veterinarian.VeterinaryClinicId
                })
            .ToListAsync();

        return CreateLookupResult(
            items,
            page,
            pageSize,
            totalItems);
    }

    public async Task<
        PaginatedResult<VeterinaryRequestCustomerOptionDto>>
        GetCustomerOptionsAsync(
            string? search,
            int page,
            int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Customers
            .AsNoTracking()
            .Where(customer => customer.IsActive);

        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(customer =>
                EF.Functions.ILike(customer.FirstName, pattern) ||
                EF.Functions.ILike(customer.LastName, pattern) ||
                (customer.SecondLastName != null &&
                    EF.Functions.ILike(
                        customer.SecondLastName,
                        pattern)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.SecondLastName)
            .ThenBy(customer => customer.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(customer => new VeterinaryRequestCustomerOptionDto
            {
                Id = customer.Id,
                DisplayName =
                    customer.FirstName + " " +
                    customer.LastName +
                    (customer.SecondLastName == null
                        ? string.Empty
                        : " " + customer.SecondLastName)
            })
            .ToListAsync();

        return CreateLookupResult(
            items,
            page,
            pageSize,
            totalItems);
    }

    public async Task<
        PaginatedResult<VeterinaryRequestPetOptionDto>>
        GetPetOptionsAsync(
            string? search,
            int page,
            int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Pets
            .AsNoTracking()
            .Where(pet =>
                pet.IsActive &&
                pet.Customer.IsActive &&
                pet.Reception == null);

        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(pet =>
                EF.Functions.ILike(pet.Name, pattern) ||
                EF.Functions.ILike(pet.Customer.FirstName, pattern) ||
                EF.Functions.ILike(pet.Customer.LastName, pattern) ||
                (pet.Customer.SecondLastName != null &&
                    EF.Functions.ILike(
                        pet.Customer.SecondLastName,
                        pattern)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(pet => pet.Name)
            .ThenBy(pet => pet.Customer.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pet => new VeterinaryRequestPetOptionDto
            {
                Id = pet.Id,
                CustomerId = pet.CustomerId,
                DisplayName =
                    pet.Name + " · " +
                    pet.Customer.FirstName + " " +
                    pet.Customer.LastName +
                    (pet.Customer.SecondLastName == null
                        ? string.Empty
                        : " " + pet.Customer.SecondLastName)
            })
            .ToListAsync();

        return CreateLookupResult(
            items,
            page,
            pageSize,
            totalItems);
    }

    private static PaginatedResult<T> CreateLookupResult<T>(
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalItems) =>
        new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };

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
        }

        ValidateReferralRelationship(
            clinic,
            veterinarian);

        return (clinic, veterinarian);
    }

    private async Task ValidateHistoricalReferralSourceAsync(
        VeterinaryRequest request)
    {
        if (!request.VeterinaryClinicId.HasValue &&
            !request.ReferringVeterinarianId.HasValue)
        {
            throw new InvalidOperationException(
                "La solicitud aprobada no conserva una fuente veterinaria válida.");
        }

        VeterinaryClinic? clinic = null;

        if (request.VeterinaryClinicId.HasValue)
        {
            clinic = await _context.VeterinaryClinics
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate =>
                    candidate.Id == request.VeterinaryClinicId.Value);

            if (clinic is null ||
                string.IsNullOrWhiteSpace(
                    request.VeterinaryClinicNameSnapshot))
            {
                throw new InvalidOperationException(
                    "La identidad histórica de la veterinaria está incompleta o dañada.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(
                     request.VeterinaryClinicNameSnapshot))
        {
            throw new InvalidOperationException(
                "La identidad histórica de la veterinaria no coincide con la solicitud aprobada.");
        }

        Veterinarian? veterinarian = null;

        if (request.ReferringVeterinarianId.HasValue)
        {
            veterinarian = await _context.Veterinarians
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate =>
                    candidate.Id ==
                    request.ReferringVeterinarianId.Value);

            if (veterinarian is null ||
                string.IsNullOrWhiteSpace(
                    request.ReferringVeterinarianNameSnapshot))
            {
                throw new InvalidOperationException(
                    "La identidad histórica del veterinario está incompleta o dañada.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(
                     request.ReferringVeterinarianNameSnapshot))
        {
            throw new InvalidOperationException(
                "La identidad histórica del veterinario no coincide con la solicitud aprobada.");
        }

        ValidateReferralRelationship(
            clinic,
            veterinarian);
    }

    private static void ValidateReferralRelationship(
        VeterinaryClinic? clinic,
        Veterinarian? veterinarian)
    {
        if (veterinarian?.VeterinaryClinicId.HasValue == true)
        {
            if (clinic is null)
            {
                throw new InvalidOperationException(
                    "El veterinario seleccionado pertenece a una veterinaria; seleccione también la veterinaria asociada.");
            }

            if (veterinarian.VeterinaryClinicId.Value != clinic.Id)
            {
                throw new InvalidOperationException(
                    "El veterinario referente no pertenece a la veterinaria seleccionada.");
            }
        }
        else if (veterinarian is not null && clinic is not null)
        {
            throw new InvalidOperationException(
                "El veterinario seleccionado es independiente y no pertenece a la veterinaria seleccionada.");
        }
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
        decimal approximateWeightKg,
        DateOnly dateOfDeath,
        CremationType? requestedCremationType)
    {
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

        if (dateOfDeath >
            CustomerPetWorkflowRules.CurrentBusinessDate())
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

    private static IQueryable<VeterinaryRequestListItemDto>
        ProjectListItems(
            IQueryable<VeterinaryRequest> query)
    {
        return query.Select(request =>
            new VeterinaryRequestListItemDto
            {
                Id = request.Id,
                VeterinaryClinicName =
                    request.VeterinaryClinicNameSnapshot,
                ReferringVeterinarianName =
                    request.ReferringVeterinarianNameSnapshot,
                Status = request.Status,
                OwnerFirstName = request.OwnerFirstName,
                OwnerLastName = request.OwnerLastName,
                OwnerSecondLastName = request.OwnerSecondLastName,
                OwnerPhone = request.OwnerPhone,
                PetName = request.PetName,
                Species = request.Species,
                Breed = request.Breed,
                ApproximateWeightKg = request.ApproximateWeightKg,
                SubmittedAt = request.SubmittedAt
            });
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
                request.VeterinaryClinicNameSnapshot,

            ReferringVeterinarianId =
                request.ReferringVeterinarianId,

            ReferringVeterinarianName =
                request.ReferringVeterinarianNameSnapshot,

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

}
