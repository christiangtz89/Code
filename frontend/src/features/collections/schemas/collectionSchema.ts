import { z } from "zod";

function isUuid(value: string): boolean {
  return z.string().uuid().safeParse(value).success;
}

function getTodayLocalDate(): string {
  const now = new Date();

  const offset = now.getTimezoneOffset() * 60_000;

  return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}

const optionalPhoneSchema = z
  .string()
  .trim()
  .max(30, "El teléfono no puede exceder 30 caracteres.")
  .refine(
    (value) => value === "" || /^[0-9+\-\s()]+$/.test(value),
    "El teléfono contiene caracteres no válidos.",
  );

const optionalEmailSchema = z
  .string()
  .trim()
  .max(150, "El correo no puede exceder 150 caracteres.")
  .refine(
    (value) => value === "" || z.string().email().safeParse(value).success,
    "Ingresa un correo electrónico válido.",
  );

const optionalWeightSchema = z
  .string()
  .trim()
  .refine((value) => {
    if (value === "") {
      return true;
    }

    const numberValue = Number(value);

    return (
      Number.isFinite(numberValue) && numberValue > 0 && numberValue <= 999.99
    );
  }, "El peso debe ser mayor que cero y no exceder 999.99 kg.");

const optionalAgeSchema = z
  .string()
  .trim()
  .refine((value) => {
    if (value === "") {
      return true;
    }

    const numberValue = Number(value);

    return (
      Number.isInteger(numberValue) && numberValue >= 0 && numberValue <= 100
    );
  }, "La edad debe estar entre 0 y 100 años.");

export const collectionSchema = z
  .object({
    customerMode: z.union([z.literal("new"), z.literal("existing")]),

    petMode: z.union([z.literal("new"), z.literal("existing")]),

    existingCustomerId: z.string(),

    existingPetId: z.string(),

    ownerFirstName: z
      .string()
      .trim()
      .max(100, "El nombre no puede exceder 100 caracteres."),

    ownerLastName: z
      .string()
      .trim()
      .max(100, "El apellido paterno no puede exceder 100 caracteres."),

    ownerSecondLastName: z
      .string()
      .trim()
      .max(100, "El apellido materno no puede exceder 100 caracteres."),

    ownerPhone: optionalPhoneSchema,

    ownerEmail: optionalEmailSchema,

    petName: z
      .string()
      .trim()
      .max(100, "El nombre de la mascota no puede exceder 100 caracteres."),

    species: z
      .string()
      .trim()
      .max(50, "La especie no puede exceder 50 caracteres."),

    breed: z
      .string()
      .trim()
      .max(100, "La raza no puede exceder 100 caracteres."),

    sex: z.string().trim().max(20, "El sexo no puede exceder 20 caracteres."),

    color: z
      .string()
      .trim()
      .max(100, "El color no puede exceder 100 caracteres."),

    approximateWeightKg: optionalWeightSchema,

    ageYears: optionalAgeSchema,

    dateOfDeath: z.string(),

    locationType: z.union([z.literal("1"), z.literal("2")]),

    veterinaryClinicId: z.string(),

    referringVeterinarianId: z.string(),

    pickupAddress: z
      .string()
      .trim()
      .min(1, "La dirección de recolección es obligatoria.")
      .max(500, "La dirección no puede exceder 500 caracteres."),

    pickupContactName: z
      .string()
      .trim()
      .max(150, "El nombre del contacto no puede exceder 150 caracteres."),

    pickupContactPhone: optionalPhoneSchema,

    hasPersonalBelongings: z.boolean(),

    personalBelongingsDescription: z
      .string()
      .trim()
      .max(500, "La descripción no puede exceder 500 caracteres."),

    notes: z
      .string()
      .trim()
      .max(1000, "Las notas no pueden exceder 1000 caracteres."),
  })
  .superRefine((values, context) => {
    if (values.customerMode === "new") {
      if (values.petMode === "existing") {
        context.addIssue({
          code: "custom",
          path: ["petMode"],
          message: "Un cliente nuevo debe registrarse con una mascota nueva.",
        });
      }

      if (!values.ownerFirstName) {
        context.addIssue({
          code: "custom",
          path: ["ownerFirstName"],
          message: "El nombre del propietario es obligatorio.",
        });
      }

      if (!values.ownerLastName) {
        context.addIssue({
          code: "custom",
          path: ["ownerLastName"],
          message: "El apellido paterno es obligatorio.",
        });
      }

      if (!values.ownerPhone) {
        context.addIssue({
          code: "custom",
          path: ["ownerPhone"],
          message: "El teléfono del propietario es obligatorio.",
        });
      }

      if (!values.ownerEmail) {
        context.addIssue({
          code: "custom",
          path: ["ownerEmail"],
          message:
            "El correo electrónico es obligatorio para crear un cliente.",
        });
      }
    } else {
      if (!isUuid(values.existingCustomerId)) {
        context.addIssue({
          code: "custom",
          path: ["existingCustomerId"],
          message: "Selecciona un cliente existente.",
        });
      }
    }

    if (values.petMode === "new") {
      if (!values.petName) {
        context.addIssue({
          code: "custom",
          path: ["petName"],
          message: "El nombre de la mascota es obligatorio.",
        });
      }

      if (!values.species) {
        context.addIssue({
          code: "custom",
          path: ["species"],
          message: "La especie es obligatoria.",
        });
      }

      if (!values.breed) {
        context.addIssue({
          code: "custom",
          path: ["breed"],
          message: "La raza es obligatoria.",
        });
      }

      if (!values.sex) {
        context.addIssue({
          code: "custom",
          path: ["sex"],
          message: "El sexo es obligatorio.",
        });
      }

      if (!values.color) {
        context.addIssue({
          code: "custom",
          path: ["color"],
          message: "El color es obligatorio.",
        });
      }

      if (!values.approximateWeightKg) {
        context.addIssue({
          code: "custom",
          path: ["approximateWeightKg"],
          message: "El peso aproximado es obligatorio.",
        });
      }

      if (!values.dateOfDeath) {
        context.addIssue({
          code: "custom",
          path: ["dateOfDeath"],
          message: "La fecha de fallecimiento es obligatoria.",
        });
      } else if (values.dateOfDeath > getTodayLocalDate()) {
        context.addIssue({
          code: "custom",
          path: ["dateOfDeath"],
          message: "La fecha de fallecimiento no puede estar en el futuro.",
        });
      }
    } else if (!isUuid(values.existingPetId)) {
      context.addIssue({
        code: "custom",
        path: ["existingPetId"],
        message: "Selecciona una mascota existente.",
      });
    }

    if (values.locationType === "1") {
      if (values.veterinaryClinicId || values.referringVeterinarianId) {
        context.addIssue({
          code: "custom",
          path: ["veterinaryClinicId"],
          message:
            "Una recolección en domicilio no debe tener referencia veterinaria.",
        });
      }
    }

    if (
      values.locationType === "2" &&
      !values.veterinaryClinicId &&
      !values.referringVeterinarianId
    ) {
      context.addIssue({
        code: "custom",
        path: ["veterinaryClinicId"],
        message: "Selecciona una veterinaria o un veterinario.",
      });
    }

    if (values.hasPersonalBelongings && !values.personalBelongingsDescription) {
      context.addIssue({
        code: "custom",
        path: ["personalBelongingsDescription"],
        message: "Describe los objetos personales recibidos.",
      });
    }
  });

export type CollectionFormValues = z.infer<typeof collectionSchema>;
