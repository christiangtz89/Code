using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.DTOs;

public class CremationPriceQuoteDto
{
    public Guid CremationPackageId { get; set; }

    public string CremationPackageName { get; set; } = string.Empty;

    public CremationPackageType PackageType { get; set; }

    public CremationType CremationType { get; set; }

    public decimal WeightKg { get; set; }

    public decimal MinimumWeightKg { get; set; }

    public decimal MaximumWeightKg { get; set; }

    public decimal Price { get; set; }
}