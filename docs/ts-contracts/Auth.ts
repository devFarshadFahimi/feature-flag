/** Local JWT sessions. Tokens require the configured issuer/audience/signature and active account.
 * Role/password/deactivation/logout changes revoke sessions through security_version. Refresh is single
 * use; inactive accounts and concurrent token reuse are rejected. Browser consumers use their backend.
 * Check HTTP status first: Persian 400 validation,401 authentication,403 permission,409 conflict,503 outage.
 */
import type { Uuid, IsoDateTime } from "./Common";

/** Source: src/Application/Features/Auth/Commands/Login/LoginCommand.cs. */
export interface LoginCommand {
  email: string;
  password: string;
}

/** Source: src/Application/Features/Auth/Commands/Login/LoginCommand.cs. */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiration: IsoDateTime;
  refreshTokenExpiration: IsoDateTime;
  user: AuthUserResponse;
}

/** Source: src/Application/Features/Auth/Commands/Login/LoginCommand.cs. */
export interface AuthUserResponse {
  id: Uuid;
  email: string;
  name: string | null;
  role: string;
}

/** Source: src/Application/Features/Auth/Commands/Logout/LogoutCommand.cs. */
export interface LogoutCommand {
  userId: Uuid;
}

/** Source: src/Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs. */
export interface RefreshTokenCommand {
  accessToken: string;
  refreshToken: string;
}

/** Source: src/Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs. */
export interface GetCurrentUserQuery {
  userId: Uuid;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface AuthEndpoints {
  /** POST /api/v1/Auth/login; anonymous. No existing bearer token is required.
   * 401 uses the generic Persian credential error for incorrect credentials, inactive accounts and temporary lockout.
   * Do not retry login automatically: repeated rejected passwords count toward the five-attempt lockout. */
  login: {
    method: "POST"; path: "/api/v1/Auth/login"; auth: "anonymous";
    pathParams: never; query: never; body: LoginCommand;
    success: LoginResponse; successStatus: 200;
  };
  /** POST /api/v1/Auth/refresh; anonymous. */
  refresh: {
    method: "POST"; path: "/api/v1/Auth/refresh"; auth: "anonymous";
    pathParams: never; query: never; body: RefreshTokenCommand;
    success: LoginResponse; successStatus: 200;
  };
  /** POST /api/v1/Auth/logout; bearer JWT required. */
  logout: {
    method: "POST"; path: "/api/v1/Auth/logout"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** GET /api/v1/Auth/me; bearer JWT required. */
  me: {
    method: "GET"; path: "/api/v1/Auth/me"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: AuthUserResponse; successStatus: 200;
  };
}

/** Self-service change; POST /api/v1/Auth/change-password, bearer JWT, no path/query.
 * Both fields are required and non-null. New password: at least 12 UTF-16 code units and at most
 * 72 UTF-8 bytes (Persian characters use multiple bytes). Current password must be correct.
 * 200 has no body. All existing access/refresh sessions, including this one, are invalidated;
 * clear local tokens and ask the user to log in again. No replacement token or email is returned.
 * Persian errors: 400 invalid new password/body, 401 incorrect current password/invalid session,
 * 429 throttled (honor Retry-After), 503 unavailable. Browser applications call through their backend.
 */
export interface ChangePasswordCommand {
  currentPassword: string;
  newPassword: string;
}

export interface AuthEndpoints {
  changePassword: {
    method: "POST"; path: "/api/v1/Auth/change-password"; auth: "bearer";
    pathParams: never; query: never; body: ChangePasswordCommand;
    success: void; successStatus: 200;
  };
}

/** Refresh secrets are stored as SHA-256 digests, never returned from storage. Keep the raw response
 * secret securely on the application backend. Submit the last access token plus its refresh token.
 * Refresh is serialized per account and returns a new pair; replace both atomically in client storage.
 * Reuse of an already consumed refresh token revokes every session for that account, then returns 401.
 * Never automatically retry/parallelize refresh. Revoked, expired, mismatched and legacy raw tokens
 * cannot renew a session. Legacy sessions require login after deployment. No numeric enums occur in
 * Auth requests; user.role is a string enum (Viewer, Editor, Admin, RootAdmin). Login failures use the
 * same generic Persian authentication message for unknown email, inactive account, wrong password,
 * or temporary account lockout (401). Five consecutive failures lock login for five minutes across
 * all application instances; successful login or a password change/reset clears failed attempts.
 * Requests may return 429 with Retry-After; honor that delay before a user-initiated retry.
 */
// Authentication responses carry Cache-Control: no-store; never cache or log token/password bodies.
