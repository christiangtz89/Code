import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";

import { hasPermission } from "../../auth/utils/permissions";

import {
  changeVeterinaryRequestStatus,
  convertVeterinaryRequest,
  createVeterinaryRequest,
  getVeterinaryRequestById,
  getVeterinaryRequests,
  searchVeterinaryRequests,
  updateVeterinaryRequest,
} from "../api/veterinaryRequestsApi";

import { VeterinaryRequestConversionModal } from "../components/VeterinaryRequestConversionModal";
import { VeterinaryRequestDetailsModal } from "../components/VeterinaryRequestDetailsModal";
import { VeterinaryRequestFormModal } from "../components/VeterinaryRequestFormModal";
import { VeterinaryRequestStatusModal } from "../components/VeterinaryRequestStatusModal";
import { VeterinaryRequestsTable } from "../components/VeterinaryRequestsTable";

import type { VeterinaryRequestConversionFormValues } from "../schemas/veterinaryRequestConversionSchema";
import type { VeterinaryRequestFormValues } from "../schemas/veterinaryRequestSchema";
import type { VeterinaryRequestStatusFormValues } from "../schemas/veterinaryRequestStatusSchema";

import {
  VeterinaryRequestStatus,
  type PaginatedVeterinaryRequests,
  type VeterinaryRequest,
  type VeterinaryRequestStatus as VeterinaryRequestStatusValue,
} from "../types/veterinaryRequest.types";

import {
  createVeterinaryRequestConversionPayload,
  createVeterinaryRequestPayload,
  createVeterinaryRequestStatusPayload,
  updateVeterinaryRequestPayload,
} from "../utils/veterinaryRequestForm";

type StatusFilter = "all" | VeterinaryRequestStatusValue;

interface ApiErrorResponse {
  title?: string;
  detail?: string;
  message?: string;
}

interface FormModalState {
  mode: "create" | "edit";
  request: VeterinaryRequest | null;
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

export function VeterinaryRequestsPage() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [formModalState, setFormModalState] = useState<FormModalState | null>(
    null,
  );

  const [statusRequest, setStatusRequest] = useState<VeterinaryRequest | null>(
    null,
  );

  const [conversionRequest, setConversionRequest] =
    useState<VeterinaryRequest | null>(null);

  const [detailsRequest, setDetailsRequest] =
    useState<VeterinaryRequest | null>(null);

  const normalizedSearch = debouncedSearch.trim();
  const searchPending = searchInput.trim() !== normalizedSearch;
  const canManageRequests = hasPermission("VeterinaryRequests.Manage");

  const selectedStatus = statusFilter === "all" ? undefined : statusFilter;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setPage(1);
      setDebouncedSearch(searchInput);
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchInput]);

  const requestsQuery = useQuery({
    queryKey: [
      "veterinary-requests",
      {
        page,
        pageSize,
        status: selectedStatus,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PaginatedVeterinaryRequests> => {
      if (normalizedSearch) {
        return searchVeterinaryRequests({
          search: normalizedSearch,
          page,
          pageSize,
          ...(selectedStatus !== undefined
            ? {
                status: selectedStatus,
              }
            : {}),
        });
      }

      return getVeterinaryRequests({
        page,
        pageSize,
        ...(selectedStatus !== undefined
          ? {
              status: selectedStatus,
            }
          : {}),
      });
    },
  });

  useEffect(() => {
    const totalPages = requestsQuery.data?.totalPages;

    if (totalPages && totalPages > 0 && page > totalPages) {
      setPage(totalPages);
    }
  }, [page, requestsQuery.data?.totalPages]);

  const detailsRequestId = detailsRequest?.id ?? "";

  const detailsQuery = useQuery({
    queryKey: ["veterinary-requests", "detail", detailsRequestId],

    queryFn: () => getVeterinaryRequestById(detailsRequestId),

    enabled: detailsRequestId.length > 0,
  });

  async function refreshRequests() {
    await queryClient.invalidateQueries({
      queryKey: ["veterinary-requests"],
    });
  }

  const createMutation = useMutation({
    mutationFn: (values: VeterinaryRequestFormValues) =>
      createVeterinaryRequest(createVeterinaryRequestPayload(values)),

    onSuccess: refreshRequests,
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string;
      values: VeterinaryRequestFormValues;
    }) => updateVeterinaryRequest(id, updateVeterinaryRequestPayload(values)),

    onSuccess: refreshRequests,
  });

  const statusMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string;
      values: VeterinaryRequestStatusFormValues;
    }) =>
      changeVeterinaryRequestStatus(
        id,
        createVeterinaryRequestStatusPayload(values),
      ),

    onSuccess: refreshRequests,
  });

  const conversionMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string;
      values: VeterinaryRequestConversionFormValues;
    }) =>
      convertVeterinaryRequest(
        id,
        createVeterinaryRequestConversionPayload(values),
      ),

    onSuccess: async () => {
      await Promise.all([
        refreshRequests(),

        queryClient.invalidateQueries({
          queryKey: ["customers"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["pets"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["receptions"],
        }),
      ]);
    },
  });

  async function handleCreate(values: VeterinaryRequestFormValues) {
    try {
      await createMutation.mutateAsync(values);

      setFormModalState(null);

      toast.success("Solicitud veterinaria registrada correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible registrar la solicitud veterinaria.",
        ),
      );
    }
  }

  async function handleUpdate(values: VeterinaryRequestFormValues) {
    const request = formModalState?.request;

    if (!request) {
      return;
    }

    try {
      await updateMutation.mutateAsync({
        id: request.id,
        values,
      });

      setFormModalState(null);

      toast.success("Solicitud veterinaria actualizada correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible actualizar la solicitud veterinaria.",
        ),
      );
    }
  }

  async function handleChangeStatus(values: VeterinaryRequestStatusFormValues) {
    if (!statusRequest) {
      return;
    }

    try {
      await statusMutation.mutateAsync({
        id: statusRequest.id,
        values,
      });

      setStatusRequest(null);

      toast.success("Estado actualizado correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible actualizar el estado."),
      );
    }
  }

  async function handleConvert(values: VeterinaryRequestConversionFormValues) {
    if (!conversionRequest) {
      return;
    }

    try {
      const converted = await conversionMutation.mutateAsync({
        id: conversionRequest.id,
        values,
      });

      setConversionRequest(null);

      toast.success(
        converted.receptionQrCode
          ? `Recepción creada. QR: ${converted.receptionQrCode}`
          : "Recepción creada correctamente.",
      );
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible crear la recepción."),
      );
    }
  }

  const requests = requestsQuery.data?.items ?? [];

  const totalItems = requestsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(requestsQuery.data?.totalPages ?? 0, 1);

  const displayedDetailsRequest = detailsQuery.data ?? detailsRequest;

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">
            Directorio veterinario
          </p>

          <h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
            Solicitudes veterinarias
          </h1>

          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Revisa solicitudes enviadas por veterinarias o veterinarios y
            conviértelas en recepciones PCMS.
          </p>
        </div>

        {canManageRequests && (
          <button
            type="button"
            onClick={() =>
              setFormModalState({
                mode: "create",
                request: null,
              })
            }
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            + Registrar solicitud
          </button>
        )}
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-col gap-3 sm:flex-row">
            <select
              value={statusFilter}
              onChange={(event) => {
                const value = event.target.value;

                if (value === "all") {
                  setPage(1);
                  setStatusFilter("all");
                  return;
                }

                setPage(1);
                setStatusFilter(Number(value) as VeterinaryRequestStatusValue);
              }}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
            >
              <option value="all">Todos los estados</option>

              <option value={VeterinaryRequestStatus.Submitted}>
                Recibida
              </option>

              <option value={VeterinaryRequestStatus.UnderReview}>
                En revisión
              </option>

              <option value={VeterinaryRequestStatus.Approved}>Aprobada</option>

              <option value={VeterinaryRequestStatus.Rejected}>
                Rechazada
              </option>

              <option value={VeterinaryRequestStatus.Converted}>
                Convertida a recepción
              </option>

              <option value={VeterinaryRequestStatus.Cancelled}>
                Cancelada
              </option>
            </select>

            {!normalizedSearch && (
              <select
                value={pageSize}
                onChange={(event) => {
                  setPage(1);
                  setPageSize(Number(event.target.value));
                }}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
              >
                <option value={10}>10 por página</option>

                <option value={20}>20 por página</option>

                <option value={50}>50 por página</option>
              </select>
            )}
          </div>

          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Buscar propietario, mascota, veterinaria..."
            className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm lg:w-96"
          />
        </div>

        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {requestsQuery.isFetching
              ? "Actualizando información..."
              : `${totalItems} ${
                  totalItems === 1 ? "solicitud" : "solicitudes"
                }`}
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

      {requestsQuery.isLoading || searchPending ? (
        <div className="rounded-2xl border border-slate-200 bg-white px-6 py-12 text-center text-sm text-slate-500">
          Cargando solicitudes...
        </div>
      ) : requestsQuery.isError ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5 text-sm text-red-700">
          No fue posible cargar las solicitudes veterinarias.
        </div>
      ) : (
        <VeterinaryRequestsTable
          requests={requests}
          canManage={canManageRequests}
          onView={setDetailsRequest}
          onEdit={(request) =>
            setFormModalState({
              mode: "edit",
              request,
            })
          }
          onChangeStatus={setStatusRequest}
          onConvert={setConversionRequest}
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
              disabled={page <= 1}
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium disabled:opacity-40"
            >
              Anterior
            </button>

            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() =>
                setPage((current) => Math.min(current + 1, totalPages))
              }
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        </div>
      )}

      {canManageRequests && (
        <VeterinaryRequestFormModal
          isOpen={formModalState !== null}
          mode={formModalState?.mode ?? "create"}
          request={formModalState?.request ?? null}
          isSubmitting={createMutation.isPending || updateMutation.isPending}
          onClose={() => setFormModalState(null)}
          onCreate={handleCreate}
          onUpdate={handleUpdate}
        />
      )}

      {canManageRequests && (
        <VeterinaryRequestStatusModal
          isOpen={statusRequest !== null}
          request={statusRequest}
          isSubmitting={statusMutation.isPending}
          onClose={() => setStatusRequest(null)}
          onSubmit={handleChangeStatus}
        />
      )}

      {canManageRequests && (
        <VeterinaryRequestConversionModal
          isOpen={conversionRequest !== null}
          request={conversionRequest}
          isSubmitting={conversionMutation.isPending}
          onClose={() => setConversionRequest(null)}
          onSubmit={handleConvert}
        />
      )}

      <VeterinaryRequestDetailsModal
        isOpen={detailsRequest !== null}
        request={displayedDetailsRequest}
        onClose={() => setDetailsRequest(null)}
      />
    </section>
  );
}
