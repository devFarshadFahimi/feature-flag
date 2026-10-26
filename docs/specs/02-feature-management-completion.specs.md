# 02 — Complete feature management with ordinary evaluation APIs

Status: **Design approved by the user on 2026-10-26; implementation pending.**

Date: 2026-10-26. This specification supersedes the SDK/application-token/UI recommendations in the historical [gap inspection](../IMPLEMENTATION-GAPS.md). It does not implement or approve archived spec 01.

## User-approved scope

- Complete all four requested milestones: correct existing behavior; deliver a configuration-to-evaluation journey for backend and browser consumers; add refresh/caching, full targeting, variants and metrics; complete approvals, scheduling, history and advanced rollout behavior.
- Fix every defect enumerated in the user's request, including authorization, initialization, EF mappings, real change application, child loading, ignored inputs, sessions, validation/Persian errors, deployment and tests.
- **Excluded:** backend/frontend SDK packages, dedicated SDK configuration/protocol endpoints, application/API-key credential infrastructure, administration UI. Do not implement Unleash protocol compatibility.
- The user explicitly confirmed: **ordinary JWT-protected evaluation endpoints are allowed; browsers consume results through their application's backend.**
- Maintain .NET 10, PostgreSQL, the four existing layers, BusinessMaker mediator/handlers and precise TypeScript endpoint contracts. No automatic repository indexing, no upstream edits or publications, no commits/pushes without separate authorization.

## Chosen approach and alternatives

The chosen approach extends the existing API with JWT-protected, server-evaluated flag results. Each consuming backend calls this API using an already accepted user JWT; it exposes only evaluated results through its own authenticated browser route. Targeting context is input data, not authorization. Browser visibility never authorizes a backend action.

An SDK/configuration-feed approach was rejected by the user. An internal-only evaluator would not satisfy the approved cross-application journey. JWT issuer/audience/signature validation remains enabled: an unrelated application's token is not accepted merely because it is called a JWT. Examples must show forwarding a token accepted by this API, without hard-coded accounts, secret keys or an anonymous fallback. Integrating additional identity providers or changing sibling applications is outside this repository's changes.

The API remains one application. Use focused existing command/query files and domain methods, one cohesive evaluator, and one small scheduled-work executor if owner reuse does not cover the responsibility. Do not create a service per strategy/operator. Owner reuse must be checked again for concrete APIs at implementation time.

## Upstream reuse evidence

Existing graph queries searched the main project and both exact upstream IDs for caching, scheduling, evaluation, authorization, user identity and auditing. No graph refresh/index operation was invoked.

| Owner | Findings and design consequence |
| --- | --- |
| BusinessMaker (`D-SourceCodes-Mine-BusinessMaker-business-maker-framework`) | Existing `ICacheAdapter` provides Add/Get/RemoveCache and an in-memory implementation; use the installed compatible capability for snapshot caching. `IUserInfoService`/`WebUserInfoService` expose claims/current-user information; normalize `sub` and NameIdentifier consistently instead of duplicating identity infrastructure. Framework audit interception supplies audit fields, not full flag history. |
| EdgeServices (`D-SourceCodes-Mine-EdgeServices-edge-services-building-block`) | `SecurityExtensions` supplies general JWT/scope helpers; preserve the application's existing local JWT validator and add application-specific permissions through supported ASP.NET authorization. `MassTrnasitServiceExtensions` supports delayed RabbitMQ scheduling; no broker is configured here, so this does not directly fulfill durable local change execution. Do not introduce a broker solely for scheduling. |

Inspected owner paths: BusinessMaker `src/00-Shared/02-Abstractions/Shared.Abstractions.Caching/ICacheAdapter.cs`, `src/00-Shared/01-Extensions/Caching/Shared.Extensions.Caching.InMemory/Services/InMemoryCacheAdapter.cs`, `src/00-Shared/02-Abstractions/Shared.Abstractions.UserManagement/IUserInfoService.cs`, `src/00-Shared/01-Extensions/UserManagement/Shared.Extensions.UserManagement/Services/WebUserInfoService.cs`; EdgeServices `src/EdgeServicesBuildingBlock.Shared/Extensions/SecurityExtensions.cs`, `src/EdgeServicesBuildingBlock.Shared/Extensions/ServiceCollectionExtensions/MassTrnasitServiceExtensions.cs` and its Shared project file.

Coverage flagged changed metadata/untracked configuration; these files were read directly. Main graph generation was `2026-10-26T08:20:29Z`; owner generations were 2026-10-07. Sibling source is not a guarantee about the installed package. Before writing integrations, verify the resolved package assets/API, including cache generic/null behavior and claim mapping. No existing feature evaluator or database-backed change-request executor was found in the targeted graph queries and owner source searches; this is a bounded finding, not proof about every owner capability.

## 1. Correct the foundation

### Authorization and sessions

Approved permission rules:

| Operation | Permission |
| --- | --- |
| Global users/roles/environments and legacy token administration | Admin or RootAdmin; only RootAdmin may grant/remove RootAdmin; preserve at least one active RootAdmin |
| Create project | Editor or above; creator becomes Owner atomically |
| Read/evaluate/project metrics/history | Admin/RootAdmin globally; otherwise active project member of any project role |
| Edit flags, strategies, variants, segments or request changes | Admin/RootAdmin globally; otherwise global Editor plus project Member/Owner |
| Manage project members/settings, delete project, protect an environment within project | Admin/RootAdmin globally; otherwise global Editor plus project Owner |
| Approve/apply/schedule | Same project-owner permission; approver must differ from creator, including administrators |

Existing global segments have no project owner field. Add nullable ProjectId: null is an explicitly shared segment managed by Admin/RootAdmin; project segments use project permissions. Preserve existing segments as shared. IsPublic does not grant anonymous access or bypass project membership. Project strategies may reference shared or same-project segments only. Do not allow read/list endpoints to bypass membership through an optional project filter.

Shared-segment changes that affect protected environments use a **Global** change request, authored by Admin/RootAdmin and approved by a different active RootAdmin. This is an explicit global-administration rule; it does not require a separate request per affected project. Its affected project/environment set and per-resource revisions are captured at submission and rechecked at apply; new references or changed scope invalidate review. Applying it updates all affected configuration revisions and history atomically. Non-global requests remain restricted to their own project/environment. Introduce a request scope discriminator and nullable ProjectId with database constraints; global requests may contain only shared-segment or global-environment-protection actions. Existing requests migrate as Project scoped.

Derive CreatedBy/ReviewedBy from the principal. Existing body identity fields, where retained temporarily for compatibility, must match the principal or be rejected in Persian. Apply authorization inside shared application paths as well as HTTP boundaries so scheduled execution cannot accidentally bypass protection.

Check account activity and current role/security version when accepting JWTs. Deactivation, password changes and role changes invalidate relevant sessions; logout revokes all refresh tokens and invalidates current sessions according to an explicit user security version. Refresh checks activity, rotates once atomically, and rejects concurrent reuse. Normalize supported identity claims centrally; never trust a role or actor ID from request JSON.

### Initialization, invariants and existing handlers

- Creating a feature initializes disabled associations for all existing environments; creating an environment initializes disabled associations for all non-archived features. Creating/enabling associations must be idempotent under concurrent requests. Environments remain global in this version.
- Add a deliberate data-repair migration for missing feature/environment associations, initially disabled. Do not run migrations on a shared database as part of implementation.
- Enforce project limits and duplicate guards in the create path with database-backed concurrency protection. Keep the existing global feature-name uniqueness rule and document it; do not silently introduce duplicate names across projects.
- Load or directly query scoped children before removing members, strategies, tokens or constraints; tests use fresh DbContexts. Retain legacy token endpoints/data without turning them into a new credential system.
- Persist Environment.Name updates with uniqueness checks. Handle both GradualRollout and FlexibleRollout parameters. Expose lifecycle transitions and ensure archived/disabled flags cannot evaluate true.
- Remove duplicate registration calls and production demo controllers. Gate administrator bootstrap behind explicit opt-in, external credentials and an empty/admin-absent check; no fixed credentials or unconditional seeding.

### Persistence

Use a single coherent Constraint entity mapping with nullable SegmentId/StrategyId and a database check requiring exactly one owner. Preserve existing constraint data through a migration and repair owner inconsistencies explicitly; do not discard rows silently. Both value and ownership changes must persist correctly. Keep public constraint payloads unchanged where possible.

Map strategy constraints and variants as real navigations; remove Ignore calls. A Variant has exactly one owner: strategy or feature environment. Retain legacy environment-level variants as a fallback, but new targeting uses strategy variants. Remove the duplicate environment-variant relationship configuration. Add collection value comparers or supported JSON/owned mappings where necessary, and prove mutation tracking with PostgreSQL integration tests.

Add optimistic concurrency revisions to mutable configuration/governance state. Record expected revisions in change items and reject conflicting updates with HTTP 409 and Persian problem details. Migration scripts must be reviewable and preserve data; verify both fresh database and upgrade from the current snapshot.

## 2. One complete evaluation journey

Proposed ordinary API surface uses `/api/v1`, existing JWT authentication and current controller conventions:

| Operation | Contract and behavior |
| --- | --- |
| `POST /api/v1/Evaluations` | Body: projectId, environmentId, appName, optional featureNames (max 100), context and per-name defaults. Return evaluated flag results only; no strategies/constraints/credentials. Missing featureNames evaluates the project's active flags with a bounded limit of 1,000; larger projects must specify batches. |
| `POST /api/v1/Evaluations/preview` | Editor/project-write permission; same evaluator, with matched strategy/constraint explanation. Never record usage or last-seen. |

Context: optional userId, sessionId, remoteAddress, applicationHostname, currentTime, and a string-to-string properties dictionary. appName is required (1–128 characters). Names and context keys are case-sensitive; string comparisons use ordinal semantics unless a constraint explicitly sets CaseInsensitive. Limit context properties to 50, each value to 1,024 characters, request body to 64 KiB. Use invariant numeric parsing and UTC dates. Runtime currentTime comes from server UTC; only preview permits a supplied simulation time.

Each flag result contains name, enabled, variant `{name, enabled, payload}`, reason code and configuration revision. Response includes evaluatedAt, snapshotLoadedAt, stale, refreshAfterSeconds and maxStaleSeconds. Stable reason codes are identifiers; human-facing error messages are Persian. Missing requested flags use explicit caller defaults (false if omitted), reason `not_found`; permission failures never become defaults. Unknown project/environment or disabled environment is handled distinctly and tested.

Flag evaluation: inactive environment, archived flag or disabled feature/environment => false. No active strategies => false. Evaluate strategies by SortOrder then stable ID; OR strategies, AND strategy constraints and every referenced segment's constraints. The first matching strategy supplies variants. Missing referenced segments or malformed stored configuration fail closed for that strategy and produce an operational diagnostic; they do not silently broaden targeting.

Examples belong in `examples/` as small runnable .NET HTTP-caller and TypeScript browser-through-backend examples, not SDK libraries or an admin application. A minimal demo backend proxy forwards only allowed context and a JWT accepted by this API; it never forwards targeting rules to the browser. An end-to-end test creates project/environment/flag, configures targeting/variant, enables the flag, calls the backend proxy and observes the browser contract. No sibling application is edited or deployed.

## 3. Targeting, variants, caching and visibility

### Complete targeting

Extend existing strategy operations to create/update parameters, constraints, segment references, enabled status and sort order in one cohesive command. Reuse this command's payload for change requests. Provide reorder and delete operations; validate segment scope, uniqueness and missing references. Guard protected environments on all equivalent write paths, including segment changes affecting their strategies.

Support all currently declared strategies except Custom, which must be rejected explicitly until a concrete custom implementation is registered; never accept and silently treat it as Default. Default matches true after constraints. UserWithId uses exact user IDs. RemoteAddress supports canonical IP/CIDR matching. ApplicationHost matches context applicationHostname against the existing ApplicationNames list. Both rollout enum members share the same deterministic percentage semantics.

Support every existing ConstraintOperator plus SemverGte and SemverLte as additive enum values. Numeric/date/SemVer operators require exactly one valid comparison value; string/inclusion operators accept a nonempty list. Missing or malformed evaluated context fails the constraint before inversion, so `NOT_IN`/Inverted cannot grant access through absent context. AlwaysTrue/AlwaysFalse need no values. Enforce known operators, field lengths and operator cardinality at write time.

Proposed deterministic assignment: SHA-256 UTF-8 over an unambiguous length-prefixed tuple (version `v1`, project ID, groupId or feature name, stickiness field, stickiness value, purpose). Read the first 8 bytes as an unsigned big-endian integer and take modulo 10,000. Rollout matches bucket < percentage × 100; percentages are integers 0–100. Variant assignment uses a distinct `variant` purpose. Publish cross-language fixtures. This is an application-defined algorithm, not Unleash compatibility.

Stickiness: default uses userId then sessionId; explicit context-property/userId/sessionId keys require a nonempty value. Missing stickiness fails closed for partial rollout/variant assignment. At 0% always false; at 100% a rollout does not require stickiness. Random stickiness, if requested, uses per-evaluation randomness and is explicitly non-sticky. groupId correlates related flags within the same project.

### Variants and advanced rollout scope

Use strategy-level variants with unique ordinal names, integer weights 0–1000 totaling 1000 for nonempty sets, optional JSON payloads capped at 16 KiB, and sticky weighted selection. Return `{name:"disabled", enabled:false, payload:null}` when the flag is false or no configured variant applies; a flag may be true without a variant. Existing environment variants are used only if the matched strategy has none. Payload changes must participate in equality/change tracking and revisioning.

For this design, **advanced rollout features** means: configurable stable stickiness/group correlation, percentage progression in scheduled stages, time-window constraints, strategy ordering, and same-project prerequisite flags with optional required variants. Add dependency management with cycle detection and a maximum evaluation depth of 20. A child requires its own rules and all prerequisites to pass in the same environment. A missing/archived parent fails closed. No automated statistical experimentation, external signal integration, release-template product, streaming or multi-region infrastructure is implied.

Scheduled rollout stages are ordinary approved change requests containing future percentage updates, not a separate orchestration platform. Overlapping/conflicting stages are rejected or suspended through revision checks. A reusable template UI/API is excluded from this version.

### Reliable refresh and caching

Cache immutable configuration snapshots by authorized project/environment; do not cache evaluation results by flag name alone. Reuse the existing compatible BusinessMaker cache adapter. Bound snapshots to 1,000 project/environment keys with eviction, deduplicate simultaneous loads, and authorize every request before using a cached snapshot. Never cache tokens or caller claims.

Proposed defaults: refresh after 5 seconds; last-known-good fallback permitted for at most 60 seconds since successful load, explicitly marked stale. Local writes invalidate local snapshots only after commit; other instances recheck within the refresh window. Database outage plus no permitted snapshot returns HTTP 503 with Persian error, rather than pretending an authoritative false evaluation succeeded. Authorization/session checks fail closed during outage; cached configuration never grants access when current permission cannot be established.

Consumer examples use a 2-second HTTP timeout, bounded retries for transient failures with jitter, cancellation and explicit caller defaults. Backend cache keys include principal/project/environment, requested names/defaults and the full normalized targeting context; cache retention never exceeds the response staleness bound or JWT expiry. Authorization errors clear cached results. Browsers receive no-store responses from the backend proxy, preventing cross-user HTTP cache reuse. A restart without a snapshot uses explicit defaults until a successful fetch; there is no separate distributed client cache or SDK.

Document that a 60-second stale allowance trades emergency disable propagation for availability. Security-sensitive enforcement must use backend authorization, never flag state. Deployment may set max stale to zero for stricter freshness. Time constraints and dependency outcomes are evaluated against current time, not frozen in a cached result.

### Usage visibility

Record evaluated boolean and variant counts automatically on successful non-preview evaluation. This measures evaluation calls, not proof of a rendered browser impression. Update last-seen from actual evaluation; enabling a flag no longer sets LastSeenAt. Track appName/project/environment and last-observed time without creating application credentials.

Provide optional authenticated, project-scoped impression reporting through the consuming backend (`POST /api/v1/Metrics/impressions`). Batch limit 100, UUID batch ID for idempotency retained 7 days, nonnegative bounded counts, no arbitrary user/context storage. Store aggregate hourly flag/variant counters and application last-seen. Preserve up to 90 days of hourly aggregates; cleanup runs in bounded batches. No raw user IDs/IPs go into metrics/history by default.

`GET /api/v1/Metrics` supports projectId/environmentId, UTC from/to (maximum 31 days), optional feature/app filters and paging (default 50, max 200). `GET /api/v1/Metrics/applications` exposes scoped last-seen records. Treat aggregate writes as short transactions; telemetry failures cannot flip a flag decision. Report telemetry persistence failures through logs/health rather than silently claiming complete metrics.

## 4. Approvals, durable scheduling and history

Add project/environment approval settings (enabled, required approvals 1–5). Keep Environment.Protected as a global mandatory gate: a protected environment always requires at least one non-author approval across all projects. Project owners can enable stricter rules but cannot disable the global gate. Direct environment flag/strategy/variant changes route through requests or return a Persian conflict explaining approval is required. Edits to shared/project segments affecting protected environments also require reviewed changes. Changes to protection settings require owner/admin permissions and history; they must not provide an unchecked bypass.

Making a protection policy stricter may be done directly by its authorized owner/admin and is audited. Weakening a project/environment policy requires a project change request satisfying the **current** policy (at least one non-author owner approval when protection is currently enabled). Disabling global Environment.Protected requires a Global request approved by a different active RootAdmin; an ordinary environment-update call cannot do it. Capture approval requirements at submission and enforce the stricter of captured and current requirements at apply. An approval-setting action cannot lower the requirements used to approve itself. Project owners cannot weaken global protection.

Change requests use a versioned, validated action vocabulary: set flag enabled, upsert/delete/reorder strategy, replace variants, replace segment constraints, update feature metadata/lifecycle, and manage prerequisites. Unknown actions or malformed/cross-project payloads are rejected before submission. Persist human-readable rejection reason, immutable approvals and expected target revisions. Remove client control over acting identities. Any permitted edit to an in-review/approved request invalidates approvals and returns it to Draft; applied/cancelled requests are immutable.

The Global scope exception above permits only its declared shared-segment/global-protection actions and a validated affected-resource set, not arbitrary cross-project flag payloads. Global request read/list/preview access is Admin/RootAdmin only; protected shared-segment/global-protection approval/apply/schedule additionally requires the global rule above. The permission matrix for project requests is unchanged.

Apply all items and history records in one database transaction using shared mutation/domain code, then mark Applied. If any item fails, roll back all items and expose a Persian failure. Repeated application of an already applied request returns the existing outcome without reapplying changes. Preview shows before/after without side effects.

The small hosted executor polls due requests every 5 seconds (configurable), claims work transactionally in PostgreSQL so two instances cannot apply it twice, and uses the same application path as manual apply. Revalidate the scheduling actor's active status/permissions and approvals at execution. Require a future UTC schedule; retries have persisted attempt/error metadata, bounded exponential delay and a limit of 5 attempts for transient failures. Revision/permission/deleted-resource conflicts suspend the request for review, not automatic retry. Graceful shutdown cancels work; database transactions/leases make restart recovery safe. Persist the resulting failed/suspended state and surface it in the API.

For multi-stage percentage progression, a request may reference one immutable predecessor request in the same project/environment. Limit chains to 20 and prohibit cycles; stage times must increase. The first stage pins baseline target revisions. Each successor explicitly pins the predecessor and consumes its recorded resulting revisions for targets it changes; other target revisions remain pinned to the submitted baseline. A stage runs only after its predecessor applied successfully. Unrelated edits still cause conflicts; a changed/rejected/cancelled/suspended predecessor suspends successors and requires review. This allows preapproved progression without treating the expected predecessor write as an accidental conflict. Changing a stage invalidates its approvals and dependent successor approvals.

Write append-only history in the same transaction as each audited configuration/protection/governance mutation: actor/system origin, UTC timestamp, project/environment, operation, resource ID, revision and safe before/after configuration snapshots. Do not store credentials, JWTs, passwords or evaluation context. Scope and page history reads. Retain history until an explicit retention policy is configured; no silent audit deletion. Restore is a new validated mutation with expected revision and normal approval rules; never edit old history. A restored configuration gets a new revision.

## Cross-cutting implementation requirements

- All authored validators, Result/exception messages, binding/auth/fallback and scheduler failure messages are Persian; stable codes, routes, identifiers and property names remain English. Consistently map validation to 400, unauthenticated to 401, forbidden to 403, missing resource to 404, concurrent/protected-state conflict to 409, unavailable dependency to 503. Preserve documented success shapes and update contracts for any deliberate changes.
- Preserve existing routes unless explicitly retired; add fields/endpoints compatibly where safe. Do not advertise existing API-token endpoints as usable application authentication. No expansion of that excluded feature.
- Update `docs/ts-contracts/<Feature>.ts` alongside every endpoint change, including comments describing forms/inputs, permissions, defaults, enums, payloads, errors, concurrency and client examples. Add Evaluations, Metrics and History contracts; extend existing Features/FeatureEnvironments/Segments/ChangeRequests/Projects/Auth contracts.
- Update root AGENTS/.ai context and docs to record exclusions and completed behavior; retain the historical audit with a scope/status note rather than rewriting inspection history.
- Update Docker to .NET 10 and actual project/central-package layout. Add meaningful CI restore/build/tests and configuration validation. Use environment variables/user-secrets, restricted configured CORS, production-safe logging and database readiness. Review dependency advisories via owning package paths without broad unrelated package churn.
- Add migrations and upgrade documentation, including explicit handling of existing invalid constraint ownership, missing environment associations and current fixed administrator bootstrap. Do not apply them to an existing/shared database automatically.

## Verification and completion gates

Use one focused test project with unit tests for evaluator/state rules and ASP.NET/PostgreSQL integration tests for persistence/permissions/HTTP behavior. Tests use disposable databases and controlled time; never production configuration or unconditional seeding. Real PostgreSQL is required for JSON, locking, concurrent refresh, constraints, migrations and transactional scheduling tests; an in-memory provider is insufficient evidence.

| Milestone | Required evidence |
| --- | --- |
| 1 — Foundation | Viewer cannot promote users/write; nonmember cannot list/read/mutate project resources; root safeguards; identity spoof rejection; create flag/environment in either order; legacy association backfill; limit concurrency; constraint/variant round trips; child removal with fresh contexts; rename/rollout persistence; inactive refresh and session invalidation; Persian failures |
| 2 — Journey | Authenticated backend and browser-through-backend examples exercise configuration → targeting → enable → evaluated results; anonymous/foreign JWT rejected; flags do not expose rules; two distinct contexts yield different correct results |
| 3 — Consumption | Cross-language hash fixtures; 0/100 boundaries; every operator including missing/inverted context; segment scope/changes; variant weights/payload updates; cache separation/load concurrency/staleness/outage/recovery; bounded metrics and idempotent impressions; actual last-seen; no preview metrics |
| 4 — Governance | Protected direct writes rejected; reviewed changes actually update all targets atomically; edit invalidates approvals; actor cannot self-approve/spoof; dependencies detect cycles/depth; scheduled progression survives restart and two-worker contention; permission/revision conflicts suspend; history before/after and approved restore |
| Delivery | Restore + Release build; test counts/results; Docker build where runtime available; precise TypeScript compile/contracts; migration fresh/upgrade checks; documentation links; no indexing artifacts or unrelated edits |

Completion means every requested milestone and enumerated defect has executable evidence or an explicit environmental blocker; a plan, compile-only result, fake in-memory persistence test or status-only endpoint is not completion. Report remaining blockers plainly and retain unchecked work rather than silently shrinking scope.

## Defect traceability

| Requested defect | Design location |
| --- | --- |
| New flags cannot complete configuration | Initialization and migration backfill; milestone 1 create-in-either-order tests |
| Management permissions incomplete | Permission matrix, principal-derived identities, scoped reads/writes and root safeguards |
| EF mappings contradict queries | Coherent owner mapping, real navigations, comparers and PostgreSQL round trips |
| Change requests do not apply | Atomic validated item application and durable executor |
| Collection removals fail | Scoped child loading and fresh-context regression checks |
| Name/GradualRollout ignored | Rename validation/persistence and shared rollout semantics |
| Refresh ignores account state | Activity/security version and atomic single-use rotation |
| Validation/Persian errors/history/deployment/tests | Cross-cutting requirements, history transactions and delivery gates |

## Review decisions

The user approved this design on 2026-10-26 and authorized implementation. This includes the permission matrix, numeric/time/cache/retention limits, hash algorithm, owner-preserving migration strategy, legacy token compatibility, and precise advanced-rollout scope above. A builder may choose internal filenames and test tooling compatible with the repository, but may not silently alter these externally observable rules.

Builder review resolved three consequential ambiguities: protected shared-segment changes use bounded Global requests with an independent RootAdmin approval; protection weakening is itself reviewed under the existing/stricter gate; staged progression explicitly links predecessor output revisions. Integration tests must cover each rule, including changed reference sets and successor suspension.
