export const VeterinaryRequestStatus = {
  Submitted: 1,
  UnderReview: 2,
  Approved: 3,
  Rejected: 4,
  Converted: 5,
  Cancelled: 6,
} as const;

export type VeterinaryRequestStatus =
  (typeof VeterinaryRequestStatus)[keyof typeof VeterinaryRequestStatus];

export const CremationType = {
  Individual: 1,
  Communal: 2,
} as const;

export type CremationType = (typeof CremationType)[keyof typeof CremationType];

export interface VeterinaryRequest {
  id: string;

  veterinaryClinicId: string | null;
  veterinaryClinicName: string | null;

  referringVeterinarianId: string | null;
  referringVeterinarianName: string | null;

  submittedByUserId: string;
  submittedByUserName: string;

  reviewedByUserId: string | null;
  reviewedByUserName: string | null;

  receptionId: string | null;
  receptionQrCode: string | null;

  status: VeterinaryRequestStatus;

  ownerFirstName: string;
  ownerLastName: string;
  ownerSecondLastName: string | null;
  ownerPhone: string;
  ownerEmail: string | null;

  petName: string;
  species: string;
  breed: string;
  sex: string;
  color: string;
  approximateWeightKg: number;
  ageYears: number | null;
  dateOfDeath: string;

  requestedCremationType: CremationType | null;
  requestedPackageName: string | null;

  requestNotes: string | null;
  internalNotes: string | null;
  rejectionReason: string | null;

  submittedAt: string;
  reviewedAt: string | null;
  convertedAt: string | null;
  createdAt: string;
}

export interface CreateVeterinaryRequestPayload {
  veterinaryClinicId: string | null;
  referringVeterinarianId: string | null;

  ownerFirstName: string;
  ownerLastName: string;
  ownerSecondLastName: string | null;
  ownerPhone: string;
  ownerEmail: string | null;

  petName: string;
  species: string;
  breed: string;
  sex: string;
  color: string;
  approximateWeightKg: number;
  ageYears: number | null;
  dateOfDeath: string;

  requestedCremationType: CremationType | null;
  requestedPackageName: string | null;
  requestNotes: string | null;
}

export type UpdateVeterinaryRequestPayload = CreateVeterinaryRequestPayload;

export interface ChangeVeterinaryRequestStatusPayload {
  status: VeterinaryRequestStatus;
  internalNotes: string | null;
  rejectionReason: string | null;
}

export interface ConvertVeterinaryRequestPayload {
  existingCustomerId: string | null;
  existingPetId: string | null;

  verifiedWeightKg: number;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  referralNotes: string | null;
  notes: string | null;
}

export interface PaginatedVeterinaryRequests {
  items: VeterinaryRequest[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetVeterinaryRequestsParams {
  page: number;
  pageSize: number;
  status?: VeterinaryRequestStatus;
}

export interface SearchVeterinaryRequestsParams {
  search: string;
  status?: VeterinaryRequestStatus;
}
