import { z } from 'zod'

export const customerSchema = z.object({
  firstName: z
    .string()
    .trim()
    .min(2, 'El nombre debe tener al menos 2 caracteres.')
    .max(100, 'El nombre no puede exceder 100 caracteres.'),

  lastName: z
    .string()
    .trim()
    .min(2, 'El apellido debe tener al menos 2 caracteres.')
    .max(100, 'El apellido no puede exceder 100 caracteres.'),

  secondLastName: z
    .string()
    .trim()
    .max(100, 'El apellido materno no puede exceder 100 caracteres.',),  

  phone: z
    .string()
    .trim()
    .min(7, 'Ingresa un número telefónico válido.')
    .max(25, 'El teléfono no puede exceder 25 caracteres.')
    .regex(
      /^[0-9+\-\s()]+$/,
      'El teléfono contiene caracteres no válidos.',
    ),

  email: z
    .string()
    .trim()
    .min(1, 'El correo electrónico es obligatorio.')
    .email('Ingresa un correo electrónico válido.')
    .max(200, 'El correo no puede exceder 200 caracteres.'),
})

export type CustomerFormValues = z.infer<
  typeof customerSchema
>