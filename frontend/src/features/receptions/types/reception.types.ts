import type { CremationStatus } from "../../cremations/types/cremation.types";

export interface Reception {
  id: string;
  petId: string;
  petName: string;

  customerId: string;
  customerName: string;

  receivedByUserId: string;
  receivedByUserName: string;

  veterinaryClinicId: string | null;
  veterinaryClinicName: string | null;

  referringVeterinarianId: string | null;
  referringVeterinarianName: string | null;

  isVeterinaryRequestOrigin: boolean;
  veterinaryRequestId: string | null;
  isCollectionOrigin: boolean;

  receivedAt: string;
  qrCode: string;
  verifiedWeightKg: number;
  latestReportedCorrectedWeightKg: number | null;

  hasCremation: boolean;
  cremationStatus: CremationStatus | null;
  currentHistoryStage: ReceptionHistoryStage;
  isNormalEditLocked: boolean;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  referralNotes: string | null;
  notes: string | null;

  isActive: boolean;
  createdAt: string;
}

export interface PagedReceptions {
  items: Reception[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CreateReceptionPayload {
  petId: string;
  veterinaryClinicId: string | null;
  referringVeterinarianId: string | null;
  verifiedWeightKg: number;
  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;
  referralNotes: string | null;
  notes: string | null;
}

export interface UpdateReceptionPayload {
  veterinaryClinicId: string | null;
  referringVeterinarianId: string | null;
  verifiedWeightKg: number;
  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;
  referralNotes: string | null;
  notes: string | null;
  confirmWeightRangeChange: boolean;
}

export interface WeightRangeChangeDetails {
  previousWeightKg: number;
  newWeightKg: number;

  previousMinimumWeightKg: number;
  previousMaximumWeightKg: number;

  newMinimumWeightKg: number;
  newMaximumWeightKg: number;

  previousPrice: number | null;
  newPrice: number | null;
  newCremationPriceId: string | null;
  priceDifference: number | null;
  amountPaid: number | null;
  remainingBalance: number | null;
  overpaymentAmount: number | null;
  requiresFinancialReview: boolean;
}

export interface WeightRangeChangeConfirmationResponse {
  success: false;
  code: "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED";
  message: string;
  weightChange: WeightRangeChangeDetails;
}

export const ReceptionHistoryEventKind = {
  PreLockEditAudit: 1,
  Correction: 2,
  Clarification: 3,
} as const;

export type ReceptionHistoryEventKind =
  (typeof ReceptionHistoryEventKind)[keyof typeof ReceptionHistoryEventKind];

export const ReceptionHistoryStage = {
  BeforeCremation: 1,
  CremationCreatedNotStarted: 2,
  CremationStarted: 3,
  CremationCompleted: 4,
} as const;

export type ReceptionHistoryStage =
  (typeof ReceptionHistoryStage)[keyof typeof ReceptionHistoryStage];

export const ReceptionHistoryField = {
  VerifiedWeightKg: 1,
  VeterinaryClinicId: 2,
  ReferringVeterinarianId: 3,
  HasPersonalBelongings: 4,
  PersonalBelongingsDescription: 5,
  ReferralNotes: 6,
} as const;

export type ReceptionHistoryField =
  (typeof ReceptionHistoryField)[keyof typeof ReceptionHistoryField];

export interface CorrectionValue<T> {
  value: T;
}

export interface CreateReceptionCorrectionPayload {
  requestId: string;
  reason: string;
  verifiedWeightKg?: CorrectionValue<number>;
  veterinaryClinicId?: CorrectionValue<string | null>;
  referringVeterinarianId?: CorrectionValue<string | null>;
  hasPersonalBelongings?: CorrectionValue<boolean>;
  personalBelongingsDescription?: CorrectionValue<string | null>;
  referralNotes?: CorrectionValue<string | null>;
  confirmWeightRangeChange: boolean;
  expectedCremationPriceId?: string | null;
  expectedNewPrice?: number | null;
}

export type ReceptionCorrectionDraft = Omit<
  CreateReceptionCorrectionPayload,
  | "requestId"
  | "confirmWeightRangeChange"
  | "expectedCremationPriceId"
  | "expectedNewPrice"
>;

export interface CreateReceptionClarificationPayload {
  requestId: string;
  text: string;
}

export interface ReceptionHistoryChange {
  field: ReceptionHistoryField;
  originalValue: string | null;
  newValue: string | null;
  originalDisplayValue: string | null;
  newDisplayValue: string | null;
}

export interface ReceptionHistoryEvent {
  id: string;
  receptionId: string;
  cremationId: string | null;
  requestId: string | null;
  sequence: number;
  eventKind: ReceptionHistoryEventKind;
  stage: ReceptionHistoryStage;
  cremationStatus: CremationStatus | null;
  reason: string | null;
  clarificationText: string | null;
  actorUserId: string;
  actorUserName: string;
  actorRole: string;
  createdAt: string;
  changes: ReceptionHistoryChange[];
}

export interface ReceptionListParams {
  page?: number;
  pageSize?: number;
}

export interface ReceptionPhoto {
  id: string;
  receptionId: string;
  uploadedByUserId: string;
  uploadedByUserName: string;
  photoType: ReceptionPhotoType;
  originalFileName: string;
  contentType: string;
  fileUrl: string;
  notes: string | null;
  isActive: boolean;
  uploadedAt: string;
}

export const ReceptionPhotoType = {
  Pet: 1,
  PersonalBelongings: 2,
  Identification: 3,
  Other: 4,
} as const;

export type ReceptionPhotoType =
  (typeof ReceptionPhotoType)[keyof typeof ReceptionPhotoType];
