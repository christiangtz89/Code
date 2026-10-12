using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class ChangeCremationActiveStateDto
{
    public Guid RequestId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
