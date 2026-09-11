import { z } from "zod";

export const createPaymentAccountSchema = z.object({
  sourceType: z.enum(["collection", "cremation"]),
  selectionId: z.string().min(1, "Selecciona un servicio válido."),
});

export type CreatePaymentAccountFormValues = z.infer<
  typeof createPaymentAccountSchema
>;
