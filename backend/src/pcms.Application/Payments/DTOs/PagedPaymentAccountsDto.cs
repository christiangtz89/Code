namespace pcms.Application.Payments.DTOs;

public class PagedPaymentAccountsDto
{
    public IEnumerable<PaymentAccountDto> Items { get; set; }
        = new List<PaymentAccountDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}