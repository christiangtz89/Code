using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcms.Infrastructure.Persistence;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "Permissions.Manage")]
public class PermissionsController(AppDbContext db) : ControllerBase
{
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() => Ok(await db.Roles.AsNoTracking().Include(x => x.RolePermissions).ThenInclude(x => x.Permission).Select(x => new { x.Id, x.Name, permissions = x.RolePermissions.Select(p => p.Permission.Code) }).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> GetPermissions() => Ok(await db.Permissions.AsNoTracking().OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync());

    [HttpPut("roles/{roleId:guid}")]
    public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] Guid[] permissionIds)
    {
        var role = await db.Roles.Include(x => x.RolePermissions).FirstOrDefaultAsync(x => x.Id == roleId);
        if (role is null) return NotFound();
        var validIds = await db.Permissions.Where(x => permissionIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        db.RolePermissions.RemoveRange(role.RolePermissions);
        db.RolePermissions.AddRange(validIds.Select(id => new pcms.Domain.Entities.RolePermission { RoleId = roleId, PermissionId = id }));
        await db.SaveChangesAsync();
        return NoContent();
    }
}
