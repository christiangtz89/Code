import {
  CremationType,
  VeterinaryRequestStatus,
  type CremationType as CremationTypeValue,
  type VeterinaryRequestStatus as VeterinaryRequestStatusValue,
} from "../types/veterinaryRequest.types";

const statusLabels: Record<VeterinaryRequestStatusValue, string> = {
  [VeterinaryRequestStatus.Submitted]: "Recibida",

  [VeterinaryRequestStatus.UnderReview]: "En revisión",

  [VeterinaryRequestStatus.Approved]: "Aprobada",

  [VeterinaryRequestStatus.Rejected]: "Rechazada",

  [VeterinaryRequestStatus.Converted]: "Convertida a recepción",

  [VeterinaryRequestStatus.Cancelled]: "Cancelada",
};

const cremationTypeLabels: Record<CremationTypeValue, string> = {
  [CremationType.Individual]: "Individual",

  [CremationType.Communal]: "Comunitaria",
};

export function getVeterinaryRequestStatusLabel(
  status: VeterinaryRequestStatusValue,
): string {
  return statusLabels[status] ?? "Estado desconocido";
}

export function getCremationTypeLabel(
  cremationType: CremationTypeValue | null,
): string {
  if (cremationType === null) {
    return "No especificado";
  }

  return cremationTypeLabels[cremationType] ?? "No especificado";
}
