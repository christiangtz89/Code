using pcms.Domain.Enums;

namespace pcms.Application.Receptions.DTOs;

public sealed class ReceptionLifecycleEventDto
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid RequestId { get; set; }

    public long Sequence { get; set; }

    public ReceptionLifecycleActionKind Action { get; set; }

    public ReceptionLifecycleOutcomeKind Outcome { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }

    public string ActorUserName { get; set; } = string.Empty;

    public string ActorRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool PreviousIsActive { get; set; }

    public bool NewIsActive { get; set; }

    public ReceptionHistoryStage OperationalStage { get; set; }

    public Guid? CollectionId { get; set; }

    public bool? CollectionIsActive { get; set; }

    public CollectionStatus? CollectionStatus { get; set; }

    public DateTime? CollectionCollectedAt { get; set; }

    public DateTime? CollectionReceivedAt { get; set; }

    public bool HasConvertedVeterinaryRequest { get; set; }

    public Guid? VeterinaryRequestId { get; set; }

    public Guid? CremationId { get; set; }

    public bool? CremationIsActive { get; set; }

    public CremationStatus? CremationStatus { get; set; }

    public Guid? PaymentAccountId { get; set; }

    public decimal? ServiceTotal { get; set; }

    public decimal? AmountPaid { get; set; }

    public decimal? RequiredCollectionPaymentAmount { get; set; }

    public bool? RequiresFinancialReview { get; set; }

    public int PaymentCount { get; set; }

    public int ActiveReceptionEvidenceCount { get; set; }

    public int CollectionEvidenceCount { get; set; }

    public int CollectionAssignmentHistoryCount { get; set; }

    public int ReceptionHistoryCount { get; set; }

    public long? LatestReceptionHistorySequence { get; set; }

    public ReceptionLifecycleDependency Dependencies { get; set; }

    public ReceptionLifecycleBlockReason BlockReason { get; set; }
}
