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
  addPayment,
  createPaymentAccount,
  getAvailablePaymentCremations,
  getPaymentAccountById,
  getPaymentAccounts,
  searchPaymentAccounts,
  updatePaymentAccount,
} from "../api/paymentsApi";
import { PaymentAccountFormModal } from "../components/PaymentAccountFormModal";
import { PaymentDetailsModal } from "../components/PaymentDetailsModal";
import { PaymentFormModal } from "../components/PaymentFormModal";
import { PaymentsTable } from "../components/PaymentsTable";
import type {
  CreatePaymentAccountFormValues,
  UpdatePaymentAccountFormValues,
} from "../schemas/paymentAccountSchema";
import type { PaymentFormValues } from "../schemas/paymentSchema";
import {
  PaymentStatus,
  type PaymentAccount,
  type PaymentStatus as PaymentStatusValue,
  type PaginatedPaymentAccounts,
} from "../types/payment.types";
import {
  createPaymentAccountPayload,
  createPaymentPayload,
  updatePaymentAccountPayload,
} from "../utils/paymentForm";

type PaymentStatusFilter = "all" | PaymentStatusValue;

type PaymentAccountFormMode = "create" | "edit";

interface PaymentAccountModalState {
  mode: PaymentAccountFormMode;
  account: PaymentAccount | null;
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

export function PaymentsPage() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [statusFilter, setStatusFilter] = useState<PaymentStatusFilter>("all");

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [accountModalState, setAccountModalState] =
    useState<PaymentAccountModalState | null>(null);

  const [paymentAccount, setPaymentAccount] = useState<PaymentAccount | null>(
    null,
  );

  const [detailsAccount, setDetailsAccount] = useState<PaymentAccount | null>(
    null,
  );

  const normalizedSearch = debouncedSearch.trim();

  const selectedStatus = statusFilter === "all" ? undefined : statusFilter;

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
  }, [normalizedSearch, pageSize, statusFilter]);

  const accountsQuery = useQuery({
    queryKey: [
      "payments",
      "accounts",
      {
        page,
        pageSize,
        status: selectedStatus,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PaginatedPaymentAccounts> => {
      if (normalizedSearch) {
        const items = await searchPaymentAccounts({
          search: normalizedSearch,
          ...(selectedStatus !== undefined
            ? {
                status: selectedStatus,
              }
            : {}),
        });

        return {
          items,
          page: 1,
          pageSize: items.length,
          totalItems: items.length,
          totalPages: items.length > 0 ? 1 : 0,
        };
      }

      return getPaymentAccounts({
        page,
        pageSize,
        ...(selectedStatus !== undefined
          ? {
              status: selectedStatus,
            }
          : {}),
      });
    },

    placeholderData: keepPreviousData,
  });

  useEffect(() => {
    const totalPages = accountsQuery.data?.totalPages;

    if (
      !normalizedSearch &&
      totalPages &&
      totalPages > 0 &&
      page > totalPages
    ) {
      setPage(totalPages);
    }
  }, [accountsQuery.data?.totalPages, normalizedSearch, page]);

  const availableCremationsQuery = useQuery({
    queryKey: ["payments", "options", "cremations"],

    queryFn: getAvailablePaymentCremations,

    enabled: accountModalState?.mode === "create",
  });

  const detailsAccountId = detailsAccount?.id ?? "";

  const detailsAccountQuery = useQuery({
    queryKey: ["payments", "account", detailsAccountId],

    queryFn: () => getPaymentAccountById(detailsAccountId),

    enabled: detailsAccountId.length > 0,
  });

  async function refreshPayments() {
    await queryClient.invalidateQueries({
      queryKey: ["payments"],
    });
  }

  const createAccountMutation = useMutation({
    mutationFn: (values: CreatePaymentAccountFormValues) =>
      createPaymentAccount(createPaymentAccountPayload(values)),

    onSuccess: refreshPayments,
  });

  const updateAccountMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string;
      values: UpdatePaymentAccountFormValues;
    }) => updatePaymentAccount(id, updatePaymentAccountPayload(values)),

    onSuccess: refreshPayments,
  });

  const addPaymentMutation = useMutation({
    mutationFn: ({
      accountId,
      values,
    }: {
      accountId: string;
      values: PaymentFormValues;
    }) => addPayment(accountId, createPaymentPayload(values)),

    onSuccess: refreshPayments,
  });

  const accountFormIsSubmitting =
    createAccountMutation.isPending || updateAccountMutation.isPending;

  async function handleCreateAccount(values: CreatePaymentAccountFormValues) {
    try {
      await createAccountMutation.mutateAsync(values);

      setAccountModalState(null);

      toast.success("Cuenta de pago registrada correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible registrar la cuenta de pago.",
        ),
      );
    }
  }

  async function handleUpdateAccount(values: UpdatePaymentAccountFormValues) {
    const account = accountModalState?.account;

    if (!account) {
      return;
    }

    try {
      await updateAccountMutation.mutateAsync({
        id: account.id,
        values,
      });

      setAccountModalState(null);

      toast.success("Total del servicio actualizado correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          "No fue posible actualizar el total del servicio.",
        ),
      );
    }
  }

  async function handleAddPayment(values: PaymentFormValues) {
    if (!paymentAccount) {
      return;
    }

    try {
      await addPaymentMutation.mutateAsync({
        accountId: paymentAccount.id,
        values,
      });

      setPaymentAccount(null);

      toast.success("Pago registrado correctamente.");
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible registrar el pago."),
      );
    }
  }

  const accounts = accountsQuery.data?.items ?? [];

  const totalItems = accountsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(accountsQuery.data?.totalPages ?? 0, 1);

  const displayedDetailsAccount = detailsAccountQuery.data ?? detailsAccount;

  return (
    <section className="space-y-6">
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Finanzas</p>

          <h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
            Pagos
          </h1>

          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Administra anticipos, pagos parciales, saldos e historial financiero
            de los servicios.
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            setAccountModalState({
              mode: "create",
              account: null,
            })
          }
          className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
        >
          + Registrar cuenta de pago
        </button>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div>
              <label htmlFor="payment-status-filter" className="sr-only">
                Estado de pago
              </label>

              <select
                id="payment-status-filter"
                value={statusFilter}
                onChange={(event) => {
                  const value = event.target.value;

                  if (value === "all") {
                    setStatusFilter("all");
                    return;
                  }

                  setStatusFilter(Number(value) as PaymentStatusValue);
                }}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:w-auto"
              >
                <option value="all">Todos los estados</option>

                <option value={PaymentStatus.Pending}>Pendiente</option>

                <option value={PaymentStatus.PartiallyPaid}>
                  Pago parcial
                </option>

                <option value={PaymentStatus.Paid}>Pagado</option>
              </select>
            </div>

            {!normalizedSearch && (
              <select
                value={pageSize}
                onChange={(event) => setPageSize(Number(event.target.value))}
                aria-label="Cuentas por página"
                className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
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
            placeholder="Buscar QR, mascota, cliente o paquete..."
            className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 lg:w-96"
          />
        </div>

        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {accountsQuery.isFetching
              ? "Actualizando información..."
              : `${totalItems} ${totalItems === 1 ? "cuenta" : "cuentas"}`}
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

      {accountsQuery.isLoading ? (
        <div className="rounded-2xl border border-slate-200 bg-white px-6 py-12 text-center text-sm text-slate-500 shadow-sm">
          Cargando cuentas de pago...
        </div>
      ) : accountsQuery.isError ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5 text-sm text-red-700">
          No fue posible cargar las cuentas de pago. Verifica que el backend
          esté funcionando.
        </div>
      ) : (
        <PaymentsTable
          accounts={accounts}
          onViewPayments={setDetailsAccount}
          onAddPayment={setPaymentAccount}
          onEditTotal={(account) =>
            setAccountModalState({
              mode: "edit",
              account,
            })
          }
        />
      )}

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

      <PaymentAccountFormModal
        isOpen={accountModalState !== null}
        mode={accountModalState?.mode ?? "create"}
        account={accountModalState?.account ?? null}
        cremations={availableCremationsQuery.data ?? []}
        isLoadingCremations={availableCremationsQuery.isLoading}
        isSubmitting={accountFormIsSubmitting}
        onClose={() => {
          if (!accountFormIsSubmitting) {
            setAccountModalState(null);
          }
        }}
        onCreate={handleCreateAccount}
        onUpdate={handleUpdateAccount}
      />

      <PaymentFormModal
        isOpen={paymentAccount !== null}
        account={paymentAccount}
        isSubmitting={addPaymentMutation.isPending}
        onClose={() => {
          if (!addPaymentMutation.isPending) {
            setPaymentAccount(null);
          }
        }}
        onSubmit={handleAddPayment}
      />

      <PaymentDetailsModal
        isOpen={detailsAccount !== null}
        account={displayedDetailsAccount}
        onClose={() => setDetailsAccount(null)}
      />
    </section>
  );
}
