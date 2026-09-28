import { useEffect, useState, type FormEvent } from "react";
import type { Reception } from "../types/reception.types";

interface ReceptionClarificationModalProps {
  isOpen: boolean;
  reception: Reception | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (text: string) => Promise<void>;
}

export function ReceptionClarificationModal({
  isOpen,
  reception,
  isSubmitting,
  onClose,
  onSubmit,
}: ReceptionClarificationModalProps) {
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setText("");
      setError(null);
    }
  }, [isOpen, reception?.id]);

  if (!isOpen || reception === null) {
    return null;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalized = text.trim();
    if (!normalized) {
      setError("El texto de la aclaración es obligatorio.");
      return;
    }
    if (normalized.length > 1000) {
      setError("La aclaración no puede exceder 1000 caracteres.");
      return;
    }

    setError(null);
    await onSubmit(normalized);
  }

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={() => {
        if (!isSubmitting) onClose();
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-clarification-title"
        className="w-full max-w-xl rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Nota adicional e inmutable
            </p>
            <h2
              id="reception-clarification-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Agregar aclaración
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              {reception.petName} · {reception.customerName}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar aclaración"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form className="space-y-5 px-6 py-6" onSubmit={handleSubmit}>
          <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
            <p className="text-sm font-medium text-slate-800">
              Notas originales de la recepción
            </p>
            <p className="mt-1 whitespace-pre-wrap text-sm text-slate-600">
              {reception.notes?.trim() || "Sin notas originales."}
            </p>
          </div>

          <div>
            <label
              htmlFor="reception-clarification-text"
              className="block text-sm font-medium text-slate-700"
            >
              Nueva aclaración
            </label>
            <textarea
              id="reception-clarification-text"
              rows={5}
              maxLength={1000}
              value={text}
              disabled={isSubmitting}
              onChange={(event) => {
                setText(event.target.value);
                setError(null);
              }}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />
            <p className="mt-2 text-xs text-slate-500">
              La aclaración se agregará al historial; no reemplaza las notas
              originales.
            </p>
            {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
          </div>

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
              {isSubmitting ? "Agregando..." : "Agregar aclaración"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
