import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import {
  createSupplier,
  deactivateSupplier,
  getSuppliers,
  restoreSupplier,
  updateSupplier,
  type Supplier,
} from "../api/inventoryApi";

const input = "w-full rounded-lg border border-slate-300 px-3 py-2 text-sm";
const fields = [
  ["name", "Nombre"],
  ["legalName", "Razón social"],
  ["taxId", "RFC"],
  ["contactName", "Contacto"],
  ["phone", "Teléfono"],
  ["email", "Correo"],
  ["website", "Sitio web"],
  ["address", "Dirección"],
  ["notes", "Notas"],
] as const;
type Form = Record<(typeof fields)[number][0], string>;
const empty = Object.fromEntries(fields.map(([key]) => [key, ""])) as Form;

export function SuppliersPage() {
  const canManage = hasPermission("Suppliers.Manage");
  const [search, setSearch] = useState("");
  const [active, setActive] = useState(true);
  const [form, setForm] = useState<Form>(empty);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const queryClient = useQueryClient();
  const suppliers = useQuery({
    queryKey: ["suppliers", search, active],
    queryFn: () => getSuppliers(search, active),
  });
  const save = useMutation({
    mutationFn: () =>
      editing ? updateSupplier(editing.id, form) : createSupplier(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["suppliers"] });
      setEditing(null);
      setForm({ ...empty });
      toast.success("Proveedor guardado.");
    },
    onError: () => toast.error("No fue posible guardar el proveedor."),
  });
  const toggle = useMutation({
    mutationFn: (supplier: Supplier) =>
      supplier.isActive
        ? deactivateSupplier(supplier.id)
        : restoreSupplier(supplier.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["suppliers"] }),
  });
  function edit(supplier: Supplier) {
    setEditing(supplier);
    setForm(
      Object.fromEntries(
        fields.map(([key]) => [key, supplier[key as keyof Supplier] ?? ""]),
      ) as Form,
    );
  }
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-slate-900">Proveedores</h1>
        <p className="mt-1 text-sm text-slate-500">
          Administra proveedores y datos de contacto.
        </p>
      </header>
      <div className="flex gap-3">
        <input
          className={input}
          placeholder="Buscar por nombre o RFC"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <label className="flex items-center gap-2 whitespace-nowrap text-sm">
          <input
            type="checkbox"
            checked={!active}
            onChange={(event) => setActive(!event.target.checked)}
          />{" "}
          Inactivos
        </label>
      </div>
      {canManage && (
        <form
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate();
          }}
          className="grid gap-3 rounded-xl border bg-white p-4 md:grid-cols-3"
        >
          {fields.map(([key, label]) => (
            <input
              key={key}
              className={input}
              placeholder={label}
              value={form[key]}
              required={key === "name"}
              onChange={(event) =>
                setForm({ ...form, [key]: event.target.value })
              }
            />
          ))}
          <div className="flex gap-2 md:col-span-3">
            <button className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white">
              {editing ? "Guardar cambios" : "Registrar proveedor"}
            </button>
            {editing && (
              <button
                type="button"
                className="rounded-lg border px-4 py-2 text-sm"
                onClick={() => {
                  setEditing(null);
                  setForm({ ...empty });
                }}
              >
                Cancelar
              </button>
            )}
          </div>
        </form>
      )}
      <div className="overflow-hidden rounded-xl border bg-white divide-y">
        {(suppliers.data?.items ?? []).map((supplier) => (
          <div
            key={supplier.id}
            className="flex flex-wrap items-center justify-between gap-3 p-4"
          >
            <div>
              <div className="font-medium">{supplier.name}</div>
              <div className="text-sm text-slate-500">
                {supplier.legalName ?? ""}{" "}
                {supplier.taxId ? `· ${supplier.taxId}` : ""}{" "}
                {supplier.email ? `· ${supplier.email}` : ""}
              </div>
            </div>
            {canManage && (
              <div className="flex gap-2">
                <button
                  className="rounded border px-3 py-1 text-sm"
                  onClick={() => edit(supplier)}
                >
                  Editar
                </button>
                <button
                  className="rounded border px-3 py-1 text-sm"
                  onClick={() => toggle.mutate(supplier)}
                >
                  {supplier.isActive ? "Desactivar" : "Restaurar"}
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
