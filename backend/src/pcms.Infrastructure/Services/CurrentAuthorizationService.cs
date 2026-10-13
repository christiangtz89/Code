using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public sealed class CurrentAuthorizationService(AppDbContext context) : ICurrentAuthorizationService
{
    // Same protected role identity and same-role Manage requirement used by C3-F1.
    private static readonly Guid ProtectedAdminRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<CurrentAuthorization?> ResolveAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // One statement/snapshot; project only authority facts, never credential data.
        var user = await context.Users.AsNoTracking()
            .TagWith("PCMS current authorization")
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.IsOwner,
                Roles = user.UserRoles.Where(membership => membership.Role.IsActive)
                    .Select(membership => new
                    {
                        membership.RoleId,
                        membership.Role.Name,
                        Permissions = membership.Role.RolePermissions
                            .Select(grant => grant.Permission.Code).ToArray()
                    }).ToArray()
            })
            .AsSingleQuery()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null) return null;

        return new CurrentAuthorization(
            user.Id,
            user.Email,
            user.Roles.Select(role => role.Name).Distinct().ToArray(),
            user.Roles.SelectMany(role => role.Permissions).Distinct().ToArray(),
            user.IsOwner,
            user.Roles.Any(role => role.RoleId == ProtectedAdminRoleId &&
                role.Permissions.Contains(PermissionCodes.CremationsManage)));
    }
}
