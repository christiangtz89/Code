import type {
  CreateCremationPackagePayload,
  CremationPackage,
  UpdateCremationPackagePayload,
} from "../types";

import { CREMATION_PACKAGE_TYPE } from "../types";

import type { CremationPackageFormValues } from "../schemas";

export const DEFAULT_CREMATION_PACKAGE_FORM_VALUES: CremationPackageFormValues =
  {
    name: "",
    shortDescription: null,
    description: null,
    packageType: CREMATION_PACKAGE_TYPE.ASHES_RETURN,
    tier: 1,
    includesUrn: true,
    allowedUrnIds: [],
    includesPawPrint: false,
    accessoryDescription: null,
    includesCertificate: true,
    imageUrl: null,
    isPublic: true,
    displayOrder: 0,
    isActive: true,
  };

export function cremationPackageToFormValues(
  cremationPackage: CremationPackage,
): CremationPackageFormValues {
  return {
    name: cremationPackage.name,
    shortDescription: cremationPackage.shortDescription,
    description: cremationPackage.description,
    packageType: cremationPackage.packageType,
    tier: cremationPackage.tier,
    includesUrn: cremationPackage.includesUrn,
    allowedUrnIds: cremationPackage.allowedUrnIds ?? [],
    includesPawPrint: cremationPackage.includesPawPrint,
    accessoryDescription: cremationPackage.accessoryDescription,
    includesCertificate: cremationPackage.includesCertificate,
    imageUrl: cremationPackage.imageUrl,
    isPublic: cremationPackage.isPublic,
    displayOrder: cremationPackage.displayOrder,
    isActive: cremationPackage.isActive,
  };
}

export function cremationPackageFormToCreatePayload(
  values: CremationPackageFormValues,
): CreateCremationPackagePayload {
  return {
    ...values,
    name: values.name.trim(),
    allowedUrnIds: values.includesUrn ? [...new Set(values.allowedUrnIds)] : [],
    shortDescription: normalizeOptional(values.shortDescription),
    description: normalizeOptional(values.description),
    accessoryDescription: normalizeOptional(values.accessoryDescription),
    imageUrl: normalizeOptional(values.imageUrl),
  };
}

export function cremationPackageFormToUpdatePayload(
  values: CremationPackageFormValues,
): UpdateCremationPackagePayload {
  return cremationPackageFormToCreatePayload(values);
}

function normalizeOptional(value: string | null): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();

  return trimmed.length > 0 ? trimmed : null;
}
