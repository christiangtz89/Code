export function formatReceptionDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no disponible";
  }

  return new Intl.DateTimeFormat("es-MX", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function formatReceptionWeight(weightKg: number): string {
  return new Intl.NumberFormat("es-MX", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(weightKg);
}

export function showOptionalReceptionValue(
  value: string | null | undefined,
  fallback = "No registrado",
): string {
  return value?.trim() || fallback;
}
