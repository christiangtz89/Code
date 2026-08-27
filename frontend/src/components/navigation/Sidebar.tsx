import { useEffect, useState, type ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { hasPermission } from "../../features/auth/utils/permissions";

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

interface NavigationItem {
  to: string;
  label: string;
  icon: ReactNode;
  end?: boolean;
  permission?: string;
  anyOf?: string[];
}

interface NavigationGroup {
  title: string;
  items: NavigationItem[];
}

function HomeIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="m3 11 9-8 9 8" />
      <path d="M5 10v10h14V10" />
      <path d="M9 20v-6h6v6" />
    </svg>
  );
}

function CustomersIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20c0-4 2.5-7 6-7s6 3 6 7" />
      <circle cx="17" cy="9" r="2" />
      <path d="M15.5 14c3.2 0 5.5 2.3 5.5 6" />
    </svg>
  );
}

function PetsIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <circle cx="7" cy="7" r="2" />
      <circle cx="17" cy="7" r="2" />
      <circle cx="5" cy="13" r="2" />
      <circle cx="19" cy="13" r="2" />
      <path d="M12 10c-3 0-6 3.3-6 6.2C6 18.5 8 20 10 19c1.3-.7 2.7-.7 4 0 2 1 4-.5 4-2.8C18 13.3 15 10 12 10Z" />
    </svg>
  );
}

function PackageIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M4 7.5 12 3l8 4.5-8 4.5-8-4.5Z" />
      <path d="M4 7.5V16l8 5 8-5V7.5" />
      <path d="M12 12v9" />
    </svg>
  );
}

function UrnIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M8 4h8" />
      <path d="M9 4c0 2-2 3-2 6 0 2 1 4 2 6" />
      <path d="M15 4c0 2 2 3 2 6 0 2-1 4-2 6" />
      <path d="M9 16h6" />
      <path d="M10 16v3h4v-3" />
      <path d="M9 20h6" />
    </svg>
  );
}

function PricingIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M12 3v18" />
      <path d="M17 7.5c0-1.9-2.1-3-5-3s-5 1.1-5 3 1.7 2.8 5 3.5 5 1.6 5 3.5-2.1 3-5 3-5-1.1-5-3" />
    </svg>
  );
}

function BuildingIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M4 21V5a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v16" />
      <path d="M16 9h2a2 2 0 0 1 2 2v10" />
      <path d="M8 7h4M8 11h4M8 15h4" />
      <path d="M2 21h20" />
    </svg>
  );
}

function VeterinarianIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <circle cx="12" cy="7" r="4" />
      <path d="M5 21c0-4 3-7 7-7s7 3 7 7" />
      <path d="M12 17v4M10 19h4" />
    </svg>
  );
}

function VeterinaryRequestIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M6 3h12a2 2 0 0 1 2 2v16H4V5a2 2 0 0 1 2-2Z" />
      <path d="M8 8h8M8 12h8M8 16h5" />
      <path d="m15 17 2 2 4-4" />
    </svg>
  );
}

function ReceptionIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <path d="M8 8h8M8 12h8M8 16h5" />
      <path d="M9 3v2M15 3v2" />
    </svg>
  );
}

function CollectionIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M3 17h18" />
      <path d="M5 17V9h11l3 4v4" />
      <path d="M16 9v4h3" />
      <circle cx="8" cy="18" r="2" />
      <circle cx="17" cy="18" r="2" />
      <path d="M7 6h6" />
      <path d="m10 3 3 3-3 3" />
    </svg>
  );
}

function CremationIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M12 22c4 0 7-3 7-7 0-5-4-7-4-11-3 2-4 5-3 8-2-1-3-3-3-5-2 2-4 5-4 8 0 4 3 7 7 7Z" />
      <path d="M12 22c2 0 4-1.6 4-4 0-2.5-2-4-3-5.5 0 2-1 3-2 4-1-.8-1.5-1.8-1.5-3C8.5 15 8 16.5 8 18c0 2.4 2 4 4 4Z" />
    </svg>
  );
}

function DiagnosticIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M4 19h16" />
      <path d="m6 15 3-4 3 2 5-7" />
      <circle cx="6" cy="15" r="1" />
      <circle cx="9" cy="11" r="1" />
      <circle cx="12" cy="13" r="1" />
      <circle cx="17" cy="6" r="1" />
    </svg>
  );
}

function PaymentIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <rect x="3" y="5" width="18" height="14" rx="2" />

      <path d="M3 10h18" />
      <path d="M7 15h4" />
    </svg>
  );
}

const navigationGroups: NavigationGroup[] = [
  {
    title: "Principal",
    items: [{ to: "/", label: "Inicio", icon: <HomeIcon />, end: true }],
  },
  {
    title: "Operación",
    items: [
      { to: "/collections", label: "Recolecciones", icon: <CollectionIcon /> },
      { to: "/receptions", label: "Recepciones", icon: <ReceptionIcon /> },
      { to: "/cremations", label: "Cremaciones", icon: <CremationIcon /> },
      { to: "/payments", label: "Pagos", icon: <PaymentIcon /> },
      {
        to: "/veterinary-requests",
        label: "Solicitudes veterinarias",
        icon: <VeterinaryRequestIcon />,
      },
    ],
  },
  {
    title: "Clientes",
    items: [
      { to: "/customers", label: "Clientes", icon: <CustomersIcon /> },
      { to: "/pets", label: "Mascotas", icon: <PetsIcon /> },
      {
        to: "/veterinary-clinics",
        label: "Veterinarias",
        icon: <BuildingIcon />,
      },
      {
        to: "/veterinarians",
        label: "Veterinarios",
        icon: <VeterinarianIcon />,
      },
    ],
  },
  {
    title: "Inventario",
    items: [
      {
        to: "/inventory",
        label: "Insumos",
        icon: <PackageIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/inventory-lots",
        label: "Lotes y bobinas",
        icon: <PackageIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/urn-inventory",
        label: "Inventario de urnas",
        icon: <UrnIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/bom",
        label: "Materiales de urnas",
        icon: <PackageIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/production",
        label: "Producción de urnas",
        icon: <UrnIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/inventory-labels",
        label: "Etiquetas de inventario",
        icon: <PackageIcon />,
        permission: "Inventory.View",
      },
      {
        to: "/inventory-reports",
        label: "Reportes de inventario",
        icon: <DiagnosticIcon />,
        permission: "Inventory.View",
      },
    ],
  },
  {
    title: "Compras",
    items: [
      {
        to: "/suppliers",
        label: "Proveedores",
        icon: <BuildingIcon />,
        permission: "Suppliers.View",
      },
      {
        to: "/purchases",
        label: "Compras",
        icon: <PackageIcon />,
        permission: "Purchasing.View",
      },
      {
        to: "/cost-analytics",
        label: "Análisis de costos",
        icon: <PricingIcon />,
        permission: "Purchasing.View",
      },
    ],
  },
  {
    title: "Finanzas",
    items: [
      {
        to: "/expenses",
        label: "Gastos",
        icon: <PricingIcon />,
        permission: "Finance.View",
      },
      {
        to: "/spending-reports",
        label: "Reportes de gastos",
        icon: <DiagnosticIcon />,
        anyOf: ["Finance.View", "Purchasing.View"],
      },
    ],
  },
  {
    title: "Administración",
    items: [
      {
        to: "/cremation-packages",
        label: "Paquetes de cremación",
        icon: <PackageIcon />,
      },
      { to: "/urns", label: "Catálogo de urnas", icon: <UrnIcon /> },
      {
        to: "/cremation-pricing",
        label: "Precios de cremación",
        icon: <PricingIcon />,
      },
      {
        to: "/permissions",
        label: "Roles y permisos",
        icon: <BuildingIcon />,
        permission: "Permissions.Manage",
      },
      { to: "/api-test", label: "Diagnóstico API", icon: <DiagnosticIcon /> },
    ],
  },
];

function ScannerIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      className="h-5 w-5"
      aria-hidden="true"
    >
      <path d="M4 7V5a1 1 0 0 1 1-1h2M17 4h2a1 1 0 0 1 1 1v2M20 17v2a1 1 0 0 1-1 1h-2M7 20H5a1 1 0 0 1-1-1v-2M7 9h10M7 12h10M7 15h10" />
    </svg>
  );
}

function isVisible(item: NavigationItem) {
  return item.permission
    ? hasPermission(item.permission)
    : item.anyOf
      ? item.anyOf.some(hasPermission)
      : true;
}

export function Sidebar({ isOpen, onClose }: SidebarProps) {
  const { pathname } = useLocation();
  const visibleGroups = navigationGroups
    .map((group) => ({ ...group, items: group.items.filter(isVisible) }))
    .filter((group) => group.items.length > 0);
  const canUseScanner = hasPermission("Inventory.View");
  const activeGroup = visibleGroups.find((group) =>
    group.items.some((item) =>
      item.end ? pathname === item.to : pathname.startsWith(item.to),
    ),
  )?.title;
  const [collapsedGroups, setCollapsedGroups] = useState<string[]>([]);

  useEffect(() => {
    if (activeGroup) {
      setCollapsedGroups((groups) =>
        groups.filter((title) => title !== activeGroup),
      );
    }
  }, [activeGroup]);

  function toggleGroup(title: string) {
    setCollapsedGroups((groups) =>
      groups.includes(title)
        ? groups.filter((group) => group !== title)
        : [...groups, title],
    );
  }
  return (
    <>
      {isOpen && (
        <button
          type="button"
          aria-label="Cerrar menú"
          onClick={onClose}
          className="fixed inset-0 z-40 bg-slate-950/50 lg:hidden"
        />
      )}

      <aside
        className={[
          "fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-slate-800 bg-slate-950 text-white transition-transform duration-200",
          isOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0",
        ].join(" ")}
      >
        <div className="flex h-20 items-center gap-3 border-b border-slate-800 px-6">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white text-lg font-bold text-slate-950">
            P
          </div>

          <div>
            <p className="font-semibold tracking-wide">PCMS</p>

            <p className="text-xs text-slate-400">Gestión de cremaciones</p>
          </div>
        </div>

        <nav
          aria-label="Navegación principal"
          className="flex-1 overflow-y-auto px-4 py-6"
        >
          <div className="space-y-7">
            {canUseScanner && (
              <NavLink
                to="/inventory-scanner"
                onClick={onClose}
                className={({ isActive }) =>
                  [
                    "flex min-h-12 items-center gap-3 rounded-xl border px-3 py-3 text-sm font-semibold transition",
                    isActive
                      ? "border-white bg-white text-slate-950"
                      : "border-cyan-400/40 bg-cyan-400/10 text-cyan-100 hover:bg-cyan-400/20",
                  ].join(" ")
                }
              >
                <ScannerIcon />
                <span>Escáner de inventario</span>
              </NavLink>
            )}
            {visibleGroups.map((group) => (
              <section key={group.title}>
                {(() => {
                  const isExpanded =
                    activeGroup === group.title ||
                    !collapsedGroups.includes(group.title);
                  const contentId = `navigation-group-${group.title.toLowerCase().replaceAll(" ", "-")}`;

                  return (
                    <>
                      <button
                        type="button"
                        aria-expanded={isExpanded}
                        aria-controls={contentId}
                        onClick={() => toggleGroup(group.title)}
                        disabled={activeGroup === group.title}
                        className="mb-2 flex min-h-10 w-full items-center justify-between rounded-lg px-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 transition hover:bg-slate-900 hover:text-slate-300 disabled:cursor-default disabled:hover:bg-transparent disabled:hover:text-slate-500"
                      >
                        <span>{group.title}</span>
                        <svg
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                          className={`h-4 w-4 transition-transform ${isExpanded ? "rotate-180" : ""}`}
                          aria-hidden="true"
                        >
                          <path d="m6 9 6 6 6-6" />
                        </svg>
                      </button>

                      <div
                        id={contentId}
                        hidden={!isExpanded}
                        className="space-y-1"
                      >
                        {group.items.map((item) => (
                          <NavLink
                            key={item.to}
                            to={item.to}
                            end={item.end}
                            onClick={onClose}
                            className={({ isActive }) =>
                              [
                                "flex min-h-11 items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition",
                                isActive
                                  ? "bg-white text-slate-950"
                                  : "text-slate-300 hover:bg-slate-900 hover:text-white",
                              ].join(" ")
                            }
                          >
                            {item.icon}
                            <span>{item.label}</span>
                          </NavLink>
                        ))}
                      </div>
                    </>
                  );
                })()}
              </section>
            ))}
          </div>
        </nav>

        <div className="border-t border-slate-800 px-6 py-5">
          <p className="text-xs text-slate-500">Sistema de gestión</p>

          <p className="mt-1 text-sm text-slate-300">Cremación de mascotas</p>
        </div>
      </aside>
    </>
  );
}
