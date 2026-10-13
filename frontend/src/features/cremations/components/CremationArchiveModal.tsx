import axios from "axios";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import toast from "react-hot-toast";
import {
  deactivateCremation,
  restoreCremation,
  getCremationArchiveEvents,
} from "../api/cremationsApi";
import { getCremationStatusLabel } from "../utils/cremationLabels";
import { getCremationApiErrorMessage } from "../utils/cremationApiErrors";
import type {
  ChangeCremationActiveStatePayload,
  Cremation,
} from "../types/cremation.types";

interface ArchiveAttempt {
  cremationId: string;
  restore: boolean;
  request: ChangeCremationActiveStatePayload;
}

interface Props {
  cremation: Cremation;
  origin: HTMLElement | null;
  onClose: () => void;
}

function actorRoleLabel(role: string): string {
  if (role === "Owner") return "Propietario";
  if (role === "Admin") return "Administrador";
  if (role === "Manager") return "Gerente";
  return role;
}

export function CremationArchiveModal({ cremation, origin, onClose }: Props) {
  const queryClient = useQueryClient();
  const restoring = !cremation.isActive;
  const title = restoring ? "Restaurar cremación" : "Archivar cremación";
  const events = useQuery({
    queryKey: ["cremations", cremation.id, "archive-events"],
    queryFn: () => getCremationArchiveEvents(cremation.id),
  });
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [uncertain, setUncertain] = useState<ArchiveAttempt | null>(null);
  const [open, setOpen] = useState(true);
  const busy = useRef(false);
  const dialog = useRef<HTMLElement>(null);
  const reasonField = useRef<HTMLTextAreaElement>(null);
  const retry = useRef<HTMLButtonElement>(null);
  const returnFocus = useRef(origin);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() =>
      (retry.current ?? reasonField.current ?? dialog.current)?.focus(),
    );
    const element = dialog.current;
    return () => {
      cancelAnimationFrame(frame);
      requestAnimationFrame(() => {
        if (element?.isConnected) return;
        const previous = returnFocus.current;
        if (previous?.isConnected && !previous.matches(":disabled"))
          previous.focus();
        else document.getElementById("cremations-title")?.focus();
      });
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() => {
      const element = dialog.current;
      if (!element?.isConnected) return;
      if (pending) element.focus();
      else if (
        !element.contains(document.activeElement) ||
        document.activeElement === element
      )
        (retry.current ?? reasonField.current ?? element).focus();
    });
    return () => cancelAnimationFrame(frame);
  }, [open, pending, uncertain]);

  function close() {
    if (busy.current) return;
    if (uncertain) setOpen(false);
    else onClose();
  }

  async function send(attempt: ArchiveAttempt) {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError("");
    try {
      if (attempt.restore)
        await restoreCremation(attempt.cremationId, attempt.request);
      else await deactivateCremation(attempt.cremationId, attempt.request);
    } catch (failure) {
      const status = axios.isAxiosError(failure)
        ? failure.response?.status
        : null;
      if (!status || status >= 500 || status === 408) {
        setUncertain(attempt);
        setError(
          "No se pudo confirmar si la acción se completó. Reintenta con la misma solicitud.",
        );
      } else {
        const data = axios.isAxiosError(failure)
          ? failure.response?.data
          : null;
        setError(getCremationApiErrorMessage(data, "No fue posible completar la acción."));
        await Promise.allSettled([
          queryClient.invalidateQueries({ queryKey: ["cremations"] }),
        ]);
      }
      busy.current = false;
      setPending(false);
      return;
    }
    // Refresh before closing: the originating row may leave this view.
    // A refresh failure must not turn a confirmed operation into an uncertain retry.
    await Promise.allSettled([
      queryClient.invalidateQueries({ queryKey: ["cremations"] }),
    ]);
    toast.success(
      restoring
        ? "Cremación restaurada. El motivo quedó registrado."
        : "Cremación archivada. El motivo quedó registrado.",
    );
    onClose();
  }

  function trap(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      close();
      return;
    }
    if (event.key !== "Tab") return;
    const controls = Array.from(
      dialog.current?.querySelectorAll<HTMLElement>(
        "button:not(:disabled), textarea:not(:disabled), summary",
      ) ?? [],
    );
    const first = controls[0];
    const last = controls.at(-1);
    if (!first) {
      event.preventDefault();
      dialog.current?.focus();
    } else if (
      event.shiftKey &&
      (document.activeElement === first ||
        document.activeElement === dialog.current)
    ) {
      event.preventDefault();
      last?.focus();
    } else if (
      !event.shiftKey &&
      (document.activeElement === last ||
        document.activeElement === dialog.current)
    ) {
      event.preventDefault();
      first.focus();
    }
  }

  if (!open)
    return (
      <div
        role="status"
        className="rounded-xl border border-amber-300 bg-amber-50 p-4"
      >
        La solicitud para {restoring ? "restaurar" : "archivar"} la cremación de{" "}
        {cremation.petName} está pendiente de confirmación.
        <button
          type="button"
          onClick={() => {
            returnFocus.current =
              document.activeElement instanceof HTMLElement
                ? document.activeElement
                : null;
            setOpen(true);
          }}
          className="ml-3 underline"
        >
          Retomar solicitud
        </button>
      </div>
    );

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) close();
      }}
    >
      <section
        ref={dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="archive-title"
        aria-describedby="archive-description"
        aria-busy={pending}
        tabIndex={-1}
        onKeyDown={trap}
        className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl"
      >
        <h2 id="archive-title" className="text-xl font-semibold">
          {title}
        </h2>
        <p id="archive-description" className="mt-2 text-sm text-slate-600">
          {cremation.petName}. Esta acción quedará registrada. El historial de
          la cremación se conservará.
        </p>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (!uncertain && reason.trim())
              void send({
                cremationId: cremation.id,
                restore: restoring,
                request: {
                  requestId: crypto.randomUUID(),
                  reason: reason.trim(),
                },
              });
          }}
        >
          <label
            htmlFor="archive-reason"
            className="mt-4 block text-sm font-medium"
          >
            {restoring ? "Motivo de la restauración" : "Motivo del archivo"} *
          </label>
          <textarea
            ref={reasonField}
            id="archive-reason"
            rows={3}
            required
            maxLength={1000}
            value={reason}
            disabled={pending || Boolean(uncertain)}
            onChange={(event) => setReason(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 p-2"
          />
          {uncertain && (
            <p role="status" className="mt-3 text-sm text-amber-800">
              La solicitud sigue pendiente de confirmación. Se conservarán sus
              datos al reintentar.
            </p>
          )}
          {error && (
            <p role="alert" className="mt-3 text-sm text-red-700">
              {error}
            </p>
          )}
          <div className="mt-5 flex justify-end gap-3">
            <button
              type="button"
              disabled={pending}
              onClick={close}
              className="rounded-lg border px-4 py-2"
            >
              {uncertain ? "Cerrar" : "Cancelar"}
            </button>
            {uncertain ? (
              <button
                ref={retry}
                type="button"
                disabled={pending}
                onClick={() => void send(uncertain)}
                className="rounded-lg bg-slate-900 px-4 py-2 text-white"
              >
                {pending ? "Confirmando..." : "Reintentar"}
              </button>
            ) : (
              <button
                type="submit"
                disabled={pending || !reason.trim()}
                className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:opacity-50"
              >
                {pending ? "Guardando..." : title}
              </button>
            )}
          </div>
        </form>
        <details className="mt-5 border-t border-slate-200 pt-4">
          <summary className="cursor-pointer text-sm font-medium">
            Historial de archivo y restauración
          </summary>
          {events.isLoading ? (
            <p className="mt-2 text-sm">Cargando historial...</p>
          ) : events.isError ? (
            <p role="alert" className="mt-2 text-sm text-red-700">
              No fue posible cargar el historial.
            </p>
          ) : events.data?.length ? (
            <ol className="mt-3 max-h-52 space-y-3 overflow-y-auto text-sm">
              {events.data.map((entry) => (
                <li
                  key={entry.id}
                  className="rounded-lg border border-slate-200 p-3"
                >
                  <p className="font-medium">
                    {entry.sequence}.{" "}
                    {entry.newIsActive ? "Restauración" : "Archivo"} ·{" "}
                    {new Date(entry.createdAt).toLocaleString("es-MX")}
                  </p>
                  <p>
                    {entry.previousIsActive ? "Activa" : "Archivada"} →{" "}
                    {entry.newIsActive ? "Activa" : "Archivada"}
                  </p>
                  <p>
                    Por: {entry.actorNameSnapshot} (
                    {actorRoleLabel(entry.actorRole)})
                  </p>
                  <p>Estado: {getCremationStatusLabel(entry.status)}</p>
                  <p>Motivo: {entry.reason}</p>
                </li>
              ))}
            </ol>
          ) : (
            <p className="mt-2 text-sm text-slate-600">
              Aún no hay acciones registradas.
            </p>
          )}
        </details>
      </section>
    </div>
  );
}
