/** GET /api/v1/History: optional projectId (omit/null for GLOBAL history, Admin/RootAdmin only),
 * optional environmentId, page=1, pageSize=50/max200. Project history requires membership.
 * 200 ConfigurationHistory[]. before/after are JSON TEXT, not embedded objects; parse explicitly.
 * POST /api/v1/History/{id}/restore: project-write permission; body RestoreHistoryInput, 200 UUID of a
 * NEW draft change request. Submit/review/apply it normally; history is never overwritten.
 * New Feature snapshots restore metadata, lifecycle, tags, stale/impression settings and prerequisites.
 * New FeatureEnvironment snapshots restore enabled state, strategies/constraints/segments and variants.
 * FeatureStrategy restores its complete configuration (deleted strategies receive a new ID);
 * Segment restores constraints; ProjectEnvironmentPolicy and Environment restore protection settings.
 * Global restores use projectId:null and normal independent RootAdmin approval at apply.
 * Strategy restore expects its parent feature/environment revision; other restore inputs use the
 * target feature, feature/environment, segment, policy or global environment revision respectively.
 * before/after retain PascalCase snapshot keys, including the nested Configuration object; enums
 * inside snapshots are numeric. Restore does not accept client-edited snapshots: it reads the row.
 * Legacy Feature records restore only description/type; legacy FeatureEnvironment only enabled.
 * Legacy strategy/segment rows without Configuration and unrelated resource types return 400.
 * Creation rows (before:null) cannot be restored. No credentials/context are exposed.
 * Persian errors: 400 invalid/unsupported restore, 401/403 access, 404 record, 409 revision, 503 unavailable.
 */
import type { Uuid, IsoDateTime } from "./Common";
export interface ConfigurationHistory {
  id: Uuid; projectId: Uuid | null; environmentId: Uuid | null; actorId: Uuid | null;
  timestamp: IsoDateTime; resourceType: string; resourceId: string; operation: string;
  revision: number; before: string | null; after: string | null;
}
export interface RestoreHistoryInput { projectId: Uuid | null; environmentId: Uuid; expectedRevision: number; }
