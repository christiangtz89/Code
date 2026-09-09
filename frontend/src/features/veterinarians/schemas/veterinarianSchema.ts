import { z } from "zod";

const optionalClinicSchema = z
  .string()
  .trim()
  .refine(
    (value) => value === "" || z.string().uuid().safeParse(value).success,
    "Selecciona una veterinaria válida.",
  );

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
  .max(150, "El correo no puede exceder 150 caracteres.")
  .refine(
    (value) => value === "" || z.string().email().safeParse(value).success,
    "Ingresa un correo electrónico válido.",
  );

export const veterinarianSchema = z.object({
  veterinaryClinicId: optionalClinicSchema,

  firstName: z
    .string()
    .trim()
    .min(2, "El nombre debe tener al menos 2 caracteres.")
    .max(100, "El nombre no puede exceder 100 caracteres."),

  lastName: z
    .string()
    .trim()
    .min(2, "El apellido paterno debe tener al menos 2 caracteres.")
    .max(100, "El apellido paterno no puede exceder 100 caracteres."),

  secondLastName: z
    .string()
    .trim()
    .max(100, "El apellido materno no puede exceder 100 caracteres."),

  phone: optionalPhoneSchema,

  email: optionalEmailSchema,

  professionalLicenseNumber: z
    .string()
    .trim()
    .max(50, "La cédula profesional no puede exceder 50 caracteres."),
});

export type VeterinarianFormValues = z.infer<typeof veterinarianSchema>;
