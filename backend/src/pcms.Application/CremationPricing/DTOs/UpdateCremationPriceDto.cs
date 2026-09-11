using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.CremationPricing.DTOs;

public class UpdateCremationPriceDto
{
    [Required]
    public Guid CremationPackageId { get; set; }

    [EnumDataType(typeof(CremationType))]
    public CremationType CremationType { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal MinimumWeightKg { get; set; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal MaximumWeightKg { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Price { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "El pago requerido para recolección debe ser mayor que cero.")]
    public decimal RequiredCollectionPaymentAmount { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool IsActive { get; set; }
}
