import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm, useWatch } from "react-hook-form";

import {
  collectionReceptionSchema,
  type CollectionReceptionFormValues,
} from "../schemas/collectionReceptionSchema";
import type { Collection } from "../types/collection.types";
import {
  formatCollectionDateTime,
  formatCollectionWeight,
} from "../utils/collectionDisplay";
import { getCollectionLocationLabel } from "../utils/collectionLabels";

interface CollectionToReceptionModalProps {
  isOpen: boolean;
  collection: Collection | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: CollectionReceptionFormValues) => Promise<void>;
}

function getDefaultValues(
  collection: Collection | null,
): CollectionReceptionFormValues {
  return {
    verifiedWeightKg:
      collection?.approximateWeightKg !== null &&
      collection?.approximateWeightKg !== undefined
        ? String(collection.approximateWeightKg)
        : "",

    hasPersonalBelongings: collection?.hasPersonalBelongings ?? false,

    personalBelongingsDescription:
      collection?.personalBelongingsDescription ?? "",

    referralNotes: "",

    notes: "",
  };
}

export function CollectionToReceptionModal({
  isOpen,
  collection,
  isSubmitting,
  onClose,
  onSubmit,
}: CollectionToReceptionModalProps) {
  const {
    register,
    reset,
    control,
    setValue,
    clearErrors,
    handleSubmit,
    formState: { errors },
  } = useForm<CollectionReceptionFormValues>({
    resolver: zodResolver(collectionReceptionSchema),
    defaultValues: getDefaultValues(null),
  });

  const hasPersonalBelongings = useWatch({
    control,
    name: "hasPersonalBelongings",
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(getDefaultValues(collection));
  }, [isOpen, collection, reset]);

  if (!isOpen || !collection) {
    return null;
  }

  const belongingsField = register("hasPersonalBelongings");

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="collection-reception-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Cadena de custodia
            </p>

            <h2
              id="collection-reception-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Recibir mascota en instalaciones
            </h2>

            <p className="mt-2 text-sm text-slate-600">
              Confirma la información física al momento de recibir la mascota.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-7 px-6 py-6"
          noValidate
        >
          {/* RESUMEN */}
          <section className="rounded-xl border border-slate-200 bg-slate-50 p-5">
            <div className="grid gap-5 sm:grid-cols-2">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Mascota
                </p>

                <p className="mt-1 text-lg font-semibold text-slate-900">
                  {collection.petName}
                </p>

                <p className="mt-1 text-sm text-slate-600">
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

                <p className="mt-1 text-sm text-slate-600">
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

                <p className="mt-1 text-sm text-slate-500">
                  {formatCollectionDateTime(collection.collectedAt)}
                </p>
              </div>

              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Peso aproximado
                </p>

                <p className="mt-1 text-sm font-medium text-slate-800">
                  {formatCollectionWeight(collection.approximateWeightKg)}
                </p>
              </div>
            </div>

            {collection.veterinaryClinicName && (
              <div className="mt-5 border-t border-slate-200 pt-4">
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

          {/* QR */}
          <section className="rounded-xl border border-emerald-200 bg-emerald-50 p-5">
            <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
              Código de cadena de custodia
            </p>

            <p className="mt-2 break-all font-mono text-sm font-semibold text-emerald-950">
              {collection.qrCode}
            </p>

            <p className="mt-3 text-sm text-emerald-800">
              Este mismo código continuará identificado a la mascota después de
              crear la recepción. No se generará un segundo código.
            </p>
          </section>

          {/* PESO */}
          <section>
            <label
              htmlFor="collection-reception-weight"
              className="block text-sm font-medium text-slate-700"
            >
              Peso verificado
            </label>

            <p className="mt-1 text-sm text-slate-500">
              Confirma el peso real al recibir la mascota en las instalaciones.
            </p>

            <div className="relative mt-2">
              <input
                id="collection-reception-weight"
                type="number"
                min="0.01"
                max="999.99"
                step="0.01"
                inputMode="decimal"
                disabled={isSubmitting}
                {...register("verifiedWeightKg")}
                className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-500">
                kg
              </span>
            </div>

            {errors.verifiedWeightKg && (
              <p className="mt-2 text-sm text-red-600">
                {errors.verifiedWeightKg.message}
              </p>
            )}
          </section>

          {/* OBJETOS */}
          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Objetos personales
            </legend>

            <label className="flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);

                  if (!event.target.checked) {
                    clearErrors("personalBelongingsDescription");

                    setValue("personalBelongingsDescription", "", {
                      shouldDirty: true,
                    });
                  }
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-500"
              />

              <span>
                <span className="block text-sm font-medium text-slate-800">
                  Se recibieron objetos personales
                </span>

                <span className="mt-1 block text-sm text-slate-500">
                  Verifica nuevamente lo que realmente llegó junto con la
                  mascota.
                </span>
              </span>
            </label>

            {hasPersonalBelongings && (
              <div className="mt-4">
                <label
                  htmlFor="collection-reception-belongings"
                  className="block text-sm font-medium text-slate-700"
                >
                  Descripción de los objetos
                </label>

                <textarea
                  id="collection-reception-belongings"
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.personalBelongingsDescription && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.personalBelongingsDescription.message}
                  </p>
                )}
              </div>
            )}
          </fieldset>

          {/* REFERENCIA */}
          {collection.locationType === 2 && (
            <section>
              <label
                htmlFor="collection-reception-referral-notes"
                className="block text-sm font-medium text-slate-700"
              >
                Notas de referencia veterinaria
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <textarea
                id="collection-reception-referral-notes"
                rows={3}
                maxLength={1000}
                disabled={isSubmitting}
                {...register("referralNotes")}
                placeholder="Indicaciones, información de la veterinaria o detalles relevantes..."
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.referralNotes && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.referralNotes.message}
                </p>
              )}
            </section>
          )}

          {/* NOTAS */}
          <section>
            <label
              htmlFor="collection-reception-notes"
              className="block text-sm font-medium text-slate-700"
            >
              Notas de recepción
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <textarea
              id="collection-reception-notes"
              rows={4}
              maxLength={1000}
              disabled={isSubmitting}
              {...register("notes")}
              placeholder="Observaciones al momento de recibir la mascota..."
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.notes && (
              <p className="mt-2 text-sm text-red-600">
                {errors.notes.message}
              </p>
            )}
          </section>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? "Registrando recepción..."
                : "Confirmar recepción"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
