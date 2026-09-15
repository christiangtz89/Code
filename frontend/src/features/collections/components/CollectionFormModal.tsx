import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";

import {
  getCollectionCustomerOptions,
  getCollectionPetOptions,
  getCollectionVeterinarianOptions,
  getCollectionVeterinaryClinicOptions,
} from "../api/collectionsApi";

import {
  collectionSchema,
  getMexicoBusinessDate,
  type CollectionFormValues,
} from "../schemas/collectionSchema";
import { CollectionLookupPagination } from "./CollectionLookupPagination";

interface CollectionFormModalProps {
  isOpen: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: CollectionFormValues) => Promise<void>;
}

const defaultValues: CollectionFormValues = {
  customerMode: "existing",
  petMode: "existing",

  existingCustomerId: "",
  existingPetId: "",

  ownerFirstName: "",
  ownerLastName: "",
  ownerPhone: "",
  ownerEmail: "",

  petName: "",
  species: "",
  breed: "",
  sex: "",
  color: "",
  approximateWeightKg: "",
  dateOfDeath: "",

  locationType: "1",

  veterinaryClinicId: "",
  referringVeterinarianId: "",

  pickupAddress: "",
  pickupContactName: "",
  pickupContactPhone: "",

  hasPersonalBelongings: false,
  personalBelongingsDescription: "",

  notes: "",
};

export function CollectionFormModal({
  isOpen,
  isSubmitting,
  onClose,
  onSubmit,
}: CollectionFormModalProps) {
  const [customerSearchInput, setCustomerSearchInput] = useState("");
  const [debouncedCustomerSearch, setDebouncedCustomerSearch] = useState("");
  const [clinicSearchInput, setClinicSearchInput] = useState("");
  const [debouncedClinicSearch, setDebouncedClinicSearch] = useState("");
  const [clinicPage, setClinicPage] = useState(1);
  const [veterinarianSearchInput, setVeterinarianSearchInput] = useState("");
  const [debouncedVeterinarianSearch, setDebouncedVeterinarianSearch] =
    useState("");
  const [veterinarianPage, setVeterinarianPage] = useState(1);

  const {
    register,
    control,
    reset,
    setValue,
    clearErrors,
    handleSubmit,
    formState: { errors },
  } = useForm<CollectionFormValues>({
    resolver: zodResolver(collectionSchema),
    defaultValues,
  });

  const customerMode = useWatch({
    control,
    name: "customerMode",
  });

  const petMode = useWatch({
    control,
    name: "petMode",
  });

  const selectedCustomerId = useWatch({
    control,
    name: "existingCustomerId",
  });

  const selectedPetId = useWatch({
    control,
    name: "existingPetId",
  });

  const locationType = useWatch({
    control,
    name: "locationType",
  });

  const selectedClinicId = useWatch({
    control,
    name: "veterinaryClinicId",
  });

  const hasPersonalBelongings = useWatch({
    control,
    name: "hasPersonalBelongings",
  });

  const normalizedCustomerSearch = debouncedCustomerSearch.trim();
  const customerSearchPending =
    customerSearchInput.trim() !== normalizedCustomerSearch;
  const normalizedClinicSearch = debouncedClinicSearch.trim();
  const clinicSearchPending =
    clinicSearchInput.trim() !== normalizedClinicSearch;
  const normalizedVeterinarianSearch = debouncedVeterinarianSearch.trim();
  const veterinarianSearchPending =
    veterinarianSearchInput.trim() !== normalizedVeterinarianSearch;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedCustomerSearch(customerSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [customerSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setClinicPage(1);
      setDebouncedClinicSearch(clinicSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [clinicSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setVeterinarianPage(1);
      setDebouncedVeterinarianSearch(veterinarianSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [veterinarianSearchInput]);

  const customersQuery = useQuery({
    queryKey: [
      "collections",
      "intake-customer-options",
      normalizedCustomerSearch,
    ],

    queryFn: () =>
      getCollectionCustomerOptions({
        search: normalizedCustomerSearch || undefined,
        page: 1,
        pageSize: 20,
      }),

    enabled: isOpen && customerMode === "existing",
  });

  const customerPetsQuery = useQuery({
    queryKey: ["collections", "intake-pet-options", selectedCustomerId],

    queryFn: () => getCollectionPetOptions(selectedCustomerId),

    enabled:
      isOpen && customerMode === "existing" && selectedCustomerId.length > 0,
  });

  const clinicsQuery = useQuery({
    queryKey: [
      "collections",
      "intake-veterinary-clinic-options",
      { search: normalizedClinicSearch, page: clinicPage },
    ],

    queryFn: () =>
      getCollectionVeterinaryClinicOptions({
        search: normalizedClinicSearch || undefined,
        page: clinicPage,
        pageSize: 20,
      }),

    enabled: isOpen && locationType === "2",
  });

  const veterinariansQuery = useQuery({
    queryKey: [
      "collections",
      "intake-veterinarian-options",
      {
        veterinaryClinicId: selectedClinicId,
        search: normalizedVeterinarianSearch,
        page: veterinarianPage,
      },
    ],

    queryFn: () =>
      getCollectionVeterinarianOptions({
        veterinaryClinicId: selectedClinicId || undefined,
        search: normalizedVeterinarianSearch || undefined,
        page: veterinarianPage,
        pageSize: 20,
      }),

    enabled: isOpen && locationType === "2",
  });

  const customers = useMemo(
    () =>
      [
        ...(customerSearchPending ? [] : (customersQuery.data?.items ?? [])),
      ].sort((first, second) =>
        first.displayName.localeCompare(second.displayName, "es-MX"),
      ),
    [customerSearchPending, customersQuery.data?.items],
  );

  const customerPets = useMemo(
    () =>
      [...(customerPetsQuery.data ?? [])].sort((first, second) =>
        first.name.localeCompare(second.name, "es-MX"),
      ),
    [customerPetsQuery.data],
  );

  const clinics = useMemo(
    () => (clinicSearchPending ? [] : (clinicsQuery.data?.items ?? [])),
    [clinicSearchPending, clinicsQuery.data?.items],
  );

  const veterinarians = useMemo(
    () =>
      veterinarianSearchPending ? [] : (veterinariansQuery.data?.items ?? []),
    [veterinarianSearchPending, veterinariansQuery.data?.items],
  );

  const selectedCustomer =
    customers.find((customer) => customer.id === selectedCustomerId) ?? null;

  const selectedPet =
    customerPets.find((pet) => pet.id === selectedPetId) ?? null;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(defaultValues);
    setCustomerSearchInput("");
    setDebouncedCustomerSearch("");
    setClinicSearchInput("");
    setDebouncedClinicSearch("");
    setClinicPage(1);
    setVeterinarianSearchInput("");
    setDebouncedVeterinarianSearch("");
    setVeterinarianPage(1);
  }, [isOpen, reset]);

  useEffect(() => {
    if (customerMode !== "new") {
      return;
    }

    setValue("petMode", "new");
    setValue("existingCustomerId", "");
    setValue("existingPetId", "");
  }, [customerMode, setValue]);

  useEffect(() => {
    setValue("existingPetId", "");
  }, [selectedCustomerId, setValue]);

  useEffect(() => {
    if (!selectedPet) {
      return;
    }

    setValue("approximateWeightKg", String(selectedPet.weightKg));
  }, [selectedPet, setValue]);

  useEffect(() => {
    if (locationType !== "1") {
      return;
    }

    setValue("veterinaryClinicId", "");
    setValue("referringVeterinarianId", "");
  }, [locationType, setValue]);

  useEffect(() => {
    setValue("referringVeterinarianId", "");
    setVeterinarianSearchInput("");
    setDebouncedVeterinarianSearch("");
    setVeterinarianPage(1);
  }, [selectedClinicId, setValue]);

  if (!isOpen) {
    return null;
  }

  const belongingsField = register("hasPersonalBelongings");

  function handleBackdropClick() {
    if (!isSubmitting) {
      onClose();
    }
  }

  function handleClinicChange(clinicId: string) {
    setValue("veterinaryClinicId", clinicId, {
      shouldValidate: true,
    });

    setValue("referringVeterinarianId", "", {
      shouldValidate: true,
    });

    if (!clinicId) {
      return;
    }

    const clinic = clinics.find((item) => item.id === clinicId) ?? null;

    if (!clinic) {
      return;
    }

    if (clinic.address) {
      setValue("pickupAddress", clinic.address, {
        shouldDirty: true,
      });
    }

    if (clinic.primaryContactName) {
      setValue("pickupContactName", clinic.primaryContactName, {
        shouldDirty: true,
      });
    }

    if (clinic.phone) {
      setValue("pickupContactPhone", clinic.phone, {
        shouldDirty: true,
      });
    }
  }

  function handleVeterinarianChange(veterinarianId: string) {
    setValue("referringVeterinarianId", veterinarianId, {
      shouldValidate: true,
    });

    if (selectedClinicId || !veterinarianId) {
      return;
    }

    const veterinarian =
      veterinarians.find((item) => item.id === veterinarianId) ?? null;

    if (!veterinarian) {
      return;
    }

    setValue("pickupContactName", `Dr. ${veterinarian.displayName}`, {
      shouldDirty: true,
    });

    if (veterinarian.phone) {
      setValue("pickupContactPhone", veterinarian.phone, {
        shouldDirty: true,
      });
    }
  }

  async function submit(values: CollectionFormValues) {
    await onSubmit(values);
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
        aria-labelledby="collection-form-title"
        className="max-h-full w-full max-w-4xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">
              Recolección y cadena de custodia
            </p>

            <h2
              id="collection-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              Registrar recolección
            </h2>

            <p className="mt-2 text-sm text-slate-500">
              Registra al cliente, la mascota y el lugar donde se realiza la
              recolección.
            </p>
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
          onSubmit={handleSubmit(submit)}
          className="space-y-7 px-6 py-6"
          noValidate
        >
          {/* CLIENTE */}
          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Cliente
            </legend>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="existing"
                  disabled={isSubmitting}
                  {...register("customerMode")}
                />

                <span>
                  <span className="block text-sm font-semibold text-slate-800">
                    Cliente existente
                  </span>

                  <span className="mt-1 block text-xs text-slate-500">
                    Seleccionar un cliente ya registrado.
                  </span>
                </span>
              </label>

              <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="new"
                  disabled={isSubmitting}
                  {...register("customerMode")}
                />

                <span>
                  <span className="block text-sm font-semibold text-slate-800">
                    Cliente nuevo
                  </span>

                  <span className="mt-1 block text-xs text-slate-500">
                    Registrar al propietario durante la recolección.
                  </span>
                </span>
              </label>
            </div>

            {customerMode === "existing" && (
              <div className="mt-5">
                <label className="block text-sm font-medium text-slate-700">
                  Buscar cliente
                </label>

                <input
                  type="search"
                  value={customerSearchInput}
                  onChange={(event) => {
                    setCustomerSearchInput(event.target.value);
                    setValue("existingCustomerId", "");
                    setValue("existingPetId", "");
                  }}
                  disabled={isSubmitting}
                  placeholder="Nombre o teléfono"
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                />

                <label className="mt-5 block text-sm font-medium text-slate-700">
                  Cliente
                </label>

                <select
                  disabled={
                    isSubmitting ||
                    customerSearchPending ||
                    customersQuery.isLoading
                  }
                  {...register("existingCustomerId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900"
                >
                  <option value="">
                    {customersQuery.isLoading
                      ? "Cargando clientes..."
                      : "Selecciona un cliente"}
                  </option>

                  {customers.map((customer) => (
                    <option key={customer.id} value={customer.id}>
                      {customer.displayName} — {customer.phone}
                    </option>
                  ))}
                </select>

                {errors.existingCustomerId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.existingCustomerId.message}
                  </p>
                )}

                {selectedCustomer && (
                  <div className="mt-3 rounded-lg bg-slate-50 px-4 py-3 text-sm text-slate-600">
                    <p>Teléfono: {selectedCustomer.phone}</p>
                  </div>
                )}

                {(customersQuery.data?.totalPages ?? 0) > 1 && (
                  <p className="mt-2 text-xs text-slate-500">
                    Refina la búsqueda para encontrar clientes fuera de los
                    primeros 20 resultados.
                  </p>
                )}
              </div>
            )}

            {customerMode === "new" && (
              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Nombre
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("ownerFirstName")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.ownerFirstName && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.ownerFirstName.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Apellido paterno
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("ownerLastName")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.ownerLastName && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.ownerLastName.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Teléfono
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("ownerPhone")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.ownerPhone && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.ownerPhone.message}
                    </p>
                  )}
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-sm font-medium text-slate-700">
                    Correo electrónico
                  </label>

                  <input
                    type="email"
                    disabled={isSubmitting}
                    {...register("ownerEmail")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.ownerEmail && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.ownerEmail.message}
                    </p>
                  )}
                </div>
              </div>
            )}
          </fieldset>

          {/* MASCOTA */}
          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Mascota
            </legend>

            {customerMode === "existing" && (
              <div className="grid gap-3 sm:grid-cols-2">
                <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                  <input
                    type="radio"
                    value="existing"
                    disabled={isSubmitting}
                    {...register("petMode")}
                  />

                  <span className="text-sm font-semibold text-slate-800">
                    Mascota existente
                  </span>
                </label>

                <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-4">
                  <input
                    type="radio"
                    value="new"
                    disabled={isSubmitting}
                    {...register("petMode")}
                  />

                  <span className="text-sm font-semibold text-slate-800">
                    Mascota nueva
                  </span>
                </label>
              </div>
            )}

            {petMode === "existing" && customerMode === "existing" && (
              <div className="mt-5">
                <label className="block text-sm font-medium text-slate-700">
                  Mascota
                </label>

                <select
                  disabled={
                    isSubmitting ||
                    !selectedCustomerId ||
                    customerPetsQuery.isLoading
                  }
                  {...register("existingPetId")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                >
                  <option value="">
                    {!selectedCustomerId
                      ? "Selecciona primero un cliente"
                      : customerPetsQuery.isLoading
                        ? "Cargando mascotas..."
                        : "Selecciona una mascota"}
                  </option>

                  {customerPets.map((pet) => (
                    <option key={pet.id} value={pet.id}>
                      {pet.name} — {pet.species} · {pet.breed}
                    </option>
                  ))}
                </select>

                {errors.existingPetId && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.existingPetId.message}
                  </p>
                )}
              </div>
            )}

            {petMode === "new" && (
              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Nombre de la mascota
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("petName")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.petName && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.petName.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Especie
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("species")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.species && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.species.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Raza
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("breed")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.breed && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.breed.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Sexo
                  </label>

                  <select
                    disabled={isSubmitting}
                    {...register("sex")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                  >
                    <option value="">Selecciona</option>
                    <option value="Macho">Macho</option>
                    <option value="Hembra">Hembra</option>
                  </select>

                  {errors.sex && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.sex.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Color
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("color")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.color && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.color.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Peso aproximado
                  </label>

                  <input
                    type="number"
                    min="0.01"
                    max="999.99"
                    step="0.01"
                    disabled={isSubmitting}
                    {...register("approximateWeightKg")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.approximateWeightKg && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.approximateWeightKg.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Fecha de fallecimiento
                  </label>

                  <input
                    type="date"
                    max={getMexicoBusinessDate()}
                    disabled={isSubmitting}
                    {...register("dateOfDeath")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  {errors.dateOfDeath && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.dateOfDeath.message}
                    </p>
                  )}
                </div>
              </div>
            )}

            {petMode === "existing" && selectedPet && (
              <div className="mt-4 rounded-lg bg-slate-50 px-4 py-3 text-sm text-slate-600">
                {selectedPet.name} · {selectedPet.species} · {selectedPet.breed}{" "}
                · {selectedPet.weightKg} kg
              </div>
            )}
          </fieldset>

          {/* LUGAR */}
          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Lugar de recolección
            </legend>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="1"
                  disabled={isSubmitting}
                  {...register("locationType")}
                  className="mt-1"
                />

                <span>
                  <span className="block font-semibold text-slate-800">
                    Domicilio del cliente
                  </span>

                  <span className="mt-1 block text-sm text-slate-500">
                    La mascota se recoge directamente con el propietario.
                  </span>
                </span>
              </label>

              <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 p-4">
                <input
                  type="radio"
                  value="2"
                  disabled={isSubmitting}
                  {...register("locationType")}
                  className="mt-1"
                />

                <span>
                  <span className="block font-semibold text-slate-800">
                    Veterinaria
                  </span>

                  <span className="mt-1 block text-sm text-slate-500">
                    La mascota se recoge en una clínica o con un veterinario
                    independiente.
                  </span>
                </span>
              </label>
            </div>

            {locationType === "2" && (
              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Veterinaria
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <input
                    type="search"
                    value={clinicSearchInput}
                    onChange={(event) => {
                      setClinicSearchInput(event.target.value);
                      setValue("veterinaryClinicId", "");
                    }}
                    disabled={isSubmitting}
                    placeholder="Buscar veterinaria"
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  <select
                    value={selectedClinicId}
                    onChange={(event) => handleClinicChange(event.target.value)}
                    disabled={
                      isSubmitting ||
                      clinicSearchPending ||
                      clinicsQuery.isLoading
                    }
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                  >
                    <option value="">Sin veterinaria</option>

                    {clinics.map((clinic) => (
                      <option key={clinic.id} value={clinic.id}>
                        {clinic.displayName}
                      </option>
                    ))}
                  </select>

                  <CollectionLookupPagination
                    page={clinicPage}
                    totalPages={clinicsQuery.data?.totalPages ?? 0}
                    isFetching={clinicSearchPending || clinicsQuery.isFetching}
                    onPageChange={setClinicPage}
                  />

                  {errors.veterinaryClinicId && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.veterinaryClinicId.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Veterinario
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <input
                    type="search"
                    value={veterinarianSearchInput}
                    onChange={(event) => {
                      setVeterinarianSearchInput(event.target.value);
                      setValue("referringVeterinarianId", "");
                    }}
                    disabled={isSubmitting}
                    placeholder="Buscar veterinario"
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />

                  <select
                    {...register("referringVeterinarianId")}
                    onChange={(event) =>
                      handleVeterinarianChange(event.target.value)
                    }
                    disabled={
                      isSubmitting ||
                      veterinarianSearchPending ||
                      veterinariansQuery.isLoading
                    }
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                  >
                    <option value="">
                      {selectedClinicId
                        ? "Sin veterinario referente"
                        : "Selecciona veterinario independiente"}
                    </option>

                    {veterinarians.map((veterinarian) => (
                      <option key={veterinarian.id} value={veterinarian.id}>
                        Dr. {veterinarian.displayName}
                      </option>
                    ))}
                  </select>

                  <CollectionLookupPagination
                    page={veterinarianPage}
                    totalPages={veterinariansQuery.data?.totalPages ?? 0}
                    isFetching={
                      veterinarianSearchPending || veterinariansQuery.isFetching
                    }
                    onPageChange={setVeterinarianPage}
                  />

                  {errors.referringVeterinarianId && (
                    <p className="mt-2 text-sm text-red-600">
                      {errors.referringVeterinarianId.message}
                    </p>
                  )}
                </div>
              </div>
            )}

            <div className="mt-5">
              <label className="block text-sm font-medium text-slate-700">
                Dirección de recolección
              </label>

              <textarea
                rows={3}
                maxLength={500}
                disabled={isSubmitting}
                {...register("pickupAddress")}
                placeholder={
                  locationType === "1"
                    ? "Dirección del domicilio"
                    : "Dirección donde se recogerá la mascota"
                }
                className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5"
              />

              {errors.pickupAddress && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.pickupAddress.message}
                </p>
              )}
            </div>

            <div className="mt-5 grid gap-5 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-slate-700">
                  Contacto en recolección
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <input
                  disabled={isSubmitting}
                  {...register("pickupContactName")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700">
                  Teléfono del contacto
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <input
                  disabled={isSubmitting}
                  {...register("pickupContactPhone")}
                  className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                />

                {errors.pickupContactPhone && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.pickupContactPhone.message}
                  </p>
                )}
              </div>
            </div>
          </fieldset>

          {/* CUSTODIA */}
          <fieldset className="rounded-xl border border-slate-200 p-5">
            <legend className="px-2 text-sm font-semibold text-slate-800">
              Cadena de custodia
            </legend>

            {petMode === "existing" && (
              <div className="mb-5">
                <label className="block text-sm font-medium text-slate-700">
                  Peso aproximado
                  <span className="ml-1 font-normal text-slate-400">
                    (opcional)
                  </span>
                </label>

                <div className="relative mt-2">
                  <input
                    type="number"
                    min="0.01"
                    max="999.99"
                    step="0.01"
                    disabled={isSubmitting}
                    {...register("approximateWeightKg")}
                    className="block w-full rounded-lg border border-slate-300 px-3 py-2.5 pr-12"
                  />

                  <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm text-slate-500">
                    kg
                  </span>
                </div>

                {errors.approximateWeightKg && (
                  <p className="mt-2 text-sm text-red-600">
                    {errors.approximateWeightKg.message}
                  </p>
                )}
              </div>
            )}

            <label className="flex items-start gap-3">
              <input
                type="checkbox"
                disabled={isSubmitting}
                {...belongingsField}
                onChange={(event) => {
                  belongingsField.onChange(event);

                  if (!event.target.checked) {
                    clearErrors("personalBelongingsDescription");

                    setValue("personalBelongingsDescription", "");
                  }
                }}
                className="mt-1 h-4 w-4 rounded border-slate-300"
              />

              <span>
                <span className="block text-sm font-medium text-slate-800">
                  Se recogieron objetos personales
                </span>

                <span className="mt-1 block text-sm text-slate-500">
                  Collar, placa, manta, transportadora u otros artículos.
                </span>
              </span>
            </label>

            {hasPersonalBelongings && (
              <div className="mt-4">
                <label className="block text-sm font-medium text-slate-700">
                  Descripción de los objetos
                </label>

                <textarea
                  rows={3}
                  maxLength={500}
                  disabled={isSubmitting}
                  {...register("personalBelongingsDescription")}
                  className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5"
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
            <label className="block text-sm font-medium text-slate-700">
              Notas
              <span className="ml-1 font-normal text-slate-400">
                (opcional)
              </span>
            </label>

            <textarea
              rows={4}
              maxLength={1000}
              disabled={isSubmitting}
              {...register("notes")}
              className="mt-2 block w-full resize-y rounded-lg border border-slate-300 px-3 py-2.5"
            />

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
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={
                isSubmitting ||
                customersQuery.isLoading ||
                clinicsQuery.isLoading ||
                veterinariansQuery.isLoading
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Registrando..." : "Registrar recolección"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
