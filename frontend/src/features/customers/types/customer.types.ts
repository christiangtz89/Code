export interface Customer {
  id: string
  firstName: string
  lastName: string
  phone: string
  email: string
  isActive: boolean
  createdAt: string
}

export interface PaginatedCustomers {
  items: Customer[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface CustomerPayload {
  firstName: string
  lastName: string
  phone: string
  email: string
}

export interface GetCustomersParams {
  page: number
  pageSize: number
  isActive: boolean
}

export interface SearchCustomersParams {
  term: string
  isActive: boolean
}