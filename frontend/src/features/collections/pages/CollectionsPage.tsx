import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import axios from "axios";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";

import {
  changeCollectionStatus,
  convertCollectionToReception,
  createCollection,
  getCollections,
  searchCollections,
  updateCollection,
} from "../api/collectionsApi";

import { uploadCollectionPhoto } from "../api/collectionPhotosApi";

import { CollectionEditModal } from "../components/CollectionEditModal";
import { CollectionFormModal } from "../components/CollectionFormModal";
import { CollectionQrModal } from "../components/CollectionQrModal";
import { CollectionsTable } from "../components/CollectionsTable";
import { CollectionToReceptionModal } from "../components/CollectionToReceptionModal";

import type { CollectionReceptionFormValues } from "../schemas/collectionReceptionSchema";
import type { CollectionFormValues } from "../schemas/collectionSchema";
import type { CollectionUpdateFormValues } from "../schemas/collectionUpdateSchema";

import {
  CollectionLocationType,
  CollectionStatus,
  type Collection,
  type CollectionLocationType as CollectionLocationTypeValue,
  type CollectionStatus as CollectionStatusValue,
  type ConvertCollectionToReceptionPayload,
  type PagedCollections,
  type UpdateCollectionPayload,
} from "../types/collection.types";

import { CollectionPhotoType } from "../types/collectionPhoto.types";

import {
  collectionReceptionPayload,
  createCollectionPayload,
  updateCollectionPayload,
} from "../utils/collectionPayload";

interface ApiErrorResponse {
  title?: string;
  detail?: string;
  message?: string;
}

function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) {
    return fallback;
  }

  if (!error.response) {
    return "No fue posible conectarse con el servidor.";
  }

  const data = error.response.data as ApiErrorResponse | string | undefined;

  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object") {
    return data.detail ?? data.message ?? data.title ?? fallback;
  }

  return fallback;
}

function parseStatusFilter(value: string): CollectionStatusValue | undefined {
  if (value === "") {
    return undefined;
  }

  const numberValue = Number(value);

  if (
    numberValue === CollectionStatus.Collected ||
    numberValue === CollectionStatus.Received ||
    numberValue === CollectionStatus.Cancelled
  ) {
    return numberValue;
  }

  return undefined;
}

function parseLocationFilter(
  value: string,
): CollectionLocationTypeValue | undefined {
  if (value === "") {
    return undefined;
  }

  const numberValue = Number(value);

  if (
    numberValue === CollectionLocationType.CustomerHome ||
    numberValue === CollectionLocationType.VeterinaryLocation
  ) {
    return numberValue;
  }

  return undefined;
}

export function CollectionsPage() {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);

  const [pageSize, setPageSize] = useState(10);

  const [searchInput, setSearchInput] = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [statusFilter, setStatusFilter] = useState("");

  const [locationFilter, setLocationFilter] = useState("");

  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const [editCollection, setEditCollection] = useState<Collection | null>(null);

  const [receiveCollection, setReceiveCollection] = useState<Collection | null>(
    null,
  );

  const [qrCollection, setQrCollection] = useState<Collection | null>(null);

  const normalizedSearch = debouncedSearch.trim();

  const selectedStatus = parseStatusFilter(statusFilter);

  const selectedLocationType = parseLocationFilter(locationFilter);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput);
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [normalizedSearch, pageSize, statusFilter, locationFilter]);

  const collectionsQuery = useQuery({
    queryKey: [
      "collections",
      {
        page,
        pageSize,
        search: normalizedSearch,
        status: selectedStatus ?? null,
        locationType: selectedLocationType ?? null,
      },
    ],

    queryFn: async (): Promise<PagedCollections> => {
      /*
       * Search endpoint returns
       * an array, so pagination
       * is done client-side only
       * for search results.
       */
      if (normalizedSearch) {
        const found = await searchCollections({
          search: normalizedSearch,

          status: selectedStatus,

          locationType: selectedLocationType,
        });

        const totalItems = found.length;

        const totalPages =
          totalItems > 0 ? Math.ceil(totalItems / pageSize) : 0;

        const startIndex = (page - 1) * pageSize;

        return {
          items: found.slice(startIndex, startIndex + pageSize),

          page,

          pageSize,

          totalItems,

          totalPages,
        };
      }

      return getCollections({
        page,

        pageSize,

        status: selectedStatus,

        locationType: selectedLocationType,
      });
    },

    placeholderData: keepPreviousData,
  });

  async function refreshCollections() {
    await queryClient.invalidateQueries({
      queryKey: ["collections"],
    });
  }

  /*
   * CREATE
   *
   * Creating a Collection may also
   * create a Customer and/or Pet.
   */
  const createMutation = useMutation({
    mutationFn: createCollection,

    onSuccess: async () => {
      await Promise.all([
        refreshCollections(),

        queryClient.invalidateQueries({
          queryKey: ["customers"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["pets"],
        }),
      ]);

      toast.success("Recolección registrada correctamente.");
    },
  });

  /*
   * UPDATE
   */
  const updateMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: UpdateCollectionPayload;
    }) => updateCollection(id, payload),

    onSuccess: async () => {
      await refreshCollections();

      toast.success("Recolección actualizada correctamente.");
    },
  });

  /*
   * CONVERT TO RECEPTION
   *
   * This creates a Reception,
   * therefore both query families
   * must be refreshed.
   */
  const receiveMutation = useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: ConvertCollectionToReceptionPayload;
    }) => convertCollectionToReception(id, payload),

    onSuccess: async () => {
      await Promise.all([
        refreshCollections(),

        queryClient.invalidateQueries({
          queryKey: ["receptions"],
        }),
      ]);

      toast.success("Mascota recibida correctamente.");
    },
  });

  /*
   * CANCEL
   */
  const cancelMutation = useMutation({
    mutationFn: (id: string) =>
      changeCollectionStatus(id, {
        status: CollectionStatus.Cancelled,
      }),

    onSuccess: async () => {
      await refreshCollections();

      toast.success("La recolección fue cancelada.");
    },

    onError: (error) => {
      toast.error(
        getApiErrorMessage(error, "No fue posible cancelar la recolección."),
      );
    },
  });

  const collections = collectionsQuery.data?.items ?? [];

  const totalItems = collectionsQuery.data?.totalItems ?? 0;

  const totalPages = Math.max(collectionsQuery.data?.totalPages ?? 0, 1);

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const pendingCollectionId = updateMutation.isPending
    ? (updateMutation.variables?.id ?? null)
    : receiveMutation.isPending
      ? (receiveMutation.variables?.id ?? null)
      : cancelMutation.isPending
        ? (cancelMutation.variables ?? null)
        : null;

  const isCreateSubmitting = createMutation.isPending;

  const isEditSubmitting = updateMutation.isPending;

  const isReceiveSubmitting = receiveMutation.isPending;

  async function handleCreateSubmit(
    values: CollectionFormValues,
    petPhotoFile: File | null,
  ) {
    let createdCollection: Collection;

    try {
      createdCollection = await createMutation.mutateAsync(
        createCollectionPayload(values),
      );
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible registrar la recolección."),
      );

      return;
    }

    if (petPhotoFile) {
      try {
        await uploadCollectionPhoto(
          createdCollection.id,
          petPhotoFile,
          CollectionPhotoType.PetIdentification,
          "Foto de identificación tomada durante la recolección.",
        );
      } catch (error) {
        toast.error(
          getApiErrorMessage(
            error,
            "La recolección se registró correctamente, pero no fue posible guardar la fotografía. Puedes agregarla nuevamente desde la recolección.",
          ),
          {
            duration: 6000,
          },
        );
      }
    }

    setIsCreateOpen(false);

    /*
     * Immediately show the QR
     * after a successful pickup
     * registration.
     */
    setQrCollection(createdCollection);
  }

  async function handleEditSubmit(values: CollectionUpdateFormValues) {
    if (!editCollection) {
      return;
    }

    try {
      await updateMutation.mutateAsync({
        id: editCollection.id,

        payload: updateCollectionPayload(values),
      });

      setEditCollection(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible actualizar la recolección."),
      );
    }
  }

  async function handleReceiveSubmit(values: CollectionReceptionFormValues) {
    if (!receiveCollection) {
      return;
    }

    try {
      const updatedCollection = await receiveMutation.mutateAsync({
        id: receiveCollection.id,

        payload: collectionReceptionPayload(values),
      });

      setReceiveCollection(null);

      /*
       * Show the same QR again after
       * Reception creation so the
       * user can visually confirm
       * custody continuity.
       */
      setQrCollection(updatedCollection);
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "No fue posible registrar la recepción."),
      );
    }
  }

  function handleCancel(collection: Collection) {
    const confirmed = window.confirm(
      `¿Deseas cancelar la recolección de ${collection.petName}?`,
    );

    if (confirmed) {
      cancelMutation.mutate(collection.id);
    }
  }

  function clearFilters() {
    setSearchInput("");
    setDebouncedSearch("");
    setStatusFilter("");
    setLocationFilter("");
    setPage(1);
  }

  const hasFilters =
    searchInput.length > 0 || statusFilter !== "" || locationFilter !== "";

  return (
    <section className="space-y-6">
      {/* HEADER */}
      <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500">Operación</p>

          <h1 className="mt-1 text-3xl font-bold tracking-tight text-slate-950">
            Recolecciones
          </h1>

          <p className="mt-2 max-w-3xl text-sm text-slate-600">
            Registra la recolección desde el domicilio del cliente o una
            veterinaria, genera la identificación QR y conserva la cadena de
            custodia hasta la recepción.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
              Recolecciones encontradas
            </p>

            <p className="mt-1 text-2xl font-semibold text-slate-900">
              {totalItems}
            </p>
          </div>

          <button
            type="button"
            onClick={() => setIsCreateOpen(true)}
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            + Registrar recolección
          </button>
        </div>
      </header>

      {/* FILTERS */}
      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-3 xl:grid-cols-[minmax(0,1fr)_220px_220px_auto]">
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Buscar por QR, mascota, cliente, teléfono, dirección o veterinaria..."
            className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          />

          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            aria-label="Filtrar por estado"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          >
            <option value="">Todos los estados</option>

            <option value={CollectionStatus.Collected}>Recolectada</option>

            <option value={CollectionStatus.Received}>
              Recibida en instalaciones
            </option>

            <option value={CollectionStatus.Cancelled}>Cancelada</option>
          </select>

          <select
            value={locationFilter}
            onChange={(event) => setLocationFilter(event.target.value)}
            aria-label="Filtrar por lugar de recolección"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          >
            <option value="">Todos los lugares</option>

            <option value={CollectionLocationType.CustomerHome}>
              Domicilio del cliente
            </option>

            <option value={CollectionLocationType.VeterinaryLocation}>
              Veterinaria
            </option>
          </select>

          {hasFilters && (
            <button
              type="button"
              onClick={clearFilters}
              className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            >
              Limpiar filtros
            </button>
          )}
        </div>

        <div className="mt-3 flex justify-end">
          <select
            value={pageSize}
            onChange={(event) => setPageSize(Number(event.target.value))}
            aria-label="Recolecciones por página"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          >
            <option value={10}>10 por página</option>

            <option value={20}>20 por página</option>

            <option value={50}>50 por página</option>
          </select>
        </div>
      </section>

      {/* TABLE */}
      <div>
        {collectionsQuery.isFetching && !collectionsQuery.isLoading && (
          <p className="mb-3 text-sm text-slate-500">
            Actualizando información...
          </p>
        )}

        {collectionsQuery.isLoading && (
          <div className="rounded-2xl border border-slate-200 bg-white px-6 py-16 text-center text-sm text-slate-500">
            Cargando recolecciones...
          </div>
        )}

        {collectionsQuery.isError && (
          <div className="rounded-2xl border border-red-200 bg-red-50 px-6 py-5">
            <p className="font-medium text-red-800">
              No fue posible cargar las recolecciones.
            </p>

            <p className="mt-1 text-sm text-red-700">
              Verifica que el backend esté funcionando e intenta nuevamente.
            </p>
          </div>
        )}

        {!collectionsQuery.isLoading && !collectionsQuery.isError && (
          <CollectionsTable
            collections={collections}
            pendingCollectionId={pendingCollectionId}
            onShowQr={setQrCollection}
            onEdit={setEditCollection}
            onReceive={setReceiveCollection}
            onCancel={handleCancel}
          />
        )}
      </div>

      {/* PAGINATION */}
      {!collectionsQuery.isLoading && !collectionsQuery.isError && (
        <footer className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white px-5 py-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-slate-600">
            Página {page} de {totalPages}
          </p>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((currentPage) => currentPage - 1)}
              disabled={page <= 1 || collectionsQuery.isFetching}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Anterior
            </button>

            <button
              type="button"
              onClick={() => setPage((currentPage) => currentPage + 1)}
              disabled={page >= totalPages || collectionsQuery.isFetching}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Siguiente
            </button>
          </div>
        </footer>
      )}

      {/* CREATE */}
      <CollectionFormModal
        isOpen={isCreateOpen}
        isSubmitting={isCreateSubmitting}
        onClose={() => {
          if (!isCreateSubmitting) {
            setIsCreateOpen(false);
          }
        }}
        onSubmit={handleCreateSubmit}
      />

      {/* EDIT */}
      <CollectionEditModal
        isOpen={editCollection !== null}
        collection={editCollection}
        isSubmitting={isEditSubmitting}
        onClose={() => {
          if (!isEditSubmitting) {
            setEditCollection(null);
          }
        }}
        onSubmit={handleEditSubmit}
      />

      {/* RECEIVE */}
      <CollectionToReceptionModal
        isOpen={receiveCollection !== null}
        collection={receiveCollection}
        isSubmitting={isReceiveSubmitting}
        onClose={() => {
          if (!isReceiveSubmitting) {
            setReceiveCollection(null);
          }
        }}
        onSubmit={handleReceiveSubmit}
      />

      {/* QR */}
      <CollectionQrModal
        isOpen={qrCollection !== null}
        collection={qrCollection}
        onClose={() => setQrCollection(null)}
      />
    </section>
  );
}
