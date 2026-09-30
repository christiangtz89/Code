import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo } from "react";
import { useForm, useWatch } from "react-hook-form";
import { getPets } from "../../pets/api/petsApi";
import {
  getVeterinarians,
  getVeterinariansByClinic,
} from "../../veterinarians/api/veterinariansApi";
import { getVeterinarianFullName } from "../../veterinarians/utils/veterinarianName";
import { getVeterinaryClinics } from "../../veterinary-clinics/api/veterinaryClinicsApi";
import {
  receptionSchema,
  type ReceptionFormValues,
} from "../schemas/receptionSchema";
import type { Reception } from "../types/reception.types";

interface ReceptionFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  reception: Reception | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: ReceptionFormValues) => Promise<void>;
}

export function ReceptionFormModal({
  isOpen,
  mode,
  reception,
  isSubmitting,
  onClose,
  onSubmit,
}: ReceptionFormModalProps) {
  const {
    register,
    reset,
    control,
    setValue,
    handleSubmit,
    formState: { errors },
  } = useForm<ReceptionFormValues>({
    resolver: zodResolver(receptionSchema),

    defaultValues: {
      petId: "",
      veterinaryClinicId: "",
      referringVeterinarianId: "",
      verifiedWeightKg: "",
      hasPersonalBelongings: false,
      personalBelongingsDescription: "",
      referralNotes: "",
      notes: "",
    },
  });

  const selectedClinicId = useWatch({
    control,
    name: "veterinaryClinicId",
  });

  const selectedVeterinarianId = useWatch({
    control,
    name: "referringVeterinarianId",
  });

  const hasPersonalBelongings = useWatch({
    control,
    name: "hasPersonalBelongings",
  });

  const petsQuery = useQuery({
    queryKey: ["pets", "reception-options", true],

    queryFn: () =>
      getPets({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const clinicsQuery = useQuery({
    queryKey: ["veterinary-clinics", "reception-options", true],

    queryFn: () =>
      getVeterinaryClinics({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const veterinariansQuery = useQuery({
    queryKey: [
      "veterinarians",
      "reception-options",
      selectedClinicId || "independent",
    ],

    queryFn: async () => {
      if (selectedClinicId) {
        return getVeterinariansByClinic(selectedClinicId, true);
      }

      const result = await getVeterinarians({
        page: 1,
        pageSize: 100,
        isActive: true,
      });

      return result.items.filter(
        (veterinarian) => veterinarian.veterinaryClinicId === null,
      );
    },

    enabled: isOpen,
  });

  const pets = useMemo(
    () =>
      [...(petsQuery.data?.items ?? [])].sort((first, second) => {
        const customerComparison = first.customerName.localeCompare(
          second.customerName,
          "es-MX",
        );

        if (customerComparison !== 0) {
          return customerComparison;
        }

        return first.name.localeCompare(second.name, "es-MX");
      }),
    [petsQuery.data?.items],
  );

  const clinics = useMemo(
    () =>
      [...(clinicsQuery.data?.items ?? [])].sort((first, second) =>
        first.name.localeCompare(second.name, "es-MX"),
      ),
    [clinicsQuery.data?.items],
  );

  const veterinarians = useMemo(
    () =>
      [...(veterinariansQuery.data ?? [])].sort((first, second) =>
        getVeterinarianFullName(first).localeCompare(
          getVeterinarianFullName(second),
          "es-MX",
        ),
      ),
    [veterinariansQuery.data],
  );

  const petIsMissingFromOptions =
    reception !== null && !pets.some((pet) => pet.id === reception.petId);

  const clinicIsMissingFromOptions =
    reception?.veterinaryClinicId !== null &&
    reception?.veterinaryClinicId !== undefined &&
    !clinics.some((clinic) => clinic.id === reception.veterinaryClinicId);

  const veterinarianIsMissingFromOptions =
    reception?.referringVeterinarianId !== null &&
    reception?.referringVeterinarianId !== undefined &&
    !veterinarians.some(
      (veterinarian) => veterinarian.id === reception.referringVeterinarianId,
    );

  const isOriginalClinicContext =
    reception !== null &&
    selectedClinicId === (reception.veterinaryClinicId ?? "");

  const shouldShowHistoricalVeterinarian =
    veterinarianIsMissingFromOptions &&
    isOriginalClinicContext &&
    selectedVeterinarianId === reception?.referringVeterinarianId;

  const isCollectionOrigin =
    mode === "edit" && reception?.isCollectionOrigin === true;
  const isNormalEditLocked =
    mode === "edit" && reception?.isNormalEditLocked === true;
  const isReferralSourceLocked =
    mode === "edit" &&
    (reception?.isVeterinaryRequestOrigin === true ||
      reception?.isCollectionOrigin === true);
  const hasReferralSource =
    selectedClinicId.length > 0 || selectedVeterinarianId.length > 0;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      petId: reception?.petId ?? "",
      veterinaryClinicId: reception?.veterinaryClinicId ?? "",
      referringVeterinarianId: reception?.referringVeterinarianId ?? "",
      verifiedWeightKg:
        reception !== null ? String(reception.verifiedWeightKg) : "",
      hasPersonalBelongings: reception?.hasPersonalBelongings ?? false,
      personalBelongingsDescription:
        reception?.personalBelongingsDescription ?? "",
      referralNotes: reception?.referralNotes ?? "",
      notes: reception?.notes ?? "",
    });
  }, [isOpen, reception, reset]);

  if (!isOpen) {
    return null;
  }

  const clinicField = register("veterinaryClinicId");
  const belongingsField = register("hasPersonalBelongings");

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
        aria-labelledby="reception-form-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Ingreso y cadena de custodia
            </p>

            <h2
              id="reception-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create"
                ? "Registrar recepción"
                : isNormalEditLocked
                  ? "Consultar recepción"
                  : "Editar recepción"}
            </h2>

            {mode === "edit" && reception && (
              <p className="mt-2 font-mono text-xs text-slate-500">
                {reception.qrCode}
              </p>
            )}
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
          className="space-y-6 px-6 py-6"
          noValidate
        >
          {isNormalEditLocked && (
            <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
              <p className="font-medium text-amber-950">
                Esta recepción ya forma parte del proceso de cremación.
              </p>
              <p className="mt-1 text-sm text-amber-800">
                Los cambios operativos deben registrarse mediante una enmienda.
              </p>
            </div>
          )}

          <div>
            <label
              htmlFor="reception-pet"
              className="block text-sm font-medium text-slate-700"
            >
              Mascota
            </label>

            <select
              id="reception-pet"
              disabled={isSubmitting || petsQuery.isLoading || mode === "edit"}
              {...register("petId")}
              className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            >
              <option value="">
                {petsQuery.isLoading
                  ? "Cargando mascotas..."
                  : "Selecciona una mascota"}
              </option>

              {petIsMissingFromOptions && reception && (
                <option value={reception.petId}>
                  {reception.petName} — {reception.customerName}
                </option>
              )}

              {pets.map((pet) => (
                <option key={pet.id} value={pet.id}>
                  {pet.name} — {pet.customerName} · {pet.species}
                </option>
              ))}
            </select>

            {mode === "edit" && (
              <p className="mt-2 text-xs text-slate-500">
                La mascota no puede cambiarse después de registrar la recepción.
              </p>
            )}

            {errors.petId && (
              <p className="mt-2 text-sm text-red-600">
                {errors.petId.message}
              </p>
            )}

            {petsQuery.isError && (
              <p className="mt-2 text-sm text-red-600">
                No fue posible cargar las mascotas activas.
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="reception-weight"
              className="block text-sm font-medium text-slate-700"
            >
              {mode === "edit" && reception?.hasCremation
                ? "Peso operativo"
                : "Peso verificado"}
            </label>

            <div className="relative mt-2">
              <input
                id="reception-weight"
                type="number"
                min="0.01"
                max="999.99"
                step="0.01"
                inputMode="decimal"
                disabled={
                  isSubmitting || isCollectionOrigin || isNormalEditLocked
                }
                {...register("verifiedWeightKg")}
                className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-500">
                kg
              </span>
            </div>

            {errors.verifiedWeightKg && (
              <p className="mt-2 text-sm text-red-600">
                {errors.verifiedWeightKg.message}
              </p>
            )}

            {isCollectionOrigin && (
              <p className="mt-2 text-xs text-slate-500">
                El peso verificado fue finalizado desde la recolección y no
                puede modificarse desde la edición normal.
              </p>
            )}

            {mode === "edit" &&
              reception?.latestReportedCorrectedWeightKg !== null &&
              reception?.latestReportedCorrectedWeightKg !== undefined &&
              reception.latestReportedCorrectedWeightKg !==
                reception.verifiedWeightKg && (
                <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2">
                  <p className="text-xs font-medium text-amber-800">
                    Peso corregido reportado
                  </p>
                  <p className="mt-1 font-semibold text-amber-950">
                    {reception.latestReportedCorrectedWeightKg.toFixed(2)} kg
                  </p>
                  <p className="mt-1 text-xs text-amber-700">
                    El peso operativo permanece en el campo superior.
                  </p>
                </div>
              )}
          </div>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Referencia veterinaria
            </legend>

            <p className="mb-4 text-sm text-slate-500">
              Deja estos campos vacíos cuando la recepción sea directa.
            </p>

            <div className="grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="reception-clinic"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinaria
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <select
                  id="reception-clinic"
                  disabled={
                    isSubmitting ||
                    clinicsQuery.isLoading ||
                    isNormalEditLocked ||
                    isReferralSourceLocked
                  }
                  {...clinicField}
                  onChange={(event) => {
                    clinicField.onChange(event);

                    setValue("referringVeterinarianId", "", {
                      shouldValidate: true,
                    });
                  }}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {clinicsQuery.isLoading
                      ? "Cargando veterinarias..."
                      : "Sin veterinaria / independiente"}
                  </option>

                  {clinicIsMissingFromOptions &&
                    reception?.veterinaryClinicId && (
                      <option value={reception.veterinaryClinicId}>
                        {reception.veterinaryClinicName ?? "Veterinaria actual"}
                      </option>
                    )}

                  {clinics.map((clinic) => (
                    <option key={clinic.id} value={clinic.id}>
                      {clinic.name}
                    </option>
                  ))}
                </select>

                {errors.veterinaryClinicId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.veterinaryClinicId.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="reception-veterinarian"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinario referente
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <select
                  id="reception-veterinarian"
                  disabled={
                    isSubmitting ||
                    isNormalEditLocked ||
                    isReferralSourceLocked ||
                    veterinariansQuery.isLoading ||
                    veterinariansQuery.isError
                  }
                  {...register("referringVeterinarianId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {veterinariansQuery.isLoading
                      ? "Cargando veterinarios..."
                      : selectedClinicId.length > 0
                        ? "Sin veterinario referente"
                        : "Sin veterinario / recepción directa"}
                  </option>

                  {shouldShowHistoricalVeterinarian &&
                    reception?.referringVeterinarianId && (
                      <option value={reception.referringVeterinarianId}>
                        Dr.{" "}
                        {reception.referringVeterinarianName ??
                          "Veterinario actual"}
                      </option>
                    )}

                  {veterinarians.map((veterinarian) => (
                    <option key={veterinarian.id} value={veterinarian.id}>
                      Dr. {getVeterinarianFullName(veterinarian)}
                    </option>
                  ))}
                </select>

                {errors.referringVeterinarianId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.referringVeterinarianId.message}
                  </p>
                )}

                {veterinariansQuery.isError && (
                  <div className="mt-2 text-sm text-red-600">
                    <p>No fue posible cargar los veterinarios activos.</p>
                    <button
                      type="button"
                      onClick={() => void veterinariansQuery.refetch()}
                      className="mt-1 font-medium underline underline-offset-2"
                    >
                      Intentar nuevamente
                    </button>
                  </div>
                )}
              </div>
            </div>

            {isReferralSourceLocked && (
              <p className="mt-3 text-xs text-slate-500">
                La veterinaria y el veterinario provienen del registro de origen
                y no pueden modificarse desde la edición normal.
              </p>
            )}

            <div className="mt-5">
              <label
                htmlFor="reception-referral-notes"
                className="block text-sm font-medium text-slate-700"
              >
                Notas de referencia
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <textarea
                id="reception-referral-notes"
                rows={3}
                maxLength={1000}
                disabled={
                  isSubmitting || isNormalEditLocked || !hasReferralSource
                }
                {...register("referralNotes")}
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.referralNotes && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.referralNotes.message}
                </p>
              )}
            </div>
          </fieldset>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Objetos personales
            </legend>

            <label className="flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting || isNormalEditLocked}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);

                  if (!event.target.checked) {
                    setValue("personalBelongingsDescription", "", {
                      shouldValidate: true,
                    });
                  }
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-500"
              />

              <span>
                <span className="block text-sm font-medium text-slate-800">
                  Se recibieron objetos personales
                </span>

                <span className="mt-1 block text-sm text-slate-500">
                  Collar, placa, manta, transportadora u otros artículos.
                </span>
              </span>
            </label>

            {hasPersonalBelongings && (
              <div className="mt-4">
                <label
                  htmlFor="reception-belongings"
                  className="block text-sm font-medium text-slate-700"
                >
                  Descripción de los objetos
                </label>

                <textarea
                  id="reception-belongings"
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting || isNormalEditLocked}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.personalBelongingsDescription && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.personalBelongingsDescription.message}
                  </p>
                )}
              </div>
            )}
          </fieldset>

          <div>
            <label
              htmlFor="reception-notes"
              className="block text-sm font-medium text-slate-700"
            >
              Notas internas
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            {isNormalEditLocked ? (
              <div
                id="reception-notes"
                className="mt-2 min-h-24 whitespace-pre-wrap rounded-lg border border-slate-200 bg-slate-50 px-3 py-2.5 text-sm text-slate-700"
              >
                {reception?.notes?.trim() || "Sin notas originales."}
              </div>
            ) : (
              <textarea
                id="reception-notes"
                rows={4}
                maxLength={1000}
                disabled={isSubmitting}
                {...register("notes")}
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />
            )}

            {isNormalEditLocked && (
              <p className="mt-2 text-xs text-slate-500">
                Las notas originales se conservan. Usa “Agregar aclaración” para
                registrar información adicional.
              </p>
            )}

            {errors.notes && (
              <p className="mt-2 text-sm text-red-600">
                {errors.notes.message}
              </p>
            )}
          </div>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              {isNormalEditLocked ? "Cerrar" : "Cancelar"}
            </button>

            {!isNormalEditLocked && (
              <button
                type="submit"
                disabled={
                  isSubmitting || petsQuery.isLoading || clinicsQuery.isLoading
                }
                className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting
                  ? "Guardando..."
                  : mode === "create"
                    ? "Registrar recepción"
                    : "Guardar cambios"}
              </button>
            )}
          </footer>
        </form>
      </section>
    </div>
  );
}
