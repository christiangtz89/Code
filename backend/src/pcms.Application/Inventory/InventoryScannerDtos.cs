using pcms.Domain.Enums;

namespace pcms.Application.Inventory;

public record InventoryScannerLotDto(
    Guid Id,
    string ScanCode,
    decimal RemainingQuantity,
    bool IsActive,
    bool IsEligible);

public record InventoryScannerResolveDto(
    Guid SupplyItemId,
    string SupplyItemName,
    string ScannedCode,
    bool IsLotScan,
    Guid? ScannedLotId,
    string? ScannedLotCode,
    decimal? ScannedLotRemainingQuantity,
    bool? ScannedLotIsActive,
    string UnitOfMeasure,
    decimal PhysicalStock,
    decimal ReservedQuantity,
    decimal AvailableStock,
    decimal UnallocatedPhysicalStock,
    IReadOnlyList<InventoryScannerLotDto> Lots,
    bool RequiresLotSelection,
    Guid? SuggestedLotId,
    bool HasLottedAndUnlottedStock,
    bool CanRecordOutgoing);

public record InventoryScannerOutgoingInput(
    Guid ClientOperationId,
    string ScanCode,
    Guid SupplyItemId,
    decimal Quantity,
    Guid? SupplyInventoryLotId,
    SupplyInventoryReasonCode ReasonCode,
    string? Notes,
    decimal ExpectedPhysicalStock,
    decimal ExpectedAvailableStock,
    decimal? ExpectedLotStock,
    decimal? ExpectedUnallocatedStock);

public record InventoryScannerOutgoingDto(
    Guid MovementId,
    Guid SupplyItemId,
    string SupplyItemName,
    string ScannedCode,
    decimal Quantity,
    string UnitOfMeasure,
    Guid? SupplyInventoryLotId,
    string? CurrentLotScanCode,
    decimal CurrentPhysicalStock,
    decimal CurrentAvailableStock,
    decimal? CurrentLotStock,
    DateTime OccurredAt,
    SupplyInventoryReasonCode ReasonCode,
    string? Notes,
    string? RecordedByDisplayNameSnapshot);

public interface IInventoryScannerService
{
    Task<InventoryScannerResolveDto> ResolveAsync(string scanCode, bool canRecordOutgoing);
    Task<InventoryScannerOutgoingDto> RecordOutgoingAsync(
        InventoryScannerOutgoingInput input,
        Guid userId);
}

public sealed class InventoryScannerConflictException(
    string message,
    InventoryScannerResolveDto? currentState = null) : Exception(message)
{
    public InventoryScannerResolveDto? CurrentState { get; } = currentState;
}
