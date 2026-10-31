/** Environments frontend contract.
 * All actions require a bearer JWT and Admin/RootAdmin. Create has numeric type and sortOrder default 0.
 * Update body requires id/name/sortOrder and persists the name and sort order.
 * Environment responses no longer contain a tokens field. Remove token forms, lists, and create/revoke
 * requests from clients; environment API-token endpoints have been removed. User JWT login is unchanged.
 * Protection is managed through the protect operation and the approved change-request workflow.
 * EF limits: environment name 128 (unique).
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid, EnvironmentType, SelectiveDTO } from "./Common";

/** Source: src/Application/Features/Environments/Commands/CreateEnvironment/CreateEnvironmentCommand.cs. */
export interface CreateEnvironmentCommand {
  name: string;
  type: EnvironmentType;
  /** Server default: 0. */
  sortOrder?: number;
}

/** Source: src/Application/Features/Environments/Commands/DeleteEnvironment/DeleteEnvironmentCommand.cs. */
export interface DeleteEnvironmentCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Environments/Commands/DisableEnvironment/DisableEnvironmentCommand.cs. */
export interface DisableEnvironmentCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Environments/Commands/EnableEnvironment/EnableEnvironmentCommand.cs. */
export interface EnableEnvironmentCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Environments/Commands/UpdateEnvironment/UpdateEnvironmentCommand.cs. */
export interface UpdateEnvironmentCommand {
  id: Uuid;
  name: string;
  sortOrder: number;
}

/** Source: src/Application/Features/Environments/Queries/GetEnvironmentById/GetEnvironmentByIdQuery.cs. */
export interface GetEnvironmentByIdQuery {
  id: Uuid;
}

/** Source: src/Application/Features/Environments/Queries/GetEnvironmentById/GetEnvironmentByIdQuery.cs. */
export interface EnvironmentResponse {
  id: Uuid;
  name: string;
  type: string;
  enabled: boolean;
  sortOrder: number;
  protected: boolean;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface EnvironmentsEndpoints {
  /** POST /api/v1/Environments; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/Environments"; auth: "bearer";
    pathParams: never; query: never; body: CreateEnvironmentCommand;
    success: Uuid; successStatus: 200;
  };
  /** GET /api/v1/Environments; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/Environments"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: EnvironmentResponse[]; successStatus: 200;
  };
  /** GET /api/v1/Environments/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/Environments/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: EnvironmentResponse; successStatus: 200;
  };
  /** PUT /api/v1/Environments/{id}; bearer JWT required. */
  update: {
    method: "PUT"; path: "/api/v1/Environments/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: UpdateEnvironmentCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Environments/{id}; bearer JWT required. */
  delete: {
    method: "DELETE"; path: "/api/v1/Environments/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Environments/{id}/enable; bearer JWT required. */
  enable: {
    method: "POST"; path: "/api/v1/Environments/{id}/enable"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Environments/{id}/disable; bearer JWT required. */
  disable: {
    method: "POST"; path: "/api/v1/Environments/{id}/disable"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
}

/** Global environment administration requires Admin/RootAdmin. Ordinary updates cannot weaken
 * global protection; use a Global request approved by a different RootAdmin to disable it. */
export interface EnvironmentResponse { revision: number; }
export interface ProtectEnvironmentCommand { environmentId: Uuid; expectedRevision: number; }
export interface EnvironmentsEndpoints {
  protect: { method: "POST"; path: "/api/v1/Environments/{id}/protection"; auth: "bearer"; pathParams: {id: Uuid}; query: never; body: ProtectEnvironmentCommand; success: void; successStatus: 200 };
}

/** Environment dropdown: GET /api/v1/Environments/selective; bearer JWT and Admin/RootAdmin required.
 * No path/query parameters or body. HTTP 200 returns a raw SelectiveDTO<Uuid>[]; [] when none exist.
 * key is the non-null environment UUID; value is its non-null name. Ordered by sortOrder, database
 * name collation, then UUID. Includes enabled/disabled and protected environments; no paging/search.
 * Invalid/revoked/missing session: 401; authenticated non-admin: 403, even with project membership.
 * Inspect HTTP status before parsing; errors contain Persian messages/ProblemDetails where available.
 *
 * Frontend: replace related Environment ID text inputs with a dropdown displaying value and binding key.
 * Submit key as environmentId (or the destination request's documented ID field), never its label.
 * Load after authorized login, show loading/empty/error states and require a valid key when mandatory.
 * Preserve an edit's saved selection only if it is returned; otherwise require a new selection.
 * Refresh after environment create/rename/delete and on account changes; clear options on logout.
 * Handle 403 explicitly for non-admin users. Listing an environment does not bypass project permissions,
 * environment enablement or approval protection on the destination operation.
 */
export type EnvironmentSelectiveDTO = SelectiveDTO<Uuid>;

export interface EnvironmentsEndpoints {
  selective: {
    method: "GET"; path: "/api/v1/Environments/selective"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: EnvironmentSelectiveDTO[]; successStatus: 200;
  };
}
