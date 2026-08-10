using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Payments.DTOs;

public class CreatePaymentDto
{
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "El monto del pago debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    [EnumDataType(
        typeof(PaymentMethod),
        ErrorMessage =
            "El método de pago no es válido.")]
    public PaymentMethod Method { get; set; }

    public DateTime PaidAt { get; set; }

    [StringLength(
        150,
        ErrorMessage =
            "La referencia no puede exceder 150 caracteres.")]
    public string? Reference { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}