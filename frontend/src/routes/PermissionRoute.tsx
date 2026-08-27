import { Navigate, Outlet } from "react-router-dom";
import { hasPermission } from "../features/auth/utils/permissions";
export function PermissionRoute({ permission, anyOf }: { permission?: string; anyOf?: string[] }) {
  const allowed = permission ? hasPermission(permission) : (anyOf ?? []).some(hasPermission);
  return allowed ? <Outlet /> : <Navigate to="/" replace />;
}
