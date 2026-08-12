using Microsoft.Extensions.DependencyInjection;
using pcms.Application.Auth;
using pcms.Application.Customers;
using pcms.Application.Receptions.Interfaces;
using pcms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using pcms.Application.VeterinaryClinics.Interfaces;
using pcms.Infrastructure.Persistence;
using pcms.Application.Pets.Interfaces;
using pcms.Application.Veterinarians.Interfaces;
using pcms.Application.Cremations.Interfaces;
using pcms.Application.Payments.Interfaces;
using pcms.Application.VeterinaryRequests.Interfaces;
using pcms.Application.Collections.Interfaces;

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

        services.AddScoped<IPetService, PetService>();

        services.AddScoped<IReceptionService, ReceptionService>();

        services.AddScoped<
    ICollectionService,
    CollectionService>();

        services.AddScoped<IVeterinaryClinicService, VeterinaryClinicService>();

        services.AddScoped<IVeterinarianService, VeterinarianService>();

        services.AddScoped<ICremationService, CremationService>();

        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<
            IVeterinaryRequestService,
            VeterinaryRequestService>();

        return services;
    }
}
