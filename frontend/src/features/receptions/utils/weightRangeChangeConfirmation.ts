import axios from "axios";

import type { WeightRangeChangeConfirmationResponse } from "../types/reception.types";

export function getWeightRangeChangeConfirmation(
  error: unknown,
): WeightRangeChangeConfirmationResponse | null {
  if (!axios.isAxiosError(error) || error.response?.status !== 409) {
    return null;
  }

  const data = error.response.data;

  if (!data || typeof data !== "object") {
    return null;
  }

  const response = data as Partial<WeightRangeChangeConfirmationResponse>;

  if (
    response.code !== "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED" ||
    !response.weightChange
  ) {
    return null;
  }

  return response as WeightRangeChangeConfirmationResponse;
}
