import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  createPaymentAccountSchema,
  type CreatePaymentAccountFormValues,
  type UpdatePaymentAccountFormValues,
} from "../schemas/paymentAccountSchema";
import type {
  PaymentAccount,
  PaymentCremationOption,
} from "../types/payment.types";
import { formatPaymentCurrency } from "../utils/paymentDisplay";

type PaymentAccountFormMode = "create" | "edit";

interface PaymentAccountFormModalProps {
  isOpen: boolean;
  mode: PaymentAccountFormMode;
  account: PaymentAccount | null;
  cremations: PaymentCremationOption[];
  isLoadingCremations: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onCreate: (values: CreatePaymentAccountFormValues) => Promise<void>;
  onUpdate: (values: UpdatePaymentAccountFormValues) => Promise<void>;
}

export function PaymentAccountFormModal({
  isOpen,
  mode,
  account,
  cremations,
  isLoadingCremations,
  isSubmitting,
  onClose,
  onCreate,
  onUpdate,
}: PaymentAccountFormModalProps) {
  const isCreate = mode === "create";

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreatePaymentAccountFormValues>({
    resolver: zodResolver(createPaymentAccountSchema),
    defaultValues: {
      cremationId: "",
      serviceTotal: 0,
    },
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (mode === "edit" && account) {
      reset({
        cremationId: account.cremationId,
        serviceTotal: account.serviceTotal,
      });

      return;
    }

    reset({
      cremationId: "",
      serviceTotal: 0,
    });
  }, [account, isOpen, mode, reset]);

  if (!isOpen) {
    return null;
  }

  const noAvailableCremations =
    isCreate && !isLoadingCremations && cremations.length === 0;

  async function submit(values: CreatePaymentAccountFormValues) {
    if (isCreate) {
      await onCreate(values);
      return;
    }

    await onUpdate({
      serviceTotal: values.serviceTotal,
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              {isCreate
                ? "Registrar cuenta de pago"
                : "Editar total del servicio"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {isCreate
                ? "Asigna el total del servicio a una cremación."
                : "Actualiza el total sin alterar el historial de pagos."}
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
          {isCreate ? (
            <div>
              <label
                htmlFor="cremationId"
                className="mb-1.5 block text-sm font-medium text-slate-700"
              >
                Cremación
              </label>

              <select
                id="cremationId"
                disabled={
                  isLoadingCremations || isSubmitting || noAvailableCremations
                }
                {...register("cremationId")}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              >
                <option value="">
                  {isLoadingCremations
                    ? "Cargando cremaciones..."
                    : "Selecciona una cremación"}
                </option>

                {cremations.map((cremation) => (
                  <option key={cremation.id} value={cremation.id}>
                    {cremation.petName} — {cremation.customerName} —{" "}
                    {cremation.packageName} — {cremation.qrCode}
                  </option>
                ))}
              </select>

              {errors.cremationId && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.cremationId.message}
                </p>
              )}

              {noAvailableCremations && (
                <p className="mt-2 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
                  No hay cremaciones disponibles para registrar una cuenta de
                  pago.
                </p>
              )}
            </div>
          ) : (
            account && (
              <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
                <p className="font-medium text-slate-900">{account.petName}</p>

                <p className="mt-1 text-sm text-slate-600">
                  {account.customerName}
                </p>

                <div className="mt-3 grid gap-2 text-sm text-slate-500 sm:grid-cols-2">
                  <p>QR: {account.qrCode}</p>
                  <p>Paquete: {account.packageName}</p>
                </div>

                <div className="mt-3 border-t border-slate-200 pt-3 text-sm">
                  <p className="text-slate-500">Ya pagado</p>

                  <p className="font-semibold text-slate-900">
                    {formatPaymentCurrency(account.amountPaid)}
                  </p>
                </div>
              </div>
            )
          )}

          <div>
            <label
              htmlFor="serviceTotal"
              className="mb-1.5 block text-sm font-medium text-slate-700"
            >
              Total del servicio
            </label>

            <div className="relative">
              <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sm text-slate-500">
                $
              </span>

              <input
                id="serviceTotal"
                type="number"
                min="0.01"
                max="9999999999.99"
                step="0.01"
                disabled={isSubmitting}
                {...register("serviceTotal", {
                  valueAsNumber: true,
                })}
                className="w-full rounded-lg border border-slate-300 py-2.5 pr-3 pl-7 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />
            </div>

            {errors.serviceTotal && (
              <p className="mt-1 text-sm text-red-600">
                {errors.serviceTotal.message}
              </p>
            )}

            {!isCreate && account && (
              <p className="mt-2 text-xs text-slate-500">
                El nuevo total no puede ser menor que lo ya pagado:{" "}
                {formatPaymentCurrency(account.amountPaid)}.
              </p>
            )}
          </div>

          <div className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting || noAvailableCremations}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSubmitting
                ? "Guardando..."
                : isCreate
                  ? "Registrar cuenta"
                  : "Guardar cambios"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
