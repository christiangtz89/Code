import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import {
  createReception,
  deactivateReception,
  getReceptions,
  searchReceptions,
  updateReception,
} from "../api/receptionsApi";
import { ReceptionFormModal } from "../components/ReceptionFormModal";
import { WeightRangeChangeConfirmationModal } from "../components/WeightRangeChangeConfirmationModal";
import { ReceptionsTable } from "../components/ReceptionsTable";
import type { ReceptionFormValues } from "../schemas/receptionSchema";
import type {
  PagedReceptions,
  Reception,
  UpdateReceptionPayload,
  WeightRangeChangeConfirmationResponse,
  WeightRangeChangeDetails,
} from "../types/reception.types";
import {
  createReceptionPayload,
  updateReceptionPayload,
} from "../utils/receptionPayload";

type ReceptionFormMode = "create" | "edit";

interface ReceptionModalState {
  mode: ReceptionFormMode;
  reception: Reception | null;
}

interface WeightRangeChangeConfirmationState {
  reception: Reception;
  values: ReceptionFormValues;
  details: WeightRangeChangeDetails;
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

function getWeightRangeChangeConfirmation(
  error: unknown,
): WeightRangeChangeConfirmationResponse | null {
  if (!axios.isAxiosError(error) || error.response?.status !== 409) {
    return null;
  }

  const data = error.response.data;

  if (!data || typeof data !== "object") {
    return null;
  }

  const response = data as Partial<WeightRangeChangeConfirmationResponse>;

  if (
    response.code !== "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED" ||
    !response.weightChange
  ) {
    return null;
  }

  return response as WeightRangeChangeConfirmationResponse;
}

export function ReceptionsPage() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [modalState, setModalState] = useState<ReceptionModalState | null>(
    null,
  );

  const [weightRangeChangeConfirmation, setWeightRangeChangeConfirmation] =
    useState<WeightRangeChangeConfirmationState | null>(null);

  const normalizedSearch = debouncedSearch.trim();

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput);
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [normalizedSearch, pageSize]);

  const receptionsQuery = useQuery({
    queryKey: [
      "receptions",
      {
        page,
        pageSize,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PagedReceptions> => {
      if (normalizedSearch) {
        const found = await searchReceptions(normalizedSearch);

        const totalItems = found.length;
        const totalPages =
          totalItems > 0 ? Math.ceil(totalItems / pageSize) : 0;

        const startIndex = (page - 1) * pageSize;
        const items = found.slice(startIndex, startIndex + pageSize);

        return {
          items,
          page,
          pageSize,
          totalItems,
          totalPages,
        };
      }

      return getReceptions({
        page,
        pageSize,
      });
    },

    placeholderData: keepPreviousData,
  });

  async function refreshReceptions() {
    await queryClient.invalidateQueries({
      queryKey: ["receptions"],
    });
  }

  const createMutation = useMutation({
    mutationFn: createReception,

    onSuccess: async () => {
      setPage(1);

      await refreshReceptions();

      toast.success("Recepción registrada correctamente.");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: UpdateReceptionPayload;
    }) => updateReception(id, payload),

    onSuccess: async () => {
      await refreshReceptions();

      toast.success("Recepción actualizada correctamente.");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateReception,

    onSuccess: async () => {
      toast.success("La recepción fue desactivada.");

      await refreshReceptions();
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible desactivar la recepción."),
      );
    },
  });

  const receptions = receptionsQuery.data?.items ?? [];
  const totalItems = receptionsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(receptionsQuery.data?.totalPages ?? 0, 1);

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  async function handleCopyQrCode(reception: Reception) {
    try {
      await navigator.clipboard.writeText(reception.qrCode);
      toast.success("Código QR copiado.");
    } catch {
      toast.error("No fue posible copiar el código QR.");
    }
  }

  async function handleFormSubmit(values: ReceptionFormValues) {
    try {
      if (modalState?.mode === "edit" && modalState.reception !== null) {
        await updateMutation.mutateAsync({
          id: modalState.reception.id,
          payload: updateReceptionPayload(values),
        });
      } else {
        await createMutation.mutateAsync(createReceptionPayload(values));
      }

      setModalState(null);
    } catch (error) {
      const confirmation = getWeightRangeChangeConfirmation(error);

      if (
        confirmation &&
        modalState?.mode === "edit" &&
        modalState.reception !== null
      ) {
        setWeightRangeChangeConfirmation({
          reception: modalState.reception,
          values,
          details: confirmation.weightChange,
        });

        return;
      }

      toast.error(
        getApiErrorMessage(
          error,
          modalState?.mode === "edit"
            ? "No fue posible actualizar la recepción."
            : "No fue posible registrar la recepción.",
        ),
      );
    }
  }

  async function handleConfirmWeightRangeChange() {
    if (!weightRangeChangeConfirmation) {
      return;
    }

    try {
      await updateMutation.mutateAsync({
        id: weightRangeChangeConfirmation.reception.id,

        payload: updateReceptionPayload(
          weightRangeChangeConfirmation.values,
          true,
        ),
      });

      setWeightRangeChangeConfirmation(null);
      setModalState(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible confirmar la corrección de peso.",
        ),
      );
    }
  }

  function handleDeactivate(reception: Reception) {
    const confirmed = window.confirm(
      `¿Deseas desactivar la recepción de ${reception.petName}?`,
    );

    if (confirmed) {
      deactivateMutation.mutate(reception.id);
    }
  }

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Operación</p>

          <h1 className="mt-1 text-3xl font-bold tracking-tight text-slate-950">
            Recepciones
          </h1>

          <p className="mt-2 max-w-3xl text-sm text-slate-600">
            Administra el ingreso de mascotas, el peso verificado, la referencia
            veterinaria, los objetos personales y su código de identificación.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
              Recepciones encontradas
            </p>

            <p className="mt-1 text-2xl font-semibold text-slate-900">
              {totalItems}
            </p>
          </div>

          <button
            type="button"
            onClick={() =>
              setModalState({
                mode: "create",
                reception: null,
              })
            }
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            + Registrar recepción
          </button>
        </div>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-1 flex-col gap-3 sm:flex-row">
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Buscar por QR, mascota, cliente o usuario..."
              className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:max-w-xl"
            />

            {searchInput && (
              <button
                type="button"
                onClick={() => setSearchInput("")}
                className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              >
                Limpiar búsqueda
              </button>
            )}
          </div>

          <select
            value={pageSize}
            onChange={(event) => setPageSize(Number(event.target.value))}
            aria-label="Recepciones por página"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          >
            <option value={10}>10 por página</option>
            <option value={20}>20 por página</option>
            <option value={50}>50 por página</option>
          </select>
        </div>
      </div>

      <div>
        {receptionsQuery.isFetching && !receptionsQuery.isLoading && (
          <p className="mb-3 text-sm text-slate-500">
            Actualizando información...
          </p>
        )}

        {receptionsQuery.isLoading && (
          <div className="rounded-2xl border border-slate-200 bg-white px-6 py-16 text-center text-sm text-slate-500">
            Cargando recepciones...
          </div>
        )}

        {receptionsQuery.isError && (
          <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5">
            <p className="font-medium text-red-800">
              No fue posible cargar las recepciones.
            </p>

            <p className="mt-1 text-sm text-red-700">
              Verifica que el backend esté funcionando e intenta nuevamente.
            </p>
          </div>
        )}

        {!receptionsQuery.isLoading && !receptionsQuery.isError && (
          <ReceptionsTable
            receptions={receptions}
            pendingReceptionId={deactivateMutation.variables ?? null}
            onCopyQrCode={handleCopyQrCode}
            onEdit={(reception) =>
              setModalState({
                mode: "edit",
                reception,
              })
            }
            onDeactivate={handleDeactivate}
          />
        )}
      </div>

      {!receptionsQuery.isLoading && !receptionsQuery.isError && (
        <footer className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white px-5 py-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-600">
            Página {page} de {totalPages}
          </p>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((currentPage) => currentPage - 1)}
              disabled={page <= 1 || receptionsQuery.isFetching}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Anterior
            </button>

            <button
              type="button"
              onClick={() => setPage((currentPage) => currentPage + 1)}
              disabled={page >= totalPages || receptionsQuery.isFetching}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Siguiente
            </button>
          </div>
        </footer>
      )}

      <ReceptionFormModal
        isOpen={modalState !== null}
        mode={modalState?.mode ?? "create"}
        reception={modalState?.reception ?? null}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setModalState(null);
          }
        }}
        onSubmit={handleFormSubmit}
      />
      {weightRangeChangeConfirmation && (
        <WeightRangeChangeConfirmationModal
          petName={weightRangeChangeConfirmation.reception.petName}
          customerName={weightRangeChangeConfirmation.reception.customerName}
          qrCode={weightRangeChangeConfirmation.reception.qrCode}
          details={weightRangeChangeConfirmation.details}
          isSubmitting={updateMutation.isPending}
          onCancel={() => setWeightRangeChangeConfirmation(null)}
          onConfirm={() => {
            void handleConfirmWeightRangeChange();
          }}
        />
      )}
    </section>
  );
}
