import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getCustomers } from "../../customers/api/customersApi";
import {
  createPet,
  deactivatePet,
  getPets,
  restorePet,
  searchPets,
  updatePet,
} from "../api/petsApi";
import { PetFormModal } from "../components/PetFormModal";
import { PetsTable } from "../components/PetsTable";
import type { PetFormValues } from "../schemas/petSchema";
import type {
  CreatePetPayload,
  PagedPets,
  Pet,
  UpdatePetPayload,
} from "../types/pet.types";

type PetStatusFilter = "active" | "inactive";

interface PetModalState {
  mode: "create" | "edit";
  pet: Pet | null;
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

function toCommonPetPayload(values: PetFormValues): UpdatePetPayload {
  return {
    name: values.name.trim(),
    species: values.species.trim(),
    breed: values.breed.trim(),
    sex: values.sex.trim(),
    color: values.color.trim(),
    weightKg: Number(values.weightKg),
    ageYears: values.ageYears === "" ? null : Number(values.ageYears),
    dateOfDeath: values.dateOfDeath,
  };
}

export function PetsPage() {
  const queryClient = useQueryClient();

  const [statusFilter, setStatusFilter] = useState<PetStatusFilter>("active");

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [modalState, setModalState] = useState<PetModalState | null>(null);

  const isActive = statusFilter === "active";

  const normalizedSearch = debouncedSearch.trim();

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [statusFilter, normalizedSearch, pageSize]);

  const customersQuery = useQuery({
    queryKey: ["customers", "pet-owner-options"],

    queryFn: () =>
      getCustomers({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    staleTime: 60_000,
  });

  const petsQuery = useQuery({
    queryKey: [
      "pets",
      {
        page,
        pageSize,
        isActive,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PagedPets> => {
      if (normalizedSearch) {
        const items = await searchPets({
          search: normalizedSearch,
          isActive,
        });

        return {
          items,
          page: 1,
          pageSize: items.length,
          totalItems: items.length,
          totalPages: items.length > 0 ? 1 : 0,
        };
      }

      return getPets({
        page,
        pageSize,
        isActive,
      });
    },

    placeholderData: keepPreviousData,
  });

  useEffect(() => {
    const totalPages = petsQuery.data?.totalPages;

    if (
      !normalizedSearch &&
      totalPages !== undefined &&
      totalPages > 0 &&
      page > totalPages
    ) {
      setPage(totalPages);
    }
  }, [normalizedSearch, page, petsQuery.data?.totalPages]);

  async function refreshPets() {
    await queryClient.invalidateQueries({
      queryKey: ["pets"],
    });
  }

  const createMutation = useMutation({
    mutationFn: createPet,

    onSuccess: async () => {
      await refreshPets();

      toast.success("Mascota registrada correctamente.");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdatePetPayload }) =>
      updatePet(id, payload),

    onSuccess: async () => {
      await refreshPets();

      toast.success("Mascota actualizada correctamente.");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivatePet,

    onSuccess: async () => {
      await refreshPets();

      toast.success("Mascota desactivada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible desactivar la mascota."),
      );
    },
  });

  const restoreMutation = useMutation({
    mutationFn: restorePet,

    onSuccess: async () => {
      await refreshPets();

      toast.success("Mascota restaurada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible restaurar la mascota."),
      );
    },
  });

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  const pendingPetId = useMemo(() => {
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

  async function handleFormSubmit(values: PetFormValues) {
    try {
      const commonPayload = toCommonPetPayload(values);

      if (modalState?.mode === "edit") {
        if (!modalState.pet) {
          return;
        }

        await updateMutation.mutateAsync({
          id: modalState.pet.id,
          payload: commonPayload,
        });
      } else {
        const payload: CreatePetPayload = {
          customerId: values.customerId,
          ...commonPayload,
        };

        await createMutation.mutateAsync(payload);
      }

      setModalState(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalState?.mode === "edit"
            ? "No fue posible actualizar la mascota."
            : "No fue posible registrar la mascota.",
        ),
      );
    }
  }

  function handleDeactivate(pet: Pet) {
    const confirmed = window.confirm(`¿Deseas desactivar a ${pet.name}?`);

    if (confirmed) {
      deactivateMutation.mutate(pet.id);
    }
  }

  function handleRestore(pet: Pet) {
    const confirmed = window.confirm(`¿Deseas restaurar a ${pet.name}?`);

    if (confirmed) {
      restoreMutation.mutate(pet.id);
    }
  }

  const pets = petsQuery.data?.items ?? [];

  const totalItems = petsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(petsQuery.data?.totalPages ?? 0, 1);

  const customers = customersQuery.data?.items ?? [];

  return (
    <section>
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Operación</p>

          <h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
            Mascotas
          </h1>

          <p className="mt-3 max-w-2xl text-slate-600">
            Administra las mascotas asociadas a cada cliente.
          </p>
        </div>

        <button
          type="button"
          disabled={customersQuery.isLoading || customers.length === 0}
          onClick={() =>
            setModalState({
              mode: "create",
              pet: null,
            })
          }
          className="inline-flex items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
        >
          + Registrar mascota
        </button>
      </header>

      {!customersQuery.isLoading && customers.length === 0 && (
        <div className="mt-6 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          Debes registrar al menos un cliente activo antes de registrar una
          mascota.
        </div>
      )}

      <div className="mt-8 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex rounded-xl bg-slate-100 p-1">
            <button
              type="button"
              onClick={() => setStatusFilter("active")}
              className={[
                "flex-1 rounded-lg px-4 py-2 text-sm font-medium transition sm:flex-none",
                statusFilter === "active"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-900",
              ].join(" ")}
            >
              Activas
            </button>

            <button
              type="button"
              onClick={() => setStatusFilter("inactive")}
              className={[
                "flex-1 rounded-lg px-4 py-2 text-sm font-medium transition sm:flex-none",
                statusFilter === "inactive"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-900",
              ].join(" ")}
            >
              Inactivas
            </button>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row">
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Buscar por mascota, especie, raza o propietario"
              className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:w-96"
            />

            {!normalizedSearch && (
              <select
                value={pageSize}
                aria-label="Mascotas por página"
                onChange={(event) => setPageSize(Number(event.target.value))}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none"
              >
                <option value={10}>10 por página</option>
                <option value={20}>20 por página</option>
                <option value={50}>50 por página</option>
              </select>
            )}
          </div>
        </div>
      </div>

      <div className="mt-5 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          {petsQuery.isFetching
            ? "Actualizando información..."
            : `${totalItems} mascota${totalItems === 1 ? "" : "s"}`}
        </p>

        {normalizedSearch && (
          <button
            type="button"
            onClick={() => setSearchInput("")}
            className="text-sm font-medium text-slate-600 hover:text-slate-900"
          >
            Limpiar búsqueda
          </button>
        )}
      </div>

      <div className="mt-4">
        {petsQuery.isLoading ? (
          <div className="rounded-2xl border border-slate-200 bg-white px-6 py-14 text-center text-slate-500">
            Cargando mascotas...
          </div>
        ) : petsQuery.isError ? (
          <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-8">
            <h2 className="font-semibold text-red-800">
              No fue posible cargar las mascotas
            </h2>

            <p className="mt-2 text-sm text-red-700">
              Verifica que el backend esté funcionando e intenta nuevamente.
            </p>

            <button
              type="button"
              onClick={() => petsQuery.refetch()}
              className="mt-4 rounded-lg border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-700"
            >
              Reintentar
            </button>
          </div>
        ) : (
          <PetsTable
            pets={pets}
            showingActive={isActive}
            pendingPetId={pendingPetId}
            onEdit={(pet) =>
              setModalState({
                mode: "edit",
                pet,
              })
            }
            onDeactivate={handleDeactivate}
            onRestore={handleRestore}
          />
        )}
      </div>

      {!normalizedSearch && !petsQuery.isLoading && !petsQuery.isError && (
        <footer className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-500">
            Página {page} de {totalPages}
          </p>

          <div className="flex gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Anterior
            </button>

            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() =>
                setPage((current) => Math.min(current + 1, totalPages))
              }
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        </footer>
      )}

      <PetFormModal
        isOpen={modalState !== null}
        mode={modalState?.mode ?? "create"}
        pet={modalState?.pet ?? null}
        customers={customers}
        customersLoading={customersQuery.isLoading}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setModalState(null);
          }
        }}
        onSubmit={handleFormSubmit}
      />
    </section>
  );
}
