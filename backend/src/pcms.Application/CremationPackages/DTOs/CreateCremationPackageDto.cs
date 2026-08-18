using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.CremationPackages.DTOs;

public class CreateCremationPackageDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? ShortDescription { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [EnumDataType(typeof(CremationPackageType))]
    public CremationPackageType PackageType { get; set; }
        = CremationPackageType.AshesReturn;

    [Range(1, 4)]
    public int? Tier { get; set; }

    public bool IncludesUrn { get; set; }

    public List<Guid> AllowedUrnIds { get; set; } = new();

    public bool IncludesPawPrint { get; set; }

    [MaxLength(500)]
    public string? AccessoryDescription { get; set; }

    public bool IncludesCertificate { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
