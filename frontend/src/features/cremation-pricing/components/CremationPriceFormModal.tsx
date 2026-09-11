import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";

import { CremationType } from "../../cremations/types/cremation.types";
import {
  CREMATION_PACKAGE_TYPE,
  type CremationPackage,
} from "../../cremation-packages/types";

import { DEFAULT_CREMATION_PRICE_FORM_VALUES } from "../helpers";

import {
  cremationPriceSchema,
  type CremationPriceFormValues,
} from "../schemas";

import type { CremationPrice, CremationPricingConfiguration } from "../types";
import { getWeightRangeOptions } from "../utils";

interface CremationPriceFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  price: CremationPrice | null;
  packages: CremationPackage[];
  configuration: CremationPricingConfiguration;
  onClose: () => void;
  onSubmit: (values: CremationPriceFormValues) => Promise<void>;
}

export function CremationPriceFormModal({
  isOpen,
  mode,
  price,
  packages,
  configuration,
  onClose,
  onSubmit,
}: CremationPriceFormModalProps) {
  const {
    register,
    reset,
    watch,
    setValue,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CremationPriceFormValues>({
    resolver: zodResolver(cremationPriceSchema),
    defaultValues: DEFAULT_CREMATION_PRICE_FORM_VALUES,
  });

  const selectedPackageId = watch("cremationPackageId");

  const minimumWeightKg = watch("minimumWeightKg");
  const maximumWeightKg = watch("maximumWeightKg");

  const weightRanges = useMemo(
    () => getWeightRangeOptions(configuration.weightInterval),
    [configuration.weightInterval],
  );

  const selectedWeightRange = `${minimumWeightKg}|${maximumWeightKg}`;

  const selectedPackage = useMemo(
    () => packages.find((item) => item.id === selectedPackageId) ?? null,
    [packages, selectedPackageId],
  );

  const selectablePackages = useMemo(() => {
    if (mode === "edit" && price) {
      return packages.filter(
        (item) => item.isActive || item.id === price.cremationPackageId,
      );
    }

    return packages.filter((item) => item.isActive);
  }, [mode, packages, price]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (mode === "edit" && price) {
      reset({
        cremationPackageId: price.cremationPackageId,
        cremationType: price.cremationType,
        minimumWeightKg: price.minimumWeightKg,
        maximumWeightKg: price.maximumWeightKg,
        price: price.price.toString(),
        requiredCollectionPaymentAmount:
          price.requiredCollectionPaymentAmount?.toString() ?? "",
        isPublic: price.isPublic,
        isActive: price.isActive,
      });

      return;
    }

    reset({
      ...DEFAULT_CREMATION_PRICE_FORM_VALUES,
      maximumWeightKg: configuration.weightInterval,
    });
  }, [configuration.weightInterval, isOpen, mode, price, reset]);

  useEffect(() => {
    if (!selectedPackage) {
      return;
    }

    if (selectedPackage.packageType === CREMATION_PACKAGE_TYPE.ASHES_RETURN) {
      setValue("cremationType", CremationType.Individual);

      return;
    }

    if (!configuration.allowIndividualNoAshes) {
      setValue("cremationType", CremationType.Communal);
    }
  }, [configuration.allowIndividualNoAshes, selectedPackage, setValue]);

  if (!isOpen) {
    return null;
  }

  const isNoAshes =
    selectedPackage?.packageType === CREMATION_PACKAGE_TYPE.NO_ASHES;

  const canChooseCremationType =
    isNoAshes && configuration.allowIndividualNoAshes;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <section className="max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-5">
          <h2 className="text-xl font-semibold text-slate-900">
            {mode === "create" ? "Registrar precio" : "Editar precio"}
          </h2>

          <p className="mt-1 text-sm text-slate-500">
            Escala activa: cada {configuration.weightInterval} kg.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 p-6">
          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              Paquete o servicio *
            </span>

            <select
              {...register("cremationPackageId")}
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            >
              <option value="">Selecciona un paquete</option>

              {selectablePackages.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                  {!item.isActive ? " — Inactivo" : ""}
                </option>
              ))}
            </select>

            {errors.cremationPackageId && (
              <span className="text-xs text-rose-600">
                {errors.cremationPackageId.message}
              </span>
            )}
          </label>

          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              Tipo de cremación *
            </span>

            {canChooseCremationType ? (
              <select
                {...register("cremationType", {
                  valueAsNumber: true,
                })}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              >
                <option value={CremationType.Communal}>Comunitaria</option>

                <option value={CremationType.Individual}>Individual</option>
              </select>
            ) : (
              <>
                <input
                  type="text"
                  readOnly
                  value={isNoAshes ? "Comunitaria" : "Individual"}
                  className="w-full rounded-lg border border-slate-300 bg-slate-100 px-3 py-2 text-slate-700"
                />

                <input
                  type="hidden"
                  {...register("cremationType", {
                    valueAsNumber: true,
                  })}
                />
              </>
            )}

            {isNoAshes && !configuration.allowIndividualNoAshes && (
              <p className="text-xs text-slate-500">
                La opción individual sin devolución de cenizas está oculta
                actualmente.
              </p>
            )}
          </label>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="block space-y-1">
              <span className="text-sm font-medium text-slate-700">
                Rango de peso *
              </span>

              <select
                value={selectedWeightRange}
                onChange={(event) => {
                  const [minimum, maximum] = event.target.value.split("|");

                  setValue("minimumWeightKg", Number(minimum), {
                    shouldValidate: true,
                  });

                  setValue("maximumWeightKg", Number(maximum), {
                    shouldValidate: true,
                  });
                }}
                className="w-full rounded-lg border border-slate-300 px-3 py-2"
              >
                {weightRanges.map((range) => (
                  <option
                    key={`${range.minimumWeightKg}-${range.maximumWeightKg}`}
                    value={`${range.minimumWeightKg}|${range.maximumWeightKg}`}
                  >
                    {range.label}
                  </option>
                ))}
              </select>

              <input
                type="hidden"
                {...register("minimumWeightKg", {
                  valueAsNumber: true,
                })}
              />

              <input
                type="hidden"
                {...register("maximumWeightKg", {
                  valueAsNumber: true,
                })}
              />

              {(errors.minimumWeightKg || errors.maximumWeightKg) && (
                <span className="text-xs text-rose-600">
                  {errors.minimumWeightKg?.message ??
                    errors.maximumWeightKg?.message}
                </span>
              )}

              <p className="text-xs text-slate-500">
                Los rangos se generan automáticamente según la escala activa de{" "}
                {configuration.weightInterval} kg.
              </p>
            </label>
          </div>

          <div className="rounded-xl bg-slate-50 p-4">
            <p className="text-sm text-slate-600">
              Los rangos deben respetar la escala activa de{" "}
              {configuration.weightInterval} kg.
            </p>

            <p className="mt-1 text-xs text-slate-500">
              Ejemplo:{" "}
              {configuration.weightInterval === 5
                ? "0–5, 5.01–10, 10.01–15 kg"
                : "0–10, 10.01–20, 20.01–30 kg"}
            </p>
          </div>

          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              Precio (MXN) *
            </span>

            <input
              type="text"
              inputMode="decimal"
              placeholder="Ej. 1500.00"
              {...register("price")}
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            />

            {errors.price && (
              <span className="text-xs text-rose-600">
                {errors.price.message}
              </span>
            )}
          </label>

          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">
              Pago requerido antes de recepción (MXN) *
            </span>

            <input
              type="text"
              inputMode="decimal"
              placeholder="Ej. 500.00"
              {...register("requiredCollectionPaymentAmount")}
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            />

            {errors.requiredCollectionPaymentAmount && (
              <span className="text-xs text-rose-600">
                {errors.requiredCollectionPaymentAmount.message}
              </span>
            )}

            <p className="text-xs text-slate-500">
              Este monto debe estar pagado para recibir una recolección.
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
                  Podrá mostrarse en el catálogo público.
                </div>
              </div>
            </label>

            <label className="flex items-center gap-3 rounded-xl border border-slate-200 p-4">
              <input type="checkbox" {...register("isActive")} />

              <div>
                <div className="text-sm font-medium text-slate-800">
                  Precio activo
                </div>

                <div className="text-xs text-slate-500">
                  Disponible para cotizaciones internas.
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
                  ? "Registrar precio"
                  : "Guardar cambios"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
