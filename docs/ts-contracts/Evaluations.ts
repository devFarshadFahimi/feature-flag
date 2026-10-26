/** Ordinary JWT evaluation API. Browsers call their own authenticated backend; do not store an
 * application credential or expose configuration rules. POST /api/v1/Evaluations returns 200 with
 * EvaluationResponse. POST /api/v1/Evaluations/preview requires project-write permission and never
 * records usage. Runtime evaluation requires active project membership or Admin/RootAdmin.
 * Errors: Persian ProblemDetails; 400 invalid input, 401 invalid/revoked session, 403 no membership,
 * 404 unknown environment, 503 unavailable current authorization/configuration. Never treat 401/403 as false.
 * Body <=64 KiB; appName 1..128; <=100 explicit featureNames; omitted names evaluate <=1,000 flags.
 * Names/keys are ordinal. Properties <=50, each value <=1,024 characters. Runtime ignores currentTime;
 * preview can simulate it. Missing names use defaults[name] or false; permission failures never do.
 */
import type { Uuid, IsoDateTime } from "./Common";

export interface EvaluationContext {
  userId?: string | null;
  sessionId?: string | null;
  remoteAddress?: string | null;
  applicationHostname?: string | null;
  currentTime?: IsoDateTime | null;
  properties?: Record<string, string> | null;
}
export interface EvaluateFlagsCommand {
  projectId: Uuid;
  environmentId: Uuid;
  appName: string;
  context: EvaluationContext;
  featureNames?: string[] | null;
  defaults?: Record<string, boolean> | null;
  /** The route determines preview mode; this field cannot turn the runtime route into preview. */
  preview?: boolean;
}
export interface EvaluatedVariant {
  name: string;
  enabled: boolean;
  payload: Record<string, unknown> | null;
}
export interface FlagDecision {
  name: string;
  enabled: boolean;
  variant: EvaluatedVariant;
  reason: string;
  revision: number;
  /** Null for normal evaluation; present only for a matching preview strategy. */
  matchedStrategyId: Uuid | null;
}
export interface EvaluationResponse {
  features: FlagDecision[];
  evaluatedAt: IsoDateTime;
  snapshotLoadedAt: IsoDateTime;
  stale: boolean;
  refreshAfterSeconds: number;
  maxStaleSeconds: number;
}
export interface EvaluationsEndpoints {
  evaluate: { method: "POST"; path: "/api/v1/Evaluations"; auth: "bearer"; body: EvaluateFlagsCommand; success: EvaluationResponse; successStatus: 200 };
  preview: { method: "POST"; path: "/api/v1/Evaluations/preview"; auth: "bearer"; body: EvaluateFlagsCommand; success: EvaluationResponse; successStatus: 200 };
}
