import { apiClient } from "../../../services/apiClient";

import type { CremationType } from "../../cremations/types/cremation.types";
import type {
  CremationPrice,
  CremationPriceQuote,
  CremationPricingConfiguration,
  CreateCremationPricePayload,
  UpdateCremationPricePayload,
  UpdateCremationPricingConfigurationPayload,
} from "../types";

export async function getCremationPricingConfiguration(): Promise<CremationPricingConfiguration> {
  const response = await apiClient.get<CremationPricingConfiguration>(
    "/CremationPricing/configuration",
  );

  return response.data;
}

export async function updateCremationPricingConfiguration(
  payload: UpdateCremationPricingConfigurationPayload,
): Promise<CremationPricingConfiguration> {
  const response = await apiClient.put<CremationPricingConfiguration>(
    "/CremationPricing/configuration",
    payload,
  );

  return response.data;
}

export interface GetCremationPricesParams {
  cremationPackageId?: string;
  cremationType?: CremationType;
  includeInactive?: boolean;
}

export async function getCremationPrices(
  params: GetCremationPricesParams = {},
): Promise<CremationPrice[]> {
  const response = await apiClient.get<CremationPrice[]>(
    "/CremationPricing/prices",
    {
      params,
    },
  );

  return response.data;
}

export async function getCremationPriceById(
  id: string,
): Promise<CremationPrice> {
  const response = await apiClient.get<CremationPrice>(
    `/CremationPricing/prices/${id}`,
  );

  return response.data;
}

export async function createCremationPrice(
  payload: CreateCremationPricePayload,
): Promise<CremationPrice> {
  const response = await apiClient.post<CremationPrice>(
    "/CremationPricing/prices",
    payload,
  );

  return response.data;
}

export async function updateCremationPrice(
  id: string,
  payload: UpdateCremationPricePayload,
): Promise<CremationPrice> {
  const response = await apiClient.put<CremationPrice>(
    `/CremationPricing/prices/${id}`,
    payload,
  );

  return response.data;
}

export interface GetCremationPriceQuoteParams {
  cremationPackageId: string;
  weightKg: number;
  cremationType?: CremationType;
}

export async function getCremationPriceQuote(
  params: GetCremationPriceQuoteParams,
): Promise<CremationPriceQuote> {
  const response = await apiClient.get<CremationPriceQuote>(
    "/CremationPricing/quote",
    {
      params,
    },
  );

  return response.data;
}
