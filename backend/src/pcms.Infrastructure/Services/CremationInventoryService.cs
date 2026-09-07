using Microsoft.EntityFrameworkCore;
using pcms.Application.Inventory;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class CremationInventoryService(AppDbContext db) :
    ICremationInventoryService,
    ICremationScannerService
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

        await AppendFulfillmentAsync(
            cremation,
            reservation,
            materials,
            userId,
            ledger,
            urnLotId: null,
            urnScannedCode: null);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(id);
    }

    public async Task<CremationScannerPreviewDto> GetPreviewAsync(Guid cremationId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{cremationId:D}");

        var cremation = await LoadScannerCremationAsync(cremationId, tracking: false);
        var preview = await BuildScannerPreviewAsync(cremation);
        await transaction.CommitAsync();
        return preview;
    }

    public async Task<CremationScannerResolveDto> ResolveAsync(
        Guid cremationId,
        string scanCode,
        CremationScannerMatchKind expectedKind,
        CremationStatus expectedCremationStatus,
        DateTime expectedReservationReservedAt,
        Guid expectedUrnSupplyItemId,
        string expectedUrnScanCode)
    {
        if (!Enum.IsDefined(expectedKind))
            throw new ArgumentException("El tipo de escaneo esperado no es válido.");
        var normalizedCode = NormalizeScanCode(scanCode);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{cremationId:D}");

        var cremation = await LoadScannerCremationAsync(cremationId, tracking: false);
        var preview = await BuildScannerPreviewAsync(cremation);
        var normalizedExpectedUrnCode = NormalizeScanCode(expectedUrnScanCode);
        if (preview.CremationStatus != expectedCremationStatus ||
            preview.ReservedUrn?.ReservedAt != expectedReservationReservedAt ||
            preview.ReservedUrn?.SupplyItemId != expectedUrnSupplyItemId ||
            !string.Equals(
                preview.ReservedUrn.ExpectedScanCode,
                normalizedExpectedUrnCode,
                StringComparison.Ordinal))
            throw new CremationScannerConflictException(
                "La entrega cambió desde la vista previa. Revisa y confirma nuevamente.",
                preview);
        var reservation = await db.CremationUrnReservations.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.CremationId == cremationId && x.Status == UrnReservationStatus.Active);

        var preliminaryIdentity = await ResolveScanIdentityAsync(normalizedCode);
        await ledger.AcquireSupplyItemLocksAsync([preliminaryIdentity.SupplyItemId]);
        var identity = await ResolveScanIdentityAsync(normalizedCode);
        if (identity.SupplyItemId != preliminaryIdentity.SupplyItemId ||
            identity.LotId != preliminaryIdentity.LotId)
            throw new CremationScannerConflictException(
                "El código cambió mientras se preparaba la consulta. Escanéalo nuevamente.",
                preview);

        var result = await BuildCremationScanResolveAsync(
            cremationId,
            identity,
            reservation,
            preview,
            ledger);
        if (result.MatchKind != expectedKind)
            result = result with
            {
                CanUse = false,
                RejectionReason = expectedKind == CremationScannerMatchKind.ReservedUrn
                    ? "El código escaneado no corresponde a la urna reservada."
                    : "El código escaneado no corresponde a un material adicional válido."
            };
        await transaction.CommitAsync();
        return result;
    }

    public async Task<CremationScannerPreviewDto> FulfillAsync(
        Guid cremationId,
        CremationScannerFulfillInput input,
        Guid userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ledger = new InventoryLedger(db);
        await ledger.AcquireResourceLockAsync($"pcms:cremation:{cremationId:D}");

        var employeeIsActive = await db.Users.AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive);
        if (!employeeIsActive)
            throw new UnauthorizedAccessException("El usuario ya no está activo.");

        var cremation = await LoadScannerCremationAsync(cremationId, tracking: true);
        var currentPreview = await BuildScannerPreviewAsync(cremation);
        if (currentPreview.IsFulfilled)
        {
            await transaction.CommitAsync();
            return currentPreview;
        }

        if (!currentPreview.CanFulfill)
            throw new CremationScannerConflictException(
                currentPreview.BlockingReason ?? "La entrega no puede registrarse en el estado actual.",
                currentPreview);
        if (input.ExpectedCremationStatus != cremation.Status)
            throw new CremationScannerConflictException(
                "El estado de la cremación cambió. Revisa y confirma nuevamente.",
                currentPreview);

        var reservation = await db.CremationUrnReservations.FirstOrDefaultAsync(x =>
            x.CremationId == cremationId && x.Status == UrnReservationStatus.Active);
        if (reservation is null)
            throw new CremationScannerConflictException(
                "La reserva de urna cambió. Revisa y confirma nuevamente.",
                currentPreview);
        if (reservation.ReservedAt != input.ExpectedReservationReservedAt)
            throw new CremationScannerConflictException(
                "La reserva de urna cambió. Revisa y confirma nuevamente.",
                currentPreview);
        if (cremation.UrnId != reservation.UrnId)
            throw new CremationScannerConflictException(
                "La urna asignada cambió. Revisa y confirma nuevamente.",
                currentPreview);

        var urnInput = input.Urn
            ?? throw new ArgumentException("Escanea la urna reservada antes de confirmar.");
        var materialInputs = input.Materials?.ToList() ?? [];
        if (urnInput.SupplyItemId != reservation.SupplyItemId)
            throw new CremationScannerConflictException(
                "La reserva de urna cambió. Revisa y confirma nuevamente.",
                currentPreview);

        var submittedInputs = new[] { urnInput }.Concat(materialInputs).ToList();
        var preliminarySelections = new List<ResolvedSelection>();
        foreach (var selection in submittedInputs)
        {
            var normalizedCode = NormalizeScanCode(selection.ScanCode);
            var quantity = Round(selection.Quantity);
            if (selection.SupplyItemId == Guid.Empty)
                throw new ArgumentException("El insumo esperado es obligatorio.");
            if (quantity <= 0)
                throw new ArgumentException("Las cantidades deben ser mayores que cero.");

            preliminarySelections.Add(new ResolvedSelection(
                selection,
                normalizedCode,
                quantity,
                await ResolveScanIdentityForConfirmationAsync(
                    normalizedCode,
                    currentPreview)));
        }

        if (preliminarySelections[0].Quantity != 1)
            throw new ArgumentException("La entrega debe consumir exactamente una urna reservada.");
        if (materialInputs.GroupBy(x => x.SupplyItemId).Any(x => x.Count() > 1))
            throw new ArgumentException("Cada material debe aparecer una sola vez; ajusta su cantidad en una sola línea.");
        if (materialInputs.Any(x => x.SupplyItemId == reservation.SupplyItemId))
            throw new ArgumentException("La urna reservada no debe registrarse también como material adicional.");

        await ledger.AcquireSupplyItemLocksAsync(
            preliminarySelections.Select(x => x.Identity.SupplyItemId)
                .Append(reservation.SupplyItemId));

        var validatedSelections = new List<ValidatedSelection>();
        foreach (var preliminary in preliminarySelections)
        {
            var identity = await ResolveScanIdentityForConfirmationAsync(
                preliminary.NormalizedCode,
                currentPreview);
            if (identity.SupplyItemId != preliminary.Identity.SupplyItemId ||
                identity.LotId != preliminary.Identity.LotId ||
                identity.SupplyItemId != preliminary.Input.SupplyItemId)
                throw new CremationScannerConflictException(
                    "La identidad de un código cambió. Revisa y confirma nuevamente.",
                    currentPreview);

            var state = await BuildCremationScanResolveAsync(
                cremationId,
                identity,
                reservation,
                currentPreview,
                ledger);
            var selectedLot = await ValidateScannerSelectionAsync(
                preliminary.Input,
                preliminary.Quantity,
                state,
                currentPreview);
            validatedSelections.Add(new ValidatedSelection(
                preliminary,
                state,
                selectedLot));
        }

        var urnSelection = validatedSelections[0];
        if (urnSelection.State.MatchKind != CremationScannerMatchKind.ReservedUrn)
            throw new ArgumentException("El código escaneado no corresponde a la urna reservada.");

        var materials = new List<MaterialRequest>();
        foreach (var material in validatedSelections.Skip(1))
        {
            if (material.State.MatchKind != CremationScannerMatchKind.AdditionalMaterial)
                throw new ArgumentException("El código escaneado no corresponde a un material adicional válido.");
            materials.Add(new MaterialRequest(
                material.State.SupplyItemId,
                material.Selection.Quantity,
                material.SelectedLot?.Id,
                material.Selection.NormalizedCode));
        }

        await AppendFulfillmentAsync(
            cremation,
            reservation,
            materials,
            userId,
            ledger,
            urnSelection.SelectedLot?.Id,
            urnSelection.Selection.NormalizedCode);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetPreviewAsync(cremationId);
    }

    private async Task AppendFulfillmentAsync(
        Cremation cremation,
        CremationUrnReservation reservation,
        IReadOnlyCollection<MaterialRequest> materials,
        Guid? userId,
        InventoryLedger ledger,
        Guid? urnLotId,
        string? urnScannedCode)
    {
        var fulfillment = new CremationInventoryFulfillment
        {
            Id = Guid.NewGuid(),
            CremationId = cremation.Id,
            CremationUrnReservationId = reservation.Id,
            FulfilledAt = DateTime.UtcNow,
            FulfilledByUserId = userId
        };

        var urnMovement = await ledger.AppendValidatedAsync(
            reservation.SupplyItemId,
            SupplyInventoryMovementType.Consumption,
            1,
            SupplyInventoryMovementOrigin.CremationFulfillment,
            urnLotId,
            userId,
            $"CREMATION:{cremation.Id}",
            "Entrega de urna reservada",
            reservation.Id,
            fulfillment.FulfilledAt,
            "unidad",
            cremationId: cremation.Id,
            scannedCode: urnScannedCode);
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
                $"CREMATION:{cremation.Id}",
                "Material adicional de entrega",
                occurredAt: fulfillment.FulfilledAt,
                unitOfMeasure: "unidad",
                cremationId: cremation.Id,
                scannedCode: material.ScannedCode);

            fulfillment.Materials.Add(new CremationInventoryMaterial
            {
                Id = Guid.NewGuid(),
                FulfillmentId = fulfillment.Id,
                SupplyItemId = item.Id,
                LotId = material.LotId,
                SupplyItemNameSnapshot = item.Name,
                UnitOfMeasureSnapshot = item.UnitOfMeasure,
                Quantity = Round(material.Quantity),
                InventoryMovementId = movement.Id
            });
        }

        reservation.Status = UrnReservationStatus.Fulfilled;
        reservation.FulfilledAt = fulfillment.FulfilledAt;
        reservation.FulfilledByUserId = userId;
        reservation.FulfillmentId = fulfillment.Id;
    }

    private async Task<Cremation> LoadScannerCremationAsync(Guid id, bool tracking)
    {
        IQueryable<Cremation> query = db.Cremations
            .Include(x => x.Urn)
            .Include(x => x.Reception)
                .ThenInclude(x => x.Pet)
                    .ThenInclude(x => x.Customer);
        if (!tracking) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("La cremación no existe.");
    }

    private async Task<CremationScannerPreviewDto> BuildScannerPreviewAsync(
        Cremation cremation)
    {
        var reservation = await db.CremationUrnReservations.AsNoTracking()
            .Where(x => x.CremationId == cremation.Id)
            .OrderByDescending(x => x.ReservedAt)
            .FirstOrDefaultAsync();
        var fulfillment = await db.CremationInventoryFulfillments.AsNoTracking()
            .Include(x => x.Materials)
            .FirstOrDefaultAsync(x => x.CremationId == cremation.Id);
        var reservedItem = reservation is null
            ? null
            : await db.SupplyItems.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == reservation.SupplyItemId);

        var isFulfilled = fulfillment is not null;
        var statusAllowsDelivery = cremation.Status is
            CremationStatus.ReadyForDelivery or CremationStatus.Delivered;
        string? blockingReason = null;
        if (isFulfilled)
            blockingReason = "Entrega ya registrada.";
        else if (!cremation.IsActive || cremation.Status == CremationStatus.Cancelled)
            blockingReason = "La cremación no está activa para entrega.";
        else if (!statusAllowsDelivery)
            blockingReason = "La cremación debe estar lista para entrega.";
        else if (!cremation.IncludesUrn || !cremation.UrnId.HasValue)
            blockingReason = "La cremación no tiene una urna de inventario asignada.";
        else if (reservation is null || reservation.Status != UrnReservationStatus.Active)
            blockingReason = "La cremación no tiene una reserva de urna activa.";
        else if (reservation.UrnId != cremation.UrnId)
            blockingReason = "La reserva no corresponde a la urna asignada actualmente.";
        else if (reservedItem is null || !reservedItem.IsActive || !reservedItem.TrackInventory)
            blockingReason = "El insumo de la urna reservada no está disponible para inventario.";

        var completedMaterials = fulfillment?.Materials
            .Select(x => new CremationScannerCompletedMaterialDto(
                x.SupplyItemNameSnapshot,
                x.Quantity,
                x.UnitOfMeasureSnapshot))
            .ToList() ?? [];

        return new CremationScannerPreviewDto(
            cremation.Id,
            cremation.Reception.QrCode,
            cremation.Reception.PetNameSnapshot,
            cremation.Reception.CustomerNameSnapshot,
            cremation.Status,
            isFulfilled,
            fulfillment?.Id,
            fulfillment?.FulfilledAt,
            reservation is null
                ? null
                : new CremationScannerReservedUrnDto(
                    reservation.SupplyItemId,
                    reservation.UrnNameSnapshot,
                    reservation.SupplyItemNameSnapshot,
                    reservedItem?.ScanCode ?? reservation.SupplyItemScanCodeSnapshot,
                    1,
                    reservation.Status,
                    reservation.ReservedAt),
            completedMaterials,
            blockingReason is null,
            blockingReason);
    }

    private async Task<CremationScannerResolveDto> BuildCremationScanResolveAsync(
        Guid cremationId,
        ScanIdentity identity,
        CremationUrnReservation? reservation,
        CremationScannerPreviewDto preview,
        InventoryLedger ledger)
    {
        var item = await db.SupplyItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == identity.SupplyItemId)
            ?? throw new KeyNotFoundException("El insumo asociado ya no existe.");
        var physicalStock = Round(await ledger.GetPhysicalStockAsync(item.Id));
        var reservedQuantity = await db.CremationUrnReservations.CountAsync(x =>
            x.SupplyItemId == item.Id && x.Status == UrnReservationStatus.Active);
        var matchesReservedUrn = reservation is not null &&
            reservation.SupplyItemId == item.Id;
        var reservationIdToConsume = matchesReservedUrn ? reservation?.Id : null;
        var protectedReservations = await db.CremationUrnReservations.CountAsync(x =>
            x.SupplyItemId == item.Id &&
            x.Status == UrnReservationStatus.Active &&
            (!reservationIdToConsume.HasValue || x.Id != reservationIdToConsume.Value));
        var availableForFulfillment = Round(Math.Max(0, physicalStock - protectedReservations));
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
        var canUseIdentity = identity.LotId.HasValue
            ? scannedLot is { IsEligible: true }
            : eligibleLots.Count > 0 || unallocatedPhysicalStock > 0;
        CremationScannerMatchKind? matchKind = matchesReservedUrn
            ? CremationScannerMatchKind.ReservedUrn
            : item.IsActive && item.TrackInventory
                ? CremationScannerMatchKind.AdditionalMaterial
                : null;
        var canUse = preview.CanFulfill &&
            item.IsActive &&
            item.TrackInventory &&
            matchKind.HasValue &&
            availableForFulfillment > 0 &&
            canUseIdentity;
        string? rejectionReason = null;
        if (!preview.CanFulfill)
            rejectionReason = preview.BlockingReason;
        else if (!item.IsActive || !item.TrackInventory)
            rejectionReason = "El insumo no está activo o no controla inventario.";
        else if (availableForFulfillment <= 0)
            rejectionReason = "No hay existencia disponible para esta entrega.";
        else if (!canUseIdentity)
            rejectionReason = "El lote no está activo o no tiene existencia disponible.";

        return new CremationScannerResolveDto(
            cremationId,
            identity.ScanCode,
            item.Id,
            item.Name,
            item.UnitOfMeasure,
            identity.LotId.HasValue,
            scannedLot?.Id,
            scannedLot?.ScanCode,
            scannedLot?.RemainingQuantity,
            scannedLot?.IsActive,
            matchKind,
            1,
            physicalStock,
            reservedQuantity,
            availableForFulfillment,
            unallocatedPhysicalStock,
            lots,
            !identity.LotId.HasValue && eligibleLots.Count > 1,
            !identity.LotId.HasValue && eligibleLots.Count == 1
                ? eligibleLots[0].Id
                : null,
            canUse,
            rejectionReason);
    }

    private async Task<InventoryScannerLotDto?> ValidateScannerSelectionAsync(
        CremationScannerSelectionInput input,
        decimal quantity,
        CremationScannerResolveDto state,
        CremationScannerPreviewDto currentPreview)
    {
        if (state.IsLotScan)
        {
            if (!state.ScannedLotId.HasValue ||
                input.SupplyInventoryLotId != state.ScannedLotId)
                throw new ArgumentException("La salida debe usar el lote escaneado.");
        }
        else
        {
            var eligibleLots = state.Lots.Where(x => x.IsEligible).ToList();
            if (eligibleLots.Count > 0 && !input.SupplyInventoryLotId.HasValue)
                throw new ArgumentException("Selecciona explícitamente el lote que se consumirá.");
        }

        InventoryScannerLotDto? selectedLot = null;
        if (input.SupplyInventoryLotId.HasValue)
        {
            selectedLot = state.Lots.FirstOrDefault(x =>
                x.Id == input.SupplyInventoryLotId.Value);
            if (selectedLot is not { IsEligible: true })
            {
                var belongsToItem = selectedLot is not null ||
                    await db.SupplyInventoryLots.AsNoTracking().AnyAsync(x =>
                        x.Id == input.SupplyInventoryLotId.Value &&
                        x.SupplyItemId == state.SupplyItemId);
                if (!belongsToItem)
                    throw new ArgumentException("El lote seleccionado no corresponde al insumo.");
                if (input.ExpectedLotStock.HasValue && Round(input.ExpectedLotStock.Value) > 0)
                    throw new CremationScannerConflictException(
                        "El lote cambió desde la vista previa. Revisa y confirma nuevamente.",
                        currentPreview);
                throw new ArgumentException("El lote seleccionado no está activo o no tiene existencia.");
            }
        }

        var stale = Round(input.ExpectedPhysicalStock) != state.PhysicalStock ||
            Round(input.ExpectedAvailableStock) != state.AvailableForFulfillment;
        if (selectedLot is not null)
            stale |= !input.ExpectedLotStock.HasValue ||
                Round(input.ExpectedLotStock.Value) != selectedLot.RemainingQuantity;
        else
            stale |= !input.ExpectedUnallocatedStock.HasValue ||
                Round(input.ExpectedUnallocatedStock.Value) != state.UnallocatedPhysicalStock;
        if (stale)
            throw new CremationScannerConflictException(
                "El inventario cambió desde la vista previa. Revisa y confirma nuevamente.",
                currentPreview);

        if (!state.CanUse || quantity > state.AvailableForFulfillment)
            throw new CremationScannerConflictException(
                "La cantidad excede la existencia disponible para esta entrega.",
                currentPreview);
        if (selectedLot is not null && quantity > selectedLot.RemainingQuantity)
            throw new CremationScannerConflictException(
                "La cantidad excede el saldo del lote.",
                currentPreview);
        if (selectedLot is null && quantity > state.UnallocatedPhysicalStock)
            throw new CremationScannerConflictException(
                "La cantidad excede la existencia física sin lote.",
                currentPreview);

        return selectedLot;
    }

    private async Task<ScanIdentity> ResolveScanIdentityAsync(string normalizedCode)
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

    private async Task<ScanIdentity> ResolveScanIdentityForConfirmationAsync(
        string normalizedCode,
        CremationScannerPreviewDto currentPreview)
    {
        try
        {
            return await ResolveScanIdentityAsync(normalizedCode);
        }
        catch (Exception ex) when (
            ex is KeyNotFoundException or InventoryScannerConflictException)
        {
            throw new CremationScannerConflictException(
                "La identidad de un código cambió. Revisa y confirma nuevamente.",
                currentPreview);
        }
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

    private static decimal Round(decimal value) => decimal.Round(value, 3);

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

    private sealed record MaterialRequest(
        Guid SupplyItemId,
        decimal Quantity,
        Guid? LotId,
        string? ScannedCode = null);

    private sealed record ScanIdentity(Guid SupplyItemId, Guid? LotId, string ScanCode);

    private sealed record ResolvedSelection(
        CremationScannerSelectionInput Input,
        string NormalizedCode,
        decimal Quantity,
        ScanIdentity Identity);

    private sealed record ValidatedSelection(
        ResolvedSelection Selection,
        CremationScannerResolveDto State,
        InventoryScannerLotDto? SelectedLot);
}
