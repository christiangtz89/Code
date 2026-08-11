using System.ComponentModel.DataAnnotations;
using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.DTOs;

public class ChangeVeterinaryRequestStatusDto
{
    [EnumDataType(
        typeof(VeterinaryRequestStatus),
        ErrorMessage =
            "El estado de la solicitud no es válido.")]
    public VeterinaryRequestStatus Status { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Las notas internas no pueden exceder 1000 caracteres.")]
    public string? InternalNotes { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "El motivo de rechazo no puede exceder 1000 caracteres.")]
    public string? RejectionReason { get; set; }
}