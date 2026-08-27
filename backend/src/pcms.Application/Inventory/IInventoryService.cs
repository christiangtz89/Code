namespace pcms.Application.Inventory;
public interface IInventoryService { Task<IEnumerable<InventoryItemDto>> GetCurrentAsync(string? search); Task<IEnumerable<InventoryMovementDto>> GetMovementsAsync(Guid supplyItemId); Task<InventoryMovementDto> RecordAsync(InventoryMovementInput input,Guid? userId); }
