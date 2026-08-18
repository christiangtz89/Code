import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  createCremationPrice,
  getCremationPriceById,
  getCremationPriceQuote,
  getCremationPrices,
  getCremationPricingConfiguration,
  type GetCremationPriceQuoteParams,
  type GetCremationPricesParams,
  updateCremationPrice,
  updateCremationPricingConfiguration,
} from "../api";

export const cremationPricingKeys = {
  all: ["cremation-pricing"] as const,

  configuration: () => [...cremationPricingKeys.all, "configuration"] as const,

  prices: () => [...cremationPricingKeys.all, "prices"] as const,

  priceList: (params: GetCremationPricesParams) =>
    [...cremationPricingKeys.prices(), params] as const,

  priceDetail: (id: string) =>
    [...cremationPricingKeys.prices(), "detail", id] as const,

  quote: (params: GetCremationPriceQuoteParams | null) =>
    [...cremationPricingKeys.all, "quote", params] as const,
};

export function useCremationPricingConfiguration() {
  return useQuery({
    queryKey: cremationPricingKeys.configuration(),

    queryFn: getCremationPricingConfiguration,
  });
}

export function useUpdateCremationPricingConfiguration() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: updateCremationPricingConfiguration,

    onSuccess: async (configuration) => {
      queryClient.setQueryData(
        cremationPricingKeys.configuration(),
        configuration,
      );

      await queryClient.invalidateQueries({
        queryKey: cremationPricingKeys.all,
      });
    },
  });
}

export function useCremationPrices(params: GetCremationPricesParams = {}) {
  return useQuery({
    queryKey: cremationPricingKeys.priceList(params),

    queryFn: () => getCremationPrices(params),
  });
}

export function useCremationPrice(id: string) {
  return useQuery({
    queryKey: cremationPricingKeys.priceDetail(id),

    queryFn: () => getCremationPriceById(id),

    enabled: Boolean(id),
  });
}

export function useCreateCremationPrice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createCremationPrice,

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: cremationPricingKeys.prices(),
      });

      await queryClient.invalidateQueries({
        queryKey: [...cremationPricingKeys.all, "quote"],
      });
    },
  });
}

export function useUpdateCremationPrice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: Parameters<typeof updateCremationPrice>[1];
    }) => updateCremationPrice(id, payload),

    onSuccess: async (price) => {
      queryClient.setQueryData(
        cremationPricingKeys.priceDetail(price.id),
        price,
      );

      await queryClient.invalidateQueries({
        queryKey: cremationPricingKeys.prices(),
      });

      await queryClient.invalidateQueries({
        queryKey: [...cremationPricingKeys.all, "quote"],
      });
    },
  });
}

export function useCremationPriceQuote(
  params: GetCremationPriceQuoteParams | null,
) {
  return useQuery({
    queryKey: cremationPricingKeys.quote(params),

    queryFn: () => {
      if (!params) {
        throw new Error("No se proporcionaron datos para cotizar.");
      }

      return getCremationPriceQuote(params);
    },

    enabled:
      params !== null &&
      Boolean(params.cremationPackageId) &&
      params.weightKg > 0,
  });
}
