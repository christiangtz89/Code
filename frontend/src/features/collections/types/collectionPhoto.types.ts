export const CollectionPhotoType = {
  PetIdentification: 1,
  PersonalBelongings: 2,
  PickupEvidence: 3,
  Other: 4,
} as const;

export type CollectionPhotoType =
  (typeof CollectionPhotoType)[keyof typeof CollectionPhotoType];

export interface CollectionPhoto {
  id: string;
  collectionId: string;

  uploadedByUserId: string;
  uploadedByUserName: string;

  photoType: CollectionPhotoType;

  originalFileName: string;
  contentType: string;

  fileUrl: string;

  notes: string | null;

  isActive: boolean;
  uploadedAt: string;
}
