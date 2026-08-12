import { QRCodeCanvas } from "qrcode.react";
import { useState } from "react";
import toast from "react-hot-toast";

import type { Collection } from "../types/collection.types";
import { formatCollectionDateTime } from "../utils/collectionDisplay";
import {
  getCollectionLocationLabel,
  getCollectionStatusLabel,
} from "../utils/collectionLabels";

interface CollectionQrModalProps {
  isOpen: boolean;
  collection: Collection | null;
  onClose: () => void;
}

function getQrFileName(collection: Collection): string {
  const safePetName = collection.petName
    .trim()
    .replace(/\s+/g, "-")
    .replace(/[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑ-]/g, "");

  return `PCMS-${safePetName || "mascota"}-${collection.qrCode}.png`;
}

function getShareText(collection: Collection): string {
  return [
    "PCMS — Recolección de mascota",
    "",
    `Mascota: ${collection.petName}`,
    `Cliente: ${collection.customerName}`,
    `Código: ${collection.qrCode}`,
    "",
    "Conserva este código como identificación del servicio.",
  ].join("\n");
}

function canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (blob) => {
        if (blob) {
          resolve(blob);
          return;
        }

        reject(new Error("No fue posible generar la imagen del código QR."));
      },
      "image/png",
      1,
    );
  });
}

export function CollectionQrModal({
  isOpen,
  collection,
  onClose,
}: CollectionQrModalProps) {
  const [isSharing, setIsSharing] = useState(false);

  if (!isOpen || !collection) {
    return null;
  }

  const currentCollection = collection;

  function getQrCanvas(): HTMLCanvasElement | null {
    return document.getElementById(
      "collection-qr-canvas",
    ) as HTMLCanvasElement | null;
  }

  async function handleCopyCode() {
    try {
      await navigator.clipboard.writeText(currentCollection.qrCode);

      toast.success("Código de recolección copiado.");
    } catch {
      toast.error("No fue posible copiar el código.");
    }
  }

  async function handleDownloadQr() {
    const canvas = getQrCanvas();

    if (!canvas) {
      toast.error("No fue posible generar la imagen del QR.");

      return;
    }

    try {
      const link = document.createElement("a");

      link.download = getQrFileName(currentCollection);

      link.href = canvas.toDataURL("image/png");

      link.click();

      toast.success("Imagen QR generada correctamente.");
    } catch {
      toast.error("No fue posible generar la imagen del QR.");
    }
  }

  async function handleShare() {
    setIsSharing(true);

    try {
      const canvas = getQrCanvas();

      const shareText = getShareText(currentCollection);

      /*
       * Preferred path:
       * share the actual PNG QR image.
       */
      if (canvas && navigator.share && navigator.canShare) {
        const blob = await canvasToBlob(canvas);

        const file = new File([blob], getQrFileName(currentCollection), {
          type: "image/png",
        });

        if (
          navigator.canShare({
            files: [file],
          })
        ) {
          await navigator.share({
            title: "PCMS — Código de recolección",
            text: shareText,
            files: [file],
          });

          return;
        }
      }

      /*
       * Fallback:
       * share the identification text.
       */
      if (navigator.share) {
        await navigator.share({
          title: "PCMS — Código de recolección",
          text: shareText,
        });

        return;
      }

      /*
       * Last fallback:
       * copy the code.
       */
      await navigator.clipboard.writeText(currentCollection.qrCode);

      toast.success(
        "Tu dispositivo no permite compartir directamente. El código fue copiado.",
      );
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      toast.error("No fue posible compartir el código QR.");
    } finally {
      setIsSharing(false);
    }
  }

  function handleBackdropClick() {
    if (!isSharing) {
      onClose();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="collection-qr-title"
        className="max-h-full w-full max-w-lg overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Cadena de custodia
            </p>

            <h2
              id="collection-qr-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Código QR de recolección
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSharing}
            aria-label="Cerrar código QR"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <div className="space-y-6 px-6 py-6">
          <section className="text-center">
            <div className="inline-flex rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <QRCodeCanvas
                id="collection-qr-canvas"
                value={collection.qrCode}
                title={`Código QR de ${collection.petName}`}
                size={240}
                level="M"
                marginSize={4}
              />
            </div>

            <p className="mt-4 break-all font-mono text-sm font-semibold text-slate-900">
              {collection.qrCode}
            </p>

            <p className="mt-2 text-sm text-slate-500">
              Este código identifica a la mascota durante la cadena de custodia.
            </p>
          </section>

          <section className="rounded-xl bg-slate-50 p-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Mascota
                </p>

                <p className="mt-1 font-semibold text-slate-900">
                  {collection.petName}
                </p>

                <p className="mt-1 text-sm text-slate-500">
                  {collection.petSpecies}
                </p>
              </div>

              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Cliente
                </p>

                <p className="mt-1 font-semibold text-slate-900">
                  {collection.customerName}
                </p>

                <p className="mt-1 text-sm text-slate-500">
                  {collection.customerPhone}
                </p>
              </div>

              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Recolección
                </p>

                <p className="mt-1 text-sm font-medium text-slate-800">
                  {getCollectionLocationLabel(collection.locationType)}
                </p>
              </div>

              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Estado
                </p>

                <p className="mt-1 text-sm font-medium text-slate-800">
                  {getCollectionStatusLabel(collection.status)}
                </p>
              </div>
            </div>

            <div className="mt-4 border-t border-slate-200 pt-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Fecha de recolección
              </p>

              <p className="mt-1 text-sm text-slate-700">
                {formatCollectionDateTime(collection.collectedAt)}
              </p>
            </div>

            {collection.veterinaryClinicName && (
              <div className="mt-4 border-t border-slate-200 pt-4">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Veterinaria
                </p>

                <p className="mt-1 text-sm font-medium text-slate-800">
                  {collection.veterinaryClinicName}
                </p>

                {collection.referringVeterinarianName && (
                  <p className="mt-1 text-sm text-slate-500">
                    Dr. {collection.referringVeterinarianName}
                  </p>
                )}
              </div>
            )}
          </section>

          <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-4">
            <p className="text-sm font-medium text-emerald-900">
              El código no cambia al recibir la mascota.
            </p>

            <p className="mt-1 text-sm text-emerald-800">
              Cuando esta recolección se convierta en recepción, PCMS conservará
              exactamente el mismo código.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-3">
            <button
              type="button"
              onClick={handleCopyCode}
              disabled={isSharing}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Copiar código
            </button>

            <button
              type="button"
              onClick={handleDownloadQr}
              disabled={isSharing}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Descargar QR
            </button>

            <button
              type="button"
              onClick={handleShare}
              disabled={isSharing}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSharing ? "Compartiendo..." : "Compartir"}
            </button>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSharing}
            className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
          >
            Cerrar
          </button>
        </div>
      </section>
    </div>
  );
}
