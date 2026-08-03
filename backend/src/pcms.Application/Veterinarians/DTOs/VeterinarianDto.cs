namespace pcms.Application.Veterinarians.DTOs;

public class VeterinarianDto
{
    public Guid Id { get; set; }

    public Guid VeterinaryClinicId { get; set; }

    public string VeterinaryClinicName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ProfessionalLicenseNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}