namespace pcms.Application.Supplies.DTOs;
public record SupplierDto(Guid Id,string Name,string? LegalName,string? TaxId,string? ContactName,string? Phone,string? Email,string? Website,string? Address,string? Notes,bool IsActive,DateTime CreatedAt,DateTime? UpdatedAt);
public record SupplierInput(string Name,string? LegalName,string? TaxId,string? ContactName,string? Phone,string? Email,string? Website,string? Address,string? Notes);
public record PagedSuppliersDto(IEnumerable<SupplierDto> Items,int Page,int PageSize,int TotalItems,int TotalPages);
