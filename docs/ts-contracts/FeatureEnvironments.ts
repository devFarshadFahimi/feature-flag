/** Feature-environment management requires an active JWT and project-write permission (global Editor
 * with Member/Owner membership, or Admin/RootAdmin); reads require membership. New flags/environments
 * initialize associations disabled. Protected configuration returns Persian 409 and requires a reviewed
 * change request. Both rollout enum values use 0..100 percentage and stable assignment. Custom is rejected.
 * PUT strategies provides complete constraints/segments/variants and expectedRevision; refetch after 409.
 * Legacy POST strategies remains available; use PUT for precise complete configuration.
 */
import type { Uuid, StrategyType } from "./Common";
import type { FeatureEnvironmentResponse } from "./Features";

/** Source: src/Application/Features/FeatureEnvironments/Commands/AddFeatureStrategy/AddFeatureStrategyCommand.cs. */
export interface AddFeatureStrategyCommand {
  featureId: Uuid;
  environmentId: Uuid;
  type: StrategyType;
  /** Server default: null. */
  rolloutPercentage?: number | null;
  /** Server default: null. */
  stickiness?: string | null;
  /** Server default: null. */
  groupId?: string | null;
  /** Server default: null. */
  userIds?: (string)[] | null;
  /** Server default: null. */
  ipAddresses?: (string)[] | null;
  /** Server default: null. */
  applicationNames?: (string)[] | null;
}

/** Source: src/Application/Features/FeatureEnvironments/Commands/DisableFeatureEnvironment/DisableFeatureEnvironmentCommand.cs. */
export interface DisableFeatureEnvironmentCommand {
  featureId: Uuid;
  environmentId: Uuid;
}

/** Source: src/Application/Features/FeatureEnvironments/Commands/EnableFeatureEnvironment/EnableFeatureEnvironmentCommand.cs. */
export interface EnableFeatureEnvironmentCommand {
  featureId: Uuid;
  environmentId: Uuid;
}

/** Source: src/Application/Features/FeatureEnvironments/Commands/RemoveFeatureStrategy/RemoveFeatureStrategyCommand.cs. */
export interface RemoveFeatureStrategyCommand {
  featureId: Uuid;
  environmentId: Uuid;
  strategyId: Uuid;
}

/** Source: src/Application/Features/FeatureEnvironments/Queries/GetFeatureEnvironmentById/GetFeatureEnvironmentByIdQuery.cs. */
export interface GetFeatureEnvironmentByIdQuery {
  featureId: Uuid;
  environmentId: Uuid;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface FeatureEnvironmentsEndpoints {
  /** GET /api/v1/features/{featureId}/environments/{environmentId}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/features/{featureId}/environments/{environmentId}"; auth: "bearer";
    pathParams: { featureId: Uuid; environmentId: Uuid }; query: never; body: never;
    success: FeatureEnvironmentResponse; successStatus: 200;
  };
  /** POST /api/v1/features/{featureId}/environments/{environmentId}/enable; bearer JWT required. */
  enable: {
    method: "POST"; path: "/api/v1/features/{featureId}/environments/{environmentId}/enable"; auth: "bearer";
    pathParams: { featureId: Uuid; environmentId: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/features/{featureId}/environments/{environmentId}/disable; bearer JWT required. */
  disable: {
    method: "POST"; path: "/api/v1/features/{featureId}/environments/{environmentId}/disable"; auth: "bearer";
    pathParams: { featureId: Uuid; environmentId: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/features/{featureId}/environments/{environmentId}/strategies; bearer JWT required. */
  addStrategy: {
    method: "POST"; path: "/api/v1/features/{featureId}/environments/{environmentId}/strategies"; auth: "bearer";
    pathParams: { featureId: Uuid; environmentId: Uuid }; query: never; body: AddFeatureStrategyCommand;
    success: Uuid; successStatus: 200;
  };
  /** DELETE /api/v1/features/{featureId}/environments/{environmentId}/strategies/{strategyId}; bearer JWT required. */
  removeStrategy: {
    method: "DELETE"; path: "/api/v1/features/{featureId}/environments/{environmentId}/strategies/{strategyId}"; auth: "bearer";
    pathParams: { featureId: Uuid; environmentId: Uuid; strategyId: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
}

export interface VariantInput { name: string; weight: number; payload?: Record<string, unknown> | null; stickiness?: string | null; }
/** Nonempty variant sets: unique names, weights0..1000 summing1000, same stickiness key, payload<=16KiB.
 * Segment references must be shared or owned by this project. Constraints use numeric input operators.
 * Numeric/date/SemVer comparison has exactly1 value; missing context fails before inversion. */
export interface ConfigureStrategyCommand extends AddFeatureStrategyCommand {
  strategyId: Uuid | null;
  expectedRevision: number;
  sortOrder?: number;
  enabled?: boolean;
  constraints?: import("./Segments").ConstraintDto[] | null;
  segmentIds?: number[] | null;
  variants?: VariantInput[] | null;
}
export interface ReorderStrategiesCommand { featureId: Uuid; environmentId: Uuid; expectedRevision: number; strategyIds: Uuid[]; }
export interface FeatureEnvironmentsEndpoints {
  configure: { method: "PUT"; path: "/api/v1/features/{featureId}/environments/{environmentId}/strategies"; auth: "bearer"; pathParams: {featureId: Uuid; environmentId: Uuid}; query: never; body: ConfigureStrategyCommand; success: Uuid; successStatus: 200 };
  reorder: { method: "PUT"; path: "/api/v1/features/{featureId}/environments/{environmentId}/strategy-order"; auth: "bearer"; pathParams: {featureId: Uuid; environmentId: Uuid}; query: never; body: ReorderStrategiesCommand; success: void; successStatus: 200 };
}
