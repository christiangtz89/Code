namespace pcms.Application.Auth;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool isOwner);
}
