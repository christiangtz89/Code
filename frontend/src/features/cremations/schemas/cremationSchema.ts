import { z } from "zod";

export const cremationSchema = z
  .object({
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
      .max(
        500,
        "La descripción del accesorio no puede exceder 500 caracteres.",
      ),

    scheduledDate: z.string(),

    scheduledTime: z.string(),

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
  .superRefine((data, ctx) => {
    const hasDate = data.scheduledDate.length > 0;
    const hasTime = data.scheduledTime.length > 0;

    if (hasDate && !hasTime) {
      ctx.addIssue({
        code: "custom",
        path: ["scheduledTime"],
        message: "Selecciona una hora programada.",
      });
    }

    if (!hasDate && hasTime) {
      ctx.addIssue({
        code: "custom",
        path: ["scheduledDate"],
        message: "Selecciona una fecha programada.",
      });
    }

    if (hasTime && !/^(?:[01]\d|2[0-3]):(?:00|30)$/.test(data.scheduledTime)) {
      ctx.addIssue({
        code: "custom",
        path: ["scheduledTime"],
        message: "La hora programada debe estar en intervalos de 30 minutos.",
      });
    }
  });

export type CremationFormValues = z.infer<typeof cremationSchema>;
