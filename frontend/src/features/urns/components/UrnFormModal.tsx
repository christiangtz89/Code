import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import { DEFAULT_URN_FORM_VALUES, urnToFormValues } from "../helpers";
import { urnSchema, type UrnFormValues } from "../schemas";
import type { Urn } from "../types";

interface UrnFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  urn: Urn | null;
  onClose: () => void;
  onSubmit: (values: UrnFormValues) => Promise<void>;
}

export function UrnFormModal({
  isOpen,
  mode,
  urn,
  onClose,
  onSubmit,
}: UrnFormModalProps) {
  const {
    register,
    reset,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UrnFormValues>({
    resolver: zodResolver(urnSchema),
    defaultValues: DEFAULT_URN_FORM_VALUES,
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(
      mode === "edit" && urn ? urnToFormValues(urn) : DEFAULT_URN_FORM_VALUES,
    );
  }, [isOpen, mode, reset, urn]);

  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <section className="max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-5">
          <h2 className="text-xl font-semibold text-slate-900">
            {mode === "create" ? "Registrar urna" : "Editar urna"}
          </h2>

          <p className="mt-1 text-sm text-slate-500">
            Configura la información comercial y visual de la urna.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 p-6">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">
                Nombre *
              </span>

              <input
                {...register("name")}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              />

              {errors.name && (
                <span className="text-xs text-rose-600">
                  {errors.name.message}
                </span>
              )}
            </label>

            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">
                Precio *
              </span>

              <input
                type="number"
                min={0}
                step="0.01"
                {...register("price", {
                  valueAsNumber: true,
                })}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              />

              {errors.price && (
                <span className="text-xs text-rose-600">
                  {errors.price.message}
                </span>
              )}
            </label>

            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">
                Material
              </span>

              <input
                {...register("material")}
                placeholder="Ej. Cerámica"
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              />
            </label>

            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">Color</span>

              <input
                {...register("color")}
                placeholder="Ej. Blanco"
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              />
            </label>

            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">
                Orden de visualización *
              </span>

              <input
                type="number"
                min={0}
                {...register("displayOrder", {
                  valueAsNumber: true,
                })}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              />

              {errors.displayOrder && (
                <span className="text-xs text-rose-600">
                  {errors.displayOrder.message}
                </span>
              )}
            </label>
          </div>

          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              Descripción
            </span>

            <textarea
              rows={4}
              {...register("description")}
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            />
          </label>

          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              URL de imagen
            </span>

            <input
              {...register("imageUrl")}
              placeholder="La carga de imágenes se agregará después"
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            />

            <p className="text-xs text-slate-500">
              Más adelante podremos cargar la fotografía directamente desde
              PCMS.
            </p>
          </label>

          <div className="grid gap-3 md:grid-cols-2">
            <label className="flex items-center gap-3 rounded-xl border border-slate-200 p-4">
              <input type="checkbox" {...register("isPublic")} />

              <div>
                <div className="text-sm font-medium text-slate-800">
                  Visible al público
                </div>

                <div className="text-xs text-slate-500">
                  Podrá mostrarse en la landing page.
                </div>
              </div>
            </label>

            <label className="flex items-center gap-3 rounded-xl border border-slate-200 p-4">
              <input type="checkbox" {...register("isActive")} />

              <div>
                <div className="text-sm font-medium text-slate-800">Activa</div>

                <div className="text-xs text-slate-500">
                  Disponible dentro de PCMS.
                </div>
              </div>
            </label>
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-200 pt-5">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar urna"
                  : "Guardar cambios"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
