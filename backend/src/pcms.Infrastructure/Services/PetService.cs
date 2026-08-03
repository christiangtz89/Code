using Microsoft.EntityFrameworkCore;
using pcms.Application.Pets.DTOs;
using pcms.Application.Pets.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class PetService : IPetService
{
    private readonly AppDbContext _context;

    public PetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PetDto> CreateAsync(CreatePetDto dto)
{

    if (dto.CustomerId == Guid.Empty)
{
    throw new ArgumentException(
        "Debe seleccionar un cliente válido.");
}

if (dto.DateOfDeath == default)
{
    throw new ArgumentException(
        "La fecha de fallecimiento es obligatoria.");
}

if (dto.DateOfDeath > DateTime.UtcNow)
{
    throw new ArgumentException(
        "La fecha de fallecimiento no puede estar en el futuro.");
}

    // Verify the customer exists
    var customer = await _context.Customers
    .FirstOrDefaultAsync(c =>
        c.Id == dto.CustomerId &&
        c.IsActive);

if (customer == null)
{
    throw new Exception("Customer not found.");
}

    var pet = new Pet
    {
        Id = Guid.NewGuid(),
        CustomerId = dto.CustomerId,
        Name = dto.Name,
        Species = dto.Species,
        Breed = dto.Breed,
        Sex = dto.Sex,
        Color = dto.Color,
        WeightKg = dto.WeightKg,
        AgeYears = dto.AgeYears,
        DateOfDeath = dto.DateOfDeath,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    _context.Pets.Add(pet);

    await _context.SaveChangesAsync();

    return new PetDto
{
    Id = pet.Id,
    CustomerId = pet.CustomerId,
    CustomerName =
        customer.FirstName + " " + customer.LastName,
    Name = pet.Name,
    Species = pet.Species,
    Breed = pet.Breed,
    Sex = pet.Sex,
    Color = pet.Color,
    WeightKg = pet.WeightKg,
    AgeYears = pet.AgeYears,
    DateOfDeath = pet.DateOfDeath,
    IsActive = pet.IsActive,
    CreatedAt = pet.CreatedAt
};
}

    public async Task<PagedPetsDto> GetAllAsync(
    int page,
    int pageSize)
{
    var query = _context.Pets
        .Where(p => p.IsActive);

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new PetDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            Name = p.Name,
            Species = p.Species,
            Breed = p.Breed,
            Sex = p.Sex,
            Color = p.Color,
            WeightKg = p.WeightKg,
            AgeYears = p.AgeYears,
            DateOfDeath = p.DateOfDeath,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        })
        .ToListAsync();

    return new PagedPetsDto
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
    };
}

    public async Task<PetDto?> GetByIdAsync(Guid id)
{
    return await _context.Pets
        .Where(p => p.Id == id && p.IsActive)
        .Select(p => new PetDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            Name = p.Name,
            Species = p.Species,
            Breed = p.Breed,
            Sex = p.Sex,
            Color = p.Color,
            WeightKg = p.WeightKg,
            AgeYears = p.AgeYears,
            DateOfDeath = p.DateOfDeath,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        })
        .FirstOrDefaultAsync();
}

    public async Task<PetDto?> UpdateAsync(
    Guid id,
    UpdatePetDto dto)
{

    if (dto.DateOfDeath == default)
{
    throw new ArgumentException(
        "La fecha de fallecimiento es obligatoria.");
}

if (dto.DateOfDeath > DateTime.UtcNow)
{
    throw new ArgumentException(
        "La fecha de fallecimiento no puede estar en el futuro.");
}

    var pet = await _context.Pets
        .Include(p => p.Customer)
        .FirstOrDefaultAsync(p =>
            p.Id == id &&
            p.IsActive);

    if (pet == null)
    {
        return null;
    }

    pet.Name = dto.Name;
    pet.Species = dto.Species;
    pet.Breed = dto.Breed;
    pet.Sex = dto.Sex;
    pet.Color = dto.Color;
    pet.WeightKg = dto.WeightKg;
    pet.AgeYears = dto.AgeYears;
    pet.DateOfDeath = dto.DateOfDeath;

    await _context.SaveChangesAsync();

    return new PetDto
    {
        Id = pet.Id,
        CustomerId = pet.CustomerId,
        CustomerName =
            pet.Customer.FirstName + " " + pet.Customer.LastName,
        Name = pet.Name,
        Species = pet.Species,
        Breed = pet.Breed,
        Sex = pet.Sex,
        Color = pet.Color,
        WeightKg = pet.WeightKg,
        AgeYears = pet.AgeYears,
        DateOfDeath = pet.DateOfDeath,
        IsActive = pet.IsActive,
        CreatedAt = pet.CreatedAt
    };
}

   public async Task<bool> DeactivateAsync(Guid id)
{
    var pet = await _context.Pets
        .FirstOrDefaultAsync(p =>
            p.Id == id &&
            p.IsActive);

    if (pet == null)
    {
        return false;
    }

    pet.IsActive = false;

    await _context.SaveChangesAsync();

    return true;
}

public async Task<bool> RestoreAsync(Guid id)
{
    var pet = await _context.Pets
        .FirstOrDefaultAsync(p =>
            p.Id == id &&
            !p.IsActive);

    if (pet == null)
    {
        return false;
    }

    pet.IsActive = true;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<IEnumerable<PetDto>> SearchAsync(string search)
{
    search = search.Trim().ToLower();

    return await _context.Pets
        .Where(p =>
            p.IsActive &&
            (
                p.Name.ToLower().Contains(search) ||
                p.Species.ToLower().Contains(search) ||
                p.Breed.ToLower().Contains(search) ||
                p.Customer.FirstName.ToLower().Contains(search) ||
                p.Customer.LastName.ToLower().Contains(search)
            ))
        .OrderBy(p => p.Name)
        .Select(p => new PetDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            CustomerName =
                p.Customer.FirstName + " " + p.Customer.LastName,
            Name = p.Name,
            Species = p.Species,
            Breed = p.Breed,
            Sex = p.Sex,
            Color = p.Color,
            WeightKg = p.WeightKg,
            AgeYears = p.AgeYears,
            DateOfDeath = p.DateOfDeath,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        })
        .ToListAsync();
}
}