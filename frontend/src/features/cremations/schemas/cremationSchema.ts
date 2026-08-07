import { z } from "zod";
import { CremationType } from "../types/cremation.types";

export const cremationSchema = z
  .object({
    receptionId: z.string().uuid("Selecciona una recepción válida."),

    assignedToUserId: z
      .string()
      .refine(
        (value) => value === "" || z.string().uuid().safeParse(value).success,
        "Selecciona un usuario válido.",
      ),

    cremationType: z.union([
      z.literal(CremationType.Individual),
      z.literal(CremationType.Communal),
    ]),

    packageName: z
      .string()
      .trim()
      .min(1, "El nombre del paquete es obligatorio.")
      .max(150, "El nombre del paquete no puede exceder 150 caracteres."),

    includesUrn: z.boolean(),

    urnDescription: z
      .string()
      .trim()
      .max(500, "La descripción de la urna no puede exceder 500 caracteres."),

    includesPawPrint: z.boolean(),

    includesCertificate: z.boolean(),

    scheduledAt: z.string(),

    specialInstructions: z
      .string()
      .trim()
      .max(
        1000,
        "Las instrucciones especiales no pueden exceder 1000 caracteres.",
      ),

    notes: z
      .string()
      .trim()
      .max(1000, "Las notas no pueden exceder 1000 caracteres."),
  })
  .superRefine((values, context) => {
    if (values.includesUrn && !values.urnDescription.trim()) {
      context.addIssue({
        code: "custom",
        path: ["urnDescription"],
        message: "Describe la urna incluida en el servicio.",
      });
    }
  });

export type CremationFormValues = z.infer<typeof cremationSchema>;
