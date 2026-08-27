import { useEffect, useState } from "react";

interface LocalCollectionPhotoPreviewProps {
  file: File;
  alt: string;
  className?: string;
}

export function LocalCollectionPhotoPreview({
  file,
  alt,
  className,
}: LocalCollectionPhotoPreviewProps) {
  const [objectUrl, setObjectUrl] = useState<string>("");

  useEffect(() => {
    const nextObjectUrl = URL.createObjectURL(file);

    setObjectUrl(nextObjectUrl);

    return () => {
      URL.revokeObjectURL(nextObjectUrl);
    };
  }, [file]);

  if (!objectUrl) {
    return null;
  }

  return <img src={objectUrl} alt={alt} className={className} />;
}
