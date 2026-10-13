using Microsoft.EntityFrameworkCore;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public async Task<IReadOnlyList<CremationStatusTransitionDto>?> GetStatusTransitionsAsync(Guid id)
    {
        // Cremations.View also protects the existing inactive-record history APIs.
        if (!await _context.Cremations.AsNoTracking().AnyAsync(x => x.Id == id)) return null;

        return await _context.CremationStatusTransitions.AsNoTracking()
            .Where(x => x.CremationId == id)
            .OrderBy(x => x.Sequence)
            .Select(x => new CremationStatusTransitionDto
            {
                Id = x.Id,
                CremationId = x.CremationId,
                Sequence = x.Sequence,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                ActorUserId = x.ActorUserId,
                ActorNameSnapshot = x.ActorNameSnapshot,
                OccurredAt = x.OccurredAt,
                OperationalCommentId = x.OperationalCommentId
            }).ToListAsync();
    }

    private async Task<User> LockActiveStatusActorAsync(Guid actorUserId)
    {
        var actorIds = await _context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "Usuarios"
            WHERE "Id" = {actorUserId} AND "Activo" = TRUE
            FOR SHARE
            """).ToListAsync();

        if (actorIds.Count != 1)
            throw new InvalidOperationException("El usuario que cambia el estado no existe o está inactivo.");

        return await _context.Users.AsNoTracking().SingleAsync(user => user.Id == actorUserId);
    }

    private async Task AddStatusTransitionAsync(
        Cremation cremation, CremationStatus previousStatus, User actor,
        DateTime occurredAt, Guid? operationalCommentId = null)
    {
        // Both callers hold the reception and cremation row locks in their existing
        // serializable transaction. Retries reload status before allocating a sequence.
        var sequence = (await _context.CremationStatusTransitions
            .Where(x => x.CremationId == cremation.Id)
            .MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;

        _context.CremationStatusTransitions.Add(new CremationStatusTransition
        {
            Id = Guid.NewGuid(),
            CremationId = cremation.Id,
            Sequence = sequence,
            PreviousStatus = previousStatus,
            NewStatus = cremation.Status,
            ActorUserId = actor.Id,
            ActorNameSnapshot = string.Join(" ", new[] { actor.FirstName, actor.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())),
            OccurredAt = occurredAt,
            OperationalCommentId = operationalCommentId
        });
    }
}
