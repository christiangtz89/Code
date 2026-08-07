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

  receivedAt: string;
  qrCode: string;
  verifiedWeightKg: number;

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
