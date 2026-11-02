/** Users frontend contract. Baseline: 2026-10-26.
 * All actions require an active bearer JWT and Admin/RootAdmin, enforced by the resource filter.
 * Create form: email/password, optional role (numeric Viewer=0), optional name. Update sends id, name, email;
 * null profile fields leave existing values unchanged. Role change body id must match route id.
 * EF limits: email/name 255; email is unique. Passwords require at least 12 UTF-16 code units and at most 72 UTF-8 bytes.
 * Read DTO differs from AuthUserResponse. Never display/store password hashes.
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid, IsoDateTime, UserRole } from "./Common";

/** Source: src/Application/Features/Users/Commands/ActivateUser/ActivateUserCommand.cs. */
export interface ActivateUserCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Users/Commands/CreateUser/CreateUserCommand.cs. */
export interface CreateUserCommand {
  email: string;
  password: string;
  /** Server default: UserRole.Viewer. */
  role?: UserRole;
  /** Server default: null. */
  name?: string | null;
}

/** Source: src/Application/Features/Users/Commands/DeactivateUser/DeactivateUserCommand.cs. */
export interface DeactivateUserCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Users/Commands/PromoteUser/PromoteUserCommand.cs. */
export interface PromoteUserCommand {
  id: Uuid;
  role: UserRole;
}

/** Source: src/Application/Features/Users/Commands/UpdateUser/UpdateUserCommand.cs. */
export interface UpdateUserCommand {
  id: Uuid;
  name: string | null;
  email: string | null;
}

/** Source: src/Application/Features/Users/Queries/GetUserById/GetUserByIdQuery.cs. */
export interface GetUserByIdQuery {
  id: Uuid;
}

/** Source: src/Application/Features/Users/Queries/GetUserById/GetUserByIdQuery.cs. */
export interface UserResponse {
  id: Uuid;
  email: string;
  name: string | null;
  role: string;
  isActive: boolean;
  createdAt: IsoDateTime;
  lastLoginAt: IsoDateTime | null;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface UsersEndpoints {
  /** POST /api/v1/Users; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/Users"; auth: "bearer";
    pathParams: never; query: never; body: CreateUserCommand;
    success: Uuid; successStatus: 200;
  };
  /** GET /api/v1/Users; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/Users"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: UserResponse[]; successStatus: 200;
  };
  /** GET /api/v1/Users/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/Users/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: UserResponse; successStatus: 200;
  };
  /** PUT /api/v1/Users/{id}; bearer JWT required. */
  update: {
    method: "PUT"; path: "/api/v1/Users/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: UpdateUserCommand;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Users/{id}/activate; bearer JWT required. */
  activate: {
    method: "POST"; path: "/api/v1/Users/{id}/activate"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Users/{id}/deactivate; bearer JWT required. */
  deactivate: {
    method: "POST"; path: "/api/v1/Users/{id}/deactivate"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** PUT /api/v1/Users/{id}/role; bearer JWT required. */
  promote: {
    method: "PUT"; path: "/api/v1/Users/{id}/role"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: PromoteUserCommand;
    success: void; successStatus: 200;
  };
}

/** All user administration requires Admin/RootAdmin. Only RootAdmin can create/change another RootAdmin;
 * at least one active RootAdmin must remain. Passwords require at least12 characters. Promotions and
 * deactivation revoke sessions; old JWTs stop working immediately on subsequent validation. */

/** Dropdown contract: GET /api/v1/Users/Selective; bearer JWT, no body, 200 SelectiveDTO[].
 * Persian errors: 400 malformed query, 401 unauthenticated, 403 denied, 404 unknown explicit project,
 * 503 unavailable. Empty matches return []; labels are display text and keys are submitted IDs.
 * Admin/RootAdmin only. includeInactive defaults false; value is name or email when name is empty.
 * Frontend: load these options for related forms and submit key, never a typed entity ID or value. */
export type UserSelectiveDTO = import("./Common").SelectiveDTO<Uuid>;
export interface UsersEndpoints {
  selective: {
    method: "GET"; path: "/api/v1/Users/Selective"; auth: "bearer";
    pathParams: never; query: { includeInactive?: boolean }; body: never;
    success: UserSelectiveDTO[]; successStatus: 200;
  };
}

/** Administrator-mediated reset; POST /api/v1/Users/{id}/reset-password, bearer JWT required.
 * Admin/RootAdmin only; only RootAdmin may reset another RootAdmin. Self reset is forbidden (403):
 * use Auth.changePassword with the current password. Supply a trusted new password through the
 * organization's existing secure channel; this operation sends no email and returns no secret.
 * Path id and required body id must match. New password is required/non-null: minimum 12 UTF-16
 * code units, maximum 72 UTF-8 bytes. Existing sessions are all invalidated, even for inactive users.
 * 200 has no JSON body. Persian errors: 400 invalid password/ID mismatch, 401 invalid session,
 * 403 insufficient hierarchy/self reset, 404 missing user, 429 throttled, 503 unavailable.
 */
export interface ResetUserPasswordCommand {
  id: Uuid;
  newPassword: string;
}

export interface UsersEndpoints {
  resetPassword: {
    method: "POST"; path: "/api/v1/Users/{id}/reset-password"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: ResetUserPasswordCommand;
    success: void; successStatus: 200;
  };
}
