import { apiClient } from "../../../services/apiClient";
import type {
  CollectionPhoto,
  CollectionPhotoType,
} from "../types/collectionPhoto.types";

const COLLECTION_PHOTOS_URL = "/CollectionPhotos";

export async function getCollectionPhotos(
  collectionId: string,
): Promise<CollectionPhoto[]> {
  const response = await apiClient.get<CollectionPhoto[]>(
    `${COLLECTION_PHOTOS_URL}/collection/${collectionId}`,
  );

  return response.data;
}

export async function uploadCollectionPhoto(
  collectionId: string,
  file: File,
  photoType: CollectionPhotoType,
  notes?: string,
): Promise<CollectionPhoto> {
  const formData = new FormData();

  formData.append("file", file);
  formData.append("photoType", String(photoType));

  if (notes?.trim()) {
    formData.append("notes", notes.trim());
  }

  const response = await apiClient.postForm<CollectionPhoto>(
    `${COLLECTION_PHOTOS_URL}/collection/${collectionId}`,
    formData,
  );

  return response.data;
}

export async function getCollectionPhotoFile(photoId: string): Promise<Blob> {
  const response = await apiClient.get<Blob>(
    `${COLLECTION_PHOTOS_URL}/${photoId}/file`,
    {
      responseType: "blob",
    },
  );

  return response.data;
}

export async function deactivateCollectionPhoto(
  photoId: string,
): Promise<void> {
  await apiClient.delete(`${COLLECTION_PHOTOS_URL}/${photoId}`);
}

export async function restoreCollectionPhoto(photoId: string): Promise<void> {
  await apiClient.patch(`${COLLECTION_PHOTOS_URL}/${photoId}/restore`);
}
