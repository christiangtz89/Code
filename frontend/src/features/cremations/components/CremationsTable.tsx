import {
  getCremationStatusLabel,
  getCremationTypeLabel,
} from "../utils/cremationLabels";
import {
  formatCremationDateTime,
  getAssignedUserLabel,
  getCremationInclusions,
} from "../utils/cremationDisplay";
import { isTerminalCremationStatus } from "../utils/cremationStatus";
import { CremationStatus, type Cremation } from "../types/cremation.types";
import {
  formatCurrency,
  formatWeightRange,
} from "../../cremation-pricing/utils";

interface CremationsTableProps {
  cremations: Cremation[];
  showingActive: boolean;
  pendingCremationId: string | null;

  onEdit: (cremation: Cremation) => void;

  onChangeStatus: (cremation: Cremation) => void;

  onDeactivate: (cremation: Cremation) => void;

  onRestore: (cremation: Cremation) => void;
  onInventory: (cremation: Cremation) => void;
}

function getStatusClasses(cremation: Cremation): string {
  switch (cremation.status) {
    case CremationStatus.Pending:
      return "bg-amber-50 text-amber-700";

    case CremationStatus.Scheduled:
      return "bg-blue-50 text-blue-700";

    case CremationStatus.InProgress:
      return "bg-indigo-50 text-indigo-700";

    case CremationStatus.Cooling:
      return "bg-cyan-50 text-cyan-700";

    case CremationStatus.ProcessingRemains:
      return "bg-violet-50 text-violet-700";

    case CremationStatus.Completed:
      return "bg-emerald-50 text-emerald-700";

    case CremationStatus.ReadyForDelivery:
      return "bg-teal-50 text-teal-700";

    case CremationStatus.Delivered:
      return "bg-green-50 text-green-700";

    case CremationStatus.Cancelled:
      return "bg-red-50 text-red-700";

    default:
      return "bg-slate-100 text-slate-600";
  }
}

export function CremationsTable({
  cremations,
  showingActive,
  pendingCremationId,
  onEdit,
  onChangeStatus,
  onDeactivate,
  onRestore,
  onInventory,
}: CremationsTableProps) {
  if (cremations.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-sm font-semibold text-slate-600">
          CR
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron cremaciones
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          {showingActive
            ? "No hay cremaciones activas que coincidan con los filtros."
            : "No hay cremaciones inactivas que coincidan con los filtros."}
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
                Recepción
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Servicio
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Incluye
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Cotización
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Programación
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Asignado a
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
            {cremations.map((cremation) => {
              const isPending = pendingCremationId === cremation.id;

              const inclusions = getCremationInclusions(cremation);

              const terminalStatus = isTerminalCremationStatus(
                cremation.status,
              );

              return (
                <tr
                  key={cremation.id}
                  className="align-top transition hover:bg-slate-50"
                >
                  <td className="px-5 py-4">
                    <p className="font-medium text-slate-900">
                      {cremation.petName}
                    </p>

                    <p className="mt-1 text-sm text-slate-600">
                      {cremation.customerName}
                    </p>

                    <div className="mt-2">
                      <span className="inline-flex rounded-md bg-slate-100 px-2 py-1 font-mono text-xs text-slate-600">
                        {cremation.qrCode}
                      </span>
                    </div>
                  </td>

                  <td className="px-5 py-4">
                    <p className="font-medium text-slate-800">
                      {cremation.packageName}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {getCremationTypeLabel(cremation.cremationType)}
                    </p>
                  </td>

                  <td className="px-5 py-4">
                    {inclusions.length > 0 ? (
                      <div className="flex max-w-52 flex-wrap gap-1.5">
                        {inclusions.map((item) => (
                          <span
                            key={item}
                            className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600"
                          >
                            {item}
                          </span>
                        ))}
                      </div>
                    ) : (
                      <span className="text-sm text-slate-400">
                        Sin adicionales
                      </span>
                    )}

                    {cremation.includesUrn && cremation.urnDescription && (
                      <p className="mt-2 max-w-48 text-xs text-slate-500">
                        {cremation.urnDescription}
                      </p>
                    )}
                  </td>

                  <td className="min-w-56 px-5 py-4">
                    {cremation.quotedPrice !== null &&
                    cremation.quotedWeightKg !== null &&
                    cremation.quotedMinimumWeightKg !== null &&
                    cremation.quotedMaximumWeightKg !== null ? (
                      <div className="space-y-1 text-sm">
                        <div className="flex justify-between gap-4">
                          <span className="text-slate-500">Precio</span>

                          <span className="font-semibold text-slate-900">
                            {formatCurrency(cremation.quotedPrice)}
                          </span>
                        </div>

                        <div className="flex justify-between gap-4">
                          <span className="text-slate-500">Peso utilizado</span>

                          <span className="font-medium text-slate-700">
                            {cremation.quotedWeightKg.toFixed(2)} kg
                          </span>
                        </div>

                        <div className="flex justify-between gap-4">
                          <span className="text-slate-500">Rango aplicado</span>

                          <span className="font-medium text-slate-700">
                            {formatWeightRange(
                              cremation.quotedMinimumWeightKg,
                              cremation.quotedMaximumWeightKg,
                            )}
                          </span>
                        </div>
                      </div>
                    ) : (
                      <p className="text-sm text-slate-500">
                        Cotización histórica no disponible.
                      </p>
                    )}
                  </td>

                  <td className="px-5 py-4 text-sm text-slate-600">
                    {formatCremationDateTime(cremation.scheduledAt)}
                  </td>

                  <td className="px-5 py-4 text-sm text-slate-700">
                    {getAssignedUserLabel(cremation)}
                  </td>

                  <td className="px-5 py-4">
                    <span
                      className={[
                        "inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold",
                        getStatusClasses(cremation),
                      ].join(" ")}
                    >
                      {getCremationStatusLabel(cremation.status)}
                    </span>
                  </td>

                  <td className="px-5 py-4 text-right">
                    <div className="flex min-w-40 flex-wrap justify-end gap-2">
                      {showingActive ? (
                        <>
                          <button
                            type="button"
                            onClick={() => onEdit(cremation)}
                            disabled={isPending}
                            className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            Editar
                          </button>
                          <button type="button" onClick={() => onInventory(cremation)} className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-50">Inventario</button>

                          {!terminalStatus && (
                            <button
                              type="button"
                              onClick={() => onChangeStatus(cremation)}
                              disabled={isPending}
                              className="rounded-lg border border-blue-200 px-3 py-2 text-sm font-medium text-blue-700 transition hover:bg-blue-50 disabled:cursor-not-allowed disabled:opacity-50"
                            >
                              Cambiar estado
                            </button>
                          )}

                          <button
                            type="button"
                            onClick={() => onDeactivate(cremation)}
                            disabled={isPending}
                            className="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            {isPending ? "Procesando..." : "Desactivar"}
                          </button>
                        </>
                      ) : (
                        <button
                          type="button"
                          onClick={() => onRestore(cremation)}
                          disabled={isPending}
                          className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isPending ? "Restaurando..." : "Restaurar"}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
