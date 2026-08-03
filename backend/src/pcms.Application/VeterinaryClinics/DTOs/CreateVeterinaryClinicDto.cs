namespace pcms.Application.VeterinaryClinics.DTOs;

public class CreateVeterinaryClinicDto
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PrimaryContactName { get; set; }
}