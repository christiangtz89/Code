export const CREMATION_PACKAGE_TYPE = {
  ASHES_RETURN: 1,
  NO_ASHES: 2,
} as const;

export type CremationPackageType =
  (typeof CREMATION_PACKAGE_TYPE)[keyof typeof CREMATION_PACKAGE_TYPE];

export interface CremationPackage {
  id: string;

  name: string;

  shortDescription: string | null;

  description: string | null;

  packageType: CremationPackageType;

  tier: number | null;

  includesUrn: boolean;

  allowedUrnIds: string[];

  includesPawPrint: boolean;

  accessoryDescription: string | null;

  includesCertificate: boolean;

  imageUrl: string | null;

  isPublic: boolean;

  displayOrder: number;

  isActive: boolean;

  createdAt: string;

  updatedAt: string | null;
}

export interface CreateCremationPackagePayload {
  name: string;

  shortDescription: string | null;

  description: string | null;

  packageType: CremationPackageType;

  tier: number | null;

  includesUrn: boolean;

  allowedUrnIds: string[];

  includesPawPrint: boolean;

  accessoryDescription: string | null;

  includesCertificate: boolean;

  imageUrl: string | null;

  isPublic: boolean;

  displayOrder: number;

  isActive: boolean;
}

export type UpdateCremationPackagePayload = CreateCremationPackagePayload;
