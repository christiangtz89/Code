using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class CremationOperationalCommentDto
{
    public Guid Id { get; init; }

    public Guid CremationId { get; init; }

    public CremationStatus Status { get; init; }

    public string Comment { get; init; } = string.Empty;

    public Guid CreatedByUserId { get; init; }

    public string CreatedByUserNameSnapshot { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
