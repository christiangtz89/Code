import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import {
  createSupplier,
  createSupplyItem,
  getSuppliers,
  getSupplyItemByScan,
  getSupplyItems,
  getCurrentInventory,
  getInventoryMovements,
  recordInventoryMovement,
  getFilament,
  getFilamentCost,
  saveFilament,
} from "../api/inventoryApi";
import { DecimalInput } from "../../../components/ui/DecimalInput";

const input = "w-full rounded-lg border border-slate-300 px-3 py-2 text-sm";
export function InventoryCatalogPage() {
  const canManage = hasPermission("Inventory.Manage");
  const canSeeCosts = hasPermission("Purchasing.View");
  const [tab, setTab] = useState<"suppliers" | "items">("suppliers");
  const [search, setSearch] = useState("");
  const [scan, setScan] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [movementType, setMovementType] = useState("ManualAdjustmentIncrease");
  const [movementQuantity, setMovementQuantity] = useState("");
  const [movementNotes, setMovementNotes] = useState("");
  const [filamentForm, setFilamentForm] = useState({ materialType: "PLA", brand: "", color: "", netUsableWeightGrams: "1000", manufacturerProductCode: "", productData: "" });
  const [supplierName, setSupplierName] = useState("");
  const [item, setItem] = useState({
    name: "",
    category: "",
    unitOfMeasure: "pieza",
    scanCode: "",
    minimumQuantity: "0",
  });
  const qc = useQueryClient();
  const suppliers = useQuery({
    queryKey: ["suppliers", search],
    queryFn: () => getSuppliers(search),
    enabled: tab === "suppliers",
  });
  const items = useQuery({
    queryKey: ["supply-items", search],
    queryFn: () => getSupplyItems(search),
    enabled: tab === "items",
  });
  const stock = useQuery({ queryKey: ["inventory-current", search], queryFn: () => getCurrentInventory(search), enabled: tab === "items" });
  const movements = useQuery({ queryKey: ["inventory-movements", selectedId], queryFn: () => getInventoryMovements(selectedId!), enabled: tab === "items" && !!selectedId });
  const filament = useQuery({ queryKey: ["filament", selectedId], queryFn: () => getFilament(selectedId!), enabled: tab === "items" && !!selectedId });
  const filamentCost = useQuery({ queryKey: ["filament-cost", selectedId], queryFn: () => getFilamentCost(selectedId!), enabled: tab === "items" && !!selectedId && canSeeCosts });
  const filamentMutation = useMutation({ mutationFn: () => saveFilament({ supplyItemId: selectedId, ...filamentForm, netUsableWeightGrams: filamentForm.netUsableWeightGrams }), onSuccess: () => { qc.invalidateQueries({ queryKey: ["filament", selectedId] }); toast.success("Especificación de filamento guardada."); }, onError: () => toast.error("No fue posible guardar el filamento.") });
  const movementMutation = useMutation({ mutationFn: () => recordInventoryMovement({ supplyItemId: selectedId, movementType, quantity: movementQuantity, notes: movementNotes }), onSuccess: () => { setMovementQuantity(""); setMovementNotes(""); qc.invalidateQueries({ queryKey: ["inventory-current"] }); qc.invalidateQueries({ queryKey: ["inventory-movements", selectedId] }); toast.success("Movimiento registrado."); }, onError: (e: any) => toast.error(e?.response?.data?.message ?? "No fue posible registrar el movimiento.") });
  const supplierMutation = useMutation({
    mutationFn: () => createSupplier({ name: supplierName }),
    onSuccess: () => {
      setSupplierName("");
      qc.invalidateQueries({ queryKey: ["suppliers"] });
      toast.success("Proveedor registrado.");
    },
    onError: () => toast.error("No fue posible registrar el proveedor."),
  });
  const itemMutation = useMutation({
    mutationFn: () =>
      createSupplyItem({
        ...item,
        minimumQuantity: item.minimumQuantity === "" ? 0 : item.minimumQuantity,
      }),
    onSuccess: () => {
      setItem({
        name: "",
        category: "",
        unitOfMeasure: "pieza",
        scanCode: "",
        minimumQuantity: "0",
      });
      qc.invalidateQueries({ queryKey: ["supply-items"] });
      toast.success("Insumo registrado.");
    },
    onError: () => toast.error("No fue posible registrar el insumo."),
  });
  async function scanItem(e: React.FormEvent) {
    e.preventDefault();
    try {
      const x = await getSupplyItemByScan(scan);
      setSelectedId(x.id);
      toast.success(`Insumo: ${x.name}`);
    } catch {
      toast.error("Código no encontrado.");
    }
  }
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-slate-900">
          Catálogo operativo
        </h1>
        <p className="mt-1 text-sm text-slate-500">
          Proveedores e insumos para compras e inventario.
        </p>
      </header>
      <div className="flex gap-2">
        <button
          className={`rounded-lg px-4 py-2 text-sm ${tab === "suppliers" ? "bg-slate-900 text-white" : "bg-white border"}`}
          onClick={() => setTab("suppliers")}
        >
          Proveedores
        </button>
        <button
          className={`rounded-lg px-4 py-2 text-sm ${tab === "items" ? "bg-slate-900 text-white" : "bg-white border"}`}
          onClick={() => setTab("items")}
        >
          Insumos
        </button>
      </div>
      <input
        className={input}
        placeholder="Buscar..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      {tab === "suppliers" ? (
        <section className="space-y-4">
          {canManage && (
            <form
              className="flex gap-2"
              onSubmit={(e) => {
                e.preventDefault();
                supplierMutation.mutate();
              }}
            >
              <input
                className={input}
                placeholder="Nombre del proveedor"
                value={supplierName}
                onChange={(e) => setSupplierName(e.target.value)}
                required
              />
              <button className="rounded-lg bg-slate-900 px-4 text-sm text-white">
                Registrar
              </button>
            </form>
          )}
          <div className="rounded-xl border bg-white divide-y">
            {(suppliers.data?.items ?? []).map((x) => (
              <div className="p-4" key={x.id}>
                <div className="font-medium">{x.name}</div>
                <div className="text-sm text-slate-500">
                  {x.taxId ?? "Sin RFC"}
                </div>
              </div>
            ))}
          </div>
        </section>
      ) : (
        <section className="space-y-4">
          {canManage && (
            <form
              className="grid gap-2 rounded-xl border bg-white p-4 md:grid-cols-5"
              onSubmit={(e) => {
                e.preventDefault();
                itemMutation.mutate();
              }}
            >
              <input
                className={input}
                placeholder="Nombre"
                value={item.name}
                onChange={(e) => setItem({ ...item, name: e.target.value })}
                required
              />
              <input
                className={input}
                placeholder="Categoría"
                value={item.category}
                onChange={(e) => setItem({ ...item, category: e.target.value })}
                required
              />
              <input
                className={input}
                placeholder="Unidad"
                value={item.unitOfMeasure}
                onChange={(e) =>
                  setItem({ ...item, unitOfMeasure: e.target.value })
                }
                required
              />
              <input
                className={input}
                inputMode="decimal"
                placeholder="Mínimo"
                value={item.minimumQuantity}
                onKeyDown={(e) => {
                  if (e.key === "ArrowUp" || e.key === "ArrowDown")
                    e.preventDefault();
                }}
                onChange={(e) =>
                  setItem({ ...item, minimumQuantity: e.target.value })
                }
              />
              <input
                className={input}
                placeholder="Código QR/barra"
                value={item.scanCode}
                onChange={(e) => setItem({ ...item, scanCode: e.target.value })}
                required
              />
              <button className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white md:col-span-5">
                Registrar insumo
              </button>
            </form>
          )}
          <form className="flex gap-2" onSubmit={scanItem}>
            <input
              className={input}
              autoFocus
              placeholder="Escanear código y presionar Enter"
              value={scan}
              onChange={(e) => setScan(e.target.value)}
            />
            <button className="rounded-lg border px-4">Resolver</button>
          </form>
          <div className="rounded-xl border bg-white p-4 space-y-3">
            <h2 className="font-semibold">Existencias</h2>
            <div className="divide-y">{(stock.data ?? []).map((x) => <button type="button" className={`flex w-full justify-between p-3 text-left ${selectedId === x.supplyItemId ? "bg-slate-100" : ""}`} key={x.supplyItemId} onClick={() => setSelectedId(x.supplyItemId)}><span>{x.name} <span className="text-sm text-slate-500">({x.unitOfMeasure})</span></span><span className={x.isLowStock ? "font-semibold text-red-600" : ""}>{x.currentQuantity} {x.isLowStock ? "· Bajo mínimo" : ""}</span></button>)}</div>
          </div>
          {selectedId && <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-xl border bg-white p-4"><h2 className="font-semibold">Filamento</h2>{filament.data ? <div className="mt-2 space-y-1 text-sm"><div>{filament.data.materialType} · {filament.data.brand ?? "Marca no indicada"} · {filament.data.color ?? "Color no indicado"}</div><div>Peso neto: {filament.data.netUsableWeightGrams} g</div>{canSeeCosts && filamentCost.data && <div className="font-medium">Costo: {filamentCost.data.costPerGram ?? "No disponible"} {filamentCost.data.currency ?? ""}/g · {filamentCost.data.costPerKilogram ?? "No disponible"}/kg</div>}</div> : <p className="mt-2 text-sm text-slate-500">Sin especificación registrada.</p>}{canManage && <form className="mt-3 space-y-2" onSubmit={(e) => { e.preventDefault(); filamentMutation.mutate(); }}><input className={input} placeholder="Material (PLA, PETG...)" value={filamentForm.materialType} onChange={(e) => setFilamentForm({ ...filamentForm, materialType: e.target.value })} required /><input className={input} placeholder="Marca" value={filamentForm.brand} onChange={(e) => setFilamentForm({ ...filamentForm, brand: e.target.value })} /><input className={input} placeholder="Color" value={filamentForm.color} onChange={(e) => setFilamentForm({ ...filamentForm, color: e.target.value })} /><DecimalInput className={input} placeholder="Peso neto (g)" value={filamentForm.netUsableWeightGrams} onChange={(e) => setFilamentForm({ ...filamentForm, netUsableWeightGrams: e.target.value })} required /><button className="rounded-lg bg-slate-900 px-3 py-2 text-sm text-white">Guardar filament</button></form>}</div>
            {canManage && <form className="space-y-3 rounded-xl border bg-white p-4" onSubmit={(e) => { e.preventDefault(); movementMutation.mutate(); }}><h2 className="font-semibold">Registrar movimiento</h2><select className={input} value={movementType} onChange={(e) => setMovementType(e.target.value)}><option value="ManualAdjustmentIncrease">Ajuste positivo</option><option value="ManualAdjustmentDecrease">Ajuste negativo</option><option value="Consumption">Consumo</option><option value="Waste">Merma</option></select><DecimalInput className={input} value={movementQuantity} onChange={(e) => setMovementQuantity(e.target.value)} placeholder="Cantidad" required /><input className={input} value={movementNotes} onChange={(e) => setMovementNotes(e.target.value)} placeholder="Notas" /><button className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white">Guardar movimiento</button></form>}
            <div className="rounded-xl border bg-white p-4"><h2 className="font-semibold">Historial</h2><div className="mt-2 space-y-2 text-sm">{(movements.data ?? []).map((x) => <div className="border-b pb-2" key={x.id}><div>{x.movementType} · {x.quantity} {x.unitOfMeasure}</div><div className="text-slate-500">{new Date(x.occurredAt).toLocaleString("es-MX")} {x.notes ?? ""}</div></div>)}</div></div>
          </div>}
          <div className="rounded-xl border bg-white divide-y">
            {(items.data?.items ?? []).map((x) => (
              <div className="flex justify-between p-4" key={x.id}>
                <div>
                  <div className="font-medium">{x.name}</div>
                  <div className="text-sm text-slate-500">
                    {x.category} · {x.unitOfMeasure}
                  </div>
                </div>
                <code className="text-sm">{x.scanCode}</code>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
