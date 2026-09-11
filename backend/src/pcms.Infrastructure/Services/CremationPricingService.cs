using Microsoft.EntityFrameworkCore;
using pcms.Application.CremationPricing.DTOs;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CremationPricingService : ICremationPricingService
{

    private const decimal MaximumSupportedWeightKg = 100m;

    private const decimal MaximumPrice = 9_999_999_999.99m;

    private readonly AppDbContext _dbContext;

    public CremationPricingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CremationPricingConfigurationDto> GetConfigurationAsync()
    {
        var configuration = await GetOrCreateConfigurationAsync();

        return MapConfigurationToDto(configuration);
    }

    public async Task<CremationPricingConfigurationDto> UpdateConfigurationAsync(
        UpdateCremationPricingConfigurationDto dto)
    {
        ValidateWeightInterval(dto.WeightInterval);

        var configuration = await GetOrCreateConfigurationAsync();

        configuration.WeightInterval = dto.WeightInterval;
        configuration.AllowIndividualNoAshes =
            dto.AllowIndividualNoAshes;
        configuration.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapConfigurationToDto(configuration);
    }

    public async Task<IEnumerable<CremationPriceDto>> GetPricesAsync(
        Guid? cremationPackageId = null,
        CremationType? cremationType = null,
        bool includeInactive = false)
    {
        var query = _dbContext.CremationPrices
            .AsNoTracking()
            .Include(p => p.CremationPackage)
            .AsQueryable();

        if (cremationPackageId.HasValue)
        {
            query = query.Where(
                p => p.CremationPackageId ==
                     cremationPackageId.Value);
        }

        if (cremationType.HasValue)
        {
            query = query.Where(
                p => p.CremationType ==
                     cremationType.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var prices = await query
            .OrderBy(p => p.CremationPackage.DisplayOrder)
            .ThenBy(p => p.CremationPackage.Name)
            .ThenBy(p => p.CremationType)
            .ThenBy(p => p.MinimumWeightKg)
            .ToListAsync();

        return prices.Select(MapPriceToDto);
    }

    public async Task<CremationPriceDto?> GetPriceByIdAsync(
        Guid id)
    {
        var price = await _dbContext.CremationPrices
            .AsNoTracking()
            .Include(p => p.CremationPackage)
            .FirstOrDefaultAsync(p => p.Id == id);

        return price is null
            ? null
            : MapPriceToDto(price);
    }

    public async Task<CremationPriceDto> CreatePriceAsync(
        CreateCremationPriceDto dto)
    {
        ValidateBasicPriceFields(
            dto.MinimumWeightKg,
            dto.MaximumWeightKg,
            dto.Price,
            dto.RequiredCollectionPaymentAmount,
            dto.CremationType);

        var configuration =
            await GetOrCreateConfigurationAsync();

        ValidateRangeAgainstConfiguration(
            dto.MinimumWeightKg,
            dto.MaximumWeightKg,
            configuration.WeightInterval);

        var package = await GetActivePackageAsync(
            dto.CremationPackageId);

        ValidatePackageCremationType(
            package,
            dto.CremationType,
            configuration.AllowIndividualNoAshes);

        if (dto.IsActive)
        {
            await ValidateNoOverlappingRangeAsync(
    dto.CremationPackageId,
    dto.CremationType,
    dto.MinimumWeightKg,
    dto.MaximumWeightKg,
    configuration.WeightInterval,
    null);
        }

        var price = new CremationPrice
        {
            Id = Guid.NewGuid(),
            CremationPackageId = dto.CremationPackageId,
            CremationType = dto.CremationType,
            MinimumWeightKg = dto.MinimumWeightKg,
            MaximumWeightKg = dto.MaximumWeightKg,
            Price = dto.Price,
            RequiredCollectionPaymentAmount =
                dto.RequiredCollectionPaymentAmount,
            IsPublic = dto.IsPublic,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _dbContext.CremationPrices.Add(price);
        await _dbContext.SaveChangesAsync();

        price.CremationPackage = package;

        return MapPriceToDto(price);
    }

    public async Task<CremationPriceDto?> UpdatePriceAsync(
        Guid id,
        UpdateCremationPriceDto dto)
    {
        var price = await _dbContext.CremationPrices
            .Include(p => p.CremationPackage)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (price is null)
        {
            return null;
        }

        ValidateBasicPriceFields(
            dto.MinimumWeightKg,
            dto.MaximumWeightKg,
            dto.Price,
            dto.RequiredCollectionPaymentAmount,
            dto.CremationType);

        var configuration =
            await GetOrCreateConfigurationAsync();

        ValidateRangeAgainstConfiguration(
            dto.MinimumWeightKg,
            dto.MaximumWeightKg,
            configuration.WeightInterval);

        CremationPackage package;

        if (price.CremationPackageId ==
            dto.CremationPackageId)
        {
            package = price.CremationPackage;
        }
        else
        {
            package = await GetActivePackageAsync(
                dto.CremationPackageId);
        }

        ValidatePackageCremationType(
            package,
            dto.CremationType,
            configuration.AllowIndividualNoAshes);

        if (dto.IsActive)
        {
            await ValidateNoOverlappingRangeAsync(
    dto.CremationPackageId,
    dto.CremationType,
    dto.MinimumWeightKg,
    dto.MaximumWeightKg,
    configuration.WeightInterval,
    id);
        }

        price.CremationPackageId =
            dto.CremationPackageId;
        price.CremationPackage = package;
        price.CremationType =
            dto.CremationType;
        price.MinimumWeightKg =
            dto.MinimumWeightKg;
        price.MaximumWeightKg =
            dto.MaximumWeightKg;
        price.Price =
            dto.Price;
        price.RequiredCollectionPaymentAmount =
            dto.RequiredCollectionPaymentAmount;
        price.IsPublic = dto.IsPublic;
        price.IsActive =
            dto.IsActive;
        price.UpdatedAt =
            DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapPriceToDto(price);
    }

    public async Task<CremationPriceQuoteDto> GetQuoteAsync(
        Guid cremationPackageId,
        decimal weightKg,
        CremationType? cremationType = null)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentException(
                "El peso debe ser mayor a cero.");
        }

        if (!HasAtMostTwoDecimalPlaces(weightKg))
        {
            throw new ArgumentException(
                "El peso no puede tener más de dos decimales.");
        }

        var configuration =
            await GetOrCreateConfigurationAsync();

        var package = await GetActivePackageAsync(
            cremationPackageId);

        var resolvedCremationType =
            ResolveCremationType(
                package,
                cremationType,
                configuration.AllowIndividualNoAshes);

        var prices = await _dbContext.CremationPrices
            .AsNoTracking()
            .Where(p =>
                p.CremationPackageId ==
                    cremationPackageId &&
                p.CremationType ==
                    resolvedCremationType &&
                p.IsActive)
            .ToListAsync();

        var matchingPrices = prices
            .Where(p =>
                IsRangeCompatibleWithInterval(
                    p.MinimumWeightKg,
                    p.MaximumWeightKg,
                    configuration.WeightInterval))
            .Where(p =>
                weightKg >= p.MinimumWeightKg &&
                weightKg <= p.MaximumWeightKg)
            .ToList();

        if (matchingPrices.Count == 0)
        {
            throw new InvalidOperationException(
                "No existe un precio configurado para el peso indicado.");
        }

        if (matchingPrices.Count > 1)
        {
            throw new InvalidOperationException(
                "Existe más de un precio aplicable al peso indicado.");
        }

        var price = matchingPrices[0];

        return new CremationPriceQuoteDto
        {
            CremationPackageId = package.Id,
            CremationPackageName = package.Name,
            PackageType = package.PackageType,
            CremationType = resolvedCremationType,
            WeightKg = weightKg,
            MinimumWeightKg = price.MinimumWeightKg,
            MaximumWeightKg = price.MaximumWeightKg,
            Price = price.Price,
            RequiredCollectionPaymentAmount =
                price.RequiredCollectionPaymentAmount
        };
    }

    private async Task<CremationPricingConfiguration>
        GetOrCreateConfigurationAsync()
    {
        var configurations =
            await _dbContext.CremationPricingConfigurations
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        if (configurations.Count > 1)
        {
            throw new InvalidOperationException(
                "Existe más de una configuración de precios de cremación.");
        }

        if (configurations.Count == 1)
        {
            return configurations[0];
        }

        var configuration =
            new CremationPricingConfiguration
            {
                Id = Guid.NewGuid(),
                WeightInterval =
                    WeightPricingInterval.FiveKg,
                AllowIndividualNoAshes = false,
                CreatedAt = DateTime.UtcNow
            };

        _dbContext.CremationPricingConfigurations.Add(
            configuration);

        await _dbContext.SaveChangesAsync();

        return configuration;
    }

    private async Task<CremationPackage>
        GetActivePackageAsync(Guid id)
    {
        var package =
            await _dbContext.CremationPackages
                .FirstOrDefaultAsync(
                    p => p.Id == id && p.IsActive);

        if (package is null)
        {
            throw new InvalidOperationException(
                "El paquete de cremación no existe o está inactivo.");
        }

        return package;
    }

    private async Task ValidateNoOverlappingRangeAsync(
    Guid packageId,
    CremationType cremationType,
    decimal minimumWeightKg,
    decimal maximumWeightKg,
    WeightPricingInterval interval,
    Guid? currentPriceId)
    {
        var existingPrices = await _dbContext.CremationPrices
            .AsNoTracking()
            .Where(p =>
                p.IsActive &&
                p.CremationPackageId == packageId &&
                p.CremationType == cremationType &&
                (!currentPriceId.HasValue ||
                 p.Id != currentPriceId.Value))
            .ToListAsync();

        var overlaps = existingPrices
            .Where(p =>
                IsRangeCompatibleWithInterval(
                    p.MinimumWeightKg,
                    p.MaximumWeightKg,
                    interval))
            .Any(p =>
                minimumWeightKg <= p.MaximumWeightKg &&
                maximumWeightKg >= p.MinimumWeightKg);

        if (overlaps)
        {
            throw new InvalidOperationException(
                "El rango de peso se traslapa con otro precio activo del mismo intervalo.");
        }
    }

    private static void ValidatePackageCremationType(
        CremationPackage package,
        CremationType cremationType,
        bool allowIndividualNoAshes)
    {
        if (package.PackageType ==
            CremationPackageType.AshesReturn)
        {
            if (cremationType != CremationType.Individual)
            {
                throw new InvalidOperationException(
                    "Los paquetes con devolución de cenizas solo permiten cremación individual.");
            }

            return;
        }

        if (cremationType == CremationType.Communal)
        {
            return;
        }

        if (cremationType == CremationType.Individual &&
            allowIndividualNoAshes)
        {
            return;
        }

        throw new InvalidOperationException(
            "La cremación individual sin devolución de cenizas no está habilitada.");
    }

    private static CremationType ResolveCremationType(
        CremationPackage package,
        CremationType? requestedType,
        bool allowIndividualNoAshes)
    {
        if (package.PackageType ==
            CremationPackageType.AshesReturn)
        {
            if (requestedType.HasValue &&
                requestedType.Value !=
                    CremationType.Individual)
            {
                throw new InvalidOperationException(
                    "Los paquetes con devolución de cenizas solo permiten cremación individual.");
            }

            return CremationType.Individual;
        }

        if (!requestedType.HasValue)
        {
            return CremationType.Communal;
        }

        ValidatePackageCremationType(
            package,
            requestedType.Value,
            allowIndividualNoAshes);

        return requestedType.Value;
    }

    private static void ValidateBasicPriceFields(
        decimal minimumWeightKg,
        decimal maximumWeightKg,
        decimal price,
        decimal requiredCollectionPaymentAmount,
        CremationType cremationType)
    {
        if (!Enum.IsDefined(
                typeof(CremationType),
                cremationType))
        {
            throw new ArgumentException(
                "El tipo de cremación no es válido.");
        }

        if (!HasAtMostTwoDecimalPlaces(minimumWeightKg) ||
            !HasAtMostTwoDecimalPlaces(maximumWeightKg))
        {
            throw new ArgumentException(
                "Los pesos no pueden tener más de dos decimales.");
        }

        if (!HasAtMostTwoDecimalPlaces(price))
        {
            throw new ArgumentException(
                "El precio no puede tener más de dos decimales.");
        }

        if (minimumWeightKg < 0)
        {
            throw new ArgumentException(
                "El peso mínimo no puede ser negativo.");
        }

        if (maximumWeightKg <= minimumWeightKg)
        {
            throw new ArgumentException(
                "El peso máximo debe ser mayor al peso mínimo.");
        }

        if (minimumWeightKg >= MaximumSupportedWeightKg)
        {
            throw new ArgumentException(
                $"El peso mínimo debe ser menor de {MaximumSupportedWeightKg:0} kg.");
        }

        if (maximumWeightKg > MaximumSupportedWeightKg)
        {
            throw new ArgumentException(
                $"El peso máximo permitido es {MaximumSupportedWeightKg:0} kg.");
        }

        if (price < 0.01m)
        {
            throw new ArgumentException(
                "El precio debe ser mayor que cero.");
        }

        if (price > MaximumPrice)
        {
            throw new ArgumentException(
                $"El precio no puede exceder {MaximumPrice:0.00}.");
        }

        if (!HasAtMostTwoDecimalPlaces(
                requiredCollectionPaymentAmount))
        {
            throw new ArgumentException(
                "El pago requerido para recolección no puede tener más de dos decimales.");
        }

        if (requiredCollectionPaymentAmount < 0.01m)
        {
            throw new ArgumentException(
                "El pago requerido para recolección debe ser mayor que cero.");
        }

        if (requiredCollectionPaymentAmount > price)
        {
            throw new ArgumentException(
                "El pago requerido para recolección no puede exceder el precio del servicio.");
        }
    }

    private static bool HasAtMostTwoDecimalPlaces(
        decimal value)
    {
        return decimal.Round(value, 2) == value;
    }

    private static void ValidateWeightInterval(
        WeightPricingInterval interval)
    {
        if (interval != WeightPricingInterval.FiveKg &&
            interval != WeightPricingInterval.TenKg)
        {
            throw new ArgumentException(
                "El intervalo de peso debe ser de 5 kg o 10 kg.");
        }
    }

    private static void ValidateRangeAgainstConfiguration(
        decimal minimumWeightKg,
        decimal maximumWeightKg,
        WeightPricingInterval interval)
    {
        if (!IsRangeCompatibleWithInterval(
                minimumWeightKg,
                maximumWeightKg,
                interval))
        {
            throw new InvalidOperationException(
                $"El rango de peso no corresponde al intervalo activo de {(int)interval} kg.");
        }
    }

    private static bool IsRangeCompatibleWithInterval(
    decimal minimumWeightKg,
    decimal maximumWeightKg,
    WeightPricingInterval interval)
    {
        var step = (decimal)(int)interval;

        if (minimumWeightKg < 0 ||
            maximumWeightKg <= minimumWeightKg)
        {
            return false;
        }

        if (minimumWeightKg == 0)
        {
            return maximumWeightKg == step;
        }

        var previousBoundary = minimumWeightKg - 0.01m;

        return previousBoundary >= 0 &&
               previousBoundary % step == 0 &&
               maximumWeightKg % step == 0 &&
               maximumWeightKg - previousBoundary == step;
    }

    private static CremationPricingConfigurationDto
        MapConfigurationToDto(
            CremationPricingConfiguration configuration)
    {
        return new CremationPricingConfigurationDto
        {
            Id = configuration.Id,
            WeightInterval =
                configuration.WeightInterval,
            AllowIndividualNoAshes =
                configuration.AllowIndividualNoAshes,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }

    private static CremationPriceDto MapPriceToDto(
        CremationPrice price)
    {
        return new CremationPriceDto
        {
            Id = price.Id,
            CremationPackageId =
                price.CremationPackageId,
            CremationPackageName =
                price.CremationPackage.Name,
            PackageType =
                price.CremationPackage.PackageType,
            CremationType =
                price.CremationType,
            MinimumWeightKg =
                price.MinimumWeightKg,
            MaximumWeightKg =
                price.MaximumWeightKg,
            Price = price.Price,
            RequiredCollectionPaymentAmount =
                price.RequiredCollectionPaymentAmount,
            IsPublic = price.IsPublic,
            IsActive = price.IsActive,
            CreatedAt = price.CreatedAt,
            UpdatedAt = price.UpdatedAt
        };
    }
}
