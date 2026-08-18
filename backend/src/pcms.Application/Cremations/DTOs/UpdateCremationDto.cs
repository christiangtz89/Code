using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Cremations.DTOs;

public class UpdateCremationDto
{
    public Guid? AssignedToUserId { get; set; }

    public Guid CremationPackageId { get; set; }

    public Guid? UrnId { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "La descripción del accesorio no puede exceder 500 caracteres.")]
    public string? AccessoryDescription { get; set; }

    public DateTime? ScheduledAt { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las instrucciones especiales no pueden exceder 1000 caracteres.")]
    public string? SpecialInstructions { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}