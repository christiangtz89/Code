import type { Pet } from '../types/pet.types'

interface PetsTableProps {
  pets: Pet[]
  showingActive: boolean
  pendingPetId: string | null
  onEdit: (pet: Pet) => void
  onDeactivate: (pet: Pet) => void
  onRestore: (pet: Pet) => void
}

const dateFormatter = new Intl.DateTimeFormat(
  'es-MX',
  {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  },
)

function formatDateOnly(
  value: string,
): string {
  const datePart = value.slice(0, 10)
  const parts = datePart.split('-').map(Number)

  if (
    parts.length !== 3 ||
    parts.some(Number.isNaN)
  ) {
    return 'Fecha no disponible'
  }

  const [year, month, day] = parts

  return dateFormatter.format(
    new Date(year, month - 1, day),
  )
}

export function PetsTable({
  pets,
  showingActive,
  pendingPetId,
  onEdit,
  onDeactivate,
  onRestore,
}: PetsTableProps) {
  if (pets.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 font-semibold text-slate-600">
          MA
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron mascotas
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          {showingActive
            ? 'No hay mascotas activas que coincidan con los filtros.'
            : 'No hay mascotas inactivas que coincidan con los filtros.'}
        </p>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Mascota
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Propietario
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Características
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Fallecimiento
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Estado
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">
                Acciones
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {pets.map((pet) => {
              const isPending =
                pendingPetId === pet.id

              return (
                <tr
                  key={pet.id}
                  className="transition hover:bg-slate-50"
                >
                  <td className="whitespace-nowrap px-5 py-4">
                    <p className="font-medium text-slate-900">
                      {pet.name}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {pet.species} · {pet.breed}
                    </p>
                  </td>

                  <td className="whitespace-nowrap px-5 py-4">
                    <p className="text-sm font-medium text-slate-700">
                      {pet.customerName ||
                        'Propietario no disponible'}
                    </p>
                  </td>

                  <td className="px-5 py-4">
                    <p className="text-sm text-slate-700">
                      {pet.sex} · {pet.color}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {pet.weightKg.toFixed(2)} kg
                      {pet.ageYears !== null
                        ? ` · ${pet.ageYears} años`
                        : ''}
                    </p>
                  </td>

                  <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-600">
                    {formatDateOnly(
                      pet.dateOfDeath,
                    )}
                  </td>

                  <td className="whitespace-nowrap px-5 py-4">
                    <span
                      className={[
                        'inline-flex rounded-full px-2.5 py-1 text-xs font-semibold',
                        pet.isActive
                          ? 'bg-emerald-50 text-emerald-700'
                          : 'bg-slate-100 text-slate-600',
                      ].join(' ')}
                    >
                      {pet.isActive
                        ? 'Activa'
                        : 'Inactiva'}
                    </span>
                  </td>

                  <td className="whitespace-nowrap px-5 py-4 text-right">
                    <div className="flex justify-end gap-2">
                      {showingActive ? (
                        <>
                          <button
                            type="button"
                            disabled={isPending}
                            onClick={() => onEdit(pet)}
                            className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                          >
                            Editar
                          </button>

                          <button
                            type="button"
                            disabled={isPending}
                            onClick={() =>
                              onDeactivate(pet)
                            }
                            className="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-50"
                          >
                            {isPending
                              ? 'Procesando...'
                              : 'Desactivar'}
                          </button>
                        </>
                      ) : (
                        <button
                          type="button"
                          disabled={isPending}
                          onClick={() =>
                            onRestore(pet)
                          }
                          className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                        >
                          {isPending
                            ? 'Restaurando...'
                            : 'Restaurar'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}