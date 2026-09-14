using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class Collection
{
    public Guid Id { get; set; }

    public Guid PetId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string CustomerNameSnapshot { get; set; } = string.Empty;

    public string? CustomerPhoneSnapshot { get; set; }

    public string PetNameSnapshot { get; set; } = string.Empty;

    public string PetSpeciesSnapshot { get; set; } = string.Empty;

    public string? VeterinaryClinicNameSnapshot { get; set; }

    public string? ReferringVeterinarianNameSnapshot { get; set; }

    public string? CreatedByUserNameSnapshot { get; set; }

    public Guid? CollectedByUserId { get; set; }

    public string? CollectedByUserNameSnapshot { get; set; }

    public Guid? AssignedDriverId { get; set; }

    public string? AssignedDriverNameSnapshot { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public string? AssignedByUserNameSnapshot { get; set; }

    public DateTime? AssignedAt { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public string? AcceptedByUserNameSnapshot { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public CollectionLocationType LocationType { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public CollectionStatus Status { get; set; }
        = CollectionStatus.Pending;

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

    public DateTime? CollectedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public Guid? ReceivedByUserId { get; set; }

    public string? ReceivedByUserNameSnapshot { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public string? CancelledByUserNameSnapshot { get; set; }

    public bool IsActive { get; set; }
        = true;

    public DateTime CreatedAt { get; set; }

    public Pet Pet { get; set; } = null!;

    public User? CreatedByUser { get; set; }

    public User? CollectedByUser { get; set; }

    public User? AssignedDriver { get; set; }

    public User? AssignedByUser { get; set; }

    public User? AcceptedByUser { get; set; }

    public User? CancelledByUser { get; set; }

    public User? ReceivedByUser { get; set; }

    public VeterinaryClinic? VeterinaryClinic { get; set; }

    public Veterinarian? ReferringVeterinarian { get; set; }

    public Reception? Reception { get; set; }

    public ICollection<CollectionPhoto> Photos { get; set; }
        = new List<CollectionPhoto>();

    public ICollection<CollectionAssignmentHistory> AssignmentHistory { get; set; }
        = new List<CollectionAssignmentHistory>();
}
