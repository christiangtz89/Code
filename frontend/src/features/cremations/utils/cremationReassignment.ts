import {
  hasPermission,
  hasRole,
  isOwnerOrAdmin,
} from "../../auth/utils/permissions";
import { CremationStatus, type Cremation } from "../types/cremation.types";

export function isAssignmentLocked(cremation: Cremation): boolean {
  return (
    Boolean(cremation.startedAt) ||
    (cremation.status >= CremationStatus.InProgress &&
      cremation.status <= CremationStatus.Delivered)
  );
}

export function canReassignCremation(cremation: Cremation): boolean {
  return (
    cremation.isActive &&
    hasPermission("Cremations.Manage") &&
    cremation.status >= CremationStatus.InProgress &&
    cremation.status <= CremationStatus.Delivered &&
    (isOwnerOrAdmin() ||
      (hasRole("MANAGER") && cremation.status < CremationStatus.Delivered))
  );
}
