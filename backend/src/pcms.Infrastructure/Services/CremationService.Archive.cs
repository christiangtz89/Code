using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Auth;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public Task<bool> DeactivateAsync(Guid id, ChangeCremationActiveStateDto dto, Guid actorUserId) =>
        ChangeActiveStateAsync(id, false, dto, actorUserId);

    public Task<bool> RestoreAsync(Guid id, ChangeCremationActiveStateDto dto, Guid actorUserId) =>
        ChangeActiveStateAsync(id, true, dto, actorUserId);

    private async Task<bool> ChangeActiveStateAsync(
        Guid id, bool newIsActive, ChangeCremationActiveStateDto dto, Guid actorUserId)
    {
        Exception? concurrencyException = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await ChangeActiveStateCoreAsync(id, newIsActive, dto, actorUserId);
            }
            catch (Exception exception) when (IsCremationConcurrencyConflict(exception) ||
                exception is DbUpdateException
                {
                    InnerException: PostgresException
                    {
                        SqlState: PostgresErrorCodes.UniqueViolation,
                        ConstraintName: "IX_EventosArchivoCremacion_RequestId"
                    }
                })
            {
                _context.ChangeTracker.Clear();
                concurrencyException = exception;
            }
        }

        throw CremationConcurrencyConflict(concurrencyException!);
    }

    private async Task<bool> ChangeActiveStateCoreAsync(
        Guid id, bool newIsActive, ChangeCremationActiveStateDto dto, Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
            throw new ArgumentException("La solicitud de archivo o restauración no es válida.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 1000)
            throw new ArgumentException("El motivo es obligatorio y no debe exceder 1000 caracteres.");
        var reason = dto.Reason.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // Match the existing reception -> cremation lock order, including inactive cases.
        var receptionIds = await _context.Database.SqlQuery<Guid>($"""
            SELECT reception."Id" AS "Value" FROM "Recepciones" AS reception
            WHERE reception."Id" = (SELECT "RecepcionId" FROM "Cremaciones" WHERE "Id" = {id})
            FOR UPDATE
            """).ToListAsync();
        if (receptionIds.Count == 0) return false;
        var cremation = await _context.Cremations.FromSqlInterpolated($"""
            SELECT * FROM "Cremaciones" WHERE "Id" = {id} FOR UPDATE
            """).SingleOrDefaultAsync();
        if (cremation is null) return false;

        var actor = await LockReassignmentUserAsync(actorUserId);
        if (actor is null || !actor.IsActive)
            throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
        var (actorRole, privileged) = await ResolveArchiveActorAsync(actor);
        var previousRequest = await _context.CremationArchiveEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == dto.RequestId);
        if (previousRequest is not null)
        {
            if (previousRequest.CremationId != id || previousRequest.NewIsActive != newIsActive ||
                previousRequest.ActorUserId != actorUserId || previousRequest.Reason != reason)
                throw new InvalidOperationException("Esta solicitud ya fue utilizada con otros datos.");
            await transaction.CommitAsync();
            return true;
        }
        var clearlyPreStart = cremation.Status is CremationStatus.Pending or CremationStatus.Scheduled &&
            !cremation.StartedAt.HasValue && !cremation.CompletedAt.HasValue &&
            !cremation.ReadyForDeliveryAt.HasValue && !cremation.DeliveredAt.HasValue &&
            !await _context.CremationStartVerifications.AnyAsync(x => x.CremationId == id);
        // Cancelled/unknown legacy states do not prove a pre-start lifecycle.
        if (!clearlyPreStart && !privileged)
            throw new UnauthorizedAccessException(
                "Solo el propietario o un administrador protegido puede archivar o restaurar una cremación iniciada o sin evidencia suficiente de que aún no ha iniciado.");

        if (cremation.IsActive == newIsActive)
            throw new InvalidOperationException(newIsActive
                ? "La cremación ya está activa. No se registró una restauración."
                : "La cremación ya está archivada. No se registró otro archivo.");

        if (newIsActive)
        {
            if (!await _context.Receptions.AnyAsync(x => x.Id == cremation.ReceptionId && x.IsActive))
                throw new InvalidOperationException(
                    "No se puede restaurar la cremación porque su recepción está inactiva.");
            if (cremation.Status is CremationStatus.Scheduled or CremationStatus.InProgress)
            {
                await AcquireOneHornoSchedulingLockAsync();
                if (cremation.Status == CremationStatus.InProgress)
                    await EnsureHornoAvailableForInProgressAsync(cremation);
                else if (cremation.ScheduledAt.HasValue)
                    await EnsureScheduleAvailableAsync(cremation.ScheduledAt.Value, cremation.Id);
                else
                    throw new InvalidOperationException(
                        "Debe establecer una fecha programada antes de restaurar una cremación programada.");
            }
        }

        var sequence = (await _context.CremationArchiveEvents.Where(x => x.CremationId == id)
            .MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        _context.CremationArchiveEvents.Add(new CremationArchiveEvent
        {
            Id = Guid.NewGuid(),
            CremationId = id,
            Sequence = sequence,
            RequestId = dto.RequestId,
            PreviousIsActive = cremation.IsActive,
            NewIsActive = newIsActive,
            ActorUserId = actor.Id,
            ActorNameSnapshot = ReassignmentUserName(actor),
            ActorRole = actorRole,
            Reason = reason,
            Status = cremation.Status,
            CreatedAt = DateTime.UtcNow
        });
        cremation.IsActive = newIsActive;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private async Task<(string Role, bool Privileged)> ResolveArchiveActorAsync(User actor)
    {
        if (actor.IsOwner) return ("Owner", true);
        var role = await _context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == actor.Id && ur.Role.IsActive &&
                ur.Role.RolePermissions.Any(rp => rp.Permission.Code == PermissionCodes.CremationsManage))
            .OrderBy(ur => ur.RoleId == ReassignmentAdminRoleId ? 0 : 1)
            .ThenBy(ur => ur.Role.NormalizedName).ThenBy(ur => ur.RoleId)
            .Select(ur => new { ur.RoleId, ur.Role.Name }).FirstOrDefaultAsync();
        if (role is null)
            throw new UnauthorizedAccessException(
                "El usuario autenticado no conserva un rol activo con permiso para administrar cremaciones.");
        return (role.RoleId == ReassignmentAdminRoleId ? "Admin" : role.Name,
            role.RoleId == ReassignmentAdminRoleId);
    }

    public async Task<IReadOnlyList<CremationArchiveEventDto>?> GetArchiveEventsAsync(Guid id)
    {
        // The controller uses Cremations.View, which already protects inactive listing.
        if (!await _context.Cremations.AsNoTracking().AnyAsync(x => x.Id == id)) return null;
        return await _context.CremationArchiveEvents.AsNoTracking()
            .Where(x => x.CremationId == id).OrderBy(x => x.Sequence)
            .Select(x => new CremationArchiveEventDto
            {
                Id = x.Id,
                CremationId = x.CremationId,
                Sequence = x.Sequence,
                RequestId = x.RequestId,
                PreviousIsActive = x.PreviousIsActive,
                NewIsActive = x.NewIsActive,
                ActorUserId = x.ActorUserId,
                ActorNameSnapshot = x.ActorNameSnapshot,
                ActorRole = x.ActorRole,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToListAsync();
    }
}
