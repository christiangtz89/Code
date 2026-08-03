using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class CremationDto
{
    public Guid Id { get; set; }

    public Guid ReceptionId { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public Guid PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid? AssignedToUserId { get; set; }

    public string? AssignedToUserName { get; set; }

    public CremationType CremationType { get; set; }

    public CremationStatus Status { get; set; }

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

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}