namespace pcms.Application.Cremations.DTOs;

public class RevalidateStartPaymentResultDto
{
    public Guid CremationId { get; set; }

    public bool Applied { get; set; }

    public bool PriceConfirmationRequired { get; set; }

    public bool Replayed { get; set; }

    public Guid SelectedCremationPriceId { get; set; }

    public decimal? PreviousQuotedPrice { get; set; }

    // When present, the account total changes from this value to NewQuotedPrice.
    public decimal? PreviousPaymentAccountServiceTotal { get; set; }

    public decimal NewQuotedPrice { get; set; }

    public decimal RequiredStartPaymentAmount { get; set; }

    public string? Message { get; set; }
}
