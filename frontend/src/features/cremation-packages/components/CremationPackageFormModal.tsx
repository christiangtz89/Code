import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  DEFAULT_CREMATION_PACKAGE_FORM_VALUES,
  cremationPackageToFormValues,
} from "../helpers";
import {
  cremationPackageSchema,
  type CremationPackageFormValues,
} from "../schemas";
import { CREMATION_PACKAGE_TYPE, type CremationPackage } from "../types";
import { useUrns } from "../../urns/hooks";

interface CremationPackageFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  cremationPackage: CremationPackage | null;
  onClose: () => void;
  onSubmit: (values: CremationPackageFormValues) => Promise<void>;
}

export function CremationPackageFormModal({
  isOpen,
  mode,
  cremationPackage,
  onClose,
  onSubmit,
}: CremationPackageFormModalProps) {
  const {
    register,
    reset,
    watch,
    setValue,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CremationPackageFormValues>({
    resolver: zodResolver(cremationPackageSchema),
    defaultValues: DEFAULT_CREMATION_PACKAGE_FORM_VALUES,
  });

  const packageType = watch("packageType");

  const tier = watch("tier");

  const includesUrn = watch("includesUrn");

  const allowedUrnIds = watch("allowedUrnIds") ?? [];

  const urnsQuery = useUrns(true);

  const selectableUrns = (urnsQuery.data ?? []).filter(
    (urn) => urn.isActive || allowedUrnIds.includes(urn.id),
  );

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(
      mode === "edit" && cremationPackage
        ? cremationPackageToFormValues(cremationPackage)
        : DEFAULT_CREMATION_PACKAGE_FORM_VALUES,
    );
  }, [cremationPackage, isOpen, mode, reset]);

  useEffect(() => {
    if (packageType === CREMATION_PACKAGE_TYPE.NO_ASHES) {
      setValue("tier", null, {
        shouldValidate: true,
      });

      setValue("includesUrn", false);
      setValue("includesPawPrint", false);
      setValue("accessoryDescription", null);
      setValue("includesCertificate", false);

      return;
    }

    if (tier === null) {
      setValue("tier", 1, {
        shouldValidate: true,
      });
    }
  }, [packageType, tier, setValue]);

  useEffect(() => {
    if (!includesUrn) {
      setValue("allowedUrnIds", [], {
        shouldValidate: true,
      });
    }
  }, [includesUrn, setValue]);

  if (!isOpen) {
    return null;
  }

  const isNoAshes = packageType === CREMATION_PACKAGE_TYPE.NO_ASHES;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <section className="max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-5">
          <h2 className="text-xl font-semibold text-slate-900">
            {mode === "create" ? "Registrar paquete" : "Editar paquete"}
          </h2>

          <p className="mt-1 text-sm text-slate-500">
            Configura la información visible y operativa del servicio.
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
                Tipo de servicio *
              </span>

              <select
                {...register("packageType", {
                  valueAsNumber: true,
                })}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              >
                <option value={CREMATION_PACKAGE_TYPE.ASHES_RETURN}>
                  Con devolución de cenizas
                </option>

                <option value={CREMATION_PACKAGE_TYPE.NO_ASHES}>
                  Sin devolución de cenizas
                </option>
              </select>
            </label>

            <label className="space-y-1">
              <span className="text-sm font-medium text-slate-700">Nivel</span>

              {isNoAshes ? (
                <input
                  type="text"
                  value="Sin nivel"
                  disabled
                  className="w-full rounded-lg border border-slate-300 bg-slate-100 px-3 py-2 text-slate-600"
                />
              ) : (
                <select
                  {...register("tier", {
                    setValueAs: (value) =>
                      value === "" || value === null || value === undefined
                        ? null
                        : Number(value),
                  })}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2"
                >
                  <option value={1}>Nivel 1</option>
                  <option value={2}>Nivel 2</option>
                  <option value={3}>Nivel 3</option>
                  <option value={4}>Nivel 4</option>
                </select>
              )}

              {errors.tier && (
                <span className="text-xs text-rose-600">
                  {errors.tier.message}
                </span>
              )}
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
              Descripción corta
            </span>

            <input
              {...register("shortDescription")}
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            />
          </label>

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
          </label>

          <div className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-medium text-slate-900">
              Contenido del paquete
            </h3>

            {isNoAshes && (
              <p className="mt-1 text-sm text-slate-500">
                La cremación comunitaria no incluye devolución de cenizas, urna,
                accesorio ni certificado.
              </p>
            )}

            <div className="mt-4 grid gap-3 md:grid-cols-3">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  disabled={isNoAshes}
                  {...register("includesUrn")}
                />
                <span className="text-sm text-slate-700">Incluye urna</span>
              </label>

              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  disabled={isNoAshes}
                  {...register("includesPawPrint")}
                />
                <span className="text-sm text-slate-700">
                  Incluye accesorio
                </span>
              </label>

              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  disabled={isNoAshes}
                  {...register("includesCertificate")}
                />
                <span className="text-sm text-slate-700">
                  Incluye certificado
                </span>
              </label>
            </div>

            {!isNoAshes && includesUrn && (
              <div className="mt-5 rounded-xl bg-slate-50 p-4">
                <div>
                  <p className="text-sm font-medium text-slate-800">
                    Urnas permitidas *
                  </p>

                  <p className="mt-1 text-xs text-slate-500">
                    Solo estas urnas estarán disponibles al registrar una
                    cremación con este paquete.
                  </p>
                </div>

                {urnsQuery.isLoading && (
                  <p className="mt-3 text-sm text-slate-500">
                    Cargando urnas...
                  </p>
                )}

                {urnsQuery.isError && (
                  <p className="mt-3 text-sm text-rose-600">
                    No fue posible cargar el catálogo de urnas.
                  </p>
                )}

                {!urnsQuery.isLoading &&
                  !urnsQuery.isError &&
                  selectableUrns.length === 0 && (
                    <p className="mt-3 text-sm text-amber-700">
                      No hay urnas activas disponibles. Registra o activa una
                      urna antes de guardar este paquete.
                    </p>
                  )}

                {selectableUrns.length > 0 && (
                  <div className="mt-3 grid gap-2 sm:grid-cols-2">
                    {selectableUrns.map((urn) => (
                      <label
                        key={urn.id}
                        className="flex items-start gap-2 rounded-lg border border-slate-200 bg-white p-3"
                      >
                        <input
                          type="checkbox"
                          value={urn.id}
                          {...register("allowedUrnIds")}
                          className="mt-0.5"
                        />

                        <span className="text-sm text-slate-700">
                          {urn.name}
                          {!urn.isActive ? " — Inactiva" : ""}
                        </span>
                      </label>
                    ))}
                  </div>
                )}

                {errors.allowedUrnIds && (
                  <p className="mt-2 text-xs text-rose-600">
                    {errors.allowedUrnIds.message}
                  </p>
                )}
              </div>
            )}

            {!isNoAshes && (
              <label className="mt-4 block space-y-1">
                <span className="text-sm font-medium text-slate-700">
                  Descripción del accesorio
                </span>

                <input
                  {...register("accessoryDescription")}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2"
                />
              </label>
            )}
          </div>

          <div className="grid gap-3 md:grid-cols-2">
            <label className="flex items-center gap-2 rounded-xl border border-slate-200 p-4">
              <input type="checkbox" {...register("isPublic")} />

              <div>
                <div className="text-sm font-medium text-slate-800">
                  Visible al público
                </div>

                <div className="text-xs text-slate-500">
                  Aparecerá en la landing page.
                </div>
              </div>
            </label>

            <label className="flex items-center gap-2 rounded-xl border border-slate-200 p-4">
              <input type="checkbox" {...register("isActive")} />

              <div>
                <div className="text-sm font-medium text-slate-800">Activo</div>

                <div className="text-xs text-slate-500">
                  Disponible para uso dentro de PCMS.
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
                  ? "Registrar paquete"
                  : "Guardar cambios"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
