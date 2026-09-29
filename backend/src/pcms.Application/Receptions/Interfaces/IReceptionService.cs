using pcms.Application.Receptions.DTOs;

namespace pcms.Application.Receptions.Interfaces;

public interface IReceptionService
{
    Task<ReceptionDto> CreateAsync(
        CreateReceptionDto dto,
        Guid receivedByUserId);

    Task<PagedReceptionsDto> GetAllAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        Guid? actorUserId = null);

    Task<ReceptionDto?> GetByIdAsync(
        Guid id,
        bool includeInactive = false,
        Guid? actorUserId = null);

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

    Task<ReceptionLifecycleDecisionDto?> DeactivateAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId);

    Task<ReceptionLifecycleDecisionDto?> RestoreAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId);

    Task<ReceptionLifecycleDecisionDto?> RequestDeactivationAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId);

    Task<IReadOnlyList<ReceptionLifecycleEventDto>?>
        GetLifecycleHistoryAsync(Guid id);

    Task<IEnumerable<ReceptionDto>> SearchAsync(string search);
}
