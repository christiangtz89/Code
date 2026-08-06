interface TopbarProps {
  onOpenMenu: () => void;
  onLogout: () => void;
}

export function Topbar({ onOpenMenu, onLogout }: TopbarProps) {
  return (
    <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div className="flex h-20 items-center justify-between gap-4 px-5 sm:px-8">
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={onOpenMenu}
            aria-label="Abrir menú"
            className="rounded-lg border border-slate-200 p-2 text-slate-600 hover:bg-slate-50 lg:hidden"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              className="h-5 w-5"
              aria-hidden="true"
            >
              <path d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>

          <div>
            <p className="text-sm text-slate-500">Sistema PCMS</p>

            <p className="font-semibold text-slate-900">Panel administrativo</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="hidden text-right sm:block">
            <p className="text-sm font-medium text-slate-800">Usuario activo</p>

            <p className="text-xs text-slate-500">Sesión autenticada</p>
          </div>

          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-sm font-semibold text-slate-700">
            UA
          </div>

          <button
            type="button"
            onClick={onLogout}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          >
            <span className="hidden sm:inline">Cerrar sesión</span>

            <span className="sm:hidden">Salir</span>
          </button>
        </div>
      </div>
    </header>
  );
}
