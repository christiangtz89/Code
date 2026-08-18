export const CremationType = {
  Individual: 1,
  Communal: 2,
} as const;

export type CremationType = (typeof CremationType)[keyof typeof CremationType];

export const CremationStatus = {
  Pending: 1,
  Scheduled: 2,
  InProgress: 3,
  Cooling: 4,
  ProcessingRemains: 5,
  Completed: 6,
  ReadyForDelivery: 7,
  Delivered: 8,
  Cancelled: 9,
} as const;

export type CremationStatus =
  (typeof CremationStatus)[keyof typeof CremationStatus];

export interface Cremation {
  id: string;

  receptionId: string;
  qrCode: string;

  petId: string;
  petName: string;

  customerId: string;
  customerName: string;

  assignedToUserId: string | null;
  assignedToUserName: string | null;

  cremationType: CremationType;
  status: CremationStatus;

  cremationPackageId: string | null;
  cremationPackageName: string | null;

  packageName: string;

  urnId: string | null;
  urnName: string | null;

  includesUrn: boolean;
  urnDescription: string | null;

  includesPawPrint: boolean;
  accessoryDescription: string | null;

  includesCertificate: boolean;

  quotedPrice: number | null;

  quotedWeightKg: number | null;

  quotedMinimumWeightKg: number | null;

  quotedMaximumWeightKg: number | null;

  scheduledAt: string | null;
  startedAt: string | null;
  completedAt: string | null;
  readyForDeliveryAt: string | null;
  deliveredAt: string | null;

  specialInstructions: string | null;
  notes: string | null;

  isActive: boolean;
  createdAt: string;
}

export interface CreateCremationPayload {
  receptionId: string;
  assignedToUserId: string | null;

  cremationPackageId: string;
  urnId: string | null;
  accessoryDescription: string | null;

  scheduledAt: string | null;
  specialInstructions: string | null;
  notes: string | null;
}

export interface UpdateCremationPayload {
  assignedToUserId: string | null;

  cremationPackageId: string;
  urnId: string | null;
  accessoryDescription: string | null;

  scheduledAt: string | null;
  specialInstructions: string | null;
  notes: string | null;
}

export interface ChangeCremationStatusPayload {
  status: CremationStatus;
  notes: string | null;
}

export interface PaginatedCremations {
  items: Cremation[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetCremationsParams {
  page: number;
  pageSize: number;
  isActive: boolean;
}

export interface SearchCremationsParams {
  search: string;
  isActive: boolean;
}
