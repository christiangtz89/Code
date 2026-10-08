using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    private static readonly Guid ReassignmentAdminRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<CremationDto?> ReassignAsync(
        Guid id, ReassignCremationDto dto, Guid actorUserId)
    {
        Exception? concurrencyException = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await ReassignCoreAsync(id, dto, actorUserId);
            }
            catch (Exception exception) when (IsCremationConcurrencyConflict(exception))
            {
                _context.ChangeTracker.Clear();
                concurrencyException = exception;
            }
        }

        throw CremationConcurrencyConflict(concurrencyException!);
    }

    private async Task<CremationDto?> ReassignCoreAsync(
        Guid id, ReassignCremationDto dto, Guid actorUserId)
    {
        if (dto.RequestId == Guid.Empty || dto.NewAssignedToUserId == Guid.Empty)
            throw new ArgumentException("Debe seleccionar un responsable y enviar una solicitud válida.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 1000)
            throw new ArgumentException("El motivo de la reasignación es obligatorio y no debe exceder 1000 caracteres.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var receptionId = await LockReceptionForCremationAsync(id);
        if (!receptionId.HasValue || !await LockActiveCremationAsync(id, receptionId.Value))
            return null;

        var cremation = await _context.Cremations.SingleAsync(c => c.Id == id);
        // Stored active identities and roles are authoritative, never client role claims.
        var actor = await LockReassignmentUserAsync(actorUserId);
        if (actor is null || !actor.IsActive)
            throw new UnauthorizedAccessException("El usuario autenticado no existe o está inactivo.");
        var actorRole = await ResolveReassignmentRoleAsync(actor);
        var reason = dto.Reason.Trim();
        var previousRequest = await _context.CremationReassignments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CremationId == id && x.RequestId == dto.RequestId);
        if (previousRequest is not null)
        {
            if (previousRequest.ActorUserId != actorUserId ||
                previousRequest.NewAssignedToUserId != dto.NewAssignedToUserId || previousRequest.Reason != reason)
                throw new InvalidOperationException("Esta solicitud ya fue utilizada con otros datos.");
            // This is a read of an applied amendment, including after later progression.
            await transaction.CommitAsync();
            return await GetByIdAsync(id);
        }

        if (cremation.Status == CremationStatus.Delivered && actorRole == "Manager")
            throw new UnauthorizedAccessException(
                "Después de la entrega, solo el propietario o un administrador protegido puede corregir el responsable.");
        if (cremation.Status is < CremationStatus.InProgress or > CremationStatus.Delivered)
            throw new InvalidOperationException("La reasignación con motivo solo está disponible desde el inicio hasta la entrega.");
        if (cremation.AssignedToUserId == dto.NewAssignedToUserId)
            throw new ArgumentException("Selecciona un responsable diferente al actual.");

        var target = await LockReassignmentUserAsync(dto.NewAssignedToUserId);
        if (target is null || !target.IsActive)
            throw new ArgumentException("No se encontró un usuario activo para asignar la cremación.");
        var previous = cremation.AssignedToUserId.HasValue
            ? await _context.Users.AsNoTracking().SingleAsync(u => u.Id == cremation.AssignedToUserId)
            : null;
        var sequence = (await _context.CremationReassignments
            .Where(x => x.CremationId == id).MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        _context.CremationReassignments.Add(new CremationReassignment
        {
            Id = Guid.NewGuid(), CremationId = id, Sequence = sequence, RequestId = dto.RequestId,
            PreviousAssignedToUserId = cremation.AssignedToUserId,
            PreviousUserNameSnapshot = previous is null ? null : ReassignmentUserName(previous),
            NewAssignedToUserId = target.Id, NewUserNameSnapshot = ReassignmentUserName(target),
            ActorUserId = actor.Id, ActorNameSnapshot = ReassignmentUserName(actor), ActorRole = actorRole,
            Reason = reason, Status = cremation.Status, CreatedAt = DateTime.UtcNow
        });
        cremation.AssignedToUserId = target.Id;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetByIdAsync(id);
    }

    private async Task<User?> LockReassignmentUserAsync(Guid id)
    {
        // SHARE holds active-user/name facts through the amendment commit.
        return await _context.Users.FromSqlInterpolated(
            $"SELECT * FROM \"Usuarios\" WHERE \"Id\" = {id} FOR SHARE")
            .AsNoTracking().SingleOrDefaultAsync();
    }

    private async Task<string> ResolveReassignmentRoleAsync(User actor)
    {
        if (actor.IsOwner) return "Owner";
        var role = await _context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == actor.Id && ur.Role.IsActive &&
                (ur.RoleId == ReassignmentAdminRoleId || ur.Role.NormalizedName == "MANAGER") &&
                ur.Role.RolePermissions.Any(rp => rp.Permission.Code == PermissionCodes.CremationsManage))
            .OrderBy(ur => ur.RoleId == ReassignmentAdminRoleId ? 0 : 1)
            .Select(ur => new { ur.RoleId }).FirstOrDefaultAsync();
        if (role is null)
            throw new UnauthorizedAccessException("La reasignación requiere autorización del propietario, administrador protegido o gerente.");
        return role.RoleId == ReassignmentAdminRoleId ? "Admin" : "Manager";
    }

    private static string ReassignmentUserName(User user) => string.Join(" ",
        new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

    public async Task<IReadOnlyList<CremationReassignmentDto>?> GetReassignmentsAsync(Guid id)
    {
        if (!await _context.Cremations.AsNoTracking().AnyAsync(c => c.Id == id && c.IsActive)) return null;
        return await _context.CremationReassignments.AsNoTracking()
            .Where(x => x.CremationId == id).OrderBy(x => x.Sequence)
            .Select(x => new CremationReassignmentDto
            {
                Id = x.Id, CremationId = x.CremationId, Sequence = x.Sequence, RequestId = x.RequestId,
                PreviousAssignedToUserId = x.PreviousAssignedToUserId,
                PreviousUserNameSnapshot = x.PreviousUserNameSnapshot,
                NewAssignedToUserId = x.NewAssignedToUserId, NewUserNameSnapshot = x.NewUserNameSnapshot,
                ActorUserId = x.ActorUserId, ActorNameSnapshot = x.ActorNameSnapshot, ActorRole = x.ActorRole,
                Reason = x.Reason, Status = x.Status, CreatedAt = x.CreatedAt
            }).ToListAsync();
    }
}
