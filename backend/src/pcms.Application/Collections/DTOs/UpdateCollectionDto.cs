using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.DTOs;

public class UpdateCollectionDto
{
    [EnumDataType(
        typeof(CollectionLocationType),
        ErrorMessage =
            "El tipo de ubicación de recolección no es válido.")]
    public CollectionLocationType LocationType { get; set; }

    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    [Required(
        ErrorMessage =
            "La dirección de recolección es obligatoria.")]
    [StringLength(
        500,
        ErrorMessage =
            "La dirección de recolección no puede exceder 500 caracteres.")]
    public string PickupAddress { get; set; }
        = string.Empty;

    [StringLength(
        150,
        ErrorMessage =
            "El nombre del contacto no puede exceder 150 caracteres.")]
    public string? PickupContactName { get; set; }

    [StringLength(
        30,
        ErrorMessage =
            "El teléfono del contacto no puede exceder 30 caracteres.")]
    public string? PickupContactPhone { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "999.99",
        ErrorMessage =
            "El peso aproximado debe ser mayor que cero.")]
    public decimal? ApproximateWeightKg { get; set; }

    public bool HasPersonalBelongings { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "La descripción de los objetos personales no puede exceder 500 caracteres.")]
    public string? PersonalBelongingsDescription { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}