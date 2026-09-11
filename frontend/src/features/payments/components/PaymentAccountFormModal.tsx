import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  createPaymentAccountSchema,
  type CreatePaymentAccountFormValues,
} from "../schemas/paymentAccountSchema";
import type {
  PaymentCollectionOption,
  PaymentCremationOption,
} from "../types/payment.types";

interface PaymentAccountFormModalProps {
  isOpen: boolean;
  cremations: PaymentCremationOption[];
  collections: PaymentCollectionOption[];
  isLoadingOptions: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onCreate: (values: CreatePaymentAccountFormValues) => Promise<void>;
}

export function PaymentAccountFormModal({
  isOpen,
  cremations,
  collections,
  isLoadingOptions,
  isSubmitting,
  onClose,
  onCreate,
}: PaymentAccountFormModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    resetField,
    watch,
    formState: { errors },
  } = useForm<CreatePaymentAccountFormValues>({
    resolver: zodResolver(createPaymentAccountSchema),
    defaultValues: {
      sourceType: "collection",
      selectionId: "",
    },
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      sourceType: "collection",
      selectionId: "",
    });
  }, [isOpen, reset]);

  const sourceType = watch("sourceType");

  if (!isOpen) {
    return null;
  }

  const options =
    sourceType === "collection"
      ? collections.map((collection) => ({
          id: `${collection.collectionId}|${collection.cremationPriceId}`,
          label: `${collection.petName} — ${collection.customerName} — ${collection.packageName} — pago requerido ${collection.requiredCollectionPaymentAmount.toLocaleString("es-MX", { style: "currency", currency: "MXN" })} — ${collection.qrCode}`,
        }))
      : cremations.map((cremation) => ({
          id: cremation.id,
          label: `${cremation.petName} — ${cremation.customerName} — ${cremation.packageName} — ${cremation.qrCode}`,
        }));
  const noAvailableOptions = !isLoadingOptions && options.length === 0;

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
              Selecciona una recolección antes de recepción o una cremación.
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
          <div className="space-y-4">
            <div>
              <label
                htmlFor="sourceType"
                className="mb-1.5 block text-sm font-medium text-slate-700"
              >
                Etapa del servicio
              </label>

              <select
                id="sourceType"
                disabled={isSubmitting}
                {...register("sourceType", {
                  onChange: () => resetField("selectionId"),
                })}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900"
              >
                <option value="collection">
                  Recolección pendiente de recepción
                </option>
                <option value="cremation">Cremación existente</option>
              </select>
            </div>

            <label
              htmlFor="selectionId"
              className="mb-1.5 block text-sm font-medium text-slate-700"
            >
              Servicio
            </label>

            <select
              id="selectionId"
              disabled={isLoadingOptions || isSubmitting || noAvailableOptions}
              {...register("selectionId")}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            >
              <option value="">
                {isLoadingOptions
                  ? "Cargando servicios..."
                  : "Selecciona un servicio"}
              </option>

              {options.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>

            {errors.selectionId && (
              <p className="mt-1 text-sm text-red-600">
                {errors.selectionId.message}
              </p>
            )}

            {noAvailableOptions && (
              <p className="mt-2 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
                No hay servicios disponibles para registrar una cuenta de pago.
              </p>
            )}
          </div>

          <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-sm text-slate-600">
              Para recolecciones, el total y el pago requerido se obtienen del
              precio de cremación configurado para su peso y paquete.
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
              disabled={isSubmitting || noAvailableOptions}
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
