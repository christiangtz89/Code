using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class UpdateCremationDto
{
    public Guid? AssignedToUserId { get; set; }

    [EnumDataType(
        typeof(CremationType),
        ErrorMessage = "El tipo de cremación no es válido.")]
    public CremationType CremationType { get; set; }

    [Required(
        ErrorMessage = "El nombre del paquete es obligatorio.")]
    [StringLength(
        150,
        ErrorMessage =
            "El nombre del paquete no puede exceder 150 caracteres.")]
    public string PackageName { get; set; } = string.Empty;

    public bool IncludesUrn { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "La descripción de la urna no puede exceder 500 caracteres.")]
    public string? UrnDescription { get; set; }

    public bool IncludesPawPrint { get; set; }

    public bool IncludesCertificate { get; set; }

    public DateTime? ScheduledAt { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las instrucciones especiales no pueden exceder 1000 caracteres.")]
    public string? SpecialInstructions { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}