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
  Pending: 4,
  Assigned: 5,
  Accepted: 6,
} as const;

export type CollectionStatus =
  (typeof CollectionStatus)[keyof typeof CollectionStatus];

export interface Collection {
  id: string;

  createdByUserId: string | null;
  createdByUserName: string | null;

  petId: string;
  petName: string;
  petSpecies: string;

  customerId: string;
  customerName: string;
  customerPhone: string | null;

  collectedByUserId: string | null;
  collectedByUserName: string | null;

  assignedDriverId: string | null;
  assignedDriverName: string | null;
  assignedByUserId: string | null;
  assignedByUserName: string | null;
  assignedAt: string | null;
  acceptedByUserId: string | null;
  acceptedByUserName: string | null;
  acceptedAt: string | null;

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

  collectedAt: string | null;
  receivedAt: string | null;
  receivedByUserId: string | null;
  receivedByUserName: string | null;
  cancelledAt: string | null;
  cancelledByUserId: string | null;
  cancelledByUserName: string | null;

  isActive: boolean;
  createdAt: string;
}

export interface CreateCollectionPayload {
  existingCustomerId: string | null;
  existingPetId: string | null;

  ownerFirstName: string | null;
  ownerLastName: string | null;
  ownerPhone: string | null;
  ownerEmail: string | null;

  petName: string | null;
  species: string | null;
  breed: string | null;
  sex: string | null;
  color: string | null;
  approximateWeightKg: number | null;
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

export interface CollectionCustomerOption {
  id: string;
  displayName: string;
  phone: string;
}

export interface CollectionPetOption {
  id: string;
  name: string;
  species: string;
  breed: string;
  weightKg: number;
}

export interface PaginatedCollectionCustomerOptions {
  items: CollectionCustomerOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface PaginatedCollectionPetOptions {
  items: CollectionPetOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetCollectionCustomerOptionsParams {
  search?: string;
  page: number;
  pageSize: number;
}

export interface GetCollectionPetOptionsParams {
  search?: string;
  page: number;
  pageSize: number;
}

export interface CollectionVeterinaryClinicOption {
  id: string;
  displayName: string;
  phone: string | null;
  address: string | null;
  primaryContactName: string | null;
}

export interface CollectionVeterinarianOption {
  id: string;
  displayName: string;
  veterinaryClinicId: string | null;
  phone: string | null;
}

export interface PaginatedCollectionVeterinaryClinicOptions {
  items: CollectionVeterinaryClinicOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface PaginatedCollectionVeterinarianOptions {
  items: CollectionVeterinarianOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetCollectionVeterinaryLookupParams {
  search?: string;
  page: number;
  pageSize: number;
}

export interface GetCollectionVeterinarianLookupParams extends GetCollectionVeterinaryLookupParams {
  veterinaryClinicId?: string;
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

export interface CollectionDriverOption {
  id: string;
  name: string;
}

export interface AssignCollectionPayload {
  driverUserId: string;
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
  page: number;
  pageSize: number;
  status?: CollectionStatus;
  locationType?: CollectionLocationType;
}
