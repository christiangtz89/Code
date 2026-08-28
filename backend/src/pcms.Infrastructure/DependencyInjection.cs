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
using pcms.Application.CremationPackages.Interfaces;
using pcms.Application.Urns.Interfaces;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Application.Collections.Photos.Interfaces;
using pcms.Application.Supplies.Interfaces;
using pcms.Application.Purchasing.Interfaces;
using pcms.Application.Inventory;
using pcms.Application.Reporting;

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

        services.AddScoped<
        ICollectionPhotoService,
        CollectionPhotoService>();

        services.AddScoped<IVeterinaryClinicService, VeterinaryClinicService>();

        services.AddScoped<IVeterinarianService, VeterinarianService>();

        services.AddScoped<ICremationService, CremationService>();

        services.AddScoped<ICremationPackageService, CremationPackageService>();

        services.AddScoped<IUrnService, UrnService>();

        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<ICremationPricingService, CremationPricingService>();

        services.AddScoped<
            IVeterinaryRequestService,
            VeterinaryRequestService>();

        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISupplyItemService, SupplyItemService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ISupplierSupplyItemService, SupplierSupplyItemService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryScannerService, InventoryScannerService>();
        services.AddScoped<IFilamentService, FilamentService>();
        services.AddScoped<IUrnBomService, UrnBomService>();
        services.AddScoped<IManufacturedUrnProductionService, ManufacturedUrnProductionService>();
        services.AddScoped<IUrnInventoryService, UrnInventoryService>();
        services.AddScoped<IInventoryLotService, InventoryLotService>();
        services.AddScoped<ISpendingReportService, SpendingReportService>();
        services.AddScoped<IInventoryReportService, InventoryReportService>();
        services.AddScoped<ICostAnalyticsService, CostAnalyticsService>();
        services.AddScoped<ICremationInventoryService, CremationInventoryService>();
        services.AddScoped<IStockCountService, StockCountService>();
        return services;
    }
}
