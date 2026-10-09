import axios from "axios";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import toast from "react-hot-toast";
import { amendCremationInstructions, getCremationById, getCremationInstructionsAmendments } from "../api/cremationsApi";
import { getCremationStatusLabel } from "../utils/cremationLabels";
import {
  CremationStatus,
  type AmendCremationInstructionsPayload,
  type Cremation,
} from "../types/cremation.types";

interface Props {
  cremation: Cremation;
  origin: HTMLElement | null;
  onClose: () => void;
}

export function CremationInstructionsAmendmentModal({ cremation, origin, onClose }: Props) {
  const queryClient = useQueryClient();
  const amendmentsQuery = useQuery({
    queryKey: ["cremations", cremation.id, "instructions-amendments"],
    queryFn: () => getCremationInstructionsAmendments(cremation.id),
  });
  const [instructions, setInstructions] = useState(cremation.specialInstructions ?? "");
  const [currentInstructions, setCurrentInstructions] = useState(cremation.specialInstructions);
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [uncertain, setUncertain] = useState<AmendCremationInstructionsPayload | null>(null);
  const [open, setOpen] = useState(true);
  const busy = useRef(false);
  const dialog = useRef<HTMLElement>(null);
  const newField = useRef<HTMLTextAreaElement>(null);
  const retry = useRef<HTMLButtonElement>(null);
  const returnFocus = useRef(origin);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() => (retry.current ?? newField.current ?? dialog.current)?.focus());
    const element = dialog.current;
    return () => {
      cancelAnimationFrame(frame);
      requestAnimationFrame(() => {
        if (element?.isConnected) return;
        const previous = returnFocus.current;
        if (previous?.isConnected && !previous.matches(":disabled")) previous.focus();
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
      else if (!element.contains(document.activeElement) || document.activeElement === element)
        (retry.current ?? newField.current ?? element).focus();
    });
    return () => cancelAnimationFrame(frame);
  }, [open, pending, uncertain]);

  function close() {
    if (busy.current) return;
    if (uncertain) setOpen(false);
    else onClose();
  }

  async function send(request: AmendCremationInstructionsPayload) {
    if (busy.current) return;
    busy.current = true;
    setPending(true);
    setError("");
    try {
      await amendCremationInstructions(cremation.id, request);
    } catch (failure) {
      const status = axios.isAxiosError(failure) ? failure.response?.status : null;
      if (!status || status >= 500 || status === 408) {
        setUncertain(request);
        setError("No se pudo confirmar si el cambio se completó. Reintenta con la misma solicitud.");
      } else {
        const data = axios.isAxiosError(failure) ? failure.response?.data : null;
        setError(typeof data === "string" ? data :
          (data?.message ?? data?.detail ?? data?.title ?? "No fue posible cambiar las instrucciones."));
        if (status === 409 && !uncertain) {
          try {
            const latest = await getCremationById(cremation.id);
            setCurrentInstructions(latest.specialInstructions);
            await queryClient.invalidateQueries({ queryKey: ["cremations", cremation.id, "instructions-amendments"] });
          } catch {
            // The server error remains readable; the user can reopen the dialog.
          }
        }
      }
      busy.current = false;
      setPending(false);
      return;
    }
    toast.success("Instrucciones actualizadas. El motivo quedó registrado.");
    onClose();
    await queryClient.invalidateQueries({ queryKey: ["cremations"] });
  }

  function trap(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      close();
      return;
    }
    if (event.key !== "Tab") return;
    const controls = Array.from(dialog.current?.querySelectorAll<HTMLElement>(
      "button:not(:disabled), textarea:not(:disabled), summary",
    ) ?? []);
    const first = controls[0];
    const last = controls.at(-1);
    if (!first) {
      event.preventDefault();
      dialog.current?.focus();
    } else if (event.shiftKey && (document.activeElement === first || document.activeElement === dialog.current)) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  if (!open) return (
    <div role="status" className="rounded-xl border border-amber-300 bg-amber-50 p-4">
      El cambio de instrucciones de {cremation.petName} está pendiente de confirmación.
      <button type="button" onClick={() => {
        returnFocus.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        setOpen(true);
      }} className="ml-3 underline">Retomar solicitud</button>
    </div>
  );

  const title = cremation.status === CremationStatus.Delivered
    ? "Corregir instrucciones" : "Modificar instrucciones";
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
      onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}>
      <section ref={dialog} role="dialog" aria-modal="true" aria-labelledby="instructions-amendment-title"
        aria-describedby="instructions-amendment-description" aria-busy={pending} tabIndex={-1}
        onKeyDown={trap} className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl">
        <h2 id="instructions-amendment-title" className="text-xl font-semibold">{title}</h2>
        <p id="instructions-amendment-description" className="mt-2 text-sm text-slate-600">
          Esta cremación ya inició. Los cambios a las instrucciones quedan registrados.
        </p>
        <p className="mt-4 text-sm font-medium">Instrucciones actuales</p>
        <p className="mt-1 whitespace-pre-wrap rounded-lg bg-slate-50 p-3 text-sm text-slate-700">
          {currentInstructions ?? "Sin instrucciones especiales"}
        </p>
        <form onSubmit={(event) => {
          event.preventDefault();
          if (!uncertain && reason.trim() && instructions.trim() !== (currentInstructions ?? ""))
            void send({ requestId: crypto.randomUUID(), newSpecialInstructions: instructions.trim() || null,
              expectedCurrentSpecialInstructions: currentInstructions,
              reason: reason.trim() });
        }}>
          <label htmlFor="instructions-amendment-new" className="mt-4 block text-sm font-medium">Nuevas instrucciones</label>
          <textarea ref={newField} id="instructions-amendment-new" rows={4} maxLength={1000}
            value={instructions} disabled={pending || Boolean(uncertain)}
            onChange={(event) => setInstructions(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 p-2" />
          <p className="text-xs text-slate-500">Deja este campo vacío para retirar las instrucciones.</p>
          <label htmlFor="instructions-amendment-reason" className="mt-4 block text-sm font-medium">Motivo del cambio *</label>
          <textarea id="instructions-amendment-reason" rows={3} required maxLength={1000}
            value={reason} disabled={pending || Boolean(uncertain)}
            onChange={(event) => setReason(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 p-2" />
          {uncertain && <p role="status" className="mt-3 text-sm text-amber-800">
            La solicitud sigue pendiente de confirmación. Se conservarán sus datos al reintentar.
          </p>}
          {error && <p role="alert" className="mt-3 text-sm text-red-700">{error}</p>}
          <div className="mt-5 flex justify-end gap-3">
            <button type="button" disabled={pending} onClick={close} className="rounded-lg border px-4 py-2">
              {uncertain ? "Cerrar" : "Cancelar"}
            </button>
            {uncertain ? <button ref={retry} type="button" disabled={pending} onClick={() => void send(uncertain)}
              className="rounded-lg bg-slate-900 px-4 py-2 text-white">
              {pending ? "Confirmando..." : "Reintentar"}
            </button> : <button type="submit"
              disabled={pending || !reason.trim() || instructions.trim() === (currentInstructions ?? "")}
              className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:opacity-50">
              {pending ? "Guardando..." : title}
            </button>}
          </div>
        </form>
        <details className="mt-5 border-t border-slate-200 pt-4">
          <summary className="cursor-pointer text-sm font-medium">Historial de cambios de instrucciones</summary>
          {amendmentsQuery.isLoading ? (
            <p className="mt-2 text-sm text-slate-600">Cargando historial...</p>
          ) : amendmentsQuery.isError ? (
            <p role="alert" className="mt-2 text-sm text-red-700">No fue posible cargar el historial.</p>
          ) : amendmentsQuery.data?.length ? (
            <ol className="mt-3 max-h-52 space-y-3 overflow-y-auto text-sm">
              {amendmentsQuery.data.map((amendment) => (
                <li key={amendment.id} className="rounded-lg border border-slate-200 p-3">
                  <p className="font-medium">Cambio {amendment.sequence} · {new Date(amendment.createdAt).toLocaleString("es-MX")}</p>
                  <p>De: {amendment.previousSpecialInstructions ?? "Sin instrucciones"}</p>
                  <p>A: {amendment.newSpecialInstructions ?? "Sin instrucciones"}</p>
                  <p>Por: {amendment.actorNameSnapshot} ({amendment.actorRole})</p>
                  <p>Estado: {getCremationStatusLabel(amendment.status)}</p>
                  <p>Motivo: {amendment.reason}</p>
                </li>
              ))}
            </ol>
          ) : (
            <p className="mt-2 text-sm text-slate-600">Aún no hay cambios registrados.</p>
          )}
        </details>
      </section>
    </div>
  );
}
