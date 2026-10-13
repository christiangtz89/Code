import { hasPermission, isOwnerOrAdmin, isOwnerOrProtectedAdmin } from "../../auth/utils/permissions";
import { CremationStatus, type Cremation } from "../types/cremation.types";

export function isAccessoryDescriptionLocked(cremation: Cremation): boolean {
  return (
    cremation.status === CremationStatus.Cancelled ||
    Boolean(cremation.startedAt) ||
    (cremation.status >= CremationStatus.InProgress &&
      cremation.status <= CremationStatus.Delivered)
  );
}

export function canAmendCremationAccessory(cremation: Cremation): boolean {
  return (
    cremation.isActive &&
    cremation.includesPawPrint &&
    hasPermission("Cremations.Manage") &&
    ((cremation.status >= CremationStatus.InProgress &&
      cremation.status <= CremationStatus.Delivered) ||
      cremation.status === CremationStatus.Cancelled) &&
    (cremation.status === CremationStatus.Delivered
      ? isOwnerOrProtectedAdmin()
      : cremation.status < CremationStatus.Delivered || isOwnerOrAdmin())
  );
}
