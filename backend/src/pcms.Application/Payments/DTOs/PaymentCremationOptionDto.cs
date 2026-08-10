namespace pcms.Application.Payments.DTOs;

public class PaymentCremationOptionDto
{
    public Guid Id { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public string PetName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;
}