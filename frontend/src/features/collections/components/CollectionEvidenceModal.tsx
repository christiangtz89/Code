import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import toast from "react-hot-toast";

import {
  deactivateCollectionPhoto,
  getCollectionPhotos,
  uploadCollectionPhoto,
} from "../api/collectionPhotosApi";
import type { Collection } from "../types/collection.types";
import {
  CollectionPhotoType,
  type CollectionPhoto,
} from "../types/collectionPhoto.types";
import { AuthenticatedCollectionPhoto } from "./AuthenticatedCollectionPhoto";
import { CollectionPetPhotoField } from "./CollectionPetPhotoField";

interface CollectionEvidenceModalProps {
  collection: Collection | null;
  canMutate: boolean;
  onClose: () => void;
  onEvidenceChanged: () => Promise<void>;
}

const evidenceTypes = new Set<number>([
  CollectionPhotoType.PetIdentification,
  CollectionPhotoType.PickupEvidence,
]);

export function CollectionEvidenceModal({
  collection,
  canMutate,
  onClose,
  onEvidenceChanged,
}: CollectionEvidenceModalProps) {
  const queryClient = useQueryClient();
  const [file, setFile] = useState<File | null>(null);

  const photosQuery = useQuery({
    queryKey: ["collection-photos", collection?.id],
    queryFn: () => getCollectionPhotos(collection!.id),
    enabled: collection !== null,
  });

  const uploadMutation = useMutation({
    mutationFn: async () => {
      if (!collection || !file) return;
      await uploadCollectionPhoto(
        collection.id,
        file,
        CollectionPhotoType.PickupEvidence,
        "Evidencia de recolección y custodia.",
      );
    },
    onSuccess: async () => {
      setFile(null);
      await queryClient.invalidateQueries({
        queryKey: ["collection-photos", collection?.id],
      });
      await onEvidenceChanged();
      toast.success("La evidencia se guardó correctamente.");
    },
    onError: () => toast.error("No fue posible guardar la evidencia."),
  });

  const deactivateMutation = useMutation({
    mutationFn: (photo: CollectionPhoto) => deactivateCollectionPhoto(photo.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["collection-photos", collection?.id],
      });
      await onEvidenceChanged();
      toast.success("La fotografía fue desactivada.");
    },
    onError: () => toast.error("No fue posible desactivar la fotografía."),
  });

  if (!collection) {
    return null;
  }

  const evidencePhotos = (photosQuery.data ?? []).filter((photo) =>
    evidenceTypes.has(photo.photoType),
  );

  const isMutating = uploadMutation.isPending || deactivateMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={() => !isMutating && onClose()}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="collection-evidence-title"
        className="max-h-full w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="border-b border-slate-200 px-6 py-5">
          <h2
            id="collection-evidence-title"
            className="text-xl font-semibold text-slate-900"
          >
            Evidencia de recolección
          </h2>
          <p className="mt-2 text-sm text-slate-500">
            {collection.petName} · La evidencia activa es obligatoria antes de
            confirmar la custodia y crear la recepción.
          </p>
        </header>

        <div className="space-y-6 px-6 py-6">
          {photosQuery.isLoading && (
            <p className="text-sm text-slate-500">Cargando fotografías...</p>
          )}

          {!photosQuery.isLoading && evidencePhotos.length === 0 && (
            <p className="rounded-lg bg-amber-50 px-4 py-3 text-sm text-amber-800">
              Aún no hay evidencia activa para esta recolección.
            </p>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            {evidencePhotos.map((photo) => (
              <article
                key={photo.id}
                className="overflow-hidden rounded-xl border border-slate-200"
              >
                <AuthenticatedCollectionPhoto
                  photoId={photo.id}
                  alt={`Evidencia de ${collection.petName}`}
                  className="aspect-[4/3] w-full object-cover"
                />
                <div className="space-y-2 p-3">
                  <p className="truncate text-sm font-medium text-slate-700">
                    {photo.originalFileName}
                  </p>
                  {canMutate && (
                    <button
                      type="button"
                      onClick={() => deactivateMutation.mutate(photo)}
                      disabled={isMutating}
                      className="text-sm font-medium text-red-700 disabled:opacity-50"
                    >
                      Desactivar fotografía
                    </button>
                  )}
                </div>
              </article>
            ))}
          </div>

          {canMutate && (
            <div className="border-t border-slate-200 pt-5">
              <CollectionPetPhotoField
                file={file}
                disabled={isMutating}
                title="Fotografía de evidencia de recolección"
                description="Toma o selecciona una fotografía que documente la identificación y entrega de la mascota."
                onChange={setFile}
              />
              <button
                type="button"
                onClick={() => uploadMutation.mutate()}
                disabled={!file || isMutating}
                className="mt-4 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
              >
                {uploadMutation.isPending
                  ? "Guardando evidencia..."
                  : "Guardar evidencia"}
              </button>
            </div>
          )}

          <footer className="flex justify-end border-t border-slate-200 pt-5">
            <button
              type="button"
              onClick={onClose}
              disabled={isMutating}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700"
            >
              Cerrar
            </button>
          </footer>
        </div>
      </section>
    </div>
  );
}
