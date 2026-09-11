import { CollectionStatus, type Collection } from "../types/collection.types";

export function canEditCollection(collection: Collection): boolean {
  return (
    collection.status === CollectionStatus.Pending ||
    collection.status === CollectionStatus.Assigned ||
    collection.status === CollectionStatus.Accepted ||
    collection.status === CollectionStatus.Collected
  );
}

export function canCancelCollection(collection: Collection): boolean {
  return canEditCollection(collection);
}

export function canConvertCollectionToReception(
  collection: Collection,
): boolean {
  return (
    collection.status === CollectionStatus.Collected &&
    collection.receptionId === null
  );
}
