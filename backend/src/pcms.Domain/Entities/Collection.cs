using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class Collection
{
    public Guid Id { get; set; }

    public Guid PetId { get; set; }

    public Guid CollectedByUserId { get; set; }

    public CollectionLocationType LocationType { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public CollectionStatus Status { get; set; }
        = CollectionStatus.Collected;

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

    public DateTime CollectedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool IsActive { get; set; }
        = true;

    public DateTime CreatedAt { get; set; }

    public Pet Pet { get; set; } = null!;

    public User CollectedByUser { get; set; } = null!;

    public VeterinaryClinic? VeterinaryClinic { get; set; }

    public Veterinarian? ReferringVeterinarian { get; set; }

    public Reception? Reception { get; set; }

    public ICollection<CollectionPhoto> Photos { get; set; }
        = new List<CollectionPhoto>();
}