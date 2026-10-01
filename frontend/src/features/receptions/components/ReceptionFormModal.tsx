import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import { useForm, useWatch } from "react-hook-form";
import { getCremationStatusLabel } from "../../cremations/utils/cremationLabels";
import { getPets } from "../../pets/api/petsApi";
import {
  getVeterinarians,
  getVeterinariansByClinic,
  searchVeterinarians,
} from "../../veterinarians/api/veterinariansApi";
import { getVeterinarianFullName } from "../../veterinarians/utils/veterinarianName";
import {
  getVeterinaryClinics,
  searchVeterinaryClinics,
} from "../../veterinary-clinics/api/veterinaryClinicsApi";
import {
  receptionSchema,
  type ReceptionFormValues,
} from "../schemas/receptionSchema";
import type { Reception } from "../types/reception.types";
import {
  formatReceptionDate,
  formatReceptionWeight,
  showOptionalReceptionValue,
} from "../utils/receptionFormatters";
import { ReceptionLookupPagination } from "./ReceptionLookupPagination";

interface SelectedLookupOption {
  id: string;
  label: string;
}

interface SelectedVeterinarianOption extends SelectedLookupOption {
  veterinaryClinicId: string | null;
}

const RECEPTION_LOOKUP_PAGE_SIZE = 20;

interface ReceptionFormModalBaseProps {
  isOpen: boolean;
  reception: Reception | null;
  onClose: () => void;
}

interface ReceptionEditableFormModalProps extends ReceptionFormModalBaseProps {
  mode: "create" | "edit";
  isSubmitting: boolean;
  onSubmit: (values: ReceptionFormValues) => Promise<void>;
}

interface ReceptionReadOnlyFormModalProps extends ReceptionFormModalBaseProps {
  mode: "view";
  reception: Reception;
  isSubmitting?: never;
  onSubmit?: never;
}

type ReceptionFormModalProps =
  ReceptionEditableFormModalProps | ReceptionReadOnlyFormModalProps;

interface ReadOnlyFieldProps {
  label: string;
  value: string;
}

function ReadOnlyField({ label, value }: ReadOnlyFieldProps) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
        {label}
      </dt>
      <dd className="mt-1 whitespace-pre-wrap text-sm text-slate-900">
        {value}
      </dd>
    </div>
  );
}

function ReceptionReadOnlyModal({
  isOpen,
  reception,
  onClose,
}: ReceptionReadOnlyFormModalProps) {
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const previouslyFocused =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    closeButtonRef.current?.focus();

    return () => previouslyFocused?.focus();
  }, [isOpen, reception.id]);

  if (!isOpen) {
    return null;
  }

  function handleDialogKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose();
      return;
    }

    if (event.key === "Tab") {
      event.preventDefault();
      closeButtonRef.current?.focus();
    }
  }

  const referralSource = reception.veterinaryClinicName
    ? reception.veterinaryClinicName
    : reception.referringVeterinarianName
      ? "Veterinario independiente"
      : "Recepción directa";
  const origin = reception.isVeterinaryRequestOrigin
    ? "Solicitud veterinaria"
    : reception.isCollectionOrigin
      ? "Recolección"
      : "Recepción directa";

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={onClose}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-detail-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
        onKeyDown={handleDialogKeyDown}
      >
        <header className="border-b border-slate-200 px-6 py-5">
          <p className="text-sm font-medium text-slate-500">
            Consulta de solo lectura
          </p>
          <h2
            id="reception-detail-title"
            className="mt-1 text-xl font-semibold text-slate-900"
          >
            Detalle de la recepción
          </h2>
          <p className="mt-2 font-mono text-xs text-slate-500">
            {reception.qrCode}
          </p>
        </header>

        <div className="space-y-6 px-6 py-6">
          <section className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-semibold text-slate-900">
              Recepción y cadena de custodia
            </h3>
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <ReadOnlyField label="Mascota" value={reception.petName} />
              <ReadOnlyField label="Cliente" value={reception.customerName} />
              <ReadOnlyField
                label="Fecha de recepción"
                value={formatReceptionDate(reception.receivedAt)}
              />
              <ReadOnlyField
                label="Recibió"
                value={reception.receivedByUserName}
              />
              <ReadOnlyField
                label="Peso verificado"
                value={`${formatReceptionWeight(reception.verifiedWeightKg)} kg`}
              />
              <ReadOnlyField
                label="Estado"
                value={reception.isActive ? "Activa" : "Inactiva"}
              />
              <ReadOnlyField label="Origen" value={origin} />
              <ReadOnlyField
                label="Cremación"
                value={
                  reception.cremationStatus === null
                    ? "Sin cremación"
                    : getCremationStatusLabel(reception.cremationStatus)
                }
              />
              {reception.latestReportedCorrectedWeightKg !== null && (
                <ReadOnlyField
                  label="Peso corregido reportado"
                  value={`${formatReceptionWeight(
                    reception.latestReportedCorrectedWeightKg,
                  )} kg`}
                />
              )}
            </dl>
          </section>

          <section className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-semibold text-slate-900">
              Referencia veterinaria
            </h3>
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <ReadOnlyField label="Procedencia" value={referralSource} />
              <ReadOnlyField
                label="Veterinario"
                value={
                  reception.referringVeterinarianName
                    ? `Dr. ${reception.referringVeterinarianName}`
                    : "No registrado"
                }
              />
              <div className="sm:col-span-2">
                <ReadOnlyField
                  label="Notas de referencia"
                  value={showOptionalReceptionValue(reception.referralNotes)}
                />
              </div>
            </dl>
          </section>

          <section className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-semibold text-slate-900">
              Objetos personales y notas
            </h3>
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <ReadOnlyField
                label="Objetos personales"
                value={reception.hasPersonalBelongings ? "Sí" : "No"}
              />
              <ReadOnlyField
                label="Descripción"
                value={showOptionalReceptionValue(
                  reception.personalBelongingsDescription,
                )}
              />
              <div className="sm:col-span-2">
                <ReadOnlyField
                  label="Notas de la recepción"
                  value={showOptionalReceptionValue(reception.notes)}
                />
              </div>
            </dl>
          </section>

          <footer className="flex justify-end border-t border-slate-200 pt-5">
            <button
              ref={closeButtonRef}
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            >
              Cerrar
            </button>
          </footer>
        </div>
      </section>
    </div>
  );
}

export function ReceptionFormModal(props: ReceptionFormModalProps) {
  if (props.mode === "view") {
    return <ReceptionReadOnlyModal {...props} />;
  }

  return <ReceptionEditableFormModal {...props} />;
}

function ReceptionEditableFormModal({
  isOpen,
  mode,
  reception,
  isSubmitting,
  onClose,
  onSubmit,
}: ReceptionEditableFormModalProps) {
  const [petSearchInput, setPetSearchInput] = useState("");
  const [debouncedPetSearch, setDebouncedPetSearch] = useState("");
  const [petPage, setPetPage] = useState(1);
  const [selectedPetOption, setSelectedPetOption] =
    useState<SelectedLookupOption | null>(null);
  const [clinicSearchInput, setClinicSearchInput] = useState("");
  const [debouncedClinicSearch, setDebouncedClinicSearch] = useState("");
  const [clinicPage, setClinicPage] = useState(1);
  const [selectedClinicOption, setSelectedClinicOption] =
    useState<SelectedLookupOption | null>(null);
  const [veterinarianSearchInput, setVeterinarianSearchInput] = useState("");
  const [debouncedVeterinarianSearch, setDebouncedVeterinarianSearch] =
    useState("");
  const [veterinarianPage, setVeterinarianPage] = useState(1);
  const [selectedVeterinarianOption, setSelectedVeterinarianOption] =
    useState<SelectedVeterinarianOption | null>(null);
  const {
    register,
    reset,
    control,
    setValue,
    handleSubmit,
    formState: { errors },
  } = useForm<ReceptionFormValues>({
    resolver: zodResolver(receptionSchema),

    defaultValues: {
      petId: "",
      veterinaryClinicId: "",
      referringVeterinarianId: "",
      verifiedWeightKg: "",
      hasPersonalBelongings: false,
      personalBelongingsDescription: "",
      referralNotes: "",
      notes: "",
    },
  });

  const selectedClinicId = useWatch({
    control,
    name: "veterinaryClinicId",
  });

  const selectedPetId = useWatch({
    control,
    name: "petId",
  });

  const selectedVeterinarianId = useWatch({
    control,
    name: "referringVeterinarianId",
  });

  const hasPersonalBelongings = useWatch({
    control,
    name: "hasPersonalBelongings",
  });

  const isCollectionOrigin =
    mode === "edit" && reception?.isCollectionOrigin === true;
  const isNormalEditLocked =
    mode === "edit" && reception?.isNormalEditLocked === true;
  const isReferralSourceLocked =
    mode === "edit" &&
    (reception?.isVeterinaryRequestOrigin === true ||
      reception?.isCollectionOrigin === true);
  const canEditReferral = !isNormalEditLocked && !isReferralSourceLocked;

  const normalizedPetSearch = debouncedPetSearch.trim();
  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const normalizedVeterinarianSearch = debouncedVeterinarianSearch.trim();
  const isPetSearchPending = petSearchInput.trim() !== normalizedPetSearch;
  const isClinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;
  const isVeterinarianSearchPending =
    veterinarianSearchInput.trim() !== normalizedVeterinarianSearch;

  const petsQuery = useQuery({
    queryKey: ["pets", "reception-options", true, normalizedPetSearch, petPage],

    queryFn: () =>
      getPets({
        page: petPage,
        pageSize: RECEPTION_LOOKUP_PAGE_SIZE,
        isActive: true,
        search: normalizedPetSearch || undefined,
      }),

    enabled: isOpen && mode === "create" && !isPetSearchPending,
  });

  const clinicsQuery = useQuery({
    queryKey: [
      "veterinary-clinics",
      "reception-options",
      true,
      normalizedClinicSearch,
      clinicPage,
    ],

    queryFn: () => {
      const params = {
        page: clinicPage,
        pageSize: RECEPTION_LOOKUP_PAGE_SIZE,
        isActive: true,
      };

      return normalizedClinicSearch
        ? searchVeterinaryClinics({
            ...params,
            search: normalizedClinicSearch,
          })
        : getVeterinaryClinics(params);
    },

    enabled: isOpen && canEditReferral && !isClinicSearchPending,
  });

  const veterinariansQuery = useQuery({
    queryKey: [
      "veterinarians",
      "reception-options",
      selectedClinicId || "independent",
      selectedClinicId ? "clinic" : normalizedVeterinarianSearch,
      selectedClinicId ? 1 : veterinarianPage,
    ],

    queryFn: async () => {
      if (selectedClinicId) {
        const items = await getVeterinariansByClinic(selectedClinicId, true);
        return {
          items,
          page: 1,
          pageSize: items.length,
          totalItems: items.length,
          totalPages: 1,
        };
      }

      const params = {
        page: veterinarianPage,
        pageSize: RECEPTION_LOOKUP_PAGE_SIZE,
        isActive: true,
        independentOnly: true,
      };

      return normalizedVeterinarianSearch
        ? searchVeterinarians({
            ...params,
            search: normalizedVeterinarianSearch,
          })
        : getVeterinarians(params);
    },

    enabled:
      isOpen &&
      canEditReferral &&
      (selectedClinicId.length > 0 || !isVeterinarianSearchPending),
  });

  const pets = useMemo(
    () =>
      [...(petsQuery.data?.items ?? [])].sort((first, second) => {
        const customerComparison = first.customerName.localeCompare(
          second.customerName,
          "es-MX",
        );

        if (customerComparison !== 0) {
          return customerComparison;
        }

        return first.name.localeCompare(second.name, "es-MX");
      }),
    [petsQuery.data?.items],
  );

  const clinics = useMemo(
    () =>
      [...(clinicsQuery.data?.items ?? [])].sort((first, second) =>
        first.name.localeCompare(second.name, "es-MX"),
      ),
    [clinicsQuery.data?.items],
  );

  const veterinarians = useMemo(
    () =>
      [...(veterinariansQuery.data?.items ?? [])].sort((first, second) =>
        getVeterinarianFullName(first).localeCompare(
          getVeterinarianFullName(second),
          "es-MX",
        ),
      ),
    [veterinariansQuery.data?.items],
  );

  const visiblePets = isPetSearchPending ? [] : pets;
  const visibleClinics = isClinicSearchPending ? [] : clinics;
  const visibleVeterinarians =
    !selectedClinicId && isVeterinarianSearchPending ? [] : veterinarians;

  const retainedPetOption =
    selectedPetId && !visiblePets.some((pet) => pet.id === selectedPetId)
      ? selectedPetOption
      : null;
  const retainedClinicOption =
    selectedClinicId &&
    !visibleClinics.some((clinic) => clinic.id === selectedClinicId) &&
    selectedClinicOption?.id === selectedClinicId
      ? selectedClinicOption
      : null;
  const selectedClinicContext = selectedClinicId || null;
  const retainedVeterinarianOption =
    selectedVeterinarianId &&
    !visibleVeterinarians.some(
      (veterinarian) => veterinarian.id === selectedVeterinarianId,
    ) &&
    selectedVeterinarianOption?.id === selectedVeterinarianId &&
    selectedVeterinarianOption.veterinaryClinicId === selectedClinicContext
      ? selectedVeterinarianOption
      : null;

  const hasReferralSource =
    selectedClinicId.length > 0 || selectedVeterinarianId.length > 0;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      petId: reception?.petId ?? "",
      veterinaryClinicId: reception?.veterinaryClinicId ?? "",
      referringVeterinarianId: reception?.referringVeterinarianId ?? "",
      verifiedWeightKg:
        reception !== null ? String(reception.verifiedWeightKg) : "",
      hasPersonalBelongings: reception?.hasPersonalBelongings ?? false,
      personalBelongingsDescription:
        reception?.personalBelongingsDescription ?? "",
      referralNotes: reception?.referralNotes ?? "",
      notes: reception?.notes ?? "",
    });
    setPetSearchInput("");
    setDebouncedPetSearch("");
    setPetPage(1);
    setSelectedPetOption(
      reception
        ? {
            id: reception.petId,
            label: `${reception.petName} — ${reception.customerName}`,
          }
        : null,
    );
    setClinicSearchInput("");
    setDebouncedClinicSearch("");
    setClinicPage(1);
    setSelectedClinicOption(
      reception?.veterinaryClinicId
        ? {
            id: reception.veterinaryClinicId,
            label: reception.veterinaryClinicName ?? "Veterinaria actual",
          }
        : null,
    );
    setVeterinarianSearchInput("");
    setDebouncedVeterinarianSearch("");
    setVeterinarianPage(1);
    setSelectedVeterinarianOption(
      reception?.referringVeterinarianId
        ? {
            id: reception.referringVeterinarianId,
            label: `Dr. ${
              reception.referringVeterinarianName ?? "Veterinario actual"
            }`,
            veterinaryClinicId: reception.veterinaryClinicId,
          }
        : null,
    );
  }, [isOpen, reception, reset]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedPetSearch(petSearchInput.trim());
      setPetPage(1);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [petSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedClinicSearch(clinicSearchInput.trim());
      setClinicPage(1);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [clinicSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedVeterinarianSearch(veterinarianSearchInput.trim());
      setVeterinarianPage(1);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [veterinarianSearchInput]);

  if (!isOpen) {
    return null;
  }

  const clinicField = register("veterinaryClinicId");
  const petField = register("petId");
  const veterinarianField = register("referringVeterinarianId");
  const belongingsField = register("hasPersonalBelongings");

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      role="presentation"
      onMouseDown={handleBackdropClick}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-form-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Ingreso y cadena de custodia
            </p>

            <h2
              id="reception-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create"
                ? "Registrar recepción"
                : isNormalEditLocked
                  ? "Consultar recepción"
                  : "Editar recepción"}
            </h2>

            {mode === "edit" && reception && (
              <p className="mt-2 font-mono text-xs text-slate-500">
                {reception.qrCode}
              </p>
            )}
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-6 px-6 py-6"
          noValidate
        >
          {isNormalEditLocked && (
            <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
              <p className="font-medium text-amber-950">
                Esta recepción ya forma parte del proceso de cremación.
              </p>
              <p className="mt-1 text-sm text-amber-800">
                Los cambios operativos deben registrarse mediante una enmienda.
              </p>
            </div>
          )}

          <div>
            <label
              htmlFor="reception-pet"
              className="block text-sm font-medium text-slate-700"
            >
              Mascota
            </label>

            {mode === "create" && (
              <div className="mt-2">
                <label
                  htmlFor="reception-pet-search"
                  className="block text-xs font-medium text-slate-600"
                >
                  Buscar mascota
                </label>
                <input
                  id="reception-pet-search"
                  type="search"
                  value={petSearchInput}
                  onChange={(event) => setPetSearchInput(event.target.value)}
                  disabled={isSubmitting}
                  placeholder="Nombre, cliente o especie"
                  className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />
                {isPetSearchPending && (
                  <p className="mt-1 text-xs text-slate-500" role="status">
                    Buscando...
                  </p>
                )}
              </div>
            )}

            <select
              id="reception-pet"
              value={selectedPetId}
              disabled={
                isSubmitting ||
                petsQuery.isLoading ||
                petsQuery.isError ||
                mode === "edit"
              }
              {...petField}
              onChange={(event) => {
                petField.onChange(event);
                const pet = pets.find(
                  (candidate) => candidate.id === event.target.value,
                );
                setSelectedPetOption(
                  pet
                    ? {
                        id: pet.id,
                        label: `${pet.name} — ${pet.customerName} · ${pet.species}`,
                      }
                    : null,
                );
              }}
              className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            >
              <option value="">
                {petsQuery.isLoading || isPetSearchPending
                  ? "Cargando mascotas..."
                  : "Selecciona una mascota"}
              </option>

              {retainedPetOption && (
                <option value={retainedPetOption.id}>
                  {retainedPetOption.label}
                </option>
              )}

              {visiblePets.map((pet) => (
                <option key={pet.id} value={pet.id}>
                  {pet.name} — {pet.customerName} · {pet.species}
                </option>
              ))}
            </select>

            {mode === "edit" && (
              <p className="mt-2 text-xs text-slate-500">
                La mascota no puede cambiarse después de registrar la recepción.
              </p>
            )}

            {errors.petId && (
              <p className="mt-2 text-sm text-red-600">
                {errors.petId.message}
              </p>
            )}

            {petsQuery.isError && (
              <div className="mt-2 text-sm text-red-600">
                <p>No fue posible cargar las mascotas activas.</p>
                <button
                  type="button"
                  onClick={() => void petsQuery.refetch()}
                  className="mt-1 font-medium underline underline-offset-2"
                >
                  Intentar nuevamente
                </button>
              </div>
            )}

            {mode === "create" &&
              !petsQuery.isLoading &&
              !petsQuery.isError &&
              !isPetSearchPending &&
              visiblePets.length === 0 && (
                <p className="mt-2 text-sm text-slate-500" role="status">
                  No se encontraron mascotas.
                </p>
              )}

            {mode === "create" &&
              !petsQuery.isLoading &&
              !petsQuery.isError &&
              !isPetSearchPending && (
                <ReceptionLookupPagination
                  page={petPage}
                  totalPages={petsQuery.data?.totalPages ?? 0}
                  isFetching={petsQuery.isFetching}
                  onPageChange={setPetPage}
                />
              )}
          </div>

          <div>
            <label
              htmlFor="reception-weight"
              className="block text-sm font-medium text-slate-700"
            >
              {mode === "edit" && reception?.hasCremation
                ? "Peso operativo"
                : "Peso verificado"}
            </label>

            <div className="relative mt-2">
              <input
                id="reception-weight"
                type="number"
                min="0.01"
                max="999.99"
                step="0.01"
                inputMode="decimal"
                disabled={
                  isSubmitting || isCollectionOrigin || isNormalEditLocked
                }
                {...register("verifiedWeightKg")}
                className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-500">
                kg
              </span>
            </div>

            {errors.verifiedWeightKg && (
              <p className="mt-2 text-sm text-red-600">
                {errors.verifiedWeightKg.message}
              </p>
            )}

            {isCollectionOrigin && (
              <p className="mt-2 text-xs text-slate-500">
                El peso verificado fue finalizado desde la recolección y no
                puede modificarse desde la edición normal.
              </p>
            )}

            {mode === "edit" &&
              reception?.latestReportedCorrectedWeightKg !== null &&
              reception?.latestReportedCorrectedWeightKg !== undefined &&
              reception.latestReportedCorrectedWeightKg !==
                reception.verifiedWeightKg && (
                <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2">
                  <p className="text-xs font-medium text-amber-800">
                    Peso corregido reportado
                  </p>
                  <p className="mt-1 font-semibold text-amber-950">
                    {reception.latestReportedCorrectedWeightKg.toFixed(2)} kg
                  </p>
                  <p className="mt-1 text-xs text-amber-700">
                    El peso operativo permanece en el campo superior.
                  </p>
                </div>
              )}
          </div>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Referencia veterinaria
            </legend>

            <p className="mb-4 text-sm text-slate-500">
              Deja estos campos vacíos cuando la recepción sea directa.
            </p>

            <div className="grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="reception-clinic"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinaria
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                {canEditReferral && (
                  <div className="mt-2">
                    <label
                      htmlFor="reception-clinic-search"
                      className="block text-xs font-medium text-slate-600"
                    >
                      Buscar veterinaria
                    </label>
                    <input
                      id="reception-clinic-search"
                      type="search"
                      value={clinicSearchInput}
                      onChange={(event) =>
                        setClinicSearchInput(event.target.value)
                      }
                      disabled={isSubmitting}
                      placeholder="Nombre de la veterinaria"
                      className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                    />
                    {isClinicSearchPending && (
                      <p className="mt-1 text-xs text-slate-500" role="status">
                        Buscando...
                      </p>
                    )}
                  </div>
                )}

                <select
                  id="reception-clinic"
                  value={selectedClinicId}
                  disabled={
                    isSubmitting ||
                    clinicsQuery.isLoading ||
                    clinicsQuery.isError ||
                    isNormalEditLocked ||
                    isReferralSourceLocked
                  }
                  {...clinicField}
                  onChange={(event) => {
                    clinicField.onChange(event);

                    const clinic = clinics.find(
                      (candidate) => candidate.id === event.target.value,
                    );
                    setSelectedClinicOption(
                      clinic ? { id: clinic.id, label: clinic.name } : null,
                    );

                    setValue("referringVeterinarianId", "", {
                      shouldValidate: true,
                    });
                    setSelectedVeterinarianOption(null);
                    setVeterinarianSearchInput("");
                    setDebouncedVeterinarianSearch("");
                    setVeterinarianPage(1);
                  }}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {clinicsQuery.isLoading || isClinicSearchPending
                      ? "Cargando veterinarias..."
                      : "Sin veterinaria / independiente"}
                  </option>

                  {retainedClinicOption && (
                    <option value={retainedClinicOption.id}>
                      {retainedClinicOption.label}
                    </option>
                  )}

                  {visibleClinics.map((clinic) => (
                    <option key={clinic.id} value={clinic.id}>
                      {clinic.name}
                    </option>
                  ))}
                </select>

                {errors.veterinaryClinicId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.veterinaryClinicId.message}
                  </p>
                )}

                {canEditReferral && clinicsQuery.isError && (
                  <div className="mt-2 text-sm text-red-600">
                    <p>No fue posible cargar las veterinarias activas.</p>
                    <button
                      type="button"
                      onClick={() => void clinicsQuery.refetch()}
                      className="mt-1 font-medium underline underline-offset-2"
                    >
                      Intentar nuevamente
                    </button>
                  </div>
                )}

                {canEditReferral &&
                  !clinicsQuery.isLoading &&
                  !clinicsQuery.isError &&
                  !isClinicSearchPending &&
                  visibleClinics.length === 0 && (
                    <p className="mt-2 text-sm text-slate-500" role="status">
                      No se encontraron veterinarias.
                    </p>
                  )}

                {canEditReferral &&
                  !clinicsQuery.isLoading &&
                  !clinicsQuery.isError &&
                  !isClinicSearchPending && (
                    <ReceptionLookupPagination
                      page={clinicPage}
                      totalPages={clinicsQuery.data?.totalPages ?? 0}
                      isFetching={clinicsQuery.isFetching}
                      onPageChange={setClinicPage}
                    />
                  )}
              </div>

              <div>
                <label
                  htmlFor="reception-veterinarian"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinario referente
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                {canEditReferral && !selectedClinicId && (
                  <div className="mt-2">
                    <label
                      htmlFor="reception-veterinarian-search"
                      className="block text-xs font-medium text-slate-600"
                    >
                      Buscar veterinario independiente
                    </label>
                    <input
                      id="reception-veterinarian-search"
                      type="search"
                      value={veterinarianSearchInput}
                      onChange={(event) =>
                        setVeterinarianSearchInput(event.target.value)
                      }
                      disabled={isSubmitting}
                      placeholder="Nombre del veterinario"
                      className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                    />
                    {isVeterinarianSearchPending && (
                      <p className="mt-1 text-xs text-slate-500" role="status">
                        Buscando...
                      </p>
                    )}
                  </div>
                )}

                <select
                  id="reception-veterinarian"
                  value={selectedVeterinarianId}
                  disabled={
                    isSubmitting ||
                    isNormalEditLocked ||
                    isReferralSourceLocked ||
                    veterinariansQuery.isLoading ||
                    veterinariansQuery.isError
                  }
                  {...veterinarianField}
                  onChange={(event) => {
                    veterinarianField.onChange(event);
                    const veterinarian = veterinarians.find(
                      (candidate) => candidate.id === event.target.value,
                    );
                    setSelectedVeterinarianOption(
                      veterinarian
                        ? {
                            id: veterinarian.id,
                            label: `Dr. ${getVeterinarianFullName(
                              veterinarian,
                            )}`,
                            veterinaryClinicId: veterinarian.veterinaryClinicId,
                          }
                        : null,
                    );
                  }}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {veterinariansQuery.isLoading ||
                    (!selectedClinicId && isVeterinarianSearchPending)
                      ? "Cargando veterinarios..."
                      : selectedClinicId.length > 0
                        ? "Sin veterinario referente"
                        : "Sin veterinario / recepción directa"}
                  </option>

                  {retainedVeterinarianOption && (
                    <option value={retainedVeterinarianOption.id}>
                      {retainedVeterinarianOption.label}
                    </option>
                  )}

                  {visibleVeterinarians.map((veterinarian) => (
                    <option key={veterinarian.id} value={veterinarian.id}>
                      Dr. {getVeterinarianFullName(veterinarian)}
                    </option>
                  ))}
                </select>

                {errors.referringVeterinarianId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.referringVeterinarianId.message}
                  </p>
                )}

                {veterinariansQuery.isError && (
                  <div className="mt-2 text-sm text-red-600">
                    <p>No fue posible cargar los veterinarios activos.</p>
                    <button
                      type="button"
                      onClick={() => void veterinariansQuery.refetch()}
                      className="mt-1 font-medium underline underline-offset-2"
                    >
                      Intentar nuevamente
                    </button>
                  </div>
                )}

                {canEditReferral &&
                  !veterinariansQuery.isLoading &&
                  !veterinariansQuery.isError &&
                  (!selectedClinicId ? !isVeterinarianSearchPending : true) &&
                  visibleVeterinarians.length === 0 && (
                    <p className="mt-2 text-sm text-slate-500" role="status">
                      {selectedClinicId
                        ? "No se encontraron veterinarios en esta veterinaria."
                        : "No se encontraron veterinarios independientes."}
                    </p>
                  )}

                {canEditReferral &&
                  !selectedClinicId &&
                  !veterinariansQuery.isLoading &&
                  !veterinariansQuery.isError &&
                  !isVeterinarianSearchPending && (
                    <ReceptionLookupPagination
                      page={veterinarianPage}
                      totalPages={veterinariansQuery.data?.totalPages ?? 0}
                      isFetching={veterinariansQuery.isFetching}
                      onPageChange={setVeterinarianPage}
                    />
                  )}
              </div>
            </div>

            {isReferralSourceLocked && (
              <p className="mt-3 text-xs text-slate-500">
                La veterinaria y el veterinario provienen del registro de origen
                y no pueden modificarse desde la edición normal.
              </p>
            )}

            <div className="mt-5">
              <label
                htmlFor="reception-referral-notes"
                className="block text-sm font-medium text-slate-700"
              >
                Notas de referencia
                <span className="ml-1 font-normal text-slate-400">
                  (opcional)
                </span>
              </label>

              <textarea
                id="reception-referral-notes"
                rows={3}
                maxLength={1000}
                disabled={
                  isSubmitting || isNormalEditLocked || !hasReferralSource
                }
                {...register("referralNotes")}
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />

              {errors.referralNotes && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.referralNotes.message}
                </p>
              )}
            </div>
          </fieldset>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Objetos personales
            </legend>

            <label className="flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting || isNormalEditLocked}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);

                  if (!event.target.checked) {
                    setValue("personalBelongingsDescription", "", {
                      shouldValidate: true,
                    });
                  }
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-500"
              />

              <span>
                <span className="block text-sm font-medium text-slate-800">
                  Se recibieron objetos personales
                </span>

                <span className="mt-1 block text-sm text-slate-500">
                  Collar, placa, manta, transportadora u otros artículos.
                </span>
              </span>
            </label>

            {hasPersonalBelongings && (
              <div className="mt-4">
                <label
                  htmlFor="reception-belongings"
                  className="block text-sm font-medium text-slate-700"
                >
                  Descripción de los objetos
                </label>

                <textarea
                  id="reception-belongings"
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting || isNormalEditLocked}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />

                {errors.personalBelongingsDescription && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.personalBelongingsDescription.message}
                  </p>
                )}
              </div>
            )}
          </fieldset>

          <div>
            <label
              htmlFor="reception-notes"
              className="block text-sm font-medium text-slate-700"
            >
              Notas internas
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            {isNormalEditLocked ? (
              <div
                id="reception-notes"
                className="mt-2 min-h-24 whitespace-pre-wrap rounded-lg border border-slate-200 bg-slate-50 px-3 py-2.5 text-sm text-slate-700"
              >
                {reception?.notes?.trim() || "Sin notas originales."}
              </div>
            ) : (
              <textarea
                id="reception-notes"
                rows={4}
                maxLength={1000}
                disabled={isSubmitting}
                {...register("notes")}
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
              />
            )}

            {isNormalEditLocked && (
              <p className="mt-2 text-xs text-slate-500">
                Las notas originales se conservan. Usa “Agregar aclaración” para
                registrar información adicional.
              </p>
            )}

            {errors.notes && (
              <p className="mt-2 text-sm text-red-600">
                {errors.notes.message}
              </p>
            )}
          </div>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              {isNormalEditLocked ? "Cerrar" : "Cancelar"}
            </button>

            {!isNormalEditLocked && (
              <button
                type="submit"
                disabled={
                  isSubmitting || petsQuery.isLoading || clinicsQuery.isLoading
                }
                className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting
                  ? "Guardando..."
                  : mode === "create"
                    ? "Registrar recepción"
                    : "Guardar cambios"}
              </button>
            )}
          </footer>
        </form>
      </section>
    </div>
  );
}
