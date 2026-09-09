using Microsoft.EntityFrameworkCore;
using pcms.Application.Common;
using pcms.Application.Veterinarians.DTOs;
using pcms.Application.Veterinarians.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class VeterinarianService : IVeterinarianService
{
    private readonly AppDbContext _context;

    public VeterinarianService(AppDbContext context)
    {
        _context = context;
    }

    private static VeterinarianDto MapToDto(
        Veterinarian veterinarian,
        string? clinicName)
    {
        return new VeterinarianDto
        {
            Id = veterinarian.Id,
            VeterinaryClinicId =
                veterinarian.VeterinaryClinicId,
            VeterinaryClinicName = clinicName,
            FirstName = veterinarian.FirstName,
            LastName = veterinarian.LastName,
            SecondLastName =
                veterinarian.SecondLastName,
            Phone = veterinarian.Phone,
            Email = veterinarian.Email,
            ProfessionalLicenseNumber =
                veterinarian.ProfessionalLicenseNumber,
            IsActive = veterinarian.IsActive,
            CreatedAt = veterinarian.CreatedAt
        };
    }

    public async Task<VeterinarianDto> CreateAsync(
        CreateVeterinarianDto dto)
    {
        var clinicId =
            dto.VeterinaryClinicId is { } selectedClinicId &&
            selectedClinicId != Guid.Empty
                ? selectedClinicId
                : (Guid?)null;

        VeterinaryClinic? clinic = null;

        if (clinicId.HasValue)
        {
            clinic = await _context.VeterinaryClinics
                .FirstOrDefaultAsync(c =>
                    c.Id == clinicId.Value &&
                    c.IsActive);

            if (clinic == null)
            {
                throw new InvalidOperationException(
                    "Active veterinary clinic not found.");
            }
        }

        var firstName =
            TextInputNormalization.Required(dto.FirstName);
        var lastName =
            TextInputNormalization.Required(dto.LastName);
        var secondLastName =
            TextInputNormalization.Optional(dto.SecondLastName);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "Veterinarian first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "Veterinarian last name is required.");
        }

        var phone = TextInputNormalization.Optional(dto.Phone);

        var email = TextInputNormalization.Optional(dto.Email)?
            .ToLowerInvariant();

        var professionalLicenseNumber =
            TextInputNormalization.Optional(
                dto.ProfessionalLicenseNumber);

        var duplicateExists = await ActiveNameExistsAsync(
            clinicId,
            firstName,
            lastName,
            secondLastName);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "An active veterinarian with this name already exists for the selected clinic assignment.");
        }

        if (professionalLicenseNumber != null)
        {
            var licenseExists = await LicenseExistsAsync(
                professionalLicenseNumber);

            if (licenseExists)
            {
                throw new InvalidOperationException(
                    "A veterinarian with this professional license number already exists.");
            }
        }

        var veterinarian = new Veterinarian
        {
            Id = Guid.NewGuid(),
            VeterinaryClinicId = clinicId,
            FirstName = firstName,
            LastName = lastName,
            SecondLastName = secondLastName,
            Phone = phone,
            Email = email,
            ProfessionalLicenseNumber =
                professionalLicenseNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Veterinarians.Add(veterinarian);

        await _context.SaveChangesAsync();

        return MapToDto(
            veterinarian,
            clinic?.Name);
    }

    public async Task<PagedVeterinariansDto> GetAllAsync(
        int page,
        int pageSize,
        bool isActive,
        Guid? veterinaryClinicId)
    {
        var query = _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.IsActive == isActive &&
                (
                    !isActive ||
                    v.VeterinaryClinicId == null ||
                    v.VeterinaryClinic!.IsActive
                ) &&
                (!veterinaryClinicId.HasValue ||
                    v.VeterinaryClinicId == veterinaryClinicId.Value));

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(v => v.LastName)
            .ThenBy(v => v.SecondLastName)
            .ThenBy(v => v.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VeterinarianDto
            {
                Id = v.Id,
                VeterinaryClinicId =
                    v.VeterinaryClinicId,
                VeterinaryClinicName =
                    v.VeterinaryClinic == null
                        ? null
                        : v.VeterinaryClinic.Name,
                FirstName = v.FirstName,
                LastName = v.LastName,
                SecondLastName =
                    v.SecondLastName,
                Phone = v.Phone,
                Email = v.Email,
                ProfessionalLicenseNumber =
                    v.ProfessionalLicenseNumber,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return new PagedVeterinariansDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize)
        };
    }

    public async Task<VeterinarianDto?> GetByIdAsync(
        Guid id)
    {
        return await _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.Id == id &&
                v.IsActive &&
                (
                    v.VeterinaryClinicId == null ||
                    v.VeterinaryClinic!.IsActive
                ))
            .Select(v => new VeterinarianDto
            {
                Id = v.Id,
                VeterinaryClinicId =
                    v.VeterinaryClinicId,
                VeterinaryClinicName =
                    v.VeterinaryClinic == null
                        ? null
                        : v.VeterinaryClinic.Name,
                FirstName = v.FirstName,
                LastName = v.LastName,
                SecondLastName =
                    v.SecondLastName,
                Phone = v.Phone,
                Email = v.Email,
                ProfessionalLicenseNumber =
                    v.ProfessionalLicenseNumber,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<VeterinarianDto>>
        GetByClinicIdAsync(
            Guid veterinaryClinicId,
            bool? isActive)
    {
        var clinicExists =
            await _context.VeterinaryClinics
                .AsNoTracking()
                .AnyAsync(c =>
                    c.Id == veterinaryClinicId);

        if (!clinicExists)
        {
            return Array.Empty<VeterinarianDto>();
        }

        var query = _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.VeterinaryClinicId ==
                    veterinaryClinicId);

        if (isActive.HasValue)
        {
            query = query.Where(v =>
                v.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.LastName)
            .ThenBy(v => v.SecondLastName)
            .ThenBy(v => v.FirstName)
            .Select(v => new VeterinarianDto
            {
                Id = v.Id,
                VeterinaryClinicId =
                    v.VeterinaryClinicId,
                VeterinaryClinicName =
                    v.VeterinaryClinic == null
                        ? null
                        : v.VeterinaryClinic.Name,
                FirstName = v.FirstName,
                LastName = v.LastName,
                SecondLastName =
                    v.SecondLastName,
                Phone = v.Phone,
                Email = v.Email,
                ProfessionalLicenseNumber =
                    v.ProfessionalLicenseNumber,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<VeterinarianDto?> UpdateAsync(
        Guid id,
        UpdateVeterinarianDto dto)
    {
        var veterinarian =
            await _context.Veterinarians
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    v.IsActive);

        if (veterinarian == null)
        {
            return null;
        }

        var clinicId =
            dto.VeterinaryClinicId is { } selectedClinicId &&
            selectedClinicId != Guid.Empty
                ? selectedClinicId
                : (Guid?)null;

        VeterinaryClinic? clinic = null;

        if (clinicId.HasValue)
        {
            clinic = await _context.VeterinaryClinics
                .FirstOrDefaultAsync(c =>
                    c.Id == clinicId.Value &&
                    c.IsActive);

            if (clinic == null)
            {
                throw new InvalidOperationException(
                    "Active veterinary clinic not found.");
            }
        }

        var firstName =
            TextInputNormalization.Required(dto.FirstName);
        var lastName =
            TextInputNormalization.Required(dto.LastName);
        var secondLastName =
            TextInputNormalization.Optional(dto.SecondLastName);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "Veterinarian first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "Veterinarian last name is required.");
        }

        var phone = TextInputNormalization.Optional(dto.Phone);

        var email = TextInputNormalization.Optional(dto.Email)?
            .ToLowerInvariant();

        var professionalLicenseNumber =
            TextInputNormalization.Optional(
                dto.ProfessionalLicenseNumber);

        var duplicateExists = await ActiveNameExistsAsync(
            clinicId,
            firstName,
            lastName,
            secondLastName,
            id);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Another active veterinarian with this name already exists for the selected clinic assignment.");
        }

        if (professionalLicenseNumber != null)
        {
            var licenseExists = await LicenseExistsAsync(
                professionalLicenseNumber,
                id);

            if (licenseExists)
            {
                throw new InvalidOperationException(
                    "Another veterinarian with this professional license number already exists.");
            }
        }

        veterinarian.VeterinaryClinicId = clinicId;
        veterinarian.FirstName = firstName;
        veterinarian.LastName = lastName;
        veterinarian.SecondLastName =
            secondLastName;
        veterinarian.Phone = phone;
        veterinarian.Email = email;
        veterinarian.ProfessionalLicenseNumber =
            professionalLicenseNumber;

        await _context.SaveChangesAsync();

        return MapToDto(
            veterinarian,
            clinic?.Name);
    }

    public async Task<bool> DeactivateAsync(
        Guid id)
    {
        var veterinarian =
            await _context.Veterinarians
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    v.IsActive);

        if (veterinarian == null)
        {
            return false;
        }

        veterinarian.IsActive = false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(
        Guid id)
    {
        var veterinarian =
            await _context.Veterinarians
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    !v.IsActive);

        if (veterinarian == null)
        {
            return false;
        }

        if (veterinarian.VeterinaryClinicId.HasValue)
        {
            var clinicIsActive =
                await _context.VeterinaryClinics
                    .AnyAsync(c =>
                        c.Id ==
                            veterinarian
                                .VeterinaryClinicId.Value &&
                        c.IsActive);

            if (!clinicIsActive)
            {
                throw new InvalidOperationException(
                    "The assigned veterinary clinic is inactive or no longer exists.");
            }
        }

        var firstName =
            TextInputNormalization.Required(veterinarian.FirstName);
        var lastName =
            TextInputNormalization.Required(veterinarian.LastName);
        var secondLastName =
            TextInputNormalization.Optional(veterinarian.SecondLastName);

        if (await ActiveNameExistsAsync(
            veterinarian.VeterinaryClinicId,
            firstName,
            lastName,
            secondLastName,
            veterinarian.Id))
        {
            throw new InvalidOperationException(
                "An active veterinarian with this name already exists for the selected clinic assignment.");
        }

        var professionalLicenseNumber =
            TextInputNormalization.Optional(
                veterinarian.ProfessionalLicenseNumber);

        if (professionalLicenseNumber != null &&
            await LicenseExistsAsync(
                professionalLicenseNumber,
                veterinarian.Id))
        {
            throw new InvalidOperationException(
                "A veterinarian with this professional license number already exists.");
        }

        veterinarian.IsActive = true;

        await _context.SaveChangesAsync();

        return true;
    }

    private Task<bool> ActiveNameExistsAsync(
        Guid? clinicId,
        string firstName,
        string lastName,
        string? secondLastName,
        Guid? excludedId = null)
    {
        var normalizedFirstName = firstName.ToLowerInvariant();
        var normalizedLastName = lastName.ToLowerInvariant();
        var normalizedSecondLastName =
            secondLastName?.ToLowerInvariant() ?? string.Empty;

        return _context.Veterinarians.AnyAsync(veterinarian =>
            veterinarian.IsActive &&
            (!excludedId.HasValue ||
                veterinarian.Id != excludedId.Value) &&
            veterinarian.VeterinaryClinicId == clinicId &&
            veterinarian.FirstName.ToLower() == normalizedFirstName &&
            veterinarian.LastName.ToLower() == normalizedLastName &&
            (veterinarian.SecondLastName ?? string.Empty).ToLower() ==
                normalizedSecondLastName);
    }

    private Task<bool> LicenseExistsAsync(
        string professionalLicenseNumber,
        Guid? excludedId = null)
    {
        var normalizedLicense =
            professionalLicenseNumber.ToLowerInvariant();

        return _context.Veterinarians.AnyAsync(veterinarian =>
            (!excludedId.HasValue ||
                veterinarian.Id != excludedId.Value) &&
            veterinarian.ProfessionalLicenseNumber != null &&
            veterinarian.ProfessionalLicenseNumber.ToLower() ==
                normalizedLicense);
    }

    public async Task<PagedVeterinariansDto>
        SearchAsync(
            string search,
            bool isActive,
            Guid? veterinaryClinicId,
            int page,
            int pageSize)
    {
        var normalizedSearch =
            search.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedSearch))
        {
            return new PagedVeterinariansDto
            {
                Page = page,
                PageSize = pageSize
            };
        }

        var pattern = $"%{normalizedSearch}%";

        var query = _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.IsActive == isActive &&
                (
                    !isActive ||
                    v.VeterinaryClinicId == null ||
                    v.VeterinaryClinic!.IsActive
                ) &&
                (!veterinaryClinicId.HasValue ||
                    v.VeterinaryClinicId == veterinaryClinicId.Value) &&
                (
                    EF.Functions.ILike(v.FirstName, pattern) ||

                    EF.Functions.ILike(v.LastName, pattern) ||

                    (
                        v.SecondLastName != null &&
                        EF.Functions.ILike(v.SecondLastName, pattern)
                    ) ||

                    (
                        v.VeterinaryClinic != null &&
                        EF.Functions.ILike(v.VeterinaryClinic.Name, pattern)
                    ) ||

                    (
                        v.Phone != null &&
                        EF.Functions.ILike(v.Phone, pattern)
                    ) ||

                    (
                        v.Email != null &&
                        EF.Functions.ILike(v.Email, pattern)
                    ) ||

                    (
                        v.ProfessionalLicenseNumber != null &&
                        EF.Functions.ILike(
                            v.ProfessionalLicenseNumber,
                            pattern)
                    )
                ));

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(v => v.LastName)
            .ThenBy(v => v.SecondLastName)
            .ThenBy(v => v.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VeterinarianDto
            {
                Id = v.Id,
                VeterinaryClinicId =
                    v.VeterinaryClinicId,
                VeterinaryClinicName =
                    v.VeterinaryClinic == null
                        ? null
                        : v.VeterinaryClinic.Name,
                FirstName = v.FirstName,
                LastName = v.LastName,
                SecondLastName =
                    v.SecondLastName,
                Phone = v.Phone,
                Email = v.Email,
                ProfessionalLicenseNumber =
                    v.ProfessionalLicenseNumber,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return new PagedVeterinariansDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };
    }

    public async Task<PaginatedResult<VeterinarianClinicOptionDto>>
        GetClinicOptionsAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.VeterinaryClinics
            .AsNoTracking()
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(clinic =>
                clinic.IsActive == isActive.Value);
        }

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
            .Select(clinic => new VeterinarianClinicOptionDto
            {
                Id = clinic.Id,
                DisplayName = clinic.Name,
                IsActive = clinic.IsActive
            })
            .ToListAsync();

        return new PaginatedResult<VeterinarianClinicOptionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };
    }
}
