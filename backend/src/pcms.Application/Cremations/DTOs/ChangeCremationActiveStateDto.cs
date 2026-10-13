using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class ChangeCremationActiveStateDto
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(1000, ErrorMessage = "El motivo no debe exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
