import {
  CremationType,
  type CreateCremationPayload,
  type Cremation,
  type UpdateCremationPayload,
} from "../types/cremation.types";
import type { CremationFormValues } from "../schemas/cremationSchema";

export const defaultCremationFormValues: CremationFormValues = {
  receptionId: "",
  assignedToUserId: "",
  cremationType: CremationType.Individual,
  packageName: "",
  includesUrn: false,
  urnDescription: "",
  includesPawPrint: false,
  includesCertificate: false,
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

    cremationType: values.cremationType,

    packageName: values.packageName.trim(),

    includesUrn: values.includesUrn,

    urnDescription: values.includesUrn
      ? normalizeOptional(values.urnDescription)
      : null,

    includesPawPrint: values.includesPawPrint,

    includesCertificate: values.includesCertificate,

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

    cremationType: values.cremationType,

    packageName: values.packageName.trim(),

    includesUrn: values.includesUrn,

    urnDescription: values.includesUrn
      ? normalizeOptional(values.urnDescription)
      : null,

    includesPawPrint: values.includesPawPrint,

    includesCertificate: values.includesCertificate,

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

    cremationType: cremation.cremationType,

    packageName: cremation.packageName,

    includesUrn: cremation.includesUrn,

    urnDescription: cremation.urnDescription ?? "",

    includesPawPrint: cremation.includesPawPrint,

    includesCertificate: cremation.includesCertificate,

    scheduledAt: isoToLocalDateTime(cremation.scheduledAt),

    specialInstructions: cremation.specialInstructions ?? "",

    notes: cremation.notes ?? "",
  };
}
