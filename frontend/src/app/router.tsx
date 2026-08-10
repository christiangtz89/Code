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
            element: <ReceptionsPage />,
          },
          {
            path: "cremations",
            element: <CremationsPage />,
          },
          {
            path: "payments",
            element: <PaymentsPage />,
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
