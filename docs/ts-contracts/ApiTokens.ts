/** ApiTokens frontend contract. Baseline: 2026-10-26.
 * All actions require a bearer JWT. Read/list metadata only; filter by optional environmentId.
 * Creation and revocation belong to Environments endpoints. No token secret/hash is returned here.
 * Import ApiTokenResponse from Environments; tokenType is a string despite numeric creation input.
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid } from "./Common";
import type { ApiTokenResponse } from "./Environments";

/** Source: src/Application/Features/ApiTokens/Queries/GetAllApiTokens/GetAllApiTokensQuery.cs. */
export interface GetAllApiTokensQuery {
  /** Server default: null. */
  environmentId?: Uuid | null;
}

/** Source: src/Application/Features/ApiTokens/Queries/GetApiTokenById/GetApiTokenByIdQuery.cs. */
export interface GetApiTokenByIdQuery {
  id: Uuid;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface ApiTokensEndpoints {
  /** GET /api/v1/ApiTokens; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/ApiTokens"; auth: "bearer";
    pathParams: never; query: { environmentId?: Uuid | null }; body: never;
    success: ApiTokenResponse[]; successStatus: 200;
  };
  /** GET /api/v1/ApiTokens/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/ApiTokens/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: ApiTokenResponse; successStatus: 200;
  };
}
