using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class CremationInventoryService(AppDbContext db) : ICremationInventoryService
{
    public async Task<CremationInventoryDto> GetAsync(Guid id)
    {
        var cremation = await db.Cremations.AsNoTracking().Include(x => x.Urn)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException("La cremación no existe.");
        return await Map(cremation);
    }

    public async Task<CremationInventoryDto> ReserveAsync(Guid id, Guid? userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{id:D}");

        var cremation = await db.Cremations.Include(x => x.Urn)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException("La cremación no existe.");
        if (!cremation.IncludesUrn || !cremation.UrnId.HasValue) return await Map(cremation);

        var mapping = await db.UrnSupplyItems.Include(x => x.SupplyItem)
            .FirstOrDefaultAsync(x =>
                x.UrnId == cremation.UrnId &&
                x.IsActive &&
                x.SupplyItem.IsActive &&
                x.SupplyItem.TrackInventory)
            ?? throw new InvalidOperationException("La urna no tiene un insumo terminado válido configurado.");

        await ledger.AcquireSupplyItemLocksAsync([mapping.SupplyItemId]);

        if (await db.CremationUrnReservations.AnyAsync(x =>
                x.CremationId == id && x.Status == UrnReservationStatus.Fulfilled))
            throw new InvalidOperationException("La urna ya fue entregada.");
        if (await db.CremationUrnReservations.AnyAsync(x =>
                x.CremationId == id && x.Status == UrnReservationStatus.Active))
            return await Map(cremation);

        var physicalStock = await ledger.GetPhysicalStockAsync(mapping.SupplyItemId);
        var protectedUnits = await db.CremationUrnReservations.CountAsync(x =>
            x.SupplyItemId == mapping.SupplyItemId && x.Status == UrnReservationStatus.Active);
        if (physicalStock - protectedUnits < 1)
            throw new InvalidOperationException("No hay urnas disponibles para reservar.");

        db.Add(new CremationUrnReservation
        {
            Id = Guid.NewGuid(),
            CremationId = id,
            UrnId = cremation.UrnId.Value,
            SupplyItemId = mapping.SupplyItemId,
            UrnNameSnapshot = cremation.Urn!.Name,
            SupplyItemNameSnapshot = mapping.SupplyItem.Name,
            SupplyItemScanCodeSnapshot = mapping.SupplyItem.ScanCode,
            Status = UrnReservationStatus.Active,
            ReservedAt = DateTime.UtcNow,
            ReservedByUserId = userId
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await Map(cremation);
    }

    public async Task<CremationInventoryDto> CancelAsync(Guid id, Guid? userId, string? reason)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{id:D}");

        var reservation = await db.CremationUrnReservations
            .FirstOrDefaultAsync(x => x.CremationId == id && x.Status == UrnReservationStatus.Active)
            ?? throw new InvalidOperationException("No existe una reserva activa.");
        await ledger.AcquireSupplyItemLocksAsync([reservation.SupplyItemId]);

        reservation.Status = UrnReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.CancelledByUserId = userId;
        reservation.CancellationReason = reason;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(id);
    }

    public async Task<CremationInventoryDto> FulfillAsync(
        Guid id,
        IReadOnlyCollection<FulfillmentMaterialInput> inputs,
        Guid? userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{id:D}");

        var cremation = await db.Cremations.Include(x => x.Urn)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException("La cremación no existe.");
        if (await db.CremationInventoryFulfillments.AnyAsync(x => x.CremationId == id))
            return await Map(cremation);

        var reservation = await db.CremationUrnReservations
            .FirstOrDefaultAsync(x => x.CremationId == id && x.Status == UrnReservationStatus.Active)
            ?? throw new InvalidOperationException("La cremación no tiene una reserva activa.");

        var materials = new List<MaterialRequest>();
        foreach (var group in inputs.GroupBy(x => x.SupplyItemId))
        {
            var lotIds = group.Select(x => x.LotId).Distinct().ToList();
            if (lotIds.Count > 1)
                throw new ArgumentException("Un material no puede consumirse desde varios lotes en la misma entrega.");

            materials.Add(new MaterialRequest(group.Key, group.Sum(x => x.Quantity), lotIds.SingleOrDefault()));
        }

        if (materials.Any(x => x.SupplyItemId == reservation.SupplyItemId))
            throw new ArgumentException("La urna reservada no debe registrarse también como material adicional.");
        if (materials.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Las cantidades deben ser mayores que cero.");

        await ledger.AcquireSupplyItemLocksAsync(
            materials.Select(x => x.SupplyItemId).Append(reservation.SupplyItemId));

        var fulfillment = new CremationInventoryFulfillment
        {
            Id = Guid.NewGuid(),
            CremationId = id,
            CremationUrnReservationId = reservation.Id,
            FulfilledAt = DateTime.UtcNow,
            FulfilledByUserId = userId
        };

        var urnMovement = await ledger.AppendValidatedAsync(
            reservation.SupplyItemId,
            SupplyInventoryMovementType.Consumption,
            1,
            SupplyInventoryMovementOrigin.CremationFulfillment,
            null,
            userId,
            $"CREMATION:{id}",
            "Entrega de urna reservada",
            reservation.Id,
            fulfillment.FulfilledAt,
            "unidad",
            cremationId: id);
        fulfillment.UrnMovementId = urnMovement.Id;
        db.Add(fulfillment);

        foreach (var material in materials)
        {
            var item = await db.SupplyItems.FirstOrDefaultAsync(x =>
                x.Id == material.SupplyItemId && x.IsActive && x.TrackInventory)
                ?? throw new ArgumentException("Material inválido.");
            var movement = await ledger.AppendValidatedAsync(
                item.Id,
                SupplyInventoryMovementType.Consumption,
                material.Quantity,
                SupplyInventoryMovementOrigin.CremationFulfillment,
                material.LotId,
                userId,
                $"CREMATION:{id}",
                "Material adicional de entrega",
                occurredAt: fulfillment.FulfilledAt,
                unitOfMeasure: "unidad",
                cremationId: id);

            fulfillment.Materials.Add(new CremationInventoryMaterial
            {
                Id = Guid.NewGuid(),
                FulfillmentId = fulfillment.Id,
                SupplyItemId = item.Id,
                LotId = material.LotId,
                SupplyItemNameSnapshot = item.Name,
                UnitOfMeasureSnapshot = item.UnitOfMeasure,
                Quantity = decimal.Round(material.Quantity, 3),
                InventoryMovementId = movement.Id
            });
        }

        reservation.Status = UrnReservationStatus.Fulfilled;
        reservation.FulfilledAt = fulfillment.FulfilledAt;
        reservation.FulfilledByUserId = userId;
        reservation.FulfillmentId = fulfillment.Id;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(id);
    }

    private async Task<CremationInventoryDto> Map(Cremation cremation)
    {
        var reservation = await db.CremationUrnReservations.AsNoTracking()
            .Where(x => x.CremationId == cremation.Id)
            .OrderByDescending(x => x.ReservedAt)
            .FirstOrDefaultAsync();
        var fulfillment = await db.CremationInventoryFulfillments.AsNoTracking()
            .Include(x => x.Materials)
            .FirstOrDefaultAsync(x => x.CremationId == cremation.Id);
        var supplyItemId = reservation?.SupplyItemId ??
            (cremation.UrnId.HasValue
                ? (await db.UrnSupplyItems.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.UrnId == cremation.UrnId && x.IsActive))?.SupplyItemId
                : null);
        var physicalStock = supplyItemId.HasValue
            ? await new InventoryLedger(db).GetPhysicalStockAsync(supplyItemId.Value)
            : 0;
        var reservedQuantity = supplyItemId.HasValue
            ? await db.CremationUrnReservations.CountAsync(x =>
                x.SupplyItemId == supplyItemId && x.Status == UrnReservationStatus.Active)
            : 0;
        var materials = fulfillment?.Materials.Select(x => new FulfillmentMaterialDto(
            x.SupplyItemId,
            x.SupplyItemNameSnapshot,
            x.Quantity,
            x.UnitOfMeasureSnapshot,
            x.LotId,
            x.InventoryMovementId)).ToList() ?? [];

        return new CremationInventoryDto(
            cremation.Id,
            cremation.UrnId,
            cremation.Urn?.Name,
            physicalStock,
            reservedQuantity,
            physicalStock - reservedQuantity,
            reservation is null
                ? null
                : new UrnReservationDto(
                    reservation.Id,
                    reservation.CremationId,
                    reservation.UrnId,
                    reservation.UrnNameSnapshot,
                    reservation.SupplyItemId,
                    reservation.SupplyItemNameSnapshot,
                    reservation.Status,
                    reservation.ReservedAt,
                    reservation.ReservedByUserId,
                    reservation.CancelledAt,
                    reservation.FulfilledAt),
            fulfillment?.Id,
            fulfillment?.FulfilledAt,
            materials);
    }

    private sealed record MaterialRequest(Guid SupplyItemId, decimal Quantity, Guid? LotId);
}
