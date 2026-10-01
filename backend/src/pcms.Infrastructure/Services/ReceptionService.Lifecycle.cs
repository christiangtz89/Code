using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Auth;
using pcms.Application.Receptions.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class ReceptionService
{
    private const int LifecycleReasonMaximumLength = 1000;
    private const int LifecycleConcurrencyMaximumAttempts = 8;

    public Task<ReceptionLifecycleDecisionDto?> DeactivateAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId) =>
        ExecuteLifecycleWithRetryAsync(
            id,
            request,
            actorUserId,
            ReceptionLifecycleActionKind.Deactivate);

    public Task<ReceptionLifecycleDecisionDto?> RestoreAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId) =>
        ExecuteLifecycleWithRetryAsync(
            id,
            request,
            actorUserId,
            ReceptionLifecycleActionKind.Restore);

    public Task<ReceptionLifecycleDecisionDto?> RequestDeactivationAsync(
        Guid id,
        ReceptionLifecycleActionRequestDto request,
        Guid actorUserId) =>
        ExecuteLifecycleWithRetryAsync(
            id,
            request,
            actorUserId,
            ReceptionLifecycleActionKind.DeactivationRequested);

    public async Task<IReadOnlyList<ReceptionLifecycleEventDto>?>
        GetLifecycleHistoryAsync(Guid id)
    {
        if (!await _context.Receptions
                .AsNoTracking()
                .AnyAsync(reception => reception.Id == id))
        {
            return null;
        }

        return await _context.ReceptionLifecycleEvents
            .AsNoTracking()
            .Where(lifecycleEvent =>
                lifecycleEvent.ReceptionId == id)
            .OrderBy(lifecycleEvent =>
                lifecycleEvent.SequenceNumber)
            .Select(lifecycleEvent =>
                MapLifecycleEvent(lifecycleEvent))
            .ToListAsync();
    }

    private async Task<ReceptionLifecycleDecisionDto?>
        ExecuteLifecycleWithRetryAsync(
            Guid id,
            ReceptionLifecycleActionRequestDto request,
            Guid actorUserId,
            ReceptionLifecycleActionKind action)
    {
        var reason = ValidateLifecycleRequest(
            request,
            actorUserId);

        Exception? concurrencyException = null;

        for (var attempt = 0;
             attempt < LifecycleConcurrencyMaximumAttempts;
             attempt++)
        {
            try
            {
                return await ExecuteLifecycleCoreAsync(
                    id,
                    request.RequestId,
                    reason,
                    actorUserId,
                    action);
            }
            catch (Exception exception)
                when (IsLifecycleConcurrencyConflict(exception))
            {
                concurrencyException = exception;
                _context.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException(
            "La operación cambió mientras se procesaba. " +
            "Actualiza la información e intenta nuevamente.",
            concurrencyException);
    }

    private async Task<ReceptionLifecycleDecisionDto?>
        ExecuteLifecycleCoreAsync(
            Guid id,
            Guid requestId,
            string reason,
            Guid actorUserId,
            ReceptionLifecycleActionKind action)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (!await LockReceptionForLifecycleAsync(id))
        {
            return null;
        }

        var reception = await _context.Receptions
            .FirstOrDefaultAsync(item => item.Id == id);

        if (reception is null)
        {
            return null;
        }

        var actor = action ==
                ReceptionLifecycleActionKind.DeactivationRequested
            ? await ResolveManagerLifecycleActorAsync(actorUserId)
            : await ResolveApprovalLifecycleActorAsync(actorUserId);

        var existingEvent = await _context.ReceptionLifecycleEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(lifecycleEvent =>
                lifecycleEvent.ReceptionId == id &&
                lifecycleEvent.RequestId == requestId);

        if (existingEvent is not null)
        {
            if (existingEvent.ActionKind != action ||
                !string.Equals(
                    existingEvent.Reason,
                    reason,
                    StringComparison.Ordinal))
            {
                throw LifecycleRequestIdConflict();
            }

            await transaction.CommitAsync();

            return BuildLifecycleDecision(
                existingEvent,
                isReplay: true);
        }

        if (action ==
                ReceptionLifecycleActionKind.DeactivationRequested &&
            !reception.IsActive)
        {
            throw new InvalidOperationException(
                "No se puede solicitar la desactivación de una recepción inactiva.");
        }

        var snapshot = await CaptureLifecycleSnapshotAsync(reception);
        var sequence = await GetNextLifecycleSequenceAsync(id);
        var previousIsActive = reception.IsActive;

        ReceptionLifecycleOutcomeKind outcome;
        ReceptionLifecycleBlockReason blockReason;
        bool newIsActive;

        switch (action)
        {
            case ReceptionLifecycleActionKind.DeactivationRequested:
                outcome = ReceptionLifecycleOutcomeKind.Recorded;
                blockReason = ReceptionLifecycleBlockReason.None;
                newIsActive = previousIsActive;
                break;

            case ReceptionLifecycleActionKind.Deactivate
                when !previousIsActive:
                outcome = ReceptionLifecycleOutcomeKind.Blocked;
                blockReason = ReceptionLifecycleBlockReason.ReceptionState;
                newIsActive = previousIsActive;
                break;

            case ReceptionLifecycleActionKind.Deactivate
                when snapshot.Dependencies !=
                     ReceptionLifecycleDependency.None:
                outcome = ReceptionLifecycleOutcomeKind.Blocked;
                blockReason = ReceptionLifecycleBlockReason.Dependencies;
                newIsActive = previousIsActive;
                break;

            case ReceptionLifecycleActionKind.Deactivate:
                outcome = ReceptionLifecycleOutcomeKind.Succeeded;
                blockReason = ReceptionLifecycleBlockReason.None;
                newIsActive = false;
                reception.IsActive = false;
                break;

            case ReceptionLifecycleActionKind.Restore
                when previousIsActive:
                outcome = ReceptionLifecycleOutcomeKind.Blocked;
                blockReason = ReceptionLifecycleBlockReason.ReceptionState;
                newIsActive = previousIsActive;
                break;

            case ReceptionLifecycleActionKind.Restore:
                outcome = ReceptionLifecycleOutcomeKind.Succeeded;
                blockReason = ReceptionLifecycleBlockReason.None;
                newIsActive = true;
                reception.IsActive = true;
                break;

            default:
                throw new InvalidOperationException(
                    "La acción de ciclo de vida no es válida.");
        }

        var lifecycleEvent = CreateLifecycleEvent(
            reception,
            requestId,
            sequence,
            action,
            outcome,
            reason,
            actor,
            previousIsActive,
            newIsActive,
            snapshot,
            blockReason);

        _context.ReceptionLifecycleEvents.Add(lifecycleEvent);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return BuildLifecycleDecision(
            lifecycleEvent,
            isReplay: false);
    }

    private async Task<LifecycleActor> ResolveApprovalLifecycleActorAsync(
        Guid actorUserId)
    {
        var actor = await GetActiveLifecycleUserAsync(actorUserId);

        if (actor.IsOwner)
        {
            return new LifecycleActor(actor, "Owner");
        }

        var isProtectedAdmin = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(userRole =>
                userRole.UserId == actor.Id &&
                userRole.RoleId == ProtectedAdminRoleId &&
                userRole.Role.IsActive &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission.Code ==
                    PermissionCodes.ReceptionsManage));

        if (!isProtectedAdmin)
        {
            throw new UnauthorizedAccessException(
                "Solo el Propietario o un Administrador activo puede aprobar esta acción.");
        }

        return new LifecycleActor(actor, "Admin");
    }

    private async Task<LifecycleActor> ResolveManagerLifecycleActorAsync(
        Guid actorUserId)
    {
        var actor = await GetActiveLifecycleUserAsync(actorUserId);

        if (actor.IsOwner)
        {
            throw new UnauthorizedAccessException(
                "Solo un Gerente activo puede registrar esta solicitud.");
        }

        var activeAuthorityRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == actor.Id &&
                userRole.Role.IsActive &&
                (userRole.RoleId == ProtectedAdminRoleId ||
                 userRole.Role.NormalizedName == "MANAGER") &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission.Code ==
                    PermissionCodes.ReceptionsManage))
            .Select(userRole => new
            {
                userRole.RoleId,
                userRole.Role.NormalizedName
            })
            .ToListAsync();

        if (activeAuthorityRoles.Any(role =>
                role.RoleId == ProtectedAdminRoleId) ||
            !activeAuthorityRoles.Any(role =>
                role.NormalizedName == "MANAGER"))
        {
            throw new UnauthorizedAccessException(
                "Solo un Gerente activo puede registrar esta solicitud.");
        }

        return new LifecycleActor(actor, "Manager");
    }

    private async Task<User> GetActiveLifecycleUserAsync(Guid actorUserId)
    {
        var actor = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == actorUserId &&
                user.IsActive);

        return actor ?? throw new UnauthorizedAccessException(
            "El usuario autenticado no existe o está inactivo.");
    }

    private async Task EnsureInactiveReceptionReadAuthorizedAsync(
        Guid? actorUserId)
    {
        if (!actorUserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Solo el Propietario o un Administrador activo puede consultar recepciones inactivas.");
        }

        try
        {
            await ResolveApprovalLifecycleActorAsync(actorUserId.Value);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException(
                "Solo el Propietario o un Administrador activo puede consultar recepciones inactivas.");
        }
    }

    private async Task<LifecycleSnapshot> CaptureLifecycleSnapshotAsync(
        Reception reception)
    {
        Collection? collection = null;

        if (reception.CollectionId is Guid collectionId)
        {
            await LockCollectionForLifecycleAsync(collectionId);

            collection = await _context.Collections
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == collectionId);
        }

        var veterinaryRequest = await _context.VeterinaryRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request =>
                request.ReceptionId == reception.Id);

        var cremationId =
            await LockCremationForLifecycleAsync(reception.Id);

        var cremation = cremationId.HasValue
            ? await _context.Cremations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == cremationId.Value)
            : null;

        var paymentAccountId = await FindPaymentAccountIdAsync(
            cremation?.Id,
            collection?.Id);

        PaymentAccount? paymentAccount = null;

        if (paymentAccountId.HasValue)
        {
            await LockPaymentAccountForLifecycleAsync(
                paymentAccountId.Value);

            paymentAccount = await _context.PaymentAccounts
                .AsNoTracking()
                .Include(account => account.Payments)
                .FirstOrDefaultAsync(account =>
                    account.Id == paymentAccountId.Value);
        }

        var activeReceptionEvidenceCount =
            await _context.ReceptionPhotos
                .AsNoTracking()
                .CountAsync(photo =>
                    photo.ReceptionId == reception.Id &&
                    photo.IsActive);

        var collectionEvidenceCount = collection is null
            ? 0
            : await _context.CollectionPhotos
                .AsNoTracking()
                .CountAsync(photo =>
                    photo.CollectionId == collection.Id);

        var collectionAssignmentHistoryCount = collection is null
            ? 0
            : await _context.CollectionAssignmentHistory
                .AsNoTracking()
                .CountAsync(history =>
                    history.CollectionId == collection.Id);

        var historyContext = await _context.ReceptionHistoryEvents
            .AsNoTracking()
            .Where(history =>
                history.ReceptionId == reception.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                LatestSequence = group.Max(history =>
                    history.SequenceNumber)
            })
            .FirstOrDefaultAsync();

        var paymentCount = paymentAccount?.Payments.Count ?? 0;
        var dependencies = ReceptionLifecycleDependency.None;

        if (collection is not null)
        {
            dependencies |= ReceptionLifecycleDependency.Collection;
        }

        if (veterinaryRequest is not null)
        {
            dependencies |=
                ReceptionLifecycleDependency.ConvertedVeterinaryRequest;
        }

        if (cremation is not null)
        {
            dependencies |= ReceptionLifecycleDependency.Cremation;
        }

        if (paymentAccount is not null)
        {
            dependencies |= ReceptionLifecycleDependency.PaymentAccount;
        }

        if (paymentCount > 0)
        {
            dependencies |= ReceptionLifecycleDependency.PaymentHistory;
        }

        if (activeReceptionEvidenceCount > 0)
        {
            dependencies |=
                ReceptionLifecycleDependency.DirectReceptionEvidence;
        }

        if (collectionEvidenceCount > 0 ||
            collectionAssignmentHistoryCount > 0)
        {
            dependencies |= ReceptionLifecycleDependency.CollectionEvidence;
        }

        return new LifecycleSnapshot
        {
            OperationalStage = cremation is null
                ? ReceptionHistoryStage.BeforeCremation
                : GetHistoryStage(cremation.Status),
            Collection = collection,
            VeterinaryRequest = veterinaryRequest,
            Cremation = cremation,
            PaymentAccount = paymentAccount,
            PaymentCount = paymentCount,
            AmountPaid = paymentAccount?.Payments.Sum(payment =>
                payment.Amount),
            ActiveReceptionEvidenceCount =
                activeReceptionEvidenceCount,
            CollectionEvidenceCount = collectionEvidenceCount,
            CollectionAssignmentHistoryCount =
                collectionAssignmentHistoryCount,
            ReceptionHistoryCount = historyContext?.Count ?? 0,
            LatestReceptionHistorySequence =
                historyContext?.LatestSequence,
            Dependencies = dependencies
        };
    }

    private ReceptionLifecycleEvent CreateLifecycleEvent(
        Reception reception,
        Guid requestId,
        long sequence,
        ReceptionLifecycleActionKind action,
        ReceptionLifecycleOutcomeKind outcome,
        string reason,
        LifecycleActor actor,
        bool previousIsActive,
        bool newIsActive,
        LifecycleSnapshot snapshot,
        ReceptionLifecycleBlockReason blockReason) =>
        new()
        {
            Id = Guid.NewGuid(),
            ReceptionId = reception.Id,
            RequestId = requestId,
            SequenceNumber = sequence,
            ActionKind = action,
            OutcomeKind = outcome,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = actor.User.Id,
            CreatedByUserNameSnapshot =
                BuildUserNameSnapshot(actor.User),
            CreatedByRoleSnapshot = actor.RoleSnapshot,
            PreviousIsActive = previousIsActive,
            NewIsActive = newIsActive,
            OperationalStageSnapshot = snapshot.OperationalStage,
            CollectionId = snapshot.Collection?.Id,
            CollectionIsActiveSnapshot = snapshot.Collection?.IsActive,
            CollectionStatusSnapshot = snapshot.Collection?.Status,
            CollectionCollectedAtSnapshot = snapshot.Collection?.CollectedAt,
            CollectionReceivedAtSnapshot = snapshot.Collection?.ReceivedAt,
            HasConvertedVeterinaryRequestSnapshot =
                snapshot.VeterinaryRequest is not null,
            VeterinaryRequestId = snapshot.VeterinaryRequest?.Id,
            CremationId = snapshot.Cremation?.Id,
            CremationIsActiveSnapshot = snapshot.Cremation?.IsActive,
            CremationStatusSnapshot = snapshot.Cremation?.Status,
            PaymentAccountId = snapshot.PaymentAccount?.Id,
            ServiceTotalSnapshot = snapshot.PaymentAccount?.ServiceTotal,
            AmountPaidSnapshot = snapshot.AmountPaid,
            RequiredCollectionPaymentAmountSnapshot =
                snapshot.PaymentAccount?
                    .RequiredCollectionPaymentAmount,
            RequiresFinancialReviewSnapshot =
                snapshot.PaymentAccount?.RequiresFinancialReview,
            PaymentCountSnapshot = snapshot.PaymentCount,
            ActiveReceptionEvidenceCountSnapshot =
                snapshot.ActiveReceptionEvidenceCount,
            CollectionEvidenceCountSnapshot =
                snapshot.CollectionEvidenceCount,
            CollectionAssignmentHistoryCountSnapshot =
                snapshot.CollectionAssignmentHistoryCount,
            ReceptionHistoryCountSnapshot =
                snapshot.ReceptionHistoryCount,
            LatestReceptionHistorySequenceSnapshot =
                snapshot.LatestReceptionHistorySequence,
            DependenciesSnapshot = snapshot.Dependencies,
            BlockReasonSnapshot = blockReason
        };

    private async Task<bool> LockReceptionForLifecycleAsync(Guid id)
    {
        var ids = await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "Recepciones"
                WHERE "Id" = {id}
                FOR UPDATE
                """)
            .ToListAsync();

        return ids.Count > 0;
    }

    private async Task LockCollectionForLifecycleAsync(Guid id)
    {
        await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "Recolecciones"
                WHERE "Id" = {id}
                FOR UPDATE
                """)
            .ToListAsync();
    }

    private async Task<Guid?> LockCremationForLifecycleAsync(
        Guid receptionId)
    {
        var ids = await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "Cremaciones"
                WHERE "RecepcionId" = {receptionId}
                FOR UPDATE
                """)
            .ToListAsync();

        return ids.Count == 0 ? null : ids[0];
    }

    private async Task<Guid?> FindPaymentAccountIdAsync(
        Guid? cremationId,
        Guid? collectionId)
    {
        var accountIds = await _context.PaymentAccounts
            .AsNoTracking()
            .Where(account =>
                (cremationId.HasValue &&
                 account.CremationId == cremationId.Value) ||
                (collectionId.HasValue &&
                 account.CollectionId == collectionId.Value))
            .OrderByDescending(account =>
                cremationId.HasValue &&
                account.CremationId == cremationId.Value)
            .ThenBy(account => account.Id)
            .Select(account => account.Id)
            .ToListAsync();

        if (accountIds.Count > 1)
        {
            throw new InvalidOperationException(
                "La recepción tiene más de una cuenta de pago vinculada y requiere revisión de integridad.");
        }

        return accountIds.Count == 0 ? null : accountIds[0];
    }

    private async Task LockPaymentAccountForLifecycleAsync(Guid id)
    {
        await _context.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "CuentasPago"
                WHERE "Id" = {id}
                FOR UPDATE
                """)
            .ToListAsync();
    }

    private async Task<long> GetNextLifecycleSequenceAsync(
        Guid receptionId)
    {
        var currentSequence = await _context.ReceptionLifecycleEvents
            .Where(lifecycleEvent =>
                lifecycleEvent.ReceptionId == receptionId)
            .MaxAsync(lifecycleEvent =>
                (long?)lifecycleEvent.SequenceNumber) ?? 0;

        return currentSequence + 1;
    }

    private static string ValidateLifecycleRequest(
        ReceptionLifecycleActionRequestDto? request,
        Guid actorUserId)
    {
        if (request is null)
        {
            throw new ArgumentException(
                "La solicitud de ciclo de vida es obligatoria.");
        }

        if (request.RequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la solicitud es obligatorio.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario autenticado.");
        }

        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("El motivo es obligatorio.");
        }

        if (reason.Length > LifecycleReasonMaximumLength)
        {
            throw new ArgumentException(
                "El motivo no puede exceder 1000 caracteres.");
        }

        return reason;
    }

    private static InvalidOperationException LifecycleRequestIdConflict() =>
        new(
            "El identificador de la solicitud ya fue utilizado para " +
            "una acción o motivo diferente.");

    private static bool IsLifecycleConcurrencyConflict(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is not PostgresException postgresException)
            {
                continue;
            }

            if (postgresException.SqlState is
                PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }

            if (postgresException.SqlState ==
                    PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName is
                    "IX_EventosCicloVidaRecepcion_RecepcionId_NumeroSecuencia" or
                    "IX_EventosCicloVidaRecepcion_RecepcionId_SolicitudId")
            {
                return true;
            }
        }

        return false;
    }

    private static ReceptionLifecycleDecisionDto BuildLifecycleDecision(
        ReceptionLifecycleEvent lifecycleEvent,
        bool isReplay) =>
        new()
        {
            Event = MapLifecycleEvent(lifecycleEvent),
            IsReplay = isReplay,
            IsBlocked = lifecycleEvent.OutcomeKind ==
                ReceptionLifecycleOutcomeKind.Blocked,
            Message = GetLifecycleMessage(lifecycleEvent)
        };

    private static string GetLifecycleMessage(
        ReceptionLifecycleEvent lifecycleEvent)
    {
        if (lifecycleEvent.ActionKind ==
            ReceptionLifecycleActionKind.DeactivationRequested)
        {
            return "Solicitud de desactivación registrada.";
        }

        if (lifecycleEvent.OutcomeKind ==
            ReceptionLifecycleOutcomeKind.Succeeded)
        {
            return lifecycleEvent.ActionKind ==
                    ReceptionLifecycleActionKind.Deactivate
                ? "La recepción fue desactivada."
                : "La recepción fue restaurada.";
        }

        if (lifecycleEvent.BlockReasonSnapshot ==
            ReceptionLifecycleBlockReason.ReceptionState)
        {
            return lifecycleEvent.ActionKind ==
                    ReceptionLifecycleActionKind.Deactivate
                ? "La recepción ya está inactiva."
                : "La recepción ya está activa.";
        }

        var dependencies = lifecycleEvent.DependenciesSnapshot;

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.Collection))
        {
            return "No se puede desactivar la recepción porque tiene una recolección vinculada.";
        }

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.ConvertedVeterinaryRequest))
        {
            return "No se puede desactivar la recepción porque proviene de una solicitud veterinaria convertida.";
        }

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.Cremation))
        {
            return "No se puede desactivar la recepción porque tiene una cremación vinculada.";
        }

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.PaymentHistory))
        {
            return "La recepción conserva historial de pagos y no puede desactivarse.";
        }

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.PaymentAccount))
        {
            return "No se puede desactivar la recepción porque tiene una cuenta de pago vinculada.";
        }

        if (dependencies.HasFlag(
                ReceptionLifecycleDependency.DirectReceptionEvidence) ||
            dependencies.HasFlag(
                ReceptionLifecycleDependency.CollectionEvidence))
        {
            return "La recepción conserva evidencia de custodia y no puede desactivarse.";
        }

        return "No se puede completar la acción de ciclo de vida solicitada.";
    }

    private static ReceptionLifecycleEventDto MapLifecycleEvent(
        ReceptionLifecycleEvent lifecycleEvent) =>
        new()
        {
            Id = lifecycleEvent.Id,
            ReceptionId = lifecycleEvent.ReceptionId,
            RequestId = lifecycleEvent.RequestId,
            Sequence = lifecycleEvent.SequenceNumber,
            Action = lifecycleEvent.ActionKind,
            Outcome = lifecycleEvent.OutcomeKind,
            Reason = lifecycleEvent.Reason,
            ActorUserId = lifecycleEvent.CreatedByUserId,
            ActorUserName = lifecycleEvent.CreatedByUserNameSnapshot,
            ActorRole = lifecycleEvent.CreatedByRoleSnapshot,
            CreatedAt = lifecycleEvent.CreatedAt,
            PreviousIsActive = lifecycleEvent.PreviousIsActive,
            NewIsActive = lifecycleEvent.NewIsActive,
            OperationalStage =
                lifecycleEvent.OperationalStageSnapshot,
            CollectionId = lifecycleEvent.CollectionId,
            CollectionIsActive =
                lifecycleEvent.CollectionIsActiveSnapshot,
            CollectionStatus = lifecycleEvent.CollectionStatusSnapshot,
            CollectionCollectedAt =
                lifecycleEvent.CollectionCollectedAtSnapshot,
            CollectionReceivedAt =
                lifecycleEvent.CollectionReceivedAtSnapshot,
            HasConvertedVeterinaryRequest =
                lifecycleEvent.HasConvertedVeterinaryRequestSnapshot,
            VeterinaryRequestId = lifecycleEvent.VeterinaryRequestId,
            CremationId = lifecycleEvent.CremationId,
            CremationIsActive =
                lifecycleEvent.CremationIsActiveSnapshot,
            CremationStatus = lifecycleEvent.CremationStatusSnapshot,
            PaymentAccountId = lifecycleEvent.PaymentAccountId,
            ServiceTotal = lifecycleEvent.ServiceTotalSnapshot,
            AmountPaid = lifecycleEvent.AmountPaidSnapshot,
            RequiredCollectionPaymentAmount =
                lifecycleEvent.RequiredCollectionPaymentAmountSnapshot,
            RequiresFinancialReview =
                lifecycleEvent.RequiresFinancialReviewSnapshot,
            PaymentCount = lifecycleEvent.PaymentCountSnapshot,
            ActiveReceptionEvidenceCount =
                lifecycleEvent.ActiveReceptionEvidenceCountSnapshot,
            CollectionEvidenceCount =
                lifecycleEvent.CollectionEvidenceCountSnapshot,
            CollectionAssignmentHistoryCount =
                lifecycleEvent.CollectionAssignmentHistoryCountSnapshot,
            ReceptionHistoryCount =
                lifecycleEvent.ReceptionHistoryCountSnapshot,
            LatestReceptionHistorySequence =
                lifecycleEvent.LatestReceptionHistorySequenceSnapshot,
            Dependencies = lifecycleEvent.DependenciesSnapshot,
            BlockReason = lifecycleEvent.BlockReasonSnapshot
        };

    private sealed record LifecycleActor(
        User User,
        string RoleSnapshot);

    private sealed class LifecycleSnapshot
    {
        public ReceptionHistoryStage OperationalStage { get; init; }

        public Collection? Collection { get; init; }

        public VeterinaryRequest? VeterinaryRequest { get; init; }

        public Cremation? Cremation { get; init; }

        public PaymentAccount? PaymentAccount { get; init; }

        public int PaymentCount { get; init; }

        public decimal? AmountPaid { get; init; }

        public int ActiveReceptionEvidenceCount { get; init; }

        public int CollectionEvidenceCount { get; init; }

        public int CollectionAssignmentHistoryCount { get; init; }

        public int ReceptionHistoryCount { get; init; }

        public long? LatestReceptionHistorySequence { get; init; }

        public ReceptionLifecycleDependency Dependencies { get; init; }
    }
}
