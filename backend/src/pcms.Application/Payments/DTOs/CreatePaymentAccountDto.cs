using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Payments.DTOs;

public class CreatePaymentAccountDto
{
    public Guid CremationId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "El total del servicio debe ser mayor que cero.")]
    public decimal ServiceTotal { get; set; }
}