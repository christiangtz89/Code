using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class CremationPrice
{
    public Guid Id { get; set; }

    public Guid CremationPackageId { get; set; }

    public CremationPackage CremationPackage { get; set; } = null!;

    public CremationType CremationType { get; set; }

    public decimal MinimumWeightKg { get; set; }

    public decimal MaximumWeightKg { get; set; }

    public decimal Price { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
