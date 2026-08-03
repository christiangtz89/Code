using pcms.Application.Common;

namespace pcms.Application.Customers;

public interface ICustomerService
{
    Task<PaginatedResult<CustomerDto>> GetAllAsync(
        int page,
        int pageSize,
        bool isActive);

    Task<CustomerDto?> GetByIdAsync(Guid id);

    Task<CustomerDto> CreateAsync(
        CreateCustomerDto dto);

    Task<CustomerDto?> UpdateAsync(
        Guid id,
        UpdateCustomerDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<CustomerDto>> SearchAsync(
        string term,
        bool isActive);
}