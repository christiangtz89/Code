using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

// Historical commentary recorded only when a status transition is applied.
public class CremationOperationalComment
{
    public Guid Id { get; init; }

    public Guid CremationId { get; init; }

    public CremationStatus Status { get; init; }

    public string Comment { get; init; } = string.Empty;

    public Guid CreatedByUserId { get; init; }

    public string CreatedByUserNameSnapshot { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public Cremation Cremation { get; init; } = null!;

    public User CreatedByUser { get; init; } = null!;
}
