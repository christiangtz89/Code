import { CremationType } from "../../cremations/types/cremation.types";
import { WEIGHT_PRICING_INTERVAL, type WeightPricingInterval } from "../types";

export const WEIGHT_PRICING_INTERVAL_LABELS: Record<
  WeightPricingInterval,
  string
> = {
  [WEIGHT_PRICING_INTERVAL.FIVE_KG]: "Cada 5 kg",
  [WEIGHT_PRICING_INTERVAL.TEN_KG]: "Cada 10 kg",
};

export function getWeightPricingIntervalLabel(
  interval: WeightPricingInterval,
): string {
  return WEIGHT_PRICING_INTERVAL_LABELS[interval];
}

export function getCremationTypeLabel(cremationType: number): string {
  switch (cremationType) {
    case CremationType.Individual:
      return "Individual";

    case CremationType.Communal:
      return "Comunitaria";

    default:
      return "Desconocido";
  }
}

export function formatWeightRange(
  minimumWeightKg: number,
  maximumWeightKg: number,
): string {
  return `${minimumWeightKg.toFixed(2)} – ${maximumWeightKg.toFixed(2)} kg`;
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("es-MX", {
    style: "currency",
    currency: "MXN",
  }).format(value);
}

export function isWeightRangeForInterval(
  minimumWeightKg: number,
  maximumWeightKg: number,
  interval: WeightPricingInterval,
): boolean {
  const step = interval;

  if (minimumWeightKg < 0 || maximumWeightKg <= minimumWeightKg) {
    return false;
  }

  if (minimumWeightKg === 0) {
    return maximumWeightKg === step;
  }

  const previousBoundary = minimumWeightKg - 0.01;

  return (
    previousBoundary >= 0 &&
    previousBoundary % step === 0 &&
    maximumWeightKg % step === 0 &&
    maximumWeightKg - previousBoundary === step
  );
}

export interface WeightRangeOption {
  minimumWeightKg: number;
  maximumWeightKg: number;
  label: string;
}

export function getWeightRangeOptions(
  interval: WeightPricingInterval,
): WeightRangeOption[] {
  const ranges: WeightRangeOption[] = [];

  const maximumSupportedWeightKg = 100;
  const numberOfRanges = maximumSupportedWeightKg / interval;

  for (let index = 0; index < numberOfRanges; index += 1) {
    const lowerBoundary = index * interval;
    const maximumWeightKg = (index + 1) * interval;

    const minimumWeightKg =
      index === 0 ? 0 : Number((lowerBoundary + 0.01).toFixed(2));

    ranges.push({
      minimumWeightKg,
      maximumWeightKg,
      label: formatWeightRange(minimumWeightKg, maximumWeightKg),
    });
  }

  return ranges;
}
