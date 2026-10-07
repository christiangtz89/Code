import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import toast from "react-hot-toast";
import { hasPermission } from "../../auth/utils/permissions";
import { revalidateStartPayment } from "../api/cremationsApi";
import {
  CremationStatus,
  type Cremation,
  type RevalidateStartPaymentRequest,
  type RevalidateStartPaymentResult,
} from "../types/cremation.types";

export function canRevalidateStartPayment(cremation: Cremation): boolean {
  return (
    hasPermission("Cremations.Manage") &&
    cremation.isActive &&
    (cremation.status === CremationStatus.Pending ||
      cremation.status === CremationStatus.Scheduled) &&
    cremation.requiredStartPaymentAmount === null &&
    !cremation.startedAt &&
    !cremation.completedAt &&
    !cremation.readyForDeliveryAt &&
    !cremation.deliveredAt
  );
}

interface RevalidationAttempt {
  cremation: Cremation;
  request: RevalidateStartPaymentRequest;
  preview: RevalidateStartPaymentResult | null;
}

export function useStartPaymentRevalidation() {
  const queryClient = useQueryClient();
  const [attempt, setAttempt] = useState<RevalidationAttempt | null>(null);
  const [isPending, setIsPending] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const [isUncertain, setIsUncertain] = useState(false);
  // Synchronous guard also covers clicks before React's next render.
  const busy = useRef(false);
  const activeAttempt = useRef<RevalidationAttempt | null>(null);
  const returnFocus = useRef<HTMLElement | null>(null);

  async function send(next: RevalidationAttempt) {
    if (busy.current || !hasPermission("Cremations.Manage")) return;
    busy.current = true;
    activeAttempt.current = next;
    setAttempt(next);
    setIsPending(true);

    let result: RevalidateStartPaymentResult;
    try {
      result = await revalidateStartPayment(next.cremation.id, next.request);
    } catch (error) {
      const status = axios.isAxiosError(error) ? error.response?.status : null;
      // A missing response or server/proxy error cannot prove no commit occurred.
      if (!status || status >= 500 || status === 408) {
        setIsUncertain(true);
        setIsOpen(true);
      } else {
        const data = axios.isAxiosError(error) ? error.response?.data : null;
        toast.error(
          typeof data === "string"
            ? data
            : (data?.detail ??
                data?.message ??
                data?.title ??
                "No fue posible validar el pago requerido. Intenta nuevamente."),
        );
        activeAttempt.current = null;
        setAttempt(null);
        setIsOpen(false);
        setIsUncertain(false);
      }
      busy.current = false;
      setIsPending(false);
      return;
    }

    if (result.applied) {
      activeAttempt.current = null;
      setAttempt(null);
      setIsOpen(false);
      setIsUncertain(false);
      toast.success("Pago requerido validado correctamente.");
      // Refresh errors must never turn a known application into a new attempt.
      await Promise.allSettled([
        queryClient.invalidateQueries({ queryKey: ["cremations"] }),
        queryClient.invalidateQueries({ queryKey: ["payments"] }),
      ]);
      if (document.activeElement === document.body) {
        document.getElementById("cremations-title")?.focus();
      }
    } else if (result.priceConfirmationRequired) {
      const reviewed = { ...next, preview: result };
      activeAttempt.current = reviewed;
      setAttempt(reviewed);
      setIsUncertain(false);
      setIsOpen(true);
    } else {
      // Retain the request if a response does not establish a known outcome.
      setIsUncertain(true);
      setIsOpen(true);
    }
    busy.current = false;
    setIsPending(false);
  }

  function start(cremation: Cremation) {
    if (
      activeAttempt.current ||
      busy.current ||
      !canRevalidateStartPayment(cremation)
    )
      return;
    returnFocus.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    void send({
      cremation,
      request: { requestId: crypto.randomUUID(), confirmPriceChange: false },
      preview: null,
    });
  }

  function confirm() {
    if (!attempt?.preview || isUncertain) return;
    const preview = attempt.preview;
    void send({
      ...attempt,
      request: {
        requestId: attempt.request.requestId,
        confirmPriceChange: true,
        expectedCurrentQuotedPrice: preview.previousQuotedPrice,
        expectedNewQuotedPrice: preview.newQuotedPrice,
        expectedCremationPriceId: preview.selectedCremationPriceId,
        expectedRequiredStartPaymentAmount: preview.requiredStartPaymentAmount,
        expectedCurrentPaymentAccountServiceTotal:
          preview.previousPaymentAccountServiceTotal,
      },
    });
  }

  function close() {
    if (busy.current) return;
    setIsOpen(false);
    // Closing an uncertain result does not abandon its idempotency evidence.
    if (!isUncertain) {
      activeAttempt.current = null;
      setAttempt(null);
    }
  }

  return {
    attempt,
    isPending,
    isOpen,
    isUncertain,
    returnFocus: returnFocus.current,
    start,
    confirm,
    close,
    reopen: () => {
      returnFocus.current =
        document.activeElement instanceof HTMLElement
          ? document.activeElement
          : null;
      setIsOpen(true);
    },
    retry: () => {
      if (attempt && isUncertain) void send(attempt);
    },
  };
}
