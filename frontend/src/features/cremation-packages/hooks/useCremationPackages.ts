import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  createCremationPackage,
  getCremationPackageById,
  getCremationPackages,
  getPublicCremationPackages,
  updateCremationPackage,
} from "../api";

export const cremationPackageKeys = {
  all: ["cremation-packages"] as const,

  lists: () => [...cremationPackageKeys.all, "list"] as const,

  list: (includeInactive: boolean) =>
    [...cremationPackageKeys.lists(), { includeInactive }] as const,

  publicList: () => [...cremationPackageKeys.all, "public"] as const,

  details: () => [...cremationPackageKeys.all, "detail"] as const,

  detail: (id: string) => [...cremationPackageKeys.details(), id] as const,
};

export function useCremationPackages(includeInactive = false) {
  return useQuery({
    queryKey: cremationPackageKeys.list(includeInactive),

    queryFn: () => getCremationPackages(includeInactive),
  });
}

export function usePublicCremationPackages() {
  return useQuery({
    queryKey: cremationPackageKeys.publicList(),

    queryFn: getPublicCremationPackages,
  });
}

export function useCremationPackage(id: string) {
  return useQuery({
    queryKey: cremationPackageKeys.detail(id),

    queryFn: () => getCremationPackageById(id),

    enabled: Boolean(id),
  });
}

export function useCreateCremationPackage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createCremationPackage,

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: cremationPackageKeys.all,
      });
    },
  });
}

export function useUpdateCremationPackage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: Parameters<typeof updateCremationPackage>[1];
    }) => updateCremationPackage(id, payload),

    onSuccess: async (updatedPackage) => {
      queryClient.setQueryData(
        cremationPackageKeys.detail(updatedPackage.id),
        updatedPackage,
      );

      await queryClient.invalidateQueries({
        queryKey: cremationPackageKeys.lists(),
      });

      await queryClient.invalidateQueries({
        queryKey: cremationPackageKeys.publicList(),
      });
    },
  });
}
