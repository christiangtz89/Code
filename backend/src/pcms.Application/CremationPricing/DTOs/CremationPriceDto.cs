using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.DTOs;

public class CremationPriceDto
{
    public Guid Id { get; set; }

    public Guid CremationPackageId { get; set; }

    public string CremationPackageName { get; set; } = string.Empty;

    public CremationPackageType PackageType { get; set; }

    public CremationType CremationType { get; set; }

    public decimal MinimumWeightKg { get; set; }

    public decimal MaximumWeightKg { get; set; }

    public decimal Price { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public bool IsPublic { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
