export interface Pet {
  id: string;
  customerId: string;
  customerName: string;
  name: string;
  species: string;
  breed: string;
  sex: string;
  color: string;
  weightKg: number;
  ageYears: number | null;
  dateOfDeath: string;
  isActive: boolean;
  createdAt: string;
}

export interface PagedPets {
  items: Pet[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface PetOwnerOption {
  id: string;
  displayName: string;
}

export interface PagedPetOwnerOptions {
  items: PetOwnerOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetPetOwnerOptionsParams {
  search?: string;
  page: number;
  pageSize: number;
}

export interface CreatePetPayload {
  customerId: string;
  name: string;
  species: string;
  breed: string;
  sex: string;
  color: string;
  weightKg: number;
  ageYears: number | null;
  dateOfDeath: string;
}

export interface UpdatePetPayload {
  name: string;
  species: string;
  breed: string;
  sex: string;
  color: string;
  weightKg: number;
  ageYears: number | null;
  dateOfDeath: string;
}

export interface GetPetsParams {
  page: number;
  pageSize: number;
  isActive: boolean;
}

export interface SearchPetsParams {
  search: string;
  isActive: boolean;
}
