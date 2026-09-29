import { getCremationStatusLabel } from "../../cremations/utils/cremationLabels";
import type { ReceptionLifecycleActionMode } from "./ReceptionLifecycleActionModal";
import type { Reception } from "../types/reception.types";
import {
  formatReceptionDate,
  formatReceptionWeight,
  showOptionalReceptionValue,
} from "../utils/receptionFormatters";

interface ReceptionsTableProps {
  receptions: Reception[];
  includeInactive: boolean;
  pendingLifecycleReceptionId: string | null;
  canManage: boolean;
  canAmend: boolean;
  canApproveLifecycle: boolean;
  canRequestDeactivation: boolean;
  canViewLifecycleHistory: boolean;
  onCopyQrCode: (reception: Reception) => void;
  onEdit: (reception: Reception) => void;
  onCorrection: (reception: Reception) => void;
  onClarification: (reception: Reception) => void;
  onHistory: (reception: Reception) => void;
  onLifecycleHistory: (reception: Reception) => void;
  onLifecycleAction: (
    mode: ReceptionLifecycleActionMode,
    reception: Reception,
  ) => void;
}

export function ReceptionsTable({
  receptions,
  includeInactive,
  pendingLifecycleReceptionId,
  canManage,
  canAmend,
  canApproveLifecycle,
  canRequestDeactivation,
  canViewLifecycleHistory,
  onCopyQrCode,
  onEdit,
  onCorrection,
  onClarification,
  onHistory,
  onLifecycleHistory,
  onLifecycleAction,
}: ReceptionsTableProps) {
  if (receptions.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 font-semibold text-slate-600">
          RC
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron recepciones
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          {includeInactive
            ? "No hay recepciones que coincidan con esta vista."
            : "No hay recepciones activas que coincidan con la búsqueda."}
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
                Código QR
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Peso
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Referencia
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Objetos personales
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Recibió
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">
                Acciones
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {receptions.map((reception) => {
              const isLifecyclePending =
                pendingLifecycleReceptionId === reception.id;

              return (
                <tr
                  key={reception.id}
                  className={[
                    "align-top transition hover:bg-slate-50",
                    reception.isActive ? "" : "bg-slate-50/70",
                  ].join(" ")}
                >
                  <td className="px-5 py-4">
                    <p className="font-medium text-slate-900">
                      {reception.petName}
                    </p>

                    <p className="mt-1 text-sm text-slate-600">
                      {reception.customerName}
                    </p>

                    <p className="mt-1 whitespace-nowrap text-xs text-slate-500">
                      {formatReceptionDate(reception.receivedAt)}
                    </p>

                    {!reception.isActive && (
                      <span className="mt-2 inline-flex rounded-full bg-slate-200 px-2.5 py-1 text-xs font-semibold text-slate-700">
                        Inactiva
                      </span>
                    )}

                    {reception.isNormalEditLocked && (
                      <div className="mt-2">
                        <span className="inline-flex rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
                          Edición normal bloqueada
                        </span>
                        {reception.cremationStatus !== null && (
                          <p className="mt-1 text-xs text-slate-500">
                            Cremación:{" "}
                            {getCremationStatusLabel(reception.cremationStatus)}
                          </p>
                        )}
                      </div>
                    )}
                  </td>

                  <td className="px-5 py-4">
                    <button
                      type="button"
                      onClick={() => onCopyQrCode(reception)}
                      title="Copiar código QR"
                      className="max-w-56 break-all rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-left font-mono text-xs font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-100"
                    >
                      {reception.qrCode}
                    </button>
                  </td>

                  <td className="whitespace-nowrap px-5 py-4">
                    <p className="font-medium text-slate-900">
                      {formatReceptionWeight(reception.verifiedWeightKg)} kg
                    </p>

                    <p className="mt-1 text-xs text-slate-500">
                      Peso operativo
                    </p>

                    {reception.latestReportedCorrectedWeightKg !== null &&
                      reception.latestReportedCorrectedWeightKg !==
                        reception.verifiedWeightKg && (
                        <div className="mt-3 border-t border-slate-200 pt-2">
                          <p className="font-medium text-amber-800">
                            {formatReceptionWeight(
                              reception.latestReportedCorrectedWeightKg,
                            )}{" "}
                            kg
                          </p>
                          <p className="mt-1 text-xs text-amber-700">
                            Peso corregido reportado
                          </p>
                        </div>
                      )}
                  </td>

                  <td className="px-5 py-4">
                    <p className="text-sm font-medium text-slate-800">
                      {showOptionalReceptionValue(
                        reception.veterinaryClinicName,
                        "Recepción directa",
                      )}
                    </p>

                    {reception.referringVeterinarianName && (
                      <p className="mt-1 text-sm text-slate-500">
                        Dr. {reception.referringVeterinarianName}
                      </p>
                    )}

                    {reception.referralNotes && (
                      <p className="mt-2 max-w-64 text-xs text-slate-500">
                        {reception.referralNotes}
                      </p>
                    )}
                  </td>

                  <td className="px-5 py-4">
                    <span
                      className={[
                        "inline-flex rounded-full px-2.5 py-1 text-xs font-semibold",
                        reception.hasPersonalBelongings
                          ? "bg-amber-50 text-amber-700"
                          : "bg-slate-100 text-slate-600",
                      ].join(" ")}
                    >
                      {reception.hasPersonalBelongings ? "Sí" : "No"}
                    </span>

                    {reception.hasPersonalBelongings && (
                      <p className="mt-2 max-w-64 text-sm text-slate-500">
                        {showOptionalReceptionValue(
                          reception.personalBelongingsDescription,
                        )}
                      </p>
                    )}
                  </td>

                  <td className="px-5 py-4">
                    <p className="text-sm font-medium text-slate-800">
                      {reception.receivedByUserName}
                    </p>

                    {reception.notes && (
                      <p className="mt-2 max-w-64 text-xs text-slate-500">
                        {reception.notes}
                      </p>
                    )}
                  </td>

                  <td className="whitespace-nowrap px-5 py-4 text-right">
                    <div className="flex max-w-80 flex-wrap justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => onHistory(reception)}
                        disabled={isLifecyclePending}
                        className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Historial de cambios
                      </button>

                      {canViewLifecycleHistory && (
                        <button
                          type="button"
                          onClick={() => onLifecycleHistory(reception)}
                          disabled={isLifecyclePending}
                          className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          Historial de ciclo
                        </button>
                      )}

                      {reception.isActive && canManage && (
                        <button
                          type="button"
                          onClick={() => onEdit(reception)}
                          disabled={isLifecyclePending}
                          className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {reception.isNormalEditLocked ? "Ver" : "Editar"}
                        </button>
                      )}

                      {reception.isActive &&
                        reception.hasCremation &&
                        canAmend && (
                          <button
                            type="button"
                            onClick={() => onCorrection(reception)}
                            disabled={isLifecyclePending}
                            className="rounded-lg border border-blue-200 px-3 py-2 text-sm font-medium text-blue-700 transition hover:bg-blue-50 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            Crear enmienda
                          </button>
                        )}

                      {reception.isActive &&
                        reception.hasCremation &&
                        canManage && (
                          <button
                            type="button"
                            onClick={() => onClarification(reception)}
                            disabled={isLifecyclePending}
                            className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            Agregar aclaración
                          </button>
                        )}

                      {reception.isActive && canApproveLifecycle && (
                        <button
                          type="button"
                          onClick={() =>
                            onLifecycleAction("deactivate", reception)
                          }
                          disabled={isLifecyclePending}
                          className="rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isLifecyclePending
                            ? "Procesando..."
                            : "Desactivar recepción"}
                        </button>
                      )}

                      {reception.isActive && canRequestDeactivation && (
                        <button
                          type="button"
                          onClick={() =>
                            onLifecycleAction("request", reception)
                          }
                          disabled={isLifecyclePending}
                          className="rounded-lg border border-amber-200 px-3 py-2 text-sm font-medium text-amber-800 transition hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isLifecyclePending
                            ? "Procesando..."
                            : "Solicitar desactivación"}
                        </button>
                      )}

                      {!reception.isActive && canApproveLifecycle && (
                        <button
                          type="button"
                          onClick={() =>
                            onLifecycleAction("restore", reception)
                          }
                          disabled={isLifecyclePending}
                          className="rounded-lg border border-emerald-200 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isLifecyclePending
                            ? "Procesando..."
                            : "Restaurar recepción"}
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
