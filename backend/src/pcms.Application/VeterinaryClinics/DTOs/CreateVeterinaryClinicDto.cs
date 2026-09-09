using System.ComponentModel.DataAnnotations;
using pcms.Application.Common;

namespace pcms.Application.VeterinaryClinics.DTOs;

public class CreateVeterinaryClinicDto
{
    private string _name = string.Empty;
    private string? _phone;
    private string? _email;
    private string? _address;
    private string? _primaryContactName;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name
    {
        get => _name;
        set => _name = TextInputNormalization.Required(value);
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

    [StringLength(300)]
    public string? Address
    {
        get => _address;
        set => _address = TextInputNormalization.Optional(value);
    }

    [StringLength(150)]
    public string? PrimaryContactName
    {
        get => _primaryContactName;
        set => _primaryContactName = TextInputNormalization.Optional(value);
    }
}
