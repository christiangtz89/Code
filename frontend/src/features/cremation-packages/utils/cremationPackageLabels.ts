import { CREMATION_PACKAGE_TYPE, type CremationPackageType } from "../types";

export const CREMATION_PACKAGE_TYPE_LABELS: Record<
  CremationPackageType,
  string
> = {
  [CREMATION_PACKAGE_TYPE.ASHES_RETURN]: "Con devolución de cenizas",

  [CREMATION_PACKAGE_TYPE.NO_ASHES]: "Sin devolución de cenizas",
};

export function getCremationPackageTypeLabel(
  type: CremationPackageType,
): string {
  return CREMATION_PACKAGE_TYPE_LABELS[type];
}

export function getCremationPackageTierLabel(tier: number | null): string {
  if (tier === null) {
    return "Sin nivel";
  }

  return `Nivel ${tier}`;
}
