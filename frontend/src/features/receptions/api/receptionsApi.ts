import { apiClient } from "../../../services/apiClient";
import type {
  CreateReceptionClarificationPayload,
  CreateReceptionCorrectionPayload,
  CreateReceptionPayload,
  PagedReceptions,
  Reception,
  ReceptionHistoryEvent,
  ReceptionLifecycleActionRequest,
  ReceptionLifecycleDecision,
  ReceptionLifecycleEvent,
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
      includeInactive: params.includeInactive ?? false,
    },
  });

  return response.data;
}

export async function getReceptionById(
  id: string,
  includeInactive = false,
): Promise<Reception> {
  const response = await apiClient.get<Reception>(`${RECEPTIONS_URL}/${id}`, {
    params: { includeInactive },
  });

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

export async function createReceptionCorrection(
  id: string,
  payload: CreateReceptionCorrectionPayload,
): Promise<ReceptionHistoryEvent> {
  const response = await apiClient.post<ReceptionHistoryEvent>(
    `${RECEPTIONS_URL}/${id}/amendments`,
    payload,
  );

  return response.data;
}

export async function createReceptionClarification(
  id: string,
  payload: CreateReceptionClarificationPayload,
): Promise<ReceptionHistoryEvent> {
  const response = await apiClient.post<ReceptionHistoryEvent>(
    `${RECEPTIONS_URL}/${id}/clarifications`,
    payload,
  );

  return response.data;
}

export async function getReceptionHistory(
  id: string,
): Promise<ReceptionHistoryEvent[]> {
  const response = await apiClient.get<ReceptionHistoryEvent[]>(
    `${RECEPTIONS_URL}/${id}/amendments`,
  );

  return response.data;
}

export async function deactivateReception(
  id: string,
  payload: ReceptionLifecycleActionRequest,
): Promise<ReceptionLifecycleDecision> {
  const response = await apiClient.post<ReceptionLifecycleDecision>(
    `${RECEPTIONS_URL}/${id}/lifecycle/deactivate`,
    payload,
  );

  return response.data;
}

export async function restoreReception(
  id: string,
  payload: ReceptionLifecycleActionRequest,
): Promise<ReceptionLifecycleDecision> {
  const response = await apiClient.post<ReceptionLifecycleDecision>(
    `${RECEPTIONS_URL}/${id}/lifecycle/restore`,
    payload,
  );

  return response.data;
}

export async function requestReceptionDeactivation(
  id: string,
  payload: ReceptionLifecycleActionRequest,
): Promise<ReceptionLifecycleDecision> {
  const response = await apiClient.post<ReceptionLifecycleDecision>(
    `${RECEPTIONS_URL}/${id}/lifecycle/deactivation-request`,
    payload,
  );

  return response.data;
}

export async function getReceptionLifecycleHistory(
  id: string,
): Promise<ReceptionLifecycleEvent[]> {
  const response = await apiClient.get<ReceptionLifecycleEvent[]>(
    `${RECEPTIONS_URL}/${id}/lifecycle`,
  );

  return response.data;
}
