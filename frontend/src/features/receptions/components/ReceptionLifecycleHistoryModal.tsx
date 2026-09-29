import { useEffect, useRef, type KeyboardEvent } from "react";
import { getCollectionStatusLabel } from "../../collections/utils/collectionLabels";
import { getCremationStatusLabel } from "../../cremations/utils/cremationLabels";
import {
  ReceptionLifecycleOutcome,
  type Reception,
  type ReceptionLifecycleEvent,
} from "../types/reception.types";
import {
  getReceptionLifecycleActionLabel,
  getReceptionLifecycleBlockReasonLabel,
  getReceptionLifecycleDependencyLabels,
  getReceptionLifecycleOutcomeLabel,
  getReceptionLifecycleStageLabel,
} from "../utils/receptionLifecycleLabels";
import { getReceptionHistoryActorRoleLabel } from "../utils/receptionHistoryLabels";

interface ReceptionLifecycleHistoryModalProps {
  isOpen: boolean;
  reception: Reception | null;
  history: ReceptionLifecycleEvent[];
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onClose: () => void;
}

const dateFormatter = new Intl.DateTimeFormat("es-MX", {
  dateStyle: "medium",
  timeStyle: "short",
});

const currencyFormatter = new Intl.NumberFormat("es-MX", {
  style: "currency",
  currency: "MXN",
});

function formatDate(value: string | null): string {
  if (!value) return "No disponible";
  return dateFormatter.format(new Date(value));
}

function formatCurrency(value: number | null): string {
  return value === null ? "No disponible" : currencyFormatter.format(value);
}

function activeStateLabel(isActive: boolean): string {
  return isActive ? "Activa" : "Inactiva";
}

export function ReceptionLifecycleHistoryModal({
  isOpen,
  reception,
  history,
  isLoading,
  isError,
  onRetry,
  onClose,
}: ReceptionLifecycleHistoryModalProps) {
  const dialogRef = useRef<HTMLElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const previouslyFocused =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    closeButtonRef.current?.focus();

    return () => previouslyFocused?.focus();
  }, [isOpen, reception?.id]);

  if (!isOpen || reception === null) {
    return null;
  }

  const orderedHistory = [...history].sort(
    (first, second) => first.sequence - second.sequence,
  );

  function handleDialogKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose();
      return;
    }

    if (event.key !== "Tab" || dialogRef.current === null) return;

    const focusable = Array.from(
      dialogRef.current.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])',
      ),
    );
    const first = focusable[0];
    const last = focusable.at(-1);

    if (!first || !last) return;

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={onClose}
    >
      <section
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-lifecycle-history-title"
        className="max-h-full w-full max-w-5xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
        onKeyDown={handleDialogKeyDown}
      >
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Registro inmutable de decisiones
            </p>
            <h2
              id="reception-lifecycle-history-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Historial de ciclo de vida
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              {reception.petName} · {reception.customerName} ·{" "}
              {reception.qrCode}
            </p>
          </div>
          <button
            ref={closeButtonRef}
            type="button"
            onClick={onClose}
            aria-label="Cerrar historial de ciclo de vida"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100"
          >
            ×
          </button>
        </header>

        <div className="space-y-4 px-6 py-6">
          {isLoading && (
            <div className="rounded-xl border border-slate-200 px-5 py-10 text-center text-sm text-slate-500">
              Cargando historial de ciclo de vida...
            </div>
          )}

          {isError && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-5">
              <p className="font-medium text-red-800">
                No fue posible cargar el historial de ciclo de vida.
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
                Sin eventos de ciclo de vida
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Todavía no hay solicitudes, desactivaciones ni restauraciones
                registradas.
              </p>
            </div>
          )}

          {!isLoading &&
            !isError &&
            orderedHistory.map((event) => {
              const dependencies = getReceptionLifecycleDependencyLabels(
                event.dependencies,
              );
              const blockReason = getReceptionLifecycleBlockReasonLabel(
                event.blockReason,
              );

              return (
                <article
                  key={event.id}
                  className="rounded-xl border border-slate-200 p-5"
                >
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="rounded-full bg-slate-900 px-2.5 py-1 text-xs font-semibold text-white">
                          #{event.sequence}
                        </span>
                        <h3 className="font-semibold text-slate-900">
                          {getReceptionLifecycleActionLabel(event.action)}
                        </h3>
                        <span
                          className={[
                            "rounded-full px-2.5 py-1 text-xs font-semibold",
                            event.outcome === ReceptionLifecycleOutcome.Blocked
                              ? "bg-red-50 text-red-700"
                              : event.outcome ===
                                  ReceptionLifecycleOutcome.Recorded
                                ? "bg-blue-50 text-blue-700"
                                : "bg-emerald-50 text-emerald-700",
                          ].join(" ")}
                        >
                          {getReceptionLifecycleOutcomeLabel(event.outcome)}
                        </span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">
                        {getReceptionLifecycleStageLabel(
                          event.operationalStage,
                        )}
                      </p>
                    </div>
                    <time className="text-sm text-slate-500">
                      {formatDate(event.createdAt)}
                    </time>
                  </div>

                  <dl className="mt-4 grid gap-3 rounded-lg bg-slate-50 p-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Actor
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {event.actorUserName}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Rol
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {getReceptionHistoryActorRoleLabel(event.actorRole)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Estado anterior
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {activeStateLabel(event.previousIsActive)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                        Estado resultante
                      </dt>
                      <dd className="mt-1 font-medium text-slate-800">
                        {activeStateLabel(event.newIsActive)}
                      </dd>
                    </div>
                  </dl>

                  <div className="mt-4">
                    <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                      Motivo
                    </p>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-slate-800">
                      {event.reason}
                    </p>
                  </div>

                  {event.outcome === ReceptionLifecycleOutcome.Blocked &&
                    blockReason && (
                      <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
                        {blockReason}
                      </div>
                    )}

                  <div className="mt-4 rounded-lg border border-slate-200 p-4">
                    <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                      Dependencias al momento de la decisión
                    </p>
                    {dependencies.length > 0 ? (
                      <ul className="mt-2 flex flex-wrap gap-2">
                        {dependencies.map((dependency) => (
                          <li
                            key={dependency}
                            className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700"
                          >
                            {dependency}
                          </li>
                        ))}
                      </ul>
                    ) : (
                      <p className="mt-2 text-sm text-slate-600">
                        Sin dependencias operativas registradas.
                      </p>
                    )}

                    <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
                      {event.collectionId && (
                        <div>
                          <dt className="text-slate-500">Recolección</dt>
                          <dd className="font-medium text-slate-800">
                            {event.collectionStatus === null
                              ? "Vinculada"
                              : getCollectionStatusLabel(
                                  event.collectionStatus,
                                )}
                            {event.collectionIsActive === null
                              ? ""
                              : event.collectionIsActive
                                ? " · Activa"
                                : " · Inactiva"}
                          </dd>
                          {(event.collectionCollectedAt ||
                            event.collectionReceivedAt) && (
                            <dd className="mt-1 text-xs text-slate-500">
                              {event.collectionCollectedAt
                                ? `Recolectada: ${formatDate(event.collectionCollectedAt)}`
                                : ""}
                              {event.collectionCollectedAt &&
                              event.collectionReceivedAt
                                ? " · "
                                : ""}
                              {event.collectionReceivedAt
                                ? `Recibida: ${formatDate(event.collectionReceivedAt)}`
                                : ""}
                            </dd>
                          )}
                        </div>
                      )}
                      {event.hasConvertedVeterinaryRequest && (
                        <div>
                          <dt className="text-slate-500">
                            Solicitud veterinaria
                          </dt>
                          <dd className="font-medium text-slate-800">
                            Convertida a recepción
                          </dd>
                        </div>
                      )}
                      {event.cremationId && (
                        <div>
                          <dt className="text-slate-500">Cremación</dt>
                          <dd className="font-medium text-slate-800">
                            {event.cremationStatus === null
                              ? "Vinculada"
                              : getCremationStatusLabel(event.cremationStatus)}
                            {event.cremationIsActive === null
                              ? ""
                              : event.cremationIsActive
                                ? " · Activa"
                                : " · Inactiva"}
                          </dd>
                        </div>
                      )}
                      {event.paymentAccountId && (
                        <div>
                          <dt className="text-slate-500">Cuenta de pago</dt>
                          <dd className="font-medium text-slate-800">
                            Total {formatCurrency(event.serviceTotal)} · Pagado{" "}
                            {formatCurrency(event.amountPaid)}
                          </dd>
                          {event.requiredCollectionPaymentAmount !== null && (
                            <dd className="mt-1 text-xs text-slate-500">
                              Pago requerido para recolección:{" "}
                              {formatCurrency(
                                event.requiredCollectionPaymentAmount,
                              )}
                            </dd>
                          )}
                        </div>
                      )}
                      {event.paymentCount > 0 && (
                        <div>
                          <dt className="text-slate-500">Historial de pagos</dt>
                          <dd className="font-medium text-slate-800">
                            {event.paymentCount} pago(s)
                          </dd>
                        </div>
                      )}
                      {event.activeReceptionEvidenceCount > 0 && (
                        <div>
                          <dt className="text-slate-500">
                            Evidencia de recepción
                          </dt>
                          <dd className="font-medium text-slate-800">
                            {event.activeReceptionEvidenceCount} archivo(s)
                          </dd>
                        </div>
                      )}
                      {(event.collectionEvidenceCount > 0 ||
                        event.collectionAssignmentHistoryCount > 0) && (
                        <div>
                          <dt className="text-slate-500">
                            Evidencia de recolección
                          </dt>
                          <dd className="font-medium text-slate-800">
                            {event.collectionEvidenceCount} archivo(s) ·{" "}
                            {event.collectionAssignmentHistoryCount}{" "}
                            movimiento(s)
                          </dd>
                        </div>
                      )}
                      <div>
                        <dt className="text-slate-500">
                          Historial de recepción
                        </dt>
                        <dd className="font-medium text-slate-800">
                          {event.receptionHistoryCount} evento(s)
                          {event.latestReceptionHistorySequence === null
                            ? ""
                            : ` · Última secuencia #${event.latestReceptionHistorySequence}`}
                        </dd>
                      </div>
                      {event.requiresFinancialReview !== null && (
                        <div>
                          <dt className="text-slate-500">
                            Revisión financiera
                          </dt>
                          <dd className="font-medium text-slate-800">
                            {event.requiresFinancialReview
                              ? "Requerida"
                              : "No requerida"}
                          </dd>
                        </div>
                      )}
                    </dl>
                  </div>
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
