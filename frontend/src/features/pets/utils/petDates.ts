export function getLocalDateInputValue(
  date = new Date(),
): string {
  const timezoneOffset =
    date.getTimezoneOffset() * 60_000

  return new Date(date.getTime() - timezoneOffset)
    .toISOString()
    .slice(0, 10)
}

export function toApiDateTime(
  dateValue: string,
): string {
  return new Date(
    `${dateValue}T12:00:00`,
  ).toISOString()
}

export function toDateInputValue(
  value: string,
): string {
  return value.slice(0, 10)
}