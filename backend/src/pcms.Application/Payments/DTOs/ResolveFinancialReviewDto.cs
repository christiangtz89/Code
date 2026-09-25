using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Payments.DTOs;

public class ResolveFinancialReviewDto
{
    [Required(
        ErrorMessage =
            "El motivo de resolución es obligatorio.")]
    [StringLength(
        1000,
        ErrorMessage =
            "El motivo de resolución no puede exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
