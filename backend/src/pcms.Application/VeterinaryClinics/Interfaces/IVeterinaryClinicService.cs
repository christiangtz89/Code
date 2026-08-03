using pcms.Application.VeterinaryClinics.DTOs;

namespace pcms.Application.VeterinaryClinics.Interfaces;

public interface IVeterinaryClinicService
{
    Task<VeterinaryClinicDto> CreateAsync(
        CreateVeterinaryClinicDto dto);

    Task<PagedVeterinaryClinicsDto> GetAllAsync(
        int page,
        int pageSize);

    Task<VeterinaryClinicDto?> GetByIdAsync(Guid id);

    Task<VeterinaryClinicDto?> UpdateAsync(
        Guid id,
        UpdateVeterinaryClinicDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<VeterinaryClinicDto>> SearchAsync(
        string search);
}