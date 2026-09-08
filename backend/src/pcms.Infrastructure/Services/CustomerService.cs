using Microsoft.EntityFrameworkCore;
using pcms.Application.Common;
using pcms.Application.Customers;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(
        AppDbContext context)
    {
        _context = context;
    }

public async Task<PaginatedResult<CustomerDto>> GetAllAsync(
    int page,
    int pageSize,
    bool isActive)
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = _context.Customers
        .AsNoTracking()
        .Where(c => c.IsActive == isActive);

    var totalItems = await query.CountAsync();

    var customers = await query
        .OrderBy(c => c.LastName)
        .ThenBy(c => c.FirstName)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new CustomerDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            SecondLastName = c.SecondLastName,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        })
        .ToListAsync();

    return new PaginatedResult<CustomerDto>
    {
        Items = customers,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize)
    };
}
    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);


        if (customer == null)
            return null;


        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            SecondLastName = customer.SecondLastName,
            Phone = customer.Phone,
            Email = customer.Email,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        };
    }


    public async Task<CustomerDto> CreateAsync(
        CreateCustomerDto dto)
    {
        var input = CustomerPetInputRules.NormalizeCustomer(
            dto.FirstName,
            dto.LastName,
            dto.SecondLastName,
            dto.Phone,
            dto.Email);

        var customer = new Customer
{
            Id = Guid.NewGuid(),
            FirstName = input.FirstName,
            LastName = input.LastName,
            SecondLastName = input.SecondLastName,
            Phone = input.Phone,
            Email = input.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
};

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();


        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            SecondLastName = customer.SecondLastName,
            Phone = customer.Phone,
            Email = customer.Email,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        };
    }

public async Task<CustomerDto?> UpdateAsync(
    Guid id,
    UpdateCustomerDto dto)
{
    var input = CustomerPetInputRules.NormalizeCustomer(
        dto.FirstName,
        dto.LastName,
        dto.SecondLastName,
        dto.Phone,
        dto.Email);

    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == id);


    if (customer == null)
        return null;


        customer.FirstName = input.FirstName;
        customer.LastName = input.LastName;
        customer.SecondLastName = input.SecondLastName;
        customer.Phone = input.Phone;
        customer.Email = input.Email;


    await _context.SaveChangesAsync();


    return new CustomerDto
    {
        Id = customer.Id,

        FirstName = customer.FirstName,

        LastName = customer.LastName,

        SecondLastName = customer.SecondLastName,

        Phone = customer.Phone,

        Email = customer.Email,

        IsActive = customer.IsActive,

        CreatedAt = customer.CreatedAt
    };
}

public async Task<bool> DeactivateAsync(Guid id)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == id);


    if (customer == null)
        return false;


    customer.IsActive = false;


    await _context.SaveChangesAsync();


    return true;
}

public async Task<IEnumerable<CustomerDto>> SearchAsync(
    string term,
    bool isActive)
{
    if (string.IsNullOrWhiteSpace(term))
    {
        return Array.Empty<CustomerDto>();
    }

    var normalizedTerm = term.Trim();

    return await _context.Customers
        .AsNoTracking()
        .Where(c => c.IsActive == isActive)
        .Where(c =>
            EF.Functions.ILike(
                c.FirstName,
                $"%{normalizedTerm}%") ||

            EF.Functions.ILike(
                c.LastName,
                $"%{normalizedTerm}%") ||

            (
                c.SecondLastName != null &&
                EF.Functions.ILike(
                    c.SecondLastName,
                    $"%{normalizedTerm}%")
            ) ||

            EF.Functions.ILike(
                c.FirstName + " " +
                c.LastName + " " +
                (c.SecondLastName ?? ""),
                $"%{normalizedTerm}%") ||

            EF.Functions.ILike(
                c.Phone,
                $"%{normalizedTerm}%") ||

            EF.Functions.ILike(
                c.Email,
                $"%{normalizedTerm}%"))
        .OrderBy(c => c.FirstName)
        .ThenBy(c => c.LastName)
        .ThenBy(c => c.SecondLastName)
        .Select(c => new CustomerDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            SecondLastName = c.SecondLastName,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        })
        .ToListAsync();
}

public async Task<bool> RestoreAsync(Guid id)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == id);


    if (customer == null)
        return false;


    customer.IsActive = true;


    await _context.SaveChangesAsync();


    return true;
}

}
