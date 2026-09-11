using pcms.Domain.Enums;

namespace pcms.Application.Payments.DTOs;

public class PaymentAccountDto
{
    public Guid Id { get; set; }

    public Guid? CremationId { get; set; }

    public Guid? CollectionId { get; set; }

    public Guid? ReceptionId { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public Guid PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public bool IsCremationActive { get; set; }

    public decimal ServiceTotal { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public bool IsCollectionPaymentSatisfied { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IEnumerable<PaymentDto> Payments { get; set; }
        = new List<PaymentDto>();
}
