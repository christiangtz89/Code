using pcms.Application.Cremations.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Cremations.Interfaces;

public interface ICremationService
{
    Task<CremationDto> CreateAsync(
        CreateCremationDto dto);

    Task<PagedCremationsDto> GetAllAsync(
    int page,
    int pageSize,
    bool isActive);

    Task<CremationDto?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<CremationOperationalCommentDto>?> GetOperationalCommentsAsync(
        Guid id);

    Task<CremationDto?> GetByReceptionIdAsync(
        Guid receptionId);

    Task<IEnumerable<CremationDto>> GetByStatusAsync(
        CremationStatus status);

    Task<CremationDto?> UpdateAsync(
        Guid id,
        UpdateCremationDto dto);

    Task<CremationDto?> ChangeStatusAsync(
        Guid id,
        ChangeCremationStatusDto dto,
        Guid actorUserId);

    Task<RevalidateStartPaymentResultDto?> RevalidateStartPaymentAsync(
        Guid id,
        RevalidateStartPaymentRequestDto dto,
        Guid actorUserId);

    Task<CremationDto?> ReassignAsync(Guid id, ReassignCremationDto dto, Guid actorUserId);

    Task<IReadOnlyList<CremationReassignmentDto>?> GetReassignmentsAsync(Guid id);

    Task<CremationDto?> AmendInstructionsAsync(Guid id, AmendCremationInstructionsDto dto, Guid actorUserId);

    Task<IReadOnlyList<CremationInstructionsAmendmentDto>?> GetInstructionsAmendmentsAsync(Guid id);

    Task<CremationDto?> AmendAccessoryAsync(Guid id, AmendCremationAccessoryDto dto, Guid actorUserId);

    Task<IReadOnlyList<CremationAccessoryAmendmentDto>?> GetAccessoryAmendmentsAsync(Guid id);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<CremationDto>> SearchAsync(
    string search,
    bool isActive);

    Task<IEnumerable<CremationReceptionOptionDto>>
    GetAvailableReceptionOptionsAsync();

    Task<IEnumerable<CremationUserOptionDto>>
        GetActiveUserOptionsAsync();
}
