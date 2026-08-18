using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.DTOs;

public class CremationPricingConfigurationDto
{
    public Guid Id { get; set; }

    public WeightPricingInterval WeightInterval { get; set; }

    public bool AllowIndividualNoAshes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}