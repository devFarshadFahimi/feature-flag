/** Segments frontend contract. Baseline: 2026-10-26.
 * All actions require a bearer JWT. Segment IDs are integers, not UUIDs.
 * Create optionally includes constraints; isPublic defaults true. For a constraint, contextName must be
 * nonblank; operator is a numeric ConstraintOperator; values is an array of strings (including numeric/date values).
 * inverted/caseInsensitive default false. Removal uses a ZERO-BASED index in the current constraint list,
 * not a constraint ID. Refetch after changes; ordering/persistence must be verified before destructive UI actions.
 * AddConstraint body segmentId and update body id must match the route. EF contextName limit is 128.
 * EF limits: segment name 128 (unique), description 2000. Update description=null clears it.
 * JSON property names use camelCase. Nullable fields without defaults remain required in this contract;
 * optional fields have server defaults. Check README.md before implementing a client.
 */
import type { Uuid, IsoDateTime, ConstraintOperator } from "./Common";

/** Source: src/Application/Features/Segments/Commands/AddSegmentConstraint/AddSegmentConstraintCommand.cs. */
export interface AddSegmentConstraintCommand {
  segmentId: number;
  contextName: string;
  operator: ConstraintOperator;
  values: (string)[];
  /** Server default: false. */
  inverted?: boolean;
  /** Server default: false. */
  caseInsensitive?: boolean;
}

/** Source: src/Application/Features/Segments/Commands/CreateSegment/CreateSegmentCommand.cs. */
export interface CreateSegmentCommand {
  name: string;
  /** Server default: null. */
  description?: string | null;
  /** Server default: true. */
  isPublic?: boolean;
  /** Server default: null. */
  constraints?: (ConstraintDto)[] | null;
}

/** Source: src/Application/Features/Segments/Commands/CreateSegment/CreateSegmentCommand.cs. */
export interface ConstraintDto {
  contextName: string;
  operator: ConstraintOperator;
  values: (string)[];
  /** Server default: false. */
  inverted?: boolean;
  /** Server default: false. */
  caseInsensitive?: boolean;
}

/** Source: src/Application/Features/Segments/Commands/DeleteSegment/DeleteSegmentCommand.cs. */
export interface DeleteSegmentCommand {
  id: number;
}

/** Source: src/Application/Features/Segments/Commands/RemoveSegmentConstraint/RemoveSegmentConstraintCommand.cs. */
export interface RemoveSegmentConstraintCommand {
  segmentId: number;
  constraintIndex: number;
}

/** Source: src/Application/Features/Segments/Commands/UpdateSegment/UpdateSegmentCommand.cs. */
export interface UpdateSegmentCommand {
  id: number;
  name: string;
  description: string | null;
}

/** Source: src/Application/Features/Segments/Queries/GetSegmentById/GetSegmentByIdQuery.cs. */
export interface GetSegmentByIdQuery {
  id: number;
}

/** Source: src/Application/Features/Segments/Queries/GetSegmentById/GetSegmentByIdQuery.cs. */
export interface SegmentResponse {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  createdAt: IsoDateTime;
  lastUsedAt: IsoDateTime | null;
  constraints: (SegmentConstraintResponse)[];
}

/** Source: src/Application/Features/Segments/Queries/GetSegmentById/GetSegmentByIdQuery.cs. */
export interface SegmentConstraintResponse {
  contextName: string;
  operator: string;
  values: (string)[];
  inverted: boolean;
  caseInsensitive: boolean;
}

/** Type-only operation map; never means omit that part of the request.
 * Success mapping is source-derived; verify installed-package/runtime behavior (README.md).
 * Status-only success has no JSON body; do not call response.json() for it.
 * All operations may fail: inspect HTTP status and ProblemDetails/IdMismatchError or an empty auth response. */
export interface SegmentsEndpoints {
  /** POST /api/v1/Segments; bearer JWT required. */
  create: {
    method: "POST"; path: "/api/v1/Segments"; auth: "bearer";
    pathParams: never; query: never; body: CreateSegmentCommand;
    success: number; successStatus: 200;
  };
  /** GET /api/v1/Segments; bearer JWT required. */
  list: {
    method: "GET"; path: "/api/v1/Segments"; auth: "bearer";
    pathParams: never; query: never; body: never;
    success: SegmentResponse[]; successStatus: 200;
  };
  /** GET /api/v1/Segments/{id}; bearer JWT required. */
  get: {
    method: "GET"; path: "/api/v1/Segments/{id}"; auth: "bearer";
    pathParams: { id: number }; query: never; body: never;
    success: SegmentResponse; successStatus: 200;
  };
  /** PUT /api/v1/Segments/{id}; bearer JWT required. */
  update: {
    method: "PUT"; path: "/api/v1/Segments/{id}"; auth: "bearer";
    pathParams: { id: number }; query: never; body: UpdateSegmentCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Segments/{id}; bearer JWT required. */
  delete: {
    method: "DELETE"; path: "/api/v1/Segments/{id}"; auth: "bearer";
    pathParams: { id: number }; query: never; body: never;
    success: void; successStatus: 200;
  };
  /** POST /api/v1/Segments/{id}/constraints; bearer JWT required. */
  addConstraint: {
    method: "POST"; path: "/api/v1/Segments/{id}/constraints"; auth: "bearer";
    pathParams: { id: number }; query: never; body: AddSegmentConstraintCommand;
    success: void; successStatus: 200;
  };
  /** DELETE /api/v1/Segments/{id}/constraints/{index}; bearer JWT required. */
  removeConstraint: {
    method: "DELETE"; path: "/api/v1/Segments/{id}/constraints/{index}"; auth: "bearer";
    pathParams: { id: number; index: number }; query: never; body: never;
    success: void; successStatus: 200;
  };
}

/** Null ownership means shared, administered by Admin/RootAdmin. Project-owned segments obey project
 * permissions. isPublic never bypasses JWT/membership. Changes affecting protected configurations need requests. */
export interface CreateSegmentCommand { projectId?: Uuid | null; }
export interface SegmentResponse { projectId: Uuid | null; revision: number; }
