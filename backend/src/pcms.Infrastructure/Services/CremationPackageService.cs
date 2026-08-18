using Microsoft.EntityFrameworkCore;
using pcms.Application.CremationPackages.DTOs;
using pcms.Application.CremationPackages.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CremationPackageService : ICremationPackageService
{
    private readonly AppDbContext _dbContext;

    public CremationPackageService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CremationPackageDto>> GetAllAsync(
        bool includeInactive = false,
        bool publicOnly = false)
    {
        var query = _dbContext.CremationPackages
            .AsNoTracking()
            .Include(package => package.UrnOptions)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (publicOnly)
        {
            query = query.Where(p => p.IsPublic);
        }

        var packages = await query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Tier)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return packages.Select(MapToDto);
    }

    public async Task<CremationPackageDto?> GetByIdAsync(Guid id)
    {
        var package = await _dbContext.CremationPackages
            .AsNoTracking()
            .Include(package => package.UrnOptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        return package is null
            ? null
            : MapToDto(package);
    }

    public async Task<CremationPackageDto> CreateAsync(
        CreateCremationPackageDto dto)
    {
        var allowedUrnIds = NormalizeAllowedUrnIds(
            dto.AllowedUrnIds);

        ValidateBasicFields(
            dto.Name,
            dto.PackageType,
            dto.Tier,
            dto.IncludesUrn,
            dto.IncludesPawPrint,
            dto.AccessoryDescription,
            dto.IncludesCertificate);

        ValidateAllowedUrnSelection(
            dto.IncludesUrn,
            allowedUrnIds);

        await ValidateCatalogRulesAsync(
            dto.PackageType,
            dto.Tier,
            dto.IsActive,
            null);

        await ValidateActiveUrnsAsync(allowedUrnIds);

        var now = DateTime.UtcNow;

        var package = new CremationPackage
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            ShortDescription = NormalizeOptional(dto.ShortDescription),
            Description = NormalizeOptional(dto.Description),
            PackageType = dto.PackageType,
            Tier = dto.Tier,
            IncludesUrn = dto.IncludesUrn,
            IncludesPawPrint = dto.IncludesPawPrint,
            AccessoryDescription = NormalizeOptional(
                dto.AccessoryDescription),
            IncludesCertificate = dto.IncludesCertificate,
            ImageUrl = NormalizeOptional(dto.ImageUrl),
            IsPublic = dto.IsPublic,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = now,
            UpdatedAt = null,
            UrnOptions = allowedUrnIds
                .Select(urnId => new CremationPackageUrn
                {
                    Id = Guid.NewGuid(),
                    UrnId = urnId,
                    IsActive = true,
                    CreatedAt = now
                })
                .ToList()
        };

        _dbContext.CremationPackages.Add(package);
        await _dbContext.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task<CremationPackageDto?> UpdateAsync(
        Guid id,
        UpdateCremationPackageDto dto)
    {
        var package = await _dbContext.CremationPackages
            .Include(existingPackage => existingPackage.UrnOptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package is null)
        {
            return null;
        }

        var allowedUrnIds = NormalizeAllowedUrnIds(
            dto.AllowedUrnIds);

        ValidateBasicFields(
            dto.Name,
            dto.PackageType,
            dto.Tier,
            dto.IncludesUrn,
            dto.IncludesPawPrint,
            dto.AccessoryDescription,
            dto.IncludesCertificate);

        ValidateAllowedUrnSelection(
            dto.IncludesUrn,
            allowedUrnIds);

        await ValidateCatalogRulesAsync(
            dto.PackageType,
            dto.Tier,
            dto.IsActive,
            id);

        await SynchronizeAllowedUrnsAsync(
            package,
            allowedUrnIds);

        package.Name = dto.Name.Trim();
        package.ShortDescription =
            NormalizeOptional(dto.ShortDescription);
        package.Description =
            NormalizeOptional(dto.Description);
        package.PackageType = dto.PackageType;
        package.Tier = dto.Tier;
        package.IncludesUrn = dto.IncludesUrn;
        package.IncludesPawPrint = dto.IncludesPawPrint;
        package.AccessoryDescription =
            NormalizeOptional(dto.AccessoryDescription);
        package.IncludesCertificate =
            dto.IncludesCertificate;
        package.ImageUrl =
            NormalizeOptional(dto.ImageUrl);
        package.IsPublic = dto.IsPublic;
        package.DisplayOrder = dto.DisplayOrder;
        package.IsActive = dto.IsActive;
        package.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapToDto(package);
    }

    private async Task ValidateActiveUrnsAsync(
        IReadOnlyCollection<Guid> urnIds)
    {
        if (urnIds.Count == 0)
        {
            return;
        }

        var activeUrnIds = await _dbContext.Urns
            .AsNoTracking()
            .Where(urn =>
                urn.IsActive &&
                urnIds.Contains(urn.Id))
            .Select(urn => urn.Id)
            .ToListAsync();

        if (activeUrnIds.Count != urnIds.Count)
        {
            throw new InvalidOperationException(
                "Una o más urnas seleccionadas no existen o están inactivas.");
        }
    }

    private async Task SynchronizeAllowedUrnsAsync(
        CremationPackage package,
        IReadOnlyCollection<Guid> allowedUrnIds)
    {
        var allowedUrnIdSet = allowedUrnIds.ToHashSet();
        var existingUrnIdSet = package.UrnOptions
            .Select(option => option.UrnId)
            .ToHashSet();

        var activeExistingUrnIdSet = package.UrnOptions
            .Where(option => option.IsActive)
            .Select(option => option.UrnId)
            .ToHashSet();

        var urnIdsRequiringValidation = allowedUrnIds
            .Where(urnId =>
                !activeExistingUrnIdSet.Contains(urnId))
            .ToList();

        var newUrnIds = allowedUrnIds
            .Where(urnId => !existingUrnIdSet.Contains(urnId))
            .ToList();

        await ValidateActiveUrnsAsync(
            urnIdsRequiringValidation);

        var now = DateTime.UtcNow;

        foreach (var option in package.UrnOptions)
        {
            var shouldBeActive = allowedUrnIdSet.Contains(
                option.UrnId);

            if (option.IsActive == shouldBeActive)
            {
                continue;
            }

            option.IsActive = shouldBeActive;
            option.UpdatedAt = now;
        }

        foreach (var urnId in newUrnIds)
        {
            package.UrnOptions.Add(new CremationPackageUrn
            {
                Id = Guid.NewGuid(),
                CremationPackageId = package.Id,
                UrnId = urnId,
                IsActive = true,
                CreatedAt = now
            });
        }
    }

    private async Task ValidateCatalogRulesAsync(
        CremationPackageType packageType,
        int? tier,
        bool isActive,
        Guid? currentPackageId)
    {
        if (!isActive)
        {
            return;
        }

        if (packageType == CremationPackageType.AshesReturn)
        {
            var tierInUse = await _dbContext.CremationPackages
                .AnyAsync(p =>
                    p.IsActive &&
                    p.PackageType ==
                        CremationPackageType.AshesReturn &&
                    p.Tier == tier &&
                    (!currentPackageId.HasValue ||
                     p.Id != currentPackageId.Value));

            if (tierInUse)
            {
                throw new InvalidOperationException(
                    $"Ya existe un paquete activo para el nivel {tier}.");
            }

            return;
        }

        var activeNoAshesPackageExists =
            await _dbContext.CremationPackages
                .AnyAsync(p =>
                    p.IsActive &&
                    p.PackageType ==
                        CremationPackageType.NoAshes &&
                    (!currentPackageId.HasValue ||
                     p.Id != currentPackageId.Value));

        if (activeNoAshesPackageExists)
        {
            throw new InvalidOperationException(
                "Solo puede existir un servicio activo sin devolución de cenizas.");
        }
    }

    private static void ValidateBasicFields(
        string name,
        CremationPackageType packageType,
        int? tier,
        bool includesUrn,
        bool includesPawPrint,
        string? accessoryDescription,
        bool includesCertificate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre del paquete es obligatorio.");
        }

        if (!Enum.IsDefined(packageType))
        {
            throw new ArgumentException(
                "El tipo de paquete no es válido.");
        }

        if (packageType == CremationPackageType.AshesReturn)
        {
            if (!tier.HasValue)
            {
                throw new InvalidOperationException(
                    "Los paquetes con devolución de cenizas deben tener un nivel.");
            }

            if (tier.Value is < 1 or > 4)
            {
                throw new InvalidOperationException(
                    "El nivel del paquete debe estar entre 1 y 4.");
            }

            return;
        }

        if (tier.HasValue)
        {
            throw new InvalidOperationException(
                "El servicio sin devolución de cenizas no debe tener nivel.");
        }

        if (includesUrn)
        {
            throw new InvalidOperationException(
                "El servicio sin devolución de cenizas no puede incluir urna.");
        }

        if (includesPawPrint)
        {
            throw new InvalidOperationException(
                "El servicio sin devolución de cenizas no puede incluir accesorio.");
        }

        if (!string.IsNullOrWhiteSpace(accessoryDescription))
        {
            throw new InvalidOperationException(
                "El servicio sin devolución de cenizas no puede tener descripción de accesorio.");
        }

        if (includesCertificate)
        {
            throw new InvalidOperationException(
                "El servicio sin devolución de cenizas no puede incluir certificado.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static List<Guid> NormalizeAllowedUrnIds(
        IEnumerable<Guid>? allowedUrnIds)
    {
        var normalizedIds = allowedUrnIds?
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (normalizedIds.Contains(Guid.Empty))
        {
            throw new ArgumentException(
                "Debe seleccionar urnas válidas para el paquete.");
        }

        return normalizedIds;
    }

    private static void ValidateAllowedUrnSelection(
        bool includesUrn,
        IReadOnlyCollection<Guid> allowedUrnIds)
    {
        if (includesUrn && allowedUrnIds.Count == 0)
        {
            throw new InvalidOperationException(
                "Los paquetes que incluyen urna deben permitir al menos una urna.");
        }

        if (!includesUrn && allowedUrnIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Un paquete que no incluye urna no puede tener urnas permitidas.");
        }
    }

    private static CremationPackageDto MapToDto(
        CremationPackage package)
    {
        return new CremationPackageDto
        {
            Id = package.Id,
            Name = package.Name,
            ShortDescription = package.ShortDescription,
            Description = package.Description,
            PackageType = package.PackageType,
            Tier = package.Tier,
            IncludesUrn = package.IncludesUrn,
            AllowedUrnIds = package.UrnOptions
                .Where(option => option.IsActive)
                .Select(option => option.UrnId)
                .OrderBy(urnId => urnId)
                .ToList(),
            IncludesPawPrint = package.IncludesPawPrint,
            AccessoryDescription = package.AccessoryDescription,
            IncludesCertificate = package.IncludesCertificate,
            ImageUrl = package.ImageUrl,
            IsPublic = package.IsPublic,
            DisplayOrder = package.DisplayOrder,
            IsActive = package.IsActive,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt
        };
    }
}
