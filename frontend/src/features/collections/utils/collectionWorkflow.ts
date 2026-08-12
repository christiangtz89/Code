import { CollectionStatus, type Collection } from "../types/collection.types";

export function canEditCollection(collection: Collection): boolean {
  return collection.status === CollectionStatus.Collected;
}

export function canCancelCollection(collection: Collection): boolean {
  return collection.status === CollectionStatus.Collected;
}

export function canConvertCollectionToReception(
  collection: Collection,
): boolean {
  return (
    collection.status === CollectionStatus.Collected &&
    collection.receptionId === null
  );
}
