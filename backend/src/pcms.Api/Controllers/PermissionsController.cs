using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Infrastructure.Persistence;
using pcms.Domain.Entities;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "Permissions.Manage")]
public class PermissionsController(AppDbContext db) : ControllerBase
{
    private static readonly Guid ProtectedAdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    private async Task<bool> IsOwnerAsync() => CurrentUserId() is Guid userId && await db.Users.AnyAsync(x => x.Id == userId && x.IsOwner);

    private async Task<HashSet<string>> ActiveEffectivePermissionCodesAsync(Guid userId)
    {
        var assignedCodes = await db.UserRoles
            .Where(x => x.UserId == userId && x.Role.IsActive)
            .SelectMany(x => x.Role.RolePermissions)
            .Select(x => x.Permission.Code)
            .ToListAsync();

        return EffectivePermissionCodes(assignedCodes);
    }

    private static HashSet<string> EffectivePermissionCodes(IEnumerable<string> permissionCodes) => permissionCodes
        .Concat(permissionCodes.Where(x => x.EndsWith(".Manage", StringComparison.Ordinal)).Select(x => x[..^7] + ".View"))
        .ToHashSet(StringComparer.Ordinal);
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() => Ok(await db.Roles.AsNoTracking().Include(x => x.RolePermissions).ThenInclude(x => x.Permission).Select(x => new { x.Id, x.Name, x.Description, x.IsActive, isProtected = x.Id == ProtectedAdminRoleId, permissions = x.RolePermissions.Select(p => p.Permission.Code) }).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> GetPermissions() => Ok(await db.Permissions.AsNoTracking().OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync());

    [HttpPut("roles/{roleId:guid}/permissions")]
    public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] Guid[] permissionIds)
    {
        var actorIsOwner = await IsOwnerAsync();
        if (roleId == ProtectedAdminRoleId && !actorIsOwner) return Forbid();
        var role = await db.Roles.Include(x => x.RolePermissions).FirstOrDefaultAsync(x => x.Id == roleId);
        if (role is null) return NotFound();
        var validPermissions = await db.Permissions.Where(x => permissionIds.Contains(x.Id)).Select(x => new { x.Id, x.Code }).ToListAsync();

        if (!actorIsOwner)
        {
            var actorId = CurrentUserId();
            if (actorId is null) return Forbid();
            var effectiveCodes = await ActiveEffectivePermissionCodesAsync(actorId.Value);
            if (validPermissions.Any(x => !effectiveCodes.Contains(x.Code))) return Forbid();
        }

        db.RolePermissions.RemoveRange(role.RolePermissions);
        db.RolePermissions.AddRange(validPermissions.Select(x => new RolePermission { RoleId = roleId, PermissionId = x.Id }));
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleInput input)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "El nombre del rol es obligatorio." });
        var normalizedName = name.ToUpperInvariant();
        if (await db.Roles.AnyAsync(x => x.NormalizedName == normalizedName)) return BadRequest(new { message = "Ya existe un rol con ese nombre." });
        var role = new Role { Id = Guid.NewGuid(), Name = name, NormalizedName = normalizedName, Description = input.Description?.Trim(), IsActive = input.IsActive };
        db.Roles.Add(role);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return BadRequest(new { message = "Ya existe un rol con ese nombre." }); }
        return Ok(new { role.Id, role.Name, role.Description, role.IsActive });
    }

    [HttpPut("roles/{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] RoleInput input)
    {
        if (roleId == ProtectedAdminRoleId) return Forbid();
        var role = await db.Roles.FindAsync(roleId); if (role is null) return NotFound();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "El nombre del rol es obligatorio." });
        var normalizedName = name.ToUpperInvariant();
        if (await db.Roles.AnyAsync(x => x.Id != roleId && x.NormalizedName == normalizedName)) return BadRequest(new { message = "Ya existe un rol con ese nombre." });
        role.Name = name; role.NormalizedName = normalizedName; role.Description = input.Description?.Trim(); role.IsActive = input.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return BadRequest(new { message = "Ya existe un rol con ese nombre." }); }
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await db.Users.AsNoTracking().Include(x => x.UserRoles).ThenInclude(x => x.Role).Select(x => new { x.Id, x.FirstName, x.LastName, x.Email, x.IsActive, x.IsOwner, roles = x.UserRoles.Select(r => new { r.RoleId, r.Role.Name }) }).ToListAsync());

    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> SetUserRoles(Guid userId, [FromBody] Guid[] roleIds)
    {
        var actorIsOwner = await IsOwnerAsync();
        var actorId = CurrentUserId();
        var user = await db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return NotFound();
        if (user.IsOwner) return Forbid();
        if (!actorIsOwner && actorId == userId) return Forbid();

        var roles = await db.Roles
            .Where(x => roleIds.Contains(x.Id) && x.IsActive)
            .Select(x => new
            {
                x.Id,
                PermissionCodes = x.RolePermissions.Select(rolePermission => rolePermission.Permission.Code)
            })
            .ToListAsync();
        var currentlyHasAdmin = user.UserRoles.Any(x => x.RoleId == ProtectedAdminRoleId);
        var requestedHasAdmin = roles.Any(x => x.Id == ProtectedAdminRoleId);
        if (!actorIsOwner && currentlyHasAdmin != requestedHasAdmin) return Forbid();

        if (!actorIsOwner)
        {
            if (actorId is null) return Forbid();
            var actorEffectiveCodes = await ActiveEffectivePermissionCodesAsync(actorId.Value);
            var requestedEffectiveCodes = EffectivePermissionCodes(roles.SelectMany(x => x.PermissionCodes));
            if (requestedEffectiveCodes.Any(x => !actorEffectiveCodes.Contains(x))) return Forbid();
        }

        db.UserRoles.RemoveRange(user.UserRoles); db.UserRoles.AddRange(roles.Select(x => new UserRole { UserId = userId, RoleId = x.Id })); await db.SaveChangesAsync(); return NoContent();
    }
}

public record RoleInput(string Name, string? Description, bool IsActive = true);
