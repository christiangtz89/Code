using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class ReceptionLifecycleEvent
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid RequestId { get; set; }

    public long SequenceNumber { get; set; }

    public ReceptionLifecycleActionKind ActionKind { get; set; }

    public ReceptionLifecycleOutcomeKind OutcomeKind { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string CreatedByUserNameSnapshot { get; set; } = string.Empty;

    public string CreatedByRoleSnapshot { get; set; } = string.Empty;

    public bool PreviousIsActive { get; set; }

    public bool NewIsActive { get; set; }

    public ReceptionHistoryStage OperationalStageSnapshot { get; set; }

    public Guid? CollectionId { get; set; }

    public bool? CollectionIsActiveSnapshot { get; set; }

    public CollectionStatus? CollectionStatusSnapshot { get; set; }

    public DateTime? CollectionCollectedAtSnapshot { get; set; }

    public DateTime? CollectionReceivedAtSnapshot { get; set; }

    public bool HasConvertedVeterinaryRequestSnapshot { get; set; }

    public Guid? VeterinaryRequestId { get; set; }

    public Guid? CremationId { get; set; }

    public bool? CremationIsActiveSnapshot { get; set; }

    public CremationStatus? CremationStatusSnapshot { get; set; }

    public Guid? PaymentAccountId { get; set; }

    public decimal? ServiceTotalSnapshot { get; set; }

    public decimal? AmountPaidSnapshot { get; set; }

    public decimal? RequiredCollectionPaymentAmountSnapshot { get; set; }

    public bool? RequiresFinancialReviewSnapshot { get; set; }

    public int PaymentCountSnapshot { get; set; }

    public int ActiveReceptionEvidenceCountSnapshot { get; set; }

    public int CollectionEvidenceCountSnapshot { get; set; }

    public int CollectionAssignmentHistoryCountSnapshot { get; set; }

    public int ReceptionHistoryCountSnapshot { get; set; }

    public long? LatestReceptionHistorySequenceSnapshot { get; set; }

    public ReceptionLifecycleDependency DependenciesSnapshot { get; set; }

    public ReceptionLifecycleBlockReason BlockReasonSnapshot { get; set; }

    public Reception Reception { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public Collection? Collection { get; set; }

    public VeterinaryRequest? VeterinaryRequest { get; set; }

    public Cremation? Cremation { get; set; }

    public PaymentAccount? PaymentAccount { get; set; }
}
