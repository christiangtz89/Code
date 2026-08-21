import type { WeightRangeChangeDetails } from "../types/reception.types";

interface WeightRangeChangeConfirmationModalProps {
  petName: string;
  customerName: string;
  qrCode: string;

  details: WeightRangeChangeDetails;

  isSubmitting: boolean;

  onCancel: () => void;
  onConfirm: () => void;
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("es-MX", {
    style: "currency",
    currency: "MXN",
  }).format(value);
}

function formatWeight(value: number): string {
  return `${value.toFixed(2)} kg`;
}

function formatRange(minimum: number, maximum: number): string {
  return `${minimum.toFixed(2)}–${maximum.toFixed(2)} kg`;
}

export function WeightRangeChangeConfirmationModal({
  petName,
  customerName,
  qrCode,
  details,
  isSubmitting,
  onCancel,
  onConfirm,
}: WeightRangeChangeConfirmationModalProps) {
  return (
    <div
      className="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/70 px-4 py-8"
      role="presentation"
      onMouseDown={() => {
        if (!isSubmitting) {
          onCancel();
        }
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="weight-range-confirmation-title"
        onMouseDown={(event) => event.stopPropagation()}
        className="max-h-full w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
      >
        <header className="border-b border-amber-200 bg-amber-50 px-6 py-5">
          <p className="text-sm font-semibold uppercase tracking-wide text-amber-700">
            Verificación requerida
          </p>

          <h2
            id="weight-range-confirmation-title"
            className="mt-1 text-xl font-semibold text-amber-950"
          >
            Cambio de rango de peso
          </h2>

          <p className="mt-2 text-sm text-amber-800">
            El nuevo peso está dentro de la tolerancia permitida de ±10%, pero
            modifica el rango utilizado para calcular el servicio.
          </p>
        </header>

        <div className="space-y-5 px-6 py-6">
          <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Verifica la mascota
            </p>

            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <div>
                <p className="text-xs text-slate-500">Mascota</p>
                <p className="font-semibold text-slate-900">{petName}</p>
              </div>

              <div>
                <p className="text-xs text-slate-500">Cliente</p>
                <p className="font-semibold text-slate-900">{customerName}</p>
              </div>
            </div>

            <div className="mt-3">
              <p className="text-xs text-slate-500">Código QR</p>
              <p className="mt-1 break-all font-mono text-sm font-medium text-slate-800">
                {qrCode}
              </p>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="rounded-xl border border-slate-200 p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Antes
              </p>

              <p className="mt-2 text-xl font-semibold text-slate-900">
                {formatWeight(details.previousWeightKg)}
              </p>

              <p className="mt-2 text-sm text-slate-600">
                Rango:{" "}
                <strong>
                  {formatRange(
                    details.previousMinimumWeightKg,
                    details.previousMaximumWeightKg,
                  )}
                </strong>
              </p>

              <p className="mt-2 text-sm text-slate-600">
                Cotización:{" "}
                <strong>
                  {details.previousPrice === null
                    ? "No disponible"
                    : formatCurrency(details.previousPrice)}
                </strong>
              </p>
            </div>

            <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-amber-700">
                Después
              </p>

              <p className="mt-2 text-xl font-semibold text-amber-950">
                {formatWeight(details.newWeightKg)}
              </p>

              <p className="mt-2 text-sm text-amber-800">
                Rango:{" "}
                <strong>
                  {formatRange(
                    details.newMinimumWeightKg,
                    details.newMaximumWeightKg,
                  )}
                </strong>
              </p>

              <p className="mt-2 text-sm text-amber-800">
                Nueva cotización:{" "}
                <strong>
                  {details.newPrice === null
                    ? "Se calculará al crear la cremación"
                    : formatCurrency(details.newPrice)}
                </strong>
              </p>
            </div>
          </div>

          <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
            <p className="text-sm font-medium text-amber-950">
              Confirma únicamente si verificaste físicamente el peso y
              corresponde a la misma mascota.
            </p>

            <p className="mt-1 text-xs text-amber-800">
              {details.newPrice === null
                ? "Al confirmar, PCMS actualizará el peso verificado. La cotización se calculará cuando se registre la cremación."
                : "Al confirmar, PCMS actualizará el peso verificado, la cotización y el total del servicio."}
            </p>
          </div>
        </div>

        <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 px-6 py-5 sm:flex-row sm:justify-end">
          <button
            type="button"
            disabled={isSubmitting}
            onClick={onCancel}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
          >
            Cancelar
          </button>

          <button
            type="button"
            disabled={isSubmitting}
            onClick={onConfirm}
            className="rounded-lg bg-amber-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-amber-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Actualizando..." : "Confirmar corrección"}
          </button>
        </footer>
      </section>
    </div>
  );
}
