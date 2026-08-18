import { apiClient } from "../../../services/apiClient";

import type {
  CremationPackage,
  CreateCremationPackagePayload,
  UpdateCremationPackagePayload,
} from "../types";

export async function getCremationPackages(
  includeInactive = false,
): Promise<CremationPackage[]> {
  const response = await apiClient.get<CremationPackage[]>(
    "/CremationPackages",
    {
      params: {
        includeInactive,
      },
    },
  );

  return response.data;
}

export async function getPublicCremationPackages(): Promise<
  CremationPackage[]
> {
  const response = await apiClient.get<CremationPackage[]>(
    "/CremationPackages/public",
  );

  return response.data;
}

export async function getCremationPackageById(
  id: string,
): Promise<CremationPackage> {
  const response = await apiClient.get<CremationPackage>(
    `/CremationPackages/${id}`,
  );

  return response.data;
}

export async function createCremationPackage(
  payload: CreateCremationPackagePayload,
): Promise<CremationPackage> {
  const response = await apiClient.post<CremationPackage>(
    "/CremationPackages",
    payload,
  );

  return response.data;
}

export async function updateCremationPackage(
  id: string,
  payload: UpdateCremationPackagePayload,
): Promise<CremationPackage> {
  const response = await apiClient.put<CremationPackage>(
    `/CremationPackages/${id}`,
    payload,
  );

  return response.data;
}
