import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { DecimalInput } from "../../../components/ui/DecimalInput";
import { ScanCodeScanner } from "../../../components/scanner/ScanCodeScanner";
import { hasPermission } from "../../auth/utils/permissions";
import {
  changePurchaseStatus,
  createPurchase,
  getPurchaseSources,
  getPurchases,
  getSuppliers,
  receivePurchase,
  getSupplyItemByScan,
  updatePurchase,
  type Purchase,
  type PurchaseSource,
} from "../api/inventoryApi";

const input = "w-full rounded-lg border border-slate-300 px-3 py-2 text-sm";
type Line = { sourceId: string; quantity: string; source: PurchaseSource | null };
type LotInput = { scanCode: string; manufacturerLotNumber: string; initialQuantity: string };
const statusLabel: Record<Purchase["status"], string> = { Draft: "Borrador", Ordered: "Ordenada", PartiallyReceived: "Parcialmente recibida", Received: "Recibida", Cancelled: "Cancelada" };

export function PurchasesPage() {
  const manage = hasPermission("Purchasing.Manage");
  const qc = useQueryClient();
  const suppliers = useQuery({ queryKey: ["suppliers-purchasing"], queryFn: () => getSuppliers("", true) });
  const purchases = useQuery({ queryKey: ["purchases"], queryFn: getPurchases });
  const [supplierId, setSupplierId] = useState("");
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [tax, setTax] = useState("");
  const [invoice, setInvoice] = useState("");
  const [lines, setLines] = useState<Line[]>([{ sourceId: "", quantity: "", source: null }]);
  const [sources, setSources] = useState<PurchaseSource[]>([]);
  const [editing, setEditing] = useState<Purchase | null>(null);
  const [editSupplierId, setEditSupplierId] = useState("");
  const [editDate, setEditDate] = useState("");
  const [editInvoice, setEditInvoice] = useState("");
  const [editTax, setEditTax] = useState("");
  const [editNotes, setEditNotes] = useState("");
  const [editLines, setEditLines] = useState<Line[]>([]);
  const [editSources, setEditSources] = useState<PurchaseSource[]>([]);
  const [receivingId, setReceivingId] = useState<string | null>(null);
  const [receiptValues, setReceiptValues] = useState<Record<string, string>>({});
  const [lotValues, setLotValues] = useState<Record<string, LotInput[]>>({});
  const [receivingScanMessage, setReceivingScanMessage] = useState("");
  async function resolveReceivingScan(purchase: Purchase, code: string) { try { const item = await getSupplyItemByScan(code); const line = purchase.items.find(x => x.supplyItemId === item.id); if (!line) { setReceivingScanMessage("Este insumo no pertenece a esta compra."); return; } setReceiptValues(values => ({ ...values, [line.id]: values[line.id] ?? "" })); setReceivingScanMessage(`Insumo seleccionado: ${line.descriptionSnapshot}. Captura la cantidad a recibir.`); } catch { setReceivingScanMessage("Código no encontrado o no corresponde a un insumo de esta compra."); } }

  const createMutation = useMutation({
    mutationFn: () => createPurchase({ supplierId, purchaseDate: date, invoiceReference: invoice, currency: "MXN", tax: tax === "" ? 0 : tax, items: lines.map((line) => ({ supplyItemId: line.source?.supplyItemId, supplierSupplyItemId: line.sourceId, quantity: line.quantity })) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["purchases"] }); setLines([{ sourceId: "", quantity: "", source: null }]); setTax(""); setInvoice(""); toast.success("Borrador creado."); },
    onError: () => toast.error("No fue posible registrar la compra."),
  });
  const updateMutation = useMutation({
    mutationFn: () => {
      if (!editing) throw new Error("No hay borrador seleccionado.");
      return updatePurchase(editing.id, { supplierId: editSupplierId, purchaseDate: editDate, invoiceReference: editInvoice, currency: editing.currency, tax: editTax === "" ? 0 : editTax, notes: editNotes, items: editLines.map((line) => ({ supplyItemId: line.source?.supplyItemId, supplierSupplyItemId: line.sourceId, quantity: line.quantity })) });
    },
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["purchases"] }); setEditing(null); toast.success("Borrador actualizado."); },
    onError: (error: any) => toast.error(error?.response?.data?.message ?? "No fue posible actualizar el borrador."),
  });
  const statusMutation = useMutation({ mutationFn: ({ id, status }: { id: string; status: number }) => changePurchaseStatus(id, status), onSuccess: () => qc.invalidateQueries({ queryKey: ["purchases"] }), onError: () => toast.error("No fue posible cambiar el estado.") });
  const receiptMutation = useMutation({
    mutationFn: (value: { id: string; items: unknown[] }) => receivePurchase(value.id, { receivedAt: new Date().toISOString(), items: value.items }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["purchases"] }); setReceivingId(null); setReceiptValues({}); setLotValues({}); toast.success("Recepción registrada."); },
    onError: (error: any) => toast.error(error?.response?.data?.message ?? "No fue posible recibir mercancía."),
  });

  async function chooseSupplier(id: string) { setSupplierId(id); setSources(id ? await getPurchaseSources(id) : []); setLines([{ sourceId: "", quantity: "", source: null }]); }
  function updateLine(index: number, sourceId: string) { const source = sources.find((entry) => entry.id === sourceId) ?? null; setLines(lines.map((line, i) => i === index ? { ...line, sourceId, source } : line)); }
  async function openDraftEditor(purchase: Purchase) {
    if (purchase.status !== "Draft") return;
    const availableSources = await getPurchaseSources(purchase.supplierId);
    setEditing(purchase); setEditSupplierId(purchase.supplierId); setEditDate(purchase.purchaseDate.slice(0, 10)); setEditInvoice(purchase.invoiceReference ?? ""); setEditTax(String(purchase.tax)); setEditNotes(purchase.notes ?? ""); setEditSources(availableSources);
    setEditLines(purchase.items.map((item) => { const source = availableSources.find((entry) => entry.supplyItemId === item.supplyItemId) ?? null; return { sourceId: source?.id ?? "", quantity: String(item.quantity), source }; }));
  }
  async function chooseEditSupplier(id: string) { setEditSupplierId(id); setEditSources(id ? await getPurchaseSources(id) : []); setEditLines([{ sourceId: "", quantity: "", source: null }]); }
  function updateEditLine(index: number, sourceId: string) { const source = editSources.find((entry) => entry.id === sourceId) ?? null; setEditLines(editLines.map((line, i) => i === index ? { ...line, sourceId, source } : line)); }
  function submitReceipt(purchase: Purchase) {
    const items = purchase.items.filter((item) => Number(receiptValues[item.id]) > 0).map((item) => ({ purchaseItemId: item.id, quantity: receiptValues[item.id], lots: (lotValues[item.id] ?? []).map((lot) => ({ scanCode: lot.scanCode, manufacturerLotNumber: lot.manufacturerLotNumber || null, initialQuantity: lot.initialQuantity })) }));
    if (!items.length) { toast.error("Captura al menos una cantidad a recibir."); return; }
    if (items.some((item) => { const purchaseItem = purchase.items.find((entry) => entry.id === item.purchaseItemId)!; return Number(item.quantity) <= 0 || Number(item.quantity) > purchaseItem.remainingQuantity; })) { toast.error("La cantidad a recibir debe ser mayor que cero y no exceder la pendiente."); return; }
    receiptMutation.mutate({ id: purchase.id, items });
  }

  return <div className="space-y-6">
    <header><h1 className="text-2xl font-bold text-slate-900">Compras</h1><p className="mt-1 text-sm text-slate-500">Registra compras con partidas y conserva los costos históricos.</p></header>
    {manage && <form className="space-y-4 rounded-xl border bg-white p-5" onSubmit={(event) => { event.preventDefault(); createMutation.mutate(); }}>
      <h2 className="font-semibold">Nuevo borrador</h2>
      <div className="grid gap-3 md:grid-cols-3"><select className={input} required value={supplierId} onChange={(event) => chooseSupplier(event.target.value)}><option value="">Proveedor</option>{(suppliers.data?.items ?? []).map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}</select><input className={input} type="date" required value={date} onChange={(event) => setDate(event.target.value)} /><input className={input} placeholder="Factura / referencia" value={invoice} onChange={(event) => setInvoice(event.target.value)} /></div>
      {lines.map((line, index) => <div className="grid gap-3 md:grid-cols-[1fr_180px_auto]" key={index}><select className={input} required value={line.sourceId} onChange={(event) => updateLine(index, event.target.value)}><option value="">Selecciona insumo y costo</option>{sources.map((source) => <option key={source.id} value={source.id}>{source.supplyItemName} · {source.purchaseUnit} · {source.currentUnitCost.toFixed(4)} {source.currency}</option>)}</select><DecimalInput className={input} placeholder="Cantidad" required value={line.quantity} onChange={(event) => setLines(lines.map((entry, i) => i === index ? { ...entry, quantity: event.target.value } : entry))} /><button type="button" className="rounded border px-3" onClick={() => setLines(lines.filter((_, i) => i !== index))}>Quitar</button></div>)}
      <div className="flex flex-wrap gap-3"><button type="button" className="rounded border px-4 py-2 text-sm" onClick={() => setLines([...lines, { sourceId: "", quantity: "", source: null }])}>+ Agregar partida</button><DecimalInput className={input} placeholder="Impuesto" value={tax} onChange={(event) => setTax(event.target.value)} /><button className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white">Crear borrador</button></div>
    </form>}
    {manage && editing && <form className="space-y-4 rounded-xl border border-slate-400 bg-white p-5" onSubmit={(event) => { event.preventDefault(); updateMutation.mutate(); }}>
      <div className="flex items-center justify-between gap-3"><h2 className="font-semibold">Editar borrador</h2><button type="button" className="text-sm underline" onClick={() => setEditing(null)}>Cerrar</button></div>
      <p className="text-sm text-slate-500">Los costos se actualizan desde la relación actual proveedor-insumo al guardar el borrador.</p>
      <div className="grid gap-3 md:grid-cols-3"><select className={input} required value={editSupplierId} onChange={(event) => chooseEditSupplier(event.target.value)}><option value="">Proveedor</option>{(suppliers.data?.items ?? []).map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.name}</option>)}</select><input className={input} type="date" required value={editDate} onChange={(event) => setEditDate(event.target.value)} /><input className={input} placeholder="Factura / referencia" value={editInvoice} onChange={(event) => setEditInvoice(event.target.value)} /></div>
      {editLines.map((line, index) => <div className="grid gap-3 md:grid-cols-[1fr_180px_auto]" key={index}><select className={input} required value={line.sourceId} onChange={(event) => updateEditLine(index, event.target.value)}><option value="">Selecciona insumo y costo</option>{editSources.map((source) => <option key={source.id} value={source.id}>{source.supplyItemName} · {source.purchaseUnit} · {source.currentUnitCost.toFixed(4)} {source.currency}</option>)}</select><DecimalInput className={input} placeholder="Cantidad" required value={line.quantity} onChange={(event) => setEditLines(editLines.map((entry, i) => i === index ? { ...entry, quantity: event.target.value } : entry))} /><button type="button" className="rounded border px-3" onClick={() => setEditLines(editLines.filter((_, i) => i !== index))}>Quitar</button></div>)}
      <div className="grid gap-3 md:grid-cols-2"><DecimalInput className={input} placeholder="Impuesto" value={editTax} onChange={(event) => setEditTax(event.target.value)} /><textarea className={input} placeholder="Notas" value={editNotes} onChange={(event) => setEditNotes(event.target.value)} /></div>
      <div className="flex flex-wrap gap-3"><button type="button" className="rounded border px-4 py-2 text-sm" onClick={() => setEditLines([...editLines, { sourceId: "", quantity: "", source: null }])}>+ Agregar partida</button><button className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white">Guardar borrador</button></div>
    </form>}
    <section className="divide-y rounded-xl border bg-white"><h2 className="p-4 font-semibold">Historial de compras</h2>
      {(purchases.data?.items ?? []).map((purchase) => <details key={purchase.id} className="p-4"><summary className="cursor-pointer"><span className="font-medium">{purchase.supplierName}</span> · {new Date(purchase.purchaseDate).toLocaleDateString("es-MX")} · {purchase.total.toFixed(2)} {purchase.currency} · {statusLabel[purchase.status]}</summary>
        <div className="mt-3 space-y-1 text-sm text-slate-600">{purchase.items.map((item) => <div key={item.id}>{item.descriptionSnapshot} · ordenada {item.quantity} · recibida {item.receivedQuantity} · pendiente {item.remainingQuantity} {item.purchaseUnitSnapshot} · costo histórico {item.unitCost.toFixed(4)} · normalizado {item.normalizedReceivedQuantity}</div>)}</div>
        {manage && <div className="mt-3 flex flex-wrap gap-2">{purchase.status === "Draft" && <><button type="button" className="rounded border px-3 py-1 text-sm" onClick={() => openDraftEditor(purchase)}>Editar borrador</button><button type="button" className="rounded border px-3 py-1 text-sm" onClick={() => statusMutation.mutate({ id: purchase.id, status: 2 })}>Marcar ordenada</button><button type="button" className="rounded border px-3 py-1 text-sm" onClick={() => statusMutation.mutate({ id: purchase.id, status: 5 })}>Cancelar</button></>}{(purchase.status === "Ordered" || purchase.status === "PartiallyReceived") && <button type="button" className="rounded bg-slate-900 px-3 py-1 text-sm text-white" onClick={() => setReceivingId(receivingId === purchase.id ? null : purchase.id)}>Recibir mercancía</button>}</div>}
        {manage && receivingId === purchase.id && <div className="mt-3 space-y-3 rounded border bg-slate-50 p-3"><h3 className="font-medium">Recibir mercancía</h3><ScanCodeScanner onCode={code => void resolveReceivingScan(purchase, code)} />{receivingScanMessage && <p className="text-sm text-amber-700">{receivingScanMessage}</p>}{purchase.items.filter((item) => item.remainingQuantity > 0).map((item) => <div className="space-y-2 border-b pb-3" key={item.id}><div className="text-sm">{item.descriptionSnapshot} · Cantidad ordenada: {item.quantity} · Cantidad recibida: {item.receivedQuantity} · Cantidad pendiente: {item.remainingQuantity}</div><DecimalInput className={input} value={receiptValues[item.id] ?? ""} onChange={(event) => setReceiptValues({ ...receiptValues, [item.id]: event.target.value })} placeholder="Cantidad a recibir" />{(lotValues[item.id] ?? []).map((lot, index) => <div className="grid gap-2 md:grid-cols-3" key={index}><input className={input} value={lot.scanCode} placeholder="Código de lote" onChange={(event) => setLotValues({ ...lotValues, [item.id]: (lotValues[item.id] ?? []).map((entry, i) => i === index ? { ...entry, scanCode: event.target.value } : entry) })} /><input className={input} value={lot.manufacturerLotNumber} placeholder="Lote fabricante (opcional)" onChange={(event) => setLotValues({ ...lotValues, [item.id]: (lotValues[item.id] ?? []).map((entry, i) => i === index ? { ...entry, manufacturerLotNumber: event.target.value } : entry) })} /><DecimalInput className={input} value={lot.initialQuantity} onChange={(event) => setLotValues({ ...lotValues, [item.id]: (lotValues[item.id] ?? []).map((entry, i) => i === index ? { ...entry, initialQuantity: event.target.value } : entry) })} placeholder="Cantidad del lote" /></div>)}<button type="button" className="text-sm underline" onClick={() => setLotValues({ ...lotValues, [item.id]: [...(lotValues[item.id] ?? []), { scanCode: "", manufacturerLotNumber: "", initialQuantity: "" }] })}>+ Agregar lote/bobina</button></div>)}<button type="button" className="rounded bg-slate-900 px-3 py-2 text-sm text-white" onClick={() => submitReceipt(purchase)}>Registrar recepción</button></div>}
        {purchase.receipts.length > 0 && <div className="mt-3 space-y-1 border-t pt-2 text-sm"><strong>Historial de recepciones</strong>{purchase.receipts.map((receipt) => <div key={receipt.id}>{new Date(receipt.receivedAt).toLocaleDateString("es-MX")} · Recibido por: {receipt.receivedByUserName ?? "Usuario no disponible"} · {receipt.items.map((item) => `${item.supplyItemName} ${item.quantityReceived}`).join(", ")}</div>)}</div>}
      </details>)}
      {!purchases.isPending && (purchases.data?.items ?? []).length === 0 && <div className="p-8 text-center text-slate-500">No hay compras registradas.</div>}
    </section>
  </div>;
}
