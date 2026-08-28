import { jwtDecode } from "jwt-decode";
import { tokenStorage } from "../../../services/tokenStorage";

interface PermissionClaims {
  permission?: string | string[];
  permissions?: string[];
  pcms_owner?: string;
}
export function currentPermissions(): string[] {
  const token = tokenStorage.get();
  if (!token) return [];
  try {
    const claims = jwtDecode<PermissionClaims>(token);
    const values = [claims.permission, claims.permissions].flatMap((x) =>
      Array.isArray(x) ? x : x ? [x] : [],
    );
    return [...new Set(values)];
  } catch {
    return [];
  }
}
export function hasPermission(permission: string): boolean {
  const token = tokenStorage.get();
  if (!token) return false;
  if (isOwner()) return true;
  const values = currentPermissions();
  if (values.includes(permission)) return true;
  if (permission.endsWith(".View"))
    return values.includes(`${permission.slice(0, -5)}.Manage`);
  if (permission === "Inventory.ScanOutgoing")
    return values.includes("Inventory.Manage");
  return false;
}
export function isOwner(): boolean {
  const token = tokenStorage.get();
  if (!token) return false;
  try {
    return jwtDecode<PermissionClaims>(token).pcms_owner === "true";
  } catch {
    return false;
  }
}
