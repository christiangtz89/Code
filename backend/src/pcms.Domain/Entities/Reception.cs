namespace pcms.Domain.Entities;

public class Reception
{
    public Guid Id { get; set; }

    public Guid PetId { get; set; }

    public string PetNameSnapshot { get; set; } = string.Empty;

    public string CustomerNameSnapshot { get; set; } = string.Empty;

    public Guid ReceivedByUserId { get; set; }

    public Guid? CollectionId { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public decimal VerifiedWeightKg { get; set; }

    public bool HasPersonalBelongings { get; set; }

    public string? PersonalBelongingsDescription { get; set; }

    public string? ReferralNotes { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Pet Pet { get; set; } = null!;

    public User ReceivedByUser { get; set; } = null!;

    public Collection? Collection { get; set; }

    public VeterinaryClinic? VeterinaryClinic { get; set; }

    public Veterinarian? ReferringVeterinarian { get; set; }

    public VeterinaryRequest? VeterinaryRequest { get; set; }

    public Cremation? Cremation { get; set; }

    public ICollection<ReceptionPhoto> Photos { get; set; }
        = new List<ReceptionPhoto>();
}
