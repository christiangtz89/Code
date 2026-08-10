import type { PaymentAccount } from "../types/payment.types";
import { PaymentHistory } from "./PaymentHistory";
import { PaymentSummaryCard } from "./PaymentSummaryCard";

interface PaymentDetailsModalProps {
  isOpen: boolean;
  account: PaymentAccount | null;
  onClose: () => void;
}

export function PaymentDetailsModal({
  isOpen,
  account,
  onClose,
}: PaymentDetailsModalProps) {
  if (!isOpen || !account) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-slate-50 shadow-xl">
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              Detalle de pagos
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Historial financiero del servicio.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg px-3 py-2 text-sm font-medium text-slate-500 transition hover:bg-slate-100 hover:text-slate-900"
          >
            Cerrar
          </button>
        </header>

        <div className="space-y-5 p-6">
          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <p className="text-lg font-semibold text-slate-900">
              {account.petName}
            </p>

            <p className="mt-1 text-sm text-slate-600">
              {account.customerName}
            </p>

            <div className="mt-4 grid gap-3 border-t border-slate-200 pt-4 text-sm sm:grid-cols-3">
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  QR
                </p>

                <p className="mt-1 font-medium text-slate-800">
                  {account.qrCode}
                </p>
              </div>

              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Paquete
                </p>

                <p className="mt-1 font-medium text-slate-800">
                  {account.packageName}
                </p>
              </div>

              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Pagos registrados
                </p>

                <p className="mt-1 font-medium text-slate-800">
                  {account.payments.length}
                </p>
              </div>
            </div>
          </div>

          <PaymentSummaryCard account={account} />

          <PaymentHistory payments={account.payments} />
        </div>
      </div>
    </div>
  );
}
