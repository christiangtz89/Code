import {
  PaymentMethod,
  PaymentStatus,
  type PaymentMethod as PaymentMethodValue,
  type PaymentStatus as PaymentStatusValue,
} from "../types/payment.types";

const paymentMethodLabels: Record<PaymentMethodValue, string> = {
  [PaymentMethod.Cash]: "Efectivo",
  [PaymentMethod.CreditCard]: "Tarjeta de crédito",
  [PaymentMethod.DebitCard]: "Tarjeta de débito",
  [PaymentMethod.BankTransfer]: "Transferencia bancaria",
  [PaymentMethod.Deposit]: "Depósito",
  [PaymentMethod.Other]: "Otro",
};

const paymentStatusLabels: Record<PaymentStatusValue, string> = {
  [PaymentStatus.Pending]: "Pendiente",
  [PaymentStatus.PartiallyPaid]: "Pago parcial",
  [PaymentStatus.Paid]: "Pagado",
};

export function getPaymentMethodLabel(method: PaymentMethodValue): string {
  return paymentMethodLabels[method] ?? "Desconocido";
}

export function getPaymentStatusLabel(status: PaymentStatusValue): string {
  return paymentStatusLabels[status] ?? "Desconocido";
}
