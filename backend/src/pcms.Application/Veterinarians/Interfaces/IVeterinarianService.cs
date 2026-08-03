using pcms.Application.Veterinarians.DTOs;

namespace pcms.Application.Veterinarians.Interfaces;

public interface IVeterinarianService
{
    Task<VeterinarianDto> CreateAsync(
        CreateVeterinarianDto dto);

    Task<PagedVeterinariansDto> GetAllAsync(
        int page,
        int pageSize);

    Task<VeterinarianDto?> GetByIdAsync(Guid id);

    Task<IEnumerable<VeterinarianDto>> GetByClinicIdAsync(
        Guid veterinaryClinicId);

    Task<VeterinarianDto?> UpdateAsync(
        Guid id,
        UpdateVeterinarianDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<VeterinarianDto>> SearchAsync(
        string search);
}