using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pcms.Application.Receptions.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateReceptionClarificationDto
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "El texto de la aclaración es obligatorio.")]
    [StringLength(
        1000,
        ErrorMessage =
            "El texto de la aclaración no puede exceder 1000 caracteres.")]
    public string Text { get; set; } = string.Empty;
}
