using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Enums;
using pcms.Domain.Inventory;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<IEnumerable<InventoryItemDto>> GetCurrentAsync(string? search)
    {
        var query = db.SupplyItems.AsNoTracking().Where(x => x.IsActive && x.TrackInventory);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.ScanCode.Contains(search));
        var items = await query.OrderBy(x => x.Name).ToListAsync();
        var ids = items.Select(x => x.Id).ToArray();
        var movements = await db.SupplyInventoryMovements.AsNoTracking().Where(x => ids.Contains(x.SupplyItemId)).ToListAsync();
        return items.Select(x =>
        {
            var quantity = movements
                .Where(m => m.SupplyItemId == x.Id)
                .Sum(m => SupplyInventoryMovementEffects.Apply(m.MovementType, m.Quantity));
            return new InventoryItemDto(x.Id, x.Name, x.UnitOfMeasure, x.MinimumQuantity, quantity, quantity <= x.MinimumQuantity, x.TrackInventory);
        });
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementsAsync(Guid supplyItemId) => await db.SupplyInventoryMovements.AsNoTracking().Include(x => x.SupplyItem).Where(x => x.SupplyItemId == supplyItemId).OrderByDescending(x => x.OccurredAt).Select(x => new InventoryMovementDto(x.Id, x.SupplyItemId, x.SupplyItem.Name, x.MovementType, x.Quantity, x.UnitOfMeasure, x.OccurredAt, x.Reference, x.Notes, x.RecordedByUserId)).ToListAsync();

    public async Task<InventoryMovementDto> RecordAsync(InventoryMovementInput input, Guid? userId)
    {
        if (input.Quantity <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
        if (input.MovementType is SupplyInventoryMovementType.PurchaseReceipt or SupplyInventoryMovementType.PurchaseReturn) throw new ArgumentException("Este movimiento se genera desde compras o devoluciones.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireSupplyItemLocksAsync([input.SupplyItemId]);
        var movement = await ledger.AppendValidatedAsync(
            input.SupplyItemId,
            input.MovementType,
            input.Quantity,
            SupplyInventoryMovementOrigin.ManualAdjustment,
            input.LotId,
            userId,
            input.Reference,
            input.Notes);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var itemName = await db.SupplyItems
            .Where(x => x.Id == movement.SupplyItemId)
            .Select(x => x.Name)
            .SingleAsync();
        return new InventoryMovementDto(movement.Id, movement.SupplyItemId, itemName, movement.MovementType, movement.Quantity, movement.UnitOfMeasure, movement.OccurredAt, movement.Reference, movement.Notes, movement.RecordedByUserId);
    }
}
