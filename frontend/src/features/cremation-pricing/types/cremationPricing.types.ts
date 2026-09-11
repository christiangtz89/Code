import type { CremationType } from "../../cremations/types/cremation.types";
import type { CremationPackageType } from "../../cremation-packages/types/cremationPackage.types";

export const WEIGHT_PRICING_INTERVAL = {
  FIVE_KG: 5,
  TEN_KG: 10,
} as const;

export type WeightPricingInterval =
  (typeof WEIGHT_PRICING_INTERVAL)[keyof typeof WEIGHT_PRICING_INTERVAL];

export interface CremationPricingConfiguration {
  id: string;

  weightInterval: WeightPricingInterval;

  allowIndividualNoAshes: boolean;

  createdAt: string;

  updatedAt: string | null;
}

export interface UpdateCremationPricingConfigurationPayload {
  weightInterval: WeightPricingInterval;

  allowIndividualNoAshes: boolean;
}

export interface CremationPrice {
  id: string;

  cremationPackageId: string;

  cremationPackageName: string;

  packageType: CremationPackageType;

  cremationType: CremationType;

  minimumWeightKg: number;

  maximumWeightKg: number;

  price: number;

  requiredCollectionPaymentAmount: number | null;

  isPublic: boolean;

  isActive: boolean;

  createdAt: string;

  updatedAt: string | null;
}

export interface CreateCremationPricePayload {
  cremationPackageId: string;

  cremationType: CremationType;

  minimumWeightKg: number;

  maximumWeightKg: number;

  price: number;

  requiredCollectionPaymentAmount: number;

  isPublic: boolean;

  isActive: boolean;
}

export type UpdateCremationPricePayload = CreateCremationPricePayload;

export interface CremationPriceQuote {
  cremationPackageId: string;

  cremationPackageName: string;

  packageType: CremationPackageType;

  cremationType: CremationType;

  weightKg: number;

  minimumWeightKg: number;

  maximumWeightKg: number;

  price: number;

  requiredCollectionPaymentAmount: number | null;
}
