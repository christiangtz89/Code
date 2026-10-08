using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

// Immutable evidence of a controlled change of current responsibility.
public class CremationReassignment
{
    public Guid Id { get; init; }
    public Guid CremationId { get; init; }
    public int Sequence { get; init; }
    public Guid RequestId { get; init; }
    public Guid? PreviousAssignedToUserId { get; init; }
    public string? PreviousUserNameSnapshot { get; init; }
    public Guid NewAssignedToUserId { get; init; }
    public string NewUserNameSnapshot { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public string ActorNameSnapshot { get; init; } = string.Empty;
    public string ActorRole { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public CremationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
