import { CremationType } from "../../cremations/types/cremation.types";
import { CREMATION_PACKAGE_TYPE, type CremationPackage } from "../types";

export function returnsAshes(cremationPackage: CremationPackage): boolean {
  return cremationPackage.packageType === CREMATION_PACKAGE_TYPE.ASHES_RETURN;
}

export function isNoAshesPackage(cremationPackage: CremationPackage): boolean {
  return cremationPackage.packageType === CREMATION_PACKAGE_TYPE.NO_ASHES;
}

export function getDefaultCremationType(cremationPackage: CremationPackage) {
  if (returnsAshes(cremationPackage)) {
    return CremationType.Individual;
  }

  return CremationType.Communal;
}

export function shouldShowUrnSelection(
  cremationPackage: CremationPackage,
): boolean {
  return returnsAshes(cremationPackage) && cremationPackage.includesUrn;
}

export function shouldShowAccessory(
  cremationPackage: CremationPackage,
): boolean {
  return returnsAshes(cremationPackage) && cremationPackage.includesPawPrint;
}

export function canSelectIndividualNoAshes(
  allowIndividualNoAshes: boolean,
  cremationPackage: CremationPackage,
): boolean {
  return allowIndividualNoAshes && isNoAshesPackage(cremationPackage);
}
