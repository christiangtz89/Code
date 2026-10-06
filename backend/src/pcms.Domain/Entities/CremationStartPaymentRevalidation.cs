namespace pcms.Domain.Entities;

public class CremationStartPaymentRevalidation
{
    public Guid Id { get; set; }

    public Guid CremationId { get; set; }

    public Guid ConfirmedByUserId { get; set; }

    public string ConfirmedByUserNameSnapshot { get; set; } = string.Empty;

    public DateTime ConfirmedAt { get; set; }

    public Guid RequestId { get; set; }

    public Guid SelectedCremationPriceId { get; set; }

    public decimal? PreviousQuotedPrice { get; set; }

    public decimal NewQuotedPrice { get; set; }

    public decimal EstablishedRequiredStartPaymentAmount { get; set; }

    public Cremation Cremation { get; set; } = null!;

    public User ConfirmedByUser { get; set; } = null!;

    public CremationPrice SelectedCremationPrice { get; set; } = null!;
}
