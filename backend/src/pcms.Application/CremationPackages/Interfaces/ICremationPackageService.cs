using pcms.Application.CremationPackages.DTOs;

namespace pcms.Application.CremationPackages.Interfaces;

public interface ICremationPackageService
{
    Task<IEnumerable<CremationPackageDto>> GetAllAsync(
        bool includeInactive = false,
        bool publicOnly = false);

    Task<CremationPackageDto?> GetByIdAsync(Guid id);

    Task<CremationPackageDto> CreateAsync(CreateCremationPackageDto dto);

    Task<CremationPackageDto?> UpdateAsync(
        Guid id,
        UpdateCremationPackageDto dto);
}