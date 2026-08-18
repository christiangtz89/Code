import axios from "axios";
import { useState } from "react";
import toast from "react-hot-toast";

import { UrnCard } from "../components/UrnCard";
import { UrnFormModal } from "../components/UrnFormModal";
import { urnFormToCreatePayload, urnFormToUpdatePayload } from "../helpers";
import { useCreateUrn, useUpdateUrn, useUrns } from "../hooks";
import type { UrnFormValues } from "../schemas";
import type { Urn } from "../types";

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

export function UrnsPage() {
  const [includeInactive, setIncludeInactive] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<ModalMode>("create");
  const [selectedUrn, setSelectedUrn] = useState<Urn | null>(null);

  const urnsQuery = useUrns(includeInactive);

  const createMutation = useCreateUrn();
  const updateMutation = useUpdateUrn();

  function openCreateModal() {
    setSelectedUrn(null);
    setModalMode("create");
    setIsModalOpen(true);
  }

  function openEditModal(urn: Urn) {
    setSelectedUrn(urn);
    setModalMode("edit");
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    setSelectedUrn(null);
  }

  async function handleSubmit(values: UrnFormValues) {
    try {
      if (modalMode === "create") {
        await createMutation.mutateAsync(urnFormToCreatePayload(values));

        toast.success("Urna registrada correctamente.");
      } else {
        if (!selectedUrn) {
          return;
        }

        await updateMutation.mutateAsync({
          id: selectedUrn.id,
          payload: urnFormToUpdatePayload(values),
        });

        toast.success("Urna actualizada correctamente.");
      }

      closeModal();
    } catch (error) {
      toast.error(
        getApiErrorMessage(
          error,
          modalMode === "create"
            ? "No fue posible registrar la urna."
            : "No fue posible actualizar la urna.",
        ),
      );
    }
  }

  const urns = urnsQuery.data ?? [];

  const publicCount = urns.filter((urn) => urn.isPublic && urn.isActive).length;

  const hiddenCount = urns.filter(
    (urn) => !urn.isPublic && urn.isActive,
  ).length;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Urnas</h1>

          <p className="mt-1 text-sm text-slate-500">
            Administra las urnas disponibles, sus imágenes, precios y
            visibilidad.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreateModal}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
        >
          + Registrar urna
        </button>
      </header>

      <section className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Urnas cargadas</div>

          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {urns.length}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Públicas activas</div>

          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {publicCount}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="text-sm text-slate-500">Ocultas activas</div>

          <div className="mt-1 text-2xl font-semibold text-slate-900">
            {hiddenCount}
          </div>
        </div>
      </section>

      <label className="flex items-center gap-2 text-sm text-slate-600">
        <input
          type="checkbox"
          checked={includeInactive}
          onChange={(event) => setIncludeInactive(event.target.checked)}
        />
        Mostrar urnas inactivas
      </label>

      {urnsQuery.isPending && (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-500">
          Cargando urnas...
        </div>
      )}

      {urnsQuery.isError && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          No fue posible cargar las urnas.
        </div>
      )}

      {!urnsQuery.isPending && !urnsQuery.isError && urns.length === 0 && (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <p className="text-slate-600">No hay urnas registradas.</p>
        </div>
      )}

      {urns.length > 0 && (
        <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {urns.map((urn) => (
            <UrnCard key={urn.id} urn={urn} onEdit={openEditModal} />
          ))}
        </section>
      )}

      <UrnFormModal
        isOpen={isModalOpen}
        mode={modalMode}
        urn={selectedUrn}
        onClose={closeModal}
        onSubmit={handleSubmit}
      />
    </div>
  );
}
