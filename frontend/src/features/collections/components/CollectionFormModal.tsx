import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo } from "react";
import { useForm, useWatch } from "react-hook-form";

import {
  getCustomerPets,
  getCustomers,
} from "../../customers/api/customersApi";
import { getVeterinarians } from "../../veterinarians/api/veterinariansApi";
import { getVeterinarianFullName } from "../../veterinarians/utils/veterinarianName";
import { getVeterinaryClinics } from "../../veterinary-clinics/api/veterinaryClinicsApi";

import {
  collectionSchema,
  type CollectionFormValues,
} from "../schemas/collectionSchema";

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
  ownerSecondLastName: "",
  ownerPhone: "",
  ownerEmail: "",

  petName: "",
  species: "",
  breed: "",
  sex: "",
  color: "",
  approximateWeightKg: "",
  ageYears: "",
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

  const customersQuery = useQuery({
    queryKey: ["customers", "collection-options", true],

    queryFn: () =>
      getCustomers({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const customerPetsQuery = useQuery({
    queryKey: ["pets", "collection-customer-options", selectedCustomerId],

    queryFn: () => getCustomerPets(selectedCustomerId),

    enabled:
      isOpen && customerMode === "existing" && selectedCustomerId.length > 0,
  });

  const clinicsQuery = useQuery({
    queryKey: ["veterinary-clinics", "collection-options", true],

    queryFn: () =>
      getVeterinaryClinics({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const veterinariansQuery = useQuery({
    queryKey: ["veterinarians", "collection-options", true],

    queryFn: () =>
      getVeterinarians({
        page: 1,
        pageSize: 100,
        isActive: true,
      }),

    enabled: isOpen,
  });

  const customers = useMemo(
    () =>
      [...(customersQuery.data?.items ?? [])].sort((first, second) => {
        const firstName = [
          first.firstName,
          first.lastName,
          first.secondLastName,
        ]
          .filter(Boolean)
          .join(" ");

        const secondName = [
          second.firstName,
          second.lastName,
          second.secondLastName,
        ]
          .filter(Boolean)
          .join(" ");

        return firstName.localeCompare(secondName, "es-MX");
      }),
    [customersQuery.data?.items],
  );

  const customerPets = useMemo(
    () =>
      [...(customerPetsQuery.data ?? [])].sort((first, second) =>
        first.name.localeCompare(second.name, "es-MX"),
      ),
    [customerPetsQuery.data],
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

  const availableVeterinarians = useMemo(() => {
    if (selectedClinicId) {
      return veterinarians.filter(
        (veterinarian) => veterinarian.veterinaryClinicId === selectedClinicId,
      );
    }

    return veterinarians.filter(
      (veterinarian) => veterinarian.veterinaryClinicId === null,
    );
  }, [selectedClinicId, veterinarians]);

  const selectedCustomer =
    customers.find((customer) => customer.id === selectedCustomerId) ?? null;

  const selectedPet =
    customerPets.find((pet) => pet.id === selectedPetId) ?? null;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset(defaultValues);
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

    setValue(
      "pickupContactName",
      `Dr. ${getVeterinarianFullName(veterinarian)}`,
      {
        shouldDirty: true,
      },
    );

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
                  Cliente
                </label>

                <select
                  disabled={isSubmitting || customersQuery.isLoading}
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
                      {[
                        customer.firstName,
                        customer.lastName,
                        customer.secondLastName,
                      ]
                        .filter(Boolean)
                        .join(" ")}
                      {" — "}
                      {customer.phone}
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

                    <p className="mt-1">Correo: {selectedCustomer.email}</p>
                  </div>
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
                    Apellido materno
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <input
                    disabled={isSubmitting}
                    {...register("ownerSecondLastName")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />
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
                    Edad
                    <span className="ml-1 font-normal text-slate-400">
                      (años)
                    </span>
                  </label>

                  <input
                    type="number"
                    min="0"
                    max="100"
                    disabled={isSubmitting}
                    {...register("ageYears")}
                    className="mt-2 block w-full rounded-lg border border-slate-300 px-3 py-2.5"
                  />
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

                  <select
                    value={selectedClinicId}
                    onChange={(event) => handleClinicChange(event.target.value)}
                    disabled={isSubmitting || clinicsQuery.isLoading}
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                  >
                    <option value="">Sin veterinaria</option>

                    {clinics.map((clinic) => (
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
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">
                    Veterinario
                    <span className="ml-1 font-normal text-slate-400">
                      (opcional)
                    </span>
                  </label>

                  <select
                    {...register("referringVeterinarianId")}
                    onChange={(event) =>
                      handleVeterinarianChange(event.target.value)
                    }
                    disabled={isSubmitting || veterinariansQuery.isLoading}
                    className="mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5"
                  >
                    <option value="">
                      {selectedClinicId
                        ? "Sin veterinario referente"
                        : "Selecciona veterinario independiente"}
                    </option>

                    {availableVeterinarians.map((veterinarian) => (
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
