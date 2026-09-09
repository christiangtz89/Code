export interface Veterinarian {
  id: string;
  veterinaryClinicId: string | null;
  veterinaryClinicName: string | null;
  firstName: string;
  lastName: string;
  secondLastName: string | null;
  phone: string | null;
  email: string | null;
  professionalLicenseNumber: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface VeterinarianPayload {
  veterinaryClinicId: string | null;
  firstName: string;
  lastName: string;
  secondLastName: string | null;
  phone: string | null;
  email: string | null;
  professionalLicenseNumber: string | null;
}

export interface PaginatedVeterinarians {
  items: Veterinarian[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetVeterinariansParams {
  page: number;
  pageSize: number;
  isActive: boolean;
  veterinaryClinicId?: string;
}

export interface SearchVeterinariansParams {
  search: string;
  isActive: boolean;
  veterinaryClinicId?: string;
  page: number;
  pageSize: number;
}

export interface VeterinarianClinicOption {
  id: string;
  displayName: string;
  isActive?: boolean;
}

export interface PaginatedVeterinarianClinicOptions {
  items: VeterinarianClinicOption[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetVeterinarianClinicOptionsParams {
  search?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
}
