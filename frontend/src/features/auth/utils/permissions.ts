import { jwtDecode } from "jwt-decode";
import { tokenStorage } from "../../../services/tokenStorage";

interface PermissionClaims { role?: string | string[]; permission?: string | string[]; permissions?: string[]; }
export function currentPermissions(): string[] { const token=tokenStorage.get(); if(!token)return []; try{const claims=jwtDecode<PermissionClaims>(token);const values=[claims.permission,claims.permissions].flatMap(x=>Array.isArray(x)?x:x?[x]:[]);return [...new Set(values)];}catch{return [];} }
export function hasPermission(permission:string): boolean { const claims=tokenStorage.get(); if(!claims)return false; try{const decoded=jwtDecode<PermissionClaims>(claims);const roles=Array.isArray(decoded.role)?decoded.role:decoded.role?[decoded.role]:[];if(roles.includes("Admin"))return true;const values=currentPermissions();if(values.includes(permission))return true;if(permission.endsWith(".View"))return values.includes(`${permission.slice(0,-5)}.Manage`);return false;}catch{return false;} }
