using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

// Append-only evidence of an actual status change, never inferred legacy history.
public class CremationStatusTransition
{
    public Guid Id { get; init; }
    public Guid CremationId { get; init; }
    public int Sequence { get; init; }
    public CremationStatus PreviousStatus { get; init; }
    public CremationStatus NewStatus { get; init; }
    public Guid ActorUserId { get; init; }
    public string ActorNameSnapshot { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public Guid? OperationalCommentId { get; init; }
}
