import { apiClient } from "../../../services/apiClient";
import type {
  GetVeterinaryClinicsParams,
  PaginatedVeterinaryClinics,
  SearchVeterinaryClinicsParams,
  Veterinarian,
  VeterinaryClinic,
  VeterinaryClinicPayload,
} from "../types/veterinaryClinic.types";

export async function getVeterinaryClinics(
  params: GetVeterinaryClinicsParams,
): Promise<PaginatedVeterinaryClinics> {
  const response = await apiClient.get<PaginatedVeterinaryClinics>(
    "/VeterinaryClinics",
    {
      params,
    },
  );

  return response.data;
}

export async function searchVeterinaryClinics(
  params: SearchVeterinaryClinicsParams,
): Promise<PaginatedVeterinaryClinics> {
  const response = await apiClient.get<PaginatedVeterinaryClinics>(
    "/VeterinaryClinics/search",
    {
      params,
    },
  );

  return response.data;
}

export async function createVeterinaryClinic(
  payload: VeterinaryClinicPayload,
): Promise<VeterinaryClinic> {
  const response = await apiClient.post<VeterinaryClinic>(
    "/VeterinaryClinics",
    payload,
  );

  return response.data;
}

export async function updateVeterinaryClinic(
  id: string,
  payload: VeterinaryClinicPayload,
): Promise<VeterinaryClinic> {
  const response = await apiClient.put<VeterinaryClinic>(
    `/VeterinaryClinics/${id}`,
    payload,
  );

  return response.data;
}

export async function deactivateVeterinaryClinic(id: string): Promise<void> {
  await apiClient.delete(`/VeterinaryClinics/${id}`);
}

export async function restoreVeterinaryClinic(id: string): Promise<void> {
  await apiClient.patch(`/VeterinaryClinics/${id}/restore`);
}

export async function getClinicVeterinarians(
  clinicId: string,
): Promise<Veterinarian[]> {
  const response = await apiClient.get<Veterinarian[]>(
    `/Veterinarians/clinic/${clinicId}`,
  );

  return response.data;
}
