import { z } from "zod";

const optionalPhoneSchema = z
  .string()
  .trim()
  .max(25, "El teléfono no puede exceder 25 caracteres.")
  .refine(
    (value) => value === "" || /^[0-9+\-\s()]+$/.test(value),
    "El teléfono contiene caracteres no válidos.",
  );

const optionalEmailSchema = z
  .string()
  .trim()
  .max(200, "El correo no puede exceder 200 caracteres.")
  .refine(
    (value) => value === "" || z.string().email().safeParse(value).success,
    "Ingresa un correo electrónico válido.",
  );

export const veterinaryClinicSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "El nombre debe tener al menos 2 caracteres.")
    .max(150, "El nombre no puede exceder 150 caracteres."),

  phone: optionalPhoneSchema,

  email: optionalEmailSchema,

  address: z
    .string()
    .trim()
    .max(300, "La dirección no puede exceder 300 caracteres."),

  primaryContactName: z
    .string()
    .trim()
    .max(150, "El contacto principal no puede exceder 150 caracteres."),
});

export type VeterinaryClinicFormValues = z.infer<typeof veterinaryClinicSchema>;
