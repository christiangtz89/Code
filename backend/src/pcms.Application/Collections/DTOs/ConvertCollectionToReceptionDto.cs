using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Collections.DTOs;

public class ConvertCollectionToReceptionDto
{
    [Range(
        typeof(decimal),
        "0.01",
        "999.99",
        ErrorMessage =
            "El peso verificado debe ser mayor que cero.")]
    public decimal VerifiedWeightKg { get; set; }

    public bool HasPersonalBelongings { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "La descripción de los objetos personales no puede exceder 500 caracteres.")]
    public string? PersonalBelongingsDescription { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas de referencia no pueden exceder 1000 caracteres.")]
    public string? ReferralNotes { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas de recepción no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}