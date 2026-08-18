using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class CremationPricingConfiguration
{
    public Guid Id { get; set; }

    public WeightPricingInterval WeightInterval { get; set; }
        = WeightPricingInterval.FiveKg;

    public bool AllowIndividualNoAshes { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}