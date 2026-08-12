const dateTimeFormatter = new Intl.DateTimeFormat("es-MX", {
  dateStyle: "medium",
  timeStyle: "short",
});

export function formatCollectionDateTime(value: string | null): string {
  if (!value) {
    return "No registrado";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Fecha no válida";
  }

  return dateTimeFormatter.format(date);
}

export function formatCollectionWeight(value: number | null): string {
  if (value === null) {
    return "No registrado";
  }

  return `${value.toLocaleString("es-MX", {
    maximumFractionDigits: 2,
  })} kg`;
}
