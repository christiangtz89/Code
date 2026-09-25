import { PaymentStatus, type PaymentAccount } from "../types/payment.types";
import {
  formatPaymentCurrency,
  formatPaymentDateTime,
} from "../utils/paymentDisplay";
import { getPaymentStatusLabel } from "../utils/paymentLabels";

interface PaymentSummaryCardProps {
  account: PaymentAccount;
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

export function PaymentSummaryCard({ account }: PaymentSummaryCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Estado de pago</p>

          <span
            className={[
              "mt-2 inline-flex rounded-full px-3 py-1 text-xs font-semibold",
              getStatusClasses(account.status),
            ].join(" ")}
          >
            {getPaymentStatusLabel(account.status)}
          </span>
        </div>

        <div className="text-left sm:text-right">
          <p className="text-sm text-slate-500">Saldo pendiente</p>

          <p className="mt-1 text-2xl font-semibold text-slate-900">
            {formatPaymentCurrency(account.balance)}
          </p>
        </div>
      </div>

      {account.requiredCollectionPaymentAmount !== null && (
        <div
          className={`mt-5 rounded-xl border px-4 py-3 text-sm ${
            account.isCollectionPaymentSatisfied
              ? "border-emerald-200 bg-emerald-50 text-emerald-700"
              : "border-amber-200 bg-amber-50 text-amber-800"
          }`}
        >
          Pago requerido para recepción:{" "}
          <strong>
            {formatPaymentCurrency(account.requiredCollectionPaymentAmount)}
          </strong>
          {account.isCollectionPaymentSatisfied
            ? " — cubierto"
            : " — pendiente"}
        </div>
      )}
      {account.isPricingProvisional && (
        <div className="mt-5 rounded-xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm font-medium text-sky-800">
          Pendiente de confirmación de peso.
        </div>
      )}
      {account.requiresFinancialReview &&
        !account.isFinancialReviewResolved && (
          <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            Revisión financiera pendiente. Los pagos exceden el total final por{" "}
            <strong>{formatPaymentCurrency(account.overpaymentAmount)}</strong>.
            Se requiere resolución de propietario o administración antes de
            continuar. No se realizará un reembolso ni se generará crédito
            automáticamente.
          </div>
        )}
      {account.requiresFinancialReview && account.isFinancialReviewResolved && (
        <div className="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          <p className="font-semibold">Revisión financiera resuelta.</p>
          <p className="mt-1">
            Resuelta por {account.financialReviewResolvedByUserName} el{" "}
            {account.financialReviewResolvedAt
              ? formatPaymentDateTime(account.financialReviewResolvedAt)
              : "—"}
            .
          </p>
          <p className="mt-1 whitespace-pre-wrap">
            Motivo: {account.financialReviewResolutionReason}
          </p>
        </div>
      )}

      <div className="mt-5 grid gap-4 border-t border-slate-200 pt-5 sm:grid-cols-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total del servicio
          </p>

          <p className="mt-1 font-semibold text-slate-900">
            {formatPaymentCurrency(account.serviceTotal)}
          </p>
        </div>

        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total pagado
          </p>

          <p className="mt-1 font-semibold text-slate-900">
            {formatPaymentCurrency(account.amountPaid)}
          </p>
        </div>

        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
            Saldo
          </p>

          <p className="mt-1 font-semibold text-slate-900">
            {formatPaymentCurrency(account.balance)}
          </p>
        </div>
      </div>
    </div>
  );
}
