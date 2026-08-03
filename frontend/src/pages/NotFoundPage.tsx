import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 px-6">
      <section className="text-center">
        <p className="text-sm font-semibold text-slate-500">
          Error 404
        </p>

        <h1 className="mt-2 text-3xl font-semibold text-slate-900">
          Página no encontrada
        </h1>

        <Link
          to="/"
          className="mt-6 inline-flex rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
        >
          Volver al inicio
        </Link>
      </section>
    </main>
  )
}