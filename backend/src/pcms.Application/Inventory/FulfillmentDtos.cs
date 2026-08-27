using pcms.Domain.Enums;
namespace pcms.Application.Inventory;
public record FulfillmentMaterialInput(Guid SupplyItemId, decimal Quantity, Guid? LotId);
public record UrnReservationDto(Guid Id, Guid CremationId, Guid UrnId, string UrnName, Guid SupplyItemId, string SupplyItemName, UrnReservationStatus Status, DateTime ReservedAt, Guid? ReservedByUserId, DateTime? CancelledAt, DateTime? FulfilledAt);
public record FulfillmentMaterialDto(Guid SupplyItemId, string SupplyItemName, decimal Quantity, string UnitOfMeasure, Guid? LotId, Guid InventoryMovementId);
public record CremationInventoryDto(Guid CremationId, Guid? UrnId, string? UrnName, decimal PhysicalStock, decimal ReservedQuantity, decimal AvailableQuantity, UrnReservationDto? Reservation, Guid? FulfillmentId, DateTime? FulfilledAt, IReadOnlyList<FulfillmentMaterialDto> Materials);
public interface ICremationInventoryService { Task<CremationInventoryDto> GetAsync(Guid cremationId); Task<CremationInventoryDto> ReserveAsync(Guid cremationId, Guid? userId); Task<CremationInventoryDto> CancelAsync(Guid cremationId, Guid? userId, string? reason); Task<CremationInventoryDto> FulfillAsync(Guid cremationId, IReadOnlyCollection<FulfillmentMaterialInput> materials, Guid? userId); }
