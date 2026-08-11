import { z } from "zod";

export const veterinaryRequestStatusSchema = z
  .object({
    status: z.union([z.literal(2), z.literal(3), z.literal(4), z.literal(6)]),

    internalNotes: z
      .string()
      .trim()
      .max(1000, "Las notas internas no pueden exceder 1000 caracteres."),

    rejectionReason: z
      .string()
      .trim()
      .max(1000, "El motivo de rechazo no puede exceder 1000 caracteres."),
  })
  .superRefine((values, context) => {
    if (values.status === 4 && !values.rejectionReason) {
      context.addIssue({
        code: "custom",
        path: ["rejectionReason"],
        message: "El motivo de rechazo es obligatorio.",
      });
    }
  });

export type VeterinaryRequestStatusFormValues = z.infer<
  typeof veterinaryRequestStatusSchema
>;
