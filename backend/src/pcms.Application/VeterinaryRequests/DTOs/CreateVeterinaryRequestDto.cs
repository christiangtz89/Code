using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.DTOs;

public class CreateVeterinaryRequestDto
{
    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    [Required(
        ErrorMessage =
            "El nombre del propietario es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage =
            "El nombre del propietario no puede exceder 100 caracteres.")]
    public string OwnerFirstName { get; set; }
        = string.Empty;

    [Required(
        ErrorMessage =
            "El apellido paterno del propietario es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage =
            "El apellido paterno no puede exceder 100 caracteres.")]
    public string OwnerLastName { get; set; }
        = string.Empty;

    [StringLength(
        100,
        ErrorMessage =
            "El apellido materno no puede exceder 100 caracteres.")]
    public string? OwnerSecondLastName { get; set; }

    [Required(
        ErrorMessage =
            "El teléfono del propietario es obligatorio.")]
    [StringLength(
        30,
        ErrorMessage =
            "El teléfono no puede exceder 30 caracteres.")]
    public string OwnerPhone { get; set; }
        = string.Empty;

    [EmailAddress(
        ErrorMessage =
            "El correo electrónico no es válido.")]
    [StringLength(
        150,
        ErrorMessage =
            "El correo electrónico no puede exceder 150 caracteres.")]
    public string? OwnerEmail { get; set; }

    [Required(
        ErrorMessage =
            "El nombre de la mascota es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage =
            "El nombre de la mascota debe tener entre 2 y 100 caracteres.")]
    public string PetName { get; set; }
        = string.Empty;

    [Required(
        ErrorMessage =
            "La especie es obligatoria.")]
    [StringLength(
        50,
        ErrorMessage =
            "La especie no puede exceder 50 caracteres.")]
    public string Species { get; set; }
        = string.Empty;

    [Required(
        ErrorMessage =
            "La raza es obligatoria.")]
    [StringLength(
        100,
        ErrorMessage =
            "La raza no puede exceder 100 caracteres.")]
    public string Breed { get; set; }
        = string.Empty;

    [Required(
        ErrorMessage =
            "El sexo es obligatorio.")]
    [StringLength(
        20,
        ErrorMessage =
            "El sexo no puede exceder 20 caracteres.")]
    public string Sex { get; set; }
        = string.Empty;

    [Required(
        ErrorMessage =
            "El color es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage =
            "El color no puede exceder 100 caracteres.")]
    public string Color { get; set; }
        = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "999.99",
        ErrorMessage =
            "El peso aproximado debe ser mayor que cero.")]
    public decimal ApproximateWeightKg { get; set; }

    [Range(
        0,
        100,
        ErrorMessage =
            "La edad debe estar entre 0 y 100 años.")]
    public int? AgeYears { get; set; }

    public DateTime DateOfDeath { get; set; }

    [EnumDataType(
        typeof(CremationType),
        ErrorMessage =
            "El tipo de cremación solicitado no es válido.")]
    public CremationType? RequestedCremationType { get; set; }

    [StringLength(
        150,
        ErrorMessage =
            "El nombre del paquete solicitado no puede exceder 150 caracteres.")]
    public string? RequestedPackageName { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas de la solicitud no pueden exceder 1000 caracteres.")]
    public string? RequestNotes { get; set; }
}