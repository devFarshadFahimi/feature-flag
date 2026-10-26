/** GET /api/v1/History: optional projectId (omit/null for GLOBAL history, Admin/RootAdmin only),
 * optional environmentId, page=1, pageSize=50/max200. Project history requires membership.
 * 200 ConfigurationHistory[]. before/after are JSON TEXT, not embedded objects; parse explicitly.
 * POST /api/v1/History/{id}/restore: project-write permission; body RestoreHistoryInput, 200 UUID of a
 * NEW draft change request. Submit/review/apply it normally; history is never overwritten.
 * Current automatic restore supports Feature metadata and FeatureEnvironment enabled state.
 * Other resource types require an explicit change action. No credentials/context are exposed.
 * Persian errors: 400 invalid/unsupported restore, 401/403 access, 404 record, 409 revision, 503 unavailable.
 */
import type { Uuid, IsoDateTime } from "./Common";
export interface ConfigurationHistory {
  id: Uuid; projectId: Uuid | null; environmentId: Uuid | null; actorId: Uuid | null;
  timestamp: IsoDateTime; resourceType: string; resourceId: string; operation: string;
  revision: number; before: string | null; after: string | null;
}
export interface RestoreHistoryInput { projectId: Uuid; environmentId: Uuid; expectedRevision: number; }
