import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  createUrn,
  getPublicUrns,
  getUrnById,
  getUrns,
  updateUrn,
} from "../api";

export const urnKeys = {
  all: ["urns"] as const,

  lists: () => [...urnKeys.all, "list"] as const,

  list: (includeInactive: boolean) =>
    [...urnKeys.lists(), { includeInactive }] as const,

  publicList: () => [...urnKeys.all, "public"] as const,

  details: () => [...urnKeys.all, "detail"] as const,

  detail: (id: string) => [...urnKeys.details(), id] as const,
};

export function useUrns(includeInactive = false) {
  return useQuery({
    queryKey: urnKeys.list(includeInactive),

    queryFn: () => getUrns(includeInactive),
  });
}

export function usePublicUrns() {
  return useQuery({
    queryKey: urnKeys.publicList(),

    queryFn: getPublicUrns,
  });
}

export function useUrn(id: string) {
  return useQuery({
    queryKey: urnKeys.detail(id),

    queryFn: () => getUrnById(id),

    enabled: Boolean(id),
  });
}

export function useCreateUrn() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createUrn,

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: urnKeys.all,
      });
    },
  });
}

export function useUpdateUrn() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: Parameters<typeof updateUrn>[1];
    }) => updateUrn(id, payload),

    onSuccess: async (updatedUrn) => {
      queryClient.setQueryData(urnKeys.detail(updatedUrn.id), updatedUrn);

      await queryClient.invalidateQueries({
        queryKey: urnKeys.lists(),
      });

      await queryClient.invalidateQueries({
        queryKey: urnKeys.publicList(),
      });
    },
  });
}
