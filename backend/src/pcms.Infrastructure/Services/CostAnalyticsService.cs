using Microsoft.EntityFrameworkCore;
using pcms.Application.Reporting;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class CostAnalyticsService(AppDbContext db, pcms.Application.Inventory.IInventoryService inventory) : ICostAnalyticsService
{
    public async Task<CostAnalyticsDto> GetAsync(DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.UtcNow.Date; var start = startDate?.Date ?? new DateTime(today.Year, today.Month, 1); var end = endDate?.Date ?? today;
        if (end < start) throw new ArgumentException("La fecha final debe ser igual o posterior a la inicial.");
        var finish = end.AddDays(1);
        var items = await db.SupplyItems.AsNoTracking().Where(x => x.IsActive).ToListAsync();
        var current = (await inventory.GetCurrentAsync(null)).ToDictionary(x => x.SupplyItemId);
        var sources = await db.SupplierSupplyItems.AsNoTracking().Include(x => x.Supplier).Include(x => x.SupplyItem).Where(x => x.IsActive && x.InventoryUnitsPerPurchaseUnit > 0).ToListAsync();
        var preferred = sources.GroupBy(x => x.SupplyItemId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IsPreferred).ThenBy(x => x.SupplierId).First());
        var supplies = items.Select(x => { preferred.TryGetValue(x.Id, out var s); current.TryGetValue(x.Id, out var q); var unit = s is null ? (decimal?)null : s.CurrentUnitCost / s.InventoryUnitsPerPurchaseUnit; var value = unit.HasValue && q is not null ? unit.Value * q.CurrentQuantity : (decimal?)null; return new CostAnalyticsSupplyDto(x.Id, x.Name, x.UnitOfMeasure, q?.CurrentQuantity ?? 0m, s is null ? null : s.CurrentUnitCost, unit, s?.Currency, value, value.HasValue); }).ToList();
        var supplierRows = sources.OrderBy(x => x.SupplyItem.Name).ThenByDescending(x => x.IsPreferred).ThenBy(x => x.SupplierId).Select(x => new CostAnalyticsSupplierDto(x.SupplyItemId, x.SupplyItem.Name, x.SupplierId, x.Supplier.Name, x.CurrentUnitCost, x.CurrentUnitCost / x.InventoryUnitsPerPurchaseUnit, x.PurchaseUnit, x.InventoryUnitsPerPurchaseUnit, x.Currency, x.IsPreferred)).ToList();
        var receipts = await db.PurchaseReceiptItems.AsNoTracking().Include(x => x.PurchaseReceipt).ThenInclude(x => x.Purchase).Include(x => x.PurchaseItem).ThenInclude(x => x.SupplyItem).Where(x => x.PurchaseReceipt.ReceivedAt >= start && x.PurchaseReceipt.ReceivedAt < finish).ToListAsync();
        var history = receipts.GroupBy(x => new { Period = new DateTime(x.PurchaseReceipt.ReceivedAt.Year, x.PurchaseReceipt.ReceivedAt.Month, 1), x.PurchaseItem.SupplyItemId, x.PurchaseItem.SupplyItem.Name, x.CurrencySnapshot }).Select(g => new CostAnalyticsHistoryDto(g.Key.Period, g.Key.SupplyItemId, g.Key.Name, g.Sum(x => x.NormalizedReceivedQuantity), g.Sum(x => x.QuantityReceived * x.UnitCostSnapshot), g.Key.CurrencySnapshot)).OrderBy(x => x.Period).ThenBy(x => x.SupplyItemName).ToList();
        var boms = await db.UrnBillOfMaterials.AsNoTracking().Include(x => x.Urn).Include(x => x.Items).ThenInclude(x => x.SupplyItem).Where(x => x.IsActive).ToListAsync();
        var bomDtos = boms.Select(b => { var rows = b.Items.Select(i => { preferred.TryGetValue(i.SupplyItemId, out var s); var unit = s is null ? (decimal?)null : s.CurrentUnitCost / s.InventoryUnitsPerPurchaseUnit; return new CostAnalyticsBomItemDto(i.SupplyItem.Name, i.RequiredQuantity, i.UnitOfMeasure, unit, unit.HasValue ? unit.Value * i.RequiredQuantity : null, s?.Currency, unit.HasValue); }).ToList(); var currencies = rows.Where(x => x.Currency is not null).Select(x => x.Currency!).Distinct().ToList(); var complete = rows.All(x => x.CostAvailable) && currencies.Count == 1; return new CostAnalyticsBomDto(b.UrnId, b.Urn.Name, b.Version, complete ? rows.Sum(x => x.EstimatedCost!.Value) : null, complete ? currencies[0] : null, complete, rows); }).ToList();
        var productions = await db.ManufacturedUrnProductions.AsNoTracking().Include(x => x.Urn).Include(x => x.MaterialUsages).Where(x => x.ProducedAt >= start && x.ProducedAt < finish).ToListAsync();
        var productionDtos = productions.Select(p => { var actual = p.MaterialUsages.All(x => x.TotalMaterialCostSnapshot.HasValue) ? p.MaterialUsages.Sum(x => x.TotalMaterialCostSnapshot!.Value) : (decimal?)null; var expectedQty = p.MaterialUsages.Sum(x => x.ExpectedQuantity); var actualQty = p.MaterialUsages.Sum(x => x.ActualQuantity); var estimated = p.MaterialUsages.All(x => preferred.ContainsKey(x.SupplyItemId)) ? p.MaterialUsages.Sum(x => (preferred[x.SupplyItemId].CurrentUnitCost / preferred[x.SupplyItemId].InventoryUnitsPerPurchaseUnit) * x.ExpectedQuantity) : (decimal?)null; return new CostAnalyticsProductionDto(p.Id, p.Urn.Name, p.ProducedAt, p.QuantityProduced, actual, actual.HasValue && p.QuantityProduced > 0 ? actual.Value / p.QuantityProduced : null, estimated, actualQty - expectedQty, actual.HasValue && estimated.HasValue ? actual.Value - estimated.Value : null, actual.HasValue); }).ToList();
        var urnMappings = await db.UrnSupplyItems.AsNoTracking().Include(x => x.Urn).Include(x => x.SupplyItem).Where(x => x.IsActive).ToListAsync();
        var urns = urnMappings.Select(m => { preferred.TryGetValue(m.SupplyItemId, out var s); current.TryGetValue(m.SupplyItemId, out var q); var unit = s is null ? (decimal?)null : s.CurrentUnitCost / s.InventoryUnitsPerPurchaseUnit; return new CostAnalyticsUrnDto(m.Urn.Name, "Comprada (reposición)", unit, s?.Currency, q?.CurrentQuantity, unit.HasValue ? unit.Value * (q?.CurrentQuantity ?? 0m) : null, unit.HasValue); }).Concat(bomDtos.Select(b => { var p = productionDtos.Where(x => x.UrnName == b.UrnName && x.CostPerUrn.HasValue).OrderByDescending(x => x.ProducedAt).FirstOrDefault(); return new CostAnalyticsUrnDto(b.UrnName, "Fabricada (último costo real)", p?.CostPerUrn, null, null, null, p?.CostPerUrn.HasValue == true); })).ToList();
        return new(start, end, supplies, supplierRows, history, bomDtos, productionDtos, urns);
    }
}
