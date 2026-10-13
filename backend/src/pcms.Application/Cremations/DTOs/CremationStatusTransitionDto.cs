using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class CremationStatusTransitionDto
{
    public Guid Id { get; set; }
    public Guid CremationId { get; set; }
    public int Sequence { get; set; }
    public CremationStatus PreviousStatus { get; set; }
    public CremationStatus NewStatus { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorNameSnapshot { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public Guid? OperationalCommentId { get; set; }
}
