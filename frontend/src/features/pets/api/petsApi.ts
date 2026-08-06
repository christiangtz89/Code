import { apiClient } from "../../../services/apiClient";
import type {
  CreatePetPayload,
  GetPetsParams,
  PagedPets,
  Pet,
  SearchPetsParams,
  UpdatePetPayload,
} from "../types/pet.types";

export async function getPets(params: GetPetsParams): Promise<PagedPets> {
  const response = await apiClient.get<PagedPets>("/Pets", {
    params,
  });

  return response.data;
}

export async function searchPets(params: SearchPetsParams): Promise<Pet[]> {
  const response = await apiClient.get<Pet[]>("/Pets/search", {
    params,
  });

  return response.data;
}

export async function createPet(payload: CreatePetPayload): Promise<Pet> {
  const response = await apiClient.post<Pet>("/Pets", payload);

  return response.data;
}

export async function updatePet(
  id: string,
  payload: UpdatePetPayload,
): Promise<Pet> {
  const response = await apiClient.put<Pet>(`/Pets/${id}`, payload);

  return response.data;
}

export async function deactivatePet(id: string): Promise<void> {
  await apiClient.delete(`/Pets/${id}`);
}

export async function restorePet(id: string): Promise<void> {
  await apiClient.patch(`/Pets/${id}/restore`);
}
