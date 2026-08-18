import { z } from "zod";

export const createPaymentAccountSchema = z.object({
  cremationId: z.string().uuid("Selecciona una cremación válida."),
});

export type CreatePaymentAccountFormValues = z.infer<
  typeof createPaymentAccountSchema
>;
