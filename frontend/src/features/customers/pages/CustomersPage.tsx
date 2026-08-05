import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import axios from 'axios'
import {
  useEffect,
  useMemo,
  useState,
} from 'react'
import toast from 'react-hot-toast'
import {
  createCustomer,
  deactivateCustomer,
  getCustomers,
  restoreCustomer,
  searchCustomers,
  updateCustomer,
} from '../api/customersApi'
import { CustomerFormModal } from '../components/CustomerFormModal'
import { CustomersTable } from '../components/CustomersTable'
import type { CustomerFormValues } from '../schemas/customerSchema'
import type {
  Customer,
  PaginatedCustomers,
} from '../types/customer.types'
import { CustomerPetsModal } from '../components/CustomerPetsModal'
import { getCustomerFullName } from '../utils/customerName'

type CustomerStatusFilter = 'active' | 'inactive'
type FormMode = 'create' | 'edit'

interface CustomerModalState {
  mode: FormMode
  customer: Customer | null
}

interface ApiErrorResponse {
  title?: string
  detail?: string
  message?: string
}

function getApiErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (!axios.isAxiosError(error)) {
    return fallback
  }

  if (!error.response) {
    return 'No fue posible conectarse con el servidor.'
  }

  const data = error.response.data as
    | ApiErrorResponse
    | string
    | undefined

  if (typeof data === 'string' && data.trim()) {
    return data
  }

  if (data && typeof data === 'object') {
    return (
      data.detail ??
      data.message ??
      data.title ??
      fallback
    )
  }

  return fallback
}

export function CustomersPage() {
  const queryClient = useQueryClient()

  const [statusFilter, setStatusFilter] =
    useState<CustomerStatusFilter>('active')

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] =
    useState('')

  const [modalState, setModalState] =
    useState<CustomerModalState | null>(null)

  const [
    selectedPetsCustomer,
    setSelectedPetsCustomer,
    ] = useState<Customer | null>(null)

  const isActive = statusFilter === 'active'
  const normalizedSearch = debouncedSearch.trim()

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput)
    }, 400)

    return () => {
      window.clearTimeout(timeoutId)
    }
  }, [searchInput])

  useEffect(() => {
    setPage(1)
  }, [statusFilter, normalizedSearch, pageSize])

  const customersQuery = useQuery({
    queryKey: [
      'customers',
      {
        page,
        pageSize,
        isActive,
        search: normalizedSearch,
      },
    ],

    queryFn: async (): Promise<PaginatedCustomers> => {
      if (normalizedSearch) {
        const items = await searchCustomers({
          term: normalizedSearch,
          isActive,
        })

        return {
          items,
          page: 1,
          pageSize: items.length,
          totalItems: items.length,
          totalPages: items.length > 0 ? 1 : 0,
        }
      }

      return getCustomers({
        page,
        pageSize,
        isActive,
      })
    },

    placeholderData: keepPreviousData,
  })

  useEffect(() => {
    const totalPages = customersQuery.data?.totalPages

    if (
      !normalizedSearch &&
      totalPages !== undefined &&
      totalPages > 0 &&
      page > totalPages
    ) {
      setPage(totalPages)
    }
  }, [
    customersQuery.data?.totalPages,
    normalizedSearch,
    page,
  ])

  async function refreshCustomers() {
    await queryClient.invalidateQueries({
      queryKey: ['customers'],
    })
  }

  const createMutation = useMutation({
    mutationFn: createCustomer,

    onSuccess: async () => {
      await refreshCustomers()
      toast.success('Cliente registrado correctamente.')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string
      values: CustomerFormValues
    }) => updateCustomer(id, values),

    onSuccess: async () => {
      await refreshCustomers()
      toast.success('Cliente actualizado correctamente.')
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateCustomer,

    onSuccess: async () => {
      await refreshCustomers()
      toast.success('Cliente desactivado correctamente.')
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(
          error,
          'No fue posible desactivar el cliente.',
        ),
      )
    },
  })

  const restoreMutation = useMutation({
    mutationFn: restoreCustomer,

    onSuccess: async () => {
      await refreshCustomers()
      toast.success('Cliente restaurado correctamente.')
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(
          error,
          'No fue posible restaurar el cliente.',
        ),
      )
    },
  })

  const isFormSubmitting =
    createMutation.isPending ||
    updateMutation.isPending

  const pendingCustomerId = useMemo(() => {
    if (deactivateMutation.isPending) {
      return deactivateMutation.variables
    }

    if (restoreMutation.isPending) {
      return restoreMutation.variables
    }

    return null
  }, [
    deactivateMutation.isPending,
    deactivateMutation.variables,
    restoreMutation.isPending,
    restoreMutation.variables,
  ])

  async function handleFormSubmit(
    values: CustomerFormValues,
  ) {
    try {
      if (modalState?.mode === 'edit') {
        if (!modalState.customer) {
          return
        }

        await updateMutation.mutateAsync({
          id: modalState.customer.id,
          values,
        })
      } else {
        await createMutation.mutateAsync(values)
      }

      setModalState(null)
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalState?.mode === 'edit'
            ? 'No fue posible actualizar el cliente.'
            : 'No fue posible registrar el cliente.',
        ),
      )
    }
  }

  function handleDeactivate(customer: Customer) {
    const confirmed = window.confirm(
      `¿Deseas desactivar a ${getCustomerFullName(customer)}?`,
    )

    if (confirmed) {
      deactivateMutation.mutate(customer.id)
    }
  }

  function handleRestore(customer: Customer) {
    const confirmed = window.confirm(
      `¿Deseas restaurar a ${getCustomerFullName(customer)}?`,
    )

    if (confirmed) {
      restoreMutation.mutate(customer.id)
    }
  }

  const customers = customersQuery.data?.items ?? []
  const totalItems =
    customersQuery.data?.totalItems ?? 0

  const totalPages = Math.max(
    customersQuery.data?.totalPages ?? 0,
    1,
  )

  return (
    <section>
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">
            Operación
          </p>

          <h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
            Clientes
          </h1>

          <p className="mt-3 max-w-2xl text-slate-600">
            Administra la información personal y de contacto de las familias.
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            setModalState({
              mode: 'create',
              customer: null,
            })
          }
          className="inline-flex items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
        >
          + Registrar cliente
        </button>
      </header>

      <div className="mt-8 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex rounded-xl bg-slate-100 p-1">
            <button
              type="button"
              onClick={() => setStatusFilter('active')}
              className={[
                'flex-1 rounded-lg px-4 py-2 text-sm font-medium transition sm:flex-none',
                statusFilter === 'active'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-500 hover:text-slate-900',
              ].join(' ')}
            >
              Activos
            </button>

            <button
              type="button"
              onClick={() => setStatusFilter('inactive')}
              className={[
                'flex-1 rounded-lg px-4 py-2 text-sm font-medium transition sm:flex-none',
                statusFilter === 'inactive'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-500 hover:text-slate-900',
              ].join(' ')}
            >
              Inactivos
            </button>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row">
            <input
              type="search"
              value={searchInput}
              onChange={(event) =>
                setSearchInput(event.target.value)
              }
              placeholder="Buscar por nombre, teléfono o correo"
              className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 sm:w-80"
            />

            {!normalizedSearch && (
              <select
                value={pageSize}
                onChange={(event) =>
                  setPageSize(Number(event.target.value))
                }
                className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none"
                aria-label="Clientes por página"
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
          {customersQuery.isFetching
            ? 'Actualizando información...'
            : `${totalItems} cliente${totalItems === 1 ? '' : 's'}`}
        </p>

        {normalizedSearch && (
          <button
            type="button"
            onClick={() => setSearchInput('')}
            className="text-sm font-medium text-slate-600 hover:text-slate-900"
          >
            Limpiar búsqueda
          </button>
        )}
      </div>

      <div className="mt-4">
        {customersQuery.isLoading ? (
          <div className="rounded-2xl border border-slate-200 bg-white px-6 py-14 text-center text-slate-500">
            Cargando clientes...
          </div>
        ) : customersQuery.isError ? (
          <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-8">
            <h2 className="font-semibold text-red-800">
              No fue posible cargar los clientes
            </h2>

            <p className="mt-2 text-sm text-red-700">
              Verifica que el backend esté funcionando e intenta nuevamente.
            </p>

            <button
              type="button"
              onClick={() => customersQuery.refetch()}
              className="mt-4 rounded-lg border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-700"
            >
              Reintentar
            </button>
          </div>
        ) : (
          <CustomersTable
            customers={customers}
            showingActive={isActive}
            pendingCustomerId={pendingCustomerId}
            onViewPets={setSelectedPetsCustomer}
            onEdit={(customer) =>
              setModalState({
                mode: 'edit',
                customer,
              })
            }
            onDeactivate={handleDeactivate}
            onRestore={handleRestore}
          />
        )}
      </div>

      {!normalizedSearch &&
        !customersQuery.isLoading &&
        !customersQuery.isError && (
          <footer className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-slate-500">
              Página {page} de {totalPages}
            </p>

            <div className="flex gap-2">
              <button
                type="button"
                onClick={() =>
                  setPage((current) =>
                    Math.max(current - 1, 1),
                  )
                }
                disabled={page <= 1}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Anterior
              </button>

              <button
                type="button"
                onClick={() =>
                  setPage((current) =>
                    Math.min(current + 1, totalPages),
                  )
                }
                disabled={page >= totalPages}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Siguiente
              </button>
            </div>
          </footer>
        )}

      <CustomerFormModal
        isOpen={modalState !== null}
        mode={modalState?.mode ?? 'create'}
        customer={modalState?.customer ?? null}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setModalState(null)
          }
        }}
        onSubmit={handleFormSubmit}
      />

      <CustomerPetsModal
        customer={selectedPetsCustomer}
        onClose={() => setSelectedPetsCustomer(null)}
      />
    </section>
  )
}