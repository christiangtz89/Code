import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Link } from "react-router-dom";
import { hasPermission } from "../../auth/utils/permissions";
import {
  cancelCremationUrn,
  getCremationInventory,
  reserveCremationUrn,
} from "../api/cremationsApi";

export function CremationInventoryPanel({ id }: { id: string }) {
  const canManageInventory = hasPermission("Inventory.Manage");
  const canScanDelivery =
    hasPermission("Inventory.ScanOutgoing") &&
    hasPermission("Cremations.View");
  const queryClient = useQueryClient();
  const inventoryQuery = useQuery({
    queryKey: ["cremation-inventory", id],
    queryFn: () => getCremationInventory(id),
  });
  const mutation = useMutation({
    mutationFn: (action: "reserve" | "cancel") =>
      action === "reserve"
        ? reserveCremationUrn(id)
        : cancelCremationUrn(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["cremation-inventory", id],
      });
      toast.success("Operación completada.");
    },
    onError: (error: { response?: { data?: { message?: string } } }) =>
      toast.error(error.response?.data?.message ?? "Operación rechazada."),
  });

  if (inventoryQuery.isLoading) return <p>Cargando inventario…</p>;
  if (inventoryQuery.isError || !inventoryQuery.data) {
    return <p className="text-red-700">No fue posible cargar el inventario.</p>;
  }

  const inventory = inventoryQuery.data;
  return (
    <section className="space-y-3 rounded-xl border bg-white p-4">
      <h2 className="font-semibold">Inventario de entrega</h2>
      {inventory.urnId ? (
        <>
          <p className="text-sm">
            Urna: {inventory.urnName ?? "Sin urna"} · Existencia:{" "}
            {inventory.physicalStock} · Disponible: {inventory.availableQuantity}
          </p>

          {inventory.fulfillmentId ? (
            <p className="rounded bg-emerald-50 p-3 text-sm text-emerald-800">
              Entrega ya registrada.
            </p>
          ) : !inventory.reservation ? (
            canManageInventory && (
              <button
                type="button"
                className="rounded bg-slate-900 px-3 py-2 text-sm text-white disabled:cursor-not-allowed disabled:opacity-50"
                disabled={inventory.availableQuantity < 1 || mutation.isPending}
                onClick={() => mutation.mutate("reserve")}
              >
                Reservar urna
              </button>
            )
          ) : inventory.reservation.status === 1 ? (
            <div className="flex flex-wrap gap-2">
              {canManageInventory && (
                <button
                  type="button"
                  className="rounded border px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
                  disabled={mutation.isPending}
                  onClick={() => mutation.mutate("cancel")}
                >
                  Liberar reserva
                </button>
              )}
              {canScanDelivery && (
                <Link
                  to={`/cremations/${id}/scan-delivery`}
                  className="rounded bg-emerald-700 px-3 py-2 text-sm font-medium text-white"
                >
                  Escanear entrega
                </Link>
              )}
            </div>
          ) : (
            <p className="text-sm text-slate-600">La reserva no está activa.</p>
          )}
        </>
      ) : (
        <p className="text-sm">Esta cremación no requiere una urna de inventario.</p>
      )}
    </section>
  );
}
