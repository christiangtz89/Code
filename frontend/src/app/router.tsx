import { createBrowserRouter } from "react-router-dom";
import { ModulePlaceholderPage } from "../components/ui/ModulePlaceholderPage";
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
            path: "receptions",
            element: (
              <ModulePlaceholderPage
                title="Recepciones"
                description="Administra el ingreso, la cadena de custodia y las pertenencias."
              />
            ),
          },
          {
            path: "cremations",
            element: (
              <ModulePlaceholderPage
                title="Cremaciones"
                description="Administra los servicios y el flujo operativo de cremación."
              />
            ),
          },
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
