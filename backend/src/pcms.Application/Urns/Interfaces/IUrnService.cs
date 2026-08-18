using pcms.Application.Urns.DTOs;

namespace pcms.Application.Urns.Interfaces;

public interface IUrnService
{
    Task<IEnumerable<UrnDto>> GetAllAsync(
        bool includeInactive = false,
        bool publicOnly = false);

    Task<UrnDto?> GetByIdAsync(Guid id);

    Task<UrnDto> CreateAsync(CreateUrnDto dto);

    Task<UrnDto?> UpdateAsync(
        Guid id,
        UpdateUrnDto dto);
}