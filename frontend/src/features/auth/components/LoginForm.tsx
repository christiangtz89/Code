import { zodResolver } from '@hookform/resolvers/zod'
import axios from 'axios'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import {
  useLocation,
  useNavigate,
} from 'react-router-dom'
import { login } from '../api/authApi'
import {
  loginSchema,
  type LoginFormValues,
} from '../schemas/loginSchema'
import { tokenStorage } from '../../../services/tokenStorage'

interface LocationState {
  from?: {
    pathname?: string
  }
}

interface ApiProblemDetails {
  title?: string
  detail?: string
  message?: string
}

function getLoginErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return 'Ocurrió un error inesperado al iniciar sesión.'
  }

  if (!error.response) {
    return 'No fue posible conectarse con el servidor.'
  }

  if (error.response.status === 401) {
    return 'El correo electrónico o la contraseña son incorrectos.'
  }

  if (error.response.status === 400) {
    return 'Revisa los datos ingresados e intenta nuevamente.'
  }

  const data = error.response.data as ApiProblemDetails | string | undefined

  if (typeof data === 'string' && data.trim()) {
    return data
  }

  if (data && typeof data === 'object') {
    return (
      data.detail ??
      data.message ??
      data.title ??
      'El servidor no pudo procesar el inicio de sesión.'
    )
  }

  return 'El servidor no pudo procesar el inicio de sesión.'
}

export function LoginForm() {
  const navigate = useNavigate()
  const location = useLocation()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: {
      errors,
      isSubmitting,
    },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  async function onSubmit(values: LoginFormValues) {
    setServerError(null)

    try {
      const response = await login(values)

      tokenStorage.set(response.token)

      toast.success('Sesión iniciada correctamente.')

      const state = location.state as LocationState | null
      const destination = state?.from?.pathname ?? '/'

      navigate(destination, {
        replace: true,
      })
    } catch (error) {
      const message = getLoginErrorMessage(error)

      setServerError(message)
      toast.error(message)
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="mt-8 space-y-5"
      noValidate
    >
      <div>
        <label
          htmlFor="email"
          className="block text-sm font-medium text-slate-700"
        >
          Correo electrónico
        </label>

        <input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="usuario@ejemplo.com"
          disabled={isSubmitting}
          {...register('email')}
          className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
        />

        {errors.email && (
          <p className="mt-2 text-sm text-red-600">
            {errors.email.message}
          </p>
        )}
      </div>

      <div>
        <label
          htmlFor="password"
          className="block text-sm font-medium text-slate-700"
        >
          Contraseña
        </label>

        <input
          id="password"
          type="password"
          autoComplete="current-password"
          placeholder="Ingresa tu contraseña"
          disabled={isSubmitting}
          {...register('password')}
          className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
        />

        {errors.password && (
          <p className="mt-2 text-sm text-red-600">
            {errors.password.message}
          </p>
        )}
      </div>

      {serverError && (
        <div
          role="alert"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {serverError}
        </div>
      )}

      <button
        type="submit"
        disabled={isSubmitting}
        className="flex w-full items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting
          ? 'Iniciando sesión...'
          : 'Iniciar sesión'}
      </button>
    </form>
  )
}