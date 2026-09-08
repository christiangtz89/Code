import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { petSchema, type PetFormValues } from "../schemas/petSchema";
import type { Pet, PetOwnerOption } from "../types/pet.types";
import { getLocalDateInputValue, toDateInputValue } from "../utils/petDates";

interface PetFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  pet: Pet | null;
  ownerOptions: PetOwnerOption[];
  ownerOptionsLoading: boolean;
  ownerOptionsError: boolean;
  ownerSearch: string;
  ownerPage: number;
  ownerTotalPages: number;
  isSubmitting: boolean;
  onOwnerSearchChange: (value: string) => void;
  onOwnerPageChange: (page: number) => void;
  onClose: () => void;
  onSubmit: (values: PetFormValues) => Promise<void>;
}

const inputClassName =
  "mt-2 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200 disabled:bg-slate-100 disabled:text-slate-500";

export function PetFormModal({
  isOpen,
  mode,
  pet,
  ownerOptions,
  ownerOptionsLoading,
  ownerOptionsError,
  ownerSearch,
  ownerPage,
  ownerTotalPages,
  isSubmitting,
  onOwnerSearchChange,
  onOwnerPageChange,
  onClose,
  onSubmit,
}: PetFormModalProps) {
  const {
    register,
    reset,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<PetFormValues>({
    resolver: zodResolver(petSchema),
    defaultValues: {
      customerId: "",
      name: "",
      species: "",
      breed: "",
      sex: "",
      color: "",
      weightKg: "",
      ageYears: "",
      dateOfDeath: "",
    },
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reset({
      customerId: pet?.customerId ?? "",
      name: pet?.name ?? "",
      species: pet?.species ?? "",
      breed: pet?.breed ?? "",
      sex: pet?.sex ?? "",
      color: pet?.color ?? "",
      weightKg: pet?.weightKg !== undefined ? String(pet.weightKg) : "",
      ageYears:
        pet?.ageYears !== null && pet?.ageYears !== undefined
          ? String(pet.ageYears)
          : "",
      dateOfDeath: pet ? toDateInputValue(pet.dateOfDeath) : "",
    });
  }, [isOpen, pet, reset]);

  if (!isOpen) {
    return null;
  }

  return (
    <div
      role="presentation"
      className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/60 px-4 py-8"
      onMouseDown={() => {
        if (!isSubmitting) {
          onClose();
        }
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="pet-form-title"
        className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-slate-500">Mascotas</p>

            <h2
              id="pet-form-title"
              className="mt-1 text-xl font-semibold text-slate-900"
            >
              {mode === "create" ? "Registrar mascota" : "Editar mascota"}
            </h2>
          </div>

          <button
            type="button"
            aria-label="Cerrar formulario"
            disabled={isSubmitting}
            onClick={onClose}
            className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-900 disabled:opacity-50"
          >
            ✕
          </button>
        </header>

        <form
          noValidate
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-6 px-6 py-6"
        >
          <div>
            <label
              htmlFor="pet-customer"
              className="block text-sm font-medium text-slate-700"
            >
              Propietario
            </label>

            {mode === "create" ? (
              <>
                <input
                  id="pet-owner-search"
                  type="search"
                  value={ownerSearch}
                  placeholder="Buscar propietario por nombre"
                  disabled={isSubmitting}
                  onChange={(event) => {
                    setValue("customerId", "");
                    onOwnerSearchChange(event.target.value);
                  }}
                  className={inputClassName}
                />

                <select
                  id="pet-customer"
                  disabled={isSubmitting || ownerOptionsLoading}
                  {...register("customerId")}
                  className={inputClassName}
                >
                  <option value="">
                    {ownerOptionsLoading
                      ? "Buscando propietarios..."
                      : ownerOptions.length === 0
                        ? "No se encontraron propietarios activos"
                        : "Selecciona un propietario"}
                  </option>

                  {ownerOptions.map((owner) => (
                    <option key={owner.id} value={owner.id}>
                      {owner.displayName}
                    </option>
                  ))}
                </select>

                {ownerOptionsError && (
                  <p className="mt-2 text-sm text-red-600">
                    No fue posible buscar propietarios.
                  </p>
                )}

                {ownerTotalPages > 1 && (
                  <div className="mt-3 flex items-center justify-between gap-3 text-sm">
                    <button
                      type="button"
                      disabled={ownerPage <= 1 || ownerOptionsLoading}
                      onClick={() => {
                        setValue("customerId", "");
                        onOwnerPageChange(ownerPage - 1);
                      }}
                      className="rounded-lg border border-slate-300 px-3 py-2 text-slate-700 disabled:opacity-40"
                    >
                      Anterior
                    </button>

                    <span className="text-slate-500">
                      Página {ownerPage} de {ownerTotalPages}
                    </span>

                    <button
                      type="button"
                      disabled={
                        ownerPage >= ownerTotalPages || ownerOptionsLoading
                      }
                      onClick={() => {
                        setValue("customerId", "");
                        onOwnerPageChange(ownerPage + 1);
                      }}
                      className="rounded-lg border border-slate-300 px-3 py-2 text-slate-700 disabled:opacity-40"
                    >
                      Siguiente
                    </button>
                  </div>
                )}
              </>
            ) : (
              <>
                <input
                  id="pet-customer"
                  type="text"
                  value={pet?.customerName ?? "Propietario no disponible"}
                  disabled
                  className={inputClassName}
                />

                <input type="hidden" {...register("customerId")} />

                <p className="mt-2 text-xs text-slate-500">
                  El propietario no puede cambiarse después del registro.
                </p>
              </>
            )}

            {errors.customerId && (
              <p className="mt-2 text-sm text-red-600">
                {errors.customerId.message}
              </p>
            )}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <div>
              <label
                htmlFor="pet-name"
                className="block text-sm font-medium text-slate-700"
              >
                Nombre
              </label>

              <input
                id="pet-name"
                type="text"
                disabled={isSubmitting}
                {...register("name")}
                className={inputClassName}
              />

              {errors.name && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-species"
                className="block text-sm font-medium text-slate-700"
              >
                Especie
              </label>

              <input
                id="pet-species"
                type="text"
                list="pet-species-options"
                placeholder="Perro, gato..."
                disabled={isSubmitting}
                {...register("species")}
                className={inputClassName}
              />

              <datalist id="pet-species-options">
                <option value="Perro" />
                <option value="Gato" />
                <option value="Ave" />
                <option value="Conejo" />
                <option value="Hámster" />
                <option value="Reptil" />
                <option value="Otro" />
              </datalist>

              {errors.species && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.species.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-breed"
                className="block text-sm font-medium text-slate-700"
              >
                Raza
              </label>

              <input
                id="pet-breed"
                type="text"
                placeholder="Mestizo, Labrador..."
                disabled={isSubmitting}
                {...register("breed")}
                className={inputClassName}
              />

              {errors.breed && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.breed.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-sex"
                className="block text-sm font-medium text-slate-700"
              >
                Sexo
              </label>

              <select
                id="pet-sex"
                disabled={isSubmitting}
                {...register("sex")}
                className={inputClassName}
              >
                <option value="">Selecciona una opción</option>
                <option value="Macho">Macho</option>
                <option value="Hembra">Hembra</option>
                <option value="No especificado">No especificado</option>
              </select>

              {errors.sex && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.sex.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-color"
                className="block text-sm font-medium text-slate-700"
              >
                Color
              </label>

              <input
                id="pet-color"
                type="text"
                placeholder="Negro, café, blanco..."
                disabled={isSubmitting}
                {...register("color")}
                className={inputClassName}
              />

              {errors.color && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.color.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-weight"
                className="block text-sm font-medium text-slate-700"
              >
                Peso, en kg
              </label>

              <input
                id="pet-weight"
                type="number"
                min="0.01"
                max="999.99"
                step="0.01"
                placeholder="12.50"
                disabled={isSubmitting}
                {...register("weightKg")}
                className={inputClassName}
              />

              {errors.weightKg && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.weightKg.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-age"
                className="block text-sm font-medium text-slate-700"
              >
                Edad en años
              </label>

              <input
                id="pet-age"
                type="number"
                min="0"
                max="100"
                step="1"
                placeholder="Opcional"
                disabled={isSubmitting}
                {...register("ageYears")}
                className={inputClassName}
              />

              {errors.ageYears && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.ageYears.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="pet-date-of-death"
                className="block text-sm font-medium text-slate-700"
              >
                Fecha de fallecimiento
              </label>

              <input
                id="pet-date-of-death"
                type="date"
                max={getLocalDateInputValue()}
                disabled={isSubmitting}
                {...register("dateOfDeath")}
                className={inputClassName}
              />

              {errors.dateOfDeath && (
                <p className="mt-2 text-sm text-red-600">
                  {errors.dateOfDeath.message}
                </p>
              )}
            </div>
          </div>

          <footer className="flex flex-col-reverse gap-3 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              disabled={isSubmitting}
              onClick={onClose}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            >
              Cancelar
            </button>

            <button
              type="submit"
              disabled={
                isSubmitting ||
                ownerOptionsLoading ||
                (mode === "create" && ownerOptions.length === 0)
              }
              className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting
                ? "Guardando..."
                : mode === "create"
                  ? "Registrar mascota"
                  : "Guardar cambios"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
