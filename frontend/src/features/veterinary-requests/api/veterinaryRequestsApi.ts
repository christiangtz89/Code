import { apiClient } from "../../../services/apiClient";
import type {
  ChangeVeterinaryRequestStatusPayload,
  ConvertVeterinaryRequestPayload,
  CreateVeterinaryRequestPayload,
  GetVeterinaryRequestsParams,
  GetVeterinaryRequestLookupParams,
  GetVeterinaryRequestVeterinarianLookupParams,
  PaginatedLookup,
  PaginatedVeterinaryRequests,
  SearchVeterinaryRequestsParams,
  UpdateVeterinaryRequestPayload,
  VeterinaryRequest,
  VeterinaryRequestClinicOption,
  VeterinaryRequestCustomerOption,
  VeterinaryRequestPetOption,
  VeterinaryRequestVeterinarianOption,
} from "../types/veterinaryRequest.types";

const VETERINARY_REQUESTS_URL = "/VeterinaryRequests";

export async function getVeterinaryRequests(
  params: GetVeterinaryRequestsParams,
): Promise<PaginatedVeterinaryRequests> {
  const response = await apiClient.get<PaginatedVeterinaryRequests>(
    VETERINARY_REQUESTS_URL,
    {
      params,
    },
  );

  return response.data;
}

export async function getVeterinaryRequestById(
  id: string,
): Promise<VeterinaryRequest> {
  const response = await apiClient.get<VeterinaryRequest>(
    `${VETERINARY_REQUESTS_URL}/${id}`,
  );

  return response.data;
}

export async function createVeterinaryRequest(
  payload: CreateVeterinaryRequestPayload,
): Promise<VeterinaryRequest> {
  const response = await apiClient.post<VeterinaryRequest>(
    VETERINARY_REQUESTS_URL,
    payload,
  );

  return response.data;
}

export async function updateVeterinaryRequest(
  id: string,
  payload: UpdateVeterinaryRequestPayload,
): Promise<VeterinaryRequest> {
  const response = await apiClient.put<VeterinaryRequest>(
    `${VETERINARY_REQUESTS_URL}/${id}`,
    payload,
  );

  return response.data;
}

export async function changeVeterinaryRequestStatus(
  id: string,
  payload: ChangeVeterinaryRequestStatusPayload,
): Promise<VeterinaryRequest> {
  const response = await apiClient.patch<VeterinaryRequest>(
    `${VETERINARY_REQUESTS_URL}/${id}/status`,
    payload,
  );

  return response.data;
}

export async function convertVeterinaryRequest(
  id: string,
  payload: ConvertVeterinaryRequestPayload,
): Promise<VeterinaryRequest> {
  const response = await apiClient.post<VeterinaryRequest>(
    `${VETERINARY_REQUESTS_URL}/${id}/convert`,
    payload,
  );

  return response.data;
}

export async function searchVeterinaryRequests(
  params: SearchVeterinaryRequestsParams,
): Promise<PaginatedVeterinaryRequests> {
  const response = await apiClient.get<PaginatedVeterinaryRequests>(
    `${VETERINARY_REQUESTS_URL}/search`,
    {
      params,
    },
  );

  return response.data;
}

export async function getVeterinaryRequestClinicOptions(
  params: GetVeterinaryRequestLookupParams,
): Promise<PaginatedLookup<VeterinaryRequestClinicOption>> {
  const response = await apiClient.get<
    PaginatedLookup<VeterinaryRequestClinicOption>
  >(`${VETERINARY_REQUESTS_URL}/lookup/clinics`, { params });

  return response.data;
}

export async function getVeterinaryRequestVeterinarianOptions(
  params: GetVeterinaryRequestVeterinarianLookupParams,
): Promise<PaginatedLookup<VeterinaryRequestVeterinarianOption>> {
  const response = await apiClient.get<
    PaginatedLookup<VeterinaryRequestVeterinarianOption>
  >(`${VETERINARY_REQUESTS_URL}/lookup/veterinarians`, { params });

  return response.data;
}

export async function getVeterinaryRequestCustomerOptions(
  params: GetVeterinaryRequestLookupParams,
): Promise<PaginatedLookup<VeterinaryRequestCustomerOption>> {
  const response = await apiClient.get<
    PaginatedLookup<VeterinaryRequestCustomerOption>
  >(`${VETERINARY_REQUESTS_URL}/lookup/customers`, { params });

  return response.data;
}

export async function getVeterinaryRequestPetOptions(
  params: GetVeterinaryRequestLookupParams,
): Promise<PaginatedLookup<VeterinaryRequestPetOption>> {
  const response = await apiClient.get<
    PaginatedLookup<VeterinaryRequestPetOption>
  >(`${VETERINARY_REQUESTS_URL}/lookup/pets`, { params });

  return response.data;
}
