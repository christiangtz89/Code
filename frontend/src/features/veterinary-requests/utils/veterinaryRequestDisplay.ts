const dateTimeFormatter = new Intl.DateTimeFormat("es-MX", {
  dateStyle: "medium",
  timeStyle: "short",
});

const dateFormatter = new Intl.DateTimeFormat("es-MX", {
  dateStyle: "medium",
  timeZone: "UTC",
});

export function formatVeterinaryRequestDateTime(value: string | null): string {
  if (!value) {
    return "No registrado";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no válida";
  }

  return dateTimeFormatter.format(date);
}

export function formatVeterinaryRequestDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no válida";
  }

  return dateFormatter.format(date);
}
