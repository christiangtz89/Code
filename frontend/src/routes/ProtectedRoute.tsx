import { Navigate, Outlet, useLocation } from "react-router-dom";
import { isAccessTokenValid } from "../features/auth/utils/authToken";
import { tokenStorage } from "../services/tokenStorage";

export function ProtectedRoute() {
  const location = useLocation();
  const token = tokenStorage.get();

  if (!token || !isAccessTokenValid(token)) {
    tokenStorage.remove();

    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
