import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'

interface SidebarProps {
  isOpen: boolean
  onClose: () => void
}

interface NavigationItem {
  to: string
  label: string
  icon: ReactNode
  end?: boolean
}

interface NavigationGroup {
  title: string
  items: NavigationItem[]
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
  )
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
  )
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
  )
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
  )
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
  )
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
  )
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
  )
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
  )
}

const navigationGroups: NavigationGroup[] = [
  {
    title: 'Principal',
    items: [
      {
        to: '/',
        label: 'Inicio',
        icon: <HomeIcon />,
        end: true,
      },
    ],
  },
  {
    title: 'Operación',
    items: [
      {
        to: '/customers',
        label: 'Clientes',
        icon: <CustomersIcon />,
      },
      {
        to: '/pets',
        label: 'Mascotas',
        icon: <PetsIcon />,
      },
      {
        to: '/receptions',
        label: 'Recepciones',
        icon: <ReceptionIcon />,
      },
      {
        to: '/cremations',
        label: 'Cremaciones',
        icon: <CremationIcon />,
      },
    ],
  },
  {
    title: 'Directorio veterinario',
    items: [
      {
        to: '/veterinary-clinics',
        label: 'Veterinarias',
        icon: <BuildingIcon />,
      },
      {
        to: '/veterinarians',
        label: 'Veterinarios',
        icon: <VeterinarianIcon />,
      },
    ],
  },
  {
    title: 'Sistema',
    items: [
      {
        to: '/api-test',
        label: 'Diagnóstico API',
        icon: <DiagnosticIcon />,
      },
    ],
  },
]

export function Sidebar({
  isOpen,
  onClose,
}: SidebarProps) {
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
          'fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-slate-800 bg-slate-950 text-white transition-transform duration-200',
          isOpen
            ? 'translate-x-0'
            : '-translate-x-full lg:translate-x-0',
        ].join(' ')}
      >
        <div className="flex h-20 items-center gap-3 border-b border-slate-800 px-6">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white text-lg font-bold text-slate-950">
            P
          </div>

          <div>
            <p className="font-semibold tracking-wide">
              PCMS
            </p>

            <p className="text-xs text-slate-400">
              Gestión de cremaciones
            </p>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto px-4 py-6">
          <div className="space-y-7">
            {navigationGroups.map((group) => (
              <section key={group.title}>
                <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  {group.title}
                </p>

                <div className="space-y-1">
                  {group.items.map((item) => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      end={item.end}
                      onClick={onClose}
                      className={({ isActive }) =>
                        [
                          'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
                          isActive
                            ? 'bg-white text-slate-950'
                            : 'text-slate-300 hover:bg-slate-900 hover:text-white',
                        ].join(' ')
                      }
                    >
                      {item.icon}
                      <span>{item.label}</span>
                    </NavLink>
                  ))}
                </div>
              </section>
            ))}
          </div>
        </nav>

        <div className="border-t border-slate-800 px-6 py-5">
          <p className="text-xs text-slate-500">
            Sistema de gestión
          </p>

          <p className="mt-1 text-sm text-slate-300">
            Cremación de mascotas
          </p>
        </div>
      </aside>
    </>
  )
}