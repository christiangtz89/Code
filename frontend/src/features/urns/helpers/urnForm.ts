import type { CreateUrnPayload, UpdateUrnPayload, Urn } from "../types";

import type { UrnFormValues } from "../schemas";

export const DEFAULT_URN_FORM_VALUES: UrnFormValues = {
  name: "",
  description: null,
  price: 0,
  material: null,
  color: null,
  imageUrl: null,
  isPublic: true,
  displayOrder: 0,
  isActive: true,
};

export function urnToFormValues(urn: Urn): UrnFormValues {
  return {
    name: urn.name,
    description: urn.description,
    price: urn.price,
    material: urn.material,
    color: urn.color,
    imageUrl: urn.imageUrl,
    isPublic: urn.isPublic,
    displayOrder: urn.displayOrder,
    isActive: urn.isActive,
  };
}

export function urnFormToCreatePayload(
  values: UrnFormValues,
): CreateUrnPayload {
  return {
    ...values,
    name: values.name.trim(),
    description: normalizeOptional(values.description),
    material: normalizeOptional(values.material),
    color: normalizeOptional(values.color),
    imageUrl: normalizeOptional(values.imageUrl),
  };
}

export function urnFormToUpdatePayload(
  values: UrnFormValues,
): UpdateUrnPayload {
  return urnFormToCreatePayload(values);
}

function normalizeOptional(value: string | null): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();

  return trimmed.length > 0 ? trimmed : null;
}
