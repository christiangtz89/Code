import type { ReceptionFormValues } from "../schemas/receptionSchema";
import type {
  CreateReceptionPayload,
  UpdateReceptionPayload,
} from "../types/reception.types";

function normalizeOptional(value: string): string | null {
  const normalizedValue = value.trim();

  return normalizedValue.length > 0 ? normalizedValue : null;
}

function createCommonPayload(values: ReceptionFormValues) {
  return {
    veterinaryClinicId: normalizeOptional(values.veterinaryClinicId),

    referringVeterinarianId: normalizeOptional(values.referringVeterinarianId),

    verifiedWeightKg: Number(values.verifiedWeightKg),

    hasPersonalBelongings: values.hasPersonalBelongings,

    personalBelongingsDescription: values.hasPersonalBelongings
      ? normalizeOptional(values.personalBelongingsDescription)
      : null,

    referralNotes: normalizeOptional(values.referralNotes),

    notes: normalizeOptional(values.notes),
  };
}

export function createReceptionPayload(
  values: ReceptionFormValues,
): CreateReceptionPayload {
  return {
    petId: values.petId,
    ...createCommonPayload(values),
  };
}

export function updateReceptionPayload(
  values: ReceptionFormValues,
): UpdateReceptionPayload {
  return createCommonPayload(values);
}
