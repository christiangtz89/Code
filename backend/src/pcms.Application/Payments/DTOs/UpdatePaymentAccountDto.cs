using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Payments.DTOs;

public class UpdatePaymentAccountDto
{
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "El total del servicio debe ser mayor que cero.")]
    public decimal ServiceTotal { get; set; }
}