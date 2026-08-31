using pcms.Domain.Enums;

namespace pcms.Application.Inventory;

public enum CremationScannerMatchKind
{
    ReservedUrn = 1,
    AdditionalMaterial = 2
}

public record CremationScannerReservedUrnDto(
    Guid SupplyItemId,
    string UrnName,
    string SupplyItemName,
    string ExpectedScanCode,
    decimal Quantity,
    UrnReservationStatus Status,
    DateTime ReservedAt);

public record CremationScannerCompletedMaterialDto(
    string SupplyItemName,
    decimal Quantity,
    string UnitOfMeasure);

public record CremationScannerPreviewDto(
    Guid CremationId,
    string CaseReference,
    string PetName,
    string CustomerName,
    CremationStatus CremationStatus,
    bool IsFulfilled,
    Guid? FulfillmentId,
    DateTime? FulfilledAt,
    CremationScannerReservedUrnDto? ReservedUrn,
    IReadOnlyList<CremationScannerCompletedMaterialDto> CompletedMaterials,
    bool CanFulfill,
    string? BlockingReason);

public record CremationScannerResolveDto(
    Guid CremationId,
    string ScannedCode,
    Guid SupplyItemId,
    string SupplyItemName,
    string UnitOfMeasure,
    bool IsLotScan,
    Guid? ScannedLotId,
    string? ScannedLotCode,
    decimal? ScannedLotRemainingQuantity,
    bool? ScannedLotIsActive,
    CremationScannerMatchKind? MatchKind,
    decimal SuggestedQuantity,
    decimal PhysicalStock,
    decimal ReservedQuantity,
    decimal AvailableForFulfillment,
    decimal UnallocatedPhysicalStock,
    IReadOnlyList<InventoryScannerLotDto> Lots,
    bool RequiresLotSelection,
    Guid? SuggestedLotId,
    bool CanUse,
    string? RejectionReason);

public record CremationScannerSelectionInput(
    string ScanCode,
    Guid SupplyItemId,
    Guid? SupplyInventoryLotId,
    decimal Quantity,
    decimal ExpectedPhysicalStock,
    decimal ExpectedAvailableStock,
    decimal? ExpectedLotStock,
    decimal? ExpectedUnallocatedStock);

public record CremationScannerFulfillInput(
    CremationStatus ExpectedCremationStatus,
    DateTime ExpectedReservationReservedAt,
    CremationScannerSelectionInput? Urn,
    IReadOnlyCollection<CremationScannerSelectionInput>? Materials);

public interface ICremationScannerService
{
    Task<CremationScannerPreviewDto> GetPreviewAsync(Guid cremationId);
    Task<CremationScannerResolveDto> ResolveAsync(
        Guid cremationId,
        string scanCode,
        CremationScannerMatchKind expectedKind,
        CremationStatus expectedCremationStatus,
        DateTime expectedReservationReservedAt,
        Guid expectedUrnSupplyItemId,
        string expectedUrnScanCode);
    Task<CremationScannerPreviewDto> FulfillAsync(
        Guid cremationId,
        CremationScannerFulfillInput input,
        Guid userId);
}

public sealed class CremationScannerConflictException(
    string message,
    CremationScannerPreviewDto? currentPreview = null) : Exception(message)
{
    public CremationScannerPreviewDto? CurrentPreview { get; } = currentPreview;
}
