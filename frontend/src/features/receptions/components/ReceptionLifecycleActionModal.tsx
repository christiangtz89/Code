import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";
import type { Reception } from "../types/reception.types";

export type ReceptionLifecycleActionMode = "deactivate" | "restore" | "request";

interface ReceptionLifecycleActionModalProps {
  isOpen: boolean;
  mode: ReceptionLifecycleActionMode;
  reception: Reception | null;
  isSubmitting: boolean;
  serverError: string | null;
  blockedDependencies: string[];
  onReasonChange: () => void;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}

const modalCopy: Record<
  ReceptionLifecycleActionMode,
  {
    eyebrow: string;
    title: string;
    explanation: string;
    submit: string;
    submitting: string;
  }
> = {
  deactivate: {
    eyebrow: "Decisión administrativa",
    title: "Desactivar recepción",
    explanation:
      "Úsala únicamente para una recepción errónea o duplicada que todavía no tenga uso operativo significativo. Las dependencias existentes impedirán la desactivación.",
    submit: "Desactivar recepción",
    submitting: "Desactivando...",
  },
  restore: {
    eyebrow: "Decisión administrativa",
    title: "Restaurar recepción",
    explanation:
      "La recepción volverá a mostrarse como activa. Esta acción no elimina ni modifica su historial operativo.",
    submit: "Restaurar recepción",
    submitting: "Restaurando...",
  },
  request: {
    eyebrow: "Solicitud de Manager",
    title: "Solicitar desactivación",
    explanation:
      "La solicitud quedará registrada, pero la recepción permanecerá activa. Owner o Admin deberá revisarla de forma independiente.",
    submit: "Registrar solicitud",
    submitting: "Registrando...",
  },
};

export function ReceptionLifecycleActionModal({
  isOpen,
  mode,
  reception,
  isSubmitting,
  serverError,
  blockedDependencies,
  onReasonChange,
  onClose,
  onSubmit,
}: ReceptionLifecycleActionModalProps) {
  const [reason, setReason] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const dialogRef = useRef<HTMLElement>(null);
  const reasonRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    if (isOpen) {
      setReason("");
      setValidationError(null);
    }
  }, [isOpen, mode, reception?.id]);

  useEffect(() => {
    if (!isOpen) return;

    const previouslyFocused =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    reasonRef.current?.focus();

    return () => previouslyFocused?.focus();
  }, [isOpen, mode, reception?.id]);

  useEffect(() => {
    if (isOpen && isSubmitting) {
      dialogRef.current?.focus();
    }
  }, [isOpen, isSubmitting]);

  if (!isOpen || reception === null) {
    return null;
  }

  const copy = modalCopy[mode];

  function handleDialogKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape" && !isSubmitting) {
      event.preventDefault();
      onClose();
      return;
    }

    if (event.key !== "Tab" || dialogRef.current === null) return;

    const focusable = Array.from(
      dialogRef.current.querySelectorAll<HTMLElement>(
        'button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [href], [tabindex]:not([tabindex="-1"])',
      ),
    );
    const first = focusable[0];
    const last = focusable.at(-1);

    if (!first || !last) {
      event.preventDefault();
      dialogRef.current.focus();
      return;
    }

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedReason = reason.trim();

    if (!normalizedReason) {
      setValidationError("El motivo es obligatorio.");
      return;
    }

    if (normalizedReason.length > 1000) {
      setValidationError("El motivo no puede exceder 1000 caracteres.");
      return;
    }

    setValidationError(null);
    await onSubmit(normalizedReason);
  }

  return (
    <div
      className="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={() => {
        if (!isSubmitting) onClose();
      }}
    >
      <section
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-lifecycle-action-title"
        className="w-full max-w-xl rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
        onKeyDown={handleDialogKeyDown}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">{copy.eyebrow}</p>
            <h2
              id="reception-lifecycle-action-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {copy.title}
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              {reception.petName} · {reception.customerName} ·{" "}
              {reception.qrCode}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label={`Cerrar ${copy.title.toLowerCase()}`}
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form className="space-y-5 px-6 py-6" onSubmit={handleSubmit}>
          <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
            {copy.explanation}
          </div>

          <div>
            <label
              htmlFor="reception-lifecycle-reason"
              className="block text-sm font-medium text-slate-700"
            >
              Motivo
            </label>
            <textarea
              ref={reasonRef}
              id="reception-lifecycle-reason"
              rows={5}
              maxLength={1000}
              value={reason}
              disabled={isSubmitting}
              onChange={(event) => {
                setReason(event.target.value);
                setValidationError(null);
                onReasonChange();
              }}
              placeholder="Describe el motivo de esta acción..."
              aria-invalid={validationError ? true : undefined}
              aria-describedby={
                validationError
                  ? "reception-lifecycle-reason-help reception-lifecycle-reason-error"
                  : "reception-lifecycle-reason-help"
              }
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />
            <div
              id="reception-lifecycle-reason-help"
              className="mt-2 flex items-start justify-between gap-4 text-xs text-slate-500"
            >
              <span>Obligatorio. Se conservará en el historial inmutable.</span>
              <span>{reason.length}/1000</span>
            </div>
            {validationError && (
              <p
                id="reception-lifecycle-reason-error"
                className="mt-2 text-sm text-red-600"
              >
                {validationError}
              </p>
            )}
          </div>

          {serverError && (
            <div
              className="rounded-xl border border-red-200 bg-red-50 px-4 py-3"
              role="alert"
            >
              <p className="font-medium text-red-800">{serverError}</p>
              {blockedDependencies.length > 0 && (
                <div className="mt-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-red-700">
                    Actividad vinculada
                  </p>
                  <ul className="mt-2 flex flex-wrap gap-2">
                    {blockedDependencies.map((dependency) => (
                      <li
                        key={dependency}
                        className="rounded-full border border-red-200 bg-white px-2.5 py-1 text-xs font-medium text-red-700"
                      >
                        {dependency}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {mode === "deactivate" && (
                <p className="mt-3 text-sm text-red-700">
                  No existe una opción para omitir estas dependencias.
                </p>
              )}
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
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? copy.submitting : copy.submit}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
