using pcms.Application.Pets.DTOs;

namespace pcms.Application.Pets.Interfaces;

public interface IPetService
{
    Task<PetDto> CreateAsync(CreatePetDto dto);

    Task<PagedPetsDto> GetAllAsync(
        int page,
        int pageSize,
        bool isActive);

    Task<PetDto?> GetByIdAsync(Guid id);

    Task<PetDto?> UpdateAsync(
        Guid id,
        UpdatePetDto dto);

    Task<IEnumerable<PetDto>> GetByCustomerIdAsync(
        Guid customerId,
        bool? isActive);    

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<PetDto>> SearchAsync(
        string search,
        bool isActive);
}