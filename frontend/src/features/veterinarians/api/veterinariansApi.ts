import { apiClient } from "../../../services/apiClient";
import type {
  GetVeterinariansParams,
  PaginatedVeterinarians,
  SearchVeterinariansParams,
  Veterinarian,
  VeterinarianPayload,
} from "../types/veterinarian.types";

export async function getVeterinarians(
  params: GetVeterinariansParams,
): Promise<PaginatedVeterinarians> {
  const response = await apiClient.get<PaginatedVeterinarians>(
    "/Veterinarians",
    {
      params,
    },
  );

  return response.data;
}

export async function searchVeterinarians(
  params: SearchVeterinariansParams,
): Promise<Veterinarian[]> {
  const response = await apiClient.get<Veterinarian[]>(
    "/Veterinarians/search",
    {
      params,
    },
  );

  return response.data;
}

export async function getVeterinariansByClinic(
  veterinaryClinicId: string,
  isActive?: boolean,
): Promise<Veterinarian[]> {
  const response = await apiClient.get<Veterinarian[]>(
    `/Veterinarians/clinic/${veterinaryClinicId}`,
    {
      params: isActive === undefined ? undefined : { isActive },
    },
  );

  return response.data;
}

export async function createVeterinarian(
  payload: VeterinarianPayload,
): Promise<Veterinarian> {
  const response = await apiClient.post<Veterinarian>(
    "/Veterinarians",
    payload,
  );

  return response.data;
}

export async function updateVeterinarian(
  id: string,
  payload: VeterinarianPayload,
): Promise<Veterinarian> {
  const response = await apiClient.put<Veterinarian>(
    `/Veterinarians/${id}`,
    payload,
  );

  return response.data;
}

export async function deactivateVeterinarian(id: string): Promise<void> {
  await apiClient.delete(`/Veterinarians/${id}`);
}

export async function restoreVeterinarian(id: string): Promise<void> {
  await apiClient.patch(`/Veterinarians/${id}/restore`);
}
