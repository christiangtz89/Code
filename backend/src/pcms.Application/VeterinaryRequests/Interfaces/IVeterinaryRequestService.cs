using pcms.Application.VeterinaryRequests.DTOs;
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

    Task<IEnumerable<VeterinaryRequestDto>> SearchAsync(
        string search,
        VeterinaryRequestStatus? status);
}