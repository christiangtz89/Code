using Microsoft.EntityFrameworkCore;
using pcms.Application.Urns.DTOs;
using pcms.Application.Urns.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class UrnService : IUrnService
{

    private readonly AppDbContext _dbContext;

    public UrnService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<UrnDto>> GetAllAsync(
        bool includeInactive = false,
        bool publicOnly = false)
    {
        var query = _dbContext.Urns
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        if (publicOnly)
        {
            query = query.Where(u => u.IsPublic);
        }

        var urns = await query
            .OrderBy(u => u.DisplayOrder)
            .ThenBy(u => u.Name)
            .ToListAsync();

        return urns.Select(MapToDto);
    }

    public async Task<UrnDto?> GetByIdAsync(Guid id)
    {
        var urn = await _dbContext.Urns
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return urn is null
            ? null
            : MapToDto(urn);
    }

    public async Task<UrnDto> CreateAsync(CreateUrnDto dto)
    {
        Validate(
            dto.Name,
            dto.DisplayOrder);

        var now = DateTime.UtcNow;

        var urn = new Urn
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = NormalizeOptional(dto.Description),
            Material = NormalizeOptional(dto.Material),
            Color = NormalizeOptional(dto.Color),
            ImageUrl = NormalizeOptional(dto.ImageUrl),
            IsPublic = dto.IsPublic,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = now,
            UpdatedAt = null
        };

        _dbContext.Urns.Add(urn);
        await _dbContext.SaveChangesAsync();

        return MapToDto(urn);
    }

    public async Task<UrnDto?> UpdateAsync(
        Guid id,
        UpdateUrnDto dto)
    {
        var urn = await _dbContext.Urns
            .FirstOrDefaultAsync(u => u.Id == id);

        if (urn is null)
        {
            return null;
        }

        Validate(
            dto.Name,
            dto.DisplayOrder);

        urn.Name = dto.Name.Trim();
        urn.Description = NormalizeOptional(dto.Description);
        urn.Material = NormalizeOptional(dto.Material);
        urn.Color = NormalizeOptional(dto.Color);
        urn.ImageUrl = NormalizeOptional(dto.ImageUrl);
        urn.IsPublic = dto.IsPublic;
        urn.DisplayOrder = dto.DisplayOrder;
        urn.IsActive = dto.IsActive;
        urn.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapToDto(urn);
    }

    private static void Validate(
        string name,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre de la urna es obligatorio.");
        }

        if (displayOrder < 0)
        {
            throw new ArgumentException(
                "El orden de visualización no puede ser negativo.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static UrnDto MapToDto(Urn urn)
    {
        return new UrnDto
        {
            Id = urn.Id,
            Name = urn.Name,
            Description = urn.Description,
            Material = urn.Material,
            Color = urn.Color,
            ImageUrl = urn.ImageUrl,
            IsPublic = urn.IsPublic,
            DisplayOrder = urn.DisplayOrder,
            IsActive = urn.IsActive,
            CreatedAt = urn.CreatedAt,
            UpdatedAt = urn.UpdatedAt
        };
    }
}
