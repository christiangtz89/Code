namespace pcms.Application.Cremations.DTOs;

public class CremationReceptionOptionDto
{
    public Guid Id { get; set; }

    public string QrCode { get; set; }
        = string.Empty;

    public string PetName { get; set; }
        = string.Empty;

    public string CustomerName { get; set; }
        = string.Empty;
}