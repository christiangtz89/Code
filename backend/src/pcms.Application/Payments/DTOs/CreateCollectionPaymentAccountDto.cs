using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Payments.DTOs;

public class CreateCollectionPaymentAccountDto
{
    [Required]
    public Guid CollectionId { get; set; }

    [Required]
    public Guid CremationPriceId { get; set; }
}
