import { jwtDecode } from "jwt-decode";
import { tokenStorage } from "../../../services/tokenStorage";

interface PermissionClaims {
  sub?: string;
  permission?: string | string[];
  permissions?: string[];
  pcms_owner?: string;
  role?: string | string[];
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?:
    string | string[];
}
export function currentUserId(): string | null {
  const token = tokenStorage.get();
  if (!token) return null;
  try {
    return jwtDecode<PermissionClaims>(token).sub ?? null;
  } catch {
    return null;
  }
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

export function currentRoles(): string[] {
  const token = tokenStorage.get();
  if (!token) return [];
  try {
    const claims = jwtDecode<PermissionClaims>(token);
    return [
      claims.role,
      claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
    ]
      .flatMap((value) => (Array.isArray(value) ? value : value ? [value] : []))
      .map((role) => role.trim().toUpperCase());
  } catch {
    return [];
  }
}

export function hasRole(normalizedRoleName: string): boolean {
  return currentRoles().includes(normalizedRoleName.trim().toUpperCase());
}

export function isOwnerOrAdmin(): boolean {
  const token = tokenStorage.get();
  if (!token) return false;
  try {
    const claims = jwtDecode<PermissionClaims>(token);
    if (claims.pcms_owner === "true") return true;
    return hasRole("ADMIN");
  } catch {
    return false;
  }
}

export function canApproveReceptionLifecycle(): boolean {
  return isOwnerOrAdmin() && hasPermission("Receptions.Manage");
}

export function canRequestReceptionDeactivation(): boolean {
  return (
    !isOwnerOrAdmin() &&
    hasRole("MANAGER") &&
    hasPermission("Receptions.Manage")
  );
}
