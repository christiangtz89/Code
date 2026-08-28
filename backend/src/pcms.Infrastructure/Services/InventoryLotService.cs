using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class InventoryLotService(AppDbContext db) : IInventoryLotService
{
    public async Task<IEnumerable<LotDto>> GetAsync(string? search)
    {
        var query = db.SupplyInventoryLots.AsNoTracking()
            .Include(x => x.SupplyItem)
            .Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.ScanCode.Contains(search) || x.SupplyItem.Name.Contains(search));

        var lots = await query.OrderBy(x => x.SupplyItem.Name).ThenBy(x => x.ScanCode).ToListAsync();
        return await Task.WhenAll(lots.Select(Map));
    }

    public async Task<LotDto?> GetByScanAsync(string code)
    {
        var lot = await db.SupplyInventoryLots.AsNoTracking()
            .Include(x => x.SupplyItem)
            .FirstOrDefaultAsync(x => x.ScanCode == code.Trim() && x.IsActive);
        return lot is null ? null : await Map(lot);
    }

    public async Task<LotDto> CreateAsync(LotInput input)
    {
        var initialQuantity = decimal.Round(input.InitialQuantity, 3);
        if (initialQuantity <= 0 || string.IsNullOrWhiteSpace(input.ScanCode))
            throw new ArgumentException("Código y cantidad inicial son obligatorios.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireSupplyItemLocksAsync([input.SupplyItemId]);

        var item = await db.SupplyItems.FirstOrDefaultAsync(x =>
            x.Id == input.SupplyItemId && x.IsActive && x.TrackInventory)
            ?? throw new ArgumentException("El insumo no existe o no controla inventario.");
        var scanCode = input.ScanCode.Trim();
        if (await db.SupplyInventoryLots.AnyAsync(x => x.ScanCode == scanCode))
            throw new InvalidOperationException("El código de lote ya existe.");

        var physicalStock = decimal.Round(await ledger.GetPhysicalStockAsync(item.Id), 3);
        var allocatedLotStock = decimal.Round(await ledger.GetAllocatedLotStockAsync(item.Id), 3);
        if (allocatedLotStock > physicalStock)
            throw new InvalidOperationException("La cantidad asignada a lotes excede el inventario físico disponible.");

        var unallocatedPhysicalStock = decimal.Round(physicalStock - allocatedLotStock, 3);
        if (initialQuantity > unallocatedPhysicalStock)
            throw new InvalidOperationException("La cantidad inicial excede el inventario físico sin asignar a lotes.");

        var lot = new SupplyInventoryLot
        {
            Id = Guid.NewGuid(),
            SupplyItemId = item.Id,
            SupplyItem = item,
            ScanCode = scanCode,
            ManufacturerLotNumber = input.ManufacturerLotNumber,
            InitialQuantity = initialQuantity,
            UnitOfMeasure = item.UnitOfMeasure,
            ReceivedAt = input.ReceivedAt ?? DateTime.UtcNow,
            Notes = input.Notes,
            CreatedAt = DateTime.UtcNow
        };
        db.Add(lot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await Map(lot);
    }

    private async Task<LotDto> Map(SupplyInventoryLot lot)
    {
        var remaining = await new InventoryLedger(db).GetLotStockAsync(lot);
        return new LotDto(
            lot.Id,
            lot.SupplyItemId,
            lot.SupplyItem.Name,
            lot.ScanCode,
            lot.ManufacturerLotNumber,
            lot.InitialQuantity,
            remaining,
            lot.UnitOfMeasure,
            lot.ReceivedAt,
            lot.IsActive);
    }
}
