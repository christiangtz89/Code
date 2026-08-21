import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";

import {
  cremationSchema,
  type CremationFormValues,
} from "../schemas/cremationSchema";
import { CremationType, type Cremation } from "../types/cremation.types";
import type {
  CremationReceptionOption,
  CremationUserOption,
} from "../types/cremationForm.types";
import {
  CREMATION_TIME_OPTIONS,
  cremationToFormValues,
  defaultCremationFormValues,
} from "../utils/cremationForm";
import { useCremationPackages } from "../../cremation-packages/hooks";
import { useUrns } from "../../urns/hooks";

import { CREMATION_PACKAGE_TYPE } from "../../cremation-packages/types";
import { useQuery } from "@tanstack/react-query";
import { getReceptionById } from "../../receptions/api/receptionsApi";
import { useCremationPriceQuote } from "../../cremation-pricing/hooks";
import {
  formatCurrency,
  formatWeightRange,
} from "../../cremation-pricing/utils";

interface CremationFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  cremation: Cremation | null;

  receptions: CremationReceptionOption[];
  users: CremationUserOption[];

  isLoadingReceptions: boolean;
  isLoadingUsers: boolean;
  isSubmitting: boolean;

  onClose: () => void;

  onSubmit: (values: CremationFormValues) => Promise<void>;
}

export function CremationFormModal({
  isOpen,
  mode,
  cremation,
  receptions,
  users,
  isLoadingReceptions,
  isLoadingUsers,
  isSubmitting,
  onClose,
  onSubmit,
}: CremationFormModalProps) {
  const {
    register,
    reset,
    setValue,
    watch,
    handleSubmit,
    clearErrors,
    setError,
    formState: { errors },
  } = useForm<CremationFormValues>({
    resolver: zodResolver(cremationSchema),
    defaultValues: defaultCremationFormValues,
  });

  const selectedPackageId = watch("cremationPackageId");

  const selectedReceptionId = watch("receptionId");

  const selectedUrnId = watch("urnId");

  const packagesQuery = useCremationPackages(true);

  const urnsQuery = useUrns(true);

  const packages = packagesQuery.data;
  const urns = urnsQuery.data;

  const selectedReceptionQuery = useQuery({
    queryKey: ["receptions", "cremation-quote", selectedReceptionId],

    queryFn: () => getReceptionById(selectedReceptionId),

    enabled: isOpen && selectedReceptionId.length > 0,
  });

  const selectedReception = selectedReceptionQuery.data ?? null;

  const selectedPackage =
    packages?.find((item) => item.id === selectedPackageId) ?? null;

  const isOriginalPackage =
    mode === "edit" &&
    cremation !== null &&
    cremation.cremationPackageId !== null &&
    selectedPackageId === cremation.cremationPackageId;

  const selectablePackages = (packages ?? []).filter(
    (item) =>
      item.isActive ||
      (mode === "edit" && item.id === cremation?.cremationPackageId),
  );

  const packageIncludesUrn = isOriginalPackage
    ? cremation.includesUrn
    : (selectedPackage?.includesUrn ?? false);

  const packageIncludesAccessory = isOriginalPackage
    ? cremation.includesPawPrint
    : (selectedPackage?.includesPawPrint ?? false);

  const packageIncludesCertificate = isOriginalPackage
    ? cremation.includesCertificate
    : (selectedPackage?.includesCertificate ?? false);

  const isNoAshesPackage = isOriginalPackage
    ? cremation.cremationType === CremationType.Communal
    : selectedPackage?.packageType === CREMATION_PACKAGE_TYPE.NO_ASHES;

  const allowedUrnIds = selectedPackage?.allowedUrnIds ?? [];

  const selectableUrns = (urns ?? []).filter((urn) => {
    if (isOriginalPackage && urn.id === cremation?.urnId) {
      return true;
    }

    return urn.isActive && allowedUrnIds.includes(urn.id);
  });

  function handlePackageSelection(packageId: string) {
    const packageChanged = packageId !== selectedPackageId;

    setValue("cremationPackageId", packageId, {
      shouldDirty: true,
      shouldValidate: true,
    });

    if (packageChanged) {
      const isRestoringOriginalPackage =
        mode === "edit" &&
        cremation !== null &&
        packageId === cremation.cremationPackageId;

      setValue(
        "urnId",
        isRestoringOriginalPackage ? (cremation.urnId ?? "") : "",
        {
          shouldDirty: true,
          shouldValidate: true,
        },
      );

      setValue(
        "accessoryDescription",
        isRestoringOriginalPackage
          ? (cremation.accessoryDescription ?? "")
          : "",
        {
          shouldDirty: true,
          shouldValidate: true,
        },
      );
    }

    clearErrors(["cremationPackageId", "urnId"]);
  }

  function handleUrnSelection(urnId: string) {
    setValue("urnId", urnId, {
      shouldDirty: true,
      shouldValidate: true,
    });

    clearErrors("urnId");
  }

  const quoteParams =
    selectedPackage &&
    selectedReception &&
    selectedReception.verifiedWeightKg > 0 &&
    !isOriginalPackage
      ? {
          cremationPackageId: selectedPackage.id,
          weightKg: selectedReception.verifiedWeightKg,
        }
      : null;

  const priceQuoteQuery = useCremationPriceQuote(quoteParams);

  const priceQuote = priceQuoteQuery.data ?? null;

  const quotedWeightKg = cremation?.quotedWeightKg ?? null;
  const quotedMinimumWeightKg = cremation?.quotedMinimumWeightKg ?? null;
  const quotedMaximumWeightKg = cremation?.quotedMaximumWeightKg ?? null;
  const currentVerifiedWeightKg = selectedReception?.verifiedWeightKg ?? null;

  const hasVerifiedWeightMismatch =
    mode === "edit" &&
    quotedWeightKg !== null &&
    currentVerifiedWeightKg !== null &&
    Math.abs(currentVerifiedWeightKg - quotedWeightKg) >= 0.01;

  const isCurrentWeightOutsideQuotedRange =
    hasVerifiedWeightMismatch &&
    quotedMinimumWeightKg !== null &&
    quotedMaximumWeightKg !== null &&
    currentVerifiedWeightKg !== null &&
    (currentVerifiedWeightKg < quotedMinimumWeightKg ||
      currentVerifiedWeightKg > quotedMaximumWeightKg);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (mode === "edit" && cremation) {
      reset(cremationToFormValues(cremation));

      return;
    }

    reset(defaultCremationFormValues);
  }, [cremation, isOpen, mode, reset]);

  if (!isOpen) {
    return null;
  }

  const noAvailableReceptions =
    mode === "create" && !isLoadingReceptions && receptions.length === 0;

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  async function submit(values: CremationFormValues) {
    if (packageIncludesUrn && !values.urnId) {
      setError("urnId", {
        type: "manual",
        message: "Selecciona una urna permitida para este paquete.",
      });

      return;
    }

    await onSubmit(values);
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
        aria-labelledby="cremation-form-title"
        onMouseDown={(event) => event.stopPropagation()}
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">Operación</p>

            <h2
              id="cremation-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create" ? "Registrar cremación" : "Editar cremación"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {mode === "create"
                ? "Configura el servicio y su programación inicial."
                : "Actualiza los detalles del servicio de cremación."}
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
          onSubmit={handleSubmit(submit)}
          noValidate
          className="space-y-7 px-6 py-6"
        >
          <section>
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Recepción
            </h3>

            {mode === "create" ? (
              <div className="mt-4">
                <label
                  htmlFor="cremation-reception"
                  className="block text-sm font-medium text-slate-700"
                >
                  Recepción
                </label>

                <select
                  id="cremation-reception"
                  disabled={isSubmitting || isLoadingReceptions}
                  {...register("receptionId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {isLoadingReceptions
                      ? "Cargando recepciones..."
                      : "Selecciona una recepción"}
                  </option>

                  {receptions.map((reception) => (
                    <option key={reception.id} value={reception.id}>
                      {reception.petName} — {reception.customerName} —{" "}
                      {reception.qrCode}
                    </option>
                  ))}
                </select>

                {errors.receptionId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.receptionId.message}
                  </p>
                )}

                {noAvailableReceptions && (
                  <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
                    No hay recepciones disponibles para registrar una cremación.
                  </p>
                )}
              </div>
            ) : (
              <>
                <input type="hidden" {...register("receptionId")} />

                <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
                  <div className="grid gap-4 sm:grid-cols-3">
                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Mascota
                      </p>

                      <p className="mt-1 font-medium text-slate-900">
                        {cremation?.petName}
                      </p>
                    </div>

                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Cliente
                      </p>

                      <p className="mt-1 text-sm text-slate-700">
                        {cremation?.customerName}
                      </p>
                    </div>

                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                        Código QR
                      </p>

                      <p className="mt-1 font-mono text-sm text-slate-700">
                        {cremation?.qrCode}
                      </p>
                    </div>
                  </div>

                  <p className="mt-3 text-xs text-slate-500">
                    La recepción no puede cambiarse después de registrar la
                    cremación.
                  </p>
                </div>
              </>
            )}
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Servicio
            </h3>

            <div className="mt-4 space-y-5">
              <div>
                <input type="hidden" {...register("cremationPackageId")} />

                <p className="text-sm font-medium text-slate-700">
                  Paquete o servicio *
                </p>

                <p className="mt-1 text-sm text-slate-500">
                  Selecciona el servicio que corresponde a esta cremación.
                </p>

                {packagesQuery.isLoading ? (
                  <div className="mt-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
                    <p className="text-sm text-slate-500">
                      Cargando paquetes...
                    </p>
                  </div>
                ) : (
                  <div className="mt-3 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                    {selectablePackages.map((cremationPackage) => {
                      const isSelected =
                        selectedPackageId === cremationPackage.id;

                      return (
                        <button
                          key={cremationPackage.id}
                          type="button"
                          disabled={isSubmitting}
                          onClick={() =>
                            handlePackageSelection(cremationPackage.id)
                          }
                          className={[
                            "overflow-hidden rounded-xl border text-left transition",
                            isSelected
                              ? "border-slate-900 ring-2 ring-slate-900/10"
                              : "border-slate-200 hover:border-slate-400",
                            isSubmitting ? "cursor-not-allowed opacity-60" : "",
                          ].join(" ")}
                        >
                          <div className="aspect-[16/9] bg-slate-100">
                            {cremationPackage.imageUrl ? (
                              <img
                                src={cremationPackage.imageUrl}
                                alt={cremationPackage.name}
                                className="h-full w-full object-cover"
                              />
                            ) : (
                              <div className="flex h-full items-center justify-center px-4 text-center text-sm text-slate-400">
                                Sin imagen
                              </div>
                            )}
                          </div>

                          <div className="p-4">
                            <div className="flex items-start justify-between gap-3">
                              <div>
                                <p className="font-semibold text-slate-900">
                                  {cremationPackage.name}
                                </p>

                                {cremationPackage.tier && (
                                  <p className="mt-1 text-xs font-medium uppercase tracking-wide text-slate-500">
                                    Nivel {cremationPackage.tier}
                                  </p>
                                )}
                              </div>

                              {isSelected && (
                                <span className="rounded-full bg-slate-900 px-2.5 py-1 text-xs font-semibold text-white">
                                  Seleccionado
                                </span>
                              )}
                            </div>

                            {cremationPackage.shortDescription && (
                              <p className="mt-3 text-sm text-slate-600">
                                {cremationPackage.shortDescription}
                              </p>
                            )}

                            <div className="mt-4 flex flex-wrap gap-2">
                              {cremationPackage.includesUrn && (
                                <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                                  Incluye urna
                                </span>
                              )}

                              {cremationPackage.includesPawPrint && (
                                <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                                  Accesorio
                                </span>
                              )}

                              {cremationPackage.includesCertificate && (
                                <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                                  Certificado
                                </span>
                              )}

                              {!cremationPackage.isActive && (
                                <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs text-amber-700">
                                  Inactivo
                                </span>
                              )}
                            </div>
                          </div>
                        </button>
                      );
                    })}
                  </div>
                )}
                {isOriginalPackage && cremation && (
                  <div className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 p-4">
                    <p className="text-sm font-medium text-emerald-800">
                      Cotización guardada
                    </p>

                    {cremation.quotedPrice !== null &&
                    cremation.quotedWeightKg !== null &&
                    cremation.quotedMinimumWeightKg !== null &&
                    cremation.quotedMaximumWeightKg !== null ? (
                      <div className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                        <p className="text-2xl font-bold text-emerald-950">
                          {formatCurrency(cremation.quotedPrice)}
                        </p>

                        <div className="text-sm text-emerald-800 sm:text-right">
                          <p>
                            Peso cotizado: {cremation.quotedWeightKg.toFixed(2)}
                            {" kg"}
                          </p>

                          <p>
                            Rango:{" "}
                            {formatWeightRange(
                              cremation.quotedMinimumWeightKg,
                              cremation.quotedMaximumWeightKg,
                            )}
                          </p>
                        </div>
                      </div>
                    ) : (
                      <p className="mt-2 text-sm text-emerald-800">
                        Esta cremación no cuenta con una cotización histórica.
                      </p>
                    )}

                    <p className="mt-3 border-t border-emerald-200 pt-3 text-xs text-emerald-700">
                      Esta cotización se conserva mientras no cambies el
                      paquete.
                    </p>
                  </div>
                )}

                {isOriginalPackage && hasVerifiedWeightMismatch && (
                  <div
                    className={[
                      "mt-4 rounded-xl border p-4",
                      isCurrentWeightOutsideQuotedRange
                        ? "border-red-200 bg-red-50"
                        : "border-amber-200 bg-amber-50",
                    ].join(" ")}
                  >
                    <p
                      className={[
                        "text-sm font-semibold",
                        isCurrentWeightOutsideQuotedRange
                          ? "text-red-900"
                          : "text-amber-900",
                      ].join(" ")}
                    >
                      {isCurrentWeightOutsideQuotedRange
                        ? "El peso verificado pertenece a un rango diferente"
                        : "El peso verificado cambió"}
                    </p>

                    <div
                      className={[
                        "mt-3 grid gap-3 text-sm sm:grid-cols-2",
                        isCurrentWeightOutsideQuotedRange
                          ? "text-red-800"
                          : "text-amber-800",
                      ].join(" ")}
                    >
                      <div>
                        <p className="text-xs font-medium uppercase tracking-wide opacity-70">
                          Peso utilizado en la cotización
                        </p>

                        <p className="mt-1 font-semibold">
                          {quotedWeightKg?.toFixed(2)} kg
                        </p>
                      </div>

                      <div>
                        <p className="text-xs font-medium uppercase tracking-wide opacity-70">
                          Peso verificado actual
                        </p>

                        <p className="mt-1 font-semibold">
                          {currentVerifiedWeightKg?.toFixed(2)} kg
                        </p>
                      </div>
                    </div>

                    {isCurrentWeightOutsideQuotedRange ? (
                      <>
                        <p className="mt-3 text-sm text-red-800">
                          El peso actual ya no se encuentra dentro del rango
                          utilizado para calcular la cotización guardada.
                        </p>

                        <p className="mt-2 text-sm font-medium text-red-900">
                          Revisa el precio antes de continuar. PCMS no modificó
                          automáticamente la cotización ni el total del
                          servicio.
                        </p>
                      </>
                    ) : (
                      <p className="mt-3 text-sm text-amber-800">
                        El peso cambió, pero continúa dentro del mismo rango de
                        la cotización. El precio guardado se conserva.
                      </p>
                    )}
                  </div>
                )}

                {selectedReception && !isOriginalPackage && (
                  <div className="mt-4 rounded-xl border border-sky-200 bg-sky-50 p-4">
                    <p className="text-sm font-medium text-sky-900">
                      {mode === "edit"
                        ? "Peso verificado actual"
                        : "Peso verificado"}
                    </p>

                    <p className="mt-1 text-xl font-semibold text-sky-950">
                      {selectedReception.verifiedWeightKg.toFixed(2)} kg
                    </p>

                    <p className="mt-1 text-xs text-sky-700">
                      Este peso proviene de la recepción y se utilizará
                      automáticamente para calcular la nueva cotización.
                    </p>
                  </div>
                )}

                {quoteParams && priceQuoteQuery.isLoading && (
                  <div className="mt-4 rounded-xl border border-slate-200 bg-white p-4">
                    <p className="text-sm text-slate-500">
                      Calculando precio...
                    </p>
                  </div>
                )}

                {priceQuote && (
                  <div className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 p-4">
                    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                      <div>
                        <p className="text-sm font-medium text-emerald-800">
                          {mode === "edit"
                            ? "Nueva cotización propuesta"
                            : "Cotización automática"}
                        </p>

                        <p className="mt-1 text-2xl font-bold text-emerald-950">
                          {formatCurrency(priceQuote.price)}
                        </p>

                        <p className="mt-2 text-sm text-emerald-800">
                          {priceQuote.cremationPackageName}
                        </p>
                      </div>

                      <div className="text-sm text-emerald-800 sm:text-right">
                        <p>
                          Peso:{" "}
                          <strong>{priceQuote.weightKg.toFixed(2)} kg</strong>
                        </p>

                        <p>
                          Rango:{" "}
                          <strong>
                            {formatWeightRange(
                              priceQuote.minimumWeightKg,
                              priceQuote.maximumWeightKg,
                            )}
                          </strong>
                        </p>
                      </div>
                    </div>

                    <p className="mt-3 border-t border-emerald-200 pt-3 text-xs text-emerald-700">
                      {mode === "edit"
                        ? "Esta cotización reemplazará la guardada al confirmar el cambio de paquete."
                        : "Precio calculado automáticamente con el peso verificado de la recepción."}
                    </p>
                  </div>
                )}

                {quoteParams && priceQuoteQuery.isError && (
                  <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4">
                    <p className="text-sm font-medium text-red-800">
                      No hay un precio configurado para esta combinación.
                    </p>

                    <p className="mt-1 text-xs text-red-700">
                      Revisa el paquete, el peso verificado y la escala activa
                      en Catálogo y precios → Precios.
                    </p>
                  </div>
                )}

                {errors.cremationPackageId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.cremationPackageId.message}
                  </p>
                )}
              </div>

              {selectedPackage && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
                  <p className="font-semibold text-slate-900">
                    {isOriginalPackage && cremation
                      ? cremation.packageName
                      : selectedPackage.name}
                  </p>

                  {!isOriginalPackage && selectedPackage.shortDescription && (
                    <p className="mt-1 text-sm text-slate-600">
                      {selectedPackage.shortDescription}
                    </p>
                  )}

                  <div className="mt-3 flex flex-wrap gap-2">
                    <span className="rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700">
                      {isNoAshesPackage
                        ? "Cremación comunitaria"
                        : "Cremación individual"}
                    </span>

                    {packageIncludesUrn && (
                      <span className="rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700">
                        Incluye urna
                      </span>
                    )}

                    {packageIncludesAccessory && (
                      <span className="rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700">
                        Incluye accesorio
                      </span>
                    )}

                    {packageIncludesCertificate && (
                      <span className="rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700">
                        Incluye certificado
                      </span>
                    )}
                  </div>
                </div>
              )}

              {packageIncludesUrn && (
                <div>
                  <input type="hidden" {...register("urnId")} />

                  <p className="text-sm font-medium text-slate-700">
                    Urna incluida *
                  </p>

                  <p className="mt-1 text-sm text-slate-500">
                    Selecciona una de las urnas disponibles para este paquete.
                  </p>

                  {urnsQuery.isLoading ? (
                    <div className="mt-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
                      <p className="text-sm text-slate-500">
                        Cargando urnas...
                      </p>
                    </div>
                  ) : urnsQuery.isError ? (
                    <div className="mt-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3">
                      <p className="text-sm text-red-700">
                        No fue posible cargar las urnas permitidas.
                      </p>
                    </div>
                  ) : selectableUrns.length === 0 ? (
                    <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
                      <p className="text-sm text-amber-800">
                        Este paquete no tiene urnas disponibles. Revisa la
                        configuración del paquete.
                      </p>
                    </div>
                  ) : (
                    <div className="mt-3 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                      {selectableUrns.map((urn) => {
                        const isSelected = selectedUrnId === urn.id;

                        return (
                          <button
                            key={urn.id}
                            type="button"
                            disabled={isSubmitting}
                            onClick={() => handleUrnSelection(urn.id)}
                            className={[
                              "overflow-hidden rounded-xl border text-left transition",
                              isSelected
                                ? "border-slate-900 ring-2 ring-slate-900/10"
                                : "border-slate-200 hover:border-slate-400",
                              isSubmitting
                                ? "cursor-not-allowed opacity-60"
                                : "",
                            ].join(" ")}
                          >
                            <div className="aspect-square bg-slate-100">
                              {urn.imageUrl ? (
                                <img
                                  src={urn.imageUrl}
                                  alt={urn.name}
                                  className="h-full w-full object-cover"
                                />
                              ) : (
                                <div className="flex h-full items-center justify-center px-4 text-center text-sm text-slate-400">
                                  Sin imagen
                                </div>
                              )}
                            </div>

                            <div className="p-4">
                              <div className="flex items-start justify-between gap-2">
                                <p className="font-semibold text-slate-900">
                                  {urn.name}
                                </p>

                                {isSelected && (
                                  <span className="rounded-full bg-slate-900 px-2.5 py-1 text-xs font-semibold text-white">
                                    Seleccionada
                                  </span>
                                )}
                              </div>

                              {(urn.material || urn.color) && (
                                <p className="mt-2 text-sm text-slate-500">
                                  {[urn.material, urn.color]
                                    .filter(Boolean)
                                    .join(" · ")}
                                </p>
                              )}

                              {urn.description && (
                                <p className="mt-2 line-clamp-2 text-sm text-slate-600">
                                  {urn.description}
                                </p>
                              )}
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  )}

                  {errors.urnId && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.urnId.message}
                    </p>
                  )}
                </div>
              )}

              {packageIncludesAccessory && (
                <div>
                  <label
                    htmlFor="cremation-accessory"
                    className="block text-sm font-medium text-slate-700"
                  >
                    Accesorio
                  </label>

                  <input
                    id="cremation-accessory"
                    type="text"
                    disabled={isSubmitting}
                    {...register("accessoryDescription")}
                    placeholder={
                      isOriginalPackage
                        ? (cremation?.accessoryDescription ??
                          "Descripción del accesorio")
                        : (selectedPackage?.accessoryDescription ??
                          "Descripción del accesorio")
                    }
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                  />

                  {errors.accessoryDescription && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.accessoryDescription.message}
                    </p>
                  )}
                </div>
              )}
            </div>
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Programación
            </h3>

            <div className="mt-4 grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="cremation-scheduled-at"
                  className="block text-sm font-medium text-slate-700"
                >
                  Fecha programada
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label
                      htmlFor="scheduledDate"
                      className="mb-1.5 block text-sm font-medium text-slate-700"
                    >
                      Fecha programada
                    </label>

                    <input
                      id="scheduledDate"
                      type="date"
                      {...register("scheduledDate")}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
                    />

                    {errors.scheduledDate && (
                      <p className="mt-1 text-sm text-red-600">
                        {errors.scheduledDate.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <label
                      htmlFor="scheduledTime"
                      className="mb-1.5 block text-sm font-medium text-slate-700"
                    >
                      Hora
                    </label>

                    <select
                      id="scheduledTime"
                      {...register("scheduledTime")}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
                    >
                      <option value="">Selecciona una hora</option>

                      {CREMATION_TIME_OPTIONS.map((time) => (
                        <option key={time} value={time}>
                          {time}
                        </option>
                      ))}
                    </select>

                    {errors.scheduledTime && (
                      <p className="mt-1 text-sm text-red-600">
                        {errors.scheduledTime.message}
                      </p>
                    )}
                  </div>
                </div>
              </div>

              <div>
                <label
                  htmlFor="cremation-user"
                  className="block text-sm font-medium text-slate-700"
                >
                  Asignado a
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <select
                  id="cremation-user"
                  disabled={isSubmitting || isLoadingUsers}
                  {...register("assignedToUserId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {isLoadingUsers ? "Cargando usuarios..." : "Sin asignar"}
                  </option>

                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.name}
                    </option>
                  ))}
                </select>

                {errors.assignedToUserId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.assignedToUserId.message}
                  </p>
                )}

                <p className="mt-2 text-xs text-slate-500">
                  Se requiere un usuario asignado antes de iniciar la cremación.
                </p>
              </div>
            </div>
          </section>

          <section className="border-t border-slate-200 pt-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Información adicional
            </h3>

            <div className="mt-4 space-y-5">
              <div>
                <label
                  htmlFor="cremation-instructions"
                  className="block text-sm font-medium text-slate-700"
                >
                  Instrucciones especiales
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <textarea
                  id="cremation-instructions"
                  rows={3}
                  disabled={isSubmitting}
                  {...register("specialInstructions")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.specialInstructions && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.specialInstructions.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="cremation-notes"
                  className="block text-sm font-medium text-slate-700"
                >
                  Notas
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <textarea
                  id="cremation-notes"
                  rows={3}
                  disabled={isSubmitting}
                  {...register("notes")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.notes && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.notes.message}
                  </p>
                )}
              </div>
            </div>
          </section>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-6 sm:flex-row sm:justify-end">
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
              disabled={
                isSubmitting || isLoadingReceptions || noAvailableReceptions
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar cremación"
                  : "Guardar cambios"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
