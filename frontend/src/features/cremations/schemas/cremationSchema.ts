import { z } from "zod";

export const cremationSchema = z.object({
  receptionId: z.string().uuid("Selecciona una recepción válida."),

  assignedToUserId: z
    .string()
    .refine(
      (value) => value === "" || z.string().uuid().safeParse(value).success,
      "Selecciona un usuario válido.",
    ),

  cremationPackageId: z
    .string()
    .uuid("Selecciona un paquete o servicio válido."),

  urnId: z
    .string()
    .refine(
      (value) => value === "" || z.string().uuid().safeParse(value).success,
      "Selecciona una urna válida.",
    ),

  accessoryDescription: z
    .string()
    .trim()
    .max(500, "La descripción del accesorio no puede exceder 500 caracteres."),

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
});

export type CremationFormValues = z.infer<typeof cremationSchema>;
