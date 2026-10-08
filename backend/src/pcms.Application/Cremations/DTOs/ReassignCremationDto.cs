using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class ReassignCremationDto
{
    public Guid RequestId { get; set; }
    public Guid NewAssignedToUserId { get; set; }

    [Required(ErrorMessage = "El motivo de la reasignación es obligatorio.")]
    [MaxLength(1000, ErrorMessage = "El motivo no debe exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
