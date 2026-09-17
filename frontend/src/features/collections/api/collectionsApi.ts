import { apiClient } from "../../../services/apiClient";
import type {
  ChangeCollectionStatusPayload,
  AssignCollectionPayload,
  Collection,
  CollectionDriverOption,
  GetCollectionVeterinarianLookupParams,
  GetCollectionVeterinaryLookupParams,
  ConvertCollectionToReceptionPayload,
  CreateCollectionPayload,
  GetCollectionCustomerOptionsParams,
  GetCollectionPetOptionsParams,
  GetCollectionsParams,
  PaginatedCollectionCustomerOptions,
  PaginatedCollectionPetOptions,
  PaginatedCollectionVeterinarianOptions,
  PaginatedCollectionVeterinaryClinicOptions,
  PagedCollections,
  SearchCollectionsParams,
  UpdateCollectionPayload,
} from "../types/collection.types";

const COLLECTIONS_URL = "/Collections";

export async function getCollections(
  params: GetCollectionsParams,
): Promise<PagedCollections> {
  const response = await apiClient.get<PagedCollections>(COLLECTIONS_URL, {
    params,
  });

  return response.data;
}

export async function getCollectionById(id: string): Promise<Collection> {
  const response = await apiClient.get<Collection>(`${COLLECTIONS_URL}/${id}`);

  return response.data;
}

export async function getCollectionByQrCode(
  qrCode: string,
): Promise<Collection> {
  const response = await apiClient.get<Collection>(
    `${COLLECTIONS_URL}/qr/${encodeURIComponent(qrCode.trim())}`,
  );

  return response.data;
}

export async function createCollection(
  payload: CreateCollectionPayload,
): Promise<Collection> {
  const response = await apiClient.post<Collection>(COLLECTIONS_URL, payload);

  return response.data;
}

export async function getCollectionCustomerOptions(
  params: GetCollectionCustomerOptionsParams,
): Promise<PaginatedCollectionCustomerOptions> {
  const response = await apiClient.get<PaginatedCollectionCustomerOptions>(
    `${COLLECTIONS_URL}/intake/customer-options`,
    { params },
  );

  return response.data;
}

export async function getCollectionPetOptions(
  customerId: string,
  params: GetCollectionPetOptionsParams,
): Promise<PaginatedCollectionPetOptions> {
  const response = await apiClient.get<PaginatedCollectionPetOptions>(
    `${COLLECTIONS_URL}/intake/customer-options/${customerId}/pets`,
    { params },
  );

  return response.data;
}

export async function getCollectionVeterinaryClinicOptions(
  params: GetCollectionVeterinaryLookupParams,
): Promise<PaginatedCollectionVeterinaryClinicOptions> {
  const response =
    await apiClient.get<PaginatedCollectionVeterinaryClinicOptions>(
      `${COLLECTIONS_URL}/intake/veterinary-clinic-options`,
      { params },
    );

  return response.data;
}

export async function getCollectionVeterinarianOptions(
  params: GetCollectionVeterinarianLookupParams,
): Promise<PaginatedCollectionVeterinarianOptions> {
  const response = await apiClient.get<PaginatedCollectionVeterinarianOptions>(
    `${COLLECTIONS_URL}/intake/veterinarian-options`,
    { params },
  );

  return response.data;
}

export async function updateCollection(
  id: string,
  payload: UpdateCollectionPayload,
): Promise<Collection> {
  const response = await apiClient.put<Collection>(
    `${COLLECTIONS_URL}/${id}`,
    payload,
  );

  return response.data;
}

export async function changeCollectionStatus(
  id: string,
  payload: ChangeCollectionStatusPayload,
): Promise<Collection> {
  const response = await apiClient.patch<Collection>(
    `${COLLECTIONS_URL}/${id}/status`,
    payload,
  );

  return response.data;
}

export async function getCollectionDriverOptions(): Promise<
  CollectionDriverOption[]
> {
  const response = await apiClient.get<CollectionDriverOption[]>(
    `${COLLECTIONS_URL}/driver-options`,
  );

  return response.data;
}

export async function assignCollection(
  id: string,
  payload: AssignCollectionPayload,
): Promise<Collection> {
  const response = await apiClient.post<Collection>(
    `${COLLECTIONS_URL}/${id}/assign`,
    payload,
  );

  return response.data;
}

export async function acceptCollection(id: string): Promise<Collection> {
  const response = await apiClient.post<Collection>(
    `${COLLECTIONS_URL}/${id}/accept`,
  );

  return response.data;
}

export async function confirmCollectionCustody(
  id: string,
): Promise<Collection> {
  const response = await apiClient.post<Collection>(
    `${COLLECTIONS_URL}/${id}/confirm-custody`,
  );

  return response.data;
}

export async function convertCollectionToReception(
  id: string,
  payload: ConvertCollectionToReceptionPayload,
): Promise<Collection> {
  const response = await apiClient.post<Collection>(
    `${COLLECTIONS_URL}/${id}/convert-to-reception`,
    payload,
  );

  return response.data;
}

export async function searchCollections(
  params: SearchCollectionsParams,
): Promise<PagedCollections> {
  const response = await apiClient.get<PagedCollections>(
    `${COLLECTIONS_URL}/search`,
    {
      params,
    },
  );

  return response.data;
}
