import {
  VeterinaryRequestStatus,
  type VeterinaryRequestStatus as VeterinaryRequestStatusValue,
} from "../types/veterinaryRequest.types";

export function canEditVeterinaryRequest(
  status: VeterinaryRequestStatusValue,
): boolean {
  return (
    status === VeterinaryRequestStatus.Submitted ||
    status === VeterinaryRequestStatus.UnderReview
  );
}

export function canConvertVeterinaryRequest(
  status: VeterinaryRequestStatusValue,
): boolean {
  return status === VeterinaryRequestStatus.Approved;
}

export function getAvailableVeterinaryRequestStatuses(
  status: VeterinaryRequestStatusValue,
): VeterinaryRequestStatusValue[] {
  switch (status) {
    case VeterinaryRequestStatus.Submitted:
      return [
        VeterinaryRequestStatus.UnderReview,
        VeterinaryRequestStatus.Cancelled,
      ];

    case VeterinaryRequestStatus.UnderReview:
      return [
        VeterinaryRequestStatus.Approved,
        VeterinaryRequestStatus.Rejected,
        VeterinaryRequestStatus.Cancelled,
      ];

    case VeterinaryRequestStatus.Approved:
      return [VeterinaryRequestStatus.Cancelled];

    default:
      return [];
  }
}
