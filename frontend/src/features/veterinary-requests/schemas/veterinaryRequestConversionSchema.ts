import { z } from "zod";

const optionalIdSchema = z
  .string()
  .trim()
  .refine(
    (value) => value === "" || z.string().uuid().safeParse(value).success,
    "La selección no es válida.",
  );

export const veterinaryRequestConversionSchema = z
  .object({
    existingCustomerId: optionalIdSchema,

    existingPetId: optionalIdSchema,

    verifiedWeightKg: z
      .number()
      .finite()
      .positive("El peso verificado debe ser mayor que cero.")
      .max(999.99, "El peso no puede exceder 999.99 kg."),

    hasPersonalBelongings: z.boolean(),

    personalBelongingsDescription: z
      .string()
      .trim()
      .max(500, "La descripción no puede exceder 500 caracteres."),

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
    if (values.hasPersonalBelongings && !values.personalBelongingsDescription) {
      context.addIssue({
        code: "custom",
        path: ["personalBelongingsDescription"],
        message: "Describe los objetos personales recibidos.",
      });
    }
  });

export type VeterinaryRequestConversionFormValues = z.infer<
  typeof veterinaryRequestConversionSchema
>;
