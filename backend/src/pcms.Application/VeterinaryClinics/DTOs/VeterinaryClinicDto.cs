namespace pcms.Application.VeterinaryClinics.DTOs;

public class VeterinaryClinicDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PrimaryContactName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}