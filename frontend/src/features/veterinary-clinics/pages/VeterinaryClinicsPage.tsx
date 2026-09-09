import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import {
  createVeterinaryClinic,
  deactivateVeterinaryClinic,
  getVeterinaryClinics,
  restoreVeterinaryClinic,
  searchVeterinaryClinics,
  updateVeterinaryClinic,
} from "../api/veterinaryClinicsApi";
import { ClinicVeterinariansModal } from "../components/ClinicVeterinariansModal";
import { VeterinaryClinicFormModal } from "../components/VeterinaryClinicFormModal";
import { VeterinaryClinicsTable } from "../components/VeterinaryClinicsTable";
import type { VeterinaryClinicFormValues } from "../schemas/veterinaryClinicSchema";
import type {
  PaginatedVeterinaryClinics,
  VeterinaryClinic,
  VeterinaryClinicPayload,
} from "../types/veterinaryClinic.types";

type ClinicStatusFilter = "active" | "inactive";

type FormMode = "create" | "edit";

interface ClinicModalState {
  mode: FormMode;
  clinic: VeterinaryClinic | null;
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

function createPayload(
  values: VeterinaryClinicFormValues,
): VeterinaryClinicPayload {
  return {
    name: values.name.trim(),
    phone: normalizeOptional(values.phone),
    email: normalizeOptional(values.email),
    address: normalizeOptional(values.address),
    primaryContactName: normalizeOptional(values.primaryContactName),
  };
}

export function VeterinaryClinicsPage() {
  const queryClient = useQueryClient();

  const [statusFilter, setStatusFilter] =
    useState<ClinicStatusFilter>("active");

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [modalState, setModalState] = useState<ClinicModalState | null>(null);

  const [selectedVeterinariansClinic, setSelectedVeterinariansClinic] =
    useState<VeterinaryClinic | null>(null);

  const isActive = statusFilter === "active";

  const normalizedSearch = debouncedSearch.trim();
  const searchPending = searchInput.trim() !== normalizedSearch;
  const canManageClinics = hasPermission("VeterinaryClinics.Manage");
  const canViewVeterinarians = hasPermission("Veterinarians.View");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setPage(1);
      setDebouncedSearch(searchInput);
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchInput]);

  const clinicsQuery = useQuery({
    queryKey: [
      "veterinary-clinics",
      {
        page,
        pageSize,
        isActive,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PaginatedVeterinaryClinics> => {
      if (normalizedSearch) {
        return searchVeterinaryClinics({
          search: normalizedSearch,
          isActive,
          page,
          pageSize,
        });
      }

      return getVeterinaryClinics({
        page,
        pageSize,
        isActive,
      });
    },
  });

  useEffect(() => {
    const totalPages = clinicsQuery.data?.totalPages;

    if (totalPages !== undefined && totalPages > 0 && page > totalPages) {
      setPage(totalPages);
    }
  }, [clinicsQuery.data?.totalPages, page]);

  async function refreshClinics() {
    await queryClient.invalidateQueries({
      queryKey: ["veterinary-clinics"],
    });
  }

  const createMutation = useMutation({
    mutationFn: createVeterinaryClinic,

    onSuccess: async () => {
      await refreshClinics();

      toast.success("Veterinaria registrada correctamente.");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: VeterinaryClinicPayload;
    }) => updateVeterinaryClinic(id, payload),

    onSuccess: async () => {
      await refreshClinics();

      toast.success("Veterinaria actualizada correctamente.");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateVeterinaryClinic,

    onSuccess: async () => {
      await refreshClinics();

      toast.success("Veterinaria desactivada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible desactivar la veterinaria."),
      );
    },
  });

  const restoreMutation = useMutation({
    mutationFn: restoreVeterinaryClinic,

    onSuccess: async () => {
      await refreshClinics();

      toast.success("Veterinaria restaurada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible restaurar la veterinaria."),
      );
    },
  });

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  const pendingClinicId = useMemo(() => {
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

  async function handleFormSubmit(values: VeterinaryClinicFormValues) {
    const payload = createPayload(values);

    try {
      if (modalState?.mode === "edit") {
        if (!modalState.clinic) {
          return;
        }

        await updateMutation.mutateAsync({
          id: modalState.clinic.id,
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
            ? "No fue posible actualizar la veterinaria."
            : "No fue posible registrar la veterinaria.",
        ),
      );
    }
  }

  async function handleDeactivate(clinic: VeterinaryClinic) {
    const confirmed = window.confirm(
      `¿Deseas desactivar la veterinaria "${clinic.name}"?`,
    );

    if (!confirmed) {
      return;
    }

    await deactivateMutation.mutateAsync(clinic.id);
  }

  async function handleRestore(clinic: VeterinaryClinic) {
    const confirmed = window.confirm(
      `¿Deseas restaurar la veterinaria "${clinic.name}"?`,
    );

    if (!confirmed) {
      return;
    }

    await restoreMutation.mutateAsync(clinic.id);
  }

  const data = clinicsQuery.data;

  const clinics = data?.items ?? [];
  const totalItems = data?.totalItems ?? 0;
  const totalPages = data?.totalPages ?? 0;

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">
            Directorio veterinario
          </p>

          <h1 className="mt-1 text-2xl font-semibold text-slate-900">
            Veterinarias
          </h1>

          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Administra clínicas veterinarias, sus datos de contacto y sus
            veterinarios asociados.
          </p>
        </div>

        {canManageClinics && (
          <button
            type="button"
            onClick={() =>
              setModalState({
                mode: "create",
                clinic: null,
              })
            }
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            + Registrar veterinaria
          </button>
        )}
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
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
              Activas
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
              Inactivas
            </button>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row">
            <label className="min-w-0 sm:w-80">
              <span className="sr-only">Buscar veterinarias</span>

              <input
                type="search"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Buscar por nombre, contacto, teléfono..."
                className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
              />
            </label>

            <label className="flex items-center gap-2 text-sm text-slate-600">
              <span>Mostrar</span>

              <select
                value={pageSize}
                onChange={(event) => {
                  setPage(1);
                  setPageSize(Number(event.target.value));
                }}
                className="rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none disabled:bg-slate-100"
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
              </select>
            </label>
          </div>
        </div>
      </div>

      {(clinicsQuery.isLoading || searchPending) && (
        <div className="rounded-2xl border border-slate-200 bg-white px-6 py-14 text-center text-sm text-slate-500">
          Cargando veterinarias...
        </div>
      )}

      {clinicsQuery.isError && (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5 text-sm text-red-700">
          No fue posible cargar las veterinarias.
        </div>
      )}

      {!clinicsQuery.isLoading && !searchPending && !clinicsQuery.isError && (
        <VeterinaryClinicsTable
          clinics={clinics}
          showingActive={isActive}
          pendingClinicId={pendingClinicId}
          canManage={canManageClinics}
          canViewVeterinarians={canViewVeterinarians}
          onViewVeterinarians={setSelectedVeterinariansClinic}
          onEdit={(clinic) =>
            setModalState({
              mode: "edit",
              clinic,
            })
          }
          onDeactivate={handleDeactivate}
          onRestore={handleRestore}
        />
      )}

      {!clinicsQuery.isLoading && !searchPending && !clinicsQuery.isError && (
        <footer className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white px-5 py-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-500">
            {totalItems === 1 ? "1 veterinaria" : `${totalItems} veterinarias`}
          </p>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={page <= 1}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Anterior
            </button>

            <span className="text-sm text-slate-600">
              Página {page} de {Math.max(totalPages, 1)}
            </span>

            <button
              type="button"
              onClick={() =>
                setPage((current) => Math.min(totalPages, current + 1))
              }
              disabled={totalPages === 0 || page >= totalPages}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Siguiente
            </button>
          </div>
        </footer>
      )}

      {canManageClinics && (
        <VeterinaryClinicFormModal
          isOpen={modalState !== null}
          mode={modalState?.mode ?? "create"}
          clinic={modalState?.clinic ?? null}
          isSubmitting={isFormSubmitting}
          onClose={() => setModalState(null)}
          onSubmit={handleFormSubmit}
        />
      )}

      {canViewVeterinarians && (
        <ClinicVeterinariansModal
          clinic={selectedVeterinariansClinic}
          onClose={() => setSelectedVeterinariansClinic(null)}
        />
      )}
    </section>
  );
}
