using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;


namespace pcms.Infrastructure.Services;


public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentAuthorizationService _authorization;

    private readonly PasswordHasher<User> _passwordHasher;


    public AuthService(AppDbContext context,
    IJwtTokenService jwtTokenService,
    ICurrentAuthorizationService authorization)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _authorization = authorization;

        _passwordHasher = new PasswordHasher<User>();
    }



    public async Task<AuthResponse> Register(
        RegisterRequest request)
    {

        var existingUser =
            await _context.Users
            .FirstOrDefaultAsync(
                x => x.Email == request.Email);


        if (existingUser != null)
        {
            throw new Exception(
                "Email already registered");
        }



        var user = new User
        {
            Id = Guid.NewGuid(),

            FirstName = request.FirstName,

            LastName = request.LastName,

            Email = request.Email,

            IsActive = true,

            CreatedAt = DateTime.UtcNow
        };



        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);



        var defaultRole = await _context.Roles
            .FirstAsync(x => x.Name == "Usuario");
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRole.Id, Role = defaultRole });
        _context.Users.Add(user);


        await _context.SaveChangesAsync();


        return await IssueTokenAsync(user.Id);
    }




    public async Task<AuthResponse> Login(
        LoginRequest request)
    {

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);



        if (user == null || !user.IsActive)
        {
            throw InvalidCredentials();
        }



        var result =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);



        if (result ==
            PasswordVerificationResult.Failed)
        {
            throw InvalidCredentials();
        }



        return await IssueTokenAsync(user.Id);
    }

    private async Task<AuthResponse> IssueTokenAsync(Guid userId)
    {
        var current = await _authorization.ResolveAsync(userId)
            ?? throw InvalidCredentials();
        return new AuthResponse
        {
            UserId = current.UserId,
            Email = current.Email,
            Token = _jwtTokenService.GenerateToken(
                current.UserId, current.Email, current.Roles, current.Permissions,
                current.IsOwner, current.IsProtectedAdmin)
        };
    }

    private static UnauthorizedAccessException InvalidCredentials() =>
        new("Correo electrónico o contraseña incorrectos.");
}
