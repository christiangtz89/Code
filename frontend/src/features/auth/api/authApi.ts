import { apiClient } from '../../../services/apiClient'
import type {
  AuthResponse,
  LoginRequest,
} from '../types/auth.types'

export async function login(
  credentials: LoginRequest,
): Promise<AuthResponse> {
  const response = await apiClient.post<AuthResponse>(
    '/auth/login',
    credentials,
  )

  return response.data
}