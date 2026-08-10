import { z } from "zod";
import { PaymentMethod } from "../types/payment.types";

const paymentMethodSchema = z.union([
  z.literal(PaymentMethod.Cash),
  z.literal(PaymentMethod.CreditCard),
  z.literal(PaymentMethod.DebitCard),
  z.literal(PaymentMethod.BankTransfer),
  z.literal(PaymentMethod.Deposit),
  z.literal(PaymentMethod.Other),
]);

export const paymentSchema = z
  .object({
    amount: z
      .number({
        error: "Ingresa el monto del pago.",
      })
      .finite("El monto del pago no es válido.")
      .positive("El monto del pago debe ser mayor que cero.")
      .max(9999999999.99, "El monto del pago excede el máximo permitido."),

    method: paymentMethodSchema,

    paidAt: z.string().min(1, "La fecha del pago es obligatoria."),

    reference: z
      .string()
      .trim()
      .max(150, "La referencia no puede exceder 150 caracteres."),

    notes: z
      .string()
      .trim()
      .max(1000, "Las notas no pueden exceder 1000 caracteres."),
  })
  .superRefine((values, ctx) => {
    if (!values.paidAt) {
      return;
    }

    const paymentDate = new Date(values.paidAt);

    if (Number.isNaN(paymentDate.getTime())) {
      ctx.addIssue({
        code: "custom",
        path: ["paidAt"],
        message: "La fecha del pago no es válida.",
      });

      return;
    }

    if (paymentDate > new Date()) {
      ctx.addIssue({
        code: "custom",
        path: ["paidAt"],
        message: "La fecha del pago no puede estar en el futuro.",
      });
    }
  });

export type PaymentFormValues = z.infer<typeof paymentSchema>;
