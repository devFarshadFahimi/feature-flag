/** JWT/project-scoped governance. Actor identity always comes from the accepted JWT; legacy createdBy/
 * reviewerId input cannot impersonate another account. projectId=null creates a Global request (Admin+).
 * Global approval/apply/schedule requires a different active RootAdmin approval; project approvers are
 * owners with global Editor+, or Admin+. Editing invalidates approvals and dependent stages.
 * Payload remains JSON TEXT inside payload, using the version-1 ChangePayload below. Submit validates
 * scope/revisions; apply mutates all items/history atomically. Protected writes require approved requests.
 * Schedule requires future UTC; executor polls every5s and rechecks permissions/revisions. Transient
 * failures retry at most5 times; conflicts suspend. Linked stages carry predecessor result revisions.
 * All failures are Persian. Success shapes remain UUID/200-empty/current response as declared below.
 */
import type { Uuid, IsoDateTime } from "./Common";

/** Source: src/Application/Features/ChangeRequests/Commands/AddChangeRequestItem/AddChangeRequestItemCommand.cs. */
export interface AddChangeRequestItemCommand {
  changeRequestId: Uuid;
  action: ChangeAction;
  /** Server default: null. */
  featureId?: Uuid | null;
  /** Server default: null. */
  payload: string;
}

/** Source: src/Application/Features/ChangeRequests/Commands/ApplyChangeRequest/ApplyChangeRequestCommand.cs. */
export interface ApplyChangeRequestCommand {
  id: Uuid;
}

/** Source: src/Application/Features/ChangeRequests/Commands/ApproveChangeRequest/ApproveChangeRequestCommand.cs. */
export interface ApproveChangeRequestCommand {
  id: Uuid;
  reviewerId: Uuid;
}

/** Source: src/Application/Features/ChangeRequests/Commands/CancelChangeRequest/CancelChangeRequestCommand.cs. */
export interface CancelChangeRequestCommand {
  id: Uuid;
}

/** Source: src/Application/Features/ChangeRequests/Commands/CreateChangeRequest/CreateChangeRequestCommand.cs. */
export interface CreateChangeRequestCommand {
  projectId: Uuid | null;
  environmentId: Uuid;
  createdBy: Uuid;
  /** Server default: null. */
  title?: string | null;
  /** Server default: null. */
  description?: string | null;
}

/** Source: src/Application/Features/ChangeRequests/Commands/RejectChangeRequest/RejectChangeRequestCommand.cs. */
export interface RejectChangeRequestCommand {
  id: Uuid;
  reviewerId: Uuid;
  /** Server default: null. */
  reason?: string | null;
}

/** Source: src/Application/Features/ChangeRequests/Commands/ScheduleChangeRequest/ScheduleChangeRequestCommand.cs. */
export interface ScheduleChangeRequestCommand {
  id: Uuid;
  scheduledAt: IsoDateTime;
  predecessorId?: Uuid | null;
}

/** Source: src/Application/Features/ChangeRequests/Commands/SubmitChangeRequestForReview/SubmitChangeRequestForReviewCommand.cs. */
export interface SubmitChangeRequestForReviewCommand {
  id: Uuid;
}

/** Source: src/Application/Features/ChangeRequests/Queries/GetAllChangeRequests/GetAllChangeRequestsQuery.cs. */
export interface GetAllChangeRequestsQuery {
  /** Server default: null. */
  projectId?: Uuid | null;
  /** Server default: null. */
  status?: string | null;
}

/** Source: src/Application/Features/ChangeRequests/Queries/GetChangeRequestById/GetChangeRequestByIdQuery.cs. */
export interface GetChangeRequestByIdQuery {
  id: Uuid;
}

/** Source: src/Application/Features/ChangeRequests/Queries/GetChangeRequestById/GetChangeRequestByIdQuery.cs. */
export interface ChangeRequestResponse {
  id: Uuid;
  projectId: Uuid | null;
  environmentId: Uuid;
  createdBy: Uuid;
  status: string;
  title: string | null;
  description: string | null;
  scheduledAt: IsoDateTime | null;
  createdAt: IsoDateTime;
  reviewedAt: IsoDateTime | null;
  reviewedBy: Uuid | null;
  items: (ChangeRequestItemResponse)[];
}

/** Source: src/Application/Features/ChangeRequests/Queries/GetChangeRequestById/GetChangeRequestByIdQuery.cs. */
export interface ChangeRequestItemResponse {
  id: Uuid;
  action: string;
  featureId: Uuid | null;
  payload: string | null;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface ChangeRequestsEndpoints {
  /** POST /api/v1/ChangeRequests; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/ChangeRequests"; auth: "bearer";
    pathParams: never; query: never; body: CreateChangeRequestCommand;
    success: Uuid; successStatus: 200;
  };
  /** GET /api/v1/ChangeRequests; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/ChangeRequests"; auth: "bearer";
    pathParams: never; query: { projectId?: Uuid | null; status?: string | null }; body: never;
    success: ChangeRequestResponse[]; successStatus: 200;
  };
  /** GET /api/v1/ChangeRequests/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/ChangeRequests/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: ChangeRequestResponse; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/items; bearer JWT required. */
  addItem: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/items"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: AddChangeRequestItemCommand;
    success: Uuid; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/submit; bearer JWT required. */
  submit: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/submit"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/approve; bearer JWT required. */
  approve: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/approve"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: ApproveChangeRequestCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/reject; bearer JWT required. */
  reject: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/reject"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: RejectChangeRequestCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/apply; bearer JWT required. */
  apply: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/apply"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/schedule; bearer JWT required. */
  schedule: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/schedule"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: ScheduleChangeRequestCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/ChangeRequests/{id}/cancel; bearer JWT required. */
  cancel: {
    method: "POST"; path: "/api/v1/ChangeRequests/{id}/cancel"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
}

export type ChangeAction = "setEnabled" | "upsertStrategy" | "deleteStrategy" | "reorderStrategies" | "replaceVariants" | "replaceSegmentConstraints" | "updateFeature" | "setPrerequisites" | "setProjectProtection" | "setGlobalProtection" | "restoreFeature" | "restoreEnvironment";
/** JSON.stringify this object into AddChangeRequestItemCommand.payload. expectedRevision is FE revision
 * for flag environment operations, feature revision for metadata/prerequisites, segment revision for
 * constraints, policy revision for project protection, or environment revision for global protection.
 * Global requests permit only shared segment constraints and global protection actions.
 * Multiple items targeting one resource must use the same baseline expected revision. */
export interface ChangePayload {
  version: 1; expectedRevision: number; enabled?: boolean | null;
  strategy?: import("./FeatureEnvironments").ConfigureStrategyCommand | null;
  strategyId?: Uuid | null; strategyIds?: Uuid[] | null;
  variants?: import("./FeatureEnvironments").VariantInput[] | null;
  segmentId?: number | null; constraints?: import("./Segments").ConstraintDto[] | null;
  description?: string | null; type?: import("./Common").FeatureType | null;
  lifecycle?: import("./Common").FeatureLifecycle | null;
  prerequisites?: import("./Features").Prerequisite[] | null;
  requiredApprovals?: number | null; protectionEnabled?: boolean | null;
  featureState?: FeatureConfigurationState | null;
  environmentState?: EnvironmentConfigurationState | null;
}
/** Full configuration restore actions require the corresponding state. restoreFeature uses the feature
 * revision; restoreEnvironment uses the feature/environment revision. Both follow normal approvals.
 * Restoring an environment recreates strategy IDs and normalizes sortOrder to consecutive ranks,
 * preserving the snapshot's SortOrder/ID evaluation order; refresh strategy forms after application. */
export interface FeatureConfigurationState {
  description: string | null; type: import("./Common").FeatureType;
  lifecycle: import("./Common").FeatureLifecycle; isStale: boolean;
  impressionDataEnabled: boolean; tags: string[]; prerequisites: import("./Features").Prerequisite[];
}
export interface EnvironmentConfigurationState {
  enabled: boolean; strategies: import("./FeatureEnvironments").ConfigureStrategyCommand[];
  variants: import("./FeatureEnvironments").VariantInput[];
}
/** GET /api/v1/ChangeRequests/{id}/preview; JWT, same scope/read permissions as get.
 * 200 before/after per item in application order, with numeric enums. Never persists mutations,
 * changes status, emits audit rows, or records metrics. Current revisions must match (409 otherwise).
 * 400 invalid/incomplete payload, 401/403 denied, 404 missing resource, 503 dependency failure.
 * Feature actions show FeatureConfigurationState; environment/strategy actions show complete
 * EnvironmentConfigurationState. Segment actions show {id,name,description,constraints}; protection
 * actions show {protected} or {enabled,requiredApprovals}. Global scope remains Admin/RootAdmin only. */
export interface ChangeRequestPreview {
  id: Uuid; requiredApprovals: number;
  items: Array<{ action: ChangeAction; featureId: Uuid | null; before: unknown; after: unknown }>;
}
export interface ChangeRequestsEndpoints {
  preview: {
    method: "GET"; path: "/api/v1/ChangeRequests/{id}/preview"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: ChangeRequestPreview; successStatus: 200;
  };
}
export interface ChangeRequestResponse {
  revision: number; scope: "Project" | "Global"; requiredApprovals: number; reviewers: Uuid[] | null;
  rejectionReason: string | null; failureReason: string | null; attempts: number;
  nextAttemptAt: IsoDateTime | null; predecessorId: Uuid | null; scheduledBy: Uuid | null;
  capturedRevisions: Record<string,number> | null; resultRevisions: Record<string,number> | null;
}

/** Set predecessor on creation, before review. Schedule may omit it or repeat the same UUID; it cannot change it. */
export interface CreateChangeRequestCommand { predecessorId?: Uuid | null; }

/** updateFeature.lifecycle accepts Active, Completed or Archived. replaceVariants.strategyId targets a strategy; omit it for environment fallback variants. Shared-resource approval thresholds use the strongest affected environment policy. */
