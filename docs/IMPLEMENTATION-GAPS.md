# Implementation gaps against an Unleash-like product

Inspection date: 2026-10-26. Goal: centrally manage flags used across backend and frontend applications.

**Subsequent scope clarification:** the user excludes SDK access, application credentials and an administration UI. Ordinary JWT evaluation endpoints through consuming backends are allowed. Those exclusions supersede the recommendations below; retain this report as historical inspection evidence. See [spec 02](specs/02-feature-management-completion.specs.md) for the proposed implementation scope.

**Assessment: this checkout implements an administrative API foundation, but it is not yet an operational feature-flag platform for consuming applications.** The largest missing pieces are evaluation, SDK-facing delivery, usable application tokens, and resilient application integration. Several existing administrative flows also need correction.

This is an inspection report, not an approved implementation specification. The archived [01 proposal](specs/01-sdk-evaluation-proposal.specs.md) contains ideas and sample code; its contents are not implementation evidence. Future specifications belong in the numbered `docs/specs/` collection.

## Evidence and limits

- Inspected the four solution projects, domain models, application requests/handlers and mappings, controllers, composition/authentication, EF configurations and current migration snapshot, deployment files and repository inventory.
- Used the existing `D-SourceCodes-Mine-feature-flag` graph, generation `2026-10-26T06:36:50Z`, for discovery and relationship checks. Checked all 187 C#/project/props paths and the relevant source scopes. Coverage reported changed metadata or untracked configuration; material findings were checked against live files and source searches. `bin`/`obj` are excluded from the graph.
- Read relevant BusinessMaker base-controller/context/registration source after checking its index coverage. Sibling HEAD is reference evidence, not proof of identical published-package internals.
- `dotnet build feature-flag.sln -c Release --no-restore --verbosity minimal` succeeded outside the sandbox: **43 warnings, 0 errors**. The sandbox attempt failed without a useful compiler error. No restore or package updates were performed.
- No application startup, database migrations, seeded database, HTTP tests, or SDK interoperability tests were run. EF runtime effects identified below still need disposable-database verification. No automated test project or active CI workflow was found in this checkout.
- Absence findings refer to executable source/configuration in this checkout, excluding generated build output. A separate frontend or SDK repository could exist; it was not provided as part of this application audit.
- No repository was indexed or re-indexed. No application source was changed for this inspection.

## Comparison baseline

Unleash distinguishes management from application consumption: backend SDKs fetch definitions and evaluate locally; frontend clients obtain context-specific evaluated results. Its Client API also accepts SDK registration and metrics. See the [official API overview](https://docs.getunleash.io/api/).

Targeting combines strategies with OR and constraints within a strategy with AND, and uses stable rollout assignment. See [activation strategies](https://docs.getunleash.io/concepts/activation-strategies). Variants add weighted, sticky assignments and payloads; see [strategy variants](https://docs.getunleash.io/concepts/strategy-variants).

“Like Unleash” does not automatically mean every Enterprise feature is required. Dependencies, advanced roles, approval workflows and other capabilities vary by edition. See the [official OSS/Enterprise comparison](https://docs.getunleash.io/support/oss-comparison). The priorities below are based on this project's stated cross-application goal.

## Current capability inventory

| Capability | Current implementation | Readiness |
| --- | --- | --- |
| Projects, members, environments | Models, persistence and management endpoints | Present; permission and collection-loading gaps |
| Flag metadata, tags, archive/stale state | Models and management endpoints | Present; environment creation path missing |
| Enable/disable per environment | Domain methods and endpoints | Requires an association no existing create flow initializes |
| Activation strategies | Parameter models and add/remove endpoints | Partial; no evaluation or complete editing/targeting API |
| Reusable segments | Segment CRUD and constraint management | Partial; not connectable through strategy endpoints |
| Variants | Domain value object and collections | No management/evaluation/client delivery workflow |
| Application tokens | Model, create/list/revoke endpoints | Partial; no usable credential/authentication path |
| Local user authentication | BCrypt, JWT, refresh and logout handlers | Present; authorization and session-state gaps |
| Change requests | Draft/review/approval/schedule/apply status transitions | Partial; no actual change application or scheduled execution |
| Backend SDK configuration API | None found | Missing |
| Frontend evaluated flag API | None found | Missing |
| SDK libraries, refresh and offline behavior | None found | Missing |
| Flag usage metrics and SDK registry | Value object only for flag metrics | Missing operational workflow |
| Administration UI | No UI application in this checkout | Missing here; external UI not assessed |

## Blockers before cross-application use

### G01 — Evaluation engine and context contract are missing

There is no executable flag evaluator or evaluation-context contract. `StrategyParameters`, `Constraint`, `StrategyType` and `ConstraintOperator` describe data, but do not decide whether a flag is enabled for a caller.

Required behavior includes environment/project isolation, environment and flag enablement, archived flags, OR across strategies, AND across constraints/segments, missing/invalid context handling, user/IP/hostname targeting, stable percentage rollout, and caller-defined defaults. Define which semantics must match Unleash, including hash inputs and stickiness, before sharing evaluation across languages.

Evidence: [strategy parameters](../src/Domain/ValueObjects/StrategyParameters.cs), [constraints](../src/Domain/ValueObjects/Constraint.cs), [strategy types](../src/Domain/Enums/StrategyType.cs), [operators](../src/Domain/Enums/ConstraintOperator.cs), and the complete controller/application inventory.

Acceptance: one flag can produce different, repeatable answers for two users in the same environment; the same context/configuration produces consistent answers across supported backend languages and frontend evaluation.

### G02 — SDK-facing APIs and protocol compatibility are missing

The existing feature endpoints are management DTOs protected by user JWTs. There is no backend configuration feed or frontend context-evaluation endpoint. Current routes, strategy names, parameter names and DTOs are not the Unleash SDK protocol.

Choose explicitly between an Unleash-compatible subset, verified against actual SDKs, and your own versioned contract with your own clients. Merely adding an endpoint named `evaluate` does not make official Unleash SDKs compatible.

A backend feed should return only authorized project/environment definitions. A browser API should return evaluated flag/variant results and avoid disclosing targeting rules, internal user lists, or backend credentials. Frontend flags must not substitute for backend authorization.

Evidence: [FeaturesController](../src/WebApi/Controllers/FeaturesController.cs), [feature DTOs](../src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs), [route base](../src/WebApi/Controllers/ApiControllerBase.cs), [API contracts](ts-contracts/README.md).

### G03 — Application token creation does not produce usable access

`CreateApiTokenCommandHandler` stores a GUID string in `TokenHash` and returns only the record ID. The field is not a hash produced from a separately delivered secret. Authentication composition registers JWT bearer authentication; there is no application-token lookup/validation scheme, endpoint token-type enforcement, or project scoping. Tokens contain an environment ID but no project scope.

Implement secure secret generation, one-time secret return, stored hash, token-type separation, expiration/revocation enforcement and authorized project/environment scope. A frontend credential must not grant management or backend-definition access.

Evidence: [create token](../src/Application/Features/Environments/Commands/CreateApiToken/CreateApiTokenCommand.cs), [token model](../src/Domain/Aggregates/ApiTokenAggregate/ApiToken.cs), [JWT registration](../src/WebApi/Configuration/JwtConfiguration.cs).

### G04 — Creating a flag does not initialize its environments

`CreateFeatureCommandHandler` creates the Feature only. `CreateEnvironmentCommandHandler` also does not associate existing flags. `Feature.InitializeEnvironment` exists, but has no source callers and no exposed initialization operation. Enable/disable/add-strategy handlers require a pre-existing `FeatureEnvironment` row.

Consequently, the ordinary create-project → create-environment → create-flag → enable/add-strategy path cannot complete using the exposed API alone. Define project/environment availability and initialize the appropriate associations, including the case where an environment is created after flags already exist.

Evidence: [create feature](../src/Application/Features/Features/Commands/CreateFeature/CreateFeatureCommand.cs), [create environment](../src/Application/Features/Environments/Commands/CreateEnvironment/CreateEnvironmentCommand.cs), [initialization method](../src/Domain/Aggregates/FeatureAggregate/Feature.cs), [enable handler](../src/Application/Features/FeatureEnvironments/Commands/EnableFeatureEnvironment/EnableFeatureEnvironmentCommand.cs).

### G05 — Management authorization is incomplete

Business controllers use plain `[Authorize]`. `AdminOnly` and `EditorOrAbove` policies are defined but not assigned to these actions. The inspected handlers do not check current-user role, project membership, or ownership; list handlers do not scope results to membership. `UsersController` exposes create/promote/deactivate actions with the same basic authenticated requirement.

At the inspected source level, a Viewer can reach administrative mutation paths, including role changes, and project membership does not establish an access boundary. Verify this with HTTP tests before deployment; no live exploit test was performed.

Enforce role/action permissions and project-level access on reads and writes. Derive change-request `CreatedBy`/`ReviewerId` from the authenticated principal, rather than trusting request-body identities. The current self-approval guard compares client-supplied GUIDs and does not establish who performed the approval.

Evidence: [UsersController](../src/WebApi/Controllers/UsersController.cs), [policies](../src/WebApi/Configuration/JwtConfiguration.cs), [project list](../src/Application/Features/Projects/Queries/GetAllProjects/GetAllProjectsQuery.cs), [approval handler](../src/Application/Features/ChangeRequests/Commands/ApproveChangeRequest/ApproveChangeRequestCommand.cs), [creation handler](../src/Application/Features/ChangeRequests/Commands/CreateChangeRequest/CreateChangeRequestCommand.cs).

### G06 — EF strategy/constraint/variant mappings contradict the read paths

`FeatureStrategyConfiguration` configures constraints/variants and then explicitly ignores both navigations. Feature reads nevertheless use `ThenInclude(s => s.Constraints)`. The current model snapshot has no mapped strategy-constraint navigation. These read paths are expected to fail EF navigation validation or projection translation; reproduce against the actual runtime model before choosing a fix.

Both `StrategyConstraintConfiguration` and `SegmentConstraintConfiguration` configure the same `Constraint` CLR entity. Both DbSet properties reference `Set<Constraint>()`; naming them differently does not create separate entity types/tables. The snapshot has one `StrategyConstraints` entity table with both shadow owner columns, not the two independent mappings suggested by comments.

`FeatureEnvironmentConfiguration` also configures its same Variants navigation twice with different foreign-key names. Strategy-level variants are ignored. Establish one coherent ownership model and verify create/read/update/delete round trips before relying on targeting persistence.

Evidence: [strategy configuration](../src/Infrastructure/Configurations/FeatureStrategyConfiguration.cs), [strategy constraints](../src/Infrastructure/Configurations/StrategyConstraintConfiguration.cs), [segment constraints](../src/Infrastructure/Configurations/SegmentConstraintConfiguration.cs), [environment mapping](../src/Infrastructure/Configurations/FeatureEnvironmentConfiguration.cs), [DbContext](../src/Infrastructure/ApplicationDbContext.cs), [snapshot](../src/Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs), [feature read](../src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs).

## Missing core feature-management behavior

### G07 — Strategy editing, constraints and segment attachment are incomplete

The API adds/removes strategies but cannot update parameters, reorder strategies, disable an individual strategy, attach/detach segments, or add/edit/remove strategy constraints. Domain methods exist for several operations, but the application endpoints do not expose them. Creating a segment therefore does not make it usable for targeting through this API.

The add-strategy command has no constraints/segment IDs/custom-parameter inputs. Its switch handles `FlexibleRollout`, but `GradualRollout` falls through to empty default parameters, ignoring its supplied percentage. `Custom` likewise has no usable custom-parameter configuration path.

Evidence: [strategy controller](../src/WebApi/Controllers/FeatureEnvironmentsController.cs), [add strategy](../src/Application/Features/FeatureEnvironments/Commands/AddFeatureStrategy/AddFeatureStrategyCommand.cs), [domain methods](../src/Domain/Aggregates/FeatureAggregate/FeatureStrategy.cs).

### G08 — Variants exist only as partial model support

There is no variant management command/endpoint, variant fields in current feature response DTOs, weighted selection, or SDK variant result. Per-variant bounds exist, but collection-level rules such as unique names and a valid total distribution are not implemented by `SetVariants`.

Complete strategy-level variants, payload semantics, assignment stability and disabled/default behavior. If Unleash interoperability is selected, align its wire payload/weight conventions rather than assuming the local dictionary payload already matches.

Evidence: [Variant](../src/Domain/ValueObjects/Variant.cs), [strategy](../src/Domain/Aggregates/FeatureAggregate/FeatureStrategy.cs), [feature response](../src/Application/Features/Features/Queries/GetFeatureById/GetFeatureByIdQuery.cs). See the upstream [variant behavior](https://docs.getunleash.io/concepts/strategy-variants).

### G09 — Consumer caching, refresh and failure behavior are missing

Registering a general-purpose in-memory cache is not a working flag delivery cache. There are no consumer SDKs here, last-known-good snapshots, startup/bootstrap behavior, refresh policy, request timeouts/backoff, configuration revisions/conditional HTTP responses, or documented stale/offline defaults.

Specify a bounded propagation delay for changes and emergency disable operations. Backend applications should normally evaluate cached definitions locally; browsers need a safe evaluated-result refresh path. Define startup behavior and what happens when the flag server is unavailable. Polling is sufficient initially; streaming and an Edge deployment can wait for a demonstrated latency/load requirement.

Evidence: [application registration](../src/Application/DependencyInjections.cs), controller inventory and repository file inventory.

### G10 — Usage metrics, impressions and connected-application tracking are missing

`FeatureMetrics` has no application ingestion/storage/query workflow. `ImpressionDataEnabled` is stored but does not emit impression events. There are no SDK registration/heartbeat APIs or an application registry. `FeatureEnvironment.Enable()` sets `LastSeenAt` at enable time; this does not represent a real SDK observation.

Add bounded metrics batches with app/environment identity and boolean/variant counts, a retention policy, and usage queries. Define how actual observations update last-seen and how optional impressions reach your analytics tooling without collecting unnecessary user data. Existing OpenTelemetry/demo counters do not provide flag usage analytics.

Evidence: [metrics model](../src/Domain/ValueObjects/FeatureMetrics.cs), [feature environment](../src/Domain/Aggregates/FeatureAggregate/FeatureEnvironment.cs), [impression toggle](../src/Application/Features/Features/Commands/EnableFeatureImpressionData/EnableFeatureImpressionDataCommand.cs), [demo telemetry](../src/WebApi/Controllers/TestController.cs).

### G11 — Administration UI and evaluation playground are absent here

There is no frontend application in this checkout. The TypeScript files are endpoint contracts, not an administration UI or runtime SDK. The product still needs flag/project/environment management screens, strategy and segment forms, credential creation, permission-aware controls and a context playground showing why an evaluation matched or failed. If a separate UI already exists, assess its coverage against these APIs separately.

Evidence: solution and repository inventory, [TypeScript contracts](ts-contracts/README.md). The [Unleash playground](https://docs.getunleash.io/concepts/playground) is a useful behavior reference.

## Existing flows requiring correction

### G12 — Change requests do not apply or enforce changes

`ApplyChangeRequestCommandHandler` changes status and contains `TODO: Apply all items to the actual features`. Scheduling only stores status/date; no executor is registered in this checkout. The workflow cannot currently change flags at approval/application time.

Additionally, direct enable/disable/strategy endpoints do not consult an approval requirement. `Environment.Protected` has no enforcement path. Items may be added in any status; `AddItem` does not invalidate approvals or restrict edits after application. Reviewer assignment is not enforced, rejection reason is discarded, and scheduling has no future-time validation.

Required before claiming governance: validated action/payload schemas; authoritative identities/permissions; protection rules; actual atomic item application; conflict detection; approval invalidation on edits; idempotent scheduled execution and visible failures. For comparison, see [Unleash change requests](https://docs.getunleash.io/concepts/change-requests).

Evidence: [apply handler](../src/Application/Features/ChangeRequests/Commands/ApplyChangeRequest/ApplyChangeRequestCommand.cs), [schedule handler](../src/Application/Features/ChangeRequests/Commands/ScheduleChangeRequest/ScheduleChangeRequestCommand.cs), [domain workflow](../src/Domain/Aggregates/ChangeRequestAggregate/ChangeRequest.cs), [environment model](../src/Domain/Aggregates/EnvironmentAggregate/Environment.cs).

### G13 — Collection mutations do not load the required state

Remove-project-member, revoke-environment-token, remove-strategy and remove-segment-constraint handlers load only the parent. No lazy-loading setup or AutoInclude was found. Their domain methods search initially empty collections, so a fresh request can report an existing child missing or reject a valid constraint index. Adding members also cannot reliably check existing membership without loading/querying it; a database uniqueness error is not a complete user-facing validation path.

Load the relevant collection or directly query the scoped child, then verify persistence and missing-child behavior in fresh DbContexts.

Evidence: [remove member](../src/Application/Features/Projects/Commands/RemoveProjectMember/RemoveProjectMemberCommand.cs), [revoke token](../src/Application/Features/Environments/Commands/RevokeApiToken/RevokeApiTokenCommand.cs), [remove strategy](../src/Application/Features/FeatureEnvironments/Commands/RemoveFeatureStrategy/RemoveFeatureStrategyCommand.cs), [remove constraint](../src/Application/Features/Segments/Commands/RemoveSegmentConstraint/RemoveSegmentConstraintCommand.cs), [infrastructure registration](../src/Infrastructure/DependencyInjections.cs).

### G14 — Feature invariants and lifecycle are only partially connected

Create-feature bypasses `Project.AddFeature`, so the feature limit and domain duplicate guard are not applied. The database declares both global name uniqueness and filtered project/name uniqueness; explicitly choose the intended naming scope before treating repeated names across applications as supported.

`Activate` and `Complete` are not exposed through the current application workflow. Archive does not disable environment records, and there is no consumer evaluator to guarantee archived flags stop being served. Automatic stale detection/expected lifetime and cleanup usage signals are also missing.

Evidence: [project guards](../src/Domain/Aggregates/ProjectAggregate/Project.cs), [create feature](../src/Application/Features/Features/Commands/CreateFeature/CreateFeatureCommand.cs), [indexes](../src/Infrastructure/Configurations/FeatureConfiguration.cs), [feature lifecycle](../src/Domain/Aggregates/FeatureAggregate/Feature.cs).

### G15 — Environment updates silently ignore Name

`UpdateEnvironmentCommand` accepts `Name`, but the handler updates only sort order. Either implement the documented rename semantics or remove/reject the unsupported input. Environment protection is also not configurable through current endpoints.

Evidence: [update environment](../src/Application/Features/Environments/Commands/UpdateEnvironment/UpdateEnvironmentCommand.cs), [environment controller](../src/WebApi/Controllers/EnvironmentsController.cs).

### G16 — Input validation and Persian error policy are incomplete

The framework validation pipeline is registered, but no application request-validator implementations were found. A global validator namespace import is not validation. Domain checks cover only a subset of requirements; lengths, enum values, malformed operator values, names, relationship ownership and action/payload semantics need deliberate validation before database errors occur.

Legacy domain/controller/auth messages are English, including explicit ID-mismatch bodies. Automatic model-binding/authentication/framework fallback messages also need verification and localization. Keep stable identifiers/codes while making authored error text Persian.

Evidence: [validation registration](../src/Application/DependencyInjections.cs), [constraint validation](../src/Domain/ValueObjects/Constraint.cs), [controller mismatch messages](../src/WebApi/Controllers/FeaturesController.cs), [login errors](../src/Application/Features/Auth/Commands/Login/LoginCommand.cs).

### G17 — Deactivation and role changes do not fully control sessions

Login checks `IsActive`, but refresh does not. Deactivation only flips a flag; it does not revoke refresh tokens. JWT validation has no active-user/token-version check, so issued access tokens retain their original roles until expiry. A deactivated account with a valid refresh token can reach the refresh path without an active check.

Define revocation requirements for logout, deactivation, password and role changes. Implement refresh rotation concurrency protection so simultaneous requests cannot reuse the same refresh token successfully. No explicit concurrency token/version is configured for these state transitions in the inspected model.

Evidence: [refresh handler](../src/Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs), [deactivate handler](../src/Application/Features/Users/Commands/DeactivateUser/DeactivateUserCommand.cs), [user model](../src/Domain/Aggregates/UserAggregate/User.cs), [JWT validation](../src/WebApi/Configuration/JwtConfiguration.cs), [refresh mapping](../src/Infrastructure/Configurations/RefreshTokenConfiguration.cs).

## Production and operability gaps

### G18 — Durable change history and rollback are not implemented

Domain events and audit-field interception exist, but no application history query/API or persisted before/after flag configuration history was found. Events declared as record types do not by themselves establish delivered notifications or a durable audit trail. Define actor, timestamp, target project/environment, old/new configuration, and a safe restore path.

Evidence: [DbContext](../src/Infrastructure/ApplicationDbContext.cs), domain event files, controller inventory. Do not confuse this finding with an absence of all framework audit metadata.

### G19 — Deployment and dependency health need attention

The root Dockerfile uses .NET 7 and copies missing `src/Framework/Framework.csproj`, while the solution targets .NET 10. It cannot build the current solution as written. The solution lists `.github/workflows/cicd.yml`, but the file does not exist in the inspected checkout.

The successful build emitted NU1902/NU1903 notices for `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` across the existing restored dependency graphs. Review their dependency owners and upgrade paths; these are build-reported advisories, not a fresh comprehensive vulnerability assessment.

Evidence: [Dockerfile](../Dockerfile), [solution](../feature-flag.sln), [target framework](../src/Directory.build.props), build output. No dependencies were changed.

### G20 — Production security/diagnostics defaults need tightening

Startup always runs an administrator seeder containing fixed credentials; replace this with a controlled bootstrap process and external secret provisioning. Never copy those values into documentation. EF sensitive-data logging is enabled unconditionally. CORS accepts any origin; define intended browser origins as part of the frontend access design.

Demo controllers include anonymous actions throwing `NotImplementedException` or returning placeholder responses. Exclude these from production API discovery/exposure. Health checks are registered without an active database check, so endpoint presence alone does not establish database readiness. Login throttling/lockout is not configured locally.

Evidence: [startup pipeline](../src/WebApi/ProgramConfiguration/ApplicationBuilder.cs), [seeder](../src/Infrastructure/Persistence/Seed/DatabaseSeeder.cs), [sensitive logging](../src/Infrastructure/ApplicationDbContext.cs), [demo controllers](../src/WebApi/Controllers/ApiControllerBase.cs), [service composition](../src/WebApi/ProgramConfiguration/ServiceCollections.cs).

### G21 — Concurrency, bounded reads and configuration propagation are undefined

Management lists return unbounded collections. There is no explicit configuration revision/ETag contract, conflict handling for concurrent editors, or cross-instance invalidation flow. Application and Infrastructure registration are each invoked twice, which also deserves a lifetime/duplicate-registration check.

Begin with bounded/paged administrative reads and a simple versioned snapshot/refresh design. If multiple API instances cache configurations, changes must become visible consistently across them. Do not add distributed infrastructure before it is needed, but do define and test the consistency contract.

Evidence: [feature listing](../src/Application/Features/Features/Queries/GetAllFeatures/GetAllFeaturesQuery.cs), [composition](../src/WebApi/ProgramConfiguration/ServiceCollections.cs), EF configurations and controller inventory.

### G22 — Automated behavioral coverage is missing

No test project was found. Compilation cannot detect the missing initialization path, ignored EF navigations, wrong authorization, or an apply operation that changes no flag.

The first regression suite should exercise: fresh-database create/configure/evaluate; environment/project isolation; Viewer/Admin permissions; token type/expiry/revocation; constraint and segment persistence; rollout determinism across languages; variants; server outage/default behavior; real and scheduled change application; and concurrent edits/refresh requests. Add CI only around meaningful checks and the supported deployment path.

## Broader Unleash parity backlog

These are absent from the inspected executable surface but do not all block a useful first release:

| Capability | Why/when to add |
| --- | --- |
| Feature prerequisites/dependencies, including variant dependencies | Coordinated rollouts; requires explicit ordering/cycle rules and SDK support |
| Context-field catalog and allowed values | Consistent targeting forms and discoverable client requirements; evaluation can first accept documented arbitrary properties |
| Import/export and configuration promotion | Move reviewed configuration between environments/instances |
| Webhooks and external integrations | Notify deployment/operations systems of changes |
| Release templates and automated progression/safeguards | Standardized progressive delivery after evaluation and metrics work reliably |
| SSO/OIDC/SAML, groups, service accounts and fine-grained custom roles | Organization-level governance; inspect upstream identity capabilities before implementing |
| Advanced dashboards and impact metrics | Operational/product analysis beyond basic flag usage |
| Streaming, Edge caching and multi-region delivery | Scale or strict propagation requirements justify them; not a prerequisite for the first working version |
| Password reset/change/invitation flows | Complete local administration if identity remains owned by this application |

The edition distinction and capabilities above are grounded in the [Unleash comparison](https://docs.getunleash.io/support/oss-comparison); migration behavior is also described in [Unleash migration guidance](https://docs.getunleash.io/guides/feature-flag-migration-best-practices).

## Recommended implementation order

1. **Correct the foundation:** role/project authorization and trusted actor identities; feature/environment initialization; EF ownership/navigation mappings; collection loading; validation/Persian errors; safe administrator bootstrap. Add regression checks for these paths.
2. **Deliver one complete consuming flow:** choose protocol compatibility; implement the smallest evaluation core, scoped application credentials, backend configuration delivery and frontend evaluated results. Prove a backend app and a browser app can use the same flag safely.
3. **Make consumption reliable:** supported SDK/client integration, deterministic targeting, segments and variants, bounded refresh/last-known-good/default behavior, configuration propagation and basic usage metrics.
4. **Complete governance when required:** actual atomic change application, approval enforcement, scheduler, durable history/restore. Until then, do not present the current status workflow as production approval protection.
5. **Expand deliberately:** administration UI coverage, playground, integrations/import-export, and only the advanced release/scale capabilities your applications require.

The existing four-layer application can support the first working release. These findings do not justify a collection of new microservices or an interface/class for every strategy. Keep handlers focused and share only behavior that must remain consistent. Before creating any service, search this repository and both indexed upstream owners, check package compatibility and coverage, and reuse suitable capabilities as required by [AGENTS.md](../AGENTS.md) and [UPSTREAM-DEPENDENCIES.md](UPSTREAM-DEPENDENCIES.md).

The next specification number is `02` at this inspection baseline. Select one bounded milestone rather than turning this entire backlog into one large implementation task.

## Manual index maintenance

No index operation was performed. Coverage reported changed metadata for the main project and inspected BusinessMaker files. The user may manually re-index `feature-flag` after accepting this report and refresh the BusinessMaker index when convenient; do not trigger either automatically.
