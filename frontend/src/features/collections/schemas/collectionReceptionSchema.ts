import { z } from "zod";

export const collectionReceptionSchema = z
  .object({
    verifiedWeightKg: z
      .string()
      .trim()
      .min(1, "El peso verificado es obligatorio.")
      .refine((value) => {
        const numberValue = Number(value);

        return (
          Number.isFinite(numberValue) &&
          numberValue > 0 &&
          numberValue <= 999.99
        );
      }, "El peso verificado debe ser mayor que cero."),

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

export type CollectionReceptionFormValues = z.infer<
  typeof collectionReceptionSchema
>;
