import { z } from "zod";

import { CREMATION_PACKAGE_TYPE } from "../types";

export const cremationPackageSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "El nombre es obligatorio.")
      .max(150, "El nombre no puede exceder 150 caracteres."),

    shortDescription: z
      .string()
      .trim()
      .max(250, "La descripción corta no puede exceder 250 caracteres.")
      .nullable(),

    description: z
      .string()
      .trim()
      .max(1000, "La descripción no puede exceder 1000 caracteres.")
      .nullable(),

    packageType: z.union([
      z.literal(CREMATION_PACKAGE_TYPE.ASHES_RETURN),
      z.literal(CREMATION_PACKAGE_TYPE.NO_ASHES),
    ]),

    tier: z.number().int().min(1).max(4).nullable(),

    includesUrn: z.boolean(),

    allowedUrnIds: z.array(z.string().uuid("Selecciona una urna válida.")),

    includesPawPrint: z.boolean(),

    accessoryDescription: z
      .string()
      .trim()
      .max(500, "La descripción del accesorio no puede exceder 500 caracteres.")
      .nullable(),

    includesCertificate: z.boolean(),

    imageUrl: z.string().trim().max(500).nullable(),

    isPublic: z.boolean(),

    displayOrder: z
      .number()
      .int()
      .min(0, "El orden de visualización no puede ser negativo."),

    isActive: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (new Set(data.allowedUrnIds).size !== data.allowedUrnIds.length) {
      ctx.addIssue({
        code: "custom",
        path: ["allowedUrnIds"],
        message: "No puedes seleccionar la misma urna más de una vez.",
      });
    }

    if (data.includesUrn && data.allowedUrnIds.length === 0) {
      ctx.addIssue({
        code: "custom",
        path: ["allowedUrnIds"],
        message: "Selecciona al menos una urna permitida para el paquete.",
      });
    }

    if (!data.includesUrn && data.allowedUrnIds.length > 0) {
      ctx.addIssue({
        code: "custom",
        path: ["allowedUrnIds"],
        message: "Un paquete sin urna no puede tener urnas permitidas.",
      });
    }

    if (data.packageType === CREMATION_PACKAGE_TYPE.ASHES_RETURN) {
      if (data.tier === null) {
        ctx.addIssue({
          code: "custom",
          path: ["tier"],
          message:
            "Los paquetes con devolución de cenizas deben tener un nivel.",
        });
      }

      return;
    }

    if (data.tier !== null) {
      ctx.addIssue({
        code: "custom",
        path: ["tier"],
        message: "La cremación sin devolución de cenizas no debe tener nivel.",
      });
    }

    if (data.includesUrn) {
      ctx.addIssue({
        code: "custom",
        path: ["includesUrn"],
        message:
          "La cremación sin devolución de cenizas no puede incluir urna.",
      });
    }

    if (data.includesPawPrint) {
      ctx.addIssue({
        code: "custom",
        path: ["includesPawPrint"],
        message:
          "La cremación sin devolución de cenizas no puede incluir accesorio.",
      });
    }

    if (data.accessoryDescription) {
      ctx.addIssue({
        code: "custom",
        path: ["accessoryDescription"],
        message:
          "La cremación sin devolución de cenizas no puede tener accesorio.",
      });
    }

    if (data.includesCertificate) {
      ctx.addIssue({
        code: "custom",
        path: ["includesCertificate"],
        message:
          "La cremación sin devolución de cenizas no puede incluir certificado.",
      });
    }
  });

export type CremationPackageFormValues = z.infer<typeof cremationPackageSchema>;
