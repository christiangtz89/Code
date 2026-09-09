import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import {
  createVeterinarian,
  deactivateVeterinarian,
  getVeterinarianClinicOptions,
  getVeterinarians,
  restoreVeterinarian,
  searchVeterinarians,
  updateVeterinarian,
} from "../api/veterinariansApi";
import { VeterinarianFormModal } from "../components/VeterinarianFormModal";
import { VeterinariansTable } from "../components/VeterinariansTable";
import type { VeterinarianFormValues } from "../schemas/veterinarianSchema";
import type {
  PaginatedVeterinarians,
  Veterinarian,
  VeterinarianClinicOption,
  VeterinarianPayload,
} from "../types/veterinarian.types";
import { getVeterinarianFullName } from "../utils/veterinarianName";

type VeterinarianStatusFilter = "active" | "inactive";

interface VeterinarianModalState {
  mode: "create" | "edit";
  veterinarian: Veterinarian | null;
}

interface ApiErrorResponse {
  title?: string;
  detail?: string;
  message?: string;
}

function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  if (!error.response) {
    return "No fue posible conectarse con el servidor.";
  }

  const data = error.response.data as ApiErrorResponse | string | undefined;

  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object") {
    return data.detail ?? data.message ?? data.title ?? fallback;
  }

  return fallback;
}

function normalizeOptional(value: string): string | null {
  const normalized = value.trim();

  return normalized.length > 0 ? normalized : null;
}

function createPayload(values: VeterinarianFormValues): VeterinarianPayload {
  return {
    veterinaryClinicId: normalizeOptional(values.veterinaryClinicId),
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    secondLastName: normalizeOptional(values.secondLastName),
    phone: normalizeOptional(values.phone),
    email: normalizeOptional(values.email),
    professionalLicenseNumber: normalizeOptional(
      values.professionalLicenseNumber,
    ),
  };
}

export function VeterinariansPage() {
  const queryClient = useQueryClient();

  const [statusFilter, setStatusFilter] =
    useState<VeterinarianStatusFilter>("active");

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [clinicFilter, setClinicFilter] = useState("");
  const [selectedClinicFilter, setSelectedClinicFilter] =
    useState<VeterinarianClinicOption | null>(null);
  const [clinicSearchInput, setClinicSearchInput] = useState("");
  const [debouncedClinicSearch, setDebouncedClinicSearch] = useState("");
  const [clinicLookupPage, setClinicLookupPage] = useState(1);

  const [modalState, setModalState] = useState<VeterinarianModalState | null>(
    null,
  );

  const isActive = statusFilter === "active";

  const normalizedSearch = debouncedSearch.trim();
  const searchPending = searchInput.trim() !== normalizedSearch;
  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const clinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;
  const canManageVeterinarians = hasPermission("Veterinarians.Manage");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setPage(1);
      setDebouncedSearch(searchInput);
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setClinicLookupPage(1);
      setDebouncedClinicSearch(clinicSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [clinicSearchInput]);

  const clinicOptionsQuery = useQuery({
    queryKey: [
      "veterinarian-clinic-options",
      { search: normalizedClinicSearch, page: clinicLookupPage },
    ],
    queryFn: () =>
      getVeterinarianClinicOptions({
        search: normalizedClinicSearch || undefined,
        page: clinicLookupPage,
        pageSize: 20,
      }),
  });

  const clinicOptions = useMemo(() => {
    const options = clinicOptionsQuery.data?.items ?? [];

    if (
      selectedClinicFilter &&
      !options.some((option) => option.id === selectedClinicFilter.id)
    ) {
      return [selectedClinicFilter, ...options];
    }

    return options;
  }, [clinicOptionsQuery.data?.items, selectedClinicFilter]);

  const veterinariansQuery = useQuery({
    queryKey: [
      "veterinarians",
      {
        page,
        pageSize,
        isActive,
        search: normalizedSearch,
        clinicId: clinicFilter,
      },
    ],

    queryFn: async (): Promise<PaginatedVeterinarians> => {
      if (normalizedSearch) {
        return searchVeterinarians({
          search: normalizedSearch,
          isActive,
          veterinaryClinicId: clinicFilter || undefined,
          page,
          pageSize,
        });
      }

      return getVeterinarians({
        page,
        pageSize,
        isActive,
        veterinaryClinicId: clinicFilter || undefined,
      });
    },
  });

  async function refreshVeterinarians() {
    await queryClient.invalidateQueries({
      queryKey: ["veterinarians"],
    });

    await queryClient.invalidateQueries({
      queryKey: ["clinic-veterinarians"],
    });
  }

  const createMutation = useMutation({
    mutationFn: createVeterinarian,

    onSuccess: async () => {
      await refreshVeterinarians();

      toast.success("Veterinario registrado correctamente.");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: VeterinarianPayload;
    }) => updateVeterinarian(id, payload),

    onSuccess: async () => {
      await refreshVeterinarians();

      toast.success("Veterinario actualizado correctamente.");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateVeterinarian,

    onSuccess: async () => {
      await refreshVeterinarians();

      toast.success("Veterinario desactivado correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible desactivar el veterinario."),
      );
    },
  });

  const restoreMutation = useMutation({
    mutationFn: restoreVeterinarian,

    onSuccess: async () => {
      await refreshVeterinarians();

      toast.success("Veterinario restaurado correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible restaurar el veterinario. Verifica que su veterinaria esté activa.",
        ),
      );
    },
  });

  const pendingVeterinarianId = useMemo(() => {
    if (deactivateMutation.isPending) {
      return deactivateMutation.variables;
    }

    if (restoreMutation.isPending) {
      return restoreMutation.variables;
    }

    return null;
  }, [
    deactivateMutation.isPending,
    deactivateMutation.variables,
    restoreMutation.isPending,
    restoreMutation.variables,
  ]);

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  async function handleFormSubmit(values: VeterinarianFormValues) {
    const payload = createPayload(values);

    try {
      if (modalState?.mode === "edit" && modalState.veterinarian) {
        await updateMutation.mutateAsync({
          id: modalState.veterinarian.id,
          payload,
        });
      } else {
        await createMutation.mutateAsync(payload);
      }

      setModalState(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalState?.mode === "edit"
            ? "No fue posible actualizar el veterinario."
            : "No fue posible registrar el veterinario.",
        ),
      );
    }
  }

  function handleDeactivate(veterinarian: Veterinarian) {
    const confirmed = window.confirm(
      `¿Deseas desactivar al Dr. ${getVeterinarianFullName(veterinarian)}?`,
    );

    if (confirmed) {
      deactivateMutation.mutate(veterinarian.id);
    }
  }

  function handleRestore(veterinarian: Veterinarian) {
    const confirmed = window.confirm(
      `¿Deseas restaurar al Dr. ${getVeterinarianFullName(veterinarian)}?`,
    );

    if (confirmed) {
      restoreMutation.mutate(veterinarian.id);
    }
  }

  const data = veterinariansQuery.data;
  const veterinarians = data?.items ?? [];

  const totalItems = data?.totalItems ?? 0;

  const totalPages = Math.max(data?.totalPages ?? 0, 1);

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">
            Directorio veterinario
          </p>

          <h1 className="mt-1 text-2xl font-semibold text-slate-900">
            Veterinarios
          </h1>

          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Administra médicos veterinarios, sus datos profesionales y su
            veterinaria asignada cuando aplique.
          </p>
        </div>

        {canManageVeterinarians && (
          <button
            type="button"
            onClick={() =>
              setModalState({
                mode: "create",
                veterinarian: null,
              })
            }
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            + Registrar veterinario
          </button>
        )}
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="inline-flex rounded-xl bg-slate-100 p-1">
            <button
              type="button"
              onClick={() => {
                setPage(1);
                setStatusFilter("active");
              }}
              className={[
                "rounded-lg px-4 py-2 text-sm font-medium transition",
                statusFilter === "active"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-900",
              ].join(" ")}
            >
              Activos
            </button>

            <button
              type="button"
              onClick={() => {
                setPage(1);
                setStatusFilter("inactive");
              }}
              className={[
                "rounded-lg px-4 py-2 text-sm font-medium transition",
                statusFilter === "inactive"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-900",
              ].join(" ")}
            >
              Inactivos
            </button>
          </div>

          <div className="grid gap-3 sm:grid-cols-3 xl:w-auto">
            <input
              type="search"
              value={clinicSearchInput}
              onChange={(event) => setClinicSearchInput(event.target.value)}
              placeholder="Buscar veterinaria"
              className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none"
            />
            <select
              value={clinicFilter}
              onChange={(event) => {
                const selected = clinicOptions.find(
                  (option) => option.id === event.target.value,
                );
                setPage(1);
                setClinicFilter(event.target.value);
                setSelectedClinicFilter(selected ?? null);
              }}
              disabled={clinicOptionsQuery.isFetching || clinicSearchPending}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none"
              aria-label="Filtrar por veterinaria"
            >
              <option value="">Todas las veterinarias</option>

              {clinicOptions.map((clinic) => (
                <option key={clinic.id} value={clinic.id}>
                  {clinic.displayName}
                  {clinic.isActive === false ? " — Inactiva" : ""}
                </option>
              ))}
            </select>

            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Buscar nombre, apellidos, veterinaria, teléfono o cédula..."
              className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:w-80"
            />
          </div>
        </div>

        <div className="mt-3 flex items-center gap-2 text-xs text-slate-500">
          <span>Resultados de veterinarias:</span>
          <button
            type="button"
            onClick={() =>
              setClinicLookupPage((current) => Math.max(1, current - 1))
            }
            disabled={clinicLookupPage <= 1 || clinicOptionsQuery.isFetching}
            className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
          >
            Anterior
          </button>
          <span>
            Página {clinicLookupPage} de{" "}
            {Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1)}
          </span>
          <button
            type="button"
            onClick={() =>
              setClinicLookupPage((current) =>
                Math.min(
                  current + 1,
                  Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1),
                ),
              )
            }
            disabled={
              clinicOptionsQuery.isFetching ||
              clinicLookupPage >=
                Math.max(clinicOptionsQuery.data?.totalPages ?? 0, 1)
            }
            className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
          >
            Siguiente
          </button>
        </div>

        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {veterinariansQuery.isFetching
              ? "Actualizando información..."
              : `${totalItems} veterinario${totalItems === 1 ? "" : "s"}`}
          </p>

          {(normalizedSearch || clinicFilter) && (
            <button
              type="button"
              onClick={() => {
                setPage(1);
                setSearchInput("");
                setClinicFilter("");
                setSelectedClinicFilter(null);
              }}
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Limpiar filtros
            </button>
          )}
        </div>
      </div>

      {veterinariansQuery.isLoading || searchPending ? (
        <div className="rounded-2xl border border-slate-200 bg-white px-6 py-12 text-center text-sm text-slate-500">
          Cargando veterinarios...
        </div>
      ) : veterinariansQuery.isError ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5 text-sm text-red-700">
          No fue posible cargar los veterinarios. Verifica que el backend esté
          funcionando.
        </div>
      ) : (
        <VeterinariansTable
          veterinarians={veterinarians}
          showingActive={isActive}
          pendingVeterinarianId={pendingVeterinarianId}
          canManage={canManageVeterinarians}
          onEdit={(veterinarian) =>
            setModalState({
              mode: "edit",
              veterinarian,
            })
          }
          onDeactivate={handleDeactivate}
          onRestore={handleRestore}
        />
      )}

      {!searchPending && totalPages > 1 && (
        <div className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-500">
            Página {page} de {totalPages}
          </p>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
              disabled={page <= 1}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-40"
            >
              Anterior
            </button>

            <button
              type="button"
              onClick={() =>
                setPage((current) => Math.min(current + 1, totalPages))
              }
              disabled={page >= totalPages}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        </div>
      )}

      {!searchPending && (
        <div className="flex justify-end">
          <select
            value={pageSize}
            onChange={(event) => {
              setPage(1);
              setPageSize(Number(event.target.value));
            }}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700"
            aria-label="Veterinarios por página"
          >
            <option value={10}>10 por página</option>
            <option value={20}>20 por página</option>
            <option value={50}>50 por página</option>
          </select>
        </div>
      )}

      {canManageVeterinarians && (
        <VeterinarianFormModal
          isOpen={modalState !== null}
          mode={modalState?.mode ?? "create"}
          veterinarian={modalState?.veterinarian ?? null}
          isSubmitting={isFormSubmitting}
          onClose={() => {
            if (!isFormSubmitting) {
              setModalState(null);
            }
          }}
          onSubmit={handleFormSubmit}
        />
      )}
    </section>
  );
}
