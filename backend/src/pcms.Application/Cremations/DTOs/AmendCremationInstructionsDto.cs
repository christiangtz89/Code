using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class AmendCremationInstructionsDto
{
    public Guid RequestId { get; set; }

    [StringLength(1000, ErrorMessage = "Las instrucciones no pueden exceder 1000 caracteres.")]
    public string? NewSpecialInstructions { get; set; }

    [StringLength(1000, ErrorMessage = "Las instrucciones no pueden exceder 1000 caracteres.")]
    public string? ExpectedCurrentSpecialInstructions { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(1000, ErrorMessage = "El motivo no debe exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
