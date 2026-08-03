using System.ComponentModel.DataAnnotations;

namespace pcms.Application.Pets.DTOs;

public class UpdatePetDto
{
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(
        50,
        ErrorMessage = "La especie no puede exceder 50 caracteres.")]
    public string Species { get; set; } = string.Empty;

    [Required(ErrorMessage = "La raza es obligatoria.")]
    [StringLength(
        100,
        ErrorMessage = "La raza no puede exceder 100 caracteres.")]
    public string Breed { get; set; } = string.Empty;

    [Required(ErrorMessage = "El sexo es obligatorio.")]
    [StringLength(
        20,
        ErrorMessage = "El sexo no puede exceder 20 caracteres.")]
    public string Sex { get; set; } = string.Empty;

    [Required(ErrorMessage = "El color es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage = "El color no puede exceder 100 caracteres.")]
    public string Color { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "999.99",
        ErrorMessage = "El peso debe ser mayor que cero.")]
    public decimal WeightKg { get; set; }

    [Range(
        0,
        100,
        ErrorMessage = "La edad debe estar entre 0 y 100 años.")]
    public int? AgeYears { get; set; }

    public DateTime DateOfDeath { get; set; }
}