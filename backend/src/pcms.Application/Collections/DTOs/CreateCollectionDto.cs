using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.DTOs;

public class CreateCollectionDto
{
    public Guid? ExistingCustomerId { get; set; }

    public Guid? ExistingPetId { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "El nombre del propietario no puede exceder 100 caracteres.")]
    public string? OwnerFirstName { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "El apellido paterno del propietario no puede exceder 100 caracteres.")]
    public string? OwnerLastName { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "El apellido materno del propietario no puede exceder 100 caracteres.")]
    public string? OwnerSecondLastName { get; set; }

    [StringLength(
        30,
        ErrorMessage =
            "El teléfono del propietario no puede exceder 30 caracteres.")]
    public string? OwnerPhone { get; set; }

    [EmailAddress(
        ErrorMessage =
            "El correo electrónico del propietario no es válido.")]
    [StringLength(
        150,
        ErrorMessage =
            "El correo electrónico del propietario no puede exceder 150 caracteres.")]
    public string? OwnerEmail { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "El nombre de la mascota no puede exceder 100 caracteres.")]
    public string? PetName { get; set; }

    [StringLength(
        50,
        ErrorMessage =
            "La especie no puede exceder 50 caracteres.")]
    public string? Species { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "La raza no puede exceder 100 caracteres.")]
    public string? Breed { get; set; }

    [StringLength(
        20,
        ErrorMessage =
            "El sexo no puede exceder 20 caracteres.")]
    public string? Sex { get; set; }

    [StringLength(
        100,
        ErrorMessage =
            "El color no puede exceder 100 caracteres.")]
    public string? Color { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "999.99",
        ErrorMessage =
            "El peso aproximado debe ser mayor que cero.")]
    public decimal? ApproximateWeightKg { get; set; }

    [Range(
        0,
        100,
        ErrorMessage =
            "La edad debe estar entre 0 y 100 años.")]
    public int? AgeYears { get; set; }

    public DateTime? DateOfDeath { get; set; }

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