/** Projects frontend contract. Baseline: 2026-10-26.
 * All actions require a bearer JWT. Build project fields plus member and feature-limit controls.
 * Route/body ids must match: update.id, addMember.projectId, setFeatureLimit.projectId.
 * Create defaultStickiness defaults to "default". setFeatureLimit.limit=null disables the limit.
 * Member roles are numeric requests, string responses. CreatedAt is declared by the DTO; the local Project
 * class does not explicitly declare it, so verify framework mapping before showing a creation timestamp.
 * EF limits: name 255, description 2000, defaultStickiness 64; name has a unique index.
 * Member mutation handlers currently do not explicitly load Members; surface server failures.
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid, IsoDateTime, ProjectRole } from "./Common";

/** Source: src/Application/Features/Projects/Commands/AddProjectMember/AddProjectMemberCommand.cs. */
export interface AddProjectMemberCommand {
  projectId: Uuid;
  userId: Uuid;
  role: ProjectRole;
}

/** Source: src/Application/Features/Projects/Commands/Create/CreateProjectCommand.cs. */
export interface CreateProjectCommand {
  name: string;
  description: string;
  /** Server default: "default". */
  defaultStickiness?: string;
}

/** Source: src/Application/Features/Projects/Commands/Delete/DeleteProjectCommand.cs. */
export interface DeleteProjectCommand {
  id: Uuid;
}

/** Source: src/Application/Features/Projects/Commands/RemoveProjectMember/RemoveProjectMemberCommand.cs. */
export interface RemoveProjectMemberCommand {
  projectId: Uuid;
  userId: Uuid;
}

/** Source: src/Application/Features/Projects/Commands/SetProjectFeatureLimit/SetProjectFeatureLimitCommand.cs. */
export interface SetProjectFeatureLimitCommand {
  projectId: Uuid;
  limit: number | null;
}

/** Source: src/Application/Features/Projects/Commands/Update/UpdateProjectCommand.cs. */
export interface UpdateProjectCommand {
  id: Uuid;
  name: string;
  description: string;
}

/** Source: src/Application/Features/Projects/Queries/GetProjectById/GetProjectByIdQuery.cs. */
export interface GetProjectByIdQuery {
  id: Uuid;
}

/** Source: src/Application/Features/Projects/Queries/GetProjectById/GetProjectByIdQuery.cs. */
export interface ProjectResponse {
  id: Uuid;
  name: string;
  description: string;
  defaultStickiness: string;
  featureLimitEnabled: boolean;
  featureLimit: number | null;
  createdAt: IsoDateTime;
  members: (ProjectMemberResponse)[];
}

/** Source: src/Application/Features/Projects/Queries/GetProjectById/GetProjectByIdQuery.cs. */
export interface ProjectMemberResponse {
  userId: Uuid;
  role: string;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface ProjectsEndpoints {
  /** POST /api/v1/Projects; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/Projects"; auth: "bearer";
    pathParams: never; query: never; body: CreateProjectCommand;
    success: Uuid; successStatus: 200;
  };
  /** GET /api/v1/Projects; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/Projects"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: ProjectResponse[]; successStatus: 200;
  };
  /** GET /api/v1/Projects/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/Projects/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: ProjectResponse; successStatus: 200;
  };
  /** PUT /api/v1/Projects/{id}; bearer JWT required. */
  update: {
    method: "PUT"; path: "/api/v1/Projects/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: UpdateProjectCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Projects/{id}; bearer JWT required. */
  delete: {
    method: "DELETE"; path: "/api/v1/Projects/{id}"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Projects/{id}/members; bearer JWT required. */
  addMember: {
    method: "POST"; path: "/api/v1/Projects/{id}/members"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: AddProjectMemberCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Projects/{id}/members/{userId}; bearer JWT required. */
  removeMember: {
    method: "DELETE"; path: "/api/v1/Projects/{id}/members/{userId}"; auth: "bearer";
    pathParams: { id: Uuid; userId: Uuid }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** PUT /api/v1/Projects/{id}/feature-limit; bearer JWT required. */
  setFeatureLimit: {
    method: "PUT"; path: "/api/v1/Projects/{id}/feature-limit"; auth: "bearer";
    pathParams: { id: Uuid }; query: never; body: SetProjectFeatureLimitCommand;
    success: void; successStatus: 200;
  };
}

/** Project creator becomes Owner. Reads/lists require membership or Admin/RootAdmin; project settings
 * and membership changes require Owner+global Editor or Admin+. Feature creation enforces its limit. */
export interface ApprovalPolicy { projectId: Uuid; environmentId: Uuid; revision: number; enabled: boolean; requiredApprovals: number; }
export interface SetApprovalPolicyCommand { projectId: Uuid; environmentId: Uuid; expectedRevision: number; enabled: boolean; requiredApprovals?: number; }
/** Required approvals1..5; increasing protection is direct, decreasing requires a reviewed request.
 * A global protected environment always requires at least1 non-author approval. Missing policy reads revision0. */
export interface ProjectsEndpoints {
  getApprovals: { method: "GET"; path: "/api/v1/Projects/{id}/environments/{environmentId}/approvals"; auth: "bearer"; pathParams: {id: Uuid; environmentId: Uuid}; query: never; body: never; success: ApprovalPolicy; successStatus: 200 };
  setApprovals: { method: "PUT"; path: "/api/v1/Projects/{id}/environments/{environmentId}/approvals"; auth: "bearer"; pathParams: {id: Uuid; environmentId: Uuid}; query: never; body: SetApprovalPolicyCommand; success: void; successStatus: 200 };
}
