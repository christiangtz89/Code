export interface Urn {
  id: string;

  name: string;

  description: string | null;

  price: number;

  material: string | null;

  color: string | null;

  imageUrl: string | null;

  isPublic: boolean;

  displayOrder: number;

  isActive: boolean;

  createdAt: string;

  updatedAt: string | null;
}

export interface CreateUrnPayload {
  name: string;

  description: string | null;

  price: number;

  material: string | null;

  color: string | null;

  imageUrl: string | null;

  isPublic: boolean;

  displayOrder: number;

  isActive: boolean;
}

export type UpdateUrnPayload = CreateUrnPayload;
