import axios from "axios";
import { useState } from "react";
import toast from "react-hot-toast";

import { CremationPackageCard } from "../components/CremationPackageCard";
import { CremationPackageFormModal } from "../components/CremationPackageFormModal";
import {
  cremationPackageFormToCreatePayload,
  cremationPackageFormToUpdatePayload,
} from "../helpers";
import {
  useCreateCremationPackage,
  useCremationPackages,
  useUpdateCremationPackage,
} from "../hooks";
import type { CremationPackageFormValues } from "../schemas";
import type { CremationPackage } from "../types";

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

export function CremationPackagesPage() {
  const [includeInactive, setIncludeInactive] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<ModalMode>("create");
  const [selectedPackage, setSelectedPackage] =
    useState<CremationPackage | null>(null);

  const packagesQuery = useCremationPackages(includeInactive);

  const createMutation = useCreateCremationPackage();
  const updateMutation = useUpdateCremationPackage();

  function openCreateModal() {
    setSelectedPackage(null);
    setModalMode("create");
    setIsModalOpen(true);
  }

  function openEditModal(cremationPackage: CremationPackage) {
    setSelectedPackage(cremationPackage);
    setModalMode("edit");
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    setSelectedPackage(null);
  }

  async function handleSubmit(values: CremationPackageFormValues) {
    try {
      if (modalMode === "create") {
        await createMutation.mutateAsync(
          cremationPackageFormToCreatePayload(values),
        );

        toast.success("Paquete registrado correctamente.");
      } else {
        if (!selectedPackage) {
          return;
        }

        await updateMutation.mutateAsync({
          id: selectedPackage.id,
          payload: cremationPackageFormToUpdatePayload(values),
        });

        toast.success("Paquete actualizado correctamente.");
      }

      closeModal();
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalMode === "create"
            ? "No fue posible registrar el paquete."
            : "No fue posible actualizar el paquete.",
        ),
      );
    }
  }

  const packages = packagesQuery.data ?? [];

  const publicCount = packages.filter(
    (item) => item.isPublic && item.isActive,
  ).length;

  const hiddenCount = packages.filter(
    (item) => !item.isPublic && item.isActive,
  ).length;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            Paquetes de cremación
          </h1>

          <p className="mt-1 text-sm text-slate-500">
            Administra los servicios, su visibilidad pública y el contenido
            incluido.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreateModal}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
        >
          + Registrar paquete
        </button>
      </header>

      <section className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Paquetes cargados</div>
          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {packages.length}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Públicos activos</div>
          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {publicCount}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Ocultos activos</div>
          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {hiddenCount}
          </div>
        </div>
      </section>

      <div className="flex items-center justify-between gap-4">
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={includeInactive}
            onChange={(event) => setIncludeInactive(event.target.checked)}
          />
          Mostrar paquetes inactivos
        </label>
      </div>

      {packagesQuery.isPending && (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
          Cargando paquetes...
        </div>
      )}

      {packagesQuery.isError && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          No fue posible cargar los paquetes de cremación.
        </div>
      )}

      {!packagesQuery.isPending &&
        !packagesQuery.isError &&
        packages.length === 0 && (
          <div className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center">
            <p className="text-slate-600">No hay paquetes registrados.</p>
          </div>
        )}

      {packages.length > 0 && (
        <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {packages.map((cremationPackage) => (
            <CremationPackageCard
              key={cremationPackage.id}
              cremationPackage={cremationPackage}
              onEdit={openEditModal}
            />
          ))}
        </section>
      )}

      <CremationPackageFormModal
        isOpen={isModalOpen}
        mode={modalMode}
        cremationPackage={selectedPackage}
        onClose={closeModal}
        onSubmit={handleSubmit}
      />
    </div>
  );
}
