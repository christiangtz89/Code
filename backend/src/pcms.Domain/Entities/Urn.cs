namespace pcms.Domain.Entities;

public class Urn
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Material { get; set; }

    public string? Color { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; } = true;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<CremationPackageUrn> PackageOptions { get; set; }
    = new List<CremationPackageUrn>();

    public ICollection<Cremation> Cremations { get; set; }
        = new List<Cremation>();
}