import { z } from "zod";

const optionalUuidSchema = z
  .string()
  .refine(
    (value) => value === "" || z.string().uuid().safeParse(value).success,
    "La selección no es válida.",
  );

const verifiedWeightSchema = z
  .string()
  .trim()
  .min(1, "El peso verificado es obligatorio.")
  .refine(
    (value) => Number.isFinite(Number(value)),
    "El peso verificado debe ser un número válido.",
  )
  .refine(
    (value) => Number(value) >= 0.01,
    "El peso verificado debe ser mayor que cero.",
  )
  .refine(
    (value) => Number(value) <= 999.99,
    "El peso verificado no puede exceder 999.99 kg.",
  );

export const receptionSchema = z
  .object({
    petId: z
      .string()
      .min(1, "Debe seleccionar una mascota.")
      .uuid("Debe seleccionar una mascota válida."),

    veterinaryClinicId: optionalUuidSchema,

    referringVeterinarianId: optionalUuidSchema,

    verifiedWeightKg: verifiedWeightSchema,

    hasPersonalBelongings: z.boolean(),

    personalBelongingsDescription: z
      .string()
      .trim()
      .max(
        500,
        "La descripción de los objetos personales no puede exceder 500 caracteres.",
      ),

    referralNotes: z
      .string()
      .trim()
      .max(1000, "Las notas de referencia no pueden exceder 1000 caracteres."),

    notes: z
      .string()
      .trim()
      .max(1000, "Las notas no pueden exceder 1000 caracteres."),
  })
  .superRefine((values, context) => {
    if (
      values.hasPersonalBelongings &&
      values.personalBelongingsDescription.length === 0
    ) {
      context.addIssue({
        code: "custom",
        path: ["personalBelongingsDescription"],
        message: "Debe describir los objetos personales recibidos.",
      });
    }

    if (
      values.referringVeterinarianId !== "" &&
      values.veterinaryClinicId === ""
    ) {
      context.addIssue({
        code: "custom",
        path: ["referringVeterinarianId"],
        message:
          "Debe seleccionar una veterinaria antes de elegir al veterinario referente.",
      });
    }

    if (values.referralNotes.length > 0 && values.veterinaryClinicId === "") {
      context.addIssue({
        code: "custom",
        path: ["referralNotes"],
        message:
          "Debe seleccionar una veterinaria para registrar notas de referencia.",
      });
    }
  });

export type ReceptionFormValues = z.infer<typeof receptionSchema>;
