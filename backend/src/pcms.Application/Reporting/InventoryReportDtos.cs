using pcms.Domain.Enums;
namespace pcms.Application.Reporting;
public record InventoryStockReportDto(Guid SupplyItemId,string Name,string Category,string UnitOfMeasure,string ScanCode,decimal CurrentQuantity,decimal MinimumQuantity,bool IsLowStock,bool IsActive);
public record MovementSummaryDto(SupplyInventoryMovementType MovementType,decimal Quantity,int Count,bool IsInbound);
public record LotReportDto(Guid Id,string SupplyItemName,string ScanCode,string? ManufacturerLotNumber,decimal InitialQuantity,decimal RemainingQuantity,string UnitOfMeasure,DateTime ReceivedAt,bool IsActive,bool IsEmpty,bool IsLow);
public record ProductionReportDto(Guid Id,string UrnName,int BomVersion,decimal QuantityProduced,DateTime ProducedAt,IReadOnlyList<ProductionVarianceDto> Materials);
public record ProductionVarianceDto(string SupplyItemName,decimal ExpectedQuantity,decimal ActualQuantity,decimal? WasteQuantity,string UnitOfMeasure,decimal Variance);
public record FinishedUrnStockDto(string UrnName,string SupplyItemName,decimal CurrentQuantity,decimal MinimumQuantity,bool IsLowStock,decimal PurchasedReceipts,decimal ManufacturedReceipts);
public record InventoryReportDto(DateTime StartDate,DateTime EndDate,IReadOnlyList<InventoryStockReportDto> Stock,IReadOnlyList<MovementSummaryDto> Movements,IReadOnlyList<LotReportDto> Lots,IReadOnlyList<ProductionReportDto> Productions,IReadOnlyList<FinishedUrnStockDto> FinishedUrns);
