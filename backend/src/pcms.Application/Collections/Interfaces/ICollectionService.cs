using pcms.Application.Collections.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.Interfaces;

public interface ICollectionService
{
    Task<CollectionDto> CreateAsync(
        CreateCollectionDto dto,
        Guid createdByUserId);

    Task<PagedCollectionsDto> GetAllAsync(
        int page,
        int pageSize,
        CollectionStatus? status,
        CollectionLocationType? locationType);

    Task<CollectionDto?> GetByIdAsync(
        Guid id);

    Task<CollectionDto?> GetByQrCodeAsync(
        string qrCode);

    Task<CollectionDto?> UpdateAsync(
        Guid id,
        UpdateCollectionDto dto);

    Task<CollectionDto?> ChangeStatusAsync(
        Guid id,
        ChangeCollectionStatusDto dto,
        Guid actorUserId);

    Task<IEnumerable<CollectionDriverOptionDto>>
        GetActiveDriverOptionsAsync();

    Task<CollectionDto?> AssignAsync(
        Guid id,
        AssignCollectionDto dto,
        Guid assignedByUserId);

    Task<CollectionDto?> AcceptAsync(
        Guid id,
        Guid actorUserId);

    Task<CollectionDto?> ConfirmCustodyAsync(
        Guid id,
        Guid actorUserId);

    Task<CollectionDto?> ConvertToReceptionAsync(
        Guid id,
        ConvertCollectionToReceptionDto dto,
        Guid receivedByUserId);

    Task<IEnumerable<CollectionDto>> SearchAsync(
        string search,
        CollectionStatus? status,
        CollectionLocationType? locationType);
}
