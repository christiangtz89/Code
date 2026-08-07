import { CremationStatus } from "../types/cremation.types";

export function getAllowedNextStatuses(
  currentStatus: CremationStatus,
): CremationStatus[] {
  switch (currentStatus) {
    case CremationStatus.Pending:
      return [CremationStatus.Scheduled, CremationStatus.Cancelled];

    case CremationStatus.Scheduled:
      return [CremationStatus.InProgress, CremationStatus.Cancelled];

    case CremationStatus.InProgress:
      return [CremationStatus.Cooling];

    case CremationStatus.Cooling:
      return [CremationStatus.ProcessingRemains];

    case CremationStatus.ProcessingRemains:
      return [CremationStatus.Completed];

    case CremationStatus.Completed:
      return [CremationStatus.ReadyForDelivery];

    case CremationStatus.ReadyForDelivery:
      return [CremationStatus.Delivered];

    case CremationStatus.Delivered:
    case CremationStatus.Cancelled:
      return [];

    default:
      return [];
  }
}

export function isTerminalCremationStatus(status: CremationStatus): boolean {
  return (
    status === CremationStatus.Delivered || status === CremationStatus.Cancelled
  );
}
