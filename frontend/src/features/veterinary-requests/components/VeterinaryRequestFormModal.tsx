import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";

import {
  getVeterinaryRequestClinicOptions,
  getVeterinaryRequestVeterinarianOptions,
} from "../api/veterinaryRequestsApi";
import {
  getMexicoBusinessDate,
  veterinaryRequestSchema,
  type VeterinaryRequestFormValues,
} from "../schemas/veterinaryRequestSchema";
import type {
  VeterinaryRequest,
  VeterinaryRequestClinicOption,
  VeterinaryRequestVeterinarianOption,
} from "../types/veterinaryRequest.types";

interface VeterinaryRequestFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  request: VeterinaryRequest | null;

  isSubmitting: boolean;

  onClose: () => void;

  onCreate: (values: VeterinaryRequestFormValues) => void;

  onUpdate: (values: VeterinaryRequestFormValues) => void;
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
  isSubmitting,
  onClose,
  onCreate,
  onUpdate,
}: VeterinaryRequestFormModalProps) {
  const [clinicSearchInput, setClinicSearchInput] = useState("");
  const [debouncedClinicSearch, setDebouncedClinicSearch] = useState("");
  const [clinicPage, setClinicPage] = useState(1);
  const [veterinarianSearchInput, setVeterinarianSearchInput] = useState("");
  const [debouncedVeterinarianSearch, setDebouncedVeterinarianSearch] =
    useState("");
  const [veterinarianPage, setVeterinarianPage] = useState(1);
  const [referralNotice, setReferralNotice] = useState<string | null>(null);

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

  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const normalizedVeterinarianSearch = debouncedVeterinarianSearch.trim();
  const clinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;
  const veterinarianSearchPending =
    veterinarianSearchInput.trim() !== normalizedVeterinarianSearch;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setClinicPage(1);
      setDebouncedClinicSearch(clinicSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [clinicSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setVeterinarianPage(1);
      setDebouncedVeterinarianSearch(veterinarianSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [veterinarianSearchInput]);

  const clinicsQuery = useQuery({
    queryKey: [
      "veterinary-request-clinic-options",
      { search: normalizedClinicSearch, page: clinicPage },
    ],
    queryFn: () =>
      getVeterinaryRequestClinicOptions({
        search: normalizedClinicSearch || undefined,
        page: clinicPage,
        pageSize: 20,
      }),
    enabled: isOpen,
  });

  const veterinariansQuery = useQuery({
    queryKey: [
      "veterinary-request-veterinarian-options",
      {
        veterinaryClinicId: selectedClinicId,
        search: normalizedVeterinarianSearch,
        page: veterinarianPage,
      },
    ],
    queryFn: () =>
      getVeterinaryRequestVeterinarianOptions({
        veterinaryClinicId: selectedClinicId || undefined,
        search: normalizedVeterinarianSearch || undefined,
        page: veterinarianPage,
        pageSize: 20,
      }),
    enabled: isOpen,
  });

  const clinics = useMemo(() => {
    const options = clinicsQuery.data?.items ?? [];
    const current: VeterinaryRequestClinicOption | null =
      request?.veterinaryClinicId && request.veterinaryClinicName
        ? {
            id: request.veterinaryClinicId,
            displayName: request.veterinaryClinicName,
          }
        : null;

    return current && !options.some((option) => option.id === current.id)
      ? [current, ...options]
      : options;
  }, [clinicsQuery.data?.items, request]);

  const veterinarians = useMemo(() => {
    const options = veterinariansQuery.data?.items ?? [];
    const current: VeterinaryRequestVeterinarianOption | null =
      request?.referringVeterinarianId &&
      request.referringVeterinarianName &&
      selectedClinicId === (request.veterinaryClinicId ?? "")
        ? {
            id: request.referringVeterinarianId,
            displayName: request.referringVeterinarianName,
            veterinaryClinicId: request.veterinaryClinicId,
          }
        : null;

    return current && !options.some((option) => option.id === current.id)
      ? [current, ...options]
      : options;
  }, [request, selectedClinicId, veterinariansQuery.data?.items]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setClinicSearchInput("");
    setDebouncedClinicSearch("");
    setClinicPage(1);
    setVeterinarianSearchInput("");
    setDebouncedVeterinarianSearch("");
    setVeterinarianPage(1);
    setReferralNotice(null);
    reset(getDefaultValues(request));
  }, [isOpen, request, mode, reset]);

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
                  {...register("veterinaryClinicId", {
                    onChange: (event) => {
                      const nextClinicId = event.target.value as string;

                      setVeterinarianPage(1);
                      setVeterinarianSearchInput("");
                      setDebouncedVeterinarianSearch("");

                      if (
                        selectedVeterinarianId &&
                        nextClinicId !== selectedClinicId
                      ) {
                        setValue("referringVeterinarianId", "", {
                          shouldValidate: true,
                        });
                        setReferralNotice(
                          "Se quitó el veterinario porque cambiaste la veterinaria. Selecciona una referencia compatible.",
                        );
                      }
                    },
                  })}
                  disabled={clinicsQuery.isFetching || clinicSearchPending}
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">Sin veterinaria</option>

                  {clinics.map((clinic) => (
                    <option key={clinic.id} value={clinic.id}>
                      {clinic.displayName}
                    </option>
                  ))}
                </select>

                <input
                  type="search"
                  value={clinicSearchInput}
                  onChange={(event) => setClinicSearchInput(event.target.value)}
                  placeholder="Buscar veterinaria"
                  className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />

                <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                  <button
                    type="button"
                    onClick={() =>
                      setClinicPage((current) => Math.max(1, current - 1))
                    }
                    disabled={clinicPage <= 1 || clinicsQuery.isFetching}
                    className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                  >
                    Anterior
                  </button>
                  <span>
                    Página {clinicPage} de{" "}
                    {Math.max(clinicsQuery.data?.totalPages ?? 0, 1)}
                  </span>
                  <button
                    type="button"
                    onClick={() =>
                      setClinicPage((current) =>
                        Math.min(
                          current + 1,
                          Math.max(clinicsQuery.data?.totalPages ?? 0, 1),
                        ),
                      )
                    }
                    disabled={
                      clinicsQuery.isFetching ||
                      clinicPage >=
                        Math.max(clinicsQuery.data?.totalPages ?? 0, 1)
                    }
                    className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                  >
                    Siguiente
                  </button>
                </div>

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
                  {...register("referringVeterinarianId", {
                    onChange: () => setReferralNotice(null),
                  })}
                  disabled={
                    veterinariansQuery.isFetching || veterinarianSearchPending
                  }
                  className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
                >
                  <option value="">Sin veterinario</option>

                  {veterinarians.map((veterinarian) => (
                    <option key={veterinarian.id} value={veterinarian.id}>
                      {veterinarian.displayName}
                    </option>
                  ))}
                </select>

                <input
                  type="search"
                  value={veterinarianSearchInput}
                  onChange={(event) =>
                    setVeterinarianSearchInput(event.target.value)
                  }
                  placeholder="Buscar veterinario"
                  className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />

                <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                  <button
                    type="button"
                    onClick={() =>
                      setVeterinarianPage((current) => Math.max(1, current - 1))
                    }
                    disabled={
                      veterinarianPage <= 1 || veterinariansQuery.isFetching
                    }
                    className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                  >
                    Anterior
                  </button>
                  <span>
                    Página {veterinarianPage} de{" "}
                    {Math.max(veterinariansQuery.data?.totalPages ?? 0, 1)}
                  </span>
                  <button
                    type="button"
                    onClick={() =>
                      setVeterinarianPage((current) =>
                        Math.min(
                          current + 1,
                          Math.max(veterinariansQuery.data?.totalPages ?? 0, 1),
                        ),
                      )
                    }
                    disabled={
                      veterinariansQuery.isFetching ||
                      veterinarianPage >=
                        Math.max(veterinariansQuery.data?.totalPages ?? 0, 1)
                    }
                    className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                  >
                    Siguiente
                  </button>
                </div>

                {errors.referringVeterinarianId && (
                  <p className="mt-1 text-xs text-red-600">
                    {errors.referringVeterinarianId.message}
                  </p>
                )}
              </div>
            </div>

            {referralNotice && (
              <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
                {referralNotice}
              </p>
            )}
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
                  max={getMexicoBusinessDate()}
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
