import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  createPaymentAccountSchema,
  type CreatePaymentAccountFormValues,
} from "../schemas/paymentAccountSchema";
import type { PaymentCremationOption } from "../types/payment.types";

interface PaymentAccountFormModalProps {
  isOpen: boolean;
  cremations: PaymentCremationOption[];
  isLoadingCremations: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onCreate: (values: CreatePaymentAccountFormValues) => Promise<void>;
}

export function PaymentAccountFormModal({
  isOpen,
  cremations,
  isLoadingCremations,
  isSubmitting,
  onClose,
  onCreate,
}: PaymentAccountFormModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreatePaymentAccountFormValues>({
    resolver: zodResolver(createPaymentAccountSchema),
    defaultValues: {
      cremationId: "",
    },
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      cremationId: "",
    });
  }, [isOpen, reset]);

  if (!isOpen) {
    return null;
  }

  const noAvailableCremations = !isLoadingCremations && cremations.length === 0;

  async function submit(values: CreatePaymentAccountFormValues) {
    await onCreate(values);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              Registrar cuenta de pago
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Selecciona la cremación para crear su cuenta de pago.
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

          <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-sm text-slate-600">
              El total del servicio se obtiene automáticamente de la cotización
              guardada en la cremación.
            </p>
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
              {isSubmitting ? "Guardando..." : "Registrar cuenta"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
