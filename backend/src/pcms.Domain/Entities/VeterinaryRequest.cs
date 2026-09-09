using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class VeterinaryRequest
{
    public Guid Id { get; set; }

    // Referral source
    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public string? VeterinaryClinicNameSnapshot { get; set; }

    public string? ReferringVeterinarianNameSnapshot { get; set; }

    // User who submitted/registered the request
    public Guid SubmittedByUserId { get; set; }

    // Internal employee who reviewed it
    public Guid? ReviewedByUserId { get; set; }

    // Set only after conversion into normal PCMS workflow
    public Guid? ReceptionId { get; set; }

    public VeterinaryRequestStatus Status { get; set; }
        = VeterinaryRequestStatus.Submitted;

    // Owner snapshot
    public string OwnerFirstName { get; set; }
        = string.Empty;

    // Apellido paterno
    public string OwnerLastName { get; set; }
        = string.Empty;

    // Apellido materno
    public string? OwnerSecondLastName { get; set; }

    public string OwnerPhone { get; set; }
        = string.Empty;

    public string? OwnerEmail { get; set; }

    // Pet snapshot
    public string PetName { get; set; }
        = string.Empty;

    public string Species { get; set; }
        = string.Empty;

    public string Breed { get; set; }
        = string.Empty;

    public string Sex { get; set; }
        = string.Empty;

    public string Color { get; set; }
        = string.Empty;

    public decimal ApproximateWeightKg { get; set; }

    public int? AgeYears { get; set; }

    public DateOnly DateOfDeath { get; set; }

    // Requested cremation information
    public CremationType? RequestedCremationType { get; set; }

    public string? RequestedPackageName { get; set; }

    public string? RequestNotes { get; set; }

    // Internal-only handling information
    public string? InternalNotes { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? ConvertedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public VeterinaryClinic? VeterinaryClinic { get; set; }

    public Veterinarian? ReferringVeterinarian { get; set; }

    public User SubmittedByUser { get; set; }
        = null!;

    public User? ReviewedByUser { get; set; }

    public Reception? Reception { get; set; }
}
