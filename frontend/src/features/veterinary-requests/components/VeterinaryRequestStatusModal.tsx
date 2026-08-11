import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  veterinaryRequestStatusSchema,
  type VeterinaryRequestStatusFormValues,
} from "../schemas/veterinaryRequestStatusSchema";
import {
  VeterinaryRequestStatus,
  type VeterinaryRequest,
} from "../types/veterinaryRequest.types";
import { getVeterinaryRequestStatusLabel } from "../utils/veterinaryRequestLabels";
import { getAvailableVeterinaryRequestStatuses } from "../utils/veterinaryRequestWorkflow";

interface VeterinaryRequestStatusModalProps {
  isOpen: boolean;
  request: VeterinaryRequest | null;
  isSubmitting: boolean;
  onClose: () => void;

  onSubmit: (values: VeterinaryRequestStatusFormValues) => void;
}

export function VeterinaryRequestStatusModal({
  isOpen,
  request,
  isSubmitting,
  onClose,
  onSubmit,
}: VeterinaryRequestStatusModalProps) {
  const availableStatuses = request
    ? getAvailableVeterinaryRequestStatuses(request.status)
    : [];

  const defaultStatus = availableStatuses[0] as
    VeterinaryRequestStatusFormValues["status"] | undefined;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<VeterinaryRequestStatusFormValues>({
    resolver: zodResolver(veterinaryRequestStatusSchema),

    defaultValues: {
      status: defaultStatus ?? VeterinaryRequestStatus.UnderReview,

      internalNotes: "",
      rejectionReason: "",
    },
  });

  useEffect(() => {
    if (!isOpen || !request || !defaultStatus) {
      return;
    }

    reset({
      status: defaultStatus,
      internalNotes: "",
      rejectionReason: "",
    });
  }, [defaultStatus, isOpen, request, reset]);

  if (!isOpen || !request || availableStatuses.length === 0) {
    return null;
  }

  const selectedStatus = watch("status");

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="w-full max-w-xl rounded-2xl bg-white shadow-xl">
        <header className="border-b border-slate-200 px-6 py-5">
          <h2 className="text-xl font-semibold text-slate-900">
            Cambiar estado
          </h2>

          <p className="mt-1 text-sm text-slate-500">
            {request.petName} · Estado actual:{" "}
            {getVeterinaryRequestStatusLabel(request.status)}
          </p>
        </header>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5 p-6">
          <div>
            <label className="text-sm font-medium text-slate-700">
              Nuevo estado
            </label>

            <select
              {...register("status", {
                valueAsNumber: true,
              })}
              className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
            >
              {availableStatuses.map((status) => (
                <option key={status} value={status}>
                  {getVeterinaryRequestStatusLabel(status)}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-sm font-medium text-slate-700">
              Notas internas
            </label>

            <textarea
              rows={4}
              {...register("internalNotes")}
              placeholder="Notas para seguimiento interno..."
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
            />

            {errors.internalNotes && (
              <p className="mt-1 text-xs text-red-600">
                {errors.internalNotes.message}
              </p>
            )}
          </div>

          {selectedStatus === VeterinaryRequestStatus.Rejected && (
            <div>
              <label className="text-sm font-medium text-slate-700">
                Motivo de rechazo *
              </label>

              <textarea
                rows={3}
                {...register("rejectionReason")}
                className="mt-1 w-full rounded-lg border border-red-300 px-3 py-2.5 text-sm"
              />

              {errors.rejectionReason && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.rejectionReason.message}
                </p>
              )}
            </div>
          )}

          {selectedStatus === VeterinaryRequestStatus.Cancelled && (
            <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              Al cancelar la solicitud ya no podrá editarse ni convertirse en
              recepción.
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-slate-200 pt-5">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-700"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              {isSubmitting ? "Actualizando..." : "Actualizar estado"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
