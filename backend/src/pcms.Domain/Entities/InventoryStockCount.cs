namespace pcms.Domain.Entities;

public class InventoryStockCount
{
    public Guid Id { get; set; }
    public Guid SupplyItemId { get; set; }
    public Guid? SupplyInventoryLotId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance { get; set; }
    public DateTime CountedAt { get; set; }
    public Guid? CountedByUserId { get; set; }
    public Guid? InventoryMovementId { get; set; }
    public SupplyItem SupplyItem { get; set; } = null!;
    public SupplyInventoryLot? SupplyInventoryLot { get; set; }
    public SupplyInventoryMovement? InventoryMovement { get; set; }
    public User? CountedByUser { get; set; }
}
