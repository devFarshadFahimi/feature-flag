import type { IsoDateTime, ProblemDetails } from "./Common";

/** JWT-protected operational history, available only to active Admin/RootAdmin accounts.
 * GET /api/v1/RateLimitHistory -> 200 RateLimitHistoryPage
 * GET /api/v1/RateLimitHistory/ips -> 200 TopRejectedIp[]
 * Send Authorization: Bearer <access_token>; no body. Responses are non-cacheable.
 * These are minute summaries of HTTP 429 observations, not a record of every request,
 * and an IP appearing here does not by itself establish malicious intent (e.g. shared NAT).
 * Reports update asynchronously, normally within a background tick; PostgreSQL outages delay transfer.
 * Redis is a bounded temporary buffer (default 100000 observations, 24h inactivity TTL).
 * Buffer saturation, eviction, or Redis loss may lose observations; this is operational reporting,
 * not a guaranteed security audit. Default database retention is 30 days, cleaned in bounded batches.
 */
export interface RateLimitHistoryFilter {
  /** ISO 8601 timestamp with Z/offset. Default: effective to minus 24 hours. */
  from?: IsoDateTime;
  /** Exclusive upper bound on the UTC minute start, at/after 1970-01-01. Default: start of next UTC minute.
   * from must precede to; maximum range 31 days. Use minute-aligned bounds. */
  to?: IsoDateTime;
  /** Exact IP; IPv4-mapped IPv6 is normalized to IPv4. */
  ipAddress?: string;
  /** Exact, case-sensitive filters, max 128 / 64 characters. */
  application?: string;
  environment?: string;
  /** Uppercased before comparison, max 16 characters. */
  method?: string;
  /** Exact route TEMPLATE from a previous report row, max 1024 characters.
   * e.g. api/v{version:apiVersion}/Auth/login; not an individual request URL. */
  route?: string;
}

/** GET /api/v1/RateLimitHistory query = filter plus pagination. */
export interface RateLimitHistoryQuery extends RateLimitHistoryFilter {
  page?: number; // Default 1; integer 1..10000.
  pageSize?: number; // Default 50; integer 1..200.
}

export interface RejectionMinute {
  minute: IsoDateTime;
  ipAddress: string;
  application: string;
  environment: string;
  method: string;
  route: string;
  rejectedRequests: number; // Backend Int64; do not coerce into a 32-bit integer.
  firstRejectedAt: IsoDateTime;
  lastRejectedAt: IsoDateTime;
}

export interface RateLimitHistoryPage {
  from: IsoDateTime;
  to: IsoDateTime;
  page: number;
  pageSize: number;
  totalCount: number; // Number of summary rows, not rejected requests.
  items: RejectionMinute[]; // UTC minute descending, stable bucket-key tie-breaker.
}

/** GET /api/v1/RateLimitHistory/ips query = filter plus limit. */
export interface TopRejectedIpsQuery extends RateLimitHistoryFilter {
  limit?: number; // Default 50; integer 1..200.
}

export interface TopRejectedIp {
  ipAddress: string;
  rejectedRequests: number;
  firstRejectedAt: IsoDateTime;
  lastRejectedAt: IsoDateTime;
}

/** 400 invalid bounds/IP/pagination/binding; 401 missing/invalid session; 403 non-admin;
 * 429 configured request limit, with Retry-After; 503 limiter Redis unavailable;
 * 503 unexpected persistence/service failure. Error titles/details are Persian.
 * No results is 200 with an empty items/list, not 404. No input/output enums.
 * Frontend: offer a UTC-aware time picker, IP/method/application/environment filters,
 * a paginated minute timeline, and a top-IP table sorted by count descending (IP tie-breaker).
 * Use the totalCount for pagination; preserve route templates when drilling into an endpoint.
 */
export type RateLimitHistoryError = ProblemDetails;
