using pcms.Domain.Enums;

namespace pcms.Application.Payments.DTOs;

public class PaymentDto
{
    public Guid Id { get; set; }

    public Guid PaymentAccountId { get; set; }

    public Guid RecordedByUserId { get; set; }

    public string RecordedByUserName { get; set; }
        = string.Empty;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}