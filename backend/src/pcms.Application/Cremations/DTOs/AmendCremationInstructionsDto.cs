using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class AmendCremationInstructionsDto
{
    public Guid RequestId { get; set; }

    [StringLength(1000)]
    public string? NewSpecialInstructions { get; set; }

    [StringLength(1000)]
    public string? ExpectedCurrentSpecialInstructions { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
