namespace pcms.Domain.Entities;

public class Veterinarian
{
    public Guid Id { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public VeterinaryClinic? VeterinaryClinic { get; set; }

    public string FirstName { get; set; }
        = string.Empty;

    // Apellido paterno
    public string LastName { get; set; }
        = string.Empty;

    // Apellido materno — opcional
    public string? SecondLastName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ProfessionalLicenseNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Reception> ReferredReceptions { get; set; }
        = new List<Reception>();
}