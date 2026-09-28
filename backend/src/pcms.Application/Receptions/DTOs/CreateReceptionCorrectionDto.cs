using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pcms.Application.Receptions.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateReceptionCorrectionDto
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "El motivo de la corrección es obligatorio.")]
    [StringLength(
        1000,
        ErrorMessage =
            "El motivo de la corrección no puede exceder 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;

    public CorrectionValueDto<decimal>? VerifiedWeightKg { get; set; }

    public CorrectionValueDto<Guid?>? VeterinaryClinicId { get; set; }

    public CorrectionValueDto<Guid?>? ReferringVeterinarianId { get; set; }

    public CorrectionValueDto<bool>? HasPersonalBelongings { get; set; }

    public CorrectionValueDto<string?>? PersonalBelongingsDescription
        { get; set; }

    public CorrectionValueDto<string?>? ReferralNotes { get; set; }

    public bool ConfirmWeightRangeChange { get; set; }

    public Guid? ExpectedCremationPriceId { get; set; }

    public decimal? ExpectedNewPrice { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CorrectionValueDto<T>
{
    [JsonRequired]
    public required T Value { get; set; }
}
