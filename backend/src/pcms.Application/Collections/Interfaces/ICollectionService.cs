using pcms.Application.Collections.DTOs;
using pcms.Application.Common;
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

    Task<PaginatedResult<CollectionCustomerOptionDto>>
        GetCustomerOptionsAsync(
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<CollectionPetOptionDto>>
        GetPetOptionsAsync(
            Guid customerId,
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<CollectionVeterinaryClinicOptionDto>>
        GetVeterinaryClinicOptionsAsync(
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<CollectionVeterinarianOptionDto>>
        GetVeterinarianOptionsAsync(
            Guid? veterinaryClinicId,
            string? search,
            int page,
            int pageSize);

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

    Task<PagedCollectionsDto> SearchAsync(
        string search,
        int page,
        int pageSize,
        CollectionStatus? status,
        CollectionLocationType? locationType);
}
