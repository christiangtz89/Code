import { z } from 'zod'
import { getLocalDateInputValue } from '../utils/petDates'

export const petSchema = z.object({
  customerId: z
    .string()
    .min(1, 'Selecciona al propietario.')
    .uuid('Selecciona un propietario válido.'),

  name: z
    .string()
    .trim()
    .min(
      2,
      'El nombre debe tener al menos 2 caracteres.',
    )
    .max(
      100,
      'El nombre no puede exceder 100 caracteres.',
    ),

  species: z
    .string()
    .trim()
    .min(1, 'La especie es obligatoria.')
    .max(
      50,
      'La especie no puede exceder 50 caracteres.',
    ),

  breed: z
    .string()
    .trim()
    .min(1, 'La raza es obligatoria.')
    .max(
      100,
      'La raza no puede exceder 100 caracteres.',
    ),

  sex: z
    .string()
    .trim()
    .min(1, 'El sexo es obligatorio.')
    .max(
      20,
      'El sexo no puede exceder 20 caracteres.',
    ),

  color: z
    .string()
    .trim()
    .min(1, 'El color es obligatorio.')
    .max(
      100,
      'El color no puede exceder 100 caracteres.',
    ),

  weightKg: z
    .string()
    .trim()
    .min(1, 'El peso es obligatorio.')
    .refine((value) => {
      const weight = Number(value)

      return (
        Number.isFinite(weight) &&
        weight >= 0.01 &&
        weight <= 999.99
      )
    }, 'El peso debe estar entre 0.01 y 999.99 kg.'),

  ageYears: z
    .string()
    .trim()
    .refine((value) => {
      if (value === '') {
        return true
      }

      if (!/^\d+$/.test(value)) {
        return false
      }

      const age = Number(value)

      return age >= 0 && age <= 100
    }, 'La edad debe estar entre 0 y 100 años.'),

  dateOfDeath: z
    .string()
    .min(
      1,
      'La fecha de fallecimiento es obligatoria.',
    )
    .regex(
      /^\d{4}-\d{2}-\d{2}$/,
      'Ingresa una fecha válida.',
    )
    .refine(
      (value) =>
        value <= getLocalDateInputValue(),
      'La fecha de fallecimiento no puede estar en el futuro.',
    ),
})

export type PetFormValues = z.infer<
  typeof petSchema
>