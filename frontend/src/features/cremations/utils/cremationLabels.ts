import { CremationStatus, CremationType } from "../types/cremation.types";

export const cremationTypeLabels: Record<CremationType, string> = {
  [CremationType.Individual]: "Individual",
  [CremationType.Communal]: "Comunitaria",
};

export const cremationStatusLabels: Record<CremationStatus, string> = {
  [CremationStatus.Pending]: "Pendiente",
  [CremationStatus.Scheduled]: "Programada",
  [CremationStatus.InProgress]: "En proceso",
  [CremationStatus.Cooling]: "En enfriamiento",
  [CremationStatus.ProcessingRemains]: "Procesando restos",
  [CremationStatus.Completed]: "Completada",
  [CremationStatus.ReadyForDelivery]: "Lista para entrega",
  [CremationStatus.Delivered]: "Entregada",
  [CremationStatus.Cancelled]: "Cancelada",
};

export function getCremationTypeLabel(type: CremationType): string {
  return cremationTypeLabels[type] ?? "Tipo desconocido";
}

export function getCremationStatusLabel(status: CremationStatus): string {
  return cremationStatusLabels[status] ?? "Estado desconocido";
}
