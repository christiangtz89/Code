namespace pcms.Application.Urns.DTOs;

public class UrnDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Material { get; set; }

    public string? Color { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsPublic { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}