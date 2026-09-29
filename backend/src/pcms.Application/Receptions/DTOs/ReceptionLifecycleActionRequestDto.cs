namespace pcms.Application.Receptions.DTOs;

public sealed class ReceptionLifecycleActionRequestDto
{
    public Guid RequestId { get; set; }

    public string? Reason { get; set; }
}
