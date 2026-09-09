using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.DTOs;

public class VeterinaryRequestListItemDto
{
    public Guid Id { get; set; }

    public string? VeterinaryClinicName { get; set; }

    public string? ReferringVeterinarianName { get; set; }

    public VeterinaryRequestStatus Status { get; set; }

    public string OwnerFirstName { get; set; } = string.Empty;

    public string OwnerLastName { get; set; } = string.Empty;

    public string? OwnerSecondLastName { get; set; }

    public string OwnerPhone { get; set; } = string.Empty;

    public string PetName { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public decimal ApproximateWeightKg { get; set; }

    public DateTime SubmittedAt { get; set; }
}
