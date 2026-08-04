import { apiClient } from '../../../services/apiClient'
import type {
  Customer,
  CustomerPayload,
  CustomerPet,
  GetCustomersParams,
  PaginatedCustomers,
  SearchCustomersParams,
} from '../types/customer.types'

export async function getCustomers(
  params: GetCustomersParams,
): Promise<PaginatedCustomers> {
  const response =
    await apiClient.get<PaginatedCustomers>('/Customers', {
      params,
    })

  return response.data
}

export async function searchCustomers(
  params: SearchCustomersParams,
): Promise<Customer[]> {
  const response = await apiClient.get<Customer[]>(
    '/Customers/search',
    {
      params,
    },
  )

  return response.data
}

export async function createCustomer(
  payload: CustomerPayload,
): Promise<Customer> {
  const response = await apiClient.post<Customer>(
    '/Customers',
    payload,
  )

  return response.data
}

export async function updateCustomer(
  id: string,
  payload: CustomerPayload,
): Promise<Customer> {
  const response = await apiClient.put<Customer>(
    `/Customers/${id}`,
    payload,
  )

  return response.data
}

export async function deactivateCustomer(
  id: string,
): Promise<void> {
  await apiClient.delete(`/Customers/${id}`)
}

export async function restoreCustomer(
  id: string,
): Promise<void> {
  await apiClient.put(`/Customers/${id}/restore`)
}

export async function getCustomerPets(
  customerId: string,
): Promise<CustomerPet[]> {
  const response = await apiClient.get<CustomerPet[]>(
    `/Pets/customer/${customerId}`,
  )

  return response.data
}