import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import {
  veterinaryClinicSchema,
  type VeterinaryClinicFormValues,
} from '../schemas/veterinaryClinicSchema'
import type { VeterinaryClinic } from '../types/veterinaryClinic.types'

interface VeterinaryClinicFormModalProps {
  isOpen: boolean
  mode: 'create' | 'edit'
  clinic: VeterinaryClinic | null
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (
    values: VeterinaryClinicFormValues,
  ) => Promise<void>
}

export function VeterinaryClinicFormModal({
  isOpen,
  mode,
  clinic,
  isSubmitting,
  onClose,
  onSubmit,
}: VeterinaryClinicFormModalProps) {
  const {
    register,
    reset,
    handleSubmit,
    formState: { errors },
  } = useForm<VeterinaryClinicFormValues>({
    resolver: zodResolver(veterinaryClinicSchema),
    defaultValues: {
      name: '',
      phone: '',
      email: '',
      address: '',
      primaryContactName: '',
    },
  })

  useEffect(() => {
    if (!isOpen) {
      return
    }

    reset({
      name: clinic?.name ?? '',
      phone: clinic?.phone ?? '',
      email: clinic?.email ?? '',
      address: clinic?.address ?? '',
      primaryContactName:
        clinic?.primaryContactName ?? '',
    })
  }, [clinic, isOpen, reset])

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
        aria-labelledby="veterinary-clinic-form-title"
        className="max-h-full w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) =>
          event.stopPropagation()
        }
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Veterinarias
            </p>

            <h2
              id="veterinary-clinic-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === 'create'
                ? 'Registrar veterinaria'
                : 'Editar veterinaria'}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-5 px-6 py-6"
          noValidate
        >
          <div>
            <label
              htmlFor="clinic-name"
              className="block text-sm font-medium text-slate-700"
            >
              Nombre de la veterinaria
            </label>

            <input
              id="clinic-name"
              type="text"
              disabled={isSubmitting}
              {...register('name')}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.name && (
              <p className="mt-2 text-sm text-red-600">
                {errors.name.message}
              </p>
            )}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label
                htmlFor="clinic-phone"
                className="block text-sm font-medium text-slate-700"
              >
                Teléfono
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <input
                id="clinic-phone"
                type="tel"
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
                htmlFor="clinic-email"
                className="block text-sm font-medium text-slate-700"
              >
                Correo electrónico
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <input
                id="clinic-email"
                type="email"
                placeholder="contacto@veterinaria.com"
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
          </div>

          <div>
            <label
              htmlFor="clinic-primary-contact"
              className="block text-sm font-medium text-slate-700"
            >
              Contacto principal
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <input
              id="clinic-primary-contact"
              type="text"
              disabled={isSubmitting}
              {...register('primaryContactName')}
              className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.primaryContactName && (
              <p className="mt-2 text-sm text-red-600">
                {errors.primaryContactName.message}
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="clinic-address"
              className="block text-sm font-medium text-slate-700"
            >
              Dirección
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <textarea
              id="clinic-address"
              rows={3}
              disabled={isSubmitting}
              {...register('address')}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />

            {errors.address && (
              <p className="mt-2 text-sm text-red-600">
                {errors.address.message}
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
                  ? 'Registrar veterinaria'
                  : 'Guardar cambios'}
            </button>
          </footer>
        </form>
      </section>
    </div>
  )
}