using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Urns.DTOs;

public class CreateUrnDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Material { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
