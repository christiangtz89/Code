import { z } from "zod";

import { WEIGHT_PRICING_INTERVAL } from "../types";

import { CremationType } from "../../cremations/types/cremation.types";

export const cremationPriceSchema = z
  .object({
    cremationPackageId: z.string().uuid("Selecciona un paquete válido."),

    cremationType: z.union([
      z.literal(CremationType.Individual),
      z.literal(CremationType.Communal),
    ]),

    minimumWeightKg: z
      .number()
      .min(0, "El peso mínimo no puede ser negativo.")
      .multipleOf(0.01, "El peso mínimo no puede tener más de dos decimales.")
      .max(100, "El peso mínimo no puede exceder 100 kg."),

    maximumWeightKg: z
      .number()
      .positive("El peso máximo debe ser mayor que cero.")
      .multipleOf(0.01, "El peso máximo no puede tener más de dos decimales.")
      .max(100, "El peso máximo no puede exceder 100 kg."),

    price: z
      .number()
      .min(0.01, "El precio debe ser mayor que cero.")
      .multipleOf(0.01, "El precio no puede tener más de dos decimales.")
      .max(9999999999.99, "El precio excede el máximo permitido."),

    isPublic: z.boolean(),

    isActive: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (data.maximumWeightKg <= data.minimumWeightKg) {
      ctx.addIssue({
        code: "custom",
        path: ["maximumWeightKg"],
        message: "El peso máximo debe ser mayor al peso mínimo.",
      });
    }
  });

export type CremationPriceFormValues = z.infer<typeof cremationPriceSchema>;

export const cremationPricingConfigurationSchema = z.object({
  weightInterval: z.union([
    z.literal(WEIGHT_PRICING_INTERVAL.FIVE_KG),
    z.literal(WEIGHT_PRICING_INTERVAL.TEN_KG),
  ]),

  allowIndividualNoAshes: z.boolean(),
});

export type CremationPricingConfigurationFormValues = z.infer<
  typeof cremationPricingConfigurationSchema
>;
