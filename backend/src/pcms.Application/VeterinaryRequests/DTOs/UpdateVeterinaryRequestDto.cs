using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.DTOs;

public class UpdateVeterinaryRequestDto
{
    public Guid? VeterinaryClinicId { get; set; }

    public Guid? ReferringVeterinarianId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string OwnerFirstName { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string OwnerLastName { get; set; }
        = string.Empty;

    [StringLength(100)]
    public string? OwnerSecondLastName { get; set; }

    [Required]
    [StringLength(25, MinimumLength = 7)]
    public string OwnerPhone { get; set; }
        = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? OwnerEmail { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string PetName { get; set; }
        = string.Empty;

    [Required]
    [StringLength(50)]
    public string Species { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100)]
    public string Breed { get; set; }
        = string.Empty;

    [Required]
    [StringLength(20)]
    public string Sex { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100)]
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

    public DateOnly DateOfDeath { get; set; }

    [EnumDataType(typeof(CremationType))]
    public CremationType? RequestedCremationType { get; set; }

    [StringLength(150)]
    public string? RequestedPackageName { get; set; }

    [StringLength(1000)]
    public string? RequestNotes { get; set; }
}
