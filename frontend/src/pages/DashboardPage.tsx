import { Link } from "react-router-dom";

const modules = [
  {
    title: "Clientes",
    description: "Información y contacto de las familias.",
    to: "/customers",
    abbreviation: "CL",
  },
  {
    title: "Mascotas",
    description: "Registro de mascotas y relación con clientes.",
    to: "/pets",
    abbreviation: "MA",
  },
  {
    title: "Recepciones",
    description: "Cadena de custodia, peso, fotografías y pertenencias.",
    to: "/receptions",
    abbreviation: "RE",
  },
  {
    title: "Cremaciones",
    description: "Servicios, programación y seguimiento operativo.",
    to: "/cremations",
    abbreviation: "CR",
  },
  {
    title: "Pagos",
    description: "Anticipos, pagos parciales, saldos e historial financiero.",
    to: "/payments",
    abbreviation: "PA",
  },
  {
    title: "Veterinarias",
    description: "Directorio de clínicas veterinarias.",
    to: "/veterinary-clinics",
    abbreviation: "VE",
  },
  {
    title: "Veterinarios",
    description: "Directorio de médicos veterinarios.",
    to: "/veterinarians",
    abbreviation: "MV",
  },
];

const workflow = [
  "Clientes",
  "Mascotas",
  "Recepciones",
  "Cremaciones",
  "Pagos",
];

export function DashboardPage() {
  return (
    <section>
      <div className="rounded-2xl bg-slate-950 px-6 py-8 text-white sm:px-8">
        <p className="text-sm font-medium text-slate-400">Panel principal</p>

        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          Bienvenido a PCMS
        </h1>

        <p className="mt-3 max-w-2xl text-slate-300">
          Administra el flujo completo de servicios de cremación de mascotas
          desde una sola plataforma.
        </p>

        <div className="mt-7 flex flex-wrap items-center gap-2">
          {workflow.map((step, index) => (
            <div key={step} className="flex items-center gap-2">
              <span className="rounded-full border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-200">
                {step}
              </span>

              {index < workflow.length - 1 && (
                <span className="text-slate-600">→</span>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="mt-8">
        <div>
          <p className="text-sm font-medium text-slate-500">Acceso rápido</p>

          <h2 className="mt-1 text-2xl font-semibold text-slate-900">
            Módulos del sistema
          </h2>
        </div>

        <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {modules.map((module) => (
            <Link
              key={module.to}
              to={module.to}
              className="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md"
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-slate-100 text-sm font-bold text-slate-700 transition group-hover:bg-slate-900 group-hover:text-white">
                  {module.abbreviation}
                </div>

                <span className="text-xl text-slate-300 transition group-hover:translate-x-1 group-hover:text-slate-600">
                  →
                </span>
              </div>

              <h3 className="mt-5 font-semibold text-slate-900">
                {module.title}
              </h3>

              <p className="mt-2 text-sm leading-6 text-slate-500">
                {module.description}
              </p>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}
