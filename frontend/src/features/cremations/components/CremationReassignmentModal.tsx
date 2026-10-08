import type { CremationUserOption } from "../types/cremationForm.types";
import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import toast from "react-hot-toast";
import { reassignCremation } from "../api/cremationsApi";
import {
  CremationStatus,
  type Cremation,
  type ReassignCremationPayload,
} from "../types/cremation.types";

interface Props {
  cremation: Cremation;
  users: CremationUserOption[];
  loadingUsers: boolean;
  origin: HTMLElement | null;
  onClose: () => void;
}

export function CremationReassignmentModal({
  cremation,
  users,
  loadingUsers,
  origin,
  onClose,
}: Props) {
  const queryClient = useQueryClient();
  const [target, setTarget] = useState("");
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [uncertain, setUncertain] = useState<ReassignCremationPayload | null>(
    null,
  );
  const [open, setOpen] = useState(true);
  const busy = useRef(false);
  const dialog = useRef<HTMLElement>(null);
  const select = useRef<HTMLSelectElement>(null);
  const retry = useRef<HTMLButtonElement>(null);
  const returnFocus = useRef<HTMLElement | null>(origin);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() =>
      (
        retry.current ??
        (select.current?.disabled ? null : select.current) ??
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
          (select.current?.disabled ? null : select.current) ??
          element
        ).focus();
    });
    return () => cancelAnimationFrame(frame);
  }, [open, pending, uncertain, loadingUsers]);

  function close() {
    if (busy.current) return;
    if (uncertain) setOpen(false);
    else onClose();
  }

  async function send(request: ReassignCremationPayload) {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError("");
    try {
      await reassignCremation(cremation.id, request);
    } catch (failure) {
      const status = axios.isAxiosError(failure)
        ? failure.response?.status
        : null;
      if (!status || status >= 500 || status === 408) {
        setUncertain(request);
        setError(
          "No se pudo confirmar si la solicitud se completó. Reintenta con la misma solicitud.",
        );
      } else {
        // A later rejection cannot disprove an earlier uncertain application.
        // Keep the original payload when this was an uncertain retry.
        const data = axios.isAxiosError(failure)
          ? failure.response?.data
          : null;
        setError(
          typeof data === "string"
            ? data
            : (data?.message ??
                data?.detail ??
                data?.title ??
                "No fue posible reasignar el responsable."),
        );
      }
      busy.current = false;
      setPending(false);
      return;
    }
    toast.success("Responsable actualizado. El motivo quedó registrado.");
    onClose();
    await Promise.allSettled([
      queryClient.invalidateQueries({ queryKey: ["cremations"] }),
    ]);
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
        "button:not(:disabled), select:not(:disabled), textarea:not(:disabled)",
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
    } else if (!event.shiftKey && document.activeElement === last) {
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
        La reasignación de {cremation.petName} está pendiente de confirmación.
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

  const title =
    cremation.status === CremationStatus.Delivered
      ? "Corrección de responsable"
      : "Reasignar responsable";
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
        aria-labelledby="reassignment-title"
        aria-describedby="reassignment-description"
        aria-busy={pending}
        tabIndex={-1}
        onKeyDown={trap}
        className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl"
      >
        <h2 id="reassignment-title" className="text-xl font-semibold">
          {title}
        </h2>
        <p
          id="reassignment-description"
          className="mt-2 text-sm text-slate-600"
        >
          {cremation.petName}: el responsable anterior y el motivo quedarán
          registrados. La confirmación original de inicio se conserva.
        </p>
        <p className="mt-3 text-sm">
          Responsable actual: {cremation.assignedToUserName ?? "Sin asignar"}
        </p>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (!uncertain && target && reason.trim())
              void send({
                requestId: crypto.randomUUID(),
                newAssignedToUserId: target,
                reason: reason.trim(),
              });
          }}
        >
          <label
            htmlFor="reassignment-user"
            className="mt-4 block text-sm font-medium"
          >
            Nuevo responsable
          </label>
          <select
            ref={select}
            id="reassignment-user"
            required
            value={target}
            disabled={pending || Boolean(uncertain) || loadingUsers}
            onChange={(event) => setTarget(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 p-2"
          >
            <option value="">
              {loadingUsers
                ? "Cargando usuarios..."
                : "Selecciona un responsable"}
            </option>
            {users
              .filter((user) => user.id !== cremation.assignedToUserId)
              .map((user) => (
                <option key={user.id} value={user.id}>
                  {user.name}
                </option>
              ))}
          </select>
          <label
            htmlFor="reassignment-reason"
            className="mt-4 block text-sm font-medium"
          >
            Motivo de la reasignación
          </label>
          <textarea
            id="reassignment-reason"
            required
            maxLength={1000}
            value={reason}
            disabled={pending || Boolean(uncertain)}
            onChange={(event) => setReason(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 p-2"
            rows={3}
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
                disabled={pending || loadingUsers || !target || !reason.trim()}
                className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:opacity-50"
              >
                {pending ? "Guardando..." : title}
              </button>
            )}
          </div>
        </form>
      </section>
    </div>
  );
}
