import { apiClient } from "../../../services/apiClient";

import type { CreateUrnPayload, UpdateUrnPayload, Urn } from "../types";

export async function getUrns(includeInactive = false): Promise<Urn[]> {
  const response = await apiClient.get<Urn[]>("/Urns", {
    params: {
      includeInactive,
    },
  });

  return response.data;
}

export async function getPublicUrns(): Promise<Urn[]> {
  const response = await apiClient.get<Urn[]>("/Urns/public");

  return response.data;
}

export async function getUrnById(id: string): Promise<Urn> {
  const response = await apiClient.get<Urn>(`/Urns/${id}`);

  return response.data;
}

export async function createUrn(payload: CreateUrnPayload): Promise<Urn> {
  const response = await apiClient.post<Urn>("/Urns", payload);

  return response.data;
}

export async function updateUrn(
  id: string,
  payload: UpdateUrnPayload,
): Promise<Urn> {
  const response = await apiClient.put<Urn>(`/Urns/${id}`, payload);

  return response.data;
}
