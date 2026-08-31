import { useCallback, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { AxiosError } from "axios";
import toast from "react-hot-toast";
import { Link, useParams } from "react-router-dom";
import { ScanCodeScanner } from "../../../components/scanner/ScanCodeScanner";
import { DecimalInput } from "../../../components/ui/DecimalInput";
import {
  fulfillCremationScanner,
  getCremationScannerPreview,
  resolveCremationScannerCode,
  type CremationScannerFulfillInput,
  type CremationScannerMatchKind,
  type CremationScannerPreview,
  type CremationScannerResolve,
  type CremationScannerSelectionInput,
} from "../../inventory/api/inventoryApi";

interface ScannerErrorResponse {
  message?: string;
  currentPreview?: CremationScannerPreview | null;
}

interface SelectedLine {
  resolved: CremationScannerResolve;
  selectedLotId: string;
  quantity: string;
  expectedReservationReservedAt: string | null;
}

const statusLabels: Record<number, string> = {
  1: "Pendiente",
  2: "Programada",
  3: "En proceso",
  4: "En enfriamiento",
  5: "Procesando restos",
  6: "Completada",
  7: "Lista para entrega",
  8: "Entregada",
  9: "Cancelada",
};

function selectedLotId(resolved: CremationScannerResolve): string {
  return resolved.scannedLotId ?? resolved.suggestedLotId ?? "";
}

function requiresLot(line: SelectedLine): boolean {
  const eligibleLots = line.resolved.lots.filter((lot) => lot.isEligible);
  return (
    !line.resolved.isLotScan &&
    eligibleLots.length > 0 &&
    !eligibleLots.some((lot) => lot.id === line.selectedLotId)
  );
}

function toSelectionInput(line: SelectedLine): CremationScannerSelectionInput {
  const lot = line.resolved.lots.find((candidate) =>
    candidate.id === line.selectedLotId,
  );
  return {
    scanCode: line.resolved.scannedCode,
    supplyItemId: line.resolved.supplyItemId,
    supplyInventoryLotId: line.selectedLotId || null,
    quantity: Number(line.quantity),
    expectedPhysicalStock: line.resolved.physicalStock,
    expectedAvailableStock: line.resolved.availableForFulfillment,
    expectedLotStock: lot?.remainingQuantity ?? null,
    expectedUnallocatedStock: line.selectedLotId
      ? null
      : line.resolved.unallocatedPhysicalStock,
  };
}

function LotSelector({
  line,
  onChange,
}: {
  line: SelectedLine;
  onChange: (lotId: string) => void;
}) {
  if (line.resolved.isLotScan) {
    return (
      <p className="text-sm text-slate-600">
        Lote escaneado: <strong>{line.resolved.scannedLotCode}</strong> ·{" "}
        {line.resolved.scannedLotRemainingQuantity} {line.resolved.unitOfMeasure}
      </p>
    );
  }
  if (!line.resolved.lots.some((lot) => lot.isEligible)) {
    return <p className="text-sm text-slate-500">Salida sin lote.</p>;
  }

  return (
    <label className="block text-sm font-medium">
      Lote
      <select
        className="mt-1 w-full rounded border px-3 py-2"
        value={line.selectedLotId}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">Selecciona un lote</option>
        {line.resolved.lots
          .filter((lot) => lot.isEligible)
          .map((lot) => (
            <option key={lot.id} value={lot.id}>
              {lot.scanCode} · {lot.remainingQuantity}{" "}
              {line.resolved.unitOfMeasure}
            </option>
          ))}
      </select>
    </label>
  );
}

export function CremationFulfillmentScannerPage() {
  const { cremationId = "" } = useParams();
  const queryClient = useQueryClient();
  const fulfillmentPendingRef = useRef(false);
  const resolveVersionRef = useRef(0);
  const [urn, setUrn] = useState<SelectedLine | null>(null);
  const [materials, setMaterials] = useState<SelectedLine[]>([]);

  const previewQuery = useQuery({
    queryKey: ["cremation-scanner-preview", cremationId],
    queryFn: () => getCremationScannerPreview(cremationId),
    enabled: !!cremationId,
  });
  const preview = previewQuery.data;

  const resolveMutation = useMutation({
    mutationFn: ({
      scanCode,
      expectedKind,
      expectedCremationStatus,
      expectedReservationReservedAt,
      expectedUrnSupplyItemId,
      expectedUrnScanCode,
    }: {
      scanCode: string;
      expectedKind: CremationScannerMatchKind;
      expectedCremationStatus: number;
      expectedReservationReservedAt: string;
      expectedUrnSupplyItemId: string;
      expectedUrnScanCode: string;
      requestVersion: number;
    }) =>
      resolveCremationScannerCode(
        cremationId,
        scanCode,
        expectedKind,
        expectedCremationStatus,
        expectedReservationReservedAt,
        expectedUrnSupplyItemId,
        expectedUrnScanCode,
      ),
    onSuccess: (resolved, request) => {
      if (
        fulfillmentPendingRef.current ||
        request.requestVersion !== resolveVersionRef.current
      )
        return;
      if (!resolved.canUse || !resolved.matchKind) {
        toast.error(resolved.rejectionReason ?? "El insumo no puede usarse en esta entrega.");
        return;
      }

      const nextLine: SelectedLine = {
        resolved,
        selectedLotId: selectedLotId(resolved),
        quantity: String(resolved.suggestedQuantity),
        expectedReservationReservedAt:
          resolved.matchKind === 1 ? request.expectedReservationReservedAt : null,
      };
      if (resolved.matchKind === 1) {
        setUrn((current) => {
          if (
            current &&
            current.resolved.supplyItemId === resolved.supplyItemId &&
            current.selectedLotId &&
            nextLine.selectedLotId &&
            current.selectedLotId !== nextLine.selectedLotId
          ) {
            toast.error("La urna ya está vinculada a otro lote seleccionado.");
            return current;
          }
          return nextLine;
        });
        toast.success("Urna reservada verificada.");
        return;
      }

      const existing = materials.find(
        (line) => line.resolved.supplyItemId === resolved.supplyItemId,
      );
      if (!existing) {
        setMaterials((current) =>
          current.some(
            (line) => line.resolved.supplyItemId === resolved.supplyItemId,
          )
            ? current
            : [...current, nextLine],
        );
        toast.success("Material agregado a la entrega.");
        return;
      }

      const sameScanCode =
        existing.resolved.scannedCode === resolved.scannedCode;
      const sameSelectedLot =
        !!existing.selectedLotId &&
        existing.selectedLotId === nextLine.selectedLotId;
      if (!sameScanCode && !sameSelectedLot) {
        toast.error("El material ya está vinculado a otro lote o forma de salida.");
        return;
      }

      toast.success(
        "El material ya estaba seleccionado; se conservó la cantidad indicada.",
      );
    },
    onError: (
      error: AxiosError<ScannerErrorResponse>,
      request: {
        scanCode: string;
        expectedKind: CremationScannerMatchKind;
        expectedCremationStatus: number;
        expectedReservationReservedAt: string;
        expectedUrnSupplyItemId: string;
        expectedUrnScanCode: string;
        requestVersion: number;
      },
    ) => {
      if (
        fulfillmentPendingRef.current ||
        request.requestVersion !== resolveVersionRef.current
      )
        return;
      const response = error.response?.data;
      if (error.response?.status === 409 && response?.currentPreview) {
        queryClient.setQueryData(
          ["cremation-scanner-preview", cremationId],
          response.currentPreview,
        );
        setUrn(null);
        setMaterials([]);
        toast.error(
          "La entrega cambió. Revisa la información y vuelve a escanear.",
        );
        return;
      }
      toast.error(response?.message ?? "Código no encontrado.");
    },
  });

  const resolveCode = resolveMutation.mutate;
  const handleCode = useCallback(
    (scanCode: string) => {
      if (fulfillmentPendingRef.current || !preview?.reservedUrn) return;
      const requestVersion = ++resolveVersionRef.current;
      resolveCode({
        scanCode,
        expectedKind: urn ? 2 : 1,
        expectedCremationStatus: preview.cremationStatus,
        expectedReservationReservedAt:
          urn?.expectedReservationReservedAt ?? preview.reservedUrn.reservedAt,
        expectedUrnSupplyItemId: preview.reservedUrn.supplyItemId,
        expectedUrnScanCode: preview.reservedUrn.expectedScanCode,
        requestVersion,
      });
    },
    [preview, resolveCode, urn],
  );

  const fulfillMutation = useMutation({
    mutationFn: (payload: CremationScannerFulfillInput) =>
      fulfillCremationScanner(cremationId, payload),
    onSuccess: (updated) => {
      queryClient.setQueryData(
        ["cremation-scanner-preview", cremationId],
        updated,
      );
      void queryClient.invalidateQueries({
        queryKey: ["cremation-inventory", cremationId],
      });
      setUrn(null);
      setMaterials([]);
      toast.success(
        updated.isFulfilled
          ? "Entrega registrada correctamente."
          : "La entrega ya estaba registrada.",
      );
    },
    onError: (error: AxiosError<ScannerErrorResponse>) => {
      const response = error.response?.data;
      if (error.response?.status === 409 && response?.currentPreview) {
        queryClient.setQueryData(
          ["cremation-scanner-preview", cremationId],
          response.currentPreview,
        );
        setUrn(null);
        setMaterials([]);
        toast.error(
          "La entrega cambió. Revisa la información y vuelve a escanear antes de confirmar.",
        );
        return;
      }
      toast.error(response?.message ?? "No fue posible registrar la entrega.");
    },
    onSettled: () => {
      fulfillmentPendingRef.current = false;
    },
  });

  const materialLinesAreValid = materials.every(
    (line) =>
      Number.isFinite(Number(line.quantity)) &&
      Number(line.quantity) > 0 &&
      !requiresLot(line),
  );
  const canSubmit =
    !!preview?.canFulfill &&
    !!urn &&
    !!urn.expectedReservationReservedAt &&
    !requiresLot(urn) &&
    materialLinesAreValid &&
    !fulfillMutation.isPending;

  function confirmFulfillment() {
    if (
      !preview ||
      !urn ||
      !urn.expectedReservationReservedAt ||
      !canSubmit
    )
      return;
    fulfillmentPendingRef.current = true;
    resolveVersionRef.current += 1;
    fulfillMutation.mutate({
      expectedCremationStatus: preview.cremationStatus,
      expectedReservationReservedAt: urn.expectedReservationReservedAt,
      urn: toSelectionInput(urn),
      materials: materials.map(toSelectionInput),
    });
  }

  if (previewQuery.isLoading) return <p>Cargando entrega de cremación…</p>;
  if (previewQuery.isError || !preview) {
    return (
      <main className="mx-auto max-w-3xl space-y-4">
        <Link className="text-sm font-medium text-slate-600" to="/cremations">
          ← Volver a cremaciones
        </Link>
        <p className="rounded border border-red-200 bg-red-50 p-4 text-red-700">
          No fue posible cargar la entrega de cremación.
        </p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl space-y-5">
      <Link className="text-sm font-medium text-slate-600" to="/cremations">
        ← Volver a cremaciones
      </Link>

      <header>
        <h1 className="text-2xl font-bold">Entrega de cremación</h1>
        <p className="text-sm text-slate-500">
          Escanear identifica los insumos. Solo “Registrar entrega” modifica el
          inventario.
        </p>
      </header>

      <section className="grid gap-3 rounded-xl border bg-white p-4 sm:grid-cols-2">
        <div>
          <p className="text-xs font-medium uppercase text-slate-500">Caso</p>
          <p className="font-mono text-sm">{preview.caseReference}</p>
        </div>
        <div>
          <p className="text-xs font-medium uppercase text-slate-500">Estado</p>
          <p>{statusLabels[preview.cremationStatus] ?? "Estado desconocido"}</p>
        </div>
        <div>
          <p className="text-xs font-medium uppercase text-slate-500">Mascota</p>
          <p className="font-semibold">{preview.petName}</p>
        </div>
        <div>
          <p className="text-xs font-medium uppercase text-slate-500">Cliente</p>
          <p>{preview.customerName}</p>
        </div>
      </section>

      {preview.isFulfilled ? (
        <section className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
          <h2 className="font-semibold text-emerald-900">Entrega ya registrada</h2>
          <p className="text-sm text-emerald-800">
            {preview.fulfilledAt
              ? new Date(preview.fulfilledAt).toLocaleString("es-MX")
              : "El inventario de esta cremación ya fue entregado."}
          </p>
          {preview.completedMaterials.map((material, index) => (
            <p
              key={`${material.supplyItemName}-${index}`}
              className="text-sm text-emerald-800"
            >
              {material.supplyItemName}: {material.quantity}{" "}
              {material.unitOfMeasure}
            </p>
          ))}
        </section>
      ) : (
        <>
          <section className="rounded-xl border bg-white p-4">
            <h2 className="font-semibold">Urna reservada</h2>
            {preview.reservedUrn ? (
              <div className="mt-2 text-sm text-slate-700">
                <p>{preview.reservedUrn.urnName}</p>
                <p>{preview.reservedUrn.supplyItemName}</p>
                <p className="font-mono text-xs">
                  Código esperado: {preview.reservedUrn.expectedScanCode}
                </p>
                <p className={urn ? "mt-2 text-emerald-700" : "mt-2 text-amber-700"}>
                  {urn ? "Urna verificada" : "Pendiente de escaneo"}
                </p>
              </div>
            ) : (
              <p className="mt-2 text-sm text-amber-700">
                No existe una reserva de urna para esta entrega.
              </p>
            )}
          </section>

          {!preview.canFulfill && (
            <p className="rounded bg-amber-50 p-3 text-sm text-amber-800">
              {preview.blockingReason}
            </p>
          )}

          {preview.canFulfill && (
            <>
              <ScanCodeScanner onCode={handleCode} />
              {resolveMutation.isPending && <p>Verificando código…</p>}

              {urn && (
                <section className="space-y-3 rounded-xl border border-emerald-200 bg-white p-4">
                  <h2 className="font-semibold">Urna escaneada</h2>
                  <p>{urn.resolved.supplyItemName} · 1 {urn.resolved.unitOfMeasure}</p>
                  <LotSelector
                    line={urn}
                    onChange={(lotId) => setUrn((current) => current && { ...current, selectedLotId: lotId })}
                  />
                </section>
              )}

              <section className="space-y-3 rounded-xl border bg-white p-4">
                <div>
                  <h2 className="font-semibold">Materiales adicionales</h2>
                  <p className="text-sm text-slate-500">
                    Escanea cada material y ajusta su cantidad antes de confirmar.
                  </p>
                </div>
                {materials.length === 0 && (
                  <p className="text-sm text-slate-500">Sin materiales adicionales.</p>
                )}
                {materials.map((line) => (
                  <div
                    key={line.resolved.supplyItemId}
                    className="space-y-2 rounded-lg border p-3"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium">{line.resolved.supplyItemName}</p>
                        <p className="text-sm text-slate-500">
                          Disponible para entrega: {line.resolved.availableForFulfillment}{" "}
                          {line.resolved.unitOfMeasure}
                        </p>
                      </div>
                      <button
                        type="button"
                        className="text-sm font-medium text-red-700"
                        onClick={() =>
                          setMaterials((current) =>
                            current.filter(
                              (candidate) =>
                                candidate.resolved.supplyItemId !==
                                line.resolved.supplyItemId,
                            ),
                          )
                        }
                      >
                        Quitar
                      </button>
                    </div>
                    <DecimalInput
                      className="w-full rounded border px-3 py-2"
                      value={line.quantity}
                      onChange={(event) =>
                        setMaterials((current) =>
                          current.map((candidate) =>
                            candidate.resolved.supplyItemId ===
                            line.resolved.supplyItemId
                              ? { ...candidate, quantity: event.target.value }
                              : candidate,
                          ),
                        )
                      }
                      placeholder="Cantidad"
                    />
                    <LotSelector
                      line={line}
                      onChange={(lotId) =>
                        setMaterials((current) =>
                          current.map((candidate) =>
                            candidate.resolved.supplyItemId ===
                            line.resolved.supplyItemId
                              ? { ...candidate, selectedLotId: lotId }
                              : candidate,
                          ),
                        )
                      }
                    />
                  </div>
                ))}
              </section>

              <section className="rounded-xl border bg-slate-50 p-4 text-sm">
                <h2 className="font-semibold">Confirmación</h2>
                <p>Urna: {urn?.resolved.supplyItemName ?? "Pendiente"}</p>
                <p>Materiales adicionales: {materials.length}</p>
              </section>

              <button
                type="button"
                onClick={confirmFulfillment}
                disabled={!canSubmit}
                className="min-h-14 w-full rounded-lg bg-emerald-700 px-5 py-3 text-base font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
              >
                {fulfillMutation.isPending ? "Registrando…" : "Registrar entrega"}
              </button>
            </>
          )}
        </>
      )}
    </main>
  );
}
