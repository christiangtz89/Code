import { useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import { DecimalInput } from "../../../components/ui/DecimalInput";
import {
  createExpense,
  getExpenseCategories,
  getExpenses,
} from "../api/inventoryApi";
const input = "w-full rounded-lg border border-slate-300 px-3 py-2 text-sm";
export function ExpensesPage() {
  const canManage = hasPermission("Finance.Manage");
  const q = useQuery({ queryKey: ["expenses"], queryFn: getExpenses });
  const categories = useQuery({
    queryKey: ["expense-categories"],
    queryFn: getExpenseCategories,
  });
  const [form, setForm] = useState({
    expenseCategoryId: "",
    description: "",
    subtotal: "",
    tax: "",
    expenseDate: new Date().toISOString().slice(0, 10),
  });
  const [saving, setSaving] = useState(false);
  async function submit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await createExpense({
        ...form,
        subtotal: form.subtotal === "" ? 0 : form.subtotal,
        tax: form.tax === "" ? 0 : form.tax,
        currency: "MXN",
      });
      setForm({ ...form, description: "", subtotal: "", tax: "" });
      await q.refetch();
      toast.success("Gasto registrado.");
    } catch {
      toast.error("No fue posible registrar el gasto.");
    } finally {
      setSaving(false);
    }
  }
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-slate-900">Gastos</h1>
        <p className="mt-1 text-sm text-slate-500">
          Registra y consulta gastos operativos.
        </p>
      </header>
      {canManage && (
        <form
          onSubmit={submit}
          className="grid gap-3 rounded-xl border bg-white p-4 md:grid-cols-5"
        >
          <select
            className={input}
            required
            value={form.expenseCategoryId}
            onChange={(e) =>
              setForm({ ...form, expenseCategoryId: e.target.value })
            }
          >
            <option value="">Categoría</option>
            {(categories.data ?? []).map((x) => (
              <option key={x.id} value={x.id}>
                {x.name}
              </option>
            ))}
          </select>
          <input
            className={input}
            required
            placeholder="Descripción"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
          <input
            className={input}
            type="date"
            required
            value={form.expenseDate}
            onChange={(e) => setForm({ ...form, expenseDate: e.target.value })}
          />
          <DecimalInput
            className={input}
            placeholder="Subtotal"
            value={form.subtotal}
            onChange={(e) => setForm({ ...form, subtotal: e.target.value })}
          />
          <DecimalInput
            className={input}
            placeholder="Impuesto"
            value={form.tax}
            onChange={(e) => setForm({ ...form, tax: e.target.value })}
          />
          <button
            disabled={saving}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white md:col-span-5"
          >
            Registrar gasto
          </button>
        </form>
      )}
      <div className="rounded-xl border bg-white divide-y">
        {(q.data?.items ?? []).map((x) => (
          <div className="flex items-center justify-between p-4" key={x.id}>
            <div>
              <div className="font-medium">{x.description}</div>
              <div className="text-sm text-slate-500">
                {x.categoryName} · {x.supplierName ?? "Sin proveedor"} ·{" "}
                {new Date(x.expenseDate).toLocaleDateString("es-MX")}
              </div>
            </div>
            <div className="font-semibold">
              {x.total.toFixed(2)} {x.currency}
            </div>
          </div>
        ))}
        {!q.isPending && (q.data?.items ?? []).length === 0 && (
          <div className="p-8 text-center text-slate-500">
            No hay gastos registrados.
          </div>
        )}
      </div>
    </div>
  );
}
