using pcms.Domain.Enums;

namespace pcms.Domain.Inventory;

public static class SupplyInventoryMovementEffects
{
    public static int Direction(SupplyInventoryMovementType movementType) => movementType switch
    {
        SupplyInventoryMovementType.PurchaseReceipt => 1,
        SupplyInventoryMovementType.ManualAdjustmentIncrease => 1,
        SupplyInventoryMovementType.ManualAdjustmentDecrease => -1,
        SupplyInventoryMovementType.Consumption => -1,
        SupplyInventoryMovementType.Waste => -1,
        SupplyInventoryMovementType.ManufacturingConsumption => -1,
        SupplyInventoryMovementType.PurchaseReturn => -1,
        SupplyInventoryMovementType.ManufacturingReceipt => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(movementType), movementType, "Tipo de movimiento de inventario no válido.")
    };

    public static decimal Apply(SupplyInventoryMovementType movementType, decimal quantity) =>
        Direction(movementType) * quantity;
}
