export const CollectionLocationType = {
  CustomerHome: 1,
  VeterinaryLocation: 2,
} as const;

export type CollectionLocationType =
  (typeof CollectionLocationType)[keyof typeof CollectionLocationType];

export const CollectionStatus = {
  Collected: 1,
  Received: 2,
  Cancelled: 3,
} as const;

export type CollectionStatus =
  (typeof CollectionStatus)[keyof typeof CollectionStatus];

export interface Collection {
  id: string;

  petId: string;
  petName: string;
  petSpecies: string;

  customerId: string;
  customerName: string;
  customerPhone: string;

  collectedByUserId: string;
  collectedByUserName: string;

  locationType: CollectionLocationType;

  veterinaryClinicId: string | null;
  veterinaryClinicName: string | null;

  referringVeterinarianId: string | null;
  referringVeterinarianName: string | null;

  status: CollectionStatus;

  qrCode: string;

  pickupAddress: string;
  pickupContactName: string | null;
  pickupContactPhone: string | null;

  approximateWeightKg: number | null;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  notes: string | null;

  receptionId: string | null;
  receptionQrCode: string | null;

  collectedAt: string;
  receivedAt: string | null;
  cancelledAt: string | null;

  isActive: boolean;
  createdAt: string;
}

export interface CreateCollectionPayload {
  existingCustomerId: string | null;
  existingPetId: string | null;

  ownerFirstName: string | null;
  ownerLastName: string | null;
  ownerSecondLastName: string | null;
  ownerPhone: string | null;
  ownerEmail: string | null;

  petName: string | null;
  species: string | null;
  breed: string | null;
  sex: string | null;
  color: string | null;
  approximateWeightKg: number | null;
  ageYears: number | null;
  dateOfDeath: string | null;

  locationType: CollectionLocationType;

  veterinaryClinicId: string | null;
  referringVeterinarianId: string | null;

  pickupAddress: string;
  pickupContactName: string | null;
  pickupContactPhone: string | null;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  notes: string | null;
}

export interface UpdateCollectionPayload {
  locationType: CollectionLocationType;

  veterinaryClinicId: string | null;
  referringVeterinarianId: string | null;

  pickupAddress: string;
  pickupContactName: string | null;
  pickupContactPhone: string | null;

  approximateWeightKg: number | null;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  notes: string | null;
}

export interface ChangeCollectionStatusPayload {
  status: CollectionStatus;
}

export interface ConvertCollectionToReceptionPayload {
  verifiedWeightKg: number;

  hasPersonalBelongings: boolean;
  personalBelongingsDescription: string | null;

  referralNotes: string | null;
  notes: string | null;
}

export interface PagedCollections {
  items: Collection[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetCollectionsParams {
  page: number;
  pageSize: number;
  status?: CollectionStatus;
  locationType?: CollectionLocationType;
}

export interface SearchCollectionsParams {
  search: string;
  status?: CollectionStatus;
  locationType?: CollectionLocationType;
}
