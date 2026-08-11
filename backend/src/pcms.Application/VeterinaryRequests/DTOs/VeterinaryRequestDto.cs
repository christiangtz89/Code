using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.DTOs;

public class VeterinaryRequestDto
{
    public Guid Id { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public string? VeterinaryClinicName { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    public string? ReferringVeterinarianName { get; set; }

    public Guid SubmittedByUserId { get; set; }

    public string SubmittedByUserName { get; set; }
        = string.Empty;

    public Guid? ReviewedByUserId { get; set; }

    public string? ReviewedByUserName { get; set; }

    public Guid? ReceptionId { get; set; }

    public string? ReceptionQrCode { get; set; }

    public VeterinaryRequestStatus Status { get; set; }

    public string OwnerFirstName { get; set; }
        = string.Empty;

    public string OwnerLastName { get; set; }
        = string.Empty;

    public string? OwnerSecondLastName { get; set; }

    public string OwnerPhone { get; set; }
        = string.Empty;

    public string? OwnerEmail { get; set; }

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

    public DateTime DateOfDeath { get; set; }

    public CremationType? RequestedCremationType { get; set; }

    public string? RequestedPackageName { get; set; }

    public string? RequestNotes { get; set; }

    public string? InternalNotes { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? ConvertedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}