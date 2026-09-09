import type { VeterinaryClinic } from "../types/veterinaryClinic.types";

interface VeterinaryClinicsTableProps {
  clinics: VeterinaryClinic[];
  showingActive: boolean;
  pendingClinicId: string | null;
  canManage: boolean;
  canViewVeterinarians: boolean;
  onViewVeterinarians: (clinic: VeterinaryClinic) => void;
  onEdit: (clinic: VeterinaryClinic) => void;
  onDeactivate: (clinic: VeterinaryClinic) => void;
  onRestore: (clinic: VeterinaryClinic) => void;
}

const dateFormatter = new Intl.DateTimeFormat("es-MX", {
  day: "2-digit",
  month: "short",
  year: "numeric",
});

function formatDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no disponible";
  }

  return dateFormatter.format(date);
}

function showOptional(value: string | null): string {
  return value?.trim() || "No registrado";
}

export function VeterinaryClinicsTable({
  clinics,
  showingActive,
  pendingClinicId,
  canManage,
  canViewVeterinarians,
  onViewVeterinarians,
  onEdit,
  onDeactivate,
  onRestore,
}: VeterinaryClinicsTableProps) {
  if (clinics.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 font-semibold text-slate-600">
          VT
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron veterinarias
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          {showingActive
            ? "No hay veterinarias activas que coincidan con los filtros."
            : "No hay veterinarias inactivas que coincidan con los filtros."}
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
                Veterinaria
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Contacto
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Dirección
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Registro
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Estado
              </th>

              {(canManage || canViewVeterinarians) && (
                <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Acciones
                </th>
              )}
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {clinics.map((clinic) => {
              const isPending = pendingClinicId === clinic.id;

              return (
                <tr key={clinic.id} className="transition hover:bg-slate-50">
                  <td className="px-5 py-4">
                    <p className="font-medium text-slate-900">{clinic.name}</p>

                    <p className="mt-1 text-sm text-slate-500">
                      Contacto: {showOptional(clinic.primaryContactName)}
                    </p>
                  </td>

                  <td className="px-5 py-4">
                    <p className="text-sm text-slate-700">
                      {showOptional(clinic.phone)}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {showOptional(clinic.email)}
                    </p>
                  </td>

                  <td className="max-w-xs px-5 py-4 text-sm text-slate-600">
                    {showOptional(clinic.address)}
                  </td>

                  <td className="whitespace-nowrap px-5 py-4 text-sm text-slate-600">
                    {formatDate(clinic.createdAt)}
                  </td>

                  <td className="whitespace-nowrap px-5 py-4">
                    <span
                      className={[
                        "inline-flex rounded-full px-2.5 py-1 text-xs font-semibold",
                        clinic.isActive
                          ? "bg-emerald-50 text-emerald-700"
                          : "bg-slate-100 text-slate-600",
                      ].join(" ")}
                    >
                      {clinic.isActive ? "Activa" : "Inactiva"}
                    </span>
                  </td>

                  {(canManage || canViewVeterinarians) && (
                    <td className="px-5 py-4 text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        {canViewVeterinarians && (
                          <button
                            type="button"
                            onClick={() => onViewVeterinarians(clinic)}
                            disabled={isPending}
                            className="rounded-lg border border-sky-200 px-3 py-2 text-sm font-medium text-sky-700 transition hover:bg-sky-50 disabled:opacity-50"
                          >
                            Ver veterinarios
                          </button>
                        )}

                        {canManage &&
                          (showingActive ? (
                            <>
                              <button
                                type="button"
                                onClick={() => onEdit(clinic)}
                                disabled={isPending}
                                className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
                              >
                                Editar
                              </button>

                              <button
                                type="button"
                                onClick={() => onDeactivate(clinic)}
                                disabled={isPending}
                                className="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:opacity-50"
                              >
                                {isPending ? "Procesando..." : "Desactivar"}
                              </button>
                            </>
                          ) : (
                            <button
                              type="button"
                              onClick={() => onRestore(clinic)}
                              disabled={isPending}
                              className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-50 disabled:opacity-50"
                            >
                              {isPending ? "Restaurando..." : "Restaurar"}
                            </button>
                          ))}
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
