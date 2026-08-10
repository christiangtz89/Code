import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  paymentSchema,
  type PaymentFormValues,
} from "../schemas/paymentSchema";
import { PaymentMethod, type PaymentAccount } from "../types/payment.types";
import { formatPaymentCurrency } from "../utils/paymentDisplay";
import {
  defaultPaymentFormValues,
  getCurrentLocalDateTime,
} from "../utils/paymentForm";

interface PaymentFormModalProps {
  isOpen: boolean;
  account: PaymentAccount | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: PaymentFormValues) => Promise<void>;
}

export function PaymentFormModal({
  isOpen,
  account,
  isSubmitting,
  onClose,
  onSubmit,
}: PaymentFormModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    setError,
    setValue,
    clearErrors,
    formState: { errors },
  } = useForm<PaymentFormValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: defaultPaymentFormValues,
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      ...defaultPaymentFormValues,
      paidAt: getCurrentLocalDateTime(),
    });
  }, [isOpen, reset]);

  if (!isOpen || !account) {
    return null;
  }
  const currentAccount = account;

  const accountIsPaid = account.balance <= 0;

  async function submit(values: PaymentFormValues) {
    clearErrors("amount");

    if (values.amount > currentAccount.balance) {
      setError("amount", {
        type: "manual",
        message: `El pago no puede exceder el saldo pendiente de ${formatPaymentCurrency(
          currentAccount.balance,
        )}.`,
      });

      return;
    }

    await onSubmit(values);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              Registrar pago
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Registra un anticipo, pago parcial o liquidación del servicio.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="rounded-lg px-3 py-2 text-sm font-medium text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            Cerrar
          </button>
        </header>

        <form onSubmit={handleSubmit(submit)} className="space-y-6 p-6">
          <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <p className="font-semibold text-slate-900">
                  {account.petName}
                </p>

                <p className="mt-1 text-sm text-slate-600">
                  {account.customerName}
                </p>

                <p className="mt-1 text-xs text-slate-500">
                  {account.packageName} · {account.qrCode}
                </p>
              </div>

              <div className="sm:text-right">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Saldo pendiente
                </p>

                <p className="mt-1 text-xl font-semibold text-slate-900">
                  {formatPaymentCurrency(account.balance)}
                </p>
              </div>
            </div>

            <div className="mt-4 grid gap-3 border-t border-slate-200 pt-4 sm:grid-cols-2">
              <div>
                <p className="text-xs text-slate-500">Total del servicio</p>

                <p className="font-medium text-slate-900">
                  {formatPaymentCurrency(account.serviceTotal)}
                </p>
              </div>

              <div>
                <p className="text-xs text-slate-500">Pagado hasta ahora</p>

                <p className="font-medium text-slate-900">
                  {formatPaymentCurrency(account.amountPaid)}
                </p>
              </div>
            </div>
          </div>

          {accountIsPaid ? (
            <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              Esta cuenta ya se encuentra pagada en su totalidad.
            </div>
          ) : (
            <>
              <div>
                <label
                  htmlFor="amount"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Monto del pago
                </label>

                <div className="relative">
                  <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sm text-slate-500">
                    $
                  </span>

                  <input
                    id="amount"
                    type="number"
                    min="0.01"
                    max={account.balance}
                    step="0.01"
                    disabled={isSubmitting}
                    {...register("amount", {
                      valueAsNumber: true,
                    })}
                    className="w-full rounded-lg border border-slate-300 py-2.5 pr-3 pl-7 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                  />
                </div>

                {errors.amount && (
                  <p className="mt-1 text-sm text-red-600">
                    {errors.amount.message}
                  </p>
                )}

                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => {
                    clearErrors("amount");

                    setValue("amount", currentAccount.balance, {
                      shouldValidate: true,
                      shouldDirty: true,
                    });
                  }}
                  className="mt-2 text-xs font-semibold text-slate-600 hover:text-slate-900"
                >
                  Usar saldo completo
                </button>
              </div>

              <div>
                <label
                  htmlFor="method"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Método de pago
                </label>

                <select
                  id="method"
                  disabled={isSubmitting}
                  {...register("method", {
                    valueAsNumber: true,
                  })}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value={PaymentMethod.Cash}>Efectivo</option>

                  <option value={PaymentMethod.CreditCard}>
                    Tarjeta de crédito
                  </option>

                  <option value={PaymentMethod.DebitCard}>
                    Tarjeta de débito
                  </option>

                  <option value={PaymentMethod.BankTransfer}>
                    Transferencia bancaria
                  </option>

                  <option value={PaymentMethod.Deposit}>Depósito</option>

                  <option value={PaymentMethod.Other}>Otro</option>
                </select>

                {errors.method && (
                  <p className="mt-1 text-sm text-red-600">
                    {errors.method.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="paidAt"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Fecha y hora del pago
                </label>

                <input
                  id="paidAt"
                  type="datetime-local"
                  max={getCurrentLocalDateTime()}
                  disabled={isSubmitting}
                  {...register("paidAt")}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.paidAt && (
                  <p className="mt-1 text-sm text-red-600">
                    {errors.paidAt.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="reference"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Referencia
                </label>

                <input
                  id="reference"
                  type="text"
                  maxLength={150}
                  disabled={isSubmitting}
                  placeholder="Opcional"
                  {...register("reference")}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.reference && (
                  <p className="mt-1 text-sm text-red-600">
                    {errors.reference.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="notes"
                  className="mb-1.5 block text-sm font-medium text-slate-700"
                >
                  Notas
                </label>

                <textarea
                  id="notes"
                  rows={4}
                  maxLength={1000}
                  disabled={isSubmitting}
                  placeholder="Observaciones adicionales..."
                  {...register("notes")}
                  className="w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.notes && (
                  <p className="mt-1 text-sm text-red-600">
                    {errors.notes.message}
                  </p>
                )}
              </div>
            </>
          )}

          <div className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            {!accountIsPaid && (
              <button
                type="submit"
                disabled={isSubmitting}
                className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSubmitting ? "Registrando..." : "Registrar pago"}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}
