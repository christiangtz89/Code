using pcms.Domain.Enums;
namespace pcms.Domain.Entities;
public class CremationUrnReservation
{
 public Guid Id { get; set; } public Guid CremationId { get; set; } public Cremation Cremation { get; set; } = null!; public Guid UrnId { get; set; } public Urn Urn { get; set; } = null!; public Guid SupplyItemId { get; set; } public SupplyItem SupplyItem { get; set; } = null!; public string UrnNameSnapshot { get; set; } = string.Empty; public string SupplyItemNameSnapshot { get; set; } = string.Empty; public string SupplyItemScanCodeSnapshot { get; set; } = string.Empty; public UrnReservationStatus Status { get; set; } public DateTime ReservedAt { get; set; } public Guid? ReservedByUserId { get; set; } public User? ReservedByUser { get; set; } public DateTime? CancelledAt { get; set; } public Guid? CancelledByUserId { get; set; } public User? CancelledByUser { get; set; } public string? CancellationReason { get; set; } public DateTime? FulfilledAt { get; set; } public Guid? FulfilledByUserId { get; set; } public User? FulfilledByUser { get; set; } public Guid? FulfillmentId { get; set; } public CremationInventoryFulfillment? Fulfillment { get; set; }
}
