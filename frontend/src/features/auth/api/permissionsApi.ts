import { apiClient } from "../../../services/apiClient";
export interface RolePermissions { id:string; name:string; permissions:string[]; }
export interface Permission { id:string; code:string; name:string; }
export const getRolePermissions=async()=>(await apiClient.get<RolePermissions[]>("/permissions/roles")).data;
export const getPermissions=async()=>(await apiClient.get<Permission[]>("/permissions")).data;
export const setRolePermissions=async(roleId:string,permissionIds:string[])=>apiClient.put(`/permissions/roles/${roleId}`,permissionIds);
