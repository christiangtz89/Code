using pcms.Domain.Enums;

namespace pcms.Application.Receptions.DTOs;

public sealed class ReceptionHistoryEventDto
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid? CremationId { get; set; }

    public Guid? RequestId { get; set; }

    public long Sequence { get; set; }

    public ReceptionHistoryEventKind EventKind { get; set; }

    public ReceptionHistoryStage Stage { get; set; }

    public CremationStatus? CremationStatus { get; set; }

    public string? Reason { get; set; }

    public string? ClarificationText { get; set; }

    public Guid ActorUserId { get; set; }

    public string ActorUserName { get; set; } = string.Empty;

    public string ActorRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<ReceptionHistoryChangeDto> Changes { get; set; }
        = [];
}

public sealed class ReceptionHistoryChangeDto
{
    public ReceptionHistoryField Field { get; set; }

    public string? OriginalValue { get; set; }

    public string? NewValue { get; set; }

    public string? OriginalDisplayValue { get; set; }

    public string? NewDisplayValue { get; set; }
}
