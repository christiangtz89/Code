import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo } from "react";
import { useForm, useWatch } from "react-hook-form";

import { getVeterinarians } from "../../veterinarians/api/veterinariansApi";
import { getVeterinarianFullName } from "../../veterinarians/utils/veterinarianName";
import { getVeterinaryClinics } from "../../veterinary-clinics/api/veterinaryClinicsApi";

import {
  collectionUpdateSchema,
  type CollectionUpdateFormValues,
} from "../schemas/collectionUpdateSchema";
import type { Collection } from "../types/collection.types";

interface CollectionEditModalProps {
  isOpen: boolean;
  collection: Collection | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: CollectionUpdateFormValues) => Promise<void>;
}

function getDefaultValues(
  collection: Collection | null,
): CollectionUpdateFormValues {
  return {
    locationType: collection?.locationType === 2 ? "2" : "1",

    veterinaryClinicId: collection?.veterinaryClinicId ?? "",

    referringVeterinarianId: collection?.referringVeterinarianId ?? "",

    pickupAddress: collection?.pickupAddress ?? "",

    pickupContactName: collection?.pickupContactName ?? "",

    pickupContactPhone: collection?.pickupContactPhone ?? "",

    approximateWeightKg:
      collection?.approximateWeightKg !== null &&
      collection?.approximateWeightKg !== undefined
        ? String(collection.approximateWeightKg)
        : "",

    hasPersonalBelongings: collection?.hasPersonalBelongings ?? false,

    personalBelongingsDescription:
      collection?.personalBelongingsDescription ?? "",

    notes: collection?.notes ?? "",
  };
}

export function CollectionEditModal({
  isOpen,
  collection,
  isSubmitting,
  onClose,
  onSubmit,
}: CollectionEditModalProps) {
  const {
    register,
    reset,
    control,
    setValue,
    clearErrors,
    handleSubmit,
    formState: { errors },
  } = useForm<CollectionUpdateFormValues>({
    resolver: zodResolver(collectionUpdateSchema),
    defaultValues: getDefaultValues(null),
  });

  const locationType = useWatch({
    control,
    name: "locationType",
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

  const clinicsQuery = useQuery({
    queryKey: ["veterinary-clinics", "collection-edit-options", true],

    queryFn: () =>
      getVeterinaryClinics({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const veterinariansQuery = useQuery({
    queryKey: ["veterinarians", "collection-edit-options", true],

    queryFn: () =>
      getVeterinarians({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const clinics = useMemo(
    () =>
      [...(clinicsQuery.data?.items ?? [])].sort((first, second) =>
        first.name.localeCompare(second.name, "es-MX"),
      ),
    [clinicsQuery.data?.items],
  );

  const veterinarians = useMemo(
    () =>
      [...(veterinariansQuery.data?.items ?? [])].sort((first, second) =>
        getVeterinarianFullName(first).localeCompare(
          getVeterinarianFullName(second),
          "es-MX",
        ),
      ),
    [veterinariansQuery.data?.items],
  );

  const availableVeterinarians = useMemo(() => {
    if (selectedClinicId) {
      return veterinarians.filter(
        (veterinarian) => veterinarian.veterinaryClinicId === selectedClinicId,
      );
    }

    return veterinarians.filter(
      (veterinarian) => veterinarian.veterinaryClinicId === null,
    );
  }, [selectedClinicId, veterinarians]);

  const currentClinicMissing =
    collection?.veterinaryClinicId !== null &&
    collection?.veterinaryClinicId !== undefined &&
    !clinics.some((clinic) => clinic.id === collection.veterinaryClinicId);

  const currentVeterinarianMissing =
    collection?.referringVeterinarianId !== null &&
    collection?.referringVeterinarianId !== undefined &&
    !availableVeterinarians.some(
      (veterinarian) => veterinarian.id === collection.referringVeterinarianId,
    );

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(getDefaultValues(collection));
  }, [isOpen, collection, reset]);

  if (!isOpen || !collection) {
    return null;
  }

  const belongingsField = register("hasPersonalBelongings");

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  function handleLocationChange(value: "1" | "2") {
    setValue("locationType", value, {
      shouldValidate: true,
      shouldDirty: true,
    });

    if (value === "1") {
      setValue("veterinaryClinicId", "", {
        shouldValidate: true,
        shouldDirty: true,
      });

      setValue("referringVeterinarianId", "", {
        shouldValidate: true,
        shouldDirty: true,
      });
    }
  }

  function handleClinicChange(clinicId: string) {
    setValue("veterinaryClinicId", clinicId, {
      shouldValidate: true,
      shouldDirty: true,
    });

    setValue("referringVeterinarianId", "", {
      shouldValidate: true,
      shouldDirty: true,
    });

    if (!clinicId) {
      return;
    }

    const clinic = clinics.find((item) => item.id === clinicId) ?? null;

    if (!clinic) {
      return;
    }

    if (clinic.address) {
      setValue("pickupAddress", clinic.address, {
        shouldDirty: true,
      });
    }

    if (clinic.primaryContactName) {
      setValue("pickupContactName", clinic.primaryContactName, {
        shouldDirty: true,
      });
    }

    if (clinic.phone) {
      setValue("pickupContactPhone", clinic.phone, {
        shouldDirty: true,
      });
    }
  }

  function handleVeterinarianChange(veterinarianId: string) {
    setValue("referringVeterinarianId", veterinarianId, {
      shouldValidate: true,
      shouldDirty: true,
    });

    if (selectedClinicId || !veterinarianId) {
      return;
    }

    const veterinarian =
      veterinarians.find((item) => item.id === veterinarianId) ?? null;

    if (!veterinarian) {
      return;
    }

    setValue(
      "pickupContactName",
      `Dr. ${getVeterinarianFullName(veterinarian)}`,
      {
        shouldDirty: true,
      },
    );

    if (veterinarian.phone) {
      setValue("pickupContactPhone", veterinarian.phone, {
        shouldDirty: true,
      });
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
        aria-labelledby="collection-edit-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Recolección y cadena de custodia
            </p>

            <h2
              id="collection-edit-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Editar recolección
            </h2>

            <p className="mt-2 text-sm text-slate-600">
              {collection.petName} — {collection.customerName}
            </p>

            <p className="mt-2 font-mono text-xs text-slate-500">
              {collection.qrCode}
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
          className="space-y-7 px-6 py-6"
          noValidate
        >
          <div className="rounded-xl bg-slate-50 px-4 py-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Mascota
            </p>

            <p className="mt-1 font-semibold text-slate-900">
              {collection.petName}
            </p>

            <p className="mt-1 text-sm text-slate-600">
              {collection.petSpecies} · {collection.customerName}
            </p>

            <p className="mt-1 text-sm text-slate-500">
              Cliente: {collection.customerPhone}
            </p>

            <p className="mt-3 text-xs text-slate-500">
              El cliente y la mascota no pueden cambiarse desde una recolección
              existente.
            </p>
          </div>

          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Lugar de recolección
            </legend>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="1"
                  checked={locationType === "1"}
                  disabled={isSubmitting}
                  onChange={() => handleLocationChange("1")}
                  className="mt-1"
                />

                <span>
                  <span className="block font-semibold text-slate-800">
                    Domicilio del cliente
                  </span>

                  <span className="mt-1 block text-sm text-slate-500">
                    Recolección directa con el propietario.
                  </span>
                </span>
              </label>

              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="2"
                  checked={locationType === "2"}
                  disabled={isSubmitting}
                  onChange={() => handleLocationChange("2")}
                  className="mt-1"
                />

                <span>
                  <span className="block font-semibold text-slate-800">
                    Veterinaria
                  </span>

                  <span className="mt-1 block text-sm text-slate-500">
                    Clínica veterinaria o veterinario independiente.
                  </span>
                </span>
              </label>
            </div>

            {locationType === "2" && (
              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Veterinaria
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <select
                    value={selectedClinicId}
                    onChange={(event) => handleClinicChange(event.target.value)}
                    disabled={isSubmitting || clinicsQuery.isLoading}
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900"
                  >
                    <option value="">Sin veterinaria</option>

                    {currentClinicMissing && collection.veterinaryClinicId && (
                      <option value={collection.veterinaryClinicId}>
                        {collection.veterinaryClinicName ??
                          "Veterinaria actual"}
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
                  <label className="block text-sm font-medium text-slate-700">
                    Veterinario
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <select
                    value={selectedVeterinarianId}
                    onChange={(event) =>
                      handleVeterinarianChange(event.target.value)
                    }
                    disabled={isSubmitting || veterinariansQuery.isLoading}
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900"
                  >
                    <option value="">
                      {selectedClinicId
                        ? "Sin veterinario referente"
                        : "Selecciona veterinario independiente"}
                    </option>

                    {currentVeterinarianMissing &&
                      collection.referringVeterinarianId && (
                        <option value={collection.referringVeterinarianId}>
                          Dr.{" "}
                          {collection.referringVeterinarianName ??
                            "Veterinario actual"}
                        </option>
                      )}

                    {availableVeterinarians.map((veterinarian) => (
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
                </div>
              </div>
            )}

            <div className="mt-5">
              <label className="block text-sm font-medium text-slate-700">
                Dirección de recolección
              </label>

              <textarea
                rows={3}
                maxLength={500}
                disabled={isSubmitting}
                {...register("pickupAddress")}
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900"
              />

              {errors.pickupAddress && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.pickupAddress.message}
                </p>
              )}
            </div>

            <div className="mt-5 grid gap-5 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-slate-700">
                  Contacto en recolección
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <input
                  disabled={isSubmitting}
                  {...register("pickupContactName")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                />

                {errors.pickupContactName && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.pickupContactName.message}
                  </p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700">
                  Teléfono del contacto
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <input
                  disabled={isSubmitting}
                  {...register("pickupContactPhone")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                />

                {errors.pickupContactPhone && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.pickupContactPhone.message}
                  </p>
                )}
              </div>
            </div>
          </fieldset>

          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Cadena de custodia
            </legend>

            <div>
              <label className="block text-sm font-medium text-slate-700">
                Peso aproximado
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <div className="relative mt-2">
                <input
                  type="number"
                  min="0.01"
                  max="999.99"
                  step="0.01"
                  disabled={isSubmitting}
                  {...register("approximateWeightKg")}
                  className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12"
                />

                <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-500">
                  kg
                </span>
              </div>

              {errors.approximateWeightKg && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.approximateWeightKg.message}
                </p>
              )}
            </div>

            <label className="mt-5 flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);

                  if (!event.target.checked) {
                    clearErrors("personalBelongingsDescription");

                    setValue("personalBelongingsDescription", "", {
                      shouldDirty: true,
                    });
                  }
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300"
              />

              <span>
                <span className="block text-sm font-medium text-slate-800">
                  Se recogieron objetos personales
                </span>

                <span className="mt-1 block text-sm text-slate-500">
                  Collar, placa, manta, transportadora u otros artículos.
                </span>
              </span>
            </label>

            {hasPersonalBelongings && (
              <div className="mt-4">
                <label className="block text-sm font-medium text-slate-700">
                  Descripción de los objetos
                </label>

                <textarea
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5"
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
            <label className="block text-sm font-medium text-slate-700">
              Notas
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <textarea
              rows={4}
              maxLength={1000}
              disabled={isSubmitting}
              {...register("notes")}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5"
            />

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
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={
                isSubmitting ||
                clinicsQuery.isLoading ||
                veterinariansQuery.isLoading
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Guardando..." : "Guardar cambios"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
