import type { CollectionStatus } from "../../collections/types/collection.types";
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
  includeInactive?: boolean;
}

export const ReceptionLifecycleAction = {
  DeactivationRequested: 1,
  Deactivate: 2,
  Restore: 3,
} as const;

export type ReceptionLifecycleAction =
  (typeof ReceptionLifecycleAction)[keyof typeof ReceptionLifecycleAction];

export const ReceptionLifecycleOutcome = {
  Recorded: 1,
  Succeeded: 2,
  Blocked: 3,
} as const;

export type ReceptionLifecycleOutcome =
  (typeof ReceptionLifecycleOutcome)[keyof typeof ReceptionLifecycleOutcome];

export const ReceptionLifecycleDependency = {
  None: 0,
  Collection: 1,
  ConvertedVeterinaryRequest: 2,
  Cremation: 4,
  PaymentAccount: 8,
  PaymentHistory: 16,
  DirectReceptionEvidence: 32,
  CollectionEvidence: 64,
} as const;

export type ReceptionLifecycleDependency = number;

export const ReceptionLifecycleBlockReason = {
  None: 0,
  Dependencies: 1,
  ReceptionState: 2,
} as const;

export type ReceptionLifecycleBlockReason =
  (typeof ReceptionLifecycleBlockReason)[keyof typeof ReceptionLifecycleBlockReason];

export interface ReceptionLifecycleActionRequest {
  requestId: string;
  reason: string;
}

export interface ReceptionLifecycleEvent {
  id: string;
  receptionId: string;
  requestId: string;
  sequence: number;
  action: ReceptionLifecycleAction;
  outcome: ReceptionLifecycleOutcome;
  reason: string;
  actorUserId: string;
  actorUserName: string;
  actorRole: string;
  createdAt: string;
  previousIsActive: boolean;
  newIsActive: boolean;
  operationalStage: ReceptionHistoryStage;
  collectionId: string | null;
  collectionIsActive: boolean | null;
  collectionStatus: CollectionStatus | null;
  collectionCollectedAt: string | null;
  collectionReceivedAt: string | null;
  hasConvertedVeterinaryRequest: boolean;
  veterinaryRequestId: string | null;
  cremationId: string | null;
  cremationIsActive: boolean | null;
  cremationStatus: CremationStatus | null;
  paymentAccountId: string | null;
  serviceTotal: number | null;
  amountPaid: number | null;
  requiredCollectionPaymentAmount: number | null;
  requiresFinancialReview: boolean | null;
  paymentCount: number;
  activeReceptionEvidenceCount: number;
  collectionEvidenceCount: number;
  collectionAssignmentHistoryCount: number;
  receptionHistoryCount: number;
  latestReceptionHistorySequence: number | null;
  dependencies: ReceptionLifecycleDependency;
  blockReason: ReceptionLifecycleBlockReason;
}

export interface ReceptionLifecycleDecision {
  event: ReceptionLifecycleEvent;
  isReplay: boolean;
  isBlocked: boolean;
  message: string;
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
