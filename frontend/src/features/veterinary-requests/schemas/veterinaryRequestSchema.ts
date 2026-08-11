import { z } from "zod";

const optionalIdSchema = z
  .string()
  .trim()
  .refine(
    (value) => value === "" || z.string().uuid().safeParse(value).success,
    "La selección no es válida.",
  );

const optionalEmailSchema = z
  .string()
  .trim()
  .max(150, "El correo no puede exceder 150 caracteres.")
  .refine(
    (value) => value === "" || z.string().email().safeParse(value).success,
    "Ingresa un correo electrónico válido.",
  );

const phoneSchema = z
  .string()
  .trim()
  .min(1, "El teléfono es obligatorio.")
  .max(30, "El teléfono no puede exceder 30 caracteres.")
  .refine(
    (value) => /^[0-9+\-\s()]+$/.test(value),
    "El teléfono contiene caracteres no válidos.",
  );

export const veterinaryRequestSchema = z
  .object({
    veterinaryClinicId: optionalIdSchema,

    referringVeterinarianId: optionalIdSchema,

    ownerFirstName: z
      .string()
      .trim()
      .min(1, "El nombre del propietario es obligatorio.")
      .max(100),

    ownerLastName: z
      .string()
      .trim()
      .min(1, "El apellido paterno es obligatorio.")
      .max(100),

    ownerSecondLastName: z.string().trim().max(100),

    ownerPhone: phoneSchema,

    ownerEmail: optionalEmailSchema,

    petName: z
      .string()
      .trim()
      .min(2, "El nombre de la mascota debe tener al menos 2 caracteres.")
      .max(100),

    species: z.string().trim().min(1, "La especie es obligatoria.").max(50),

    breed: z.string().trim().min(1, "La raza es obligatoria.").max(100),

    sex: z.string().trim().min(1, "El sexo es obligatorio.").max(20),

    color: z.string().trim().min(1, "El color es obligatorio.").max(100),

    approximateWeightKg: z
      .number()
      .finite()
      .positive("El peso aproximado debe ser mayor que cero.")
      .max(999.99, "El peso aproximado no puede exceder 999.99 kg."),

    ageYears: z
      .number()
      .int()
      .min(0, "La edad no puede ser negativa.")
      .max(100, "La edad no puede exceder 100 años.")
      .nullable(),

    dateOfDeath: z
      .string()
      .min(1, "La fecha de fallecimiento es obligatoria.")
      .refine((value) => {
        const date = new Date(`${value}T00:00:00`);

        if (Number.isNaN(date.getTime())) {
          return false;
        }

        const today = new Date();

        today.setHours(23, 59, 59, 999);

        return date <= today;
      }, "La fecha de fallecimiento no puede estar en el futuro."),

    requestedCremationType: z.union([
      z.literal(""),
      z.literal("1"),
      z.literal("2"),
    ]),

    requestedPackageName: z.string().trim().max(150),

    requestNotes: z
      .string()
      .trim()
      .max(1000, "Las notas no pueden exceder 1000 caracteres."),
  })
  .superRefine((values, context) => {
    if (!values.veterinaryClinicId && !values.referringVeterinarianId) {
      context.addIssue({
        code: "custom",
        path: ["veterinaryClinicId"],
        message: "Selecciona una veterinaria o un veterinario referente.",
      });
    }
  });

export type VeterinaryRequestFormValues = z.infer<
  typeof veterinaryRequestSchema
>;
