using pcms.Application.Receptions.DTOs;

namespace pcms.Application.Receptions.Interfaces;

public interface IReceptionService
{
    Task<ReceptionDto> CreateAsync(
        CreateReceptionDto dto,
        Guid receivedByUserId);

    Task<PagedReceptionsDto> GetAllAsync(
        int page,
        int pageSize);

    Task<ReceptionDto?> GetByIdAsync(Guid id);

    Task<ReceptionDto?> GetByQrCodeAsync(string qrCode);

    Task<ReceptionDto?> UpdateAsync(
        Guid id,
        UpdateReceptionDto dto,
        Guid actorUserId);

    Task<ReceptionHistoryEventDto?> CreateCorrectionAsync(
        Guid id,
        CreateReceptionCorrectionDto dto,
        Guid actorUserId);

    Task<ReceptionHistoryEventDto?> CreateClarificationAsync(
        Guid id,
        CreateReceptionClarificationDto dto,
        Guid actorUserId);

    Task<IReadOnlyList<ReceptionHistoryEventDto>?> GetHistoryAsync(
        Guid id);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<ReceptionDto>> SearchAsync(string search);
}
