namespace pcms.Application.Reporting;

public record CostAnalyticsSupplyDto(Guid SupplyItemId, string Name, string UnitOfMeasure, decimal CurrentQuantity, decimal? CurrentUnitCost, decimal? NormalizedCost, string? Currency, decimal? EstimatedReplacementValue, bool CostAvailable);
public record CostAnalyticsSupplierDto(Guid SupplyItemId, string SupplyItemName, Guid SupplierId, string SupplierName, decimal UnitCost, decimal NormalizedCost, string PurchaseUnit, decimal InventoryUnitsPerPurchaseUnit, string Currency, bool IsPreferred);
public record CostAnalyticsHistoryDto(DateTime Period, Guid SupplyItemId, string SupplyItemName, decimal Quantity, decimal Amount, string Currency);
public record CostAnalyticsBomDto(Guid UrnId, string UrnName, int Version, decimal? EstimatedMaterialCost, string? Currency, bool CostComplete, IReadOnlyList<CostAnalyticsBomItemDto> Items);
public record CostAnalyticsBomItemDto(string SupplyItemName, decimal RequiredQuantity, string UnitOfMeasure, decimal? UnitCost, decimal? EstimatedCost, string? Currency, bool CostAvailable);
public record CostAnalyticsProductionDto(Guid ProductionId, string UrnName, DateTime ProducedAt, decimal QuantityProduced, decimal? ActualMaterialCost, decimal? CostPerUrn, decimal? ExpectedMaterialCost, decimal? QuantityVariance, decimal? CostVariance, bool CostComplete);
public record CostAnalyticsUrnDto(string UrnName, string CostBasis, decimal? CostPerUnit, string? Currency, decimal? QuantityInStock, decimal? EstimatedReplacementValue, bool CostAvailable);
public record CostAnalyticsDto(DateTime StartDate, DateTime EndDate, IReadOnlyList<CostAnalyticsSupplyDto> Supplies, IReadOnlyList<CostAnalyticsSupplierDto> Suppliers, IReadOnlyList<CostAnalyticsHistoryDto> PurchaseHistory, IReadOnlyList<CostAnalyticsBomDto> Boms, IReadOnlyList<CostAnalyticsProductionDto> Productions, IReadOnlyList<CostAnalyticsUrnDto> Urns);
public interface ICostAnalyticsService { Task<CostAnalyticsDto> GetAsync(DateTime? startDate, DateTime? endDate); }
