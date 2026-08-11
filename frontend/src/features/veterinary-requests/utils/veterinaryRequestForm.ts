import type { VeterinaryRequestConversionFormValues } from "../schemas/veterinaryRequestConversionSchema";
import type { VeterinaryRequestFormValues } from "../schemas/veterinaryRequestSchema";
import type { VeterinaryRequestStatusFormValues } from "../schemas/veterinaryRequestStatusSchema";
import type {
  ChangeVeterinaryRequestStatusPayload,
  ConvertVeterinaryRequestPayload,
  CreateVeterinaryRequestPayload,
  CremationType,
} from "../types/veterinaryRequest.types";

function normalizeOptional(value: string): string | null {
  const normalized = value.trim();

  return normalized.length > 0 ? normalized : null;
}

function dateToIso(value: string): string {
  return `${value}T00:00:00.000Z`;
}

export function createVeterinaryRequestPayload(
  values: VeterinaryRequestFormValues,
): CreateVeterinaryRequestPayload {
  return {
    veterinaryClinicId: normalizeOptional(values.veterinaryClinicId),

    referringVeterinarianId: normalizeOptional(values.referringVeterinarianId),

    ownerFirstName: values.ownerFirstName.trim(),

    ownerLastName: values.ownerLastName.trim(),

    ownerSecondLastName: normalizeOptional(values.ownerSecondLastName),

    ownerPhone: values.ownerPhone.trim(),

    ownerEmail: normalizeOptional(values.ownerEmail),

    petName: values.petName.trim(),

    species: values.species.trim(),

    breed: values.breed.trim(),

    sex: values.sex.trim(),

    color: values.color.trim(),

    approximateWeightKg: values.approximateWeightKg,

    ageYears: values.ageYears,

    dateOfDeath: dateToIso(values.dateOfDeath),

    requestedCremationType:
      values.requestedCremationType === ""
        ? null
        : (Number(values.requestedCremationType) as CremationType),

    requestedPackageName: normalizeOptional(values.requestedPackageName),

    requestNotes: normalizeOptional(values.requestNotes),
  };
}

export const updateVeterinaryRequestPayload = createVeterinaryRequestPayload;

export function createVeterinaryRequestStatusPayload(
  values: VeterinaryRequestStatusFormValues,
): ChangeVeterinaryRequestStatusPayload {
  return {
    status: values.status,

    internalNotes: normalizeOptional(values.internalNotes),

    rejectionReason:
      values.status === 4 ? normalizeOptional(values.rejectionReason) : null,
  };
}

export function createVeterinaryRequestConversionPayload(
  values: VeterinaryRequestConversionFormValues,
): ConvertVeterinaryRequestPayload {
  return {
    existingCustomerId: normalizeOptional(values.existingCustomerId),

    existingPetId: normalizeOptional(values.existingPetId),

    verifiedWeightKg: values.verifiedWeightKg,

    hasPersonalBelongings: values.hasPersonalBelongings,

    personalBelongingsDescription: values.hasPersonalBelongings
      ? normalizeOptional(values.personalBelongingsDescription)
      : null,

    referralNotes: normalizeOptional(values.referralNotes),

    notes: normalizeOptional(values.notes),
  };
}
