import type { Veterinarian } from "../types/veterinarian.types";
import { getVeterinarianFullName } from "../utils/veterinarianName";

interface VeterinariansTableProps {
  veterinarians: Veterinarian[];
  showingActive: boolean;
  pendingVeterinarianId: string | null;
  canManage: boolean;
  onEdit: (veterinarian: Veterinarian) => void;
  onDeactivate: (veterinarian: Veterinarian) => void;
  onRestore: (veterinarian: Veterinarian) => void;
}

function showOptional(value: string | null): string {
  return value?.trim() || "No registrado";
}

export function VeterinariansTable({
  veterinarians,
  showingActive,
  pendingVeterinarianId,
  canManage,
  onEdit,
  onDeactivate,
  onRestore,
}: VeterinariansTableProps) {
  if (veterinarians.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 font-semibold text-slate-600">
          MV
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron veterinarios
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          {showingActive
            ? "No hay veterinarios activos que coincidan con los filtros."
            : "No hay veterinarios inactivos que coincidan con los filtros."}
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Veterinario
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Veterinaria
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Cédula
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Contacto
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Estado
              </th>

              {canManage && (
                <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Acciones
                </th>
              )}
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {veterinarians.map((veterinarian) => {
              const isPending = pendingVeterinarianId === veterinarian.id;

              return (
                <tr
                  key={veterinarian.id}
                  className="transition hover:bg-slate-50"
                >
                  <td className="px-5 py-4">
                    <p className="font-medium text-slate-900">
                      Dr. {getVeterinarianFullName(veterinarian)}
                    </p>
                  </td>

                  <td className="px-5 py-4 text-sm text-slate-700">
                    {veterinarian.veterinaryClinicName?.trim() || "Sin asignar"}
                  </td>

                  <td className="px-5 py-4 text-sm text-slate-600">
                    {showOptional(veterinarian.professionalLicenseNumber)}
                  </td>

                  <td className="px-5 py-4">
                    <p className="text-sm text-slate-700">
                      {showOptional(veterinarian.phone)}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {showOptional(veterinarian.email)}
                    </p>
                  </td>

                  <td className="px-5 py-4">
                    <span
                      className={[
                        "inline-flex rounded-full px-2.5 py-1 text-xs font-semibold",
                        veterinarian.isActive
                          ? "bg-emerald-50 text-emerald-700"
                          : "bg-slate-100 text-slate-600",
                      ].join(" ")}
                    >
                      {veterinarian.isActive ? "Activo" : "Inactivo"}
                    </span>
                  </td>

                  {canManage && (
                    <td className="px-5 py-4 text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        {showingActive ? (
                          <>
                            <button
                              type="button"
                              onClick={() => onEdit(veterinarian)}
                              disabled={isPending}
                              className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
                            >
                              Editar
                            </button>

                            <button
                              type="button"
                              onClick={() => onDeactivate(veterinarian)}
                              disabled={isPending}
                              className="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:opacity-50"
                            >
                              {isPending ? "Procesando..." : "Desactivar"}
                            </button>
                          </>
                        ) : (
                          <button
                            type="button"
                            onClick={() => onRestore(veterinarian)}
                            disabled={isPending}
                            className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-50 disabled:opacity-50"
                          >
                            {isPending ? "Restaurando..." : "Restaurar"}
                          </button>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
