using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public async Task<CremationDto?> AmendInstructionsAsync(
        Guid id, AmendCremationInstructionsDto dto, Guid actorUserId)
    {
        Exception? concurrencyException = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await AmendInstructionsCoreAsync(id, dto, actorUserId);
            }
            catch (Exception exception) when (IsCremationConcurrencyConflict(exception))
            {
                _context.ChangeTracker.Clear();
                concurrencyException = exception;
            }
        }

        throw CremationConcurrencyConflict(concurrencyException!);
    }

    private async Task<CremationDto?> AmendInstructionsCoreAsync(
        Guid id, AmendCremationInstructionsDto dto, Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
            throw new ArgumentException("La solicitud de cambio no es válida.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 1000)
            throw new ArgumentException("El motivo del cambio es obligatorio y no debe exceder 1000 caracteres.");
        if (dto.NewSpecialInstructions?.Length > 1000)
            throw new ArgumentException("Las instrucciones especiales no pueden exceder 1000 caracteres.");
        if (dto.ExpectedCurrentSpecialInstructions?.Length > 1000)
            throw new ArgumentException("Las instrucciones actuales no pueden exceder 1000 caracteres.");

        var reason = dto.Reason.Trim();
        var newInstructions = string.IsNullOrWhiteSpace(dto.NewSpecialInstructions)
            ? null : dto.NewSpecialInstructions.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var receptionId = await LockReceptionForCremationAsync(id);
        if (!receptionId.HasValue || !await LockActiveCremationAsync(id, receptionId.Value))
            return null;

        var cremation = await _context.Cremations.SingleAsync(x => x.Id == id);
        var actor = await LockReassignmentUserAsync(actorUserId);
        if (actor is null || !actor.IsActive)
            throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
        var actorRole = await ResolveInstructionsActorRoleAsync(actor);

        var previousRequest = await _context.CremationInstructionsAmendments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CremationId == id && x.RequestId == dto.RequestId);
        if (previousRequest is not null)
        {
            if (previousRequest.ActorUserId != actorUserId ||
                previousRequest.NewSpecialInstructions != newInstructions ||
                previousRequest.PreviousSpecialInstructions != dto.ExpectedCurrentSpecialInstructions ||
                previousRequest.Reason != reason)
                throw new InvalidOperationException("Esta solicitud ya fue utilizada con otros datos.");
            await transaction.CommitAsync();
            return await GetByIdAsync(id);
        }

        if (cremation.Status is < CremationStatus.InProgress or > CremationStatus.Delivered)
            throw new InvalidOperationException(
                "El cambio con motivo solo está disponible desde el inicio hasta la entrega.");
        if (cremation.Status == CremationStatus.Delivered &&
            actorRole is not ("Owner" or "Admin"))
            throw new UnauthorizedAccessException(
                "Después de la entrega, solo el propietario o un administrador protegido puede corregir las instrucciones.");
        if (cremation.SpecialInstructions != dto.ExpectedCurrentSpecialInstructions)
            throw new InvalidOperationException(
                "Las instrucciones cambiaron desde que se abrió la solicitud. Revise el valor actual e intente nuevamente.");
        if (cremation.SpecialInstructions == newInstructions)
            throw new ArgumentException("Las nuevas instrucciones deben ser diferentes a las actuales.");

        var sequence = (await _context.CremationInstructionsAmendments
            .Where(x => x.CremationId == id)
            .MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        _context.CremationInstructionsAmendments.Add(new CremationInstructionsAmendment
        {
            Id = Guid.NewGuid(),
            CremationId = id,
            Sequence = sequence,
            RequestId = dto.RequestId,
            PreviousSpecialInstructions = cremation.SpecialInstructions,
            NewSpecialInstructions = newInstructions,
            ActorUserId = actor.Id,
            ActorNameSnapshot = ReassignmentUserName(actor),
            ActorRole = actorRole,
            Reason = reason,
            Status = cremation.Status,
            CreatedAt = DateTime.UtcNow
        });
        cremation.SpecialInstructions = newInstructions;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetByIdAsync(id);
    }

    private async Task<string> ResolveInstructionsActorRoleAsync(User actor)
    {
        if (actor.IsOwner) return "Owner";
        var role = await _context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == actor.Id && ur.Role.IsActive &&
                ur.Role.RolePermissions.Any(rp => rp.Permission.Code == PermissionCodes.CremationsManage))
            .OrderBy(ur => ur.RoleId == ReassignmentAdminRoleId ? 0 : 1)
            .ThenBy(ur => ur.Role.NormalizedName)
            .ThenBy(ur => ur.RoleId)
            .Select(ur => new { ur.RoleId, ur.Role.Name })
            .FirstOrDefaultAsync();
        if (role is null)
            throw new UnauthorizedAccessException(
                "El usuario autenticado no conserva un rol activo con permiso para administrar cremaciones.");
        return role.RoleId == ReassignmentAdminRoleId ? "Admin" : role.Name;
    }

    public async Task<IReadOnlyList<CremationInstructionsAmendmentDto>?> GetInstructionsAmendmentsAsync(Guid id)
    {
        if (!await _context.Cremations.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive))
            return null;
        return await _context.CremationInstructionsAmendments.AsNoTracking()
            .Where(x => x.CremationId == id).OrderBy(x => x.Sequence)
            .Select(x => new CremationInstructionsAmendmentDto
            {
                Id = x.Id,
                CremationId = x.CremationId,
                Sequence = x.Sequence,
                RequestId = x.RequestId,
                PreviousSpecialInstructions = x.PreviousSpecialInstructions,
                NewSpecialInstructions = x.NewSpecialInstructions,
                ActorUserId = x.ActorUserId,
                ActorNameSnapshot = x.ActorNameSnapshot,
                ActorRole = x.ActorRole,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToListAsync();
    }
}
