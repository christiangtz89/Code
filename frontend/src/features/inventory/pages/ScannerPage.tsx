import { useCallback, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import type { AxiosError } from "axios";
import toast from "react-hot-toast";
import { ScanCodeScanner } from "../../../components/scanner/ScanCodeScanner";
import { DecimalInput } from "../../../components/ui/DecimalInput";
import { hasPermission } from "../../auth/utils/permissions";
import {
  recordScannerOutgoing,
  resolveInventoryScannerCode,
  type InventoryScannerOutgoing,
  type InventoryScannerReasonCode,
  type InventoryScannerResolve,
} from "../api/inventoryApi";

interface ScannerErrorResponse {
  message?: string;
  currentState?: InventoryScannerResolve | null;
}

const reasonLabels: Record<InventoryScannerReasonCode, string> = {
  1: "Entrega",
  2: "Consumo operativo",
  3: "Salida manual autorizada",
};

function defaultQuantity(unitOfMeasure: string): string {
  const unit = unitOfMeasure.trim().toLocaleLowerCase("es-MX");
  return ["pieza", "pza", "unidad", "unidades", "unit"].includes(unit)
    ? "1"
    : "";
}

function newOperationId(): string {
  if (typeof crypto.randomUUID === "function") return crypto.randomUUID();

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = [...bytes].map((value) => value.toString(16).padStart(2, "0"));
  return `${hex.slice(0, 4).join("")}-${hex.slice(4, 6).join("")}-${hex.slice(6, 8).join("")}-${hex.slice(8, 10).join("")}-${hex.slice(10).join("")}`;
}

export function ScannerPage() {
  const canRecordOutgoing = hasPermission("Inventory.ScanOutgoing");
  const outgoingPendingRef = useRef(false);
  const resolveRequestVersionRef = useRef(0);
  const activeCandidateRef = useRef<{
    operationId: string;
    supplyItemId: string;
    scanCode: string;
  } | null>(null);
  const [preview, setPreview] = useState<InventoryScannerResolve | null>(null);
  const [selectedLotId, setSelectedLotId] = useState("");
  const [quantity, setQuantity] = useState("");
  const [reasonCode, setReasonCode] = useState<"" | InventoryScannerReasonCode>(
    "",
  );
  const [notes, setNotes] = useState("");
  const [operationId, setOperationId] = useState(newOperationId);
  const [lastResult, setLastResult] =
    useState<InventoryScannerOutgoing | null>(null);

  const resolveMutation = useMutation({
    mutationFn: ({ scanCode }: { scanCode: string; requestVersion: number }) =>
      resolveInventoryScannerCode(scanCode),
    onSuccess: (resolved, request) => {
      if (
        outgoingPendingRef.current ||
        request.requestVersion !== resolveRequestVersionRef.current
      )
        return;

      const nextOperationId = newOperationId();
      setPreview(resolved);
      setSelectedLotId(
        resolved.scannedLotId ?? resolved.suggestedLotId ?? "",
      );
      setQuantity(defaultQuantity(resolved.unitOfMeasure));
      setReasonCode("");
      setNotes("");
      setOperationId(nextOperationId);
      activeCandidateRef.current = {
        operationId: nextOperationId,
        supplyItemId: resolved.supplyItemId,
        scanCode: resolved.scannedCode,
      };
      setLastResult(null);
    },
    onError: (
      error: AxiosError<ScannerErrorResponse>,
      request: { scanCode: string; requestVersion: number },
    ) => {
      if (
        outgoingPendingRef.current ||
        request.requestVersion !== resolveRequestVersionRef.current
      )
        return;

      setPreview(null);
      activeCandidateRef.current = null;
      setLastResult(null);
      toast.error(error.response?.data?.message ?? "Código no encontrado.");
    },
  });
  const resolveCode = resolveMutation.mutate;
  const handleCode = useCallback(
    (scanCode: string) => {
      if (outgoingPendingRef.current) return;
      const requestVersion = ++resolveRequestVersionRef.current;
      resolveCode({ scanCode, requestVersion });
    },
    [resolveCode],
  );

  const outgoingMutation = useMutation({
    mutationFn: recordScannerOutgoing,
    onSuccess: (result, submitted) => {
      setLastResult(result);
      const candidate = activeCandidateRef.current;
      if (
        candidate?.operationId === submitted.clientOperationId &&
        candidate.supplyItemId === submitted.supplyItemId &&
        candidate.scanCode === submitted.scanCode
      ) {
        setPreview(null);
        setSelectedLotId("");
        setQuantity("");
        setReasonCode("");
        setNotes("");
        setOperationId(newOperationId());
        activeCandidateRef.current = null;
      }
      toast.success("Salida registrada correctamente.");
    },
    onError: (
      error: AxiosError<ScannerErrorResponse>,
      submitted,
    ) => {
      const response = error.response?.data;
      if (error.response?.status === 409 && response?.currentState) {
        const refreshed = response.currentState;
        const identityChanged = refreshed.supplyItemId !== submitted.supplyItemId;
        const nextOperationId = identityChanged
          ? newOperationId()
          : submitted.clientOperationId;
        setPreview(refreshed);
        setSelectedLotId((current) =>
          refreshed.lots.some((lot) => lot.id === current && lot.isEligible)
            ? current
            : (refreshed.scannedLotId ?? refreshed.suggestedLotId ?? ""),
        );
        if (identityChanged) {
          setQuantity(defaultQuantity(refreshed.unitOfMeasure));
          setReasonCode("");
          setNotes("");
        }
        setOperationId(nextOperationId);
        activeCandidateRef.current = {
          operationId: nextOperationId,
          supplyItemId: refreshed.supplyItemId,
          scanCode: refreshed.scannedCode,
        };
        setLastResult(null);
        toast.error(
          response.message?.includes("cambió")
            ? "El inventario cambió. Revisa la información y confirma nuevamente."
            : (response.message ?? "La salida no puede registrarse."),
        );
        return;
      }
      toast.error(response?.message ?? "No fue posible registrar la salida.");
    },
    onSettled: () => {
      outgoingPendingRef.current = false;
    },
  });

  const selectedLot = preview?.lots.find((lot) => lot.id === selectedLotId);
  const numericQuantity = Number(quantity);
  const manualNotesRequired = reasonCode === 3;
  const missingRequiredLot =
    !!preview &&
    !preview.isLotScan &&
    preview.lots.some((lot) => lot.isEligible) &&
    !selectedLotId;
  const canSubmit =
    !!preview?.canRecordOutgoing &&
    canRecordOutgoing &&
    Number.isFinite(numericQuantity) &&
    numericQuantity > 0 &&
    reasonCode !== "" &&
    !missingRequiredLot &&
    (!manualNotesRequired || !!notes.trim()) &&
    !outgoingMutation.isPending;

  function submitOutgoing() {
    if (!preview || !canSubmit) return;
    outgoingPendingRef.current = true;
    resolveRequestVersionRef.current += 1;
    outgoingMutation.mutate({
      clientOperationId: operationId,
      scanCode: preview.scannedCode,
      supplyItemId: preview.supplyItemId,
      quantity: numericQuantity,
      supplyInventoryLotId: selectedLotId || null,
      reasonCode,
      notes: notes.trim() || null,
      expectedPhysicalStock: preview.physicalStock,
      expectedAvailableStock: preview.availableStock,
      expectedLotStock: selectedLot?.remainingQuantity ?? null,
      expectedUnallocatedStock: selectedLotId
        ? null
        : preview.unallocatedPhysicalStock,
    });
  }

  function cancelOperation() {
    resolveRequestVersionRef.current += 1;
    setPreview(null);
    setSelectedLotId("");
    setQuantity("");
    setReasonCode("");
    setNotes("");
    setOperationId(newOperationId());
    activeCandidateRef.current = null;
    setLastResult(null);
  }

  return (
    <main className="mx-auto max-w-2xl space-y-5">
      <header>
        <h1 className="text-2xl font-bold">Escáner de inventario</h1>
        <p className="text-sm text-slate-500">
          Escanea o escribe un código. Identificar un producto nunca registra una
          salida automáticamente.
        </p>
      </header>

      <ScanCodeScanner onCode={handleCode} />
      {resolveMutation.isPending && <p>Resolviendo código…</p>}

      {preview && (
        <section className="space-y-4 rounded-xl border bg-white p-4">
          <div>
            <h2 className="text-lg font-semibold">{preview.supplyItemName}</h2>
            <p className="text-sm text-slate-600">
              Código: {preview.scannedCode} · Identificación:{" "}
              {preview.isLotScan ? "Lote" : "Insumo"}
            </p>
          </div>

          <div className="grid gap-3 rounded-lg bg-slate-50 p-3 sm:grid-cols-2">
            <p>
              Existencia física: <strong>{preview.physicalStock}</strong>{" "}
              {preview.unitOfMeasure}
            </p>
            <p>
              Disponible: <strong>{preview.availableStock}</strong>{" "}
              {preview.unitOfMeasure}
            </p>
            <p className="text-sm text-slate-600">
              Reservado para cremaciones: {preview.reservedQuantity}{" "}
              {preview.unitOfMeasure}
            </p>
            <p className="text-sm text-slate-600">
              Sin asignar a lote: {preview.unallocatedPhysicalStock}{" "}
              {preview.unitOfMeasure}
            </p>
          </div>

          {preview.lots.length > 0 && (
            <div className="space-y-2">
              <h3 className="font-medium">Existencia por lote</h3>
              {preview.lots.map((lot) => (
                <p key={lot.id} className="text-sm text-slate-600">
                  {lot.scanCode}: {lot.remainingQuantity} {preview.unitOfMeasure}
                  {!lot.isActive ? " · Inactivo, no seleccionable" : ""}
                </p>
              ))}
            </div>
          )}

          {preview.hasLottedAndUnlottedStock && (
            <p className="rounded bg-amber-50 p-3 text-sm text-amber-800">
              Hay existencia con lote y sin lote. La salida solo usará el lote
              seleccionado; no se elegirá uno automáticamente.
            </p>
          )}

          {canRecordOutgoing && preview.canRecordOutgoing ? (
            <div className="space-y-4 border-t pt-4">
              {preview.isLotScan ? (
                <p className="text-sm">
                  Lote seleccionado: <strong>{preview.scannedLotCode}</strong> ·{" "}
                  {preview.scannedLotRemainingQuantity} {preview.unitOfMeasure}
                </p>
              ) : preview.lots.some((lot) => lot.isEligible) ? (
                <label className="block text-sm font-medium">
                  Lote
                  <select
                    className="mt-1 w-full rounded border px-3 py-3"
                    value={selectedLotId}
                    onChange={(event) => setSelectedLotId(event.target.value)}
                  >
                    <option value="">Selecciona un lote</option>
                    {preview.lots
                      .filter((lot) => lot.isEligible)
                      .map((lot) => (
                        <option key={lot.id} value={lot.id}>
                          {lot.scanCode} · {lot.remainingQuantity}{" "}
                          {preview.unitOfMeasure}
                        </option>
                      ))}
                  </select>
                </label>
              ) : (
                <p className="text-sm text-slate-600">
                  Esta salida se registrará sin lote.
                </p>
              )}

              <label className="block text-sm font-medium">
                Cantidad
                <DecimalInput
                  className="mt-1 w-full rounded border px-3 py-3"
                  value={quantity}
                  onChange={(event) => setQuantity(event.target.value)}
                  placeholder="Cantidad"
                />
              </label>

              <label className="block text-sm font-medium">
                Motivo
                <select
                  className="mt-1 w-full rounded border px-3 py-3"
                  value={reasonCode}
                  onChange={(event) =>
                    setReasonCode(
                      event.target.value
                        ? (Number(event.target.value) as InventoryScannerReasonCode)
                        : "",
                    )
                  }
                >
                  <option value="">Selecciona un motivo</option>
                  {Object.entries(reasonLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block text-sm font-medium">
                Notas {manualNotesRequired ? "(obligatorias)" : "(opcionales)"}
                <textarea
                  className="mt-1 min-h-24 w-full rounded border px-3 py-2"
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                />
              </label>

              <div className="rounded-lg border p-3 text-sm">
                <p className="font-medium">Confirmación de salida</p>
                <p>
                  {preview.supplyItemName} · {preview.scannedCode}
                </p>
                <p>
                  {numericQuantity || 0} {preview.unitOfMeasure} ·{" "}
                  {reasonCode === "" ? "Sin motivo" : reasonLabels[reasonCode]}
                </p>
                <p>
                  Lote: {selectedLot?.scanCode ?? preview.scannedLotCode ?? "Sin lote"}
                </p>
                {notes.trim() && <p>Notas: {notes.trim()}</p>}
              </div>

              <div className="flex flex-col gap-2 sm:flex-row">
                <button
                  type="button"
                  onClick={submitOutgoing}
                  disabled={!canSubmit}
                  className="min-h-14 flex-1 rounded-lg bg-emerald-700 px-5 py-3 text-base font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {outgoingMutation.isPending
                    ? "Registrando…"
                    : "Registrar salida"}
                </button>
                <button
                  type="button"
                  onClick={cancelOperation}
                  disabled={outgoingMutation.isPending}
                  className="min-h-12 rounded-lg border px-4 py-2 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Cancelar
                </button>
              </div>
            </div>
          ) : canRecordOutgoing ? (
            <p className="rounded bg-amber-50 p-3 text-sm text-amber-800">
              No hay existencia elegible para registrar una salida.
            </p>
          ) : (
            <p className="rounded bg-slate-50 p-3 text-sm text-slate-600">
              Consulta únicamente. Tu permiso permite identificar y revisar
              existencias, pero no registrar salidas.
            </p>
          )}
        </section>
      )}

      {lastResult && (
        <section className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
          <h2 className="font-semibold text-emerald-900">Salida confirmada</h2>
          <p className="text-sm text-emerald-900">
            {lastResult.quantity} {lastResult.unitOfMeasure} · Existencia actual:{" "}
            {lastResult.currentPhysicalStock} · Disponible actualmente:{" "}
            {lastResult.currentAvailableStock}
          </p>
          <p className="text-sm text-emerald-800">
            Registró: {lastResult.recordedByDisplayNameSnapshot ?? "Usuario activo"}
          </p>
        </section>
      )}
    </main>
  );
}
