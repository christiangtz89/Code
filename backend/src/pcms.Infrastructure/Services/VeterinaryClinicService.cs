using Microsoft.EntityFrameworkCore;
using pcms.Application.VeterinaryClinics.DTOs;
using pcms.Application.VeterinaryClinics.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class VeterinaryClinicService
    : IVeterinaryClinicService
{
    private readonly AppDbContext _context;

    public VeterinaryClinicService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VeterinaryClinicDto> CreateAsync(
    CreateVeterinaryClinicDto dto)
{
    var clinicName = dto.Name.Trim();

    if (string.IsNullOrWhiteSpace(clinicName))
    {
        throw new ArgumentException(
            "Veterinary clinic name is required.");
    }

    var clinicExists = await _context.VeterinaryClinics
        .AnyAsync(v =>
            v.IsActive &&
            v.Name.ToLower() == clinicName.ToLower());

    if (clinicExists)
    {
        throw new InvalidOperationException(
            "An active veterinary clinic with this name already exists.");
    }

    var clinic = new VeterinaryClinic
    {
        Id = Guid.NewGuid(),
        Name = clinicName,
        Phone = dto.Phone?.Trim(),
        Email = dto.Email?.Trim().ToLower(),
        Address = dto.Address?.Trim(),
        PrimaryContactName = dto.PrimaryContactName?.Trim(),
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    _context.VeterinaryClinics.Add(clinic);

    await _context.SaveChangesAsync();

    return new VeterinaryClinicDto
    {
        Id = clinic.Id,
        Name = clinic.Name,
        Phone = clinic.Phone,
        Email = clinic.Email,
        Address = clinic.Address,
        PrimaryContactName = clinic.PrimaryContactName,
        IsActive = clinic.IsActive,
        CreatedAt = clinic.CreatedAt
    };
}

    public async Task<PagedVeterinaryClinicsDto> GetAllAsync(
    int page,
    int pageSize,
    bool isActive)
{
    var query = _context.VeterinaryClinics
        .AsNoTracking()
        .Where(v => v.IsActive == isActive);

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderBy(v => v.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(v => new VeterinaryClinicDto
        {
            Id = v.Id,
            Name = v.Name,
            Phone = v.Phone,
            Email = v.Email,
            Address = v.Address,
            PrimaryContactName = v.PrimaryContactName,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        })
        .ToListAsync();

    return new PagedVeterinaryClinicsDto
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling(
            (double)totalItems / pageSize)
    };
}

    public async Task<VeterinaryClinicDto?> GetByIdAsync(Guid id)
{
    return await _context.VeterinaryClinics
        .AsNoTracking()
        .Where(v =>
            v.Id == id &&
            v.IsActive)
        .Select(v => new VeterinaryClinicDto
        {
            Id = v.Id,
            Name = v.Name,
            Phone = v.Phone,
            Email = v.Email,
            Address = v.Address,
            PrimaryContactName = v.PrimaryContactName,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        })
        .FirstOrDefaultAsync();
}

    public async Task<VeterinaryClinicDto?> UpdateAsync(
    Guid id,
    UpdateVeterinaryClinicDto dto)
{
    var clinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(v =>
            v.Id == id &&
            v.IsActive);

    if (clinic == null)
    {
        return null;
    }

    var clinicName = dto.Name.Trim();

    if (string.IsNullOrWhiteSpace(clinicName))
    {
        throw new ArgumentException(
            "Veterinary clinic name is required.");
    }

    var duplicateExists = await _context.VeterinaryClinics
        .AnyAsync(v =>
            v.Id != id &&
            v.IsActive &&
            v.Name.ToLower() == clinicName.ToLower());

    if (duplicateExists)
    {
        throw new InvalidOperationException(
            "Another active veterinary clinic with this name already exists.");
    }

    clinic.Name = clinicName;
    clinic.Phone = dto.Phone?.Trim();
    clinic.Email = dto.Email?.Trim().ToLower();
    clinic.Address = dto.Address?.Trim();
    clinic.PrimaryContactName =
        dto.PrimaryContactName?.Trim();

    await _context.SaveChangesAsync();

    return new VeterinaryClinicDto
    {
        Id = clinic.Id,
        Name = clinic.Name,
        Phone = clinic.Phone,
        Email = clinic.Email,
        Address = clinic.Address,
        PrimaryContactName = clinic.PrimaryContactName,
        IsActive = clinic.IsActive,
        CreatedAt = clinic.CreatedAt
    };
}

    public async Task<bool> DeactivateAsync(Guid id)
{
    var clinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(v =>
            v.Id == id &&
            v.IsActive);

    if (clinic == null)
    {
        return false;
    }

    clinic.IsActive = false;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<bool> RestoreAsync(Guid id)
{
    var clinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(v =>
            v.Id == id &&
            !v.IsActive);

    if (clinic == null)
    {
        return false;
    }

    clinic.IsActive = true;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<PagedVeterinaryClinicsDto> SearchAsync(
    string search,
    bool isActive,
    int page,
    int pageSize)
{
    var normalizedSearch = search.Trim();

    if (string.IsNullOrWhiteSpace(normalizedSearch))
    {
        return new PagedVeterinaryClinicsDto
        {
            Page = page,
            PageSize = pageSize
        };
    }

    var pattern = $"%{normalizedSearch}%";

    var query = _context.VeterinaryClinics
        .AsNoTracking()
        .Where(v =>
            v.IsActive == isActive &&
            (
                EF.Functions.ILike(v.Name, pattern) ||
                (
                    v.Phone != null &&
                    EF.Functions.ILike(v.Phone, pattern)
                ) ||
                (
                    v.Email != null &&
                    EF.Functions.ILike(v.Email, pattern)
                ) ||
                (
                    v.Address != null &&
                    EF.Functions.ILike(v.Address, pattern)
                ) ||
                (
                    v.PrimaryContactName != null &&
                    EF.Functions.ILike(v.PrimaryContactName, pattern)
                )
            ));

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderBy(v => v.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(v => new VeterinaryClinicDto
        {
            Id = v.Id,
            Name = v.Name,
            Phone = v.Phone,
            Email = v.Email,
            Address = v.Address,
            PrimaryContactName =
                v.PrimaryContactName,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        })
        .ToListAsync();

    return new PagedVeterinaryClinicsDto
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
