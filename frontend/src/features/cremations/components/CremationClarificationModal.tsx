import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import {
  createCremationClarification,
  getCremationClarifications,
} from "../api/cremationsApi";
import type {
  Cremation,
  CreateCremationClarificationPayload,
} from "../types/cremation.types";

interface Props {
  cremation: Cremation;
  origin: HTMLElement | null;
  onClose: () => void;
}

interface ClarificationAttempt {
  cremationId: string;
  request: CreateCremationClarificationPayload;
}

function roleLabel(role: string): string {
  return (
    (
      {
        Owner: "Propietario",
        Admin: "Administrador",
        Manager: "Gerente",
      } as Record<string, string>
    )[role] ?? role
  );
}

export function CremationClarificationModal({
  cremation,
  origin,
  onClose,
}: Props) {
  const queryClient = useQueryClient();
  const canAdd = hasPermission("Cremations.Manage");
  const history = useQuery({
    queryKey: ["cremations", cremation.id, "clarifications"],
    queryFn: () => getCremationClarifications(cremation.id),
  });
  const [text, setText] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [uncertain, setUncertain] = useState<ClarificationAttempt | null>(null);
  const [open, setOpen] = useState(true);
  const busy = useRef(false);
  const dialog = useRef<HTMLElement>(null);
  const textField = useRef<HTMLTextAreaElement>(null);
  const retry = useRef<HTMLButtonElement>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  const returnFocus = useRef(origin);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() =>
      (
        retry.current ??
        textField.current ??
        closeButton.current ??
        dialog.current
      )?.focus(),
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
        (
          retry.current ??
          textField.current ??
          closeButton.current ??
          element
        ).focus();
    });
    return () => cancelAnimationFrame(frame);
  }, [open, pending, uncertain]);

  function close() {
    if (busy.current) return;
    if (uncertain) setOpen(false);
    else onClose();
  }

  async function send(attempt: ClarificationAttempt) {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError("");
    try {
      await createCremationClarification(attempt.cremationId, attempt.request);
    } catch (failure) {
      const status = axios.isAxiosError(failure)
        ? failure.response?.status
        : null;
      if (!status || status >= 500 || status === 408) {
        setUncertain(attempt);
        setError(
          "No se pudo confirmar si la aclaración se guardó. Reintenta con la misma solicitud.",
        );
      } else {
        const data = axios.isAxiosError(failure)
          ? failure.response?.data
          : null;
        setError(
          typeof data === "string"
            ? data
            : (data?.message ??
                data?.detail ??
                "No fue posible guardar la aclaración."),
        );
      }
      busy.current = false;
      setPending(false);
      return;
    }
    // Refresh failures must not turn a confirmed append into an uncertain request.
    await Promise.allSettled([
      queryClient.invalidateQueries({ queryKey: ["cremations"] }),
    ]);
    toast.success("Aclaración guardada. Las notas originales se conservaron.");
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
        "button:not(:disabled), textarea:not(:disabled)",
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
        La aclaración de {cremation.petName} está pendiente de confirmación.
        <button
          type="button"
          className="ml-3 underline"
          onClick={() => {
            returnFocus.current =
              document.activeElement instanceof HTMLElement
                ? document.activeElement
                : null;
            setOpen(true);
          }}
        >
          Retomar aclaración
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
        aria-labelledby="clarification-title"
        aria-describedby="clarification-description"
        aria-busy={pending}
        tabIndex={-1}
        onKeyDown={trap}
        className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl"
      >
        <h2 id="clarification-title" className="text-xl font-semibold">
          Aclaraciones del caso
        </h2>
        <p
          id="clarification-description"
          className="mt-2 text-sm text-slate-600"
        >
          {cremation.petName}. Este registro está cancelado. Los datos
          históricos no se pueden modificar directamente. Las aclaraciones se
          agregan sin reemplazar las notas ni los comentarios operativos.
        </p>
        <dl className="mt-4 space-y-2 rounded-lg bg-slate-50 p-3 text-sm">
          <div>
            <dt className="font-medium">Responsable</dt>
            <dd>{cremation.assignedToUserName ?? "Sin asignar"}</dd>
          </div>
          <div>
            <dt className="font-medium">Paquete registrado</dt>
            <dd>{cremation.packageName || "Sin paquete registrado"}</dd>
          </div>
          <div>
            <dt className="font-medium">Urna registrada</dt>
            <dd>{cremation.urnDescription ?? "Sin urna registrada"}</dd>
          </div>
          <div>
            <dt className="font-medium">Cotización registrada</dt>
            <dd>
              {cremation.quotedPrice === null
                ? "Sin cotización registrada"
                : new Intl.NumberFormat("es-MX", {
                    style: "currency",
                    currency: "MXN",
                  }).format(cremation.quotedPrice)}
            </dd>
          </div>
          <div>
            <dt className="font-medium">Instrucciones especiales</dt>
            <dd className="whitespace-pre-wrap">
              {cremation.specialInstructions ?? "Sin instrucciones"}
            </dd>
          </div>
          <div>
            <dt className="font-medium">Descripción del accesorio</dt>
            <dd className="whitespace-pre-wrap">
              {cremation.accessoryDescription ?? "Sin descripción"}
            </dd>
          </div>
          <div>
            <dt className="font-medium">Notas originales del caso</dt>
            <dd className="whitespace-pre-wrap">
              {cremation.notes ?? "Sin notas registradas"}
            </dd>
          </div>
        </dl>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (canAdd && !uncertain && text.trim())
              void send({
                cremationId: cremation.id,
                request: { requestId: crypto.randomUUID(), text: text.trim() },
              });
          }}
        >
          {canAdd && (
            <>
              <label
                htmlFor="clarification-text"
                className="mt-4 block text-sm font-medium"
              >
                Aclaración del caso *
              </label>
              <textarea
                ref={textField}
                id="clarification-text"
                rows={3}
                required
                maxLength={1000}
                value={text}
                disabled={pending || Boolean(uncertain)}
                onChange={(event) => setText(event.target.value)}
                className="mt-1 w-full rounded-lg border border-slate-300 p-2"
              />
            </>
          )}
          {uncertain && (
            <p role="status" className="mt-3 text-sm text-amber-800">
              Se conservarán los mismos datos al reintentar.
            </p>
          )}
          {error && (
            <p role="alert" className="mt-3 text-sm text-red-700">
              {error}
            </p>
          )}
          <div className="mt-5 flex justify-end gap-3">
            <button
              ref={closeButton}
              type="button"
              disabled={pending}
              onClick={close}
              className="rounded-lg border px-4 py-2"
            >
              Cerrar
            </button>
            {canAdd &&
              (uncertain ? (
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
                  disabled={pending || !text.trim()}
                  className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:opacity-50"
                >
                  {pending ? "Guardando..." : "Agregar aclaración"}
                </button>
              ))}
          </div>
        </form>
        <h3 className="mt-5 border-t border-slate-200 pt-4 font-medium">
          Aclaraciones registradas
        </h3>
        {history.isLoading ? (
          <p className="mt-2 text-sm">Cargando aclaraciones...</p>
        ) : history.isError ? (
          <p role="alert" className="mt-2 text-sm text-red-700">
            No fue posible cargar las aclaraciones.
          </p>
        ) : history.data?.length ? (
          <ol className="mt-3 space-y-3 text-sm">
            {history.data.map((entry) => (
              <li
                key={entry.id}
                className="rounded-lg border border-slate-200 p-3"
              >
                <p className="font-medium">
                  {entry.sequence}.{" "}
                  {new Date(entry.createdAt).toLocaleString("es-MX")} ·
                  Cancelada
                </p>
                <p>
                  Por: {entry.actorNameSnapshot} ({roleLabel(entry.actorRole)})
                </p>
                <p className="whitespace-pre-wrap">{entry.text}</p>
              </li>
            ))}
          </ol>
        ) : (
          <p className="mt-2 text-sm text-slate-600">
            Aún no hay aclaraciones registradas.
          </p>
        )}
      </section>
    </div>
  );
}
