using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.Photos.DTOs;

public class UploadCollectionPhotoDto
{
    [EnumDataType(
        typeof(CollectionPhotoType),
        ErrorMessage =
            "Debe seleccionar un tipo de fotografía válido.")]
    public CollectionPhotoType PhotoType { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }
}