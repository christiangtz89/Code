import { Navigate } from 'react-router-dom'
import { LoginForm } from '../features/auth/components/LoginForm'
import { isAccessTokenValid } from '../features/auth/utils/authToken'
import { tokenStorage } from '../services/tokenStorage'

export function LoginPage() {
  const token = tokenStorage.get()

  if (token && isAccessTokenValid(token)) {
    return <Navigate to="/" replace />
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 px-6 py-10">
      <section className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <p className="text-sm font-semibold uppercase tracking-wider text-slate-500">
          PCMS
        </p>

        <h1 className="mt-2 text-3xl font-semibold text-slate-900">
          Iniciar sesión
        </h1>

        <p className="mt-3 text-slate-600">
          Accede al sistema de gestión de cremación de mascotas.
        </p>

        <LoginForm />
      </section>
    </main>
  )
}