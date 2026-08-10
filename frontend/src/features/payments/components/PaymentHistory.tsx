import type { Payment } from "../types/payment.types";
import {
  formatPaymentCurrency,
  formatPaymentDateTime,
} from "../utils/paymentDisplay";
import { getPaymentMethodLabel } from "../utils/paymentLabels";

interface PaymentHistoryProps {
  payments: Payment[];
}

export function PaymentHistory({ payments }: PaymentHistoryProps) {
  if (payments.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-10 text-center">
        <p className="font-medium text-slate-700">
          Aún no hay pagos registrados
        </p>

        <p className="mt-1 text-sm text-slate-500">
          Los anticipos y pagos parciales aparecerán aquí.
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-5 py-4">
        <h3 className="font-semibold text-slate-900">Historial de pagos</h3>

        <p className="mt-1 text-sm text-slate-500">
          {payments.length}{" "}
          {payments.length === 1 ? "pago registrado" : "pagos registrados"}
        </p>
      </div>

      <div className="divide-y divide-slate-200">
        {payments.map((payment) => (
          <article key={payment.id} className="p-5">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <p className="text-lg font-semibold text-slate-900">
                  {formatPaymentCurrency(payment.amount)}
                </p>

                <p className="mt-1 text-sm font-medium text-slate-600">
                  {getPaymentMethodLabel(payment.method)}
                </p>
              </div>

              <div className="sm:text-right">
                <p className="text-sm font-medium text-slate-700">
                  {formatPaymentDateTime(payment.paidAt)}
                </p>

                <p className="mt-1 text-xs text-slate-500">
                  Registrado en sistema:{" "}
                  {formatPaymentDateTime(payment.createdAt)}
                </p>
              </div>
            </div>

            <dl className="mt-4 grid gap-4 border-t border-slate-100 pt-4 sm:grid-cols-2">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Registrado por
                </dt>

                <dd className="mt-1 text-sm text-slate-800">
                  {payment.recordedByUserName}
                </dd>
              </div>

              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Referencia
                </dt>

                <dd className="mt-1 text-sm text-slate-800">
                  {payment.reference ?? "Sin referencia"}
                </dd>
              </div>
            </dl>

            {payment.notes && (
              <div className="mt-4 rounded-lg bg-slate-50 px-4 py-3">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Notas
                </p>

                <p className="mt-1 whitespace-pre-wrap text-sm text-slate-700">
                  {payment.notes}
                </p>
              </div>
            )}
          </article>
        ))}
      </div>
    </div>
  );
}
