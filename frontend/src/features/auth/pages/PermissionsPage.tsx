import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import {
  getPermissions,
  getRolePermissions,
  setRolePermissions,
} from "../api/permissionsApi";

export function PermissionsPage() {
  const roles = useQuery({
    queryKey: ["roles-permissions"],
    queryFn: getRolePermissions,
  });
  const permissions = useQuery({
    queryKey: ["permissions"],
    queryFn: getPermissions,
  });
  const [selected, setSelected] = useState("");
  const [checked, setChecked] = useState<string[]>([]);
  const queryClient = useQueryClient();
  const save = useMutation({
    mutationFn: () =>
      setRolePermissions(
        selected,
        (permissions.data ?? [])
          .filter((item) => checked.includes(item.code))
          .map((item) => item.id),
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["roles-permissions"] });
      toast.success(
        "Permisos guardados. Se requiere iniciar sesión nuevamente para actualizar el token.",
      );
    },
    onError: () => toast.error("No fue posible guardar los permisos."),
  });
  function selectRole(id: string) {
    setSelected(id);
    setChecked(roles.data?.find((role) => role.id === id)?.permissions ?? []);
  }
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-slate-900">Permisos por rol</h1>
        <p className="mt-1 text-sm text-slate-500">
          Asigna permisos de Ver o Administrar a cada rol.
        </p>
      </header>
      <div className="grid gap-6 md:grid-cols-[220px_1fr]">
        <div className="rounded-xl border bg-white p-3">
          {(roles.data ?? []).map((role) => (
            <button
              key={role.id}
              className={`block w-full rounded px-3 py-2 text-left text-sm ${selected === role.id ? "bg-slate-900 text-white" : "hover:bg-slate-100"}`}
              onClick={() => selectRole(role.id)}
            >
              {role.name}
            </button>
          ))}
        </div>
        <section className="rounded-xl border bg-white p-5">
          <h2 className="font-semibold">
            {roles.data?.find((role) => role.id === selected)?.name ??
              "Selecciona un rol"}
          </h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            {(permissions.data ?? []).map((permission) => (
              <label
                key={permission.id}
                className="flex items-start gap-3 rounded border p-3 text-sm"
              >
                <input
                  type="checkbox"
                  disabled={!selected}
                  checked={checked.includes(permission.code)}
                  onChange={(event) =>
                    setChecked(
                      event.target.checked
                        ? [...checked, permission.code]
                        : checked.filter((code) => code !== permission.code),
                    )
                  }
                />
                <span>
                  <span className="block font-medium">
                    {permission.code.endsWith(".Manage")
                      ? "Administrar"
                      : "Ver"}
                  </span>
                  <span className="text-slate-500">
                    {permission.name} · {permission.code}
                  </span>
                </span>
              </label>
            ))}
          </div>
          <button
            disabled={!selected || save.isPending}
            onClick={() => save.mutate()}
            className="mt-5 rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
          >
            Guardar cambios
          </button>
        </section>
      </div>
    </div>
  );
}
