export const PaymentMethod = {
  Cash: 1,
  CreditCard: 2,
  DebitCard: 3,
  BankTransfer: 4,
  Deposit: 5,
  Other: 6,
} as const;

export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export const PaymentStatus = {
  Pending: 1,
  PartiallyPaid: 2,
  Paid: 3,
} as const;

export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export interface Payment {
  id: string;
  paymentAccountId: string;
  recordedByUserId: string;
  recordedByUserName: string;
  amount: number;
  method: PaymentMethod;
  paidAt: string;
  reference: string | null;
  notes: string | null;
  createdAt: string;
}

export interface PaymentAccount {
  id: string;
  cremationId: string;
  receptionId: string;
  qrCode: string;
  petId: string;
  petName: string;
  customerId: string;
  customerName: string;
  packageName: string;
  isCremationActive: boolean;
  serviceTotal: number;
  amountPaid: number;
  balance: number;
  status: PaymentStatus;
  createdAt: string;
  updatedAt: string | null;
  payments: Payment[];
}

export interface CreatePaymentAccountPayload {
  cremationId: string;
}

export interface CreatePaymentPayload {
  amount: number;
  method: PaymentMethod;
  paidAt: string;
  reference: string | null;
  notes: string | null;
}

export interface PaginatedPaymentAccounts {
  items: PaymentAccount[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface GetPaymentAccountsParams {
  page: number;
  pageSize: number;
  status?: PaymentStatus;
}

export interface SearchPaymentAccountsParams {
  search: string;
  status?: PaymentStatus;
}

export interface PaymentCremationOption {
  id: string;
  qrCode: string;
  petName: string;
  customerName: string;
  packageName: string;
}
