using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.DTOs;

public class UpdateCremationPricingConfigurationDto
{
    [EnumDataType(typeof(WeightPricingInterval))]
    public WeightPricingInterval WeightInterval { get; set; }

    public bool AllowIndividualNoAshes { get; set; }
}