namespace pcms.Domain.Entities;

public class PaymentAccount
{
    public Guid Id { get; set; }

    public Guid? CremationId { get; set; }

    public Guid? CollectionId { get; set; }

    public Guid? CremationPriceId { get; set; }

    public Guid? CremationPackageId { get; set; }

    public string? PackageName { get; set; }

    public decimal ServiceTotal { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Cremation? Cremation { get; set; }

    public Collection? Collection { get; set; }

    public CremationPrice? CremationPrice { get; set; }

    public CremationPackage? CremationPackage { get; set; }

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
