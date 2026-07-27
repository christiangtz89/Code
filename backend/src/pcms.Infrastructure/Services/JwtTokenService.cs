using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using pcms.Application.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace pcms.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;


    public JwtTokenService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }



    public string GenerateToken(
    Guid userId,
    string email,
    string role)
    {

        var claims = new[]
{
    new Claim(
        JwtRegisteredClaimNames.Sub,
        userId.ToString()),

    new Claim(
        JwtRegisteredClaimNames.Email,
        email),

    new Claim(
        ClaimTypes.Role,
        role)
};


        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!
                ));


        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);



        var token =
            new JwtSecurityToken(
                issuer:
                _configuration["Jwt:Issuer"],

                audience:
                _configuration["Jwt:Audience"],

                claims: claims,

                expires:
                DateTime.UtcNow.AddMinutes(
                    double.Parse(
                    _configuration["Jwt:ExpiryMinutes"]!
                    )),

                signingCredentials:
                credentials);



        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}