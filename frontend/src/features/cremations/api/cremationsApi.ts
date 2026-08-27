import { apiClient } from "../../../services/apiClient";
import type {
  ChangeCremationStatusPayload,
  CreateCremationPayload,
  Cremation,
  CremationStatus,
  GetCremationsParams,
  PaginatedCremations,
  SearchCremationsParams,
  UpdateCremationPayload,
} from "../types/cremation.types";
import type {
  CremationReceptionOption,
  CremationUserOption,
} from "../types/cremationForm.types";

export interface CremationInventory { cremationId:string; urnId:string|null; urnName:string|null; physicalStock:number; reservedQuantity:number; availableQuantity:number; reservation:{status:number; supplyItemId:string; reservedAt:string; fulfilledAt:string|null}|null; fulfillmentId:string|null; fulfilledAt:string|null; materials:Array<{supplyItemName:string;quantity:number;unitOfMeasure:string}>; }
export const getCremationInventory = async (id:string) => (await apiClient.get<CremationInventory>(`/cremations/${id}/inventory`)).data;
export const reserveCremationUrn = async (id:string) => (await apiClient.post<CremationInventory>(`/cremations/${id}/inventory/reserve`)).data;
export const cancelCremationUrn = async (id:string, reason?:string) => (await apiClient.post<CremationInventory>(`/cremations/${id}/inventory/cancel`, reason ?? null)).data;
export const fulfillCremationInventory = async (id:string, materials:unknown[]=[]) => (await apiClient.post<CremationInventory>(`/cremations/${id}/inventory/fulfill`, materials)).data;

export async function getAvailableCremationReceptions(): Promise<
  CremationReceptionOption[]
> {
  const response = await apiClient.get<CremationReceptionOption[]>(
    "/Cremations/options/receptions",
  );

  return response.data;
}

export async function getCremationUserOptions(): Promise<
  CremationUserOption[]
> {
  const response = await apiClient.get<CremationUserOption[]>(
    "/Cremations/options/users",
  );

  return response.data;
}

export async function getCremations(
  params: GetCremationsParams,
): Promise<PaginatedCremations> {
  const response = await apiClient.get<PaginatedCremations>("/Cremations", {
    params,
  });

  return response.data;
}

export async function getCremationById(id: string): Promise<Cremation> {
  const response = await apiClient.get<Cremation>(`/Cremations/${id}`);

  return response.data;
}

export async function getCremationByReception(
  receptionId: string,
): Promise<Cremation> {
  const response = await apiClient.get<Cremation>(
    `/Cremations/reception/${receptionId}`,
  );

  return response.data;
}

export async function getCremationsByStatus(
  status: CremationStatus,
): Promise<Cremation[]> {
  const response = await apiClient.get<Cremation[]>(
    `/Cremations/status/${status}`,
  );

  return response.data;
}

export async function searchCremations(
  params: SearchCremationsParams,
): Promise<Cremation[]> {
  const response = await apiClient.get<Cremation[]>("/Cremations/search", {
    params,
  });

  return response.data;
}

export async function createCremation(
  payload: CreateCremationPayload,
): Promise<Cremation> {
  const response = await apiClient.post<Cremation>("/Cremations", payload);

  return response.data;
}

export async function updateCremation(
  id: string,
  payload: UpdateCremationPayload,
): Promise<Cremation> {
  const response = await apiClient.put<Cremation>(`/Cremations/${id}`, payload);

  return response.data;
}

export async function changeCremationStatus(
  id: string,
  payload: ChangeCremationStatusPayload,
): Promise<Cremation> {
  const response = await apiClient.patch<Cremation>(
    `/Cremations/${id}/status`,
    payload,
  );

  return response.data;
}

export async function deactivateCremation(id: string): Promise<void> {
  await apiClient.delete(`/Cremations/${id}`);
}

export async function restoreCremation(id: string): Promise<void> {
  await apiClient.patch(`/Cremations/${id}/restore`);
}
