using pcms.Application.Veterinarians.DTOs;
using pcms.Application.Common;

namespace pcms.Application.Veterinarians.Interfaces;

public interface IVeterinarianService
{
    Task<VeterinarianDto> CreateAsync(
        CreateVeterinarianDto dto);

    Task<PagedVeterinariansDto> GetAllAsync(
        int page,
        int pageSize,
        bool isActive,
        Guid? veterinaryClinicId);

    Task<VeterinarianDto?> GetByIdAsync(Guid id);

    Task<IEnumerable<VeterinarianDto>>
        GetByClinicIdAsync(
            Guid veterinaryClinicId,
            bool? isActive);

    Task<VeterinarianDto?> UpdateAsync(
        Guid id,
        UpdateVeterinarianDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<PagedVeterinariansDto> SearchAsync(
        string search,
        bool isActive,
        Guid? veterinaryClinicId,
        int page,
        int pageSize);

    Task<PaginatedResult<VeterinarianClinicOptionDto>>
        GetClinicOptionsAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize);
}
