import {
  VeterinaryRequestStatus,
  type VeterinaryRequestStatus as VeterinaryRequestStatusValue,
} from "../types/veterinaryRequest.types";
import { getVeterinaryRequestStatusLabel } from "../utils/veterinaryRequestLabels";

interface VeterinaryRequestStatusBadgeProps {
  status: VeterinaryRequestStatusValue;
}

function getStatusClasses(status: VeterinaryRequestStatusValue): string {
  switch (status) {
    case VeterinaryRequestStatus.Submitted:
      return "bg-blue-100 text-blue-700";

    case VeterinaryRequestStatus.UnderReview:
      return "bg-amber-100 text-amber-700";

    case VeterinaryRequestStatus.Approved:
      return "bg-emerald-100 text-emerald-700";

    case VeterinaryRequestStatus.Rejected:
      return "bg-red-100 text-red-700";

    case VeterinaryRequestStatus.Converted:
      return "bg-violet-100 text-violet-700";

    case VeterinaryRequestStatus.Cancelled:
      return "bg-slate-200 text-slate-700";

    default:
      return "bg-slate-100 text-slate-700";
  }
}

export function VeterinaryRequestStatusBadge({
  status,
}: VeterinaryRequestStatusBadgeProps) {
  return (
    <span
      className={[
        "inline-flex whitespace-nowrap rounded-full px-3 py-1 text-xs font-semibold",
        getStatusClasses(status),
      ].join(" ")}
    >
      {getVeterinaryRequestStatusLabel(status)}
    </span>
  );
}
