using pcms.Domain.Enums;

namespace pcms.Application.Collections.DTOs;

public class CollectionDto
{
    public Guid Id { get; set; }

    public Guid PetId { get; set; }

    public string PetName { get; set; }
        = string.Empty;

    public string PetSpecies { get; set; }
        = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }
        = string.Empty;

    public string CustomerPhone { get; set; }
        = string.Empty;

    public Guid? CollectedByUserId { get; set; }

    public string? CollectedByUserName { get; set; }

    public Guid? AssignedDriverId { get; set; }

    public string? AssignedDriverName { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public string? AssignedByUserName { get; set; }

    public DateTime? AssignedAt { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public string? AcceptedByUserName { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public CollectionLocationType LocationType { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public string? VeterinaryClinicName { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public string? ReferringVeterinarianName { get; set; }

    public CollectionStatus Status { get; set; }

    public string QrCode { get; set; }
        = string.Empty;

    public string PickupAddress { get; set; }
        = string.Empty;

    public string? PickupContactName { get; set; }

    public string? PickupContactPhone { get; set; }

    public decimal? ApproximateWeightKg { get; set; }

    public bool HasPersonalBelongings { get; set; }

    public string? PersonalBelongingsDescription { get; set; }

    public string? Notes { get; set; }

    public Guid? ReceptionId { get; set; }

    public string? ReceptionQrCode { get; set; }

    public DateTime? CollectedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
