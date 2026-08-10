namespace pcms.Domain.Entities;

public class PaymentAccount
{
    public Guid Id { get; set; }

    public Guid CremationId { get; set; }

    public decimal ServiceTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Cremation Cremation { get; set; }
        = null!;

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}