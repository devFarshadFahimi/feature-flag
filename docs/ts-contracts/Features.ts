/** Features frontend contract. Baseline: 2026-10-26.
 * All actions require a bearer JWT. Filter lists with optional projectId; there is no pagination.
 * Create type/update type are numeric enums; response type/lifecycle are strings.
 * Update description/type null means leave unchanged; description=null does not clear the value.
 * Route/body ids must match. Encode tag as one URL path component for removal.
 * Create does not initialize feature-environment links and bypasses Project.AddFeature's limit checks.
 * Existing EF indexes include a global unique name index and a filtered project/name index.
 * EF limits: name 255, description 2000. Do not expose SDK evaluation/variant editing as implemented endpoints.
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid, IsoDateTime, FeatureType, SelectiveDTO } from "./Common";

/** Source: src/Application/Features/Features/Commands/AddFeatureTag/AddFeatureTagCommand.cs. */
export interface AddFeatureTagCommand {
  featureId: Uuid;
  tag: string;
}

/** Source: src/Application/Features/Features/Commands/ArchiveFeature/ArchiveFeatureCommand.cs. */
export interface ArchiveFeatureCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Features/Commands/CreateFeature/CreateFeatureCommand.cs. */
export interface CreateFeatureCommand {
  projectId: Uuid;
  name: string;
  type: FeatureType;
  /** Server default: null. */
  description?: string | null;
}

/** Source: src/Application/Features/Features/Commands/EnableFeatureImpressionData/EnableFeatureImpressionDataCommand.cs. */
export interface EnableFeatureImpressionDataCommand {
  id: Uuid;
  enabled: boolean;
}

/** Source: src/Application/Features/Features/Commands/MarkFeatureAsStale/MarkFeatureAsStaleCommand.cs. */
export interface MarkFeatureAsStaleCommand {
  id: Uuid;
  isStale: boolean;
}

/** Source: src/Application/Features/Features/Commands/RemoveFeatureTag/RemoveFeatureTagCommand.cs. */
export interface RemoveFeatureTagCommand {
  featureId: Uuid;
  tag: string;
}

/** Source: src/Application/Features/Features/Commands/UpdateFeature/UpdateFeatureCommand.cs. */
export interface UpdateFeatureCommand {
  id: Uuid;
  description: string | null;
  type: FeatureType | null;
}

/** Source: src/Application/Features/Features/Queries/GetAllFeatures/GetAllFeaturesQuery.cs. */
export interface GetAllFeaturesQuery {
  /** Server default: null. */
  projectId?: Uuid | null;
}

/** Source: src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs. */
export interface GetFeatureByIdQuery {
  id: Uuid;
}

/** Source: src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs. */
export interface FeatureResponse {
  id: Uuid;
  projectId: Uuid;
  name: string;
  type: string;
  description: string | null;
  lifecycle: string;
  isStale: boolean;
  impressionDataEnabled: boolean;
  createdAt: IsoDateTime;
  archivedAt: IsoDateTime | null;
  tags: (string)[];
  environments: (FeatureEnvironmentResponse)[];
}

/** Source: src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs. */
export interface FeatureEnvironmentResponse {
  id: Uuid;
  environmentId: Uuid;
  enabled: boolean;
  lastSeenAt: IsoDateTime | null;
  strategies: (FeatureStrategyResponse)[];
}

/** Source: src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs. */
export interface FeatureStrategyResponse {
  id: Uuid;
  type: string;
  sortOrder: number;
  parameters: FeatureStrategyParametersResponse;
  constraints: (ConstraintResponse)[];
  segmentIds: (number)[];
}

/** Source: src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs. */
export interface ConstraintResponse {
  contextName: string;
  operator: string;
  values: (string)[];
  inverted: boolean;
  caseInsensitive: boolean;
}

/** Both read paths supply these keys, with normalized defaults (0, "default", "").
 * customParameters is present in Features projections, omitted by the standalone
 * FeatureEnvironments query. These normalized values do not imply every input is supported. */
export interface FeatureStrategyParametersResponse {
  rolloutPercentage: number;
  stickiness: string;
  groupId: string;
  userIds: string[];
  ipAddresses: string[];
  applicationNames: string[];
  customParameters?: Record<string, unknown>;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface FeaturesEndpoints {
  /** POST /api/v1/Features; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/Features"; auth: "bearer";
    pathParams: never; query: never; body: CreateFeatureCommand;
    success: Uuid; successStatus: 200;
  };
  /** GET /api/v1/Features; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/Features"; auth: "bearer";
    pathParams: never; query: { projectId?: Uuid | null }; body: never;
    success: FeatureResponse[]; successStatus: 200;
  };
  /** GET /api/v1/Features/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/Features/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: FeatureResponse; successStatus: 200;
  };
  /** PUT /api/v1/Features/{id}; bearer JWT required. */
  update: {
    method: "PUT"; path: "/api/v1/Features/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: UpdateFeatureCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Features/{id}/archive; bearer JWT required. */
  archive: {
    method: "POST"; path: "/api/v1/Features/{id}/archive"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** PUT /api/v1/Features/{id}/stale; bearer JWT required. */
  markStale: {
    method: "PUT"; path: "/api/v1/Features/{id}/stale"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: MarkFeatureAsStaleCommand;
    success: void; successStatus: 200;
  };
  /** PUT /api/v1/Features/{id}/impression-data; bearer JWT required. */
  impressionData: {
    method: "PUT"; path: "/api/v1/Features/{id}/impression-data"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: EnableFeatureImpressionDataCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Features/{id}/tags; bearer JWT required. */
  addTag: {
    method: "POST"; path: "/api/v1/Features/{id}/tags"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: AddFeatureTagCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Features/{id}/tags/{tag}; bearer JWT required. */
  removeTag: {
    method: "DELETE"; path: "/api/v1/Features/{id}/tags/{tag}"; auth: "bearer";
    pathParams: { id: Uuid; tag: string }; query: never; body: never;
    success: void; successStatus: 200;
  };
}

/** New configuration fields returned on feature and environment reads. */
export interface Prerequisite { featureId: Uuid; variant?: string | null; }
export interface VariantResponse { name: string; weight: number; stickiness: string | null; payload: Record<string, unknown>; isDefault: boolean; }
export interface FeatureResponse { revision: number; prerequisites: Prerequisite[] | null; }
export interface FeatureEnvironmentResponse { revision: number; variants: VariantResponse[] | null; }
export interface FeatureStrategyResponse { revision: number; enabled: boolean; variants: VariantResponse[] | null; }
/** PUT /api/v1/Features/{id}/prerequisites, project-write permission. Same-project, acyclic, depth<=20.
 * Body featureId must match path; expectedRevision is the feature revision. 200 empty,409 conflict/protection. */
export interface SetPrerequisitesCommand { featureId: Uuid; expectedRevision: number; prerequisites: Prerequisite[]; }
export interface FeaturesEndpoints {
  prerequisites: { method: "PUT"; path: "/api/v1/Features/{id}/prerequisites"; auth: "bearer"; pathParams: { id: Uuid }; query: never; body: SetPrerequisitesCommand; success: void; successStatus: 200 };
}

/** Feature dropdown: GET /api/v1/Features/selective?projectId=<UUID>; bearer JWT required.
 * Optional projectId defaults to null (all visible projects); omit it rather than sending "null".
 * No path parameters/body. HTTP 200 returns a raw SelectiveDTO<Uuid>[]; [] means no matching features.
 * key is the non-null feature UUID; value is its non-null name. Ordered by database name collation,
 * then UUID. No pagination/search; includes archived features and features disabled in an environment.
 * Admin/RootAdmin see all projects; other active users see only project memberships, including Viewer.
 * Explicit unknown project: 404; inaccessible project: 403; malformed UUID: 400; invalid session: 401.
 * Inspect HTTP status before parsing; errors contain Persian messages/ProblemDetails where available.
 *
 * Frontend: replace related Feature ID text inputs with a dropdown displaying value and binding key.
 * For a project-scoped form, load Projects/selective first, then pass the selected projectId here.
 * Clear the selected feature and reload options whenever the project/account changes; ignore stale
 * responses from earlier requests. Revalidate saved edit selections against the returned keys.
 * Submit key as featureId (or the destination request's documented ID field), never the display name.
 * Evaluation endpoints instead require featureNames; use their documented contract, not these UUIDs.
 * Show loading/empty/error states and disable required submission without a valid selection.
 * Reload after feature creation/rename and clear options on logout. Selection does not grant write access.
 */
export type FeatureSelectiveDTO = SelectiveDTO<Uuid>;

export interface FeaturesEndpoints {
  selective: {
    method: "GET"; path: "/api/v1/Features/selective"; auth: "bearer";
    pathParams: never; query: { projectId?: Uuid | null }; body: never;
    success: FeatureSelectiveDTO[]; successStatus: 200;
  };
}
