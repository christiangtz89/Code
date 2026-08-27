import { useEffect, useState } from "react";

import { getCollectionPhotoFile } from "../api/collectionPhotosApi";

interface AuthenticatedCollectionPhotoProps {
  photoId: string;
  alt: string;
  className?: string;
}

export function AuthenticatedCollectionPhoto({
  photoId,
  alt,
  className,
}: AuthenticatedCollectionPhotoProps) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  const [isLoading, setIsLoading] = useState(true);

  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let active = true;
    let createdObjectUrl: string | null = null;

    async function loadPhoto() {
      setIsLoading(true);
      setHasError(false);

      try {
        const blob = await getCollectionPhotoFile(photoId);

        if (!active) {
          return;
        }

        createdObjectUrl = URL.createObjectURL(blob);

        setObjectUrl(createdObjectUrl);
      } catch {
        if (active) {
          setHasError(true);
          setObjectUrl(null);
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    void loadPhoto();

    return () => {
      active = false;

      if (createdObjectUrl) {
        URL.revokeObjectURL(createdObjectUrl);
      }
    };
  }, [photoId]);

  if (isLoading) {
    return (
      <div
        className={[
          "flex items-center justify-center bg-slate-100 text-sm text-slate-400",
          className ?? "",
        ].join(" ")}
      >
        Cargando foto...
      </div>
    );
  }

  if (hasError || !objectUrl) {
    return (
      <div
        className={[
          "flex items-center justify-center bg-slate-100 px-4 text-center text-sm text-slate-500",
          className ?? "",
        ].join(" ")}
      >
        No fue posible cargar la fotografía.
      </div>
    );
  }

  return <img src={objectUrl} alt={alt} className={className} />;
}
