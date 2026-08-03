namespace pcms.Application.Receptions.DTOs;

public class ReceptionDto
{
    public Guid Id { get; set; }

    public Guid PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid ReceivedByUserId { get; set; }

    public string ReceivedByUserName { get; set; } = string.Empty;

    public Guid? VeterinaryClinicId { get; set; }

    public string? VeterinaryClinicName { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public string? ReferringVeterinarianName { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public decimal VerifiedWeightKg { get; set; }

    public bool HasPersonalBelongings { get; set; }

    public string? PersonalBelongingsDescription { get; set; }

    public string? ReferralNotes { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}