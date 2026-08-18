import { z } from "zod";

export const urnSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "El nombre es obligatorio.")
    .max(150, "El nombre no puede exceder 150 caracteres."),

  description: z
    .string()
    .trim()
    .max(1000, "La descripción no puede exceder 1000 caracteres.")
    .nullable(),

  price: z
    .number()
    .min(0, "El precio no puede ser negativo.")
    .multipleOf(0.01, "El precio no puede tener más de dos decimales.")
    .max(9999999999.99, "El precio excede el máximo permitido."),

  material: z
    .string()
    .trim()
    .max(100, "El material no puede exceder 100 caracteres.")
    .nullable(),

  color: z
    .string()
    .trim()
    .max(100, "El color no puede exceder 100 caracteres.")
    .nullable(),

  imageUrl: z.string().trim().max(500).nullable(),

  isPublic: z.boolean(),

  displayOrder: z
    .number()
    .int()
    .min(0, "El orden de visualización no puede ser negativo."),

  isActive: z.boolean(),
});

export type UrnFormValues = z.infer<typeof urnSchema>;
