using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class PaymentAccount
{
    public Guid Id { get; set; }

    public Guid? CremationId { get; set; }

    public Guid? CollectionId { get; set; }

    public Guid? CremationPriceId { get; set; }

    public Guid? ProvisionalCremationPriceId { get; set; }

    public Guid? CremationPackageId { get; set; }

    public string? PackageName { get; set; }

    public CremationType? CremationTypeSnapshot { get; set; }

    public decimal? MinimumWeightKgSnapshot { get; set; }

    public decimal? MaximumWeightKgSnapshot { get; set; }

    public decimal? WeightKgSnapshot { get; set; }

    public decimal? ProvisionalMinimumWeightKgSnapshot { get; set; }

    public decimal? ProvisionalMaximumWeightKgSnapshot { get; set; }

    public decimal? ProvisionalWeightKgSnapshot { get; set; }

    public decimal? ProvisionalServiceTotal { get; set; }

    public decimal ServiceTotal { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public Guid? WeightRangeChangeConfirmedByUserId { get; set; }

    public string? WeightRangeChangeConfirmedByUserNameSnapshot { get; set; }

    public DateTime? WeightRangeChangeConfirmedAt { get; set; }

    public bool RequiresFinancialReview { get; set; }

    public Guid? FinancialReviewResolvedByUserId { get; set; }

    public string? FinancialReviewResolvedByUserNameSnapshot { get; set; }

    public DateTime? FinancialReviewResolvedAt { get; set; }

    public string? FinancialReviewResolutionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Cremation? Cremation { get; set; }

    public Collection? Collection { get; set; }

    public CremationPrice? CremationPrice { get; set; }

    public CremationPrice? ProvisionalCremationPrice { get; set; }

    public User? WeightRangeChangeConfirmedByUser { get; set; }

    public User? FinancialReviewResolvedByUser { get; set; }

    public CremationPackage? CremationPackage { get; set; }

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
