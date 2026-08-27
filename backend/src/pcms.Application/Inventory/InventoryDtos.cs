using pcms.Domain.Enums;
namespace pcms.Application.Inventory;
public record InventoryMovementInput(Guid SupplyItemId,SupplyInventoryMovementType MovementType,decimal Quantity,string? Reference,string? Notes,Guid? LotId=null);
public record InventoryMovementDto(Guid Id,Guid SupplyItemId,string SupplyItemName,SupplyInventoryMovementType MovementType,decimal Quantity,string UnitOfMeasure,DateTime OccurredAt,string? Reference,string? Notes,Guid? RecordedByUserId);
public record InventoryItemDto(Guid SupplyItemId,string Name,string UnitOfMeasure,decimal MinimumQuantity,decimal CurrentQuantity,bool IsLowStock,bool TrackInventory);
