using Microsoft.EntityFrameworkCore;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public async Task<IReadOnlyList<CremationOperationalCommentDto>?> GetOperationalCommentsAsync(
        Guid id)
    {
        if (!await _context.Cremations.AsNoTracking()
                .AnyAsync(cremation => cremation.Id == id && cremation.IsActive))
        {
            return null;
        }

        return await _context.CremationOperationalComments.AsNoTracking()
            .Where(comment => comment.CremationId == id)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Select(comment => new CremationOperationalCommentDto
            {
                Id = comment.Id,
                CremationId = comment.CremationId,
                Status = comment.Status,
                Comment = comment.Comment,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedByUserNameSnapshot = comment.CreatedByUserNameSnapshot,
                CreatedAt = comment.CreatedAt
            })
            .ToListAsync();
    }

    private async Task AddOperationalCommentAsync(
        Guid cremationId,
        CremationStatus status,
        string? notes,
        Guid actorUserId,
        DateTime createdAt,
        User? startActor)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        // Start already holds its actor lock. Other transitions retain their
        // existing authorization; this lookup captures identity, not a new role policy.
        var actor = startActor ?? await _context.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == actorUserId);

        if (actor is null)
        {
            throw new InvalidOperationException(
                "No se pudo identificar al usuario que registra el comentario.");
        }

        _context.CremationOperationalComments.Add(new CremationOperationalComment
        {
            Id = Guid.NewGuid(),
            CremationId = cremationId,
            Status = status,
            Comment = notes.Trim(),
            CreatedByUserId = actor.Id,
            CreatedByUserNameSnapshot = string.Join(" ",
                new[] { actor.FirstName, actor.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())),
            CreatedAt = createdAt
        });
    }
}
