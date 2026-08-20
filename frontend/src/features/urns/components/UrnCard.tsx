import type { Urn } from "../types";

interface UrnCardProps {
  urn: Urn;
  onEdit: (urn: Urn) => void;
}

export function UrnCard({ urn, onEdit }: UrnCardProps) {
  return (
    <article className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="aspect-[4/3] bg-slate-100">
        {urn.imageUrl ? (
          <img
            src={urn.imageUrl}
            alt={urn.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-slate-500">
            Sin imagen
          </div>
        )}
      </div>

      <div className="space-y-4 p-5">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">{urn.name}</h2>
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            <span
              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                urn.isPublic
                  ? "bg-emerald-100 text-emerald-700"
                  : "bg-slate-100 text-slate-600"
              }`}
            >
              {urn.isPublic ? "Pública" : "Oculta"}
            </span>

            <span
              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                urn.isActive
                  ? "bg-blue-100 text-blue-700"
                  : "bg-rose-100 text-rose-700"
              }`}
            >
              {urn.isActive ? "Activa" : "Inactiva"}
            </span>
          </div>
        </div>

        <p className="min-h-10 text-sm text-slate-600">
          {urn.description ?? "Sin descripción."}
        </p>

        <div className="space-y-2 rounded-xl bg-slate-50 p-3 text-sm">
          <div className="flex justify-between gap-4">
            <span className="text-slate-500">Material</span>

            <span className="font-medium text-slate-800">
              {urn.material ?? "No especificado"}
            </span>
          </div>

          <div className="flex justify-between gap-4">
            <span className="text-slate-500">Color</span>

            <span className="font-medium text-slate-800">
              {urn.color ?? "No especificado"}
            </span>
          </div>

          <div className="flex justify-between gap-4">
            <span className="text-slate-500">Orden</span>

            <span className="font-medium text-slate-800">
              {urn.displayOrder}
            </span>
          </div>
        </div>

        <button
          type="button"
          onClick={() => onEdit(urn)}
          className="w-full rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
        >
          Editar urna
        </button>
      </div>
    </article>
  );
}
