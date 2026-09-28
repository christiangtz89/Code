import { z } from "zod";

const optionalUuidSchema = z
  .string()
  .refine(
    (value) => value === "" || z.string().uuid().safeParse(value).success,
    "La selección no es válida.",
  );

const correctedWeightSchema = z
  .string()
  .trim()
  .min(1, "El peso corregido es obligatorio.")
  .regex(/^\d+(?:\.\d{1,2})?$/, "El peso admite como máximo dos decimales.")
  .refine(
    (value) => Number.isFinite(Number(value)) && Number(value) > 0,
    "El peso corregido debe ser mayor que cero.",
  )
  .refine(
    (value) => Number(value) <= 999.99,
    "El peso corregido no puede exceder 999.99 kg.",
  );

export const receptionCorrectionSchema = z
  .object({
    reason: z
      .string()
      .trim()
      .min(1, "El motivo de la enmienda es obligatorio.")
      .max(1000, "El motivo no puede exceder 1000 caracteres."),
    verifiedWeightKg: correctedWeightSchema,
    veterinaryClinicId: optionalUuidSchema,
    referringVeterinarianId: optionalUuidSchema,
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
      values.referralNotes.length > 0 &&
      values.veterinaryClinicId === "" &&
      values.referringVeterinarianId === ""
    ) {
      context.addIssue({
        code: "custom",
        path: ["referralNotes"],
        message:
          "Seleccione una veterinaria o un veterinario referente para conservar notas de referencia.",
      });
    }
  });

export type ReceptionCorrectionFormValues = z.infer<
  typeof receptionCorrectionSchema
>;
