using Microsoft.EntityFrameworkCore;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Domain.Inventory;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

internal sealed class InventoryLedger(AppDbContext db)
{
    private readonly HashSet<Guid> lockedSupplyItems = [];
    private readonly Dictionary<Guid, string?> recordedByDisplayNames = [];

    public async Task AcquireResourceLockAsync(string resource)
    {
        EnsureTransaction();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({resource}, 0))");
    }

    public async Task AcquireSupplyItemLocksAsync(IEnumerable<Guid> supplyItemIds)
    {
        foreach (var supplyItemId in supplyItemIds.Distinct().Order())
        {
            await AcquireResourceLockAsync($"pcms:inventory:item:{supplyItemId:D}");
            lockedSupplyItems.Add(supplyItemId);
        }
    }

    public async Task<decimal> GetPhysicalStockAsync(Guid supplyItemId)
    {
        var persistedMovements = await db.SupplyInventoryMovements
            .Where(x => x.SupplyItemId == supplyItemId)
            .Select(x => new { x.MovementType, x.Quantity })
            .ToListAsync();

        return persistedMovements.Sum(x => SupplyInventoryMovementEffects.Apply(x.MovementType, x.Quantity))
            + PendingEffect(x => x.SupplyItemId == supplyItemId);
    }

    public async Task<decimal> GetLotStockAsync(SupplyInventoryLot lot)
    {
        var persistedMovements = await db.SupplyInventoryMovements
            .Where(x => x.SupplyInventoryLotId == lot.Id)
            .Select(x => new { x.MovementType, x.Quantity })
            .ToListAsync();

        return lot.InitialQuantity
            + persistedMovements.Sum(x => SupplyInventoryMovementEffects.Apply(x.MovementType, x.Quantity))
            + PendingEffect(x => x.SupplyInventoryLotId == lot.Id);
    }

    public async Task<decimal> GetAllocatedLotStockAsync(Guid supplyItemId)
    {
        var lots = await db.SupplyInventoryLots
            .Where(x => x.SupplyItemId == supplyItemId)
            .Select(x => new { x.Id, x.InitialQuantity })
            .ToListAsync();
        if (lots.Count == 0) return 0;

        var lotIds = lots.Select(x => x.Id).ToHashSet();
        var persistedMovements = await db.SupplyInventoryMovements
            .Where(x => x.SupplyInventoryLotId.HasValue &&
                lotIds.Contains(x.SupplyInventoryLotId.Value))
            .Select(x => new { x.MovementType, x.Quantity })
            .ToListAsync();

        return lots.Sum(x => x.InitialQuantity)
            + persistedMovements.Sum(x => SupplyInventoryMovementEffects.Apply(x.MovementType, x.Quantity))
            + PendingEffect(x => x.SupplyInventoryLotId.HasValue &&
                lotIds.Contains(x.SupplyInventoryLotId.Value));
    }

    public async Task<SupplyInventoryMovement> AppendValidatedAsync(
        Guid supplyItemId,
        SupplyInventoryMovementType movementType,
        decimal quantity,
        SupplyInventoryMovementOrigin origin,
        Guid? lotId,
        Guid? recordedByUserId,
        string? reference,
        string? notes,
        Guid? reservationIdToConsume = null,
        DateTime? occurredAt = null,
        string? unitOfMeasure = null,
        Guid? cremationId = null,
        string? scannedCode = null,
        SupplyInventoryReasonCode? reasonCode = null,
        Guid? clientOperationId = null)
    {
        EnsureTransaction();
        if (!lockedSupplyItems.Contains(supplyItemId))
            throw new InvalidOperationException("El insumo debe bloquearse antes de registrar el movimiento.");

        var roundedQuantity = decimal.Round(quantity, 3);
        if (roundedQuantity <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        if (reasonCode.HasValue && !Enum.IsDefined(reasonCode.Value))
            throw new ArgumentOutOfRangeException(nameof(reasonCode));
        if (scannedCode?.Length > 150)
            throw new ArgumentException("El código escaneado excede la longitud permitida.");

        var direction = SupplyInventoryMovementEffects.Direction(movementType);
        var item = await db.SupplyItems.FirstOrDefaultAsync(x =>
            x.Id == supplyItemId && x.IsActive && x.TrackInventory)
            ?? throw new ArgumentException("El insumo no existe, está inactivo o no controla inventario.");

        SupplyInventoryLot? lot = null;
        if (lotId.HasValue)
        {
            lot = await db.SupplyInventoryLots.FirstOrDefaultAsync(x =>
                x.Id == lotId && x.SupplyItemId == supplyItemId && x.IsActive)
                ?? throw new ArgumentException("El lote no existe o no corresponde al insumo.");
        }

        if (direction < 0)
        {
            var physicalStock = await GetPhysicalStockAsync(supplyItemId);
            if (physicalStock < roundedQuantity)
                throw new InvalidOperationException("La operación dejaría el inventario en negativo.");

            if (lot is not null && await GetLotStockAsync(lot) < roundedQuantity)
                throw new InvalidOperationException("La cantidad excede el saldo del lote.");

            if (reservationIdToConsume.HasValue && !await db.CremationUrnReservations.AnyAsync(x =>
                    x.Id == reservationIdToConsume &&
                    x.SupplyItemId == supplyItemId &&
                    x.Status == UrnReservationStatus.Active))
                throw new InvalidOperationException("La reserva que se intenta consumir no está activa o no corresponde al insumo.");

            var protectedUnits = await db.CremationUrnReservations.CountAsync(x =>
                x.SupplyItemId == supplyItemId &&
                x.Status == UrnReservationStatus.Active &&
                (!reservationIdToConsume.HasValue || x.Id != reservationIdToConsume));

            if (physicalStock - roundedQuantity < protectedUnits)
                throw new InvalidOperationException("La operación dejaría insuficientes las unidades reservadas.");
        }

        var movement = new SupplyInventoryMovement
        {
            Id = Guid.NewGuid(),
            SupplyItemId = item.Id,
            SupplyItemNameSnapshot = item.Name,
            SupplyInventoryLotId = lotId,
            MovementType = movementType,
            Origin = origin,
            ReasonCode = reasonCode,
            ScannedCode = scannedCode,
            ClientOperationId = clientOperationId,
            Quantity = roundedQuantity,
            UnitOfMeasure = unitOfMeasure ?? item.UnitOfMeasure,
            OccurredAt = occurredAt ?? DateTime.UtcNow,
            Reference = reference,
            Notes = notes,
            CremationId = cremationId,
            RecordedByUserId = recordedByUserId,
            RecordedByDisplayNameSnapshot = await GetRecordedByDisplayNameSnapshotAsync(recordedByUserId),
            CreatedAt = DateTime.UtcNow
        };

        db.SupplyInventoryMovements.Add(movement);
        return movement;
    }

    public async Task<string?> GetRecordedByDisplayNameSnapshotAsync(Guid? userId)
    {
        if (!userId.HasValue) return null;
        if (recordedByDisplayNames.TryGetValue(userId.Value, out var cachedName)) return cachedName;

        var userName = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => new { x.FirstName, x.LastName })
            .SingleOrDefaultAsync();
        var displayName = userName is null
            ? null
            : string.Join(" ", new[] { userName.FirstName, userName.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        recordedByDisplayNames[userId.Value] = displayName;
        return displayName;
    }

    private decimal PendingEffect(Func<SupplyInventoryMovement, bool> predicate) =>
        db.ChangeTracker.Entries<SupplyInventoryMovement>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .Where(predicate)
            .Sum(x => SupplyInventoryMovementEffects.Apply(x.MovementType, x.Quantity));

    private void EnsureTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La operación de inventario requiere una transacción activa.");
    }
}
