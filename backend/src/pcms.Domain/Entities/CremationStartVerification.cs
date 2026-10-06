namespace pcms.Domain.Entities;

public class CremationStartVerification
{
    public Guid Id { get; set; }

    public Guid CremationId { get; set; }

    public Guid ReceptionId { get; set; }

    public string ReceptionQrCodeSnapshot { get; set; } = string.Empty;

    public Guid ConfirmedByUserId { get; set; }

    public string ConfirmedByUserNameSnapshot { get; set; } = string.Empty;

    public DateTime ConfirmedAt { get; set; }

    public Guid RequestId { get; set; }

    public Cremation Cremation { get; set; } = null!;

    public Reception Reception { get; set; } = null!;

    public User ConfirmedByUser { get; set; } = null!;
}
