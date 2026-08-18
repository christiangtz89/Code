using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class CremationPackage
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public CremationPackageType PackageType { get; set; }
        = CremationPackageType.AshesReturn;

    public int? Tier { get; set; }

    public bool IncludesUrn { get; set; }

    // Backend name kept for compatibility.
    // Frontend label: "Incluye accesorio".
    public bool IncludesPawPrint { get; set; }

    public string? AccessoryDescription { get; set; }

    public bool IncludesCertificate { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; } = true;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<CremationPrice> Prices { get; set; }
    = new List<CremationPrice>();

    public ICollection<CremationPackageUrn> UrnOptions { get; set; }
    = new List<CremationPackageUrn>();

    public ICollection<Cremation> Cremations { get; set; }
        = new List<Cremation>();

}