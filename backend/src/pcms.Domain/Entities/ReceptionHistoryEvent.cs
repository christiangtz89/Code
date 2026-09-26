using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class ReceptionHistoryEvent
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid? CremationId { get; set; }

    public Guid? RequestId { get; set; }

    public long SequenceNumber { get; set; }

    public ReceptionHistoryEventKind EventKind { get; set; }

    public ReceptionHistoryStage ReceptionStage { get; set; }

    public CremationStatus? CremationStatusSnapshot { get; set; }

    public string? Reason { get; set; }

    public string? ClarificationText { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string CreatedByUserNameSnapshot { get; set; } = string.Empty;

    public string CreatedByRoleSnapshot { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Reception Reception { get; set; } = null!;

    public Cremation? Cremation { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<ReceptionHistoryChange> Changes { get; set; }
        = new List<ReceptionHistoryChange>();
}
