using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
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
        return items.Select(x => { var quantity = movements.Where(m => m.SupplyItemId == x.Id).Sum(m => Sign(m.MovementType) * m.Quantity); return new InventoryItemDto(x.Id, x.Name, x.UnitOfMeasure, x.MinimumQuantity, quantity, quantity <= x.MinimumQuantity, x.TrackInventory); });
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementsAsync(Guid supplyItemId) => await db.SupplyInventoryMovements.AsNoTracking().Include(x => x.SupplyItem).Where(x => x.SupplyItemId == supplyItemId).OrderByDescending(x => x.OccurredAt).Select(x => new InventoryMovementDto(x.Id, x.SupplyItemId, x.SupplyItem.Name, x.MovementType, x.Quantity, x.UnitOfMeasure, x.OccurredAt, x.Reference, x.Notes, x.RecordedByUserId)).ToListAsync();

    public async Task<InventoryMovementDto> RecordAsync(InventoryMovementInput input, Guid? userId)
    {
        if (input.Quantity <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
        if (input.MovementType is SupplyInventoryMovementType.PurchaseReceipt or SupplyInventoryMovementType.PurchaseReturn) throw new ArgumentException("Este movimiento se genera desde compras o devoluciones.");
        var item = await db.SupplyItems.FirstOrDefaultAsync(x => x.Id == input.SupplyItemId && x.IsActive && x.TrackInventory) ?? throw new ArgumentException("El insumo no existe, está inactivo o no controla inventario.");
        SupplyInventoryLot? lot = null;
        if (input.LotId.HasValue) lot = await db.SupplyInventoryLots.FirstOrDefaultAsync(x => x.Id == input.LotId && x.SupplyItemId == item.Id && x.IsActive) ?? throw new ArgumentException("El lote no existe o no corresponde al insumo.");
        if (Sign(input.MovementType) < 0)
        {
            var existing = await db.SupplyInventoryMovements.Where(x => x.SupplyItemId == item.Id && (!input.LotId.HasValue || x.SupplyInventoryLotId == input.LotId)).Select(x => new { x.MovementType, x.Quantity }).ToListAsync();
            if (lot is not null) { var lotRemaining = lot.InitialQuantity + existing.Sum(x => Sign(x.MovementType) * x.Quantity); if (lotRemaining < input.Quantity) throw new InvalidOperationException("La cantidad excede el saldo del lote."); }
            if (existing.Sum(x => Sign(x.MovementType) * x.Quantity) < input.Quantity) throw new InvalidOperationException("La operación dejaría el inventario en negativo.");
            var protectedUnits = await db.CremationUrnReservations.CountAsync(x => x.SupplyItemId == item.Id && x.Status == pcms.Domain.Enums.UrnReservationStatus.Active); if ((input.Reference ?? string.Empty).StartsWith("CREMATION:", StringComparison.OrdinalIgnoreCase)) protectedUnits = Math.Max(0, protectedUnits - 1); if (existing.Sum(x => Sign(x.MovementType) * x.Quantity) - input.Quantity < protectedUnits) throw new InvalidOperationException("La operación dejaría insuficientes las unidades reservadas.");
        }
        var movement = new SupplyInventoryMovement { Id = Guid.NewGuid(), SupplyItemId = item.Id, SupplyInventoryLotId = input.LotId, MovementType = input.MovementType, Quantity = decimal.Round(input.Quantity, 3), UnitOfMeasure = item.UnitOfMeasure, OccurredAt = DateTime.UtcNow, Reference = input.Reference, Notes = input.Notes, RecordedByUserId = userId, CreatedAt = DateTime.UtcNow };
        db.SupplyInventoryMovements.Add(movement);
        await db.SaveChangesAsync();
        return new InventoryMovementDto(movement.Id, item.Id, item.Name, movement.MovementType, movement.Quantity, movement.UnitOfMeasure, movement.OccurredAt, movement.Reference, movement.Notes, movement.RecordedByUserId);
    }

    private static int Sign(SupplyInventoryMovementType type) => type is SupplyInventoryMovementType.ManualAdjustmentDecrease or SupplyInventoryMovementType.Consumption or SupplyInventoryMovementType.Waste or SupplyInventoryMovementType.PurchaseReturn ? -1 : 1;
}
