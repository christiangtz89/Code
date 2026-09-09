using System.ComponentModel.DataAnnotations;
using pcms.Application.Common;

namespace pcms.Application.Veterinarians.DTOs;

public class UpdateVeterinarianDto
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _secondLastName;
    private string? _phone;
    private string? _email;
    private string? _professionalLicenseNumber;

    public Guid? VeterinaryClinicId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName
    {
        get => _firstName;
        set => _firstName = TextInputNormalization.Required(value);
    }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName
    {
        get => _lastName;
        set => _lastName = TextInputNormalization.Required(value);
    }

    [StringLength(100)]
    public string? SecondLastName
    {
        get => _secondLastName;
        set => _secondLastName = TextInputNormalization.Optional(value);
    }

    [StringLength(25)]
    [RegularExpression(@"^[0-9+\-\s()]+$")]
    public string? Phone
    {
        get => _phone;
        set => _phone = TextInputNormalization.Optional(value);
    }

    [EmailAddress]
    [StringLength(150)]
    public string? Email
    {
        get => _email;
        set => _email = TextInputNormalization.Optional(value);
    }

    [StringLength(50)]
    public string? ProfessionalLicenseNumber
    {
        get => _professionalLicenseNumber;
        set => _professionalLicenseNumber = TextInputNormalization.Optional(value);
    }
}
