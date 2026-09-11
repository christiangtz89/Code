import { type ChangeEvent, useRef, useState } from "react";

import { LocalCollectionPhotoPreview } from "./LocalCollectionPhotoPreview";

interface CollectionPetPhotoFieldProps {
  file: File | null;
  disabled?: boolean;
  title?: string;
  description?: string;
  onChange: (file: File | null) => void;
}

const MAXIMUM_FILE_SIZE = 10 * 1024 * 1024;

const ALLOWED_CONTENT_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
]);

export function CollectionPetPhotoField({
  file,
  disabled = false,
  title = "Foto de identificación de la mascota",
  description = "Esta fotografía acompañará a la mascota durante la cadena de custodia para ayudar a verificar su identidad en recepción y cremación.",
  onChange,
}: CollectionPetPhotoFieldProps) {
  const cameraInputRef = useRef<HTMLInputElement>(null);

  const galleryInputRef = useRef<HTMLInputElement>(null);

  const [error, setError] = useState<string | null>(null);

  function validateFile(selectedFile: File): string | null {
    if (!ALLOWED_CONTENT_TYPES.has(selectedFile.type)) {
      return "Solo se permiten fotografías JPEG, PNG o WebP.";
    }

    if (selectedFile.size > MAXIMUM_FILE_SIZE) {
      return "La fotografía no puede exceder 10 MB.";
    }

    if (selectedFile.size <= 0) {
      return "La fotografía está vacía.";
    }

    return null;
  }

  function handleFileSelection(event: ChangeEvent<HTMLInputElement>) {
    const selectedFile = event.target.files?.[0] ?? null;

    // Allows selecting the same file again later.
    event.target.value = "";

    if (!selectedFile) {
      return;
    }

    const validationError = validateFile(selectedFile);

    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    onChange(selectedFile);
  }

  function removePhoto() {
    setError(null);
    onChange(null);
  }

  return (
    <div className="space-y-3">
      <div>
        <p className="text-sm font-medium text-slate-700">{title}</p>

        <p className="mt-1 text-xs text-slate-500">{description}</p>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
        {file ? (
          <LocalCollectionPhotoPreview
            file={file}
            alt="Vista previa de la mascota"
            className="aspect-[4/3] w-full object-cover"
          />
        ) : (
          <div className="flex aspect-[4/3] items-center justify-center px-6 text-center">
            <div>
              <p className="text-3xl">📷</p>

              <p className="mt-2 text-sm font-medium text-slate-700">
                Sin fotografía
              </p>

              <p className="mt-1 text-xs text-slate-500">
                Toma una foto clara donde sea fácil identificar a la mascota.
              </p>
            </div>
          </div>
        )}
      </div>

      <input
        ref={cameraInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        capture="environment"
        disabled={disabled}
        onChange={handleFileSelection}
        className="hidden"
      />

      <input
        ref={galleryInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        disabled={disabled}
        onChange={handleFileSelection}
        className="hidden"
      />

      <div className="flex flex-col gap-2 sm:flex-row">
        <button
          type="button"
          disabled={disabled}
          onClick={() => cameraInputRef.current?.click()}
          className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
        >
          📷 Tomar foto
        </button>

        <button
          type="button"
          disabled={disabled}
          onClick={() => galleryInputRef.current?.click()}
          className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          🖼 Seleccionar foto
        </button>

        {file && (
          <button
            type="button"
            disabled={disabled}
            onClick={removePhoto}
            className="rounded-lg border border-red-200 bg-white px-4 py-2.5 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Quitar foto
          </button>
        )}
      </div>

      {file && (
        <div className="rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-600">
          <p className="truncate font-medium">{file.name}</p>

          <p className="mt-1">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
        </div>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      <p className="text-xs text-slate-400">JPEG, PNG o WebP · Máximo 10 MB</p>
    </div>
  );
}
