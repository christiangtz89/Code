namespace pcms.Domain.Entities;

public class Veterinarian
{
    public Guid Id { get; set; }

    public Guid VeterinaryClinicId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ProfessionalLicenseNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public VeterinaryClinic VeterinaryClinic { get; set; } = null!;

    public ICollection<Reception> ReferredReceptions { get; set; }
    = new List<Reception>();
}