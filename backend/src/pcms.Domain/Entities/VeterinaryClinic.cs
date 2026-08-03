namespace pcms.Domain.Entities;

public class VeterinaryClinic
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PrimaryContactName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Veterinarian> Veterinarians { get; set; }
        = new List<Veterinarian>();

    public ICollection<Reception> ReferredReceptions { get; set; }
    = new List<Reception>();
}