import { CollectionStatus, type Collection } from "../types/collection.types";

import {
  formatCollectionDateTime,
  formatCollectionWeight,
} from "../utils/collectionDisplay";

import {
  getCollectionLocationLabel,
  getCollectionStatusLabel,
} from "../utils/collectionLabels";

import {
  canCancelCollection,
  canConvertCollectionToReception,
  canEditCollection,
} from "../utils/collectionWorkflow";

interface CollectionsTableProps {
  collections: Collection[];

  pendingCollectionId: string | null;
  currentUserId: string | null;
  canManageCollections: boolean;

  onShowQr: (collection: Collection) => void;

  onEdit: (collection: Collection) => void;

  onAssign: (collection: Collection) => void;

  onAccept: (collection: Collection) => void;

  onEvidence: (collection: Collection) => void;

  onConfirmCustody: (collection: Collection) => void;

  onReceive: (collection: Collection) => void;

  onCancel: (collection: Collection) => void;
}

function getStatusClasses(status: Collection["status"]): string {
  switch (status) {
    case CollectionStatus.Pending:
      return "bg-slate-100 text-slate-700 ring-slate-500/20";

    case CollectionStatus.Assigned:
      return "bg-blue-50 text-blue-700 ring-blue-600/20";

    case CollectionStatus.Accepted:
      return "bg-violet-50 text-violet-700 ring-violet-600/20";

    case CollectionStatus.Collected:
      return "bg-amber-50 text-amber-700 ring-amber-600/20";

    case CollectionStatus.Received:
      return "bg-emerald-50 text-emerald-700 ring-emerald-600/20";

    case CollectionStatus.Cancelled:
      return "bg-red-50 text-red-700 ring-red-600/20";

    default:
      return "bg-slate-100 text-slate-700 ring-slate-500/20";
  }
}

function showOptionalValue(
  value: string | null | undefined,
  fallback = "No registrado",
): string {
  const normalized = value?.trim();

  return normalized || fallback;
}

export function CollectionsTable({
  collections,
  pendingCollectionId,
  currentUserId,
  canManageCollections,
  onShowQr,
  onEdit,
  onAssign,
  onAccept,
  onEvidence,
  onConfirmCustody,
  onReceive,
  onCancel,
}: CollectionsTableProps) {
  if (collections.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-14 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 font-semibold text-slate-600">
          RE
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">
          No se encontraron recolecciones
        </h2>

        <p className="mt-2 text-sm text-slate-500">
          No hay recolecciones activas que coincidan con los filtros o la
          búsqueda.
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
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Mascota / cliente
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Estado
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Recolección
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Peso
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Código QR
              </th>

              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                Recolectó
              </th>

              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">
                Acciones
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {collections.map((collection) => {
              const isPending = pendingCollectionId === collection.id;

              const canEdit = canEditCollection(collection);

              const canReceive = canConvertCollectionToReception(collection);

              const canCancel = canCancelCollection(collection);

              const isAssignedDriver =
                currentUserId !== null &&
                collection.assignedDriverId === currentUserId;

              const canAssign =
                canManageCollections &&
                (collection.status === CollectionStatus.Pending ||
                  collection.status === CollectionStatus.Assigned ||
                  collection.status === CollectionStatus.Accepted);

              const canAccept =
                isAssignedDriver &&
                collection.status === CollectionStatus.Assigned;

              const canManageEvidence =
                canManageCollections || isAssignedDriver;

              const canConfirmCustody =
                isAssignedDriver &&
                collection.status === CollectionStatus.Accepted;

              return (
                <tr
                  key={collection.id}
                  className="align-top transition hover:bg-slate-50"
                >
                  {/* MASCOTA / CLIENTE */}
                  <td className="px-5 py-4">
                    <p className="font-semibold text-slate-900">
                      {collection.petName}
                    </p>

                    <p className="mt-1 text-sm text-slate-500">
                      {collection.petSpecies}
                    </p>

                    <p className="mt-2 text-sm font-medium text-slate-700">
                      {collection.customerName}
                    </p>

                    <p className="mt-1 text-xs text-slate-500">
                      {collection.customerPhone}
                    </p>
                  </td>

                  {/* ESTADO */}
                  <td className="px-5 py-4">
                    <span
                      className={[
                        "inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset",
                        getStatusClasses(collection.status),
                      ].join(" ")}
                    >
                      {getCollectionStatusLabel(collection.status)}
                    </span>

                    {collection.assignedDriverName && (
                      <p className="mt-3 text-xs text-slate-600">
                        Conductor: {collection.assignedDriverName}
                      </p>
                    )}

                    {collection.acceptedAt && (
                      <p className="mt-2 text-xs text-violet-700">
                        Aceptada:{" "}
                        {formatCollectionDateTime(collection.acceptedAt)}
                      </p>
                    )}

                    {collection.collectedAt && (
                      <p className="mt-2 whitespace-nowrap text-xs text-amber-700">
                        Custodia:{" "}
                        {formatCollectionDateTime(collection.collectedAt)}
                      </p>
                    )}

                    {collection.receivedAt && (
                      <p className="mt-2 text-xs text-emerald-700">
                        Recibida:{" "}
                        {formatCollectionDateTime(collection.receivedAt)}
                      </p>
                    )}

                    {collection.cancelledAt && (
                      <p className="mt-2 text-xs text-red-600">
                        Cancelada:{" "}
                        {formatCollectionDateTime(collection.cancelledAt)}
                      </p>
                    )}
                  </td>

                  {/* UBICACIÓN */}
                  <td className="px-5 py-4">
                    <p className="text-sm font-semibold text-slate-800">
                      {getCollectionLocationLabel(collection.locationType)}
                    </p>

                    <p className="mt-2 max-w-64 text-sm text-slate-600">
                      {collection.pickupAddress}
                    </p>

                    {collection.veterinaryClinicName && (
                      <p className="mt-3 text-sm font-medium text-slate-700">
                        {collection.veterinaryClinicName}
                      </p>
                    )}

                    {collection.referringVeterinarianName && (
                      <p className="mt-1 text-sm text-slate-500">
                        Dr. {collection.referringVeterinarianName}
                      </p>
                    )}

                    {(collection.pickupContactName ||
                      collection.pickupContactPhone) && (
                      <div className="mt-3 text-xs text-slate-500">
                        <p>
                          Contacto:{" "}
                          {showOptionalValue(collection.pickupContactName)}
                        </p>

                        {collection.pickupContactPhone && (
                          <p className="mt-1">
                            {collection.pickupContactPhone}
                          </p>
                        )}
                      </div>
                    )}
                  </td>

                  {/* PESO */}
                  <td className="whitespace-nowrap px-5 py-4">
                    <p className="font-medium text-slate-900">
                      {formatCollectionWeight(collection.approximateWeightKg)}
                    </p>

                    <p className="mt-1 text-xs text-slate-500">
                      Peso aproximado
                    </p>

                    {collection.hasPersonalBelongings && (
                      <div className="mt-3">
                        <span className="inline-flex rounded-full bg-amber-50 px-2 py-1 text-xs font-semibold text-amber-700">
                          Con objetos
                        </span>

                        {collection.personalBelongingsDescription && (
                          <p className="mt-2 max-w-48 text-xs text-slate-500">
                            {collection.personalBelongingsDescription}
                          </p>
                        )}
                      </div>
                    )}
                  </td>

                  {/* QR */}
                  <td className="px-5 py-4">
                    <button
                      type="button"
                      onClick={() => onShowQr(collection)}
                      disabled={isPending}
                      title="Ver código QR"
                      className="max-w-52 break-all rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-left font-mono text-xs font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-100 disabled:opacity-50"
                    >
                      {collection.qrCode}
                    </button>

                    {collection.receptionId && (
                      <div className="mt-3">
                        <p className="text-xs font-semibold text-emerald-700">
                          Recepción creada
                        </p>

                        {collection.receptionQrCode && (
                          <p className="mt-1 max-w-52 break-all font-mono text-xs text-slate-500">
                            {collection.receptionQrCode}
                          </p>
                        )}
                      </div>
                    )}
                  </td>

                  {/* RECOLECTÓ */}
                  <td className="px-5 py-4">
                    <p className="text-sm font-medium text-slate-800">
                      {collection.collectedByUserName ??
                        "Custodia no confirmada"}
                    </p>

                    {collection.notes && (
                      <p className="mt-2 max-w-56 text-xs text-slate-500">
                        {collection.notes}
                      </p>
                    )}
                  </td>

                  {/* ACCIONES */}
                  <td className="whitespace-nowrap px-5 py-4 text-right">
                    <div className="flex flex-col items-end gap-2">
                      <button
                        type="button"
                        onClick={() => onShowQr(collection)}
                        disabled={isPending}
                        className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Ver QR
                      </button>

                      {canAssign && (
                        <button
                          type="button"
                          onClick={() => onAssign(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg border border-blue-200 px-3 py-2 text-sm font-medium text-blue-700 disabled:opacity-50"
                        >
                          {collection.assignedDriverId
                            ? "Reasignar conductor"
                            : "Asignar conductor"}
                        </button>
                      )}

                      {canAccept && (
                        <button
                          type="button"
                          onClick={() => onAccept(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg bg-violet-700 px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"
                        >
                          Aceptar asignación
                        </button>
                      )}

                      {canManageEvidence &&
                        collection.status !== CollectionStatus.Received &&
                        collection.status !== CollectionStatus.Cancelled && (
                          <button
                            type="button"
                            onClick={() => onEvidence(collection)}
                            disabled={isPending}
                            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
                          >
                            Gestionar evidencia
                          </button>
                        )}

                      {canConfirmCustody && (
                        <button
                          type="button"
                          onClick={() => onConfirmCustody(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg bg-amber-700 px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"
                        >
                          Confirmar custodia
                        </button>
                      )}

                      {canReceive && (
                        <button
                          type="button"
                          onClick={() => onReceive(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg bg-emerald-700 px-3 py-2 text-sm font-semibold text-white transition hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          Recibir
                        </button>
                      )}

                      {canEdit && (
                        <button
                          type="button"
                          onClick={() => onEdit(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          Editar
                        </button>
                      )}

                      {canCancel && (
                        <button
                          type="button"
                          onClick={() => onCancel(collection)}
                          disabled={isPending}
                          className="w-full rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isPending ? "Procesando..." : "Cancelar"}
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
