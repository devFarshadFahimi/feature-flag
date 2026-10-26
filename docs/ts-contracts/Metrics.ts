/** JWT/project-scoped usage, not proof that a browser rendered a feature. No raw targeting context is
 * stored. GET /api/v1/Metrics: required projectId/environmentId/from/to (UTC, to>from, <=31 days),
 * optional featureName/appName, page=1, pageSize=50 (max200). Returns 200 FlagUsage[].
 * GET /api/v1/Metrics/applications: projectId/environmentId plus same paging, returns 200 ApplicationUsage[].
 * POST /api/v1/Metrics/impressions: 200 empty body, <=100 counts, counts 0..1,000,000,000,
 * UUID batchId deduplicated within project/environment for 7 days. Retry the SAME batchId.
 * Hourly aggregates retain 90 days. Persian 400/401/403/404/409/503 errors use ProblemDetails.
 */
import type { Uuid, IsoDateTime } from "./Common";
export interface ImpressionInput { featureName: string; enabled: boolean; variant: string; count: number; }
export interface ReportImpressionsCommand {
  projectId: Uuid; environmentId: Uuid; appName: string; batchId: Uuid; counts: ImpressionInput[];
}
export interface FlagUsage extends ImpressionInput {
  projectId: Uuid; environmentId: Uuid; appName: string; hour: IsoDateTime; lastSeenAt: IsoDateTime; impression: boolean;
}
export interface ApplicationUsage { appName: string; lastSeenAt: IsoDateTime; }
export interface MetricsQuery {
  projectId: Uuid; environmentId: Uuid; from: IsoDateTime; to: IsoDateTime;
  featureName?: string; appName?: string; page?: number; pageSize?: number;
}
