import type { VeterinaryRequest } from "../types/veterinaryRequest.types";
import { formatVeterinaryRequestDateTime } from "../utils/veterinaryRequestDisplay";
import {
  canConvertVeterinaryRequest,
  canEditVeterinaryRequest,
  getAvailableVeterinaryRequestStatuses,
} from "../utils/veterinaryRequestWorkflow";
import { VeterinaryRequestStatusBadge } from "./VeterinaryRequestStatusBadge";

interface VeterinaryRequestsTableProps {
  requests: VeterinaryRequest[];
  onView: (request: VeterinaryRequest) => void;
  onEdit: (request: VeterinaryRequest) => void;
  onChangeStatus: (request: VeterinaryRequest) => void;
  onConvert: (request: VeterinaryRequest) => void;
}

export function VeterinaryRequestsTable({
  requests,
  onView,
  onEdit,
  onChangeStatus,
  onConvert,
}: VeterinaryRequestsTableProps) {
  if (requests.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center">
        <p className="font-medium text-slate-700">
          No se encontraron solicitudes veterinarias
        </p>

        <p className="mt-1 text-sm text-slate-500">
          Las solicitudes registradas aparecerán aquí.
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
                Mascota
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Propietario
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Referencia
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Estado
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                Fecha
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                Acciones
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-200 bg-white">
            {requests.map((request) => {
              const canEdit = canEditVeterinaryRequest(request.status);

              const canConvert = canConvertVeterinaryRequest(request.status);

              const canChangeStatus =
                getAvailableVeterinaryRequestStatuses(request.status).length >
                0;

              return (
                <tr
                  key={request.id}
                  className="align-top transition hover:bg-slate-50"
                >
                  <td className="px-5 py-4">
                    <div className="min-w-44">
                      <p className="font-semibold text-slate-900">
                        {request.petName}
                      </p>

                      <p className="mt-1 text-sm text-slate-600">
                        {request.species} · {request.breed}
                      </p>

                      <p className="mt-1 text-xs text-slate-500">
                        {request.approximateWeightKg} kg aprox.
                      </p>
                    </div>
                  </td>

                  <td className="px-5 py-4">
                    <div className="min-w-48">
                      <p className="text-sm font-medium text-slate-900">
                        {[
                          request.ownerFirstName,
                          request.ownerLastName,
                          request.ownerSecondLastName,
                        ]
                          .filter(Boolean)
                          .join(" ")}
                      </p>

                      <p className="mt-1 text-xs text-slate-500">
                        {request.ownerPhone}
                      </p>
                    </div>
                  </td>

                  <td className="px-5 py-4">
                    <div className="min-w-52">
                      {request.veterinaryClinicName && (
                        <p className="text-sm font-medium text-slate-800">
                          {request.veterinaryClinicName}
                        </p>
                      )}

                      {request.referringVeterinarianName && (
                        <p className="mt-1 text-xs text-slate-500">
                          {request.referringVeterinarianName}
                        </p>
                      )}

                      {!request.veterinaryClinicName &&
                        !request.referringVeterinarianName && (
                          <p className="text-sm text-slate-400">
                            Sin referencia
                          </p>
                        )}
                    </div>
                  </td>

                  <td className="px-5 py-4">
                    <VeterinaryRequestStatusBadge status={request.status} />
                  </td>

                  <td className="px-5 py-4">
                    <p className="min-w-36 text-sm text-slate-700">
                      {formatVeterinaryRequestDateTime(request.submittedAt)}
                    </p>
                  </td>

                  <td className="px-5 py-4">
                    <div className="flex min-w-40 flex-col gap-2 sm:items-end">
                      <button
                        type="button"
                        onClick={() => onView(request)}
                        className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                      >
                        Ver detalle
                      </button>

                      {canEdit && (
                        <button
                          type="button"
                          onClick={() => onEdit(request)}
                          className="rounded-lg px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100 hover:text-slate-900"
                        >
                          Editar
                        </button>
                      )}

                      {canChangeStatus && (
                        <button
                          type="button"
                          onClick={() => onChangeStatus(request)}
                          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                        >
                          Cambiar estado
                        </button>
                      )}

                      {canConvert && (
                        <button
                          type="button"
                          onClick={() => onConvert(request)}
                          className="rounded-lg bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-slate-800"
                        >
                          Crear recepción
                        </button>
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
