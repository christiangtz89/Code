import type { Veterinarian } from "../types/veterinarian.types";

type VeterinarianNameFields = Pick<
  Veterinarian,
  "firstName" | "lastName" | "secondLastName"
>;

export function getVeterinarianFullName(
  veterinarian: VeterinarianNameFields,
): string {
  return [
    veterinarian.firstName,
    veterinarian.lastName,
    veterinarian.secondLastName,
  ]
    .map((value) => value?.trim())
    .filter((value): value is string => Boolean(value))
    .join(" ");
}
