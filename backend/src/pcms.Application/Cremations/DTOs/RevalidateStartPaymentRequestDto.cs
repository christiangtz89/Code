namespace pcms.Application.Cremations.DTOs;

public class RevalidateStartPaymentRequestDto
{
    public Guid RequestId { get; set; }

    public bool ConfirmPriceChange { get; set; }

    public decimal? ExpectedCurrentQuotedPrice { get; set; }

    public decimal? ExpectedNewQuotedPrice { get; set; }

    public decimal? ExpectedRequiredStartPaymentAmount { get; set; }

    public decimal? ExpectedCurrentPaymentAccountServiceTotal { get; set; }

    public Guid? ExpectedCremationPriceId { get; set; }
}
