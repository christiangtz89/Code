import { getCremationStatusLabel } from "../../cremations/utils/cremationLabels";
import {
  CremationStatus as CremationStatusValue,
  type CremationStatus,
} from "../../cremations/types/cremation.types";
import {
  ReceptionHistoryEventKind,
  ReceptionHistoryField,
  ReceptionHistoryStage,
  type ReceptionHistoryChange,
  type ReceptionHistoryEventKind as ReceptionHistoryEventKindValue,
  type ReceptionHistoryField as ReceptionHistoryFieldValue,
  type ReceptionHistoryStage as ReceptionHistoryStageValue,
} from "../types/reception.types";

export function getReceptionHistoryEventLabel(
  eventKind: ReceptionHistoryEventKindValue,
): string {
  const labels: Record<ReceptionHistoryEventKindValue, string> = {
    [ReceptionHistoryEventKind.PreLockEditAudit]:
      "Auditoría antes de cremación",
    [ReceptionHistoryEventKind.Correction]: "Enmienda",
    [ReceptionHistoryEventKind.Clarification]: "Aclaración",
  };
  return labels[eventKind] ?? "Evento de recepción";
}

export function getReceptionHistoryStageLabel(
  stage: ReceptionHistoryStageValue,
): string {
  const labels: Record<ReceptionHistoryStageValue, string> = {
    [ReceptionHistoryStage.BeforeCremation]: "Antes de la cremación",
    [ReceptionHistoryStage.CremationCreatedNotStarted]:
      "Cremación creada, sin iniciar",
    [ReceptionHistoryStage.CremationStarted]: "Cremación iniciada",
    [ReceptionHistoryStage.CremationCompleted]: "Cremación finalizada",
  };
  return labels[stage] ?? "Etapa no disponible";
}

export function getReceptionHistoryFieldLabel(
  field: ReceptionHistoryFieldValue,
  eventKind?: ReceptionHistoryEventKindValue,
  cremationStatus?: CremationStatus | null,
): string {
  if (
    field === ReceptionHistoryField.VerifiedWeightKg &&
    eventKind === ReceptionHistoryEventKind.Correction &&
    cremationStatus !== null &&
    cremationStatus !== undefined &&
    cremationStatus !== CremationStatusValue.Pending &&
    cremationStatus !== CremationStatusValue.Scheduled
  ) {
    return "Peso corregido reportado";
  }

  const labels: Record<ReceptionHistoryFieldValue, string> = {
    [ReceptionHistoryField.VerifiedWeightKg]: "Peso verificado",
    [ReceptionHistoryField.VeterinaryClinicId]: "Veterinaria",
    [ReceptionHistoryField.ReferringVeterinarianId]: "Veterinario referente",
    [ReceptionHistoryField.HasPersonalBelongings]: "Objetos personales",
    [ReceptionHistoryField.PersonalBelongingsDescription]:
      "Descripción de objetos personales",
    [ReceptionHistoryField.ReferralNotes]: "Notas de referencia",
  };
  return labels[field] ?? "Campo de recepción";
}

export function getReceptionHistoryStatusLabel(
  status: CremationStatus | null,
): string | null {
  return status === null ? null : getCremationStatusLabel(status);
}

export function getReceptionHistoryActorRoleLabel(role: string): string {
  const normalized = role.trim().toUpperCase();
  const labels: Record<string, string> = {
    OWNER: "Propietario",
    ADMIN: "Administrador",
    MANAGER: "Gerente",
    RECEPTIONIST: "Recepcionista",
  };
  return labels[normalized] ?? role;
}

export function getReceptionHistoryChangeValue(
  change: ReceptionHistoryChange,
  side: "original" | "new",
): string {
  const displayValue =
    side === "original" ? change.originalDisplayValue : change.newDisplayValue;
  const storedValue =
    side === "original" ? change.originalValue : change.newValue;
  const value = displayValue ?? storedValue;

  if (value === null || value.trim() === "") {
    return "Sin valor";
  }

  if (change.field === ReceptionHistoryField.HasPersonalBelongings) {
    return ["true", "sí", "si", "1"].includes(value.toLowerCase())
      ? "Sí"
      : "No";
  }

  return value;
}

export function getReceptionHistoryStoredValue(
  change: ReceptionHistoryChange,
  side: "original" | "new",
): string {
  const value = side === "original" ? change.originalValue : change.newValue;

  if (value === null || value.trim() === "") {
    return "Sin valor";
  }

  if (change.field === ReceptionHistoryField.HasPersonalBelongings) {
    return ["true", "sí", "si", "1"].includes(value.toLowerCase())
      ? "Sí"
      : "No";
  }

  return value;
}
