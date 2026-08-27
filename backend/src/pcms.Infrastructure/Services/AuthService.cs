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



        var defaultRole = await _context.Roles.FirstAsync(x => x.Name == "Usuario");
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRole.Id });
        _context.Users.Add(user);


        await _context.SaveChangesAsync();


var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
var permissions = user.UserRoles.SelectMany(x => x.Role.RolePermissions).Select(x => x.Permission.Code).Distinct().ToList();


var token = _jwtTokenService.GenerateToken(
    user.Id,
    user.Email,
    roles,
    permissions);


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
    .ThenInclude(x => x.RolePermissions)
    .ThenInclude(x => x.Permission)
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



        var registeredUser = await _context.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission).FirstAsync(x => x.Id == user.Id);
        var roles = registeredUser.UserRoles.Select(x => x.Role.Name).ToList();
        var permissions = registeredUser.UserRoles.SelectMany(x => x.Role.RolePermissions).Select(x => x.Permission.Code).Distinct().ToList();


var token = _jwtTokenService.GenerateToken(
    user.Id,
    user.Email,
    roles,
    permissions);

           return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Token = token
        };
    }
}
