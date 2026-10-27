/** Anonymous operational endpoints; no JWT, query, path or body fields.
 * GET /health/live: 200 {status:"alive"}; process liveness only.
 * GET /health/ready: 200 {status:"ready"} when PostgreSQL is reachable and all known migrations
 * have been applied; otherwise 503 {status:"unavailable",message:string} with a Persian message.
 * Readiness sends Cache-Control:no-store, never applies migrations or seeds data, and exposes no
 * credentials or database diagnostics. These routes are unversioned and outside /api/v1.
 * Use readiness for traffic admission; a live process alone does not establish database readiness. */
export type ReadinessResponse = { status: "ready" } | { status: "unavailable"; message: string };
export interface HealthEndpoints {
  live: { method: "GET"; path: "/health/live"; auth: "none"; body: never; success: { status: "alive" }; successStatus: 200 };
  ready: { method: "GET"; path: "/health/ready"; auth: "none"; body: never; success: { status: "ready" }; successStatus: 200; unavailable: { status: "unavailable"; message: string }; unavailableStatus: 503 };
}
