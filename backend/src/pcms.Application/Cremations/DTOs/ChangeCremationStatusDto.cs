using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Cremations.DTOs;

public class ChangeCremationStatusDto
{
    [EnumDataType(
        typeof(CremationStatus),
        ErrorMessage = "El estado de cremación no es válido.")]
    public CremationStatus Status { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas del cambio de estado no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}