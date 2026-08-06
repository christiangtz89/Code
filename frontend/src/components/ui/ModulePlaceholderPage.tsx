interface ModulePlaceholderPageProps {
  title: string;
  description: string;
}

export function ModulePlaceholderPage({
  title,
  description,
}: ModulePlaceholderPageProps) {
  return (
    <section>
      <div>
        <p className="text-sm font-medium text-slate-500">Módulo</p>

        <h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
          {title}
        </h1>

        <p className="mt-3 max-w-2xl text-slate-600">{description}</p>
      </div>

      <div className="mt-8 rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-lg font-semibold text-slate-600">
          {title.charAt(0)}
        </div>

        <h2 className="mt-4 font-semibold text-slate-900">Módulo preparado</h2>

        <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
          La estructura de navegación está lista. Los datos y formularios se
          conectarán al backend en las siguientes fases.
        </p>
      </div>
    </section>
  );
}
