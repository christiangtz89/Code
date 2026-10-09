using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    public async Task<CremationDto?> AmendAccessoryAsync(
        Guid id, AmendCremationAccessoryDto dto, Guid actorUserId)
    {
        Exception? concurrencyException = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await AmendAccessoryCoreAsync(id, dto, actorUserId);
            }
            catch (Exception exception) when (IsCremationConcurrencyConflict(exception))
            {
                _context.ChangeTracker.Clear();
                concurrencyException = exception;
            }
        }

        throw CremationConcurrencyConflict(concurrencyException!);
    }

    private async Task<CremationDto?> AmendAccessoryCoreAsync(
        Guid id, AmendCremationAccessoryDto dto, Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty)
            throw new ArgumentException("La solicitud de cambio no es válida.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 1000)
            throw new ArgumentException("El motivo del cambio es obligatorio y no debe exceder 1000 caracteres.");
        if (dto.NewAccessoryDescription?.Length > 500 ||
            dto.ExpectedCurrentAccessoryDescription?.Length > 500)
            throw new ArgumentException("La descripción del accesorio no puede exceder 500 caracteres.");

        var reason = dto.Reason.Trim();
        var newDescription = string.IsNullOrWhiteSpace(dto.NewAccessoryDescription)
            ? null : dto.NewAccessoryDescription.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var receptionId = await LockReceptionForCremationAsync(id);
        if (!receptionId.HasValue || !await LockActiveCremationAsync(id, receptionId.Value))
            return null;

        var cremation = await _context.Cremations.SingleAsync(x => x.Id == id);
        var actor = await LockReassignmentUserAsync(actorUserId);
        if (actor is null || !actor.IsActive)
            throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
        var actorRole = await ResolveAccessoryActorRoleAsync(actor);

        var previousRequest = await _context.CremationAccessoryAmendments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CremationId == id && x.RequestId == dto.RequestId);
        if (previousRequest is not null)
        {
            if (previousRequest.ActorUserId != actorUserId ||
                previousRequest.NewAccessoryDescription != newDescription ||
                previousRequest.PreviousAccessoryDescription != dto.ExpectedCurrentAccessoryDescription ||
                previousRequest.Reason != reason)
                throw new InvalidOperationException("Esta solicitud ya fue utilizada con otros datos.");
            await transaction.CommitAsync();
            return await GetByIdAsync(id);
        }

        if (cremation.Status is < CremationStatus.InProgress or > CremationStatus.Delivered)
            throw new InvalidOperationException(
                "El cambio con motivo solo está disponible desde el inicio hasta la entrega.");
        if (!cremation.IncludesPawPrint)
            throw new InvalidOperationException("Esta cremación no incluye accesorio.");
        if (cremation.Status == CremationStatus.Delivered &&
            actorRole is not ("Owner" or "Admin"))
            throw new UnauthorizedAccessException(
                "Después de la entrega, solo el propietario o un administrador protegido puede corregir la descripción del accesorio.");
        if (cremation.AccessoryDescription != dto.ExpectedCurrentAccessoryDescription)
            throw new InvalidOperationException(
                "La descripción del accesorio cambió desde que se abrió la solicitud. Revise el valor actual e intente nuevamente.");
        if (cremation.AccessoryDescription == newDescription)
            throw new ArgumentException("La nueva descripción debe ser diferente a la actual.");

        var sequence = (await _context.CremationAccessoryAmendments
            .Where(x => x.CremationId == id)
            .MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        _context.CremationAccessoryAmendments.Add(new CremationAccessoryAmendment
        {
            Id = Guid.NewGuid(),
            CremationId = id,
            Sequence = sequence,
            RequestId = dto.RequestId,
            PreviousAccessoryDescription = cremation.AccessoryDescription,
            NewAccessoryDescription = newDescription,
            ActorUserId = actor.Id,
            ActorNameSnapshot = ReassignmentUserName(actor),
            ActorRole = actorRole,
            Reason = reason,
            Status = cremation.Status,
            CreatedAt = DateTime.UtcNow
        });
        cremation.AccessoryDescription = newDescription;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetByIdAsync(id);
    }

    private async Task<string> ResolveAccessoryActorRoleAsync(User actor)
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

    public async Task<IReadOnlyList<CremationAccessoryAmendmentDto>?> GetAccessoryAmendmentsAsync(Guid id)
    {
        if (!await _context.Cremations.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive))
            return null;
        return await _context.CremationAccessoryAmendments.AsNoTracking()
            .Where(x => x.CremationId == id).OrderBy(x => x.Sequence)
            .Select(x => new CremationAccessoryAmendmentDto
            {
                Id = x.Id,
                CremationId = x.CremationId,
                Sequence = x.Sequence,
                RequestId = x.RequestId,
                PreviousAccessoryDescription = x.PreviousAccessoryDescription,
                NewAccessoryDescription = x.NewAccessoryDescription,
                ActorUserId = x.ActorUserId,
                ActorNameSnapshot = x.ActorNameSnapshot,
                ActorRole = x.ActorRole,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToListAsync();
    }
}
