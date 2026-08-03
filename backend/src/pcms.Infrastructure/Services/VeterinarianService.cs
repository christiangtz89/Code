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

    public async Task<VeterinarianDto> CreateAsync(
    CreateVeterinarianDto dto)
{
    var clinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(c =>
            c.Id == dto.VeterinaryClinicId &&
            c.IsActive);

    if (clinic == null)
    {
        throw new InvalidOperationException(
            "Active veterinary clinic not found.");
    }

    var firstName = dto.FirstName.Trim();
    var lastName = dto.LastName.Trim();

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

    var email = string.IsNullOrWhiteSpace(dto.Email)
        ? null
        : dto.Email.Trim().ToLower();

    var professionalLicenseNumber =
        string.IsNullOrWhiteSpace(dto.ProfessionalLicenseNumber)
            ? null
            : dto.ProfessionalLicenseNumber.Trim();

    var duplicateExists = await _context.Veterinarians
        .AnyAsync(v =>
            v.IsActive &&
            v.VeterinaryClinicId == dto.VeterinaryClinicId &&
            v.FirstName.ToLower() == firstName.ToLower() &&
            v.LastName.ToLower() == lastName.ToLower());

    if (duplicateExists)
    {
        throw new InvalidOperationException(
            "An active veterinarian with this name already exists in the selected clinic.");
    }

    if (professionalLicenseNumber != null)
    {
        var licenseExists = await _context.Veterinarians
            .AnyAsync(v =>
                v.ProfessionalLicenseNumber != null &&
                v.ProfessionalLicenseNumber.ToLower() ==
                    professionalLicenseNumber.ToLower());

        if (licenseExists)
        {
            throw new InvalidOperationException(
                "A veterinarian with this professional license number already exists.");
        }
    }

    var veterinarian = new Veterinarian
    {
        Id = Guid.NewGuid(),
        VeterinaryClinicId = clinic.Id,
        FirstName = firstName,
        LastName = lastName,
        Phone = string.IsNullOrWhiteSpace(dto.Phone)
            ? null
            : dto.Phone.Trim(),
        Email = email,
        ProfessionalLicenseNumber = professionalLicenseNumber,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    _context.Veterinarians.Add(veterinarian);

    await _context.SaveChangesAsync();

    return new VeterinarianDto
    {
        Id = veterinarian.Id,
        VeterinaryClinicId = veterinarian.VeterinaryClinicId,
        VeterinaryClinicName = clinic.Name,
        FirstName = veterinarian.FirstName,
        LastName = veterinarian.LastName,
        Phone = veterinarian.Phone,
        Email = veterinarian.Email,
        ProfessionalLicenseNumber =
            veterinarian.ProfessionalLicenseNumber,
        IsActive = veterinarian.IsActive,
        CreatedAt = veterinarian.CreatedAt
    };
}

    public async Task<PagedVeterinariansDto> GetAllAsync(
    int page,
    int pageSize)
{
    var query = _context.Veterinarians
        .AsNoTracking()
        .Where(v =>
            v.IsActive &&
            v.VeterinaryClinic.IsActive);

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderBy(v => v.LastName)
        .ThenBy(v => v.FirstName)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(v => new VeterinarianDto
        {
            Id = v.Id,
            VeterinaryClinicId = v.VeterinaryClinicId,
            VeterinaryClinicName = v.VeterinaryClinic.Name,
            FirstName = v.FirstName,
            LastName = v.LastName,
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

    public async Task<VeterinarianDto?> GetByIdAsync(Guid id)
{
    return await _context.Veterinarians
        .AsNoTracking()
        .Where(v =>
            v.Id == id &&
            v.IsActive &&
            v.VeterinaryClinic.IsActive)
        .Select(v => new VeterinarianDto
        {
            Id = v.Id,
            VeterinaryClinicId = v.VeterinaryClinicId,
            VeterinaryClinicName = v.VeterinaryClinic.Name,
            FirstName = v.FirstName,
            LastName = v.LastName,
            Phone = v.Phone,
            Email = v.Email,
            ProfessionalLicenseNumber =
                v.ProfessionalLicenseNumber,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        })
        .FirstOrDefaultAsync();
}

    public async Task<IEnumerable<VeterinarianDto>> GetByClinicIdAsync(
    Guid veterinaryClinicId)
{
    var clinicExists = await _context.VeterinaryClinics
        .AsNoTracking()
        .AnyAsync(c =>
            c.Id == veterinaryClinicId &&
            c.IsActive);

    if (!clinicExists)
    {
        return Array.Empty<VeterinarianDto>();
    }

    return await _context.Veterinarians
        .AsNoTracking()
        .Where(v =>
            v.VeterinaryClinicId == veterinaryClinicId &&
            v.IsActive)
        .OrderBy(v => v.LastName)
        .ThenBy(v => v.FirstName)
        .Select(v => new VeterinarianDto
        {
            Id = v.Id,
            VeterinaryClinicId = v.VeterinaryClinicId,
            VeterinaryClinicName = v.VeterinaryClinic.Name,
            FirstName = v.FirstName,
            LastName = v.LastName,
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
    var veterinarian = await _context.Veterinarians
        .FirstOrDefaultAsync(v =>
            v.Id == id &&
            v.IsActive);

    if (veterinarian == null)
    {
        return null;
    }

    var clinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(c =>
            c.Id == dto.VeterinaryClinicId &&
            c.IsActive);

    if (clinic == null)
    {
        throw new InvalidOperationException(
            "Active veterinary clinic not found.");
    }

    var firstName = dto.FirstName.Trim();
    var lastName = dto.LastName.Trim();

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

    var phone = string.IsNullOrWhiteSpace(dto.Phone)
        ? null
        : dto.Phone.Trim();

    var email = string.IsNullOrWhiteSpace(dto.Email)
        ? null
        : dto.Email.Trim().ToLower();

    var professionalLicenseNumber =
        string.IsNullOrWhiteSpace(dto.ProfessionalLicenseNumber)
            ? null
            : dto.ProfessionalLicenseNumber.Trim();

    var duplicateExists = await _context.Veterinarians
        .AnyAsync(v =>
            v.Id != id &&
            v.IsActive &&
            v.VeterinaryClinicId == dto.VeterinaryClinicId &&
            v.FirstName.ToLower() == firstName.ToLower() &&
            v.LastName.ToLower() == lastName.ToLower());

    if (duplicateExists)
    {
        throw new InvalidOperationException(
            "Another active veterinarian with this name already exists in the selected clinic.");
    }

    if (professionalLicenseNumber != null)
    {
        var licenseExists = await _context.Veterinarians
            .AnyAsync(v =>
                v.Id != id &&
                v.ProfessionalLicenseNumber != null &&
                v.ProfessionalLicenseNumber.ToLower() ==
                    professionalLicenseNumber.ToLower());

        if (licenseExists)
        {
            throw new InvalidOperationException(
                "Another veterinarian with this professional license number already exists.");
        }
    }

    veterinarian.VeterinaryClinicId = clinic.Id;
    veterinarian.FirstName = firstName;
    veterinarian.LastName = lastName;
    veterinarian.Phone = phone;
    veterinarian.Email = email;
    veterinarian.ProfessionalLicenseNumber =
        professionalLicenseNumber;

    await _context.SaveChangesAsync();

    return new VeterinarianDto
    {
        Id = veterinarian.Id,
        VeterinaryClinicId = veterinarian.VeterinaryClinicId,
        VeterinaryClinicName = clinic.Name,
        FirstName = veterinarian.FirstName,
        LastName = veterinarian.LastName,
        Phone = veterinarian.Phone,
        Email = veterinarian.Email,
        ProfessionalLicenseNumber =
            veterinarian.ProfessionalLicenseNumber,
        IsActive = veterinarian.IsActive,
        CreatedAt = veterinarian.CreatedAt
    };
}

    public async Task<bool> DeactivateAsync(Guid id)
{
    var veterinarian = await _context.Veterinarians
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

   public async Task<bool> RestoreAsync(Guid id)
{
    var veterinarian = await _context.Veterinarians
        .FirstOrDefaultAsync(v =>
            v.Id == id &&
            !v.IsActive);

    if (veterinarian == null)
    {
        return false;
    }

    var clinicIsActive = await _context.VeterinaryClinics
        .AnyAsync(c =>
            c.Id == veterinarian.VeterinaryClinicId &&
            c.IsActive);

    if (!clinicIsActive)
    {
        return false;
    }

    veterinarian.IsActive = true;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<IEnumerable<VeterinarianDto>> SearchAsync(
    string search)
{
    var normalizedSearch = search.Trim().ToLower();

    if (string.IsNullOrWhiteSpace(normalizedSearch))
    {
        return Array.Empty<VeterinarianDto>();
    }

    return await _context.Veterinarians
        .AsNoTracking()
        .Where(v =>
            v.IsActive &&
            v.VeterinaryClinic.IsActive &&
            (
                v.FirstName.ToLower().Contains(normalizedSearch) ||
                v.LastName.ToLower().Contains(normalizedSearch) ||
                v.VeterinaryClinic.Name
                    .ToLower()
                    .Contains(normalizedSearch) ||
                (v.Phone != null &&
                    v.Phone.ToLower().Contains(normalizedSearch)) ||
                (v.Email != null &&
                    v.Email.ToLower().Contains(normalizedSearch)) ||
                (v.ProfessionalLicenseNumber != null &&
                    v.ProfessionalLicenseNumber
                        .ToLower()
                        .Contains(normalizedSearch))
            ))
        .OrderBy(v => v.LastName)
        .ThenBy(v => v.FirstName)
        .Select(v => new VeterinarianDto
        {
            Id = v.Id,
            VeterinaryClinicId = v.VeterinaryClinicId,
            VeterinaryClinicName = v.VeterinaryClinic.Name,
            FirstName = v.FirstName,
            LastName = v.LastName,
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