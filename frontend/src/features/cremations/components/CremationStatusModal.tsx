import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";

import { currentUserId, hasPermission } from "../../auth/utils/permissions";

import {
  CremationStatus,
  type ChangeCremationStatusPayload,
  type Cremation,
  type StartCremationStatusPayload,
} from "../types/cremation.types";
import { getCremationStatusLabel } from "../utils/cremationLabels";
import { getAllowedNextStatuses } from "../utils/cremationStatus";

interface CremationStatusModalProps {
  isOpen: boolean;
  cremation: Cremation | null;
  isSubmitting: boolean;
  retryRequest: StartCremationStatusPayload | null;

  onClose: () => void;

  onSubmit: (payload: ChangeCremationStatusPayload) => Promise<void>;
  onRetry: () => Promise<void>;
}

function getStatusDescription(status: CremationStatus): string {
  switch (status) {
    case CremationStatus.Scheduled:
      return "La cremación queda programada para la fecha registrada.";

    case CremationStatus.InProgress:
      return "Registra el inicio del proceso de cremación.";

    case CremationStatus.Cooling:
      return "Indica que la cremación terminó y comenzó el periodo de enfriamiento.";

    case CremationStatus.ProcessingRemains:
      return "Indica que los restos están siendo preparados después del enfriamiento.";

    case CremationStatus.Completed:
      return "Marca el proceso de cremación como completado.";

    case CremationStatus.ReadyForDelivery:
      return "Indica que los restos y artículos del servicio están listos para entrega.";

    case CremationStatus.Delivered:
      return "Marca la cremación como entregada al cliente.";

    case CremationStatus.Cancelled:
      return "Cancela la cremación. Este estado es final.";

    default:
      return "";
  }
}

function getPrerequisiteMessage(
  cremation: Cremation,
  status: CremationStatus,
): string | null {
  if (status === CremationStatus.Scheduled && !cremation.scheduledAt) {
    return "Debes establecer una fecha programada antes de cambiar a Programada.";
  }

  if (status === CremationStatus.InProgress && !cremation.assignedToUserId) {
    return "Debes asignar un usuario antes de iniciar la cremación.";
  }

  return null;
}

export function CremationStatusModal({
  isOpen,
  cremation,
  isSubmitting,
  retryRequest,
  onClose,
  onSubmit,
  onRetry,
}: CremationStatusModalProps) {
  const dialogRef = useRef<HTMLElement>(null);
  const qrInputRef = useRef<HTMLInputElement>(null);
  const retryButtonRef = useRef<HTMLButtonElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<CremationStatus | null>(
    null,
  );

  const [notes, setNotes] = useState("");
  const [receptionQrCode, setReceptionQrCode] = useState("");
  const [custodyAccepted, setCustodyAccepted] = useState(false);

  const allowedStatuses = useMemo(() => {
    if (!cremation || !hasPermission("Cremations.Manage")) {
      return [];
    }

    const actorId = currentUserId();
    return getAllowedNextStatuses(cremation.status).filter(
      (status) =>
        status !== CremationStatus.InProgress ||
        (actorId !== null &&
          actorId.toLowerCase() === cremation.assignedToUserId?.toLowerCase()),
    );
  }, [cremation]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setSelectedStatus(
      retryRequest
        ? CremationStatus.InProgress
        : allowedStatuses.length === 1
          ? allowedStatuses[0]
          : null,
    );
    setNotes(retryRequest?.notes ?? "");
    setReceptionQrCode(retryRequest?.receptionQrCode ?? "");
    setCustodyAccepted(retryRequest?.custodyAccepted ?? false);
  }, [allowedStatuses, isOpen, retryRequest]);

  useEffect(() => {
    if (!isOpen) return;
    returnFocusRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;

    return () => {
      requestAnimationFrame(() => {
        const previous = returnFocusRef.current;
        if (previous?.isConnected && !previous.matches(":disabled"))
          previous.focus();
        else document.getElementById("cremations-title")?.focus();
      });
    };
  }, [isOpen, cremation?.id]);

  useEffect(() => {
    if (!isOpen) return;
    requestAnimationFrame(() => {
      if (retryRequest) {
        retryButtonRef.current?.focus();
      } else {
        const firstEnabledStatus =
          dialogRef.current?.querySelector<HTMLElement>(
            'input[name="cremation-status"]:not(:disabled)',
          );
        const fallback = dialogRef.current?.querySelector<HTMLElement>(
          "button:not(:disabled)",
        );
        (firstEnabledStatus ?? fallback ?? dialogRef.current)?.focus();
      }
    });
  }, [isOpen, retryRequest]);

  if (!isOpen || !cremation) {
    return null;
  }

  const prerequisiteMessage =
    selectedStatus === null
      ? null
      : getPrerequisiteMessage(cremation, selectedStatus);

  const isStart = selectedStatus === CremationStatus.InProgress;
  const canSubmit =
    selectedStatus !== null &&
    prerequisiteMessage === null &&
    !isSubmitting &&
    !retryRequest &&
    (!isStart || (receptionQrCode.trim() !== "" && custodyAccepted));

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSubmit || selectedStatus === null) {
      return;
    }

    if (selectedStatus === CremationStatus.InProgress) {
      await onSubmit({
        status: CremationStatus.InProgress,
        notes: notes.trim() || null,
        requestId: crypto.randomUUID(),
        receptionQrCode,
        custodyAccepted: true,
      });
    } else {
      await onSubmit({
        status: selectedStatus,
        notes: notes.trim() || null,
      });
    }
  }

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      if (!isSubmitting) onClose();
      return;
    }
    if (event.key !== "Tab") return;

    const controls = dialogRef.current?.querySelectorAll<HTMLElement>(
      "button:not([disabled]), input:not([disabled]), textarea:not([disabled])",
    );
    const first = controls?.[0];
    const last = controls?.[controls.length - 1];
    if (!first || !last) {
      event.preventDefault();
      dialogRef.current?.focus();
    } else if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="cremation-status-title"
        aria-busy={isSubmitting}
        onKeyDown={handleKeyDown}
        onMouseDown={(event) => event.stopPropagation()}
        className="max-h-full w-full max-w-xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Flujo operativo
            </p>

            <h2
              id="cremation-status-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {isStart ? "Iniciar cremación" : "Cambiar estado"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {cremation.petName}
              {cremation.status !== CremationStatus.Scheduled &&
                ` · ${cremation.qrCode}`}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form onSubmit={handleSubmit} className="space-y-6 px-6 py-6">
          <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
              Estado actual
            </p>

            <p className="mt-2 font-semibold text-slate-900">
              {getCremationStatusLabel(cremation.status)}
            </p>
          </div>

          {retryRequest && (
            <div
              role="alert"
              className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
            >
              No se pudo confirmar si la solicitud se completó. Puedes volver a
              intentarlo con la misma solicitud.
            </div>
          )}

          <div>
            <p className="text-sm font-medium text-slate-700">Nuevo estado</p>

            <div className="mt-3 space-y-3">
              {allowedStatuses.length > 0 ? (
                allowedStatuses.map((status) => {
                  const prerequisite = getPrerequisiteMessage(
                    cremation,
                    status,
                  );

                  const selected = selectedStatus === status;

                  return (
                    <label
                      key={status}
                      className={[
                        "block rounded-xl border p-4 transition",
                        prerequisite
                          ? "cursor-not-allowed border-slate-200 bg-slate-50 opacity-70"
                          : "cursor-pointer",
                        selected
                          ? "border-slate-900 ring-1 ring-slate-900"
                          : "border-slate-200",
                      ].join(" ")}
                    >
                      <div className="flex items-start gap-3">
                        <input
                          type="radio"
                          name="cremation-status"
                          value={status}
                          checked={selected}
                          disabled={
                            Boolean(prerequisite) ||
                            isSubmitting ||
                            Boolean(retryRequest)
                          }
                          onChange={() => {
                            setSelectedStatus(status);
                            if (status === CremationStatus.InProgress)
                              requestAnimationFrame(() =>
                                qrInputRef.current?.focus(),
                              );
                          }}
                          className="mt-1 h-4 w-4 border-slate-300"
                        />

                        <div>
                          <p className="font-medium text-slate-900">
                            {getCremationStatusLabel(status)}
                          </p>

                          <p className="mt-1 text-sm text-slate-500">
                            {getStatusDescription(status)}
                          </p>

                          {prerequisite && (
                            <p className="mt-2 text-sm font-medium text-amber-700">
                              {prerequisite}
                            </p>
                          )}
                        </div>
                      </div>
                    </label>
                  );
                })
              ) : (
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-5 text-sm text-slate-600">
                  Esta cremación no tiene más cambios de estado disponibles.
                </div>
              )}
            </div>
          </div>

          {selectedStatus !== null && (
            <div className="rounded-xl bg-blue-50 px-4 py-3 text-sm text-blue-700">
              Cambiarás de{" "}
              <strong>{getCremationStatusLabel(cremation.status)}</strong> a{" "}
              <strong>{getCremationStatusLabel(selectedStatus)}</strong>.
            </div>
          )}

          {prerequisiteMessage && (
            <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              {prerequisiteMessage}
            </div>
          )}

          {isStart && (
            <div className="space-y-4 rounded-xl border border-blue-200 bg-blue-50 p-4">
              <div>
                <label
                  htmlFor="cremation-start-qr"
                  className="block text-sm font-medium text-slate-800"
                >
                  Código del caso
                </label>
                <p
                  id="cremation-start-qr-help"
                  className="mt-1 text-sm text-slate-600"
                >
                  Escanea o ingresa el código de la recepción para confirmar la
                  identidad.
                </p>
                <input
                  ref={qrInputRef}
                  id="cremation-start-qr"
                  type="text"
                  autoComplete="off"
                  value={receptionQrCode}
                  onChange={(event) => setReceptionQrCode(event.target.value)}
                  disabled={isSubmitting || Boolean(retryRequest)}
                  required
                  aria-describedby="cremation-start-qr-help"
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />
              </div>
              <label className="flex items-start gap-3 text-sm text-slate-800">
                <input
                  type="checkbox"
                  checked={custodyAccepted}
                  onChange={(event) => setCustodyAccepted(event.target.checked)}
                  disabled={isSubmitting || Boolean(retryRequest)}
                  required
                  className="mt-0.5 h-4 w-4 rounded border-slate-300"
                />
                <span>
                  Confirmo que recibí el caso y verifiqué su identidad.
                </span>
              </label>
            </div>
          )}

          <div>
            <label
              htmlFor="cremation-status-notes"
              className="block text-sm font-medium text-slate-700"
            >
              Notas del cambio
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <textarea
              id="cremation-status-notes"
              rows={4}
              maxLength={1000}
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              disabled={isSubmitting || Boolean(retryRequest)}
              placeholder="Agrega alguna observación relevante sobre este cambio."
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            <div className="mt-1 text-right text-xs text-slate-400">
              {notes.length}/1000
            </div>
          </div>

          {selectedStatus === CremationStatus.Delivered && (
            <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
              Al confirmar la entrega se registrará automáticamente la fecha y
              hora de entrega.
            </div>
          )}

          {selectedStatus === CremationStatus.Cancelled && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              La cancelación es un estado final. Después de confirmarla no habrá
              más transiciones disponibles.
            </div>
          )}

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              ref={retryButtonRef}
              type={retryRequest ? "button" : "submit"}
              onClick={retryRequest ? onRetry : undefined}
              disabled={retryRequest ? isSubmitting : !canSubmit}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSubmitting
                ? isStart
                  ? "Iniciando..."
                  : "Actualizando..."
                : retryRequest
                  ? "Reintentar"
                  : isStart
                    ? "Iniciar cremación"
                    : "Confirmar cambio"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
