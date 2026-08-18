using pcms.Domain.Enums;

namespace pcms.Application.CremationPackages.DTOs;

public class CremationPackageDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public CremationPackageType PackageType { get; set; }

    public int? Tier { get; set; }

    public bool IncludesUrn { get; set; }

    public List<Guid> AllowedUrnIds { get; set; } = new();

    public bool IncludesPawPrint { get; set; }

    public string? AccessoryDescription { get; set; }

    public bool IncludesCertificate { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
