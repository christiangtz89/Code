import type { CremationPackage } from "../types";
import {
  getCremationPackageTierLabel,
  getCremationPackageTypeLabel,
} from "../utils";

interface CremationPackageCardProps {
  cremationPackage: CremationPackage;
  onEdit: (cremationPackage: CremationPackage) => void;
}

export function CremationPackageCard({
  cremationPackage,
  onEdit,
}: CremationPackageCardProps) {
  return (
    <article className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="aspect-[16/9] bg-slate-100">
        {cremationPackage.imageUrl ? (
          <img
            src={cremationPackage.imageUrl}
            alt={cremationPackage.name}
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
            <h2 className="text-lg font-semibold text-slate-900">
              {cremationPackage.name}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {getCremationPackageTypeLabel(cremationPackage.packageType)}
            </p>
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            <span
              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                cremationPackage.isPublic
                  ? "bg-emerald-100 text-emerald-700"
                  : "bg-slate-100 text-slate-600"
              }`}
            >
              {cremationPackage.isPublic ? "Público" : "Oculto"}
            </span>

            <span
              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                cremationPackage.isActive
                  ? "bg-blue-100 text-blue-700"
                  : "bg-rose-100 text-rose-700"
              }`}
            >
              {cremationPackage.isActive ? "Activo" : "Inactivo"}
            </span>
          </div>
        </div>

        <p className="min-h-10 text-sm text-slate-600">
          {cremationPackage.shortDescription ??
            cremationPackage.description ??
            "Sin descripción."}
        </p>

        <div className="rounded-xl bg-slate-50 p-3 text-sm">
          <div className="flex justify-between gap-4">
            <span className="text-slate-500">Nivel</span>
            <span className="font-medium text-slate-800">
              {getCremationPackageTierLabel(cremationPackage.tier)}
            </span>
          </div>

          <div className="mt-2 flex justify-between gap-4">
            <span className="text-slate-500">Orden</span>
            <span className="font-medium text-slate-800">
              {cremationPackage.displayOrder}
            </span>
          </div>
        </div>

        <div className="flex flex-wrap gap-2 text-xs">
          {cremationPackage.includesUrn && (
            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-700">
              Urna
            </span>
          )}

          {cremationPackage.includesPawPrint && (
            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-700">
              Accesorio
            </span>
          )}

          {cremationPackage.includesCertificate && (
            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-700">
              Certificado
            </span>
          )}

          {!cremationPackage.includesUrn &&
            !cremationPackage.includesPawPrint &&
            !cremationPackage.includesCertificate && (
              <span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-700">
                Sin devolución de cenizas
              </span>
            )}
        </div>

        <button
          type="button"
          onClick={() => onEdit(cremationPackage)}
          className="w-full rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
        >
          Editar paquete
        </button>
      </div>
    </article>
  );
}
