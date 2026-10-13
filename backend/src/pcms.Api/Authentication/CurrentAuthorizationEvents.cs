using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using pcms.Application.Auth;

namespace pcms.Api.Authentication;

public sealed class CurrentAuthorizationEvents(ICurrentAuthorizationService authorization) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        // Runs only after normal JWT signature/issuer/audience/lifetime validation.
        var subjects = context.Principal?.FindAll(JwtRegisteredClaimNames.Sub).ToArray();
        if (subjects is not { Length: 1 } ||
            !Guid.TryParse(subjects[0].Value, out var userId) || userId == Guid.Empty)
        {
            context.Fail("No se pudo identificar al usuario autenticado.");
            return;
        }

        // JWT authentication caches its result within this request. No cross-request cache.
        // Database errors propagate to the existing error middleware; never reuse JWT grants.
        var current = await authorization.ResolveAsync(userId, context.HttpContext.RequestAborted);
        if (current is null)
        {
            context.Fail("El usuario autenticado no existe o está inactivo.");
            return;
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, current.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, current.Email)
        };
        claims.AddRange(current.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(current.Permissions.Select(permission => new Claim("permission", permission)));
        if (current.IsOwner) claims.Add(new Claim("pcms_owner", "true"));
        if (current.IsProtectedAdmin) claims.Add(new Claim("pcms_protected_admin", "true"));

        // Replace the entire principal, including every stale role/permission/privilege claim.
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims, context.Scheme.Name, ClaimTypes.Name, ClaimTypes.Role));
    }
}
