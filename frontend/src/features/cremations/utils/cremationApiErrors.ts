type CremationApiError = {
  message?: unknown;
  detail?: unknown;
  title?: unknown;
  errors?: Record<string, unknown>;
};

export function getCremationApiErrorMessage(
  data: unknown,
  fallback: string,
): string {
  if (typeof data === "string") return data.trim() || fallback;
  if (!data || typeof data !== "object") return fallback;

  const response = data as CremationApiError;
  if (typeof response.message === "string" && response.message.trim())
    return response.message;
  if (typeof response.detail === "string" && response.detail.trim())
    return response.detail;

  if (response.errors && typeof response.errors === "object") {
    for (const messages of Object.values(response.errors)) {
      if (!Array.isArray(messages)) continue;
      const first = messages.find(
        (message): message is string =>
          typeof message === "string" && Boolean(message.trim()),
      );
      if (first) return first;
    }
    return fallback;
  }

  return typeof response.title === "string" && response.title.trim()
    ? response.title
    : fallback;
}
