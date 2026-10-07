import { useEffect, useRef, type KeyboardEvent } from "react";
import { formatCurrency } from "../../cremation-pricing/utils";
import type {
  Cremation,
  RevalidateStartPaymentResult,
} from "../types/cremation.types";

interface Props {
  cremation: Cremation;
  preview: RevalidateStartPaymentResult | null;
  isPending: boolean;
  isUncertain: boolean;
  returnFocus: HTMLElement | null;
  onClose: () => void;
  onConfirm: () => void;
  onRetry: () => void;
}

export function StartPaymentRevalidationModal({
  cremation,
  preview,
  isPending,
  isUncertain,
  returnFocus,
  onClose,
  onConfirm,
  onRetry,
}: Props) {
  const dialogRef = useRef<HTMLElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    closeRef.current?.focus();
    return () => {
      // Wait for the trigger to be enabled again (or removed after success).
      requestAnimationFrame(() => {
        if (dialog?.isConnected) return;
        if (returnFocus?.isConnected && !returnFocus.matches(":disabled"))
          returnFocus.focus();
        else document.getElementById("cremations-title")?.focus();
      });
    };
  }, [returnFocus]);

  useEffect(() => {
    if (isPending) dialogRef.current?.focus();
  }, [isPending]);

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      if (!isPending) onClose();
      return;
    }
    if (event.key !== "Tab") return;
    const buttons = dialogRef.current?.querySelectorAll<HTMLButtonElement>(
      "button:not([disabled])",
    );
    const first = buttons?.[0];
    const last = buttons?.[buttons.length - 1];
    if (!first || !last) {
      event.preventDefault();
      dialogRef.current?.focus();
    } else if (document.activeElement === dialogRef.current) {
      event.preventDefault();
      (event.shiftKey ? last : first).focus();
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
      className="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={() => {
        if (!isPending) onClose();
      }}
    >
      <section
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby="revalidation-title"
        aria-describedby="revalidation-description"
        aria-busy={isPending}
        onKeyDown={handleKeyDown}
        onMouseDown={(event) => event.stopPropagation()}
        className="max-h-full w-full max-w-xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
      >
        <header className="border-b border-slate-200 px-6 py-5">
          <h2
            id="revalidation-title"
            className="text-xl font-semibold text-slate-900"
          >
            {isUncertain
              ? "Verificar resultado de la validación"
              : "Confirmar actualización de precio"}
          </h2>
          <p className="mt-2 text-sm text-slate-600">
            {cremation.petName} · {cremation.qrCode}
          </p>
        </header>
        <div className="space-y-5 px-6 py-5">
          <p
            id="revalidation-description"
            className="text-sm text-slate-700"
            role={isUncertain ? "alert" : undefined}
          >
            {isUncertain
              ? "No pudimos confirmar el resultado. Reintenta para verificar si la validación se completó. Conservaremos esta solicitud aunque cierres el diálogo."
              : "La información actual cambió y requiere tu confirmación antes de validar el pago requerido."}
          </p>
          {preview && (
            <>
              <dl className="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm">
                <div className="flex justify-between gap-4">
                  <dt>Cotización actual</dt>
                  <dd className="font-semibold">
                    {preview.previousQuotedPrice === null
                      ? "Sin cotización registrada"
                      : formatCurrency(preview.previousQuotedPrice)}
                  </dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt>Nueva cotización</dt>
                  <dd className="font-semibold">
                    {formatCurrency(preview.newQuotedPrice)}
                  </dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt>Pago requerido</dt>
                  <dd className="font-semibold">
                    {formatCurrency(preview.requiredStartPaymentAmount)}
                  </dd>
                </div>
              </dl>
              {preview.previousPaymentAccountServiceTotal !== null && (
                <section
                  aria-labelledby="revalidation-account-title"
                  className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-slate-900"
                >
                  <h3 id="revalidation-account-title" className="font-semibold">
                    Cuenta de pago
                  </h3>
                  <dl className="mt-3 space-y-2">
                    <div className="flex justify-between gap-4">
                      <dt>Total actual</dt>
                      <dd>
                        {formatCurrency(
                          preview.previousPaymentAccountServiceTotal,
                        )}
                      </dd>
                    </div>
                    <div className="flex justify-between gap-4">
                      <dt>Nuevo total</dt>
                      <dd className="font-semibold">
                        {formatCurrency(preview.newQuotedPrice)}
                      </dd>
                    </div>
                  </dl>
                </section>
              )}
            </>
          )}
        </div>
        <footer className="flex flex-wrap justify-end gap-3 border-t border-slate-200 px-6 py-5">
          <button
            ref={closeRef}
            type="button"
            disabled={isPending}
            onClick={onClose}
            className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            {isUncertain ? "Cerrar" : "Cancelar"}
          </button>
          <button
            type="button"
            disabled={isPending}
            onClick={isUncertain ? onRetry : onConfirm}
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {isPending
              ? "Validando..."
              : isUncertain
                ? "Reintentar validación"
                : "Confirmar y validar"}
          </button>
        </footer>
      </section>
    </div>
  );
}
