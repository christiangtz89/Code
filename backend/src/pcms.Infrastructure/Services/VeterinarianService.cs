using Microsoft.EntityFrameworkCore;
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

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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

        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var secondLastName =
            NormalizeOptional(dto.SecondLastName);

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

        var phone = NormalizeOptional(dto.Phone);

        var email = NormalizeOptional(dto.Email)?
            .ToLowerInvariant();

        var professionalLicenseNumber =
            NormalizeOptional(
                dto.ProfessionalLicenseNumber);

        var normalizedSecondLastName =
            secondLastName?.ToLowerInvariant() ?? string.Empty;

        var duplicateExists =
            await _context.Veterinarians.AnyAsync(v =>
                v.IsActive &&
                v.VeterinaryClinicId == clinicId &&
                v.FirstName.ToLower() ==
                    firstName.ToLower() &&
                v.LastName.ToLower() ==
                    lastName.ToLower() &&
                (v.SecondLastName ?? string.Empty)
                    .ToLower() ==
                    normalizedSecondLastName);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "An active veterinarian with this name already exists for the selected clinic assignment.");
        }

        if (professionalLicenseNumber != null)
        {
            var normalizedLicense =
                professionalLicenseNumber.ToLower();

            var licenseExists =
                await _context.Veterinarians.AnyAsync(v =>
                    v.ProfessionalLicenseNumber != null &&
                    v.ProfessionalLicenseNumber
                        .ToLower() == normalizedLicense);

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
        bool isActive)
    {
        var query = _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.IsActive == isActive &&
                (
                    !isActive ||
                    v.VeterinaryClinicId == null ||
                    v.VeterinaryClinic!.IsActive
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

        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var secondLastName =
            NormalizeOptional(dto.SecondLastName);

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

        var phone = NormalizeOptional(dto.Phone);

        var email = NormalizeOptional(dto.Email)?
            .ToLowerInvariant();

        var professionalLicenseNumber =
            NormalizeOptional(
                dto.ProfessionalLicenseNumber);

        var normalizedSecondLastName =
            secondLastName?.ToLowerInvariant() ?? string.Empty;

        var duplicateExists =
            await _context.Veterinarians.AnyAsync(v =>
                v.Id != id &&
                v.IsActive &&
                v.VeterinaryClinicId == clinicId &&
                v.FirstName.ToLower() ==
                    firstName.ToLower() &&
                v.LastName.ToLower() ==
                    lastName.ToLower() &&
                (v.SecondLastName ?? string.Empty)
                    .ToLower() ==
                    normalizedSecondLastName);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Another active veterinarian with this name already exists for the selected clinic assignment.");
        }

        if (professionalLicenseNumber != null)
        {
            var normalizedLicense =
                professionalLicenseNumber.ToLower();

            var licenseExists =
                await _context.Veterinarians.AnyAsync(v =>
                    v.Id != id &&
                    v.ProfessionalLicenseNumber != null &&
                    v.ProfessionalLicenseNumber
                        .ToLower() == normalizedLicense);

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
                return false;
            }
        }

        veterinarian.IsActive = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<VeterinarianDto>>
        SearchAsync(
            string search,
            bool isActive)
    {
        var normalizedSearch =
            search.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(
            normalizedSearch))
        {
            return Array.Empty<VeterinarianDto>();
        }

        return await _context.Veterinarians
            .AsNoTracking()
            .Where(v =>
                v.IsActive == isActive &&
                (
                    !isActive ||
                    v.VeterinaryClinicId == null ||
                    v.VeterinaryClinic!.IsActive
                ) &&
                (
                    v.FirstName
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    v.LastName
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    (
                        v.SecondLastName != null &&
                        v.SecondLastName
                            .ToLower()
                            .Contains(normalizedSearch)
                    ) ||

                    (
                        v.VeterinaryClinic != null &&
                        v.VeterinaryClinic.Name
                            .ToLower()
                            .Contains(normalizedSearch)
                    ) ||

                    (
                        v.Phone != null &&
                        v.Phone
                            .ToLower()
                            .Contains(normalizedSearch)
                    ) ||

                    (
                        v.Email != null &&
                        v.Email
                            .ToLower()
                            .Contains(normalizedSearch)
                    ) ||

                    (
                        v.ProfessionalLicenseNumber != null &&
                        v.ProfessionalLicenseNumber
                            .ToLower()
                            .Contains(normalizedSearch)
                    )
                ))
            .OrderBy(v => v.LastName)
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
}