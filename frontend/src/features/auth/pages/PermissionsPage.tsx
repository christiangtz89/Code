import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import {
  createRole,
  getAuthorizationUsers,
  getPermissions,
  getRolePermissions,
  setRolePermissions,
  setUserRoles,
  updateRole,
  type RolePermissions,
} from "../api/permissionsApi";
import { isOwner } from "../utils/permissions";

const modules: Record<string, [string, string]> = {
  Customers: ["Clientes", "Clientes"],
  Pets: ["Clientes", "Mascotas"],
  VeterinaryClinics: ["Clientes", "Veterinarias"],
  Veterinarians: ["Clientes", "Veterinarios"],
  VeterinaryRequests: ["Operación", "Solicitudes veterinarias"],
  Collections: ["Operación", "Recolecciones"],
  Receptions: ["Operación", "Recepciones"],
  Cremations: ["Operación", "Cremaciones"],
  Payments: ["Operación", "Pagos"],
  Inventory: ["Inventario", "Inventario"],
  Suppliers: ["Compras", "Proveedores"],
  Purchasing: ["Compras", "Compras"],
  Finance: ["Finanzas", "Finanzas"],
  CremationPackages: [
    "Administración / Configuración",
    "Paquetes de cremación",
  ],
  Urns: ["Administración / Configuración", "Urnas"],
  CremationPricing: ["Administración / Configuración", "Precios de cremación"],
};

const inventoryManagePermission = "Inventory.Manage";
const inventoryScanOutgoingPermission = "Inventory.ScanOutgoing";

export function PermissionsPage() {
  const owner = isOwner();
  const qc = useQueryClient();
  const [mode, setMode] = useState<"none" | "create" | "edit">("none");
  const [selected, setSelected] = useState("");
  const [checked, setChecked] = useState<string[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [active, setActive] = useState(true);
  const nameInput = useRef<HTMLInputElement>(null);
  const [userId, setUserId] = useState("");
  const [assigned, setAssigned] = useState<string[]>([]);
  const roles = useQuery({
    queryKey: ["roles-permissions"],
    queryFn: getRolePermissions,
  });
  const permissions = useQuery({
    queryKey: ["permissions"],
    queryFn: getPermissions,
  });
  const users = useQuery({
    queryKey: ["authorization-users"],
    queryFn: getAuthorizationUsers,
  });
  const role = roles.data?.find((x) => x.id === selected);
  const lockPermissions = !!role?.isProtected && !owner;
  const isCreating = mode === "create";
  const isEditing = mode === "edit" && !!selected;
  const refreshRoles = async () =>
    qc.invalidateQueries({ queryKey: ["roles-permissions"] });

  useEffect(() => {
    if (isCreating) nameInput.current?.focus();
  }, [isCreating]);
  const savePermissions = useMutation({
    mutationFn: () =>
      setRolePermissions(
        selected,
        (permissions.data ?? [])
          .filter((p) => checked.includes(p.code))
          .map((p) => p.id),
      ),
    onSuccess: () => {
      refreshRoles();
      toast.success(
        "Los cambios de permisos se aplicarán al volver a iniciar sesión.",
      );
    },
    onError: () => toast.error("No fue posible guardar los permisos."),
  });
  const saveRole = useMutation({
    mutationFn: async () => {
      const input = {
        name,
        description: description || undefined,
        isActive: active,
      };

      if (isCreating) return createRole(input);

      await updateRole(selected, input);
      return undefined;
    },
    onSuccess: async (createdRole) => {
      if (createdRole) {
        setMode("edit");
        setSelected(createdRole.id);
        setChecked([]);
        setName(createdRole.name);
        setDescription(createdRole.description ?? "");
        setActive(createdRole.isActive);
        toast.success("Rol creado correctamente.");
      } else {
        toast.success("Rol guardado correctamente.");
      }

      await refreshRoles();
    },
    onError: () => toast.error("El nombre del rol debe ser único."),
  });
  const saveUser = useMutation({
    mutationFn: () => setUserRoles(userId, assigned),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["authorization-users"] });
      toast.success(
        "Los cambios de permisos se aplicarán al volver a iniciar sesión.",
      );
    },
    onError: () => toast.error("No fue posible guardar los roles."),
  });
  function startCreate() {
    setMode("create");
    setSelected("");
    setChecked([]);
    setName("");
    setDescription("");
    setActive(true);
  }
  function cancelCreate() {
    setMode("none");
    setSelected("");
    setChecked([]);
    setName("");
    setDescription("");
    setActive(true);
  }
  function selectRole(value: string) {
    const item = roles.data?.find((x) => x.id === value);
    if (!item) return;
    setMode("edit");
    setSelected(value);
    setChecked(item.permissions);
    setName(item.name);
    setDescription(item.description ?? "");
    setActive(item.isActive);
  }
  function toggle(code: string, on: boolean) {
    if (lockPermissions) return;
    const view = code.endsWith(".Manage") ? code.slice(0, -7) + ".View" : code;
    setChecked((current) =>
      on
        ? [
            ...new Set([
              ...current,
              code,
              ...(code.endsWith(".Manage") ? [view] : []),
            ]),
          ]
        : current.filter((x) => x !== code),
    );
  }
  const groups = [...new Set(Object.values(modules).map((x) => x[0]))];
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold">Roles y permisos</h1>
        <p className="text-sm text-slate-500">
          Los cambios de permisos se aplicarán al volver a iniciar sesión.
        </p>
      </header>
      <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
        <aside className="rounded-xl border bg-white p-4">
          <button
            onClick={startCreate}
            className="w-full rounded bg-slate-900 px-3 py-2 text-sm text-white"
          >
            Nuevo rol
          </button>
          <div className="mt-3 space-y-1">
            {(roles.data ?? []).map((item) => (
              <button
                key={item.id}
                onClick={() => selectRole(item.id)}
                className={`w-full rounded px-3 py-2 text-left text-sm ${mode === "edit" && selected === item.id ? "bg-slate-100" : "hover:bg-slate-50"}`}
              >
                {item.name}
                {item.isProtected ? " · Protegido" : ""}
              </button>
            ))}
          </div>
        </aside>
        <section className="rounded-xl border bg-white p-5">
          {mode === "none" ? (
            <p className="text-sm text-slate-600">
              Selecciona un rol para editarlo o crea uno nuevo.
            </p>
          ) : (
            <>
              <header className="mb-5">
                <h2 className="text-lg font-semibold">
                  {isCreating ? "Nuevo rol" : `Editar rol: ${name}`}
                </h2>
                {isCreating && (
                  <p className="mt-1 text-sm text-slate-500">
                    Crea el rol antes de configurar sus permisos.
                  </p>
                )}
              </header>
              <div className="grid gap-3 sm:grid-cols-2">
                <input
                  ref={nameInput}
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Nombre del rol"
                  disabled={!!role?.isProtected}
                  className="rounded border px-3 py-2 disabled:cursor-not-allowed disabled:bg-slate-100"
                />
                <input
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Descripción opcional"
                  disabled={!!role?.isProtected}
                  className="rounded border px-3 py-2 disabled:cursor-not-allowed disabled:bg-slate-100"
                />
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={active}
                    disabled={!!role?.isProtected}
                    onChange={(e) => setActive(e.target.checked)}
                  />
                  Activo
                </label>
                <div className="flex gap-2">
                  <button
                    onClick={() => saveRole.mutate()}
                    disabled={
                      !name.trim() || !!role?.isProtected || saveRole.isPending
                    }
                    className="rounded bg-slate-900 px-4 py-2 text-sm text-white disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {isCreating ? "Crear rol" : "Guardar rol"}
                  </button>
                  {isCreating && (
                    <button
                      onClick={cancelCreate}
                      disabled={saveRole.isPending}
                      className="rounded border px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      Cancelar
                    </button>
                  )}
                </div>
              </div>
            </>
          )}
          {isCreating ? (
            <p className="mt-5 rounded bg-slate-50 p-3 text-sm text-slate-600">
              Guarda el nuevo rol para habilitar la matriz de permisos.
            </p>
          ) : lockPermissions ? (
            <p className="mt-5 rounded bg-amber-50 p-3 text-sm text-amber-800">
              Rol protegido. Solo el Propietario puede configurar sus permisos.
            </p>
          ) : isEditing ? (
            <>
              <div className="mt-6 space-y-5">
                {groups.map((group) => (
                  <div key={group}>
                    <h2 className="mb-2 text-sm font-semibold">{group}</h2>
                    <div className="grid grid-cols-[minmax(0,1fr)_52px_96px] gap-2 text-sm">
                      <strong>Módulo</strong>
                      <strong>Ver</strong>
                      <strong>Administrar</strong>
                      {Object.entries(modules)
                        .filter(([, v]) => v[0] === group)
                        .map(([code, [, label]]) => (
                          <div key={code} className="contents">
                            <span className="py-2">{label}</span>
                            <input
                              type="checkbox"
                              aria-label={`Ver ${label}`}
                              checked={
                                checked.includes(`${code}.View`) ||
                                checked.includes(`${code}.Manage`)
                              }
                              disabled={
                                !selected || checked.includes(`${code}.Manage`)
                              }
                              onChange={(e) =>
                                toggle(`${code}.View`, e.target.checked)
                              }
                            />
                            <input
                              type="checkbox"
                              aria-label={`Administrar ${label}`}
                              checked={checked.includes(`${code}.Manage`)}
                              disabled={!selected}
                              onChange={(e) =>
                                toggle(`${code}.Manage`, e.target.checked)
                              }
                            />
                          </div>
                        ))}
                    </div>
                    {group === "Inventario" && (
                      <label className="mt-3 flex items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          checked={
                            checked.includes(inventoryScanOutgoingPermission) ||
                            checked.includes(inventoryManagePermission)
                          }
                          disabled={
                            !selected ||
                            checked.includes(inventoryManagePermission)
                          }
                          onChange={(e) =>
                            toggle(
                              inventoryScanOutgoingPermission,
                              e.target.checked,
                            )
                          }
                        />
                        Registrar salidas por escaneo
                      </label>
                    )}
                  </div>
                ))}
              </div>
              <label className="mt-4 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={checked.includes("Permissions.Manage")}
                  disabled={!selected}
                  onChange={(e) =>
                    toggle("Permissions.Manage", e.target.checked)
                  }
                />
                Administrar roles y permisos
              </label>
              <button
                onClick={() => savePermissions.mutate()}
                disabled={!selected || savePermissions.isPending}
                className="mt-4 rounded bg-slate-900 px-4 py-2 text-sm text-white"
              >
                Guardar permisos
              </button>
            </>
          ) : null}
        </section>
      </div>
      <section className="rounded-xl border bg-white p-5">
        <h2 className="font-semibold">Asignación de roles</h2>
        <select
          value={userId}
          onChange={(e) => {
            setUserId(e.target.value);
            const user = (users.data ?? []).find(
              (x: any) => x.id === e.target.value,
            );
            setAssigned(user?.roles.map((r: any) => r.roleId) ?? []);
          }}
          className="mt-3 w-full max-w-md rounded border px-3 py-2"
        >
          <option value="">Selecciona un usuario</option>
          {(users.data ?? [])
            .filter((user: any) => !user.isOwner)
            .map((user: any) => (
              <option key={user.id} value={user.id}>
                {user.firstName} {user.lastName} · {user.email}
              </option>
            ))}
        </select>
        <div className="mt-4 flex flex-wrap gap-3">
          {(roles.data ?? [])
            .filter(
              (item: RolePermissions) =>
                item.isActive && (!item.isProtected || owner),
            )
            .map((item) => (
              <label
                key={item.id}
                className="flex min-h-11 items-center gap-2 text-sm"
              >
                <input
                  type="checkbox"
                  checked={assigned.includes(item.id)}
                  disabled={!userId}
                  onChange={(e) =>
                    setAssigned((current) =>
                      e.target.checked
                        ? [...current, item.id]
                        : current.filter((id) => id !== item.id),
                    )
                  }
                />
                {item.name}
              </label>
            ))}
        </div>
        <button
          onClick={() => saveUser.mutate()}
          disabled={!userId || saveUser.isPending}
          className="mt-4 rounded bg-slate-900 px-4 py-2 text-sm text-white"
        >
          Guardar roles
        </button>
      </section>
    </div>
  );
}
