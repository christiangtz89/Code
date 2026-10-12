using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public async Task<CremationClarificationDto?> CreateClarificationAsync(
        Guid id, CreateCremationClarificationDto dto, Guid actorUserId)
    {
        Exception? concurrencyException = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await CreateClarificationCoreAsync(id, dto, actorUserId);
            }
            catch (Exception exception) when (IsCremationConcurrencyConflict(exception) ||
                exception is DbUpdateException
                {
                    InnerException: PostgresException
                    {
                        SqlState: PostgresErrorCodes.UniqueViolation,
                        ConstraintName: "IX_AclaracionesCremacion_RequestId"
                    }
                })
            {
                _context.ChangeTracker.Clear();
                concurrencyException = exception;
            }
        }

        throw CremationConcurrencyConflict(concurrencyException!);
    }

    private async Task<CremationClarificationDto?> CreateClarificationCoreAsync(
        Guid id, CreateCremationClarificationDto dto, Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
            throw new ArgumentException("La solicitud de aclaración no es válida.");
        if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > 1000)
            throw new ArgumentException("La aclaración es obligatoria y no debe exceder 1000 caracteres.");
        var text = dto.Text.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // Reconcile an already committed request even if the case was archived
        // after its response was lost. This branch never creates or updates evidence.
        var previousRequest = await _context.CremationClarifications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == dto.RequestId);
        if (previousRequest is not null)
        {
            var replayActor = await LockReassignmentUserAsync(actorUserId);
            if (replayActor is null || !replayActor.IsActive)
                throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
            _ = await ResolveInstructionsActorRoleAsync(replayActor);
            if (previousRequest.CremationId != id || previousRequest.ActorUserId != actorUserId ||
                previousRequest.Text != text)
                throw new InvalidOperationException("Esta solicitud ya fue utilizada con otros datos.");
            await transaction.CommitAsync();
            return MapClarification(previousRequest);
        }

        var receptionId = await LockReceptionForCremationAsync(id);
        if (!receptionId.HasValue || !await LockActiveCremationAsync(id, receptionId.Value))
            return null;

        var cremation = await _context.Cremations.SingleAsync(x => x.Id == id);
        var actor = await LockReassignmentUserAsync(actorUserId);
        if (actor is null || !actor.IsActive)
            throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
        var actorRole = await ResolveInstructionsActorRoleAsync(actor);

        if (cremation.Status != CremationStatus.Cancelled)
            throw new InvalidOperationException("Las aclaraciones del caso solo están disponibles para cremaciones canceladas.");

        var sequence = (await _context.CremationClarifications.Where(x => x.CremationId == id)
            .MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        var clarification = new CremationClarification
        {
            Id = Guid.NewGuid(), CremationId = id, Sequence = sequence, RequestId = dto.RequestId,
            Text = text, ActorUserId = actor.Id, ActorNameSnapshot = ReassignmentUserName(actor),
            ActorRole = actorRole, Status = CremationStatus.Cancelled, CreatedAt = DateTime.UtcNow
        };
        // Clarifications never replace Notes or operational/status-transition commentary.
        _context.CremationClarifications.Add(clarification);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return MapClarification(clarification);
    }

    public async Task<IReadOnlyList<CremationClarificationDto>?> GetClarificationsAsync(Guid id)
    {
        if (!await _context.Cremations.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive))
            return null;
        var entries = await _context.CremationClarifications.AsNoTracking()
            .Where(x => x.CremationId == id).OrderBy(x => x.Sequence).ToListAsync();
        return entries.Select(MapClarification).ToList();
    }

    private static CremationClarificationDto MapClarification(CremationClarification entry) => new()
    {
        Id = entry.Id, CremationId = entry.CremationId, Sequence = entry.Sequence,
        RequestId = entry.RequestId, Text = entry.Text, ActorUserId = entry.ActorUserId,
        ActorNameSnapshot = entry.ActorNameSnapshot, ActorRole = entry.ActorRole,
        Status = entry.Status, CreatedAt = entry.CreatedAt
    };
}
