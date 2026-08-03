using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class Cremation
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public CremationType CremationType { get; set; }

    public CremationStatus Status { get; set; }
        = CremationStatus.Pending;

    public string PackageName { get; set; } = string.Empty;

    public bool IncludesUrn { get; set; }

    public string? UrnDescription { get; set; }

    public bool IncludesPawPrint { get; set; }

    public bool IncludesCertificate { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? ReadyForDeliveryAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string? SpecialInstructions { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Reception Reception { get; set; } = null!;

    public User? AssignedToUser { get; set; }
}