import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";

import {
  getVeterinaryRequestCustomerOptions,
  getVeterinaryRequestPetOptions,
} from "../api/veterinaryRequestsApi";
import {
  veterinaryRequestConversionSchema,
  type VeterinaryRequestConversionFormValues,
} from "../schemas/veterinaryRequestConversionSchema";
import type { VeterinaryRequest } from "../types/veterinaryRequest.types";

type ConversionMode = "new" | "existingCustomer" | "existingPet";

interface VeterinaryRequestConversionModalProps {
  isOpen: boolean;
  request: VeterinaryRequest | null;

  isSubmitting: boolean;

  onClose: () => void;

  onSubmit: (values: VeterinaryRequestConversionFormValues) => void;
}

export function VeterinaryRequestConversionModal({
  isOpen,
  request,
  isSubmitting,
  onClose,
  onSubmit,
}: VeterinaryRequestConversionModalProps) {
  const [conversionMode, setConversionMode] = useState<ConversionMode>("new");
  const [customerSearchInput, setCustomerSearchInput] = useState("");
  const [debouncedCustomerSearch, setDebouncedCustomerSearch] = useState("");
  const [customerPage, setCustomerPage] = useState(1);
  const [petSearchInput, setPetSearchInput] = useState("");
  const [debouncedPetSearch, setDebouncedPetSearch] = useState("");
  const [petPage, setPetPage] = useState(1);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors },
  } = useForm<VeterinaryRequestConversionFormValues>({
    resolver: zodResolver(veterinaryRequestConversionSchema),

    defaultValues: {
      existingCustomerId: "",
      existingPetId: "",
      verifiedWeightKg: 0,
      hasPersonalBelongings: false,
      personalBelongingsDescription: "",
      referralNotes: "",
      notes: "",
    },
  });

  const normalizedCustomerSearch = debouncedCustomerSearch.trim();
  const normalizedPetSearch = debouncedPetSearch.trim();
  const customerSearchPending =
    customerSearchInput.trim() !== normalizedCustomerSearch;
  const petSearchPending = petSearchInput.trim() !== normalizedPetSearch;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setCustomerPage(1);
      setDebouncedCustomerSearch(customerSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [customerSearchInput]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setPetPage(1);
      setDebouncedPetSearch(petSearchInput);
    }, 400);

    return () => window.clearTimeout(timeoutId);
  }, [petSearchInput]);

  const customersQuery = useQuery({
    queryKey: [
      "veterinary-request-customer-options",
      { search: normalizedCustomerSearch, page: customerPage },
    ],
    queryFn: () =>
      getVeterinaryRequestCustomerOptions({
        search: normalizedCustomerSearch || undefined,
        page: customerPage,
        pageSize: 20,
      }),
    enabled: isOpen && conversionMode === "existingCustomer",
  });

  const petsQuery = useQuery({
    queryKey: [
      "veterinary-request-pet-options",
      { search: normalizedPetSearch, page: petPage },
    ],
    queryFn: () =>
      getVeterinaryRequestPetOptions({
        search: normalizedPetSearch || undefined,
        page: petPage,
        pageSize: 20,
      }),
    enabled: isOpen && conversionMode === "existingPet",
  });

  useEffect(() => {
    if (!isOpen || !request) {
      return;
    }

    setConversionMode("new");
    setCustomerSearchInput("");
    setDebouncedCustomerSearch("");
    setCustomerPage(1);
    setPetSearchInput("");
    setDebouncedPetSearch("");
    setPetPage(1);

    reset({
      existingCustomerId: "",
      existingPetId: "",

      verifiedWeightKg: request.approximateWeightKg,

      hasPersonalBelongings: false,

      personalBelongingsDescription: "",

      referralNotes: request.requestNotes ?? "",

      notes: "",
    });
  }, [isOpen, request, reset]);

  if (!isOpen || !request) {
    return null;
  }

  function changeMode(mode: ConversionMode) {
    setConversionMode(mode);

    setValue("existingCustomerId", "");

    setValue("existingPetId", "");
  }

  const customers = customersQuery.data?.items ?? [];
  const pets = petsQuery.data?.items ?? [];

  const submitConversion = handleSubmit((values) => {
    if (conversionMode === "existingCustomer" && !values.existingCustomerId) {
      setError("existingCustomerId", {
        type: "manual",
        message: "Selecciona un cliente existente.",
      });

      return;
    }

    if (conversionMode === "existingPet" && !values.existingPetId) {
      setError("existingPetId", {
        type: "manual",
        message: "Selecciona una mascota existente.",
      });

      return;
    }

    onSubmit(values);
  });

  const newCustomerNeedsEmail = conversionMode === "new" && !request.ownerEmail;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div className="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white shadow-xl">
        <header className="sticky top-0 z-10 flex items-start justify-between border-b border-slate-200 bg-white px-6 py-5">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              Crear recepción
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              {request.petName} · {request.ownerFirstName}{" "}
              {request.ownerLastName}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="rounded-lg px-3 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100"
          >
            Cerrar
          </button>
        </header>

        <form onSubmit={submitConversion} className="space-y-6 p-6">
          <section>
            <h3 className="font-semibold text-slate-900">
              ¿Cómo deseas registrar al cliente y mascota?
            </h3>

            <div className="mt-4 grid gap-3 md:grid-cols-3">
              <button
                type="button"
                onClick={() => changeMode("new")}
                className={[
                  "rounded-xl border p-4 text-left transition",
                  conversionMode === "new"
                    ? "border-slate-900 bg-slate-50"
                    : "border-slate-200 hover:bg-slate-50",
                ].join(" ")}
              >
                <p className="font-semibold text-slate-900">Nuevo cliente</p>

                <p className="mt-1 text-xs text-slate-500">
                  Crear cliente y mascota con la información de la solicitud.
                </p>
              </button>

              <button
                type="button"
                onClick={() => changeMode("existingCustomer")}
                className={[
                  "rounded-xl border p-4 text-left transition",
                  conversionMode === "existingCustomer"
                    ? "border-slate-900 bg-slate-50"
                    : "border-slate-200 hover:bg-slate-50",
                ].join(" ")}
              >
                <p className="font-semibold text-slate-900">
                  Cliente existente
                </p>

                <p className="mt-1 text-xs text-slate-500">
                  Usar cliente registrado y crear una mascota nueva.
                </p>
              </button>

              <button
                type="button"
                onClick={() => changeMode("existingPet")}
                className={[
                  "rounded-xl border p-4 text-left transition",
                  conversionMode === "existingPet"
                    ? "border-slate-900 bg-slate-50"
                    : "border-slate-200 hover:bg-slate-50",
                ].join(" ")}
              >
                <p className="font-semibold text-slate-900">
                  Mascota existente
                </p>

                <p className="mt-1 text-xs text-slate-500">
                  Usar mascota y cliente ya registrados.
                </p>
              </button>
            </div>
          </section>

          {conversionMode === "new" && (
            <section className="rounded-xl border border-slate-200 bg-slate-50 p-4">
              <p className="font-medium text-slate-900">Se crearán:</p>

              <p className="mt-2 text-sm text-slate-600">
                Cliente:{" "}
                {[
                  request.ownerFirstName,
                  request.ownerLastName,
                  request.ownerSecondLastName,
                ]
                  .filter(Boolean)
                  .join(" ")}
              </p>

              <p className="mt-1 text-sm text-slate-600">
                Mascota: {request.petName}
              </p>

              {newCustomerNeedsEmail && (
                <div className="mt-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  Esta solicitud no tiene correo electrónico del propietario.
                  Agrega el correo antes de crear un cliente nuevo.
                </div>
              )}
            </section>
          )}

          {conversionMode === "existingCustomer" && (
            <div>
              <label className="text-sm font-medium text-slate-700">
                Cliente existente *
              </label>

              <select
                {...register("existingCustomerId")}
                disabled={customersQuery.isFetching || customerSearchPending}
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
              >
                <option value="">Seleccionar cliente</option>

                {customers.map((customer) => (
                  <option key={customer.id} value={customer.id}>
                    {customer.displayName}
                  </option>
                ))}
              </select>

              <input
                type="search"
                value={customerSearchInput}
                onChange={(event) => setCustomerSearchInput(event.target.value)}
                placeholder="Buscar cliente por nombre"
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />

              <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                <button
                  type="button"
                  onClick={() =>
                    setCustomerPage((current) => Math.max(1, current - 1))
                  }
                  disabled={customerPage <= 1 || customersQuery.isFetching}
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Anterior
                </button>
                <span>
                  Página {customerPage} de{" "}
                  {Math.max(customersQuery.data?.totalPages ?? 0, 1)}
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setCustomerPage((current) =>
                      Math.min(
                        current + 1,
                        Math.max(customersQuery.data?.totalPages ?? 0, 1),
                      ),
                    )
                  }
                  disabled={
                    customersQuery.isFetching ||
                    customerPage >=
                      Math.max(customersQuery.data?.totalPages ?? 0, 1)
                  }
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Siguiente
                </button>
              </div>

              {errors.existingCustomerId && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.existingCustomerId.message}
                </p>
              )}
            </div>
          )}

          {conversionMode === "existingPet" && (
            <div>
              <label className="text-sm font-medium text-slate-700">
                Mascota existente *
              </label>

              <select
                {...register("existingPetId")}
                disabled={petsQuery.isFetching || petSearchPending}
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm"
              >
                <option value="">Seleccionar mascota</option>

                {pets.map((pet) => (
                  <option key={pet.id} value={pet.id}>
                    {pet.displayName}
                  </option>
                ))}
              </select>

              <input
                type="search"
                value={petSearchInput}
                onChange={(event) => setPetSearchInput(event.target.value)}
                placeholder="Buscar mascota o propietario"
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              />

              <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                <button
                  type="button"
                  onClick={() =>
                    setPetPage((current) => Math.max(1, current - 1))
                  }
                  disabled={petPage <= 1 || petsQuery.isFetching}
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Anterior
                </button>
                <span>
                  Página {petPage} de{" "}
                  {Math.max(petsQuery.data?.totalPages ?? 0, 1)}
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setPetPage((current) =>
                      Math.min(
                        current + 1,
                        Math.max(petsQuery.data?.totalPages ?? 0, 1),
                      ),
                    )
                  }
                  disabled={
                    petsQuery.isFetching ||
                    petPage >= Math.max(petsQuery.data?.totalPages ?? 0, 1)
                  }
                  className="rounded border border-slate-300 px-2 py-1 disabled:opacity-40"
                >
                  Siguiente
                </button>
              </div>

              {errors.existingPetId && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.existingPetId.message}
                </p>
              )}
            </div>
          )}

          <section className="rounded-2xl border border-slate-200 p-5">
            <h3 className="font-semibold text-slate-900">Recepción física</h3>

            <div className="mt-5">
              <label className="text-sm font-medium text-slate-700">
                Peso verificado (kg) *
              </label>

              <input
                type="number"
                step="0.01"
                min="0.01"
                {...register("verifiedWeightKg", {
                  valueAsNumber: true,
                })}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
              />

              {errors.verifiedWeightKg && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.verifiedWeightKg.message}
                </p>
              )}
            </div>

            <label className="mt-5 flex items-center gap-3">
              <input
                type="checkbox"
                {...register("hasPersonalBelongings")}
                className="h-4 w-4"
              />

              <span className="text-sm font-medium text-slate-700">
                Se recibieron objetos personales
              </span>
            </label>

            <div className="mt-4">
              <label className="text-sm font-medium text-slate-700">
                Descripción de objetos personales
              </label>

              <textarea
                rows={3}
                {...register("personalBelongingsDescription")}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
              />

              {errors.personalBelongingsDescription && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.personalBelongingsDescription.message}
                </p>
              )}
            </div>

            <div className="mt-4">
              <label className="text-sm font-medium text-slate-700">
                Notas de referencia
              </label>

              <textarea
                rows={3}
                {...register("referralNotes")}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
              />
            </div>

            <div className="mt-4">
              <label className="text-sm font-medium text-slate-700">
                Notas de recepción
              </label>

              <textarea
                rows={3}
                {...register("notes")}
                className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm"
              />
            </div>
          </section>

          <div className="rounded-xl border border-violet-200 bg-violet-50 p-4 text-sm text-violet-800">
            Al confirmar, PCMS generará automáticamente una recepción y su
            código QR.
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-200 pt-5">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-700"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={isSubmitting || newCustomerNeedsEmail}
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSubmitting ? "Creando recepción..." : "Crear recepción"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
