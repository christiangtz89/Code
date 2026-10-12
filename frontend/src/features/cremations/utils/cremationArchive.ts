import { hasPermission, isOwnerOrAdmin } from "../../auth/utils/permissions";
import type { Cremation } from "../types/cremation.types";

export function canArchiveOrRestoreCremation(cremation: Cremation): boolean {
  return (
    hasPermission("Cremations.Manage") &&
    (cremation.requiresPrivilegedArchive === false || isOwnerOrAdmin())
  );
}
