import { useEffect, useState } from "react";

import type {
  Collection,
  CollectionDriverOption,
} from "../types/collection.types";

interface CollectionAssignmentModalProps {
  collection: Collection | null;
  drivers: CollectionDriverOption[];
  isLoadingDrivers: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (driverUserId: string) => Promise<void>;
}

export function CollectionAssignmentModal({
  collection,
  drivers,
  isLoadingDrivers,
  isSubmitting,
  onClose,
  onSubmit,
}: CollectionAssignmentModalProps) {
  const [driverUserId, setDriverUserId] = useState("");

  useEffect(() => {
    setDriverUserId(collection?.assignedDriverId ?? "");
  }, [collection]);

  if (!collection) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/60 px-4"
      role="presentation"
      onMouseDown={() => !isSubmitting && onClose()}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="collection-assignment-title"
        className="w-full max-w-lg rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="border-b border-slate-200 px-6 py-5">
          <h2
            id="collection-assignment-title"
            className="text-xl font-semibold text-slate-900"
          >
            Asignar conductor
          </h2>
          <p className="mt-2 text-sm text-slate-500">
            Recolección de {collection.petName}. La asignación no confirma la
            custodia física.
          </p>
        </header>

        <form
          className="space-y-5 px-6 py-6"
          onSubmit={(event) => {
            event.preventDefault();
            if (driverUserId) void onSubmit(driverUserId);
          }}
        >
          <div>
            <label
              htmlFor="collection-driver"
              className="block text-sm font-medium text-slate-700"
            >
              Conductor activo
            </label>
            <select
              id="collection-driver"
              value={driverUserId}
              onChange={(event) => setDriverUserId(event.target.value)}
              disabled={isLoadingDrivers || isSubmitting}
              required
              className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
            >
              <option value="">Selecciona un conductor</option>
              {drivers.map((driver) => (
                <option key={driver.id} value={driver.id}>
                  {driver.name}
                </option>
              ))}
            </select>
          </div>

          <footer className="flex justify-end gap-3">
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
              disabled={!driverUserId || isLoadingDrivers || isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
            >
              {isSubmitting ? "Guardando..." : "Guardar asignación"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
