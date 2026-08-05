import { useQuery } from '@tanstack/react-query'
import { getClinicVeterinarians } from '../api/veterinaryClinicsApi'
import type { VeterinaryClinic } from '../types/veterinaryClinic.types'

interface ClinicVeterinariansModalProps {
  clinic: VeterinaryClinic | null
  onClose: () => void
}

function showOptional(
  value: string | null,
): string {
  return value?.trim() || 'No registrado'
}

export function ClinicVeterinariansModal({
  clinic,
  onClose,
}: ClinicVeterinariansModalProps) {
  const veterinariansQuery = useQuery({
    queryKey: [
      'clinic-veterinarians',
      clinic?.id ?? null,
    ],

    queryFn: () =>
      getClinicVeterinarians(clinic?.id ?? ''),

    enabled: Boolean(clinic),
  })

  if (!clinic) {
    return null
  }

  const veterinarians =
    veterinariansQuery.data ?? []

  const activeVeterinarians =
    veterinarians.filter(
      (veterinarian) =>
        veterinarian.isActive,
    ).length

  const inactiveVeterinarians =
    veterinarians.length -
    activeVeterinarians

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={onClose}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="clinic-veterinarians-title"
        className="max-h-full w-full max-w-4xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) =>
          event.stopPropagation()
        }
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Veterinarios asociados
            </p>

            <h2
              id="clinic-veterinarians-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {clinic.name}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {showOptional(clinic.phone)}
              {' · '}
              {showOptional(clinic.email)}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar veterinarios asociados"
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
                {veterinarians.length}
              </p>
            </div>

            <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
              <p className="text-sm text-emerald-700">
                Activos
              </p>

              <p className="mt-1 text-2xl font-semibold text-emerald-800">
                {activeVeterinarians}
              </p>
            </div>

            <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-sm text-slate-600">
                Inactivos
              </p>

              <p className="mt-1 text-2xl font-semibold text-slate-800">
                {inactiveVeterinarians}
              </p>
            </div>
          </div>

          {veterinariansQuery.isLoading && (
            <div className="rounded-xl border border-slate-200 px-5 py-10 text-center text-sm text-slate-500">
              Cargando veterinarios...
            </div>
          )}

          {veterinariansQuery.isError && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700">
              No fue posible cargar los
              veterinarios asociados.
            </div>
          )}

          {!veterinariansQuery.isLoading &&
            !veterinariansQuery.isError &&
            veterinarians.length === 0 && (
              <div className="rounded-xl border border-dashed border-slate-300 px-5 py-10 text-center">
                <p className="font-medium text-slate-900">
                  Esta veterinaria no tiene
                  veterinarios registrados.
                </p>

                <p className="mt-2 text-sm text-slate-500">
                  Los veterinarios se podrán
                  registrar en la siguiente parte
                  de esta fase.
                </p>
              </div>
            )}

          {!veterinariansQuery.isLoading &&
            !veterinariansQuery.isError &&
            veterinarians.length > 0 && (
              <div className="space-y-3">
                {veterinarians.map(
                  (veterinarian) => (
                    <article
                      key={veterinarian.id}
                      className="flex flex-col gap-4 rounded-xl border border-slate-200 p-4 sm:flex-row sm:items-center sm:justify-between"
                    >
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className="font-semibold text-slate-900">
                            Dr.{' '}
                            {
                              veterinarian.firstName
                            }{' '}
                            {
                              veterinarian.lastName
                            }
                          </h3>

                          <span
                            className={[
                              'rounded-full px-2.5 py-1 text-xs font-semibold',
                              veterinarian.isActive
                                ? 'bg-emerald-50 text-emerald-700'
                                : 'bg-slate-100 text-slate-600',
                            ].join(' ')}
                          >
                            {veterinarian.isActive
                              ? 'Activo'
                              : 'Inactivo'}
                          </span>
                        </div>

                        <p className="mt-2 text-sm text-slate-600">
                          Cédula profesional:{' '}
                          {showOptional(
                            veterinarian.professionalLicenseNumber,
                          )}
                        </p>

                        <p className="mt-1 text-sm text-slate-500">
                          {showOptional(
                            veterinarian.phone,
                          )}
                          {' · '}
                          {showOptional(
                            veterinarian.email,
                          )}
                        </p>
                      </div>
                    </article>
                  ),
                )}
              </div>
            )}
        </div>

        <footer className="flex justify-end border-t border-slate-200 px-6 py-5">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          >
            Cerrar
          </button>
        </footer>
      </section>
    </div>
  )
}