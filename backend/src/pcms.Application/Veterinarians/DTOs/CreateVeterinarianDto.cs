using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Veterinarians.DTOs;

public class CreateVeterinarianDto
{
    public Guid? VeterinaryClinicId { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; }
        = string.Empty;

    [StringLength(100)]
    public string? SecondLastName { get; set; }

    [StringLength(25)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? ProfessionalLicenseNumber { get; set; }
}