export interface VeterinaryClinic {
  id: string;
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  primaryContactName: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface VeterinaryClinicPayload {
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  primaryContactName: string | null;
}

export interface PaginatedVeterinaryClinics {
  items: VeterinaryClinic[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetVeterinaryClinicsParams {
  page: number;
  pageSize: number;
  isActive: boolean;
}

export interface SearchVeterinaryClinicsParams {
  search: string;
  isActive: boolean;
  page: number;
  pageSize: number;
}

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
