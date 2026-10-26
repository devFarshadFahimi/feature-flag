# Frontend TypeScript contracts

Baseline: 2026-10-26. These files describe the existing controller actions, Application request/response records, domain behavior and Mapster configuration. They contain type-only operation maps and commented frontend guidance; they are not a generated SDK, runtime validation library or proof that every endpoint currently works.

## Files

| Contract | Client responsibility |
| --- | --- |
| [Auth.ts](Auth.ts) | Login/refresh, token rotation, logout, current user |
| [Users.ts](Users.ts) | Administrative user fields, activation and numeric role requests |
| [Projects.ts](Projects.ts) | Project forms, member roles, nullable feature limit |
| [Features.ts](Features.ts) | Feature forms, lifecycle metadata, flags and tags |
| [Environments.ts](Environments.ts) | Environment forms and token metadata/create/revoke |
| [FeatureEnvironments.ts](FeatureEnvironments.ts) | Nested strategy operations and actual parameter handling |
| [Segments.ts](Segments.ts) | Integer segment IDs, constraints and index-based removal |
| [ApiTokens.ts](ApiTokens.ts) | Token metadata queries only |
| [ChangeRequests.ts](ChangeRequests.ts) | Review state transitions and current apply/schedule limitations |
| [Diagnostics.ts](Diagnostics.ts) | Existing test/demo actions, including unimplemented actions |
| [Common.ts](Common.ts) | UUID/date aliases, numeric enums, ProblemDetails and mismatch errors |

There is no AmazingDeals endpoint in the inspected source; the example filename in the requested structure is replaced by real feature names. The inventory covers **61 business controller actions and 4 diagnostic/demo actions**. It does not claim to enumerate package-owned infrastructure routes (health/OpenAPI/Scalar/API descriptor), or route aliases from inherited attributes.

## Wire conventions

- Paths are relative to the configured API origin; ordinary business actions use API version 1.0, represented as `/api/v1`. Replace `{id}` placeholders and URL-encode path components. Route constraints are UUIDs except segment IDs and constraint indexes, which are integers. The feature-environment nested route contains two UUIDs; read its comments before using inherited route aliases.
- Auth login/refresh are anonymous. Other business operations require `Authorization: Bearer <accessToken>`. Plain `[Authorize]` does not establish role/project-membership enforcement. Login responses contain tokens in JSON; no cookie/session contract is implemented. Use `Content-Type: application/json` when sending a JSON body.
- JSON uses camelCase. Requests use numeric C# enums; DTO enum properties are strings populated using `ToString()`. Do not use the numeric enum as the response field type. Defined values are in `Common.ts`; do not submit arbitrary enum numbers even if a binder accepts them.
- `Uuid` and `IsoDateTime` are JSON strings. Send timezone-qualified ISO dates (prefer UTC); TypeScript aliases do not validate formats. Integers must be integral numbers. Collection fields are arrays. Nullable fields use `| null`; optional properties have C# constructor defaults documented beside them. A nullable constructor parameter without a default is deliberately required by these client contracts, even if runtime model binding permits omission. Framework binding behavior is not inferred from C# nullability alone.
- `never` in an operation map means omit that request component. Some exported command records represent values the controller constructs from path/claims; do not send them as bodies unless the operation's `body` explicitly names them. For example, logout sends no body, even though `LogoutCommand` exists.
- Query filters are optional: omit null/undefined values instead of serializing the string `"null"`. Current list endpoints are arrays with no pagination envelope. Do not add page/size parameters to a form based on unrelated pagination utilities.

## Success and errors

The inspected upstream `BusinessApiControllerBase` delegates commands to `Result.ToActionResult` and queries to `Ok(dto)` (null query results can map to 404). `Result<T>` success is **HTTP 200 with the raw body**, such as a JSON UUID string, integer, object or array. Status-only Result success is **HTTP 200 with an empty body**. Creates do not establish 201/Location, and deletes/updates do not establish 204. Do not expect `{ succeeded, body }`, `{ data }` or HATEOAS wrappers simply because helper packages are registered.

These status/body mappings were checked against live upstream checkout source, and existing assets resolve framework 0.3.22-preview. Matching package internals and live HTTP responses were not executed for this documentation task, so the maps remain source-derived contracts. Verify installed-package behavior on a configured disposable environment before client release, especially on package changes or inconsistent projections.

On non-success statuses, inspect the response content type/body. Framework Result failures commonly return ProblemDetails with their configured status. Controller ID mismatches explicitly return **400** with `{ "message": "..." }`; automatic model validation can return validation ProblemDetails. Authentication challenges can return **401 with no JSON body**; policy denial can return 403. Missing resources in local handlers usually throw `InvalidEntityStateException`, so **do not assume a 404**: status/body depends on the registered exception handler and package version. Unexpected exceptions can become 500 ProblemDetails. Unsupported routes/versions can fail before the action. Do not retry mutation requests automatically based only on a network error.

All newly authored error titles/details/messages must be Persian. Current backend/package defaults still include English text, so a client must tolerate legacy messages until those paths are localized. Preserve field names/status/error codes while displaying a useful error. There is no single guaranteed error envelope for all infrastructure responses.

## Client examples

Create a project (authenticated):

```http
POST /api/v1/Projects
Authorization: Bearer <accessToken>
Content-Type: application/json

{"name":"checkout","description":"Checkout flags","defaultStickiness":"default"}
```

Expected source-derived HTTP 200 body is a JSON UUID string, for example `"11111111-1111-4111-8111-111111111111"`. For a status-only mutation, inspect success and do not call `response.json()` on the empty response.

Add a flexible rollout strategy to an **existing** feature-environment association:

```json
{
  "featureId": "11111111-1111-4111-8111-111111111111",
  "environmentId": "22222222-2222-4222-8222-222222222222",
  "type": 3,
  "rolloutPercentage": 25,
  "stickiness": "userId",
  "groupId": "checkout"
}
```

Send it with POST to `/api/v1/features/{featureId}/environments/{environmentId}/strategies`, repeating those exact UUIDs in the path. `GradualRollout=2` currently does not use percentage inputs; the handler consumes them for `FlexibleRollout=3`.

Types for form state and API success can be selected without introducing a client framework:

```typescript
import type { ProjectsEndpoints, CreateProjectCommand } from "./Projects";

const form: CreateProjectCommand = { name: "checkout", description: "Checkout flags" };
type CreatedProjectId = ProjectsEndpoints["create"]["success"];
```

## Maintenance and validation

For any endpoint change, update its contract alongside controller binding, request/response records, mapping and domain guards. Check exact field names, required/default/null semantics, enum numeric values, ID types, date handling, path/body equality, auth, error behavior and known backend limitations. Do not infer implemented endpoints from an archived spec.

Compile using an available TypeScript compiler with strict checking and no emit; pass all `.ts` files explicitly if the shell does not expand a glob. Recheck every controller action against the operation inventory and verify runtime responses when implementation changes. Do not install a frontend project merely to store contracts, and never re-index a repository automatically.
