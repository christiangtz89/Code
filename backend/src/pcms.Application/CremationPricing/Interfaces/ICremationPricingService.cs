using pcms.Application.CremationPricing.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.Interfaces;

public interface ICremationPricingService
{
    Task<CremationPricingConfigurationDto> GetConfigurationAsync();

    Task<CremationPricingConfigurationDto> UpdateConfigurationAsync(
        UpdateCremationPricingConfigurationDto dto);

    Task<IEnumerable<CremationPriceDto>> GetPricesAsync(
        Guid? cremationPackageId = null,
        CremationType? cremationType = null,
        bool includeInactive = false);

    Task<CremationPriceDto?> GetPriceByIdAsync(Guid id);

    Task<CremationPriceDto> CreatePriceAsync(
        CreateCremationPriceDto dto);

    Task<CremationPriceDto?> UpdatePriceAsync(
        Guid id,
        UpdateCremationPriceDto dto);

    Task<CremationPriceQuoteDto> GetQuoteAsync(
        Guid cremationPackageId,
        decimal weightKg,
        CremationType? cremationType = null);
}