/** Shared wire types. Baseline: 2026-10-26. See README.md for provenance and HTTP caveats.
 * UUID/date aliases document JSON strings; they do not perform runtime validation.
 * Request enums are numeric: MVC AddControllers has no JsonStringEnumConverter registration.
 * DTO enum fields are strings because handlers/Mapster call ToString(), independently of EF storage.
 */
export type Uuid = string;
export type IsoDateTime = string;

/** Reusable dropdown option: key is the entity ID; value is its display label.
 * Wire properties are camelCase even though the C# DTO uses Key and Value. */
export interface SelectiveDTO<TKey> {
  key: TKey;
  value: string;
}

/** Result/exception failures commonly use ProblemDetails. Not every failure shares this shape.
 * Local validation/auth/exception title/detail/messages are Persian.
 * Check HTTP status before parsing success; status may be omitted from some error bodies.
 */
export interface ProblemDetails {
  type?: string; title?: string; status?: number; detail?: string; instance?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
}
/** Existing route/body mismatch responses have this separate shape (HTTP 400). */
export interface IdMismatchError { message: string; }

export enum ApiTokenType {
  Client = 0,
  Admin = 1,
  Frontend = 2
}

export enum AuditAction {
  Created = 0,
  Updated = 1,
  Deleted = 2,
  Enabled = 3,
  Disabled = 4,
  Archived = 5,
  StrategyAdded = 10,
  StrategyUpdated = 11,
  StrategyDeleted = 12,
  ConstraintAdded = 20,
  ConstraintUpdated = 21,
  ConstraintDeleted = 22
}

export enum ChangeRequestStatus {
  Draft = 0,
  InReview = 1,
  Approved = 2,
  Rejected = 3,
  Applied = 4,
  Scheduled = 5,
  Cancelled = 6,
  Suspended = 7,
  Failed = 8
}

export enum ConstraintOperator {
  In = 0,
  NotIn = 1,
  StrStartsWith = 2,
  StrEndsWith = 3,
  StrContains = 4,

  NumEq = 10,
  NumGt = 11,
  NumGte = 12,
  NumLt = 13,
  NumLte = 14,

  DateAfter = 20,
  DateBefore = 21,

  SemverEq = 30,
  SemverGt = 31,
  SemverLt = 32,
  SemverGte = 33,
  SemverLte = 34,

  AlwaysTrue = 99,
  AlwaysFalse = 100
}

export enum EnvironmentType {
  Development = 0,
  Staging = 1,
  Production = 2,
  Testing = 3
}

export enum FeatureLifecycle {
  Planned = 0,
  Active = 1,
  Completed = 2,
  Archived = 3
}

export enum FeatureType {
  Release = 0,
  Experiment = 1,
  Operational = 2,
  KillSwitch = 3,
  Permission = 4
}

export enum ProjectRole {
  Viewer = 0,
  Member = 1,
  Owner = 2
}

export enum StrategyType {
  Default = 0,
  UserWithId = 1,
  GradualRollout = 2,
  FlexibleRollout = 3,
  RemoteAddress = 4,
  ApplicationHost = 5,
  Custom = 99
}

export enum UserRole {
  Viewer = 0,
  Editor = 1,
  Admin = 2,
  RootAdmin = 3
}

