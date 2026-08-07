import { apiClient } from "../../../services/apiClient";
import type {
  CreateReceptionPayload,
  PagedReceptions,
  Reception,
  ReceptionListParams,
  UpdateReceptionPayload,
} from "../types/reception.types";

const RECEPTIONS_URL = "/Receptions";

export async function getReceptions(
  params: ReceptionListParams = {},
): Promise<PagedReceptions> {
  const response = await apiClient.get<PagedReceptions>(RECEPTIONS_URL, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 10,
    },
  });

  return response.data;
}

export async function getReceptionById(id: string): Promise<Reception> {
  const response = await apiClient.get<Reception>(`${RECEPTIONS_URL}/${id}`);

  return response.data;
}

export async function getReceptionByQrCode(qrCode: string): Promise<Reception> {
  const response = await apiClient.get<Reception>(
    `${RECEPTIONS_URL}/qr/${encodeURIComponent(qrCode.trim())}`,
  );

  return response.data;
}

export async function searchReceptions(search: string): Promise<Reception[]> {
  const response = await apiClient.get<Reception[]>(
    `${RECEPTIONS_URL}/search`,
    {
      params: {
        search: search.trim(),
      },
    },
  );

  return response.data;
}

export async function createReception(
  payload: CreateReceptionPayload,
): Promise<Reception> {
  const response = await apiClient.post<Reception>(RECEPTIONS_URL, payload);

  return response.data;
}

export async function updateReception(
  id: string,
  payload: UpdateReceptionPayload,
): Promise<Reception> {
  const response = await apiClient.put<Reception>(
    `${RECEPTIONS_URL}/${id}`,
    payload,
  );

  return response.data;
}

export async function deactivateReception(id: string): Promise<void> {
  await apiClient.delete(`${RECEPTIONS_URL}/${id}`);
}

export async function restoreReception(id: string): Promise<void> {
  await apiClient.patch(`${RECEPTIONS_URL}/${id}/restore`);
}
