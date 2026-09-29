import {
  ReceptionHistoryStage,
  ReceptionLifecycleAction,
  ReceptionLifecycleBlockReason,
  ReceptionLifecycleDependency,
  ReceptionLifecycleOutcome,
  type ReceptionHistoryStage as ReceptionHistoryStageValue,
  type ReceptionLifecycleAction as ReceptionLifecycleActionValue,
  type ReceptionLifecycleBlockReason as ReceptionLifecycleBlockReasonValue,
  type ReceptionLifecycleOutcome as ReceptionLifecycleOutcomeValue,
} from "../types/reception.types";

export function getReceptionLifecycleActionLabel(
  action: ReceptionLifecycleActionValue,
): string {
  switch (action) {
    case ReceptionLifecycleAction.DeactivationRequested:
      return "Solicitud de desactivación";
    case ReceptionLifecycleAction.Deactivate:
      return "Desactivación";
    case ReceptionLifecycleAction.Restore:
      return "Restauración";
    default:
      return "Acción de ciclo de vida";
  }
}

export function getReceptionLifecycleOutcomeLabel(
  outcome: ReceptionLifecycleOutcomeValue,
): string {
  switch (outcome) {
    case ReceptionLifecycleOutcome.Recorded:
      return "Registrada";
    case ReceptionLifecycleOutcome.Succeeded:
      return "Realizada";
    case ReceptionLifecycleOutcome.Blocked:
      return "Bloqueada";
    default:
      return "Resultado no disponible";
  }
}

export function getReceptionLifecycleStageLabel(
  stage: ReceptionHistoryStageValue,
): string {
  switch (stage) {
    case ReceptionHistoryStage.BeforeCremation:
      return "Antes de la cremación";
    case ReceptionHistoryStage.CremationCreatedNotStarted:
      return "Cremación creada, sin iniciar";
    case ReceptionHistoryStage.CremationStarted:
      return "Cremación iniciada";
    case ReceptionHistoryStage.CremationCompleted:
      return "Cremación completada";
    default:
      return "Etapa operativa no disponible";
  }
}

const dependencyLabels: ReadonlyArray<readonly [number, string]> = [
  [ReceptionLifecycleDependency.Collection, "Recolección"],
  [
    ReceptionLifecycleDependency.ConvertedVeterinaryRequest,
    "Solicitud veterinaria",
  ],
  [ReceptionLifecycleDependency.Cremation, "Cremación"],
  [ReceptionLifecycleDependency.PaymentAccount, "Cuenta de pago"],
  [ReceptionLifecycleDependency.PaymentHistory, "Historial de pagos"],
  [
    ReceptionLifecycleDependency.DirectReceptionEvidence,
    "Evidencia de recepción",
  ],
  [ReceptionLifecycleDependency.CollectionEvidence, "Evidencia de recolección"],
];

export function getReceptionLifecycleDependencyLabels(
  dependencies: number,
): string[] {
  return dependencyLabels
    .filter(([flag]) => (dependencies & flag) === flag)
    .map(([, label]) => label);
}

export function getReceptionLifecycleBlockReasonLabel(
  reason: ReceptionLifecycleBlockReasonValue,
): string | null {
  switch (reason) {
    case ReceptionLifecycleBlockReason.Dependencies:
      return "La recepción conserva actividad operativa vinculada.";
    case ReceptionLifecycleBlockReason.ReceptionState:
      return "El estado actual de la recepción no permite esta acción.";
    default:
      return null;
  }
}
