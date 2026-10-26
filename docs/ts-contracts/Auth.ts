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
  /** POST /api/v1/Auth/login; anonymous. */
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
