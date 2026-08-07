import type { Cremation } from "../types/cremation.types";

export function formatCremationDateTime(value: string | null): string {
  if (!value) {
    return "No programada";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no válida";
  }

  return new Intl.DateTimeFormat("es-MX", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function getCremationInclusions(cremation: Cremation): string[] {
  const inclusions: string[] = [];

  if (cremation.includesUrn) {
    inclusions.push("Urna");
  }

  if (cremation.includesPawPrint) {
    inclusions.push("Huella");
  }

  if (cremation.includesCertificate) {
    inclusions.push("Certificado");
  }

  return inclusions;
}

export function getAssignedUserLabel(cremation: Cremation): string {
  return cremation.assignedToUserName?.trim() || "Sin asignar";
}
