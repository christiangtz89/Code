import { apiClient } from "../../../services/apiClient";
export interface RolePermissions {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  isProtected: boolean;
  permissions: string[];
}
export interface Permission {
  id: string;
  code: string;
  name: string;
}
export interface CreatedRole {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}
export const getRolePermissions = async () =>
  (await apiClient.get<RolePermissions[]>("/permissions/roles")).data;
export const getPermissions = async () =>
  (await apiClient.get<Permission[]>("/permissions")).data;
export const setRolePermissions = async (
  roleId: string,
  permissionIds: string[],
) => apiClient.put(`/permissions/roles/${roleId}/permissions`, permissionIds);
export const createRole = async (input: {
  name: string;
  description?: string;
  isActive: boolean;
}) => (await apiClient.post<CreatedRole>("/permissions/roles", input)).data;
export const updateRole = async (
  id: string,
  input: { name: string; description?: string; isActive: boolean },
) => apiClient.put(`/permissions/roles/${id}`, input);
export const getAuthorizationUsers = async () =>
  (await apiClient.get("/permissions/users")).data;
export const setUserRoles = async (id: string, roleIds: string[]) =>
  apiClient.put(`/permissions/users/${id}/roles`, roleIds);
