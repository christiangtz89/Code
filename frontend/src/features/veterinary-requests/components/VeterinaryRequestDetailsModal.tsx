import type { VeterinaryRequest } from "../types/veterinaryRequest.types";
import {
  formatVeterinaryRequestDate,
  formatVeterinaryRequestDateTime,
} from "../utils/veterinaryRequestDisplay";
import { getCremationTypeLabel } from "../utils/veterinaryRequestLabels";
import { VeterinaryRequestStatusBadge } from "./VeterinaryRequestStatusBadge";

interface VeterinaryRequestDetailsModalProps {
  isOpen: boolean;
  request: VeterinaryRequest | null;
  onClose: () => void;
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
        {label}
      </p>

      <p className="mt-1 text-sm font-medium text-slate-800">{value}</p>
    </div>
  );
}

export function VeterinaryRequestDetailsModal({
  isOpen,
  request,
  onClose,
}: VeterinaryRequestDetailsModalProps) {
  if (!isOpen || !request) {
    return null;
  }

  const ownerName = [
    request.ownerFirstName,
    request.ownerLastName,
    request.ownerSecondLastName,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[90vh] w-full max-w-5xl overflow-y-auto rounded-2xl bg-slate-50 shadow-xl">
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <div className="flex flex-wrap items-center gap-3">
              <h2 className="text-xl font-semibold text-slate-900">
                Solicitud veterinaria
              </h2>

              <VeterinaryRequestStatusBadge status={request.status} />
            </div>

            <p className="mt-1 text-sm text-slate-500">
              {request.petName} · {ownerName}
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
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="font-semibold text-slate-900">
              Referencia veterinaria
            </h3>

            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <DetailItem
                label="Veterinaria"
                value={request.veterinaryClinicName ?? "Sin veterinaria"}
              />

              <DetailItem
                label="Veterinario referente"
                value={request.referringVeterinarianName ?? "Sin veterinario"}
              />
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="font-semibold text-slate-900">Propietario</h3>

            <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <DetailItem label="Nombre" value={ownerName} />

              <DetailItem label="Teléfono" value={request.ownerPhone} />

              <DetailItem
                label="Correo"
                value={request.ownerEmail ?? "No registrado"}
              />
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="font-semibold text-slate-900">Mascota</h3>

            <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <DetailItem label="Nombre" value={request.petName} />

              <DetailItem label="Especie" value={request.species} />

              <DetailItem label="Raza" value={request.breed} />

              <DetailItem label="Sexo" value={request.sex} />

              <DetailItem label="Color" value={request.color} />

              <DetailItem
                label="Peso aproximado"
                value={`${request.approximateWeightKg} kg`}
              />

              <DetailItem
                label="Edad"
                value={
                  request.ageYears === null
                    ? "No registrada"
                    : `${request.ageYears} años`
                }
              />

              <DetailItem
                label="Fecha de fallecimiento"
                value={formatVeterinaryRequestDate(request.dateOfDeath)}
              />
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="font-semibold text-slate-900">
              Servicio solicitado
            </h3>

            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <DetailItem
                label="Tipo de cremación"
                value={getCremationTypeLabel(request.requestedCremationType)}
              />

              <DetailItem
                label="Paquete solicitado"
                value={request.requestedPackageName ?? "No especificado"}
              />
            </div>

            <div className="mt-5">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Notas de la solicitud
              </p>

              <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">
                {request.requestNotes ?? "Sin notas"}
              </p>
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="font-semibold text-slate-900">
              Seguimiento interno
            </h3>

            <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <DetailItem
                label="Registrada por"
                value={request.submittedByUserName}
              />

              <DetailItem
                label="Fecha de solicitud"
                value={formatVeterinaryRequestDateTime(request.submittedAt)}
              />

              <DetailItem
                label="Revisada por"
                value={request.reviewedByUserName ?? "No revisada"}
              />

              <DetailItem
                label="Fecha de revisión"
                value={formatVeterinaryRequestDateTime(request.reviewedAt)}
              />
            </div>

            <div className="mt-5">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Notas internas
              </p>

              <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">
                {request.internalNotes ?? "Sin notas internas"}
              </p>
            </div>

            {request.rejectionReason && (
              <div className="mt-5 rounded-xl border border-red-200 bg-red-50 p-4">
                <p className="text-xs font-semibold uppercase tracking-wide text-red-600">
                  Motivo de rechazo
                </p>

                <p className="mt-2 text-sm text-red-800">
                  {request.rejectionReason}
                </p>
              </div>
            )}
          </section>

          {request.receptionId && (
            <section className="rounded-2xl border border-violet-200 bg-violet-50 p-5">
              <h3 className="font-semibold text-violet-900">
                Recepción generada
              </h3>

              <div className="mt-4 grid gap-4 sm:grid-cols-3">
                <DetailItem
                  label="QR"
                  value={request.receptionQrCode ?? "No registrado"}
                />

                <DetailItem
                  label="Fecha de conversión"
                  value={formatVeterinaryRequestDateTime(request.convertedAt)}
                />

                <DetailItem label="Recepción" value={request.receptionId} />
              </div>
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
