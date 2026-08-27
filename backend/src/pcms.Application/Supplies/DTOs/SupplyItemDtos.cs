namespace pcms.Application.Supplies.DTOs;
public record SupplyItemDto(Guid Id,string Name,string? Description,string? InternalSku,string Category,string UnitOfMeasure,bool TrackInventory,decimal MinimumQuantity,string ScanCode,bool IsActive,DateTime CreatedAt,DateTime? UpdatedAt);
public record SupplyItemInput(string Name,string? Description,string? InternalSku,string Category,string UnitOfMeasure,bool TrackInventory,decimal MinimumQuantity,string ScanCode);
public record PagedSupplyItemsDto(IEnumerable<SupplyItemDto> Items,int Page,int PageSize,int TotalItems,int TotalPages);
