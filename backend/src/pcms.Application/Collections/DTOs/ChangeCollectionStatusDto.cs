using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.DTOs;

public class ChangeCollectionStatusDto
{
    [EnumDataType(
        typeof(CollectionStatus),
        ErrorMessage =
            "El estado de la recolección no es válido.")]
    public CollectionStatus Status { get; set; }
}