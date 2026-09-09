import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { getVeterinarianClinicOptions } from "../api/veterinariansApi";
import {
  veterinarianSchema,
  type VeterinarianFormValues,
} from "../schemas/veterinarianSchema";
import type { Veterinarian } from "../types/veterinarian.types";

interface VeterinarianFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  veterinarian: Veterinarian | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: VeterinarianFormValues) => Promise<void>;
}

export function VeterinarianFormModal({
  isOpen,
  mode,
  veterinarian,
  isSubmitting,
  onClose,
  onSubmit,
}: VeterinarianFormModalProps) {
  const [clinicSearchInput, setClinicSearchInput] = useState("");
  const [debouncedClinicSearch, setDebouncedClinicSearch] = useState("");
  const [clinicPage, setClinicPage] = useState(1);

  const {
    register,
    reset,
    handleSubmit,
    formState: { errors },
  } = useForm<VeterinarianFormValues>({
    resolver: zodResolver(veterinarianSchema),

    defaultValues: {
      veterinaryClinicId: "",
      firstName: "",
      lastName: "",
      secondLastName: "",
      phone: "",
      email: "",
      professionalLicenseNumber: "",
    },
  });

  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const clinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setClinicPage(1);
      setDebouncedClinicSearch(clinicSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [clinicSearchInput]);

  const clinicOptionsQuery = useQuery({
    queryKey: [
      "veterinarian-clinic-options",
      "form",
      { search: normalizedClinicSearch, page: clinicPage },
    ],
    queryFn: () =>
      getVeterinarianClinicOptions({
        search: normalizedClinicSearch || undefined,
        isActive: true,
        page: clinicPage,
        pageSize: 20,
      }),
    enabled: isOpen,
  });

  const clinicOptions = useMemo(() => {
    const options = clinicOptionsQuery.data?.items ?? [];

    if (
      veterinarian?.veterinaryClinicId &&
      veterinarian.veterinaryClinicName &&
      !options.some((option) => option.id === veterinarian.veterinaryClinicId)
    ) {
      return [
        {
          id: veterinarian.veterinaryClinicId,
          displayName: veterinarian.veterinaryClinicName,
        },
        ...options,
      ];
    }

    return options;
  }, [clinicOptionsQuery.data?.items, veterinarian]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setClinicSearchInput("");
    setDebouncedClinicSearch("");
    setClinicPage(1);

    reset({
      veterinaryClinicId: veterinarian?.veterinaryClinicId ?? "",
      firstName: veterinarian?.firstName ?? "",
      lastName: veterinarian?.lastName ?? "",
      secondLastName: veterinarian?.secondLastName ?? "",
      phone: veterinarian?.phone ?? "",
      email: veterinarian?.email ?? "",
      professionalLicenseNumber: veterinarian?.professionalLicenseNumber ?? "",
    });
  }, [isOpen, reset, veterinarian]);

  if (!isOpen) {
    return null;
  }

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
        aria-labelledby="veterinarian-form-title"
        className="max-h-full w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Directorio veterinario
            </p>

            <h2
              id="veterinarian-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create"
                ? "Registrar veterinario"
                : "Editar veterinario"}
            </h2>
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
          className="space-y-5 px-6 py-6"
          noValidate
        >
          <div>
            <label
              htmlFor="veterinarian-clinic"
              className="block text-sm font-medium text-slate-700"
            >
              Veterinaria
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <select
              id="veterinarian-clinic"
              disabled={
                isSubmitting ||
                clinicOptionsQuery.isFetching ||
                clinicSearchPending
              }
              {...register("veterinaryClinicId")}
              className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            >
              <option value="">
                {clinicOptionsQuery.isLoading
                  ? "Cargando veterinarias..."
                  : "Sin veterinaria asignada"}
              </option>

              {clinicOptions.map((clinic) => (
                <option key={clinic.id} value={clinic.id}>
                  {clinic.displayName}
                  {clinic.isActive === false ? " — Inactiva" : ""}
                </option>
              ))}
            </select>

            <div className="mt-2 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <input
                type="search"
                value={clinicSearchInput}
                onChange={(event) => setClinicSearchInput(event.target.value)}
                placeholder="Buscar veterinaria por nombre"
                disabled={isSubmitting}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm sm:max-w-xs"
              />

              <div className="flex items-center gap-2 text-xs text-slate-500">
                <button
                  type="button"
                  onClick={() =>
                    setClinicPage((current) => Math.max(1, current - 1))
                  }
                  disabled={clinicPage <= 1 || clinicOptionsQuery.isFetching}
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Anterior
                </button>
                <span>
                  Página {clinicPage} de{" "}
                  {Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1)}
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setClinicPage((current) =>
                      Math.min(
                        current + 1,
                        Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1),
                      ),
                    )
                  }
                  disabled={
                    clinicOptionsQuery.isFetching ||
                    clinicPage >=
                      Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1)
                  }
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Siguiente
                </button>
              </div>
            </div>

            {errors.veterinaryClinicId && (
              <p className="mt-2 text-sm text-red-600">
                {errors.veterinaryClinicId.message}
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="veterinarian-first-name"
              className="block text-sm font-medium text-slate-700"
            >
              Nombre(s)
            </label>

            <input
              id="veterinarian-first-name"
              type="text"
              autoComplete="given-name"
              disabled={isSubmitting}
              {...register("firstName")}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.firstName && (
              <p className="mt-2 text-sm text-red-600">
                {errors.firstName.message}
              </p>
            )}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label
                htmlFor="veterinarian-last-name"
                className="block text-sm font-medium text-slate-700"
              >
                Apellido paterno
              </label>

              <input
                id="veterinarian-last-name"
                type="text"
                autoComplete="family-name"
                disabled={isSubmitting}
                {...register("lastName")}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.lastName && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.lastName.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="veterinarian-second-last-name"
                className="block text-sm font-medium text-slate-700"
              >
                Apellido materno
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <input
                id="veterinarian-second-last-name"
                type="text"
                autoComplete="additional-name"
                disabled={isSubmitting}
                {...register("secondLastName")}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.secondLastName && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.secondLastName.message}
                </p>
              )}
            </div>
          </div>

          <div>
            <label
              htmlFor="veterinarian-license"
              className="block text-sm font-medium text-slate-700"
            >
              Cédula profesional
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <input
              id="veterinarian-license"
              type="text"
              disabled={isSubmitting}
              {...register("professionalLicenseNumber")}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.professionalLicenseNumber && (
              <p className="mt-2 text-sm text-red-600">
                {errors.professionalLicenseNumber.message}
              </p>
            )}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label
                htmlFor="veterinarian-phone"
                className="block text-sm font-medium text-slate-700"
              >
                Teléfono
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <input
                id="veterinarian-phone"
                type="tel"
                autoComplete="tel"
                placeholder="81 1234 5678"
                disabled={isSubmitting}
                {...register("phone")}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.phone && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.phone.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="veterinarian-email"
                className="block text-sm font-medium text-slate-700"
              >
                Correo electrónico
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <input
                id="veterinarian-email"
                type="email"
                autoComplete="email"
                disabled={isSubmitting}
                {...register("email")}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.email && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.email.message}
                </p>
              )}
            </div>
          </div>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
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
              disabled={isSubmitting || clinicOptionsQuery.isLoading}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar veterinario"
                  : "Guardar cambios"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
