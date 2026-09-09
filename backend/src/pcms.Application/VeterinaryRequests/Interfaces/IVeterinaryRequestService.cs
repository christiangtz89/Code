using pcms.Application.VeterinaryRequests.DTOs;
using pcms.Application.Common;
using pcms.Domain.Enums;

namespace pcms.Application.VeterinaryRequests.Interfaces;

public interface IVeterinaryRequestService
{
    Task<VeterinaryRequestDto> CreateAsync(
        CreateVeterinaryRequestDto dto,
        Guid submittedByUserId);

    Task<PagedVeterinaryRequestsDto> GetAllAsync(
        int page,
        int pageSize,
        VeterinaryRequestStatus? status);

    Task<VeterinaryRequestDto?> GetByIdAsync(
        Guid id);

    Task<VeterinaryRequestDto?> UpdateAsync(
        Guid id,
        UpdateVeterinaryRequestDto dto);

    Task<VeterinaryRequestDto?> ChangeStatusAsync(
        Guid id,
        ChangeVeterinaryRequestStatusDto dto,
        Guid reviewedByUserId);

    Task<VeterinaryRequestDto?> ConvertAsync(
        Guid id,
        ConvertVeterinaryRequestDto dto,
        Guid receivedByUserId);

    Task<PagedVeterinaryRequestsDto> SearchAsync(
        string search,
        VeterinaryRequestStatus? status,
        int page,
        int pageSize);

    Task<PaginatedResult<VeterinaryRequestClinicOptionDto>>
        GetClinicOptionsAsync(
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<VeterinaryRequestVeterinarianOptionDto>>
        GetVeterinarianOptionsAsync(
            Guid? veterinaryClinicId,
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<VeterinaryRequestCustomerOptionDto>>
        GetCustomerOptionsAsync(
            string? search,
            int page,
            int pageSize);

    Task<PaginatedResult<VeterinaryRequestPetOptionDto>>
        GetPetOptionsAsync(
            string? search,
            int page,
            int pageSize);
}
