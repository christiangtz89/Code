using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class CremationClarification
{
    public Guid Id { get; set; }
    public Guid CremationId { get; set; }
    public int Sequence { get; set; }
    public Guid RequestId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string ActorNameSnapshot { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public CremationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
