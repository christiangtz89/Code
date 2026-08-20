import { CremationType } from "../../cremations/types/cremation.types";

import type {
  CremationPriceFormValues,
  CremationPricingConfigurationFormValues,
} from "../schemas";

import type {
  CreateCremationPricePayload,
  UpdateCremationPricePayload,
  UpdateCremationPricingConfigurationPayload,
} from "../types";

import { WEIGHT_PRICING_INTERVAL } from "../types";

export const DEFAULT_PRICING_CONFIGURATION_FORM_VALUES: CremationPricingConfigurationFormValues =
  {
    weightInterval: WEIGHT_PRICING_INTERVAL.FIVE_KG,
    allowIndividualNoAshes: false,
  };

export const DEFAULT_CREMATION_PRICE_FORM_VALUES: CremationPriceFormValues = {
  cremationPackageId: "",
  cremationType: CremationType.Individual,
  minimumWeightKg: 0,
  maximumWeightKg: 5,
  price: "",
  isPublic: true,
  isActive: true,
};

export function pricingConfigurationFormToPayload(
  values: CremationPricingConfigurationFormValues,
): UpdateCremationPricingConfigurationPayload {
  return {
    weightInterval: values.weightInterval,
    allowIndividualNoAshes: values.allowIndividualNoAshes,
  };
}

function parsePrice(value: string): number {
  return Number(value.replace(",", "."));
}

export function cremationPriceFormToCreatePayload(
  values: CremationPriceFormValues,
): CreateCremationPricePayload {
  return {
    ...values,
    price: parsePrice(values.price),
  };
}

export function cremationPriceFormToUpdatePayload(
  values: CremationPriceFormValues,
): UpdateCremationPricePayload {
  return {
    ...values,
    price: parsePrice(values.price),
  };
}
