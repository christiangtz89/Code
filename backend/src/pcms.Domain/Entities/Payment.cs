using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid PaymentAccountId { get; set; }

    public Guid RecordedByUserId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public PaymentAccount PaymentAccount { get; set; }
        = null!;

    public User RecordedByUser { get; set; }
        = null!;
}