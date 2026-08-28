using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class ManufacturedUrnProductionService(AppDbContext db) : IManufacturedUrnProductionService
{
    public async Task<ProductionDto> CreateAsync(ProductionInput input, Guid? userId)
    {
        if (input.QuantityProduced <= 0 || input.Usages.Count == 0)
            throw new ArgumentException("La producción requiere cantidad y materiales.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var bom = await db.UrnBillOfMaterials
            .Include(x => x.Urn)
            .Include(x => x.Items)
            .ThenInclude(x => x.SupplyItem)
            .FirstOrDefaultAsync(x => x.Id == input.UrnBillOfMaterialsId && x.UrnId == input.UrnId);
        if (bom is null) throw new ArgumentException("La BOM no existe.");

        var mapping = await db.UrnSupplyItems.Include(x => x.SupplyItem)
            .FirstOrDefaultAsync(x => x.UrnId == input.UrnId && x.IsActive);
        if (mapping is null ||
            !mapping.SupplyItem.IsActive ||
            !mapping.SupplyItem.TrackInventory ||
            !Piece(mapping.SupplyItem.UnitOfMeasure))
            throw new InvalidOperationException("La urna requiere un insumo terminado activo y compatible.");
        if (await db.ManufacturedUrnProductions.AnyAsync(x =>
                x.FinishedGoodsReceiptMovementId != null && x.Id == Guid.Empty))
            throw new InvalidOperationException("Producción duplicada.");

        var usages = input.Usages.ToDictionary(x => x.SupplyItemId);
        if (usages.Count != bom.Items.Count || bom.Items.Any(x => !usages.ContainsKey(x.SupplyItemId)))
            throw new ArgumentException("Debe registrar todos los materiales de la BOM.");

        var ledger = new InventoryLedger(db);
        await ledger.AcquireSupplyItemLocksAsync(
            usages.Keys.Append(mapping.SupplyItemId));

        var production = new ManufacturedUrnProduction
        {
            Id = Guid.NewGuid(),
            UrnId = input.UrnId,
            UrnBillOfMaterialsId = bom.Id,
            QuantityProduced = decimal.Round(input.QuantityProduced, 3),
            ProducedAt = input.ProducedAt ?? DateTime.UtcNow,
            Notes = input.Notes,
            RecordedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var bomItem in bom.Items)
        {
            var usage = usages[bomItem.SupplyItemId];
            if (usage.ActualQuantity <= 0)
                throw new ArgumentException("Las cantidades utilizadas deben ser mayores que cero.");

            var item = await db.SupplyItems.FirstOrDefaultAsync(x =>
                x.Id == bomItem.SupplyItemId && x.IsActive && x.TrackInventory)
                ?? throw new InvalidOperationException("Un material no controla inventario.");
            var source = await db.SupplierSupplyItems.AsNoTracking()
                .Where(x =>
                    x.SupplyItemId == item.Id &&
                    x.IsActive &&
                    x.IsPreferred &&
                    x.InventoryUnitsPerPurchaseUnit > 0)
                .SingleOrDefaultAsync();
            decimal? cost = source is null
                ? null
                : source.CurrentUnitCost / source.InventoryUnitsPerPurchaseUnit;

            var movement = await ledger.AppendValidatedAsync(
                item.Id,
                SupplyInventoryMovementType.ManufacturingConsumption,
                usage.ActualQuantity,
                SupplyInventoryMovementOrigin.Manufacturing,
                null,
                userId,
                production.Id.ToString(),
                "Consumo de producción",
                occurredAt: production.ProducedAt,
                unitOfMeasure: bomItem.UnitOfMeasure);
            production.MaterialUsages.Add(new ManufacturingMaterialUsage
            {
                Id = Guid.NewGuid(),
                ProductionId = production.Id,
                SupplyItemId = item.Id,
                SupplyItemNameSnapshot = item.Name,
                ExpectedQuantity = bomItem.RequiredQuantity,
                ActualQuantity = decimal.Round(usage.ActualQuantity, 3),
                WasteQuantity = usage.WasteQuantity is null
                    ? null
                    : decimal.Round(usage.WasteQuantity.Value, 3),
                UnitOfMeasure = bomItem.UnitOfMeasure,
                CostPerUnitSnapshot = cost,
                TotalMaterialCostSnapshot = cost is null
                    ? null
                    : decimal.Round(cost.Value * usage.ActualQuantity, 4),
                InventoryMovementId = movement.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        var receipt = await ledger.AppendValidatedAsync(
            mapping.SupplyItemId,
            SupplyInventoryMovementType.ManufacturingReceipt,
            production.QuantityProduced,
            SupplyInventoryMovementOrigin.Manufacturing,
            null,
            userId,
            production.Id.ToString(),
            "Entrada de urna fabricada",
            occurredAt: production.ProducedAt);
        production.FinishedGoodsReceiptMovementId = receipt.Id;
        db.Add(production);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (await GetAllAsync(production.UrnId, false)).First(x => x.Id == production.Id);
    }

    public async Task<IEnumerable<ProductionDto>> GetAllAsync(Guid? urnId, bool includeCost)
    {
        var query = db.ManufacturedUrnProductions.AsNoTracking()
            .Include(x => x.Urn)
            .Include(x => x.UrnBillOfMaterials)
            .Include(x => x.MaterialUsages)
            .ThenInclude(x => x.SupplyItem)
            .AsQueryable();
        if (urnId.HasValue) query = query.Where(x => x.UrnId == urnId);
        return (await query.OrderByDescending(x => x.ProducedAt).ToListAsync())
            .Select(x => Map(x, includeCost));
    }

    private static ProductionDto Map(ManufacturedUrnProduction production, bool includeCost)
    {
        var usages = production.MaterialUsages.Select(x => new ProductionUsageDto(
            x.Id,
            x.SupplyItemId,
            x.SupplyItemNameSnapshot,
            x.ExpectedQuantity,
            x.ActualQuantity,
            x.WasteQuantity,
            x.UnitOfMeasure,
            includeCost ? x.CostPerUnitSnapshot : null,
            includeCost ? x.TotalMaterialCostSnapshot : null)).ToList();
        var costComplete = usages.All(x => x.CostPerUnitSnapshot.HasValue);
        return new ProductionDto(
            production.Id,
            production.UrnId,
            production.Urn.Name,
            production.UrnBillOfMaterialsId,
            production.UrnBillOfMaterials.Version,
            production.QuantityProduced,
            production.ProducedAt,
            production.Notes,
            usages,
            includeCost && costComplete
                ? usages.Sum(x => x.TotalMaterialCostSnapshot!.Value)
                : null,
            costComplete);
    }

    private static bool Piece(string unitOfMeasure) =>
        new[] { "pieza", "pza", "unidad", "units" }.Contains(unitOfMeasure.ToLowerInvariant());
}
