using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class AmendCremationAccessoryDto
{
    public Guid RequestId { get; set; }

    [StringLength(500)]
    public string? NewAccessoryDescription { get; set; }

    [StringLength(500)]
    public string? ExpectedCurrentAccessoryDescription { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
