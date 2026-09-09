namespace pcms.Application.Veterinarians.DTOs;

public class VeterinarianClinicOptionDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
