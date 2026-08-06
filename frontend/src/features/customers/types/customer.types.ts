export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  secondLastName: string | null;
  phone: string;
  email: string;
  isActive: boolean;
  createdAt: string;
}

export interface PaginatedCustomers {
  items: Customer[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CustomerPayload {
  firstName: string;
  lastName: string;
  secondLastName: string;
  phone: string;
  email: string;
}

export interface GetCustomersParams {
  page: number;
  pageSize: number;
  isActive: boolean;
}

export interface SearchCustomersParams {
  term: string;
  isActive: boolean;
}

export interface CustomerPet {
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
