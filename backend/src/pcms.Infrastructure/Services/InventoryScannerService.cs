using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class InventoryScannerService(AppDbContext db) : IInventoryScannerService
{
    public async Task<InventoryScannerResolveDto> ResolveAsync(
        string scanCode,
        bool canRecordOutgoing)
    {
        var normalizedCode = NormalizeScanCode(scanCode);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        var preliminaryIdentity = await ResolveIdentityAsync(normalizedCode);
        await ledger.AcquireSupplyItemLocksAsync([preliminaryIdentity.SupplyItemId]);
        var identity = await ResolveIdentityAsync(normalizedCode);
        if (identity.SupplyItemId != preliminaryIdentity.SupplyItemId ||
            identity.LotId != preliminaryIdentity.LotId)
            throw new InventoryScannerConflictException(
                "El código cambió mientras se preparaba la consulta. Escanéalo nuevamente.");

        var result = await BuildResolveDtoAsync(identity, canRecordOutgoing, ledger);
        await transaction.CommitAsync();
        return result;
    }

    public async Task<InventoryScannerOutgoingDto> RecordOutgoingAsync(
        InventoryScannerOutgoingInput input,
        Guid userId)
    {
        var normalizedCode = NormalizeScanCode(input.ScanCode);
        var quantity = decimal.Round(input.Quantity, 3);
        var notes = NormalizeOptional(input.Notes);
        ValidateOutgoingInput(input, quantity, notes);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync(
            $"pcms:inventory:scanner-operation:{input.ClientOperationId:D}");

        var employee = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (employee is null || !employee.IsActive)
            throw new UnauthorizedAccessException("El usuario ya no está activo.");

        var existingMovement = await db.SupplyInventoryMovements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientOperationId == input.ClientOperationId);
        if (existingMovement is not null)
        {
            EnsureEquivalentReplay(existingMovement, input, userId, normalizedCode, quantity, notes);
            await ledger.AcquireSupplyItemLocksAsync([existingMovement.SupplyItemId]);
            var replayResult = await BuildResultAsync(existingMovement, ledger);
            await transaction.CommitAsync();
            return replayResult;
        }

        var preliminaryIdentity = await ResolveIdentityAsync(normalizedCode);
        await ledger.AcquireSupplyItemLocksAsync([preliminaryIdentity.SupplyItemId]);
        var identity = await ResolveIdentityAsync(normalizedCode);
        if (identity.SupplyItemId != preliminaryIdentity.SupplyItemId ||
            identity.LotId != preliminaryIdentity.LotId)
            throw new InventoryScannerConflictException(
                "El código cambió mientras se preparaba la operación. Escanéalo nuevamente.");

        var currentState = await BuildResolveDtoAsync(identity, true, ledger);
        if (identity.SupplyItemId != input.SupplyItemId)
            throw new InventoryScannerConflictException(
                "El insumo identificado cambió desde la vista previa. Revisa y confirma nuevamente.",
                currentState);

        var selectedLot = await ValidateLotSelectionAsync(
            currentState,
            input.SupplyInventoryLotId,
            input.ExpectedLotStock);
        EnsurePreviewIsCurrent(input, currentState, selectedLot);
        EnsureQuantityIsAvailable(quantity, currentState, selectedLot);

        var movement = await ledger.AppendValidatedAsync(
            currentState.SupplyItemId,
            SupplyInventoryMovementType.Consumption,
            quantity,
            SupplyInventoryMovementOrigin.Scanner,
            selectedLot?.Id,
            userId,
            null,
            notes,
            scannedCode: normalizedCode,
            reasonCode: input.ReasonCode,
            clientOperationId: input.ClientOperationId);

        var result = await BuildResultAsync(movement, ledger, selectedLot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<ScanIdentity> ResolveIdentityAsync(string normalizedCode)
    {
        var itemMatch = await db.SupplyItems.AsNoTracking()
            .Where(x => x.ScanCode == normalizedCode)
            .Select(x => new { x.Id })
            .SingleOrDefaultAsync();
        var lotMatch = await db.SupplyInventoryLots.AsNoTracking()
            .Where(x => x.ScanCode == normalizedCode)
            .Select(x => new { x.Id, x.SupplyItemId })
            .SingleOrDefaultAsync();

        if (itemMatch is not null && lotMatch is not null)
            throw new InventoryScannerConflictException(
                "El código coincide con un insumo y un lote. Corrige la duplicidad antes de continuar.");
        if (itemMatch is null && lotMatch is null)
            throw new KeyNotFoundException("Código no encontrado.");

        return itemMatch is not null
            ? new ScanIdentity(itemMatch.Id, null, normalizedCode)
            : new ScanIdentity(lotMatch!.SupplyItemId, lotMatch.Id, normalizedCode);
    }

    private async Task<InventoryScannerResolveDto> BuildResolveDtoAsync(
        ScanIdentity identity,
        bool canRecordOutgoing,
        InventoryLedger? existingLedger = null)
    {
        var item = await db.SupplyItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == identity.SupplyItemId)
            ?? throw new KeyNotFoundException("El insumo asociado ya no existe.");
        var ledger = existingLedger ?? new InventoryLedger(db);
        var physicalStock = Round(await ledger.GetPhysicalStockAsync(item.Id));
        var reservedQuantity = await db.CremationUrnReservations.CountAsync(x =>
            x.SupplyItemId == item.Id && x.Status == UrnReservationStatus.Active);
        var availableStock = Round(Math.Max(0, physicalStock - reservedQuantity));
        var unallocatedPhysicalStock = Round(
            physicalStock - await ledger.GetAllocatedLotStockAsync(item.Id));

        var lotEntities = await db.SupplyInventoryLots.AsNoTracking()
            .Where(x => x.SupplyItemId == item.Id)
            .OrderBy(x => x.ReceivedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();
        var lots = new List<InventoryScannerLotDto>();
        foreach (var lot in lotEntities)
        {
            var remaining = Round(await ledger.GetLotStockAsync(lot));
            if (remaining > 0 || lot.Id == identity.LotId)
                lots.Add(new InventoryScannerLotDto(
                    lot.Id,
                    lot.ScanCode,
                    remaining,
                    lot.IsActive,
                    lot.IsActive && remaining > 0));
        }

        var scannedLot = identity.LotId.HasValue
            ? lots.FirstOrDefault(x => x.Id == identity.LotId.Value)
            : null;
        var eligibleLots = lots.Where(x => x.IsEligible).ToList();
        var canUseCurrentIdentity = identity.LotId.HasValue
            ? scannedLot is { IsEligible: true }
            : eligibleLots.Count > 0 || unallocatedPhysicalStock > 0;

        return new InventoryScannerResolveDto(
            item.Id,
            item.Name,
            identity.ScanCode,
            identity.LotId.HasValue,
            scannedLot?.Id,
            scannedLot?.ScanCode,
            scannedLot?.RemainingQuantity,
            scannedLot?.IsActive,
            item.UnitOfMeasure,
            physicalStock,
            reservedQuantity,
            availableStock,
            unallocatedPhysicalStock,
            lots,
            !identity.LotId.HasValue && eligibleLots.Count > 1,
            !identity.LotId.HasValue && eligibleLots.Count == 1
                ? eligibleLots[0].Id
                : null,
            !identity.LotId.HasValue && eligibleLots.Count > 0 && unallocatedPhysicalStock > 0,
            canRecordOutgoing && item.IsActive && item.TrackInventory &&
                availableStock > 0 && canUseCurrentIdentity);
    }

    private async Task<InventoryScannerLotDto?> ValidateLotSelectionAsync(
        InventoryScannerResolveDto state,
        Guid? requestedLotId,
        decimal? expectedLotStock)
    {
        if (state.IsLotScan)
        {
            if (!state.ScannedLotId.HasValue || requestedLotId != state.ScannedLotId)
                throw new ArgumentException("La salida debe usar el lote escaneado.");
        }
        else
        {
            var eligibleLots = state.Lots.Where(x => x.IsEligible).ToList();
            if (eligibleLots.Count > 0 && !requestedLotId.HasValue)
                throw new ArgumentException("Selecciona explícitamente el lote que se consumirá.");
        }

        if (!requestedLotId.HasValue) return null;
        var currentLot = state.Lots.FirstOrDefault(x => x.Id == requestedLotId);
        if (currentLot is { IsEligible: true }) return currentLot;

        var belongsToItem = currentLot is not null || await db.SupplyInventoryLots
            .AsNoTracking()
            .AnyAsync(x => x.Id == requestedLotId && x.SupplyItemId == state.SupplyItemId);
        if (!belongsToItem)
            throw new ArgumentException("El lote seleccionado no corresponde al insumo.");

        if (expectedLotStock.HasValue && Round(expectedLotStock.Value) > 0)
            throw new InventoryScannerConflictException(
                "El lote cambió desde la vista previa. Revisa y confirma nuevamente.",
                state);

        throw new ArgumentException("El lote seleccionado no está activo o no tiene existencia.");
    }

    private static void EnsurePreviewIsCurrent(
        InventoryScannerOutgoingInput input,
        InventoryScannerResolveDto state,
        InventoryScannerLotDto? selectedLot)
    {
        var stale = Round(input.ExpectedPhysicalStock) != state.PhysicalStock ||
            Round(input.ExpectedAvailableStock) != state.AvailableStock;

        if (selectedLot is not null)
            stale |= !input.ExpectedLotStock.HasValue ||
                Round(input.ExpectedLotStock.Value) != selectedLot.RemainingQuantity;
        else
            stale |= !input.ExpectedUnallocatedStock.HasValue ||
                Round(input.ExpectedUnallocatedStock.Value) != state.UnallocatedPhysicalStock;

        if (stale)
            throw new InventoryScannerConflictException(
                "El inventario cambió desde la vista previa. Revisa y confirma nuevamente.",
                state);
    }

    private static void EnsureQuantityIsAvailable(
        decimal quantity,
        InventoryScannerResolveDto state,
        InventoryScannerLotDto? selectedLot)
    {
        if (!state.CanRecordOutgoing || quantity > state.AvailableStock)
            throw new InventoryScannerConflictException(
                "La cantidad excede la existencia disponible.",
                state);
        if (selectedLot is not null && quantity > selectedLot.RemainingQuantity)
            throw new InventoryScannerConflictException(
                "La cantidad excede el saldo del lote.",
                state);
        if (selectedLot is null && quantity > state.UnallocatedPhysicalStock)
            throw new InventoryScannerConflictException(
                "La cantidad excede la existencia física sin lote.",
                state);
    }

    private async Task<InventoryScannerOutgoingDto> BuildResultAsync(
        SupplyInventoryMovement movement,
        InventoryLedger ledger,
        InventoryScannerLotDto? selectedLot = null)
    {
        var physicalStock = Round(await ledger.GetPhysicalStockAsync(movement.SupplyItemId));
        var reservedQuantity = await db.CremationUrnReservations.CountAsync(x =>
            x.SupplyItemId == movement.SupplyItemId && x.Status == UrnReservationStatus.Active);
        decimal? currentLotStock = movement.SupplyInventoryLotId.HasValue
            ? Round(await ledger.GetLotStockAsync(
                await db.SupplyInventoryLots.AsNoTracking().SingleAsync(x =>
                    x.Id == movement.SupplyInventoryLotId.Value)))
            : null;
        var currentLotScanCode = selectedLot?.ScanCode;
        if (currentLotScanCode is null && movement.SupplyInventoryLotId.HasValue)
            currentLotScanCode = await db.SupplyInventoryLots.AsNoTracking()
                .Where(x => x.Id == movement.SupplyInventoryLotId.Value)
                .Select(x => x.ScanCode)
                .SingleAsync();

        return new InventoryScannerOutgoingDto(
            movement.Id,
            movement.SupplyItemId,
            movement.SupplyItemNameSnapshot,
            movement.ScannedCode ?? string.Empty,
            movement.Quantity,
            movement.UnitOfMeasure,
            movement.SupplyInventoryLotId,
            currentLotScanCode,
            physicalStock,
            Round(Math.Max(0, physicalStock - reservedQuantity)),
            currentLotStock,
            movement.OccurredAt,
            movement.ReasonCode!.Value,
            movement.Notes,
            movement.RecordedByDisplayNameSnapshot);
    }

    private static void EnsureEquivalentReplay(
        SupplyInventoryMovement movement,
        InventoryScannerOutgoingInput input,
        Guid userId,
        string normalizedCode,
        decimal quantity,
        string? notes)
    {
        var equivalent = movement.Origin == SupplyInventoryMovementOrigin.Scanner &&
            movement.MovementType == SupplyInventoryMovementType.Consumption &&
            movement.CremationId is null &&
            movement.RecordedByUserId == userId &&
            movement.SupplyItemId == input.SupplyItemId &&
            movement.ScannedCode == normalizedCode &&
            movement.Quantity == quantity &&
            movement.SupplyInventoryLotId == input.SupplyInventoryLotId &&
            movement.ReasonCode == input.ReasonCode &&
            movement.Notes == notes;
        if (!equivalent)
            throw new InventoryScannerConflictException(
                "El identificador de operación ya se utilizó con datos diferentes.");
    }

    private static void ValidateOutgoingInput(
        InventoryScannerOutgoingInput input,
        decimal quantity,
        string? notes)
    {
        if (input.ClientOperationId == Guid.Empty)
            throw new ArgumentException("El identificador de operación es obligatorio.");
        if (input.SupplyItemId == Guid.Empty)
            throw new ArgumentException("El insumo esperado es obligatorio.");
        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor que cero.");
        if (!Enum.IsDefined(input.ReasonCode))
            throw new ArgumentException("El motivo de salida no es válido.");
        if (input.ReasonCode == SupplyInventoryReasonCode.SalidaManualAutorizada && notes is null)
            throw new ArgumentException("Explica el motivo de la salida manual autorizada.");
    }

    private static string NormalizeScanCode(string? scanCode)
    {
        var normalizedCode = scanCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            throw new ArgumentException("El código escaneado es obligatorio.");
        if (normalizedCode.Length > 150)
            throw new ArgumentException("El código escaneado excede la longitud permitida.");
        return normalizedCode;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Round(decimal value) => decimal.Round(value, 3);

    private sealed record ScanIdentity(Guid SupplyItemId, Guid? LotId, string ScanCode);
}
