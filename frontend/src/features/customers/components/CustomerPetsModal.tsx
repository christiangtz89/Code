import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getCustomerPets } from '../api/customersApi'
import type { Customer } from '../types/customer.types'
import { getCustomerFullName } from '../utils/customerName'

interface CustomerPetsModalProps {
  customer: Customer | null
  onClose: () => void
}

export function CustomerPetsModal({
  customer,
  onClose,
}: CustomerPetsModalProps) {
  const petsQuery = useQuery({
    queryKey: [
      'customer-pets',
      customer?.id ?? null,
    ],
    queryFn: () =>
      getCustomerPets(customer?.id ?? ''),
    enabled: Boolean(customer),
  })

  if (!customer) {
    return null
  }

  const pets = petsQuery.data ?? []

  const activePets = pets.filter(
    (pet) => pet.isActive,
  ).length

  const inactivePets = pets.length - activePets

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={onClose}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="customer-pets-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) =>
          event.stopPropagation()
        }
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Mascotas asociadas
            </p>

            <h2
              id="customer-pets-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {getCustomerFullName(customer)}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {customer.phone} · {customer.email}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar mascotas asociadas"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900"
          >
            ×
          </button>
        </header>

        <div className="px-6 py-6">
          <div className="mb-6 grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-slate-200 p-4">
              <p className="text-sm text-slate-500">
                Total
              </p>

              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {pets.length}
              </p>
            </div>

            <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
              <p className="text-sm text-emerald-700">
                Activas
              </p>

              <p className="mt-1 text-2xl font-semibold text-emerald-800">
                {activePets}
              </p>
            </div>

            <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-sm text-slate-600">
                Inactivas
              </p>

              <p className="mt-1 text-2xl font-semibold text-slate-800">
                {inactivePets}
              </p>
            </div>
          </div>

          {petsQuery.isLoading && (
            <div className="rounded-xl border border-slate-200 px-5 py-10 text-center text-sm text-slate-500">
              Cargando mascotas...
            </div>
          )}

          {petsQuery.isError && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700">
              No fue posible cargar las mascotas
              asociadas.
            </div>
          )}

          {!petsQuery.isLoading &&
            !petsQuery.isError &&
            pets.length === 0 && (
              <div className="rounded-xl border border-dashed border-slate-300 px-5 py-10 text-center">
                <p className="font-medium text-slate-900">
                  Este cliente no tiene mascotas
                  registradas.
                </p>

                <p className="mt-2 text-sm text-slate-500">
                  Puedes registrar una mascota desde
                  el módulo de Mascotas.
                </p>
              </div>
            )}

          {!petsQuery.isLoading &&
            !petsQuery.isError &&
            pets.length > 0 && (
              <div className="space-y-3">
                {pets.map((pet) => (
                  <article
                    key={pet.id}
                    className="flex flex-col gap-4 rounded-xl border border-slate-200 p-4 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-semibold text-slate-900">
                          {pet.name}
                        </h3>

                        <span
                          className={[
                            'rounded-full px-2.5 py-1 text-xs font-semibold',
                            pet.isActive
                              ? 'bg-emerald-50 text-emerald-700'
                              : 'bg-slate-100 text-slate-600',
                          ].join(' ')}
                        >
                          {pet.isActive
                            ? 'Activa'
                            : 'Inactiva'}
                        </span>
                      </div>

                      <p className="mt-2 text-sm text-slate-600">
                        {pet.species} · {pet.breed}
                      </p>

                      <p className="mt-1 text-sm text-slate-500">
                        {pet.sex} · {pet.color} ·{' '}
                        {pet.weightKg} kg
                      </p>
                    </div>

                    <div className="text-sm text-slate-500">
                      {pet.ageYears !== null
                        ? `${pet.ageYears} años`
                        : 'Edad no registrada'}
                    </div>
                  </article>
                ))}
              </div>
            )}
        </div>

        <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 px-6 py-5 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          >
            Cerrar
          </button>

          <Link
            to="/pets"
            onClick={onClose}
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-center text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            Abrir módulo Mascotas
          </Link>
        </footer>
      </section>
    </div>
  )
}