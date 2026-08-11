import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";

import {
  veterinaryRequestSchema,
  type VeterinaryRequestFormValues,
} from "../schemas/veterinaryRequestSchema";
import type { VeterinaryRequest } from "../types/veterinaryRequest.types";

export interface VeterinaryClinicOption {
  id: string;
  name: string;
}

export interface VeterinarianOption {
  id: string;
  veterinaryClinicId: string | null;
  firstName: string;
  lastName: string;
  secondLastName?: string | null;
}

interface VeterinaryRequestFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  request: VeterinaryRequest | null;

  clinics: VeterinaryClinicOption[];
  veterinarians: VeterinarianOption[];

  isLoadingOptions: boolean;
  isSubmitting: boolean;

  onClose: () => void;

  onCreate: (values: VeterinaryRequestFormValues) => void;

  onUpdate: (values: VeterinaryRequestFormValues) => void;
}

function buildVeterinarianName(veterinarian: VeterinarianOption): string {
  return [
    veterinarian.firstName,
    veterinarian.lastName,
    veterinarian.secondLastName,
  ]
    .filter(Boolean)
    .join(" ");
}

function getTodayLocalDate(): string {
  const now = new Date();
  const offset = now.getTimezoneOffset() * 60_000;

  return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}

function getDefaultValues(
  request: VeterinaryRequest | null,
): VeterinaryRequestFormValues {
  return {
    veterinaryClinicId: request?.veterinaryClinicId ?? "",

    referringVeterinarianId: request?.referringVeterinarianId ?? "",

    ownerFirstName: request?.ownerFirstName ?? "",

    ownerLastName: request?.ownerLastName ?? "",

    ownerSecondLastName: request?.ownerSecondLastName ?? "",

    ownerPhone: request?.ownerPhone ?? "",

    ownerEmail: request?.ownerEmail ?? "",

    petName: request?.petName ?? "",

    species: request?.species ?? "",

    breed: request?.breed ?? "",

    sex: request?.sex ?? "",

    color: request?.color ?? "",

    approximateWeightKg: request?.approximateWeightKg ?? 0,

    ageYears: request?.ageYears ?? null,

    dateOfDeath: request?.dateOfDeath.slice(0, 10) ?? "",

    requestedCremationType: request?.requestedCremationType
      ? (String(request.requestedCremationType) as "1" | "2")
      : "",

    requestedPackageName: request?.requestedPackageName ?? "",

    requestNotes: request?.requestNotes ?? "",
  };
}

export function VeterinaryRequestFormModal({
  isOpen,
  mode,
  request,
  clinics,
  veterinarians,
  isLoadingOptions,
  isSubmitting,
  onClose,
  onCreate,
  onUpdate,
}: VeterinaryRequestFormModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<VeterinaryRequestFormValues>({
    resolver: zodResolver(veterinaryRequestSchema),

    defaultValues: getDefaultValues(request),
  });

  const selectedClinicId = watch("veterinaryClinicId");

  const selectedVeterinarianId = watch("referringVeterinarianId");

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

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(getDefaultValues(request));
  }, [isOpen, request, mode, reset]);

  useEffect(() => {
    if (!selectedVeterinarianId) {
      return;
    }

    const veterinarian = veterinarians.find(
      (item) => item.id === selectedVeterinarianId,
    );

    if (!veterinarian) {
      setValue("referringVeterinarianId", "");
      return;
    }

    if (
      selectedClinicId &&
      veterinarian.veterinaryClinicId !== selectedClinicId
    ) {
      setValue("referringVeterinarianId", "", {
        shouldValidate: true,
      });

      return;
    }

    if (!selectedClinicId && veterinarian.veterinaryClinicId !== null) {
      setValue("referringVeterinarianId", "", {
        shouldValidate: true,
      });
    }
  }, [selectedClinicId, selectedVeterinarianId, setValue, veterinarians]);

  if (!isOpen) {
    return null;
  }

  const submitForm = handleSubmit((values) => {
    if (mode === "create") {
      onCreate(values);
      return;
    }

    onUpdate(values);
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[92vh] w-full max-w-5xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              {mode === "create"
                ? "Registrar solicitud veterinaria"
                : "Editar solicitud veterinaria"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Información del propietario, mascota y referencia veterinaria.
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

        <form onSubmit={submitForm} className="space-y-6 p-6">
          <section className="rounded-2xl border border-slate-200 p-5">
            <h3 className="font-semibold text-slate-900">
              Referencia veterinaria
            </h3>

            <p className="mt-1 text-sm text-slate-500">
              Selecciona una veterinaria, un veterinario independiente o ambos
              cuando pertenezcan a la misma clínica.
            </p>

            <div className="mt-5 grid gap-4 md:grid-cols-2">
              <div>
                <label
                  htmlFor="veterinaryClinicId"
                  className="text-sm font-medium text-slate-700"
                >
                  Veterinaria
                </label>

                <select
                  id="veterinaryClinicId"
                  {...register("veterinaryClinicId")}
                  disabled={isLoadingOptions}
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">Sin veterinaria</option>

                  {clinics.map((clinic) => (
                    <option key={clinic.id} value={clinic.id}>
                      {clinic.name}
                    </option>
                  ))}
                </select>

                {errors.veterinaryClinicId && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.veterinaryClinicId.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="referringVeterinarianId"
                  className="text-sm font-medium text-slate-700"
                >
                  Veterinario referente
                </label>

                <select
                  id="referringVeterinarianId"
                  {...register("referringVeterinarianId")}
                  disabled={isLoadingOptions}
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">Sin veterinario</option>

                  {availableVeterinarians.map((veterinarian) => (
                    <option key={veterinarian.id} value={veterinarian.id}>
                      {buildVeterinarianName(veterinarian)}
                    </option>
                  ))}
                </select>

                {errors.referringVeterinarianId && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.referringVeterinarianId.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 p-5">
            <h3 className="font-semibold text-slate-900">Propietario</h3>

            <div className="mt-5 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              <div>
                <label className="text-sm font-medium text-slate-700">
                  Nombre *
                </label>

                <input
                  {...register("ownerFirstName")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.ownerFirstName && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.ownerFirstName.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Apellido paterno *
                </label>

                <input
                  {...register("ownerLastName")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.ownerLastName && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.ownerLastName.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Apellido materno
                </label>

                <input
                  {...register("ownerSecondLastName")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Teléfono *
                </label>

                <input
                  {...register("ownerPhone")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.ownerPhone && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.ownerPhone.message}
                  </p>
                )}
              </div>

              <div className="md:col-span-2">
                <label className="text-sm font-medium text-slate-700">
                  Correo electrónico
                </label>

                <input
                  type="email"
                  {...register("ownerEmail")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.ownerEmail && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.ownerEmail.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 p-5">
            <h3 className="font-semibold text-slate-900">Mascota</h3>

            <div className="mt-5 grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <div>
                <label className="text-sm font-medium text-slate-700">
                  Nombre *
                </label>

                <input
                  {...register("petName")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.petName && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.petName.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Especie *
                </label>

                <input
                  {...register("species")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.species && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.species.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Raza *
                </label>

                <input
                  {...register("breed")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.breed && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.breed.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Sexo *
                </label>

                <select
                  {...register("sex")}
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">Seleccionar</option>
                  <option value="Macho">Macho</option>
                  <option value="Hembra">Hembra</option>
                </select>

                {errors.sex && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.sex.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Color *
                </label>

                <input
                  {...register("color")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.color && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.color.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Peso aproximado (kg) *
                </label>

                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  {...register("approximateWeightKg", {
                    valueAsNumber: true,
                  })}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.approximateWeightKg && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.approximateWeightKg.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Edad aproximada
                </label>

                <input
                  type="number"
                  min="0"
                  max="100"
                  {...register("ageYears", {
                    setValueAs: (value) =>
                      value === "" ? null : Number(value),
                  })}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.ageYears && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.ageYears.message}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Fecha de fallecimiento *
                </label>

                <input
                  type="date"
                  max={getTodayLocalDate()}
                  {...register("dateOfDeath")}
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />

                {errors.dateOfDeath && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.dateOfDeath.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 p-5">
            <h3 className="font-semibold text-slate-900">
              Servicio solicitado
            </h3>

            <div className="mt-5 grid gap-4 md:grid-cols-2">
              <div>
                <label className="text-sm font-medium text-slate-700">
                  Tipo de cremación
                </label>

                <select
                  {...register("requestedCremationType")}
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">No especificado</option>

                  <option value="1">Individual</option>

                  <option value="2">Comunitaria</option>
                </select>
              </div>

              <div>
                <label className="text-sm font-medium text-slate-700">
                  Paquete solicitado
                </label>

                <input
                  {...register("requestedPackageName")}
                  placeholder="Ej. Paquete Premium"
                  className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
                />
              </div>
            </div>

            <div className="mt-4">
              <label className="text-sm font-medium text-slate-700">
                Notas de la solicitud
              </label>

              <textarea
                rows={4}
                {...register("requestNotes")}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
              />
            </div>
          </section>

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
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar solicitud"
                  : "Guardar cambios"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
