namespace pcms.Application.Receptions.DTOs;

public sealed class ReceptionLifecycleDecisionDto
{
    public ReceptionLifecycleEventDto Event { get; set; } = null!;

    public bool IsReplay { get; set; }

    public bool IsBlocked { get; set; }

    public string Message { get; set; } = string.Empty;
}
