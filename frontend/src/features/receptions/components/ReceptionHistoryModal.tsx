import type {
  Reception,
  ReceptionHistoryChange,
  ReceptionHistoryEvent,
} from "../types/reception.types";
import { ReceptionHistoryEventKind } from "../types/reception.types";
import {
  getReceptionHistoryActorRoleLabel,
  getReceptionHistoryChangeValue,
  getReceptionHistoryEventLabel,
  getReceptionHistoryFieldLabel,
  getReceptionHistoryStageLabel,
  getReceptionHistoryStoredValue,
  getReceptionHistoryStatusLabel,
} from "../utils/receptionHistoryLabels";

interface ReceptionHistoryModalProps {
  isOpen: boolean;
  reception: Reception | null;
  history: ReceptionHistoryEvent[];
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onClose: () => void;
}

function formatHistoryDate(value: string): string {
  return new Intl.DateTimeFormat("es-MX", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function HistoryChangeValue({
  change,
  side,
}: {
  change: ReceptionHistoryChange;
  side: "original" | "new";
}) {
  const value = getReceptionHistoryChangeValue(change, side);
  const storedValue = getReceptionHistoryStoredValue(change, side);
  const displayValue =
    side === "original" ? change.originalDisplayValue : change.newDisplayValue;

  return (
    <>
      <p className="mt-1 break-words text-sm text-slate-800">{value}</p>
      {displayValue !== null && storedValue !== value && (
        <p className="mt-1 break-all font-mono text-xs text-slate-500">
          Valor almacenado: {storedValue}
        </p>
      )}
    </>
  );
}

export function ReceptionHistoryModal({
  isOpen,
  reception,
  history,
  isLoading,
  isError,
  onRetry,
  onClose,
}: ReceptionHistoryModalProps) {
  if (!isOpen || reception === null) {
    return null;
  }

  const orderedHistory = [...history].sort(
    (first, second) => first.sequence - second.sequence,
  );

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={onClose}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-history-title"
        className="max-h-full w-full max-w-4xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Registro cronológico de solo lectura
            </p>
            <h2
              id="reception-history-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Historial de la recepción
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              {reception.petName} · {reception.customerName} ·{" "}
              {reception.qrCode}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar historial"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100"
          >
            ×
          </button>
        </header>

        <div className="space-y-4 px-6 py-6">
          {isLoading && (
            <div className="rounded-xl border border-slate-200 px-5 py-10 text-center text-sm text-slate-500">
              Cargando historial...
            </div>
          )}

          {isError && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-5">
              <p className="font-medium text-red-800">
                No fue posible cargar el historial de la recepción.
              </p>
              <button
                type="button"
                onClick={onRetry}
                className="mt-3 rounded-lg border border-red-300 bg-white px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50"
              >
                Intentar nuevamente
              </button>
            </div>
          )}

          {!isLoading && !isError && orderedHistory.length === 0 && (
            <div className="rounded-xl border border-dashed border-slate-300 px-5 py-10 text-center">
              <p className="font-medium text-slate-800">
                Sin movimientos en el historial
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Todavía no hay auditorías, enmiendas ni aclaraciones
                registradas.
              </p>
            </div>
          )}

          {!isLoading &&
            !isError &&
            orderedHistory.map((historyEvent) => {
              const statusLabel = getReceptionHistoryStatusLabel(
                historyEvent.cremationStatus,
              );
              const isClarification =
                historyEvent.eventKind ===
                ReceptionHistoryEventKind.Clarification;

              return (
                <article
                  key={historyEvent.id}
                  className="rounded-xl border border-slate-200 p-5"
                >
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="rounded-full bg-slate-900 px-2.5 py-1 text-xs font-semibold text-white">
                          #{historyEvent.sequence}
                        </span>
                        <h3 className="font-semibold text-slate-900">
                          {getReceptionHistoryEventLabel(
                            historyEvent.eventKind,
                          )}
                        </h3>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">
                        {getReceptionHistoryStageLabel(historyEvent.stage)}
                        {statusLabel ? ` · Estado: ${statusLabel}` : ""}
                      </p>
                    </div>
                    <time className="text-sm text-slate-500">
                      {formatHistoryDate(historyEvent.createdAt)}
                    </time>
                  </div>

                  <dl className="mt-4 grid gap-3 rounded-lg bg-slate-50 p-4 text-sm sm:grid-cols-2">
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Registró
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {historyEvent.actorUserName}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Rol
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {getReceptionHistoryActorRoleLabel(
                          historyEvent.actorRole,
                        )}
                      </dd>
                    </div>
                  </dl>

                  {historyEvent.reason && (
                    <div className="mt-4">
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Motivo
                      </p>
                      <p className="mt-1 whitespace-pre-wrap text-sm text-slate-800">
                        {historyEvent.reason}
                      </p>
                    </div>
                  )}

                  {isClarification ? (
                    <div className="mt-4 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3">
                      <p className="text-xs font-medium uppercase tracking-wide text-blue-700">
                        Aclaración
                      </p>
                      <p className="mt-1 whitespace-pre-wrap text-sm text-blue-950">
                        {historyEvent.clarificationText ??
                          "Sin texto disponible."}
                      </p>
                    </div>
                  ) : (
                    <div className="mt-4 space-y-3">
                      {historyEvent.changes.map((change) => (
                        <div
                          key={`${historyEvent.id}-${change.field}`}
                          className="rounded-lg border border-slate-200 p-4"
                        >
                          <p className="text-sm font-semibold text-slate-800">
                            {getReceptionHistoryFieldLabel(
                              change.field,
                              historyEvent.eventKind,
                              historyEvent.cremationStatus,
                            )}
                          </p>
                          <div className="mt-3 grid gap-3 sm:grid-cols-2">
                            <div>
                              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                                Valor anterior
                              </p>
                              <HistoryChangeValue
                                change={change}
                                side="original"
                              />
                            </div>
                            <div>
                              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                                Valor nuevo
                              </p>
                              <HistoryChangeValue change={change} side="new" />
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </article>
              );
            })}
        </div>

        <footer className="sticky bottom-0 flex justify-end border-t border-slate-200 bg-white px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          >
            Cerrar
          </button>
        </footer>
      </section>
    </div>
  );
}
