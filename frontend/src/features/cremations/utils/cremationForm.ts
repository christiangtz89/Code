import type {
  CreateCremationPayload,
  Cremation,
  UpdateCremationPayload,
} from "../types/cremation.types";

import type { CremationFormValues } from "../schemas/cremationSchema";

export const CREMATION_TIME_OPTIONS = Array.from({ length: 48 }, (_, index) => {
  const hours = Math.floor(index / 2);
  const minutes = index % 2 === 0 ? "00" : "30";

  return `${hours.toString().padStart(2, "0")}:${minutes}`;
});

export const defaultCremationFormValues: CremationFormValues = {
  receptionId: "",
  assignedToUserId: "",
  cremationPackageId: "",
  urnId: "",
  accessoryDescription: "",
  scheduledDate: "",
  scheduledTime: "",
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

function scheduledDateTimeToIso(date: string, time: string): string | null {
  if (!date || !time) {
    return null;
  }

  return localDateTimeToIso(`${date}T${time}`);
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

function splitScheduledDateTime(value: string | null): {
  scheduledDate: string;
  scheduledTime: string;
} {
  const localValue = isoToLocalDateTime(value);

  if (!localValue) {
    return {
      scheduledDate: "",
      scheduledTime: "",
    };
  }

  const [scheduledDate, time = ""] = localValue.split("T");

  return {
    scheduledDate,
    scheduledTime: time.slice(0, 5),
  };
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

    scheduledAt: scheduledDateTimeToIso(
      values.scheduledDate,
      values.scheduledTime,
    ),

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

    scheduledAt: scheduledDateTimeToIso(
      values.scheduledDate,
      values.scheduledTime,
    ),

    specialInstructions: normalizeOptional(values.specialInstructions),

    notes: normalizeOptional(values.notes),
  };
}

export function cremationToFormValues(
  cremation: Cremation,
): CremationFormValues {
  const schedule = splitScheduledDateTime(cremation.scheduledAt);
  return {
    receptionId: cremation.receptionId,

    assignedToUserId: cremation.assignedToUserId ?? "",

    cremationPackageId: cremation.cremationPackageId ?? "",

    urnId: cremation.urnId ?? "",

    accessoryDescription: cremation.accessoryDescription ?? "",

    scheduledDate: schedule.scheduledDate,

    scheduledTime: schedule.scheduledTime,

    specialInstructions: cremation.specialInstructions ?? "",

    notes: cremation.notes ?? "",
  };
}
