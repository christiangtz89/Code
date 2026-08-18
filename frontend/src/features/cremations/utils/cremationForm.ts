import type {
  CreateCremationPayload,
  Cremation,
  UpdateCremationPayload,
} from "../types/cremation.types";

import type { CremationFormValues } from "../schemas/cremationSchema";

export const defaultCremationFormValues: CremationFormValues = {
  receptionId: "",
  assignedToUserId: "",
  cremationPackageId: "",
  urnId: "",
  accessoryDescription: "",
  scheduledAt: "",
  specialInstructions: "",
  notes: "",
};

export function normalizeOptional(value: string): string | null {
  const normalized = value.trim();

  return normalized.length > 0 ? normalized : null;
}

export function localDateTimeToIso(value: string): string | null {
  if (!value.trim()) {
    return null;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

export function isoToLocalDateTime(value: string | null): string {
  if (!value) {
    return "";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const localDate = new Date(
    date.getTime() - date.getTimezoneOffset() * 60_000,
  );

  return localDate.toISOString().slice(0, 16);
}

export function createCremationPayload(
  values: CremationFormValues,
): CreateCremationPayload {
  return {
    receptionId: values.receptionId,

    assignedToUserId: normalizeOptional(values.assignedToUserId),

    cremationPackageId: values.cremationPackageId,

    urnId: values.urnId || null,

    accessoryDescription: values.accessoryDescription.trim() || null,

    scheduledAt: localDateTimeToIso(values.scheduledAt),

    specialInstructions: normalizeOptional(values.specialInstructions),

    notes: normalizeOptional(values.notes),
  };
}

export function updateCremationPayload(
  values: CremationFormValues,
): UpdateCremationPayload {
  return {
    assignedToUserId: normalizeOptional(values.assignedToUserId),

    cremationPackageId: values.cremationPackageId,

    urnId: values.urnId || null,

    accessoryDescription: values.accessoryDescription.trim() || null,

    scheduledAt: localDateTimeToIso(values.scheduledAt),

    specialInstructions: normalizeOptional(values.specialInstructions),

    notes: normalizeOptional(values.notes),
  };
}

export function cremationToFormValues(
  cremation: Cremation,
): CremationFormValues {
  return {
    receptionId: cremation.receptionId,

    assignedToUserId: cremation.assignedToUserId ?? "",

    cremationPackageId: cremation.cremationPackageId ?? "",

    urnId: cremation.urnId ?? "",

    accessoryDescription: cremation.accessoryDescription ?? "",

    scheduledAt: isoToLocalDateTime(cremation.scheduledAt),

    specialInstructions: cremation.specialInstructions ?? "",

    notes: cremation.notes ?? "",
  };
}
