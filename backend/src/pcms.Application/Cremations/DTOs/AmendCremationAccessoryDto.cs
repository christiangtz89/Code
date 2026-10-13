using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class AmendCremationAccessoryDto
{
    public Guid RequestId { get; set; }

    [StringLength(500, ErrorMessage = "La descripción del accesorio no puede exceder 500 caracteres.")]
    public string? NewAccessoryDescription { get; set; }

    [StringLength(500, ErrorMessage = "La descripción del accesorio no puede exceder 500 caracteres.")]
    public string? ExpectedCurrentAccessoryDescription { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(1000, ErrorMessage = "El motivo no debe exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
