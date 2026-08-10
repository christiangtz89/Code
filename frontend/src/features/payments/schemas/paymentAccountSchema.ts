import { z } from "zod";

const serviceTotalSchema = z
  .number({
    error: "Ingresa el total del servicio.",
  })
  .finite("El total del servicio no es válido.")
  .positive("El total del servicio debe ser mayor que cero.")
  .max(9999999999.99, "El total del servicio excede el máximo permitido.");

export const createPaymentAccountSchema = z.object({
  cremationId: z
    .string()
    .min(1, "Selecciona una cremación.")
    .uuid("Selecciona una cremación válida."),

  serviceTotal: serviceTotalSchema,
});

export const updatePaymentAccountSchema = z.object({
  serviceTotal: serviceTotalSchema,
});

export type CreatePaymentAccountFormValues = z.infer<
  typeof createPaymentAccountSchema
>;

export type UpdatePaymentAccountFormValues = z.infer<
  typeof updatePaymentAccountSchema
>;
