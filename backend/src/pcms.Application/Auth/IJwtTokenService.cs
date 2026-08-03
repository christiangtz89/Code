namespace pcms.Application.Auth;

public interface IJwtTokenService
{
    string GenerateToken(
    Guid userId,
    string email,
    string role);
}