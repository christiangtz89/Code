using pcms.Domain.Enums;

namespace pcms.Domain.Entities;

public class SupplyInventoryMovement
{
    public Guid Id { get; set; }
    public Guid SupplyItemId { get; set; }
    public SupplyItem SupplyItem { get; set; } = null!;
    public string SupplyItemNameSnapshot { get; set; } = string.Empty;
    public Guid? SupplyInventoryLotId { get; set; }
    public SupplyInventoryLot? SupplyInventoryLot { get; set; }
    public Guid? PurchaseReceiptItemId { get; set; }
    public PurchaseReceiptItem? PurchaseReceiptItem { get; set; }
    public SupplyInventoryMovementType MovementType { get; set; }
    public SupplyInventoryMovementOrigin Origin { get; set; }
    public SupplyInventoryReasonCode? ReasonCode { get; set; }
    public string? ScannedCode { get; set; }
    public Guid? ClientOperationId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public Guid? CremationId { get; set; }
    public Cremation? Cremation { get; set; }
    public Guid? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public string? RecordedByDisplayNameSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }
}
