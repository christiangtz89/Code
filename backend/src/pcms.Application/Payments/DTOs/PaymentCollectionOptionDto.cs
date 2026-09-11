namespace pcms.Application.Payments.DTOs;

public class PaymentCollectionOptionDto
{
    public Guid CollectionId { get; set; }

    public Guid CremationPriceId { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public string PetName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }

    public decimal ServiceTotal { get; set; }

    public decimal RequiredCollectionPaymentAmount { get; set; }
}
