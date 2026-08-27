import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "../layouts/AppLayout";
import { ApiTestPage } from "../pages/ApiTestPage";
import { DashboardPage } from "../pages/DashboardPage";
import { LoginPage } from "../pages/LoginPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { ProtectedRoute } from "../routes/ProtectedRoute";
import { CustomersPage } from "../features/customers/pages/CustomersPage";
import { PetsPage } from "../features/pets/pages/PetsPage";
import { VeterinaryClinicsPage } from "../features/veterinary-clinics/pages/VeterinaryClinicsPage";
import { VeterinariansPage } from "../features/veterinarians/pages/VeterinariansPage";
import { ReceptionsPage } from "../features/receptions/pages/ReceptionsPage";
import { CremationsPage } from "../features/cremations/pages/CremationsPage";
import { PaymentsPage } from "../features/payments/pages/PaymentsPage";
import { VeterinaryRequestsPage } from "../features/veterinary-requests/pages/VeterinaryRequestsPage";
import { CollectionsPage } from "../features/collections/pages/CollectionsPage";
import { CremationPackagesPage } from "../features/cremation-packages/pages";
import { UrnsPage } from "../features/urns/pages";
import { CremationPricingPage } from "../features/cremation-pricing/pages";
import { InventoryCatalogPage } from "../features/inventory/pages/InventoryCatalogPage";
import { ExpensesPage } from "../features/inventory/pages/ExpensesPage";
import { SuppliersPage } from "../features/inventory/pages/SuppliersPage";
import { PurchasesPage } from "../features/inventory/pages/PurchasesPage";
import { PermissionRoute } from "../routes/PermissionRoute";
import { PermissionsPage } from "../features/auth/pages/PermissionsPage";
import { BomPage } from "../features/inventory/pages/BomPage";
import { ProductionPage } from "../features/inventory/pages/ProductionPage";
import { UrnInventoryPage } from "../features/inventory/pages/UrnInventoryPage";
import { InventoryLabelsPage } from "../features/inventory/pages/InventoryLabelsPage";
import { LotsPage } from "../features/inventory/pages/LotsPage";
import { SpendingReportsPage } from "../features/inventory/pages/SpendingReportsPage";
import { InventoryReportsPage } from "../features/inventory/pages/InventoryReportsPage";
import { CostAnalyticsPage } from "../features/inventory/pages/CostAnalyticsPage";
import { ScannerPage } from "../features/inventory/pages/ScannerPage";

export const router = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: "/",
        element: <AppLayout />,
        children: [
          {
            index: true,
            element: <DashboardPage />,
          },
          {
            path: "customers",
            element: <CustomersPage />,
          },
          {
            path: "pets",
            element: <PetsPage />,
          },
          {
            path: "veterinary-clinics",
            element: <VeterinaryClinicsPage />,
          },
          {
            path: "veterinarians",
            element: <VeterinariansPage />,
          },
          {
            path: "veterinary-requests",
            element: <VeterinaryRequestsPage />,
          },
          {
            path: "collections",
            element: <CollectionsPage />,
          },
          {
            path: "receptions",
            element: <ReceptionsPage />,
          },
          {
            path: "cremations",
            element: <CremationsPage />,
          },
          {
            path: "cremation-packages",
            element: <CremationPackagesPage />,
          },
          {
            path: "urns",
            element: <UrnsPage />,
          },
          {
            path: "cremation-pricing",
            element: <CremationPricingPage />,
          },
          {
            path: "payments",
            element: <PaymentsPage />,
          },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "inventory", element: <InventoryCatalogPage /> }] },
          { element: <PermissionRoute permission="Suppliers.View" />, children: [{ path: "suppliers", element: <SuppliersPage /> }] },
          { element: <PermissionRoute permission="Finance.View" />, children: [{ path: "expenses", element: <ExpensesPage /> }] },
          { element: <PermissionRoute permission="Permissions.Manage" />, children: [{ path: "permissions", element: <PermissionsPage /> }] },
          { element: <PermissionRoute permission="Purchasing.View" />, children: [{ path: "purchases", element: <PurchasesPage /> }] },
          { element: <PermissionRoute anyOf={["Finance.View", "Purchasing.View"]} />, children: [{ path: "spending-reports", element: <SpendingReportsPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "inventory-reports", element: <InventoryReportsPage /> }] },
          { element: <PermissionRoute permission="Purchasing.View" />, children: [{ path: "cost-analytics", element: <CostAnalyticsPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "inventory-scanner", element: <ScannerPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "bom", element: <BomPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "production", element: <ProductionPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "urn-inventory", element: <UrnInventoryPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "inventory-labels", element: <InventoryLabelsPage /> }] },
          { element: <PermissionRoute permission="Inventory.View" />, children: [{ path: "inventory-lots", element: <LotsPage /> }] },
          {
            path: "api-test",
            element: <ApiTestPage />,
          },
        ],
      },
    ],
  },
  {
    path: "*",
    element: <NotFoundPage />,
  },
]);
