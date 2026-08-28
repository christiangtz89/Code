namespace pcms.Application.Auth;
public static class PermissionCodes
{
    public const string Manage = "Permissions.Manage";
    public const string SuppliersView = "Suppliers.View";
    public const string SuppliersManage = "Suppliers.Manage";
    public const string FinanceView = "Finance.View";
    public const string FinanceManage = "Finance.Manage";
    public const string InventoryView = "Inventory.View";
    public const string InventoryManage = "Inventory.Manage";
    public const string InventoryScanOutgoing = "Inventory.ScanOutgoing";
    public const string PurchasingView = "Purchasing.View";
    public const string PurchasingManage = "Purchasing.Manage";
    public const string CustomersView = "Customers.View";
    public const string CustomersManage = "Customers.Manage";
    public const string PetsView = "Pets.View";
    public const string PetsManage = "Pets.Manage";
    public const string VeterinaryClinicsView = "VeterinaryClinics.View";
    public const string VeterinaryClinicsManage = "VeterinaryClinics.Manage";
    public const string VeterinariansView = "Veterinarians.View";
    public const string VeterinariansManage = "Veterinarians.Manage";
    public const string VeterinaryRequestsView = "VeterinaryRequests.View";
    public const string VeterinaryRequestsManage = "VeterinaryRequests.Manage";
    public const string CollectionsView = "Collections.View";
    public const string CollectionsManage = "Collections.Manage";
    public const string ReceptionsView = "Receptions.View";
    public const string ReceptionsManage = "Receptions.Manage";
    public const string CremationsView = "Cremations.View";
    public const string CremationsManage = "Cremations.Manage";
    public const string PaymentsView = "Payments.View";
    public const string PaymentsManage = "Payments.Manage";
    public const string CremationPackagesView = "CremationPackages.View";
    public const string CremationPackagesManage = "CremationPackages.Manage";
    public const string UrnsView = "Urns.View";
    public const string UrnsManage = "Urns.Manage";
    public const string CremationPricingView = "CremationPricing.View";
    public const string CremationPricingManage = "CremationPricing.Manage";

    public static readonly string[] All =
    [
        Manage, SuppliersView, SuppliersManage, FinanceView, FinanceManage,
        InventoryView, InventoryManage, InventoryScanOutgoing, PurchasingView, PurchasingManage,
        CustomersView, CustomersManage, PetsView, PetsManage,
        VeterinaryClinicsView, VeterinaryClinicsManage, VeterinariansView, VeterinariansManage,
        VeterinaryRequestsView, VeterinaryRequestsManage, CollectionsView, CollectionsManage,
        ReceptionsView, ReceptionsManage, CremationsView, CremationsManage,
        PaymentsView, PaymentsManage, CremationPackagesView, CremationPackagesManage,
        UrnsView, UrnsManage, CremationPricingView, CremationPricingManage,
    ];
}
