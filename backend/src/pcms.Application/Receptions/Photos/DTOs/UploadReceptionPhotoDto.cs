using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Receptions.Photos.DTOs;

public class UploadReceptionPhotoDto
{
    [EnumDataType(
        typeof(ReceptionPhotoType),
        ErrorMessage = "Debe seleccionar un tipo de fotografía válido.")]
    public ReceptionPhotoType PhotoType { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}