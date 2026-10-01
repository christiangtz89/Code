import type { Reception } from "../types/reception.types";

type ReceptionOriginFields = Pick<
  Reception,
  "isVeterinaryRequestOrigin" | "isCollectionOrigin"
>;

type ReceptionReferralFields = Pick<
  Reception,
  "veterinaryClinicName" | "referringVeterinarianName"
>;

export function getReceptionOriginLabel(
  reception: ReceptionOriginFields,
): string {
  if (reception.isVeterinaryRequestOrigin) {
    return "Desde solicitud veterinaria";
  }

  if (reception.isCollectionOrigin) {
    return "Desde recolección";
  }

  return "Recepción directa";
}

export function getReceptionReferralSourceLabel(
  reception: ReceptionReferralFields,
): string {
  if (reception.veterinaryClinicName) {
    return reception.veterinaryClinicName;
  }

  if (reception.referringVeterinarianName) {
    return "Veterinario independiente";
  }

  return "Sin referencia veterinaria";
}
