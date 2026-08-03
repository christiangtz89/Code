import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import {
  customerSchema,
  type CustomerFormValues,
} from '../schemas/customerSchema'
import type { Customer } from '../types/customer.types'

interface CustomerFormModalProps {
  isOpen: boolean
  mode: 'create' | 'edit'
  customer: Customer | null
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (values: CustomerFormValues) => Promise<void>
}

export function CustomerFormModal({
  isOpen,
  mode,
  customer,
  isSubmitting,
  onClose,
  onSubmit,
}: CustomerFormModalProps) {
  const {
    register,
    reset,
    handleSubmit,
    formState: {
      errors,
    },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phone: '',
      email: '',
    },
  })

  useEffect(() => {
    if (!isOpen) {
      return
    }

    reset({
      firstName: customer?.firstName ?? '',
      lastName: customer?.lastName ?? '',
      phone: customer?.phone ?? '',
      email: customer?.email ?? '',
    })
  }, [customer, isOpen, reset])

  if (!isOpen) {
    return null
  }

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose()
    }
  }

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="customer-form-title"
        className="max-h-full w-full max-w-xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Clientes
            </p>

            <h2
              id="customer-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === 'create'
                ? 'Registrar cliente'
                : 'Editar cliente'}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ✕
          </button>
        </header>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-5 px-6 py-6"
          noValidate
        >
          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label
                htmlFor="customer-first-name"
                className="block text-sm font-medium text-slate-700"
              >
                Nombre
              </label>

              <input
                id="customer-first-name"
                type="text"
                autoComplete="given-name"
                disabled={isSubmitting}
                {...register('firstName')}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.firstName && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.firstName.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="customer-last-name"
                className="block text-sm font-medium text-slate-700"
              >
                Apellido
              </label>

              <input
                id="customer-last-name"
                type="text"
                autoComplete="family-name"
                disabled={isSubmitting}
                {...register('lastName')}
                className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.lastName && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.lastName.message}
                </p>
              )}
            </div>
          </div>

          <div>
            <label
              htmlFor="customer-phone"
              className="block text-sm font-medium text-slate-700"
            >
              Teléfono
            </label>

            <input
              id="customer-phone"
              type="tel"
              autoComplete="tel"
              placeholder="81 1234 5678"
              disabled={isSubmitting}
              {...register('phone')}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.phone && (
              <p className="mt-2 text-sm text-red-600">
                {errors.phone.message}
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="customer-email"
              className="block text-sm font-medium text-slate-700"
            >
              Correo electrónico
            </label>

            <input
              id="customer-email"
              type="email"
              autoComplete="email"
              placeholder="cliente@ejemplo.com"
              disabled={isSubmitting}
              {...register('email')}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.email && (
              <p className="mt-2 text-sm text-red-600">
                {errors.email.message}
              </p>
            )}
          </div>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? 'Guardando...'
                : mode === 'create'
                  ? 'Registrar cliente'
                  : 'Guardar cambios'}
            </button>
          </footer>
        </form>
      </section>
    </div>
  )
}