namespace pcms.Application.Veterinarians.DTOs;

public class CreateVeterinarianDto
{
    public Guid VeterinaryClinicId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ProfessionalLicenseNumber { get; set; }
}