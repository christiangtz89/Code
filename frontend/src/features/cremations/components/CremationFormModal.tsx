import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  cremationSchema,
  type CremationFormValues,
} from "../schemas/cremationSchema";
import { CremationType, type Cremation } from "../types/cremation.types";
import type {
  CremationReceptionOption,
  CremationUserOption,
} from "../types/cremationForm.types";
import {
  cremationToFormValues,
  defaultCremationFormValues,
} from "../utils/cremationForm";
import { getCremationTypeLabel } from "../utils/cremationLabels";

interface CremationFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  cremation: Cremation | null;

  receptions: CremationReceptionOption[];
  users: CremationUserOption[];

  isLoadingReceptions: boolean;
  isLoadingUsers: boolean;
  isSubmitting: boolean;

  onClose: () => void;

  onSubmit: (values: CremationFormValues) => Promise<void>;
}

export function CremationFormModal({
  isOpen,
  mode,
  cremation,
  receptions,
  users,
  isLoadingReceptions,
  isLoadingUsers,
  isSubmitting,
  onClose,
  onSubmit,
}: CremationFormModalProps) {
  const {
    register,
    reset,
    setValue,
    watch,
    handleSubmit,
    formState: { errors },
  } = useForm<CremationFormValues>({
    resolver: zodResolver(cremationSchema),
    defaultValues: defaultCremationFormValues,
  });

  const includesUrn = watch("includesUrn");

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (mode === "edit" && cremation) {
      reset(cremationToFormValues(cremation));

      return;
    }

    reset(defaultCremationFormValues);
  }, [cremation, isOpen, mode, reset]);

  useEffect(() => {
    if (!includesUrn) {
      setValue("urnDescription", "");
    }
  }, [includesUrn, setValue]);

  if (!isOpen) {
    return null;
  }

  const noAvailableReceptions =
    mode === "create" && !isLoadingReceptions && receptions.length === 0;

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="cremation-form-title"
        onMouseDown={(event) => event.stopPropagation()}
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">Operación</p>

            <h2
              id="cremation-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create" ? "Registrar cremación" : "Editar cremación"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {mode === "create"
                ? "Configura el servicio y su programación inicial."
                : "Actualiza los detalles del servicio de cremación."}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form
          onSubmit={handleSubmit(onSubmit)}
          noValidate
          className="space-y-7 px-6 py-6"
        >
          <section>
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Recepción
            </h3>

            {mode === "create" ? (
              <div className="mt-4">
                <label
                  htmlFor="cremation-reception"
                  className="block text-sm font-medium text-slate-700"
                >
                  Recepción
                </label>

                <select
                  id="cremation-reception"
                  disabled={isSubmitting || isLoadingReceptions}
                  {...register("receptionId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {isLoadingReceptions
                      ? "Cargando recepciones..."
                      : "Selecciona una recepción"}
                  </option>

                  {receptions.map((reception) => (
                    <option key={reception.id} value={reception.id}>
                      {reception.petName} — {reception.customerName} —{" "}
                      {reception.qrCode}
                    </option>
                  ))}
                </select>

                {errors.receptionId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.receptionId.message}
                  </p>
                )}

                {noAvailableReceptions && (
                  <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
                    No hay recepciones disponibles para registrar una cremación.
                  </p>
                )}
              </div>
            ) : (
              <>
                <input type="hidden" {...register("receptionId")} />

                <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
                  <div className="grid gap-4 sm:grid-cols-3">
                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Mascota
                      </p>

                      <p className="mt-1 font-medium text-slate-900">
                        {cremation?.petName}
                      </p>
                    </div>

                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Cliente
                      </p>

                      <p className="mt-1 text-sm text-slate-700">
                        {cremation?.customerName}
                      </p>
                    </div>

                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Código QR
                      </p>

                      <p className="mt-1 font-mono text-sm text-slate-700">
                        {cremation?.qrCode}
                      </p>
                    </div>
                  </div>

                  <p className="mt-3 text-xs text-slate-500">
                    La recepción no puede cambiarse después de registrar la
                    cremación.
                  </p>
                </div>
              </>
            )}
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Servicio
            </h3>

            <div className="mt-4 grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="cremation-type"
                  className="block text-sm font-medium text-slate-700"
                >
                  Tipo de cremación
                </label>

                <select
                  id="cremation-type"
                  disabled={isSubmitting}
                  {...register("cremationType", {
                    valueAsNumber: true,
                  })}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value={CremationType.Individual}>
                    {getCremationTypeLabel(CremationType.Individual)}
                  </option>

                  <option value={CremationType.Communal}>
                    {getCremationTypeLabel(CremationType.Communal)}
                  </option>
                </select>

                {errors.cremationType && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.cremationType.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="cremation-package"
                  className="block text-sm font-medium text-slate-700"
                >
                  Nombre del paquete
                </label>

                <input
                  id="cremation-package"
                  type="text"
                  disabled={isSubmitting}
                  placeholder="Ej. Despedida Especial"
                  {...register("packageName")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.packageName && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.packageName.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Incluye
            </h3>

            <div className="mt-4 grid gap-3 sm:grid-cols-3">
              <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="checkbox"
                  disabled={isSubmitting}
                  {...register("includesUrn")}
                  className="h-4 w-4 rounded border-slate-300"
                />

                <span className="text-sm font-medium text-slate-700">Urna</span>
              </label>

              <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="checkbox"
                  disabled={isSubmitting}
                  {...register("includesPawPrint")}
                  className="h-4 w-4 rounded border-slate-300"
                />

                <span className="text-sm font-medium text-slate-700">
                  Huella
                </span>
              </label>

              <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="checkbox"
                  disabled={isSubmitting}
                  {...register("includesCertificate")}
                  className="h-4 w-4 rounded border-slate-300"
                />

                <span className="text-sm font-medium text-slate-700">
                  Certificado
                </span>
              </label>
            </div>

            {includesUrn && (
              <div className="mt-5">
                <label
                  htmlFor="cremation-urn"
                  className="block text-sm font-medium text-slate-700"
                >
                  Descripción de la urna
                </label>

                <textarea
                  id="cremation-urn"
                  rows={3}
                  disabled={isSubmitting}
                  placeholder="Describe el modelo, material, color u otras características."
                  {...register("urnDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.urnDescription && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.urnDescription.message}
                  </p>
                )}
              </div>
            )}
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Programación
            </h3>

            <div className="mt-4 grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="cremation-scheduled-at"
                  className="block text-sm font-medium text-slate-700"
                >
                  Fecha programada
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <input
                  id="cremation-scheduled-at"
                  type="datetime-local"
                  disabled={isSubmitting}
                  {...register("scheduledAt")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.scheduledAt && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.scheduledAt.message}
                  </p>
                )}

                {mode === "create" && (
                  <p className="mt-2 text-xs text-slate-500">
                    Si agregas una fecha, la cremación se registrará
                    inicialmente como Programada. Sin fecha se registrará como
                    Pendiente.
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="cremation-user"
                  className="block text-sm font-medium text-slate-700"
                >
                  Asignado a
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <select
                  id="cremation-user"
                  disabled={isSubmitting || isLoadingUsers}
                  {...register("assignedToUserId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {isLoadingUsers ? "Cargando usuarios..." : "Sin asignar"}
                  </option>

                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.name}
                    </option>
                  ))}
                </select>

                {errors.assignedToUserId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.assignedToUserId.message}
                  </p>
                )}

                <p className="mt-2 text-xs text-slate-500">
                  Se requiere un usuario asignado antes de iniciar la cremación.
                </p>
              </div>
            </div>
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Información adicional
            </h3>

            <div className="mt-4 space-y-5">
              <div>
                <label
                  htmlFor="cremation-instructions"
                  className="block text-sm font-medium text-slate-700"
                >
                  Instrucciones especiales
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <textarea
                  id="cremation-instructions"
                  rows={3}
                  disabled={isSubmitting}
                  {...register("specialInstructions")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.specialInstructions && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.specialInstructions.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="cremation-notes"
                  className="block text-sm font-medium text-slate-700"
                >
                  Notas
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <textarea
                  id="cremation-notes"
                  rows={3}
                  disabled={isSubmitting}
                  {...register("notes")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.notes && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.notes.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-6 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={
                isSubmitting || isLoadingReceptions || noAvailableReceptions
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar cremación"
                  : "Guardar cambios"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
