import { z } from "zod";

const optionalPhoneSchema = z
  .string()
  .trim()
  .max(30, "El teléfono no puede exceder 30 caracteres.")
  .refine(
    (value) => value === "" || /^[0-9+\-\s()]+$/.test(value),
    "El teléfono contiene caracteres no válidos.",
  );

export const collectionUpdateSchema = z
  .object({
    locationType: z.union([z.literal("1"), z.literal("2")]),

    veterinaryClinicId: z.string(),

    referringVeterinarianId: z.string(),

    pickupAddress: z
      .string()
      .trim()
      .min(1, "La dirección de recolección es obligatoria.")
      .max(500, "La dirección no puede exceder 500 caracteres."),

    pickupContactName: z.string().trim().max(150),

    pickupContactPhone: optionalPhoneSchema,

    approximateWeightKg: z
      .string()
      .trim()
      .refine((value) => {
        if (value === "") {
          return true;
        }

        const numberValue = Number(value);

        return (
          Number.isFinite(numberValue) &&
          numberValue > 0 &&
          numberValue <= 999.99
        );
      }, "El peso debe ser mayor que cero y no exceder 999.99 kg."),

    hasPersonalBelongings: z.boolean(),

    personalBelongingsDescription: z.string().trim().max(500),

    notes: z.string().trim().max(1000),
  })
  .superRefine((values, context) => {
    if (
      values.locationType === "1" &&
      (values.veterinaryClinicId || values.referringVeterinarianId)
    ) {
      context.addIssue({
        code: "custom",
        path: ["veterinaryClinicId"],
        message:
          "Una recolección en domicilio no debe tener referencia veterinaria.",
      });
    }

    if (
      values.locationType === "2" &&
      !values.veterinaryClinicId &&
      !values.referringVeterinarianId
    ) {
      context.addIssue({
        code: "custom",
        path: ["veterinaryClinicId"],
        message: "Selecciona una veterinaria o un veterinario.",
      });
    }

    if (values.hasPersonalBelongings && !values.personalBelongingsDescription) {
      context.addIssue({
        code: "custom",
        path: ["personalBelongingsDescription"],
        message: "Describe los objetos personales recibidos.",
      });
    }
  });

export type CollectionUpdateFormValues = z.infer<typeof collectionUpdateSchema>;
