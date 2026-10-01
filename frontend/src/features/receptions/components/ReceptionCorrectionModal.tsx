import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
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
  receptionCorrectionSchema,
  type ReceptionCorrectionFormValues,
} from "../schemas/receptionCorrectionSchema";
import type {
  Reception,
  ReceptionCorrectionDraft,
} from "../types/reception.types";
import { buildReceptionCorrectionDraft } from "../utils/receptionCorrection";
import { formatReceptionWeight } from "../utils/receptionFormatters";
import { ReceptionLookupPagination } from "./ReceptionLookupPagination";

interface SelectedLookupOption {
  id: string;
  label: string;
}

interface SelectedVeterinarianOption extends SelectedLookupOption {
  veterinaryClinicId: string | null;
}

const RECEPTION_LOOKUP_PAGE_SIZE = 20;

interface ReceptionCorrectionModalProps {
  isOpen: boolean;
  reception: Reception | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (draft: ReceptionCorrectionDraft) => Promise<void>;
}

export function ReceptionCorrectionModal({
  isOpen,
  reception,
  isSubmitting,
  onClose,
  onSubmit,
}: ReceptionCorrectionModalProps) {
  const [noChangesMessage, setNoChangesMessage] = useState<string | null>(null);
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
  } = useForm<ReceptionCorrectionFormValues>({
    resolver: zodResolver(receptionCorrectionSchema),
    defaultValues: {
      reason: "",
      verifiedWeightKg: "",
      veterinaryClinicId: "",
      referringVeterinarianId: "",
      hasPersonalBelongings: false,
      personalBelongingsDescription: "",
      referralNotes: "",
    },
  });

  const selectedClinicId = useWatch({
    control,
    name: "veterinaryClinicId",
  });
  const selectedVeterinarianId = useWatch({
    control,
    name: "referringVeterinarianId",
  });
  const hasPersonalBelongings = useWatch({
    control,
    name: "hasPersonalBelongings",
  });

  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const normalizedVeterinarianSearch = debouncedVeterinarianSearch.trim();
  const isClinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;
  const isVeterinarianSearchPending =
    veterinarianSearchInput.trim() !== normalizedVeterinarianSearch;

  const clinicsQuery = useQuery({
    queryKey: [
      "veterinary-clinics",
      "reception-correction-options",
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
    enabled: isOpen && !isClinicSearchPending,
  });

  const veterinariansQuery = useQuery({
    queryKey: [
      "veterinarians",
      "reception-correction-options",
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
      isOpen && (selectedClinicId.length > 0 || !isVeterinarianSearchPending),
  });

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

  const visibleClinics = isClinicSearchPending ? [] : clinics;
  const visibleVeterinarians =
    !selectedClinicId && isVeterinarianSearchPending ? [] : veterinarians;

  useEffect(() => {
    if (!isOpen || reception === null) {
      return;
    }

    reset({
      reason: "",
      verifiedWeightKg: String(
        reception.latestReportedCorrectedWeightKg ?? reception.verifiedWeightKg,
      ),
      veterinaryClinicId: reception.veterinaryClinicId ?? "",
      referringVeterinarianId: reception.referringVeterinarianId ?? "",
      hasPersonalBelongings: reception.hasPersonalBelongings,
      personalBelongingsDescription:
        reception.personalBelongingsDescription ?? "",
      referralNotes: reception.referralNotes ?? "",
    });
    setNoChangesMessage(null);
    setClinicSearchInput("");
    setDebouncedClinicSearch("");
    setClinicPage(1);
    setSelectedClinicOption(
      reception.veterinaryClinicId
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
      reception.referringVeterinarianId
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

  if (!isOpen || reception === null) {
    return null;
  }

  const activeReception = reception;
  const clinicField = register("veterinaryClinicId");
  const veterinarianField = register("referringVeterinarianId");
  const belongingsField = register("hasPersonalBelongings");
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

  async function submit(values: ReceptionCorrectionFormValues) {
    const draft = buildReceptionCorrectionDraft(activeReception, values);
    if (draft === null) {
      setNoChangesMessage(
        "Modifica al menos un campo para registrar la enmienda.",
      );
      return;
    }

    setNoChangesMessage(null);
    await onSubmit(draft);
  }

  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/65 px-4 py-8"
      role="presentation"
      onMouseDown={() => {
        if (!isSubmitting) onClose();
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="reception-correction-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Registro formal e inmutable
            </p>
            <h2
              id="reception-correction-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Crear enmienda
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              {reception.petName} · {reception.customerName}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar enmienda"
            className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 disabled:opacity-50"
          >
            ×
          </button>
        </header>

        <form
          className="space-y-6 px-6 py-6"
          onSubmit={handleSubmit(submit)}
          noValidate
        >
          <div className="rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">
            Registra únicamente la información corregida. La recepción original
            y el historial anterior no se reemplazan.
          </div>

          <div>
            <label
              htmlFor="correction-reason"
              className="block text-sm font-medium text-slate-700"
            >
              Motivo de la enmienda
            </label>
            <textarea
              id="correction-reason"
              rows={3}
              maxLength={1000}
              disabled={isSubmitting}
              {...register("reason")}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />
            {errors.reason && (
              <p className="mt-2 text-sm text-red-600">
                {errors.reason.message}
              </p>
            )}
          </div>

          <div className="rounded-xl border border-slate-200 p-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Peso operativo
                </p>
                <p className="mt-1 font-semibold text-slate-900">
                  {formatReceptionWeight(reception.verifiedWeightKg)} kg
                </p>
              </div>
              {reception.latestReportedCorrectedWeightKg !== null &&
                reception.latestReportedCorrectedWeightKg !==
                  reception.verifiedWeightKg && (
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-amber-700">
                      Peso corregido reportado
                    </p>
                    <p className="mt-1 font-semibold text-amber-900">
                      {formatReceptionWeight(
                        reception.latestReportedCorrectedWeightKg,
                      )}{" "}
                      kg
                    </p>
                  </div>
                )}
            </div>
            <label
              htmlFor="correction-weight"
              className="mt-4 block text-sm font-medium text-slate-700"
            >
              Peso corregido
            </label>
            <div className="relative mt-2">
              <input
                id="correction-weight"
                type="number"
                min="0.01"
                max="999.99"
                step="0.01"
                inputMode="decimal"
                disabled={isSubmitting || reception.isCollectionOrigin}
                {...register("verifiedWeightKg")}
                className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
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
            {reception.isCollectionOrigin && (
              <p className="mt-2 text-xs text-slate-500">
                El peso finalizado desde recolección continúa protegido y no
                puede corregirse desde esta acción.
              </p>
            )}
          </div>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Referencia veterinaria
            </legend>
            <p className="mb-4 text-sm text-slate-500">
              Puede elegir una veterinaria con su veterinario o un veterinario
              independiente.
            </p>
            <div className="grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="correction-clinic"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinaria
                </label>
                <div className="mt-2">
                  <label
                    htmlFor="correction-clinic-search"
                    className="block text-xs font-medium text-slate-600"
                  >
                    Buscar veterinaria
                  </label>
                  <input
                    id="correction-clinic-search"
                    type="search"
                    value={clinicSearchInput}
                    onChange={(event) =>
                      setClinicSearchInput(event.target.value)
                    }
                    disabled={isSubmitting}
                    placeholder="Nombre de la veterinaria"
                    className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                  />
                  {isClinicSearchPending && (
                    <p className="mt-1 text-xs text-slate-500" role="status">
                      Buscando...
                    </p>
                  )}
                </div>
                <select
                  id="correction-clinic"
                  value={selectedClinicId}
                  disabled={
                    isSubmitting ||
                    clinicsQuery.isLoading ||
                    clinicsQuery.isError
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
                    setNoChangesMessage(null);
                  }}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
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
                {clinicsQuery.isError && (
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
                {!clinicsQuery.isLoading &&
                  !clinicsQuery.isError &&
                  !isClinicSearchPending &&
                  visibleClinics.length === 0 && (
                    <p className="mt-2 text-sm text-slate-500" role="status">
                      No se encontraron veterinarias.
                    </p>
                  )}
                {!clinicsQuery.isLoading &&
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
                  htmlFor="correction-veterinarian"
                  className="block text-sm font-medium text-slate-700"
                >
                  Veterinario referente
                </label>
                {!selectedClinicId && (
                  <div className="mt-2">
                    <label
                      htmlFor="correction-veterinarian-search"
                      className="block text-xs font-medium text-slate-600"
                    >
                      Buscar veterinario independiente
                    </label>
                    <input
                      id="correction-veterinarian-search"
                      type="search"
                      value={veterinarianSearchInput}
                      onChange={(event) =>
                        setVeterinarianSearchInput(event.target.value)
                      }
                      disabled={isSubmitting}
                      placeholder="Nombre del veterinario"
                      className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                    />
                    {isVeterinarianSearchPending && (
                      <p className="mt-1 text-xs text-slate-500" role="status">
                        Buscando...
                      </p>
                    )}
                  </div>
                )}
                <select
                  id="correction-veterinarian"
                  value={selectedVeterinarianId}
                  disabled={
                    isSubmitting ||
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
                    setNoChangesMessage(null);
                  }}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                >
                  <option value="">
                    {veterinariansQuery.isLoading ||
                    (!selectedClinicId && isVeterinarianSearchPending)
                      ? "Cargando veterinarios..."
                      : selectedClinicId
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
                {!veterinariansQuery.isLoading &&
                  !veterinariansQuery.isError &&
                  (!selectedClinicId ? !isVeterinarianSearchPending : true) &&
                  visibleVeterinarians.length === 0 && (
                    <p className="mt-2 text-sm text-slate-500" role="status">
                      {selectedClinicId
                        ? "No se encontraron veterinarios en esta veterinaria."
                        : "No se encontraron veterinarios independientes."}
                    </p>
                  )}
                {!selectedClinicId &&
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

            <label
              htmlFor="correction-referral-notes"
              className="mt-5 block text-sm font-medium text-slate-700"
            >
              Notas de referencia
            </label>
            <textarea
              id="correction-referral-notes"
              rows={3}
              maxLength={1000}
              disabled={isSubmitting}
              {...register("referralNotes")}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
            />
            {errors.referralNotes && (
              <p className="mt-2 text-sm text-red-600">
                {errors.referralNotes.message}
              </p>
            )}
          </fieldset>

          <fieldset className="rounded-xl border border-slate-200 p-4">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Objetos personales
            </legend>
            <label className="flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);
                  if (!event.target.checked) {
                    setValue("personalBelongingsDescription", "", {
                      shouldValidate: true,
                    });
                  }
                  setNoChangesMessage(null);
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-500"
              />
              <span className="text-sm font-medium text-slate-800">
                Se recibieron objetos personales
              </span>
            </label>
            {hasPersonalBelongings && (
              <div className="mt-4">
                <label
                  htmlFor="correction-belongings"
                  className="block text-sm font-medium text-slate-700"
                >
                  Descripción de los objetos
                </label>
                <textarea
                  id="correction-belongings"
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100"
                />
                {errors.personalBelongingsDescription && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.personalBelongingsDescription.message}
                  </p>
                )}
              </div>
            )}
          </fieldset>

          {noChangesMessage && (
            <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {noChangesMessage}
            </p>
          )}

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={
                isSubmitting ||
                clinicsQuery.isLoading ||
                clinicsQuery.isError ||
                veterinariansQuery.isLoading ||
                veterinariansQuery.isError
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Registrando..." : "Registrar enmienda"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
