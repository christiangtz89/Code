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
  canApproveReceptionLifecycle,
  canRequestReceptionDeactivation,
  hasPermission,
  hasRole,
  isOwnerOrAdmin,
} from "../../auth/utils/permissions";
import {
  createReceptionClarification,
  createReceptionCorrection,
  createReception,
  deactivateReception,
  getReceptionLifecycleHistory,
  getReceptionHistory,
  getReceptions,
  requestReceptionDeactivation,
  restoreReception,
  searchReceptions,
  updateReception,
} from "../api/receptionsApi";
import { ReceptionClarificationModal } from "../components/ReceptionClarificationModal";
import { ReceptionCorrectionModal } from "../components/ReceptionCorrectionModal";
import { ReceptionFormModal } from "../components/ReceptionFormModal";
import { ReceptionHistoryModal } from "../components/ReceptionHistoryModal";
import {
  ReceptionLifecycleActionModal,
  type ReceptionLifecycleActionMode,
} from "../components/ReceptionLifecycleActionModal";
import { ReceptionLifecycleHistoryModal } from "../components/ReceptionLifecycleHistoryModal";
import { WeightRangeChangeConfirmationModal } from "../components/WeightRangeChangeConfirmationModal";
import { ReceptionsTable } from "../components/ReceptionsTable";
import type { ReceptionFormValues } from "../schemas/receptionSchema";
import type {
  PagedReceptions,
  Reception,
  ReceptionCorrectionDraft,
  ReceptionLifecycleActionRequest,
  ReceptionLifecycleDecision,
  UpdateReceptionPayload,
  WeightRangeChangeDetails,
} from "../types/reception.types";
import {
  createReceptionPayload,
  updateReceptionPayload,
} from "../utils/receptionPayload";
import { getWeightRangeChangeConfirmation } from "../utils/weightRangeChangeConfirmation";
import { getReceptionCorrectionFingerprint } from "../utils/receptionCorrection";
import { getReceptionLifecycleDependencyLabels } from "../utils/receptionLifecycleLabels";

type ReceptionModalState =
  | { mode: "create"; reception: null }
  | { mode: "edit" | "view"; reception: Reception };

interface NormalWeightRangeChangeConfirmationState {
  kind: "normalUpdate";
  reception: Reception;
  values: ReceptionFormValues;
  details: WeightRangeChangeDetails;
}

interface CorrectionWeightRangeChangeConfirmationState {
  kind: "correction";
  reception: Reception;
  draft: ReceptionCorrectionDraft;
  requestId: string;
  fingerprint: string;
  details: WeightRangeChangeDetails;
}

type WeightRangeChangeConfirmationState =
  | NormalWeightRangeChangeConfirmationState
  | CorrectionWeightRangeChangeConfirmationState;

interface CorrectionAttempt {
  receptionId: string;
  requestId: string;
  fingerprint: string;
}

interface ClarificationAttempt {
  receptionId: string;
  requestId: string;
  text: string;
}

interface LifecycleActionState {
  mode: ReceptionLifecycleActionMode;
  reception: Reception;
}

interface LifecycleAttempt {
  receptionId: string;
  mode: ReceptionLifecycleActionMode;
  normalizedReason: string;
  requestId: string;
}

interface LifecycleActionVariables {
  id: string;
  mode: ReceptionLifecycleActionMode;
  payload: ReceptionLifecycleActionRequest;
}

interface LifecycleErrorDetails {
  message: string;
  dependencies: string[];
  eventRecorded: boolean;
}

interface ApiErrorResponse {
  title?: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
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
    const validationMessage = Object.values(data.errors ?? {})
      .flat()
      .find((message) => message.trim().length > 0);

    if (validationMessage) {
      return validationMessage;
    }

    return data.detail ?? data.message ?? data.title ?? fallback;
  }

  return fallback;
}

function getLifecycleErrorDetails(error: unknown): LifecycleErrorDetails {
  const fallback = "No fue posible completar la acción de ciclo de vida.";

  if (!axios.isAxiosError(error) || !error.response) {
    return {
      message: getApiErrorMessage(error, fallback),
      dependencies: [],
      eventRecorded: false,
    };
  }

  const data = error.response.data as
    Partial<ReceptionLifecycleDecision> | ApiErrorResponse | string | undefined;
  const candidateMessage = getApiErrorMessage(error, fallback);
  const message = /(postgres|npgsql|sqlstate|exception|stack trace)/i.test(
    candidateMessage,
  )
    ? fallback
    : candidateMessage;
  const dependencies =
    data &&
    typeof data === "object" &&
    "event" in data &&
    data.event &&
    typeof data.event.dependencies === "number"
      ? getReceptionLifecycleDependencyLabels(data.event.dependencies)
      : [];
  const eventRecorded =
    error.response.status === 409 &&
    data !== undefined &&
    typeof data === "object" &&
    "isBlocked" in data &&
    data.isBlocked === true &&
    "event" in data &&
    data.event !== null &&
    typeof data.event === "object" &&
    typeof data.event.id === "string";

  return { message, dependencies, eventRecorded };
}

export function ReceptionsPage() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [includeInactive, setIncludeInactive] = useState(false);

  const [modalState, setModalState] = useState<ReceptionModalState | null>(
    null,
  );

  const [weightRangeChangeConfirmation, setWeightRangeChangeConfirmation] =
    useState<WeightRangeChangeConfirmationState | null>(null);
  const [correctionReception, setCorrectionReception] =
    useState<Reception | null>(null);
  const [clarificationReception, setClarificationReception] =
    useState<Reception | null>(null);
  const [historyReception, setHistoryReception] = useState<Reception | null>(
    null,
  );
  const [lifecycleHistoryReception, setLifecycleHistoryReception] =
    useState<Reception | null>(null);
  const [lifecycleAction, setLifecycleAction] =
    useState<LifecycleActionState | null>(null);
  const [lifecycleAttempt, setLifecycleAttempt] =
    useState<LifecycleAttempt | null>(null);
  const [lifecycleError, setLifecycleError] =
    useState<LifecycleErrorDetails | null>(null);
  const [correctionAttempt, setCorrectionAttempt] =
    useState<CorrectionAttempt | null>(null);
  const [clarificationAttempt, setClarificationAttempt] =
    useState<ClarificationAttempt | null>(null);

  const canManage = hasPermission("Receptions.Manage");
  const canViewLifecycleHistory = hasPermission("Receptions.View");
  const canApproveLifecycle = canApproveReceptionLifecycle();
  const canRequestDeactivation = canRequestReceptionDeactivation();
  const canAmend =
    hasPermission("Receptions.Amend") &&
    (isOwnerOrAdmin() || hasRole("MANAGER"));

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
  }, [normalizedSearch, pageSize, includeInactive]);

  useEffect(() => {
    if (!canApproveLifecycle && includeInactive) {
      setIncludeInactive(false);
    }
  }, [canApproveLifecycle, includeInactive]);

  const receptionsQuery = useQuery({
    queryKey: [
      "receptions",
      {
        page,
        pageSize,
        search: normalizedSearch,
        includeInactive,
      },
    ],

    queryFn: async (): Promise<PagedReceptions> => {
      if (normalizedSearch && !includeInactive) {
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
        includeInactive,
      });
    },

    placeholderData: keepPreviousData,
  });

  async function refreshReceptions() {
    await queryClient.invalidateQueries({
      queryKey: ["receptions"],
    });
  }

  async function refreshReceptionAndHistory(receptionId: string) {
    await Promise.all([
      refreshReceptions(),
      queryClient.invalidateQueries({
        queryKey: ["reception-history", receptionId],
      }),
      queryClient.invalidateQueries({
        queryKey: ["reception-lifecycle", receptionId],
      }),
    ]);
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

  const lifecycleMutation = useMutation({
    mutationFn: ({ id, mode, payload }: LifecycleActionVariables) => {
      switch (mode) {
        case "deactivate":
          return deactivateReception(id, payload);
        case "restore":
          return restoreReception(id, payload);
        case "request":
          return requestReceptionDeactivation(id, payload);
      }
    },
  });

  const correctionMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: Parameters<typeof createReceptionCorrection>[1];
    }) => createReceptionCorrection(id, payload),
  });

  const clarificationMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: Parameters<typeof createReceptionClarification>[1];
    }) => createReceptionClarification(id, payload),
  });

  const historyQuery = useQuery({
    queryKey: ["reception-history", historyReception?.id],
    queryFn: () => getReceptionHistory(historyReception!.id),
    enabled: historyReception !== null,
  });

  const lifecycleHistoryQuery = useQuery({
    queryKey: ["reception-lifecycle", lifecycleHistoryReception?.id],
    queryFn: () => getReceptionLifecycleHistory(lifecycleHistoryReception!.id),
    enabled: lifecycleHistoryReception !== null,
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
          kind: "normalUpdate",
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
      if (weightRangeChangeConfirmation.kind === "normalUpdate") {
        await updateMutation.mutateAsync({
          id: weightRangeChangeConfirmation.reception.id,
          payload: updateReceptionPayload(
            weightRangeChangeConfirmation.values,
            true,
          ),
        });
        setModalState(null);
      } else {
        await correctionMutation.mutateAsync({
          id: weightRangeChangeConfirmation.reception.id,
          payload: {
            ...weightRangeChangeConfirmation.draft,
            requestId: weightRangeChangeConfirmation.requestId,
            confirmWeightRangeChange: true,
            expectedCremationPriceId:
              weightRangeChangeConfirmation.details.newCremationPriceId,
            expectedNewPrice: weightRangeChangeConfirmation.details.newPrice,
          },
        });
        await refreshReceptionAndHistory(
          weightRangeChangeConfirmation.reception.id,
        );
        setCorrectionReception(null);
        setCorrectionAttempt(null);
        toast.success("Enmienda registrada correctamente.");
      }

      setWeightRangeChangeConfirmation(null);
    } catch (error) {
      const confirmation = getWeightRangeChangeConfirmation(error);

      if (confirmation && weightRangeChangeConfirmation.kind === "correction") {
        setWeightRangeChangeConfirmation({
          ...weightRangeChangeConfirmation,
          details: confirmation.weightChange,
        });
        return;
      }

      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible confirmar la corrección de peso.",
        ),
      );
    }
  }

  async function handleCorrectionSubmit(draft: ReceptionCorrectionDraft) {
    if (correctionReception === null) {
      return;
    }

    const fingerprint = getReceptionCorrectionFingerprint(draft);
    const requestId =
      correctionAttempt?.receptionId === correctionReception.id &&
      correctionAttempt.fingerprint === fingerprint
        ? correctionAttempt.requestId
        : crypto.randomUUID();

    setCorrectionAttempt({
      receptionId: correctionReception.id,
      requestId,
      fingerprint,
    });

    try {
      await correctionMutation.mutateAsync({
        id: correctionReception.id,
        payload: {
          ...draft,
          requestId,
          confirmWeightRangeChange: false,
        },
      });
      await refreshReceptionAndHistory(correctionReception.id);
      setCorrectionReception(null);
      setCorrectionAttempt(null);
      toast.success("Enmienda registrada correctamente.");
    } catch (error) {
      const confirmation = getWeightRangeChangeConfirmation(error);
      if (confirmation) {
        setWeightRangeChangeConfirmation({
          kind: "correction",
          reception: correctionReception,
          draft,
          requestId,
          fingerprint,
          details: confirmation.weightChange,
        });
        return;
      }

      toast.error(
        getApiErrorMessage(error, "No fue posible registrar la enmienda."),
      );
    }
  }

  async function handleClarificationSubmit(text: string) {
    if (clarificationReception === null) {
      return;
    }

    const requestId =
      clarificationAttempt?.receptionId === clarificationReception.id &&
      clarificationAttempt.text === text
        ? clarificationAttempt.requestId
        : crypto.randomUUID();

    setClarificationAttempt({
      receptionId: clarificationReception.id,
      requestId,
      text,
    });

    try {
      await clarificationMutation.mutateAsync({
        id: clarificationReception.id,
        payload: { requestId, text },
      });
      await refreshReceptionAndHistory(clarificationReception.id);
      setClarificationReception(null);
      setClarificationAttempt(null);
      toast.success("Aclaración agregada correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible agregar la aclaración."),
      );
    }
  }

  function openLifecycleAction(
    mode: ReceptionLifecycleActionMode,
    reception: Reception,
  ) {
    setLifecycleAttempt(null);
    setLifecycleError(null);
    setLifecycleAction({ mode, reception });
  }

  function closeLifecycleAction() {
    if (lifecycleMutation.isPending) {
      return;
    }

    setLifecycleAction(null);
    setLifecycleAttempt(null);
    setLifecycleError(null);
  }

  async function handleLifecycleSubmit(reason: string) {
    if (lifecycleAction === null) {
      return;
    }

    const normalizedReason = reason.trim();
    const requestId =
      lifecycleAttempt?.receptionId === lifecycleAction.reception.id &&
      lifecycleAttempt.mode === lifecycleAction.mode &&
      lifecycleAttempt.normalizedReason === normalizedReason
        ? lifecycleAttempt.requestId
        : crypto.randomUUID();

    setLifecycleAttempt({
      receptionId: lifecycleAction.reception.id,
      mode: lifecycleAction.mode,
      normalizedReason,
      requestId,
    });
    setLifecycleError(null);

    try {
      await lifecycleMutation.mutateAsync({
        id: lifecycleAction.reception.id,
        mode: lifecycleAction.mode,
        payload: { requestId, reason: normalizedReason },
      });

      await refreshReceptionAndHistory(lifecycleAction.reception.id);

      if (lifecycleAction.mode === "deactivate") {
        toast.success("La recepción fue desactivada.");
      } else if (lifecycleAction.mode === "restore") {
        toast.success("La recepción fue restaurada.");
      } else {
        toast.success(
          "Solicitud de desactivación registrada. La recepción permanece activa; el Propietario o un Administrador debe revisarla de forma independiente.",
        );
      }

      setLifecycleAction(null);
      setLifecycleAttempt(null);
      setLifecycleError(null);
    } catch (error) {
      const details = getLifecycleErrorDetails(error);

      if (details.eventRecorded) {
        await queryClient.invalidateQueries({
          queryKey: ["reception-lifecycle", lifecycleAction.reception.id],
        });
      }

      setLifecycleError(details);
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

          {canManage && (
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
          )}
        </div>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-1 flex-col gap-3 sm:flex-row">
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              disabled={includeInactive}
              placeholder={
                includeInactive
                  ? "La búsqueda está disponible en la vista de activas"
                  : "Buscar por QR, mascota, cliente o usuario..."
              }
              className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 sm:max-w-xl"
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

          {canApproveLifecycle && (
            <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                checked={includeInactive}
                onChange={(event) => {
                  const nextValue = event.target.checked;
                  setIncludeInactive(nextValue);
                  if (nextValue) {
                    setSearchInput("");
                    setDebouncedSearch("");
                  }
                }}
                className="h-4 w-4 rounded border-slate-300"
              />
              Incluir inactivas
            </label>
          )}

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
            includeInactive={includeInactive}
            pendingLifecycleReceptionId={
              lifecycleMutation.isPending
                ? (lifecycleMutation.variables?.id ?? null)
                : null
            }
            canManage={canManage}
            canAmend={canAmend}
            canApproveLifecycle={canApproveLifecycle}
            canRequestDeactivation={canRequestDeactivation}
            canViewLifecycleHistory={canViewLifecycleHistory}
            onCopyQrCode={handleCopyQrCode}
            onView={(reception) =>
              setModalState({
                mode: "view",
                reception,
              })
            }
            onEdit={(reception) =>
              setModalState({
                mode: "edit",
                reception,
              })
            }
            onCorrection={(reception) => {
              setCorrectionAttempt(null);
              setCorrectionReception(reception);
            }}
            onClarification={(reception) => {
              setClarificationAttempt(null);
              setClarificationReception(reception);
            }}
            onHistory={setHistoryReception}
            onLifecycleHistory={setLifecycleHistoryReception}
            onLifecycleAction={openLifecycleAction}
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

      {modalState?.mode === "view" ? (
        <ReceptionFormModal
          isOpen
          mode="view"
          reception={modalState.reception}
          onClose={() => setModalState(null)}
        />
      ) : (
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
      )}
      <ReceptionCorrectionModal
        isOpen={correctionReception !== null}
        reception={correctionReception}
        isSubmitting={correctionMutation.isPending}
        onClose={() => {
          if (!correctionMutation.isPending) {
            setCorrectionReception(null);
            setCorrectionAttempt(null);
          }
        }}
        onSubmit={handleCorrectionSubmit}
      />
      <ReceptionClarificationModal
        isOpen={clarificationReception !== null}
        reception={clarificationReception}
        isSubmitting={clarificationMutation.isPending}
        onClose={() => {
          if (!clarificationMutation.isPending) {
            setClarificationReception(null);
            setClarificationAttempt(null);
          }
        }}
        onSubmit={handleClarificationSubmit}
      />
      <ReceptionHistoryModal
        isOpen={historyReception !== null}
        reception={historyReception}
        history={historyQuery.data ?? []}
        isLoading={historyQuery.isLoading}
        isError={historyQuery.isError}
        onRetry={() => {
          void historyQuery.refetch();
        }}
        onClose={() => setHistoryReception(null)}
      />
      <ReceptionLifecycleHistoryModal
        isOpen={lifecycleHistoryReception !== null}
        reception={lifecycleHistoryReception}
        history={lifecycleHistoryQuery.data ?? []}
        isLoading={lifecycleHistoryQuery.isLoading}
        isError={lifecycleHistoryQuery.isError}
        onRetry={() => {
          void lifecycleHistoryQuery.refetch();
        }}
        onClose={() => setLifecycleHistoryReception(null)}
      />
      <ReceptionLifecycleActionModal
        isOpen={lifecycleAction !== null}
        mode={lifecycleAction?.mode ?? "deactivate"}
        reception={lifecycleAction?.reception ?? null}
        isSubmitting={lifecycleMutation.isPending}
        serverError={lifecycleError?.message ?? null}
        blockedDependencies={lifecycleError?.dependencies ?? []}
        onReasonChange={() => setLifecycleError(null)}
        onClose={closeLifecycleAction}
        onSubmit={handleLifecycleSubmit}
      />
      {weightRangeChangeConfirmation && (
        <WeightRangeChangeConfirmationModal
          petName={weightRangeChangeConfirmation.reception.petName}
          customerName={weightRangeChangeConfirmation.reception.customerName}
          qrCode={weightRangeChangeConfirmation.reception.qrCode}
          details={weightRangeChangeConfirmation.details}
          context={
            weightRangeChangeConfirmation.kind === "correction"
              ? "receptionCorrection"
              : undefined
          }
          isSubmitting={
            weightRangeChangeConfirmation.kind === "correction"
              ? correctionMutation.isPending
              : updateMutation.isPending
          }
          onCancel={() => setWeightRangeChangeConfirmation(null)}
          onConfirm={() => {
            void handleConfirmWeightRangeChange();
          }}
        />
      )}
    </section>
  );
}
