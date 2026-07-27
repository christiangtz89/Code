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

    private readonly PasswordHasher<User> _passwordHasher;


    public AuthService(AppDbContext context,
    IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;

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



        _context.Users.Add(user);


        await _context.SaveChangesAsync();


var role = user.UserRoles
    .Select(x => x.Role.Name)
    .FirstOrDefault() ?? "Usuario";


var token = _jwtTokenService.GenerateToken(
    user.Id,
    user.Email,
    role);


return new AuthResponse
{
    UserId = user.Id,
    Email = user.Email,
    Token = token
};
    }




    public async Task<AuthResponse> Login(
        LoginRequest request)
    {

        var user = await _context.Users
    .Include(x => x.UserRoles)
    .ThenInclude(x => x.Role)
    .FirstOrDefaultAsync(x => x.Email == request.Email);



        if (user == null)
        {
            throw new Exception(
                "Invalid email or password");
        }



        var result =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);



        if (result ==
            PasswordVerificationResult.Failed)
        {
            throw new Exception(
                "Invalid email or password");
        }



        var role = user.UserRoles
    .Select(x => x.Role.Name)
    .FirstOrDefault() ?? "Usuario";


var token = _jwtTokenService.GenerateToken(
    user.Id,
    user.Email,
    role);

           return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Token = token
        };
    }
}