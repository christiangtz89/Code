import { PaymentStatus, type PaymentAccount } from "../types/payment.types";
import { formatPaymentCurrency } from "../utils/paymentDisplay";
import { getPaymentStatusLabel } from "../utils/paymentLabels";

interface PaymentsTableProps {
  accounts: PaymentAccount[];
  onViewPayments: (account: PaymentAccount) => void;
  onAddPayment: (account: PaymentAccount) => void;
}

function getStatusClasses(status: PaymentAccount["status"]): string {
  switch (status) {
    case PaymentStatus.Paid:
      return "bg-emerald-100 text-emerald-700";

    case PaymentStatus.PartiallyPaid:
      return "bg-amber-100 text-amber-700";

    case PaymentStatus.Pending:
    default:
      return "bg-slate-100 text-slate-700";
  }
}

export function PaymentsTable({
  accounts,
  onViewPayments,
  onAddPayment,
}: PaymentsTableProps) {
  if (accounts.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center">
        <p className="font-medium text-slate-700">
          No se encontraron cuentas de pago
        </p>

        <p className="mt-1 text-sm text-slate-500">
          Las cuentas registradas aparecerán aquí.
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Servicio
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Cliente
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                Total
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                Pagado
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                Saldo
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Estado
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                Acciones
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-200 bg-white">
            {accounts.map((account) => {
              const isPaid =
                account.status === PaymentStatus.Paid || account.balance <= 0;

              const canAddPayment = !isPaid && account.isCremationActive;

              return (
                <tr key={account.id} className="align-top hover:bg-slate-50">
                  <td className="px-5 py-4">
                    <div className="min-w-52">
                      <p className="font-semibold text-slate-900">
                        {account.petName}
                      </p>

                      <p className="mt-1 text-sm text-slate-600">
                        {account.packageName}
                      </p>

                      <p className="mt-1 text-xs font-medium text-slate-500">
                        QR: {account.qrCode}
                      </p>

                      {account.requiredCollectionPaymentAmount !== null && (
                        <p
                          className={`mt-2 text-xs font-semibold ${
                            account.isCollectionPaymentSatisfied
                              ? "text-emerald-700"
                              : "text-amber-700"
                          }`}
                        >
                          Pago para recepción{" "}
                          {account.isCollectionPaymentSatisfied
                            ? "cubierto"
                            : "pendiente"}
                        </p>
                      )}
                    </div>
                  </td>

                  <td className="px-5 py-4">
                    <p className="min-w-44 text-sm font-medium text-slate-800">
                      {account.customerName}
                    </p>
                  </td>

                  <td className="px-5 py-4 text-right">
                    <span className="whitespace-nowrap text-sm font-semibold text-slate-900">
                      {formatPaymentCurrency(account.serviceTotal)}
                    </span>
                  </td>

                  <td className="px-5 py-4 text-right">
                    <span className="whitespace-nowrap text-sm font-medium text-slate-700">
                      {formatPaymentCurrency(account.amountPaid)}
                    </span>
                  </td>

                  <td className="px-5 py-4 text-right">
                    <span
                      className={[
                        "whitespace-nowrap text-sm font-semibold",
                        isPaid ? "text-emerald-700" : "text-slate-900",
                      ].join(" ")}
                    >
                      {formatPaymentCurrency(account.balance)}
                    </span>
                  </td>

                  <td className="px-5 py-4">
                    <span
                      className={[
                        "inline-flex whitespace-nowrap rounded-full px-3 py-1 text-xs font-semibold",
                        getStatusClasses(account.status),
                      ].join(" ")}
                    >
                      {getPaymentStatusLabel(account.status)}
                    </span>
                  </td>

                  <td className="px-5 py-4">
                    <div className="flex min-w-40 flex-col items-stretch gap-2 sm:items-end">
                      <button
                        type="button"
                        onClick={() => onViewPayments(account)}
                        className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                      >
                        Ver pagos
                      </button>

                      {canAddPayment && (
                        <button
                          type="button"
                          onClick={() => onAddPayment(account)}
                          className="rounded-lg bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-slate-800"
                        >
                          Registrar pago
                        </button>
                      )}

                      {!account.isCremationActive && (
                        <span className="px-3 py-1 text-right text-xs font-medium text-slate-500">
                          Servicio inactivo
                        </span>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
