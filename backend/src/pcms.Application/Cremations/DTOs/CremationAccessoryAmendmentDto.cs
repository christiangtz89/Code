using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class CremationAccessoryAmendmentDto
{
    public Guid Id { get; set; }
    public Guid CremationId { get; set; }
    public int Sequence { get; set; }
    public Guid RequestId { get; set; }
    public string? PreviousAccessoryDescription { get; set; }
    public string? NewAccessoryDescription { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorNameSnapshot { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public CremationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
