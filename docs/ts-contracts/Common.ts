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

// Controller/action attributes and per-type route arrays configure the shared Redis limiters.
// Defaults: login 10/60s and refresh 60/60s sliding windows; evaluation 600/60s and general 120/60s fixed windows.
// Different limiter types compose; for the same type, explicit attributes override configuration,
// and exact paths override controller patterns (e.g. evaluations/*), which override the * fallback.
// Optional methods restrict a rule to endpoints whose HTTP methods all match that list.
// All limits partition by source IP, HTTP method, route, application and environment.
// HTTP 429 carries Retry-After (rounded up to seconds, minimum 1) and Persian application/problem+json.
// Do not submit again before that delay.
// HTTP 503 can also mean the limiter's Redis storage is unavailable; no controller operation ran in that case.
// Hosting configuration can change these limits. Browsers continue calling through their own backend.
// Redis keeps separate rejected-IP diagnostics for 15 minutes of inactivity; these do not affect admission.
/* Hosting configuration examples (use real routes; prefix api/v1/ is optional):
 * "FixedWindowRateLimit": [{ "path": "projects/*", "limit": 120, "windowSeconds": 60 }]
 * "SlidingWindowRateLimit": [{ "path": "auth/login", "methods": ["POST"], "limit": 10, "windowSeconds": 60 }]
 * "TokenBucketRateLimit": [{ "path": "evaluations/*", "capacity": 100, "refillRate": 10 }]
 * "ConcurrencyRateLimit": [{ "path": "evaluations/*", "maxConcurrentRequests": 20 }]
 * refillRate is tokens per second; the concurrency limit counts in-flight requests.
 * Empty arrays disable that configured type; explicit C# attributes can still apply.
 * Changes take effect after application restart. Arrays in environment-specific configuration override by index;
 * setting an empty array in an override file does not erase entries inherited from appsettings.json.
 */
