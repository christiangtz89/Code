import {
  PaymentMethod,
  type CreateCollectionPaymentAccountPayload,
  type CreatePaymentAccountPayload,
  type CreatePaymentPayload,
} from "../types/payment.types";
import type { CreatePaymentAccountFormValues } from "../schemas/paymentAccountSchema";
import type { PaymentFormValues } from "../schemas/paymentSchema";

function normalizeOptional(value: string): string | null {
  const normalized = value.trim();

  return normalized.length > 0 ? normalized : null;
}

export function localDateTimeToIso(value: string): string {
  return new Date(value).toISOString();
}

export function isoToLocalDateTime(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const offset = date.getTimezoneOffset() * 60_000;

  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

export function getCurrentLocalDateTime(): string {
  const now = new Date();

  const offset = now.getTimezoneOffset() * 60_000;

  return new Date(now.getTime() - offset).toISOString().slice(0, 16);
}

export function createPaymentAccountPayload(
  values: CreatePaymentAccountFormValues,
): CreatePaymentAccountPayload {
  return {
    cremationId: values.selectionId,
  };
}

export function createCollectionPaymentAccountPayload(
  values: CreatePaymentAccountFormValues,
): CreateCollectionPaymentAccountPayload {
  const [collectionId, cremationPriceId] = values.selectionId.split("|");

  return {
    collectionId,
    cremationPriceId,
  };
}

export function createPaymentPayload(
  values: PaymentFormValues,
): CreatePaymentPayload {
  return {
    amount: values.amount,
    method: values.method,
    paidAt: localDateTimeToIso(values.paidAt),
    reference: normalizeOptional(values.reference),
    notes: normalizeOptional(values.notes),
  };
}

export const defaultPaymentFormValues: PaymentFormValues = {
  amount: 0,
  method: PaymentMethod.Cash,
  paidAt: getCurrentLocalDateTime(),
  reference: "",
  notes: "",
};
