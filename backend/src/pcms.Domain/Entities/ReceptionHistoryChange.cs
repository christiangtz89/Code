using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class ReceptionHistoryChange
{
    public Guid Id { get; set; }

    public Guid ReceptionHistoryEventId { get; set; }

    public ReceptionHistoryField Field { get; set; }

    public string? OriginalValue { get; set; }

    public string? NewValue { get; set; }

    public string? OriginalDisplayValue { get; set; }

    public string? NewDisplayValue { get; set; }

    public ReceptionHistoryEvent ReceptionHistoryEvent { get; set; } = null!;
}
