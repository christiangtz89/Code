using Microsoft.Extensions.DependencyInjection;
using pcms.Application.Auth;
using pcms.Application.Customers;
using pcms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure;

public static class DependencyInjection
{
   public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection")
        ));

    services.AddScoped<IAuthService, AuthService>();

    services.AddScoped<IJwtTokenService, JwtTokenService>();

    services.AddScoped<ICustomerService, CustomerService>();

    return services;
  }
}
