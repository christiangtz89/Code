import { hasPermission, isOwnerOrAdmin } from "../../auth/utils/permissions";
import { CremationStatus, type Cremation } from "../types/cremation.types";

export function areInstructionsLocked(cremation: Cremation): boolean {
  return Boolean(cremation.startedAt) ||
    (cremation.status >= CremationStatus.InProgress &&
      cremation.status <= CremationStatus.Delivered);
}

export function canAmendCremationInstructions(cremation: Cremation): boolean {
  return cremation.isActive &&
    hasPermission("Cremations.Manage") &&
    cremation.status >= CremationStatus.InProgress &&
    cremation.status <= CremationStatus.Delivered &&
    (cremation.status !== CremationStatus.Delivered || isOwnerOrAdmin());
}
