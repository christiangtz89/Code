import {
  CollectionLocationType,
  CollectionStatus,
  type CollectionLocationType as CollectionLocationTypeValue,
  type CollectionStatus as CollectionStatusValue,
} from "../types/collection.types";

export function getCollectionStatusLabel(
  status: CollectionStatusValue,
): string {
  switch (status) {
    case CollectionStatus.Collected:
      return "Recolectada";

    case CollectionStatus.Received:
      return "Recibida en instalaciones";

    case CollectionStatus.Cancelled:
      return "Cancelada";

    default:
      return "Estado desconocido";
  }
}

export function getCollectionLocationLabel(
  locationType: CollectionLocationTypeValue,
): string {
  switch (locationType) {
    case CollectionLocationType.CustomerHome:
      return "Domicilio del cliente";

    case CollectionLocationType.VeterinaryLocation:
      return "Veterinaria";

    default:
      return "Ubicación desconocida";
  }
}
