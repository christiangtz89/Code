namespace pcms.Application.Inventory;
public record UrnInventoryDto(Guid Id,Guid UrnId,string UrnName,Guid SupplyItemId,string SupplyItemName,string ScanCode,string UnitOfMeasure,decimal CurrentQuantity,decimal MinimumQuantity,bool IsLowStock);
public record UrnSupplyItemInput(Guid UrnId,Guid SupplyItemId);
public interface IUrnInventoryService { Task<IEnumerable<UrnInventoryDto>> GetAsync(); Task<UrnInventoryDto> SaveAsync(UrnSupplyItemInput input); }
