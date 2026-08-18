using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Payments.DTOs;

public class CreatePaymentAccountDto
{
    [Required]
    public Guid CremationId { get; set; }
}