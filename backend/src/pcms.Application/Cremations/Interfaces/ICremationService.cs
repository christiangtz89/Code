using pcms.Application.Cremations.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Cremations.Interfaces;

public interface ICremationService
{
    Task<CremationDto> CreateAsync(
        CreateCremationDto dto);

    Task<PagedCremationsDto> GetAllAsync(
        int page,
        int pageSize);

    Task<CremationDto?> GetByIdAsync(Guid id);

    Task<CremationDto?> GetByReceptionIdAsync(
        Guid receptionId);

    Task<IEnumerable<CremationDto>> GetByStatusAsync(
        CremationStatus status);

    Task<CremationDto?> UpdateAsync(
        Guid id,
        UpdateCremationDto dto);

    Task<CremationDto?> ChangeStatusAsync(
        Guid id,
        ChangeCremationStatusDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<CremationDto>> SearchAsync(
        string search);
}