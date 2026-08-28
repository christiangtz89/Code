using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class StockCountService(AppDbContext db) : IStockCountService
{
    public async Task<StockCountDto> RecordAsync(StockCountInput input, Guid? userId)
    {
        if (input.CountedQuantity < 0)
            throw new ArgumentException("La cantidad contada no puede ser negativa.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireSupplyItemLocksAsync([input.SupplyItemId]);

        var item = await db.SupplyItems.FirstOrDefaultAsync(x =>
            x.Id == input.SupplyItemId && x.IsActive && x.TrackInventory)
            ?? throw new ArgumentException("El insumo no existe o no controla inventario.");
        SupplyInventoryLot? lot = null;
        if (input.LotId.HasValue)
        {
            lot = await db.SupplyInventoryLots.FirstOrDefaultAsync(x =>
                x.Id == input.LotId && x.SupplyItemId == item.Id && x.IsActive)
                ?? throw new ArgumentException("El lote no corresponde al insumo.");
        }

        var systemQuantity = decimal.Round(
            lot is null
                ? await ledger.GetPhysicalStockAsync(item.Id)
                : await ledger.GetLotStockAsync(lot),
            3);
        if (input.ExpectedSystemQuantity.HasValue &&
            decimal.Round(input.ExpectedSystemQuantity.Value, 3) != systemQuantity)
            throw new InvalidOperationException("El inventario cambió desde que se inició el conteo. Revisa y confirma nuevamente.");

        var variance = decimal.Round(input.CountedQuantity - systemQuantity, 3);
        SupplyInventoryMovement? movement = null;
        if (variance != 0)
        {
            movement = await ledger.AppendValidatedAsync(
                item.Id,
                variance > 0
                    ? SupplyInventoryMovementType.ManualAdjustmentIncrease
                    : SupplyInventoryMovementType.ManualAdjustmentDecrease,
                Math.Abs(variance),
                input.LotId,
                userId,
                "STOCK-COUNT",
                "Conteo físico confirmado");
        }

        var count = new InventoryStockCount
        {
            Id = Guid.NewGuid(),
            SupplyItemId = item.Id,
            SupplyInventoryLotId = input.LotId,
            SystemQuantity = systemQuantity,
            CountedQuantity = input.CountedQuantity,
            Variance = variance,
            CountedAt = DateTime.UtcNow,
            CountedByUserId = userId,
            InventoryMovementId = movement?.Id
        };
        db.Add(count);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Map(count);
    }

    public async Task<IEnumerable<StockCountDto>> GetAsync(Guid id) =>
        await db.InventoryStockCounts.AsNoTracking()
            .Where(x => x.SupplyItemId == id)
            .OrderByDescending(x => x.CountedAt)
            .Select(x => new StockCountDto(
                x.Id,
                x.SupplyItemId,
                x.SupplyInventoryLotId,
                x.SystemQuantity,
                x.CountedQuantity,
                x.Variance,
                x.CountedAt,
                x.CountedByUserId,
                x.InventoryMovementId))
            .ToListAsync();

    private static StockCountDto Map(InventoryStockCount count) => new(
        count.Id,
        count.SupplyItemId,
        count.SupplyInventoryLotId,
        count.SystemQuantity,
        count.CountedQuantity,
        count.Variance,
        count.CountedAt,
        count.CountedByUserId,
        count.InventoryMovementId);
}
