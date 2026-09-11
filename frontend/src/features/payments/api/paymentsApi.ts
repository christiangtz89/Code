import { apiClient } from "../../../services/apiClient";
import type {
  CreatePaymentAccountPayload,
  CreateCollectionPaymentAccountPayload,
  CreatePaymentPayload,
  GetPaymentAccountsParams,
  PaginatedPaymentAccounts,
  Payment,
  PaymentAccount,
  PaymentCremationOption,
  PaymentCollectionOption,
  SearchPaymentAccountsParams,
} from "../types/payment.types";

const PAYMENTS_URL = "/Payments";

export async function getPaymentAccounts(
  params: GetPaymentAccountsParams,
): Promise<PaginatedPaymentAccounts> {
  const response = await apiClient.get<PaginatedPaymentAccounts>(
    `${PAYMENTS_URL}/accounts`,
    {
      params,
    },
  );

  return response.data;
}

export async function getPaymentAccountById(
  id: string,
): Promise<PaymentAccount> {
  const response = await apiClient.get<PaymentAccount>(
    `${PAYMENTS_URL}/accounts/${id}`,
  );

  return response.data;
}

export async function getPaymentAccountByCremationId(
  cremationId: string,
): Promise<PaymentAccount> {
  const response = await apiClient.get<PaymentAccount>(
    `${PAYMENTS_URL}/accounts/cremation/${cremationId}`,
  );

  return response.data;
}

export async function searchPaymentAccounts(
  params: SearchPaymentAccountsParams,
): Promise<PaymentAccount[]> {
  const response = await apiClient.get<PaymentAccount[]>(
    `${PAYMENTS_URL}/accounts/search`,
    {
      params,
    },
  );

  return response.data;
}

export async function createPaymentAccount(
  payload: CreatePaymentAccountPayload,
): Promise<PaymentAccount> {
  const response = await apiClient.post<PaymentAccount>(
    `${PAYMENTS_URL}/accounts`,
    payload,
  );

  return response.data;
}

export async function createCollectionPaymentAccount(
  payload: CreateCollectionPaymentAccountPayload,
): Promise<PaymentAccount> {
  const response = await apiClient.post<PaymentAccount>(
    `${PAYMENTS_URL}/accounts/collection`,
    payload,
  );

  return response.data;
}

export async function addPayment(
  paymentAccountId: string,
  payload: CreatePaymentPayload,
): Promise<Payment> {
  const response = await apiClient.post<Payment>(
    `${PAYMENTS_URL}/accounts/${paymentAccountId}/payments`,
    payload,
  );

  return response.data;
}

export async function getPaymentHistory(
  paymentAccountId: string,
): Promise<Payment[]> {
  const response = await apiClient.get<Payment[]>(
    `${PAYMENTS_URL}/accounts/${paymentAccountId}/payments`,
  );

  return response.data;
}

export async function getAvailablePaymentCremations(): Promise<
  PaymentCremationOption[]
> {
  const response = await apiClient.get<PaymentCremationOption[]>(
    `${PAYMENTS_URL}/options/cremations`,
  );

  return response.data;
}

export async function getAvailablePaymentCollections(): Promise<
  PaymentCollectionOption[]
> {
  const response = await apiClient.get<PaymentCollectionOption[]>(
    `${PAYMENTS_URL}/options/collections`,
  );

  return response.data;
}
