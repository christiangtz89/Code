import { CremationReassignmentModal } from "../components/CremationReassignmentModal";
import { CremationInstructionsAmendmentModal } from "../components/CremationInstructionsAmendmentModal";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useMemo, useRef, useState } from "react";
import toast from "react-hot-toast";

import {
  changeCremationStatus,
  createCremation,
  deactivateCremation,
  getAvailableCremationReceptions,
  getCremations,
  getCremationUserOptions,
  restoreCremation,
  searchCremations,
  updateCremation,
} from "../api/cremationsApi";
import { CremationFormModal } from "../components/CremationFormModal";
import { CremationsTable } from "../components/CremationsTable";
import { CremationStatusModal } from "../components/CremationStatusModal";
import { CremationInventoryPanel } from "../components/CremationInventoryPanel";
import { StartPaymentRevalidationModal } from "../components/StartPaymentRevalidationModal";
import { useStartPaymentRevalidation } from "../hooks/useStartPaymentRevalidation";
import { currentUserId, hasPermission } from "../../auth/utils/permissions";
import type { CremationFormValues } from "../schemas/cremationSchema";
import type {
  ChangeCremationStatusPayload,
  Cremation,
  PaginatedCremations,
  StartCremationStatusPayload,
} from "../types/cremation.types";
import { CremationStatus } from "../types/cremation.types";
import { getCremationStatusLabel } from "../utils/cremationLabels";
import {
  createCremationPayload,
  updateCremationPayload,
} from "../utils/cremationForm";

type CremationStatusFilter = "active" | "inactive";
type CremationFormMode = "create" | "edit";

interface CremationModalState {
  mode: CremationFormMode;
  cremation: Cremation | null;
}

interface UncertainStartAttempt {
  cremation: Cremation;
  request: StartCremationStatusPayload;
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

export function CremationsPage() {
  const queryClient = useQueryClient();
  const [reassigning, setReassigning] = useState<Cremation | null>(null);
  const reassignmentOrigin = useRef<HTMLElement | null>(null);
  const [amendingInstructions, setAmendingInstructions] = useState<Cremation | null>(null);
  const instructionsAmendmentOrigin = useRef<HTMLElement | null>(null);
  const revalidation = useStartPaymentRevalidation();

  const [statusFilter, setStatusFilter] =
    useState<CremationStatusFilter>("active");

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [modalState, setModalState] = useState<CremationModalState | null>(
    null,
  );

  const [selectedStatusCremation, setSelectedStatusCremation] =
    useState<Cremation | null>(null);
  const [uncertainStartAttempt, setUncertainStartAttempt] =
    useState<UncertainStartAttempt | null>(null);
  const statusRequestBusy = useRef(false);
  const [selectedInventoryCremation, setSelectedInventoryCremation] = useState<Cremation | null>(null);

  const isActive = statusFilter === "active";
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
  }, [statusFilter, normalizedSearch, pageSize]);

  const cremationsQuery = useQuery({
    queryKey: [
      "cremations",
      {
        page,
        pageSize,
        isActive,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PaginatedCremations> => {
      if (normalizedSearch) {
        const items = await searchCremations({
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

      return getCremations({
        page,
        pageSize,
        isActive,
      });
    },

    placeholderData: keepPreviousData,
  });

  useEffect(() => {
    const totalPages = cremationsQuery.data?.totalPages;

    if (
      !normalizedSearch &&
      totalPages &&
      totalPages > 0 &&
      page > totalPages
    ) {
      setPage(totalPages);
    }
  }, [cremationsQuery.data?.totalPages, normalizedSearch, page]);

  const availableReceptionsQuery = useQuery({
    queryKey: ["cremations", "options", "receptions"],

    queryFn: getAvailableCremationReceptions,

    enabled: modalState?.mode === "create",
  });

  const userOptionsQuery = useQuery({
    queryKey: ["cremations", "options", "users"],

    queryFn: getCremationUserOptions,

    enabled: modalState !== null || reassigning !== null,
  });

  async function refreshCremations() {
    await queryClient.invalidateQueries({
      queryKey: ["cremations"],
    });
  }

  async function refreshReceptionOptions() {
    await queryClient.invalidateQueries({
      queryKey: ["cremations", "options", "receptions"],
    });
  }

  const createMutation = useMutation({
    mutationFn: createCremation,

    onSuccess: async () => {
      await refreshCremations();
      await refreshReceptionOptions();

      toast.success("Cremación registrada correctamente.");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: CremationFormValues }) =>
      updateCremation(id, updateCremationPayload(values)),

    onSuccess: async () => {
      await refreshCremations();

      await queryClient.invalidateQueries({
        queryKey: ["payments"],
      });

      toast.success("Cremación actualizada correctamente.");
    },
  });

  const changeStatusMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: ChangeCremationStatusPayload;
    }) => changeCremationStatus(id, payload),

  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateCremation,

    onSuccess: async () => {
      await refreshCremations();

      toast.success("Cremación desactivada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible desactivar la cremación."),
      );
    },
  });

  const restoreMutation = useMutation({
    mutationFn: restoreCremation,

    onSuccess: async () => {
      await refreshCremations();

      toast.success("Cremación restaurada correctamente.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible restaurar la cremación."),
      );
    },
  });

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending;

  const pendingCremationId = useMemo(() => {
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

  async function handleFormSubmit(values: CremationFormValues) {
    try {
      if (modalState?.mode === "edit" && modalState.cremation) {
        await updateMutation.mutateAsync({
          id: modalState.cremation.id,
          values,
        });
      } else {
        await createMutation.mutateAsync(createCremationPayload(values));
      }

      setModalState(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalState?.mode === "edit"
            ? "No fue posible actualizar la cremación."
            : "No fue posible registrar la cremación.",
        ),
      );
    }
  }

  async function sendStatusRequest(
    cremation: Cremation,
    payload: ChangeCremationStatusPayload,
  ) {
    if (statusRequestBusy.current || !hasPermission("Cremations.Manage")) return;
    if (
      payload.status === CremationStatus.InProgress &&
      currentUserId()?.toLowerCase() !==
        cremation.assignedToUserId?.toLowerCase()
    ) {
      toast.error("Solo el empleado asignado puede iniciar esta cremación.");
      return;
    }

    statusRequestBusy.current = true;
    try {
      const current = await changeStatusMutation.mutateAsync({
        id: cremation.id,
        payload,
      });
      setUncertainStartAttempt(null);
      setSelectedStatusCremation(null);
      toast.success(
        payload.status === CremationStatus.InProgress
          ? `Inicio confirmado. Estado actual: ${getCremationStatusLabel(current.status)}.`
          : "Estado actualizado correctamente.",
      );
      // A refresh failure cannot turn a confirmed start into an uncertain request.
      await Promise.allSettled([refreshCremations()]);
    } catch (error) {
      const status = axios.isAxiosError(error) ? error.response?.status : null;
      if (
        payload.status === CremationStatus.InProgress &&
        axios.isAxiosError(error) &&
        (!status || status === 408 || status >= 500)
      ) {
        setUncertainStartAttempt({ cremation, request: payload });
      } else {
        setUncertainStartAttempt(null);
        toast.error(
          getApiErrorMessage(
            error,
            "No fue posible cambiar el estado de la cremación.",
          ),
        );
      }
    } finally {
      statusRequestBusy.current = false;
    }
  }

  async function handleStatusSubmit(payload: ChangeCremationStatusPayload) {
    if (!selectedStatusCremation || uncertainStartAttempt) return;
    await sendStatusRequest(selectedStatusCremation, payload);
  }

  async function handleStatusRetry() {
    if (!uncertainStartAttempt) return;
    await sendStatusRequest(
      uncertainStartAttempt.cremation,
      uncertainStartAttempt.request,
    );
  }

  function handleDeactivate(cremation: Cremation) {
    const confirmed = window.confirm(
      `¿Deseas desactivar la cremación de ${cremation.petName}?`,
    );

    if (!confirmed) {
      return;
    }

    deactivateMutation.mutate(cremation.id);
  }

  function handleRestore(cremation: Cremation) {
    const confirmed = window.confirm(
      `¿Deseas restaurar la cremación de ${cremation.petName}?`,
    );

    if (!confirmed) {
      return;
    }

    restoreMutation.mutate(cremation.id);
  }

  const cremations = cremationsQuery.data?.items ?? [];

  const totalItems = cremationsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(cremationsQuery.data?.totalPages ?? 0, 1);

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Operación</p>

          <h1
            id="cremations-title"
            tabIndex={-1}
            className="mt-1 text-3xl font-semibold tracking-tight text-slate-900"
          >
            Cremaciones
          </h1>

          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Administra los servicios de cremación, programación y flujo
            operativo hasta la entrega.
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            setModalState({
              mode: "create",
              cremation: null,
            })
          }
          className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
        >
          + Registrar cremación
        </button>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
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

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Buscar QR, mascota, cliente o paquete..."
              className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:w-96"
            />

            {!normalizedSearch && (
              <select
                value={pageSize}
                onChange={(event) => setPageSize(Number(event.target.value))}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none"
                aria-label="Cremaciones por página"
              >
                <option value={10}>10 por página</option>

                <option value={20}>20 por página</option>

                <option value={50}>50 por página</option>
              </select>
            )}
          </div>
        </div>

        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {cremationsQuery.isFetching
              ? "Actualizando información..."
              : `${totalItems} cremación${totalItems === 1 ? "" : "es"}`}
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
      </div>

      {revalidation.isUncertain &&
        revalidation.attempt &&
        hasPermission("Cremations.Manage") && (
          <div
            role="status"
            className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"
          >
            <p>
              Falta confirmar el resultado de la validación de{" "}
              {revalidation.attempt.cremation.petName}.
            </p>
            <button
              type="button"
              disabled={revalidation.isPending}
              onClick={revalidation.reopen}
              className="mt-2 rounded-lg border border-amber-300 px-3 py-2 font-semibold disabled:opacity-50"
            >
              Revisar validación pendiente
            </button>
          </div>
        )}

      {uncertainStartAttempt && hasPermission("Cremations.Manage") && (
        <div
          role="status"
          className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"
        >
          <p>
            Falta confirmar el resultado del inicio de{" "}
            {uncertainStartAttempt.cremation.petName}.
          </p>
          <button
            type="button"
            disabled={changeStatusMutation.isPending}
            onClick={() =>
              setSelectedStatusCremation(uncertainStartAttempt.cremation)
            }
            className="mt-2 rounded-lg border border-amber-300 px-3 py-2 font-semibold disabled:opacity-50"
          >
            Revisar inicio pendiente
          </button>
        </div>
      )}

      {cremationsQuery.isError ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5 text-sm text-red-700">
          No fue posible cargar las cremaciones. Verifica que el backend esté
          funcionando.
        </div>
      ) : (
        <CremationsTable
          onAmendInstructions={(cremation) => {
            instructionsAmendmentOrigin.current = document.activeElement instanceof HTMLElement
              ? document.activeElement : null;
            setAmendingInstructions(cremation);
          }}
          instructionsAmendmentBusy={amendingInstructions !== null}
          onReassign={(cremation) => {
            reassignmentOrigin.current = document.activeElement instanceof HTMLElement
              ? document.activeElement : null;
            setReassigning(cremation);
          }}
          reassignmentBusy={reassigning !== null}
          cremations={cremations}
          showingActive={isActive}
          pendingCremationId={pendingCremationId}
          onEdit={(cremation) =>
            setModalState({
              mode: "edit",
              cremation,
            })
          }
          onChangeStatus={(cremation) => {
            if (!uncertainStartAttempt) setSelectedStatusCremation(cremation);
          }}
          onDeactivate={handleDeactivate}
          onRestore={handleRestore}
          onInventory={setSelectedInventoryCremation}
          onRevalidateStartPayment={revalidation.start}
          revalidationBusy={
            revalidation.isPending || revalidation.attempt !== null
          }
          revalidatingId={
            revalidation.isPending
              ? (revalidation.attempt?.cremation.id ?? null)
              : null
          }
          statusChangeBusy={
            changeStatusMutation.isPending || uncertainStartAttempt !== null
          }
        />
      )}

      {selectedInventoryCremation && <CremationInventoryPanel id={selectedInventoryCremation.id} />}

      {!normalizedSearch && totalPages > 1 && (
        <div className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-500">
            Página {page} de {totalPages}
          </p>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
              disabled={page <= 1}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Anterior
            </button>

            <button
              type="button"
              onClick={() =>
                setPage((current) => Math.min(current + 1, totalPages))
              }
              disabled={page >= totalPages}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        </div>
      )}

      {reassigning && (
        <CremationReassignmentModal
          cremation={reassigning}
          origin={reassignmentOrigin.current}
          users={userOptionsQuery.data ?? []}
          loadingUsers={userOptionsQuery.isLoading}
          onClose={() => setReassigning(null)}
        />
      )}

      {amendingInstructions && (
        <CremationInstructionsAmendmentModal
          cremation={amendingInstructions}
          origin={instructionsAmendmentOrigin.current}
          onClose={() => setAmendingInstructions(null)}
        />
      )}

      <CremationFormModal
        isOpen={modalState !== null}
        mode={modalState?.mode ?? "create"}
        cremation={modalState?.cremation ?? null}
        receptions={availableReceptionsQuery.data ?? []}
        users={userOptionsQuery.data ?? []}
        isLoadingReceptions={availableReceptionsQuery.isLoading}
        isLoadingUsers={userOptionsQuery.isLoading}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setModalState(null);
          }
        }}
        onSubmit={handleFormSubmit}
      />

      {revalidation.isOpen &&
        revalidation.attempt &&
        hasPermission("Cremations.Manage") && (
          <StartPaymentRevalidationModal
            cremation={revalidation.attempt.cremation}
            preview={revalidation.attempt.preview}
            isPending={revalidation.isPending}
            isUncertain={revalidation.isUncertain}
            returnFocus={revalidation.returnFocus}
            onClose={revalidation.close}
            onConfirm={revalidation.confirm}
            onRetry={revalidation.retry}
          />
        )}

      <CremationStatusModal
        isOpen={selectedStatusCremation !== null}
        cremation={selectedStatusCremation}
        isSubmitting={changeStatusMutation.isPending}
        retryRequest={
          uncertainStartAttempt &&
          selectedStatusCremation?.id === uncertainStartAttempt.cremation.id
            ? uncertainStartAttempt.request
            : null
        }
        onClose={() => {
          if (!changeStatusMutation.isPending) {
            setSelectedStatusCremation(null);
          }
        }}
        onSubmit={handleStatusSubmit}
        onRetry={handleStatusRetry}
      />
    </section>
  );
}
