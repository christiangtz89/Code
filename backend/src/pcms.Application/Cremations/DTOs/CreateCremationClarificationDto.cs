using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class CreateCremationClarificationDto
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "La aclaración es obligatoria.")]
    [StringLength(1000, ErrorMessage = "La aclaración no puede exceder 1000 caracteres.")]
    public string Text { get; set; } = string.Empty;
}
