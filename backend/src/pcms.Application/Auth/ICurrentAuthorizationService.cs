namespace pcms.Application.Auth;

public interface ICurrentAuthorizationService
{
    Task<CurrentAuthorization?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CurrentAuthorization(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsOwner,
    bool IsProtectedAdmin);
