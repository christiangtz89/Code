import type { CollectionFormValues } from "../schemas/collectionSchema";
import type { CollectionReceptionFormValues } from "../schemas/collectionReceptionSchema";
import type { CollectionUpdateFormValues } from "../schemas/collectionUpdateSchema";
import type {
  CollectionLocationType,
  ConvertCollectionToReceptionPayload,
  CreateCollectionPayload,
  UpdateCollectionPayload,
} from "../types/collection.types";

function normalizeOptional(value: string): string | null {
  const normalized = value.trim();

  return normalized ? normalized : null;
}

function dateToIso(value: string): string {
  return `${value}T00:00:00.000Z`;
}

export function createCollectionPayload(
  values: CollectionFormValues,
): CreateCollectionPayload {
  const creatingCustomer = values.customerMode === "new";

  const creatingPet = values.petMode === "new";

  return {
    existingCustomerId: creatingCustomer ? null : values.existingCustomerId,

    existingPetId: creatingPet ? null : values.existingPetId,

    ownerFirstName: creatingCustomer ? values.ownerFirstName.trim() : null,

    ownerLastName: creatingCustomer ? values.ownerLastName.trim() : null,

    ownerSecondLastName: creatingCustomer
      ? normalizeOptional(values.ownerSecondLastName)
      : null,

    ownerPhone: creatingCustomer ? values.ownerPhone.trim() : null,

    ownerEmail: creatingCustomer ? values.ownerEmail.trim() : null,

    petName: creatingPet ? values.petName.trim() : null,

    species: creatingPet ? values.species.trim() : null,

    breed: creatingPet ? values.breed.trim() : null,

    sex: creatingPet ? values.sex.trim() : null,

    color: creatingPet ? values.color.trim() : null,

    approximateWeightKg: values.approximateWeightKg
      ? Number(values.approximateWeightKg)
      : null,

    ageYears:
      creatingPet && values.ageYears !== "" ? Number(values.ageYears) : null,

    dateOfDeath:
      creatingPet && values.dateOfDeath ? dateToIso(values.dateOfDeath) : null,

    locationType: Number(values.locationType) as CollectionLocationType,

    veterinaryClinicId: normalizeOptional(values.veterinaryClinicId),

    referringVeterinarianId: normalizeOptional(values.referringVeterinarianId),

    pickupAddress: values.pickupAddress.trim(),

    pickupContactName: normalizeOptional(values.pickupContactName),

    pickupContactPhone: normalizeOptional(values.pickupContactPhone),

    hasPersonalBelongings: values.hasPersonalBelongings,

    personalBelongingsDescription: values.hasPersonalBelongings
      ? normalizeOptional(values.personalBelongingsDescription)
      : null,

    notes: normalizeOptional(values.notes),
  };
}

export function updateCollectionPayload(
  values: CollectionUpdateFormValues,
): UpdateCollectionPayload {
  return {
    locationType: Number(values.locationType) as CollectionLocationType,

    veterinaryClinicId: normalizeOptional(values.veterinaryClinicId),

    referringVeterinarianId: normalizeOptional(values.referringVeterinarianId),

    pickupAddress: values.pickupAddress.trim(),

    pickupContactName: normalizeOptional(values.pickupContactName),

    pickupContactPhone: normalizeOptional(values.pickupContactPhone),

    approximateWeightKg: values.approximateWeightKg
      ? Number(values.approximateWeightKg)
      : null,

    hasPersonalBelongings: values.hasPersonalBelongings,

    personalBelongingsDescription: values.hasPersonalBelongings
      ? normalizeOptional(values.personalBelongingsDescription)
      : null,

    notes: normalizeOptional(values.notes),
  };
}

export function collectionReceptionPayload(
  values: CollectionReceptionFormValues,
): ConvertCollectionToReceptionPayload {
  return {
    verifiedWeightKg: Number(values.verifiedWeightKg),

    hasPersonalBelongings: values.hasPersonalBelongings,

    personalBelongingsDescription: values.hasPersonalBelongings
      ? normalizeOptional(values.personalBelongingsDescription)
      : null,

    referralNotes: normalizeOptional(values.referralNotes),

    notes: normalizeOptional(values.notes),
  };
}
