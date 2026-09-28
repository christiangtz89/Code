import type { ReceptionCorrectionFormValues } from "../schemas/receptionCorrectionSchema";
import type {
  Reception,
  ReceptionCorrectionDraft,
} from "../types/reception.types";

function normalizeOptional(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

export function buildReceptionCorrectionDraft(
  reception: Reception,
  values: ReceptionCorrectionFormValues,
): ReceptionCorrectionDraft | null {
  const draft: ReceptionCorrectionDraft = {
    reason: values.reason.trim(),
  };

  const correctedWeight = Number(values.verifiedWeightKg);
  const currentReportedWeight =
    reception.latestReportedCorrectedWeightKg ?? reception.verifiedWeightKg;
  if (correctedWeight !== currentReportedWeight) {
    draft.verifiedWeightKg = { value: correctedWeight };
  }

  const veterinaryClinicId = normalizeOptional(values.veterinaryClinicId);
  if (veterinaryClinicId !== reception.veterinaryClinicId) {
    draft.veterinaryClinicId = { value: veterinaryClinicId };
  }

  const referringVeterinarianId = normalizeOptional(
    values.referringVeterinarianId,
  );
  if (referringVeterinarianId !== reception.referringVeterinarianId) {
    draft.referringVeterinarianId = { value: referringVeterinarianId };
  }

  if (values.hasPersonalBelongings !== reception.hasPersonalBelongings) {
    draft.hasPersonalBelongings = {
      value: values.hasPersonalBelongings,
    };
  }

  const belongingsDescription = values.hasPersonalBelongings
    ? normalizeOptional(values.personalBelongingsDescription)
    : null;
  if (belongingsDescription !== reception.personalBelongingsDescription) {
    draft.personalBelongingsDescription = {
      value: belongingsDescription,
    };
  }

  const referralNotes = normalizeOptional(values.referralNotes);
  if (referralNotes !== reception.referralNotes) {
    draft.referralNotes = { value: referralNotes };
  }

  return Object.keys(draft).length > 1 ? draft : null;
}

export function getReceptionCorrectionFingerprint(
  draft: ReceptionCorrectionDraft,
): string {
  return JSON.stringify(draft);
}
