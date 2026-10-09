import { hasPermission, isOwnerOrAdmin } from "../../auth/utils/permissions";
import { CremationStatus, type Cremation } from "../types/cremation.types";

export function isAccessoryDescriptionLocked(cremation: Cremation): boolean {
  return (
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
    cremation.status >= CremationStatus.InProgress &&
    cremation.status <= CremationStatus.Delivered &&
    (cremation.status !== CremationStatus.Delivered || isOwnerOrAdmin())
  );
}
