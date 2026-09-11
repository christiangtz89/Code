import axios from "axios";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";

import { CremationPriceFormModal } from "../components/CremationPriceFormModal";

import {
  cremationPriceFormToCreatePayload,
  cremationPriceFormToUpdatePayload,
} from "../helpers";

import {
  useCreateCremationPrice,
  useCremationPrices,
  useCremationPricingConfiguration,
  useUpdateCremationPrice,
  useUpdateCremationPricingConfiguration,
} from "../hooks";

import type { CremationPriceFormValues } from "../schemas";

import { WEIGHT_PRICING_INTERVAL, type CremationPrice } from "../types";

import {
  formatCurrency,
  formatWeightRange,
  getCremationTypeLabel,
  getWeightPricingIntervalLabel,
  isWeightRangeForInterval,
} from "../utils";

import { useCremationPackages } from "../../cremation-packages/hooks";

type ModalMode = "create" | "edit";

function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  if (!error.response) {
    return "No fue posible conectarse con el servidor.";
  }

  const data = error.response.data;

  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object") {
    const apiData = data as {
      detail?: string;
      message?: string;
      title?: string;
    };

    return apiData.detail ?? apiData.message ?? apiData.title ?? fallback;
  }

  return fallback;
}

export function CremationPricingPage() {
  const [showInactive, setShowInactive] = useState(false);

  const [isModalOpen, setIsModalOpen] = useState(false);

  const [modalMode, setModalMode] = useState<ModalMode>("create");

  const [selectedPrice, setSelectedPrice] = useState<CremationPrice | null>(
    null,
  );

  const configurationQuery = useCremationPricingConfiguration();

  const packagesQuery = useCremationPackages(true);

  const pricesQuery = useCremationPrices({
    includeInactive: true,
  });

  const updateConfigurationMutation = useUpdateCremationPricingConfiguration();

  const createPriceMutation = useCreateCremationPrice();

  const updatePriceMutation = useUpdateCremationPrice();

  const configuration = configurationQuery.data;

  const packages = packagesQuery.data ?? [];

  const allPrices = useMemo(() => pricesQuery.data ?? [], [pricesQuery.data]);

  const visiblePrices = useMemo(() => {
    if (!configuration) {
      return [];
    }

    return allPrices
      .filter((price) =>
        isWeightRangeForInterval(
          price.minimumWeightKg,
          price.maximumWeightKg,
          configuration.weightInterval,
        ),
      )
      .filter((price) => showInactive || price.isActive);
  }, [allPrices, configuration, showInactive]);

  async function handleIntervalChange(value: number) {
    if (!configuration) {
      return;
    }

    const weightInterval =
      value === WEIGHT_PRICING_INTERVAL.TEN_KG
        ? WEIGHT_PRICING_INTERVAL.TEN_KG
        : WEIGHT_PRICING_INTERVAL.FIVE_KG;

    if (weightInterval === configuration.weightInterval) {
      return;
    }

    const confirmed = window.confirm(
      `¿Cambiar la escala de precios de ${configuration.weightInterval} kg a ${weightInterval} kg?\n\n` +
        "Las cotizaciones comenzarán a utilizar inmediatamente la nueva escala.",
    );

    if (!confirmed) {
      return;
    }

    try {
      await updateConfigurationMutation.mutateAsync({
        weightInterval,
        allowIndividualNoAshes: configuration.allowIndividualNoAshes,
      });

      toast.success(
        `Escala actualizada a ${getWeightPricingIntervalLabel(
          weightInterval,
        )}.`,
      );
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible actualizar la escala."),
      );
    }
  }

  function openCreateModal() {
    setSelectedPrice(null);
    setModalMode("create");
    setIsModalOpen(true);
  }

  function openEditModal(price: CremationPrice) {
    setSelectedPrice(price);
    setModalMode("edit");
    setIsModalOpen(true);
  }

  function closeModal() {
    setSelectedPrice(null);
    setIsModalOpen(false);
  }

  async function handlePriceSubmit(values: CremationPriceFormValues) {
    try {
      if (modalMode === "create") {
        await createPriceMutation.mutateAsync(
          cremationPriceFormToCreatePayload(values),
        );

        toast.success("Precio registrado correctamente.");
      } else {
        if (!selectedPrice) {
          return;
        }

        await updatePriceMutation.mutateAsync({
          id: selectedPrice.id,
          payload: cremationPriceFormToUpdatePayload(values),
        });

        toast.success("Precio actualizado correctamente.");
      }

      closeModal();
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalMode === "create"
            ? "No fue posible registrar el precio."
            : "No fue posible actualizar el precio.",
        ),
      );
    }
  }

  if (
    configurationQuery.isPending ||
    packagesQuery.isPending ||
    pricesQuery.isPending
  ) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
        Cargando precios...
      </div>
    );
  }

  if (
    configurationQuery.isError ||
    packagesQuery.isError ||
    pricesQuery.isError ||
    !configuration
  ) {
    return (
      <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
        No fue posible cargar la configuración de precios.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            Precios de cremación
          </h1>

          <p className="mt-1 text-sm text-slate-500">
            Configura la escala por peso y los precios de cada paquete.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreateModal}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
        >
          + Registrar precio
        </button>
      </header>

      <section className="rounded-2xl border border-slate-200 bg-white p-5">
        <h2 className="text-lg font-semibold text-slate-900">
          Configuración de precios
        </h2>

        <div className="mt-5 grid gap-5 lg:grid-cols-2">
          <label className="space-y-2">
            <span className="text-sm font-medium text-slate-700">
              Escala activa
            </span>

            <select
              value={configuration.weightInterval}
              onChange={(event) =>
                void handleIntervalChange(Number(event.target.value))
              }
              className="w-full rounded-lg border border-slate-300 px-3 py-2"
            >
              <option value={5}>Cada 5 kg</option>

              <option value={10}>Cada 10 kg</option>
            </select>

            <p className="text-xs text-slate-500">
              Solo esta escala se utilizará para cotizaciones y el catálogo
              público.
            </p>
          </label>
          <div className="mt-4 rounded-xl bg-amber-50 p-4 text-sm text-amber-800">
            <strong>Importante:</strong> cambiar esta escala modifica
            inmediatamente qué tabla de precios utiliza PCMS para generar
            cotizaciones. Los precios de la otra escala permanecen guardados,
            pero no se utilizan mientras esté inactiva.
          </div>
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white">
        <div className="flex flex-col gap-3 border-b border-slate-200 p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold text-slate-900">
              Precios —{" "}
              {getWeightPricingIntervalLabel(configuration.weightInterval)}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Se muestran solamente los rangos correspondientes a la escala
              activa.
            </p>
          </div>

          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input
              type="checkbox"
              checked={showInactive}
              onChange={(event) => setShowInactive(event.target.checked)}
            />
            Mostrar inactivos
          </label>
        </div>

        {visiblePrices.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            No hay precios configurados para esta escala.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Paquete
                  </th>

                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Tipo
                  </th>

                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Peso
                  </th>

                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Precio
                  </th>

                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Pago para recepción
                  </th>

                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Visibilidad
                  </th>

                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Estado
                  </th>

                  <th className="px-4 py-3" />
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100">
                {visiblePrices.map((price) => (
                  <tr key={price.id}>
                    <td className="px-4 py-3 text-sm font-medium text-slate-900">
                      {price.cremationPackageName}
                    </td>

                    <td className="px-4 py-3 text-sm text-slate-600">
                      {getCremationTypeLabel(price.cremationType)}
                    </td>

                    <td className="px-4 py-3 text-sm text-slate-600">
                      {formatWeightRange(
                        price.minimumWeightKg,
                        price.maximumWeightKg,
                      )}
                    </td>

                    <td className="px-4 py-3 text-right text-sm font-semibold text-slate-900">
                      {formatCurrency(price.price)}
                    </td>

                    <td className="px-4 py-3 text-right text-sm font-medium text-slate-700">
                      {price.requiredCollectionPaymentAmount === null
                        ? "Sin configurar"
                        : formatCurrency(price.requiredCollectionPaymentAmount)}
                    </td>

                    <td className="px-4 py-3 text-center">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                          price.isPublic
                            ? "bg-blue-100 text-blue-700"
                            : "bg-slate-100 text-slate-600"
                        }`}
                      >
                        {price.isPublic ? "Público" : "Oculto"}
                      </span>
                    </td>

                    <td className="px-4 py-3 text-center">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                          price.isActive
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-slate-100 text-slate-600"
                        }`}
                      >
                        {price.isActive ? "Activo" : "Inactivo"}
                      </span>
                    </td>

                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => openEditModal(price)}
                        className="text-sm font-medium text-slate-700 hover:text-slate-950"
                      >
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <CremationPriceFormModal
        isOpen={isModalOpen}
        mode={modalMode}
        price={selectedPrice}
        packages={packages}
        configuration={configuration}
        onClose={closeModal}
        onSubmit={handlePriceSubmit}
      />
    </div>
  );
}
