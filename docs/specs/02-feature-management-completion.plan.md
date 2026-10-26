# Feature management completion implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for native implementation or superpowers:subagent-driven-development if the user selects delegation. Track the steps below; do not commit without separate authorization.

**Goal:** Complete all four milestones and defect fixes in approved spec 02.

**Architecture:** Extend the existing Domain/Application/Infrastructure/WebApi solution and custom BusinessMaker mediator. Keep requests and handlers together; share one evaluator and one mutation path between direct writes and reviewed changes. PostgreSQL remains the source of truth; immutable configuration snapshots use the compatible BusinessMaker cache adapter.

**Tech stack:** .NET 10, EF Core/Npgsql, existing BusinessFramework 0.3.22-preview and EdgeServicesBuildingBlock 0.3.4-preview references, xUnit, ASP.NET integration hosting, PostgreSQL, TypeScript contracts.

**Spec:** [02-feature-management-completion.specs.md](02-feature-management-completion.specs.md), approved 2026-10-26. Status: plan prepared for review; no product implementation yet.

## Global constraints

- No SDK packages, SDK feeds, application credentials/API-key authentication infrastructure, administration UI, or Unleash compatibility.
- JWT-protected evaluation; browsers call their own backend. Authorization uses current account state and scoped membership on every request.
- All authored error text is Persian. Preserve stable identifiers and documented successful response shapes.
- Search existing local and both indexed upstream repositories before introducing any service; verify coverage and installed APIs, record why reuse does or does not fit.
- Never index or refresh repositories. Preserve the user's `nuget.config` and central package edits. No upstream changes, shared database migration/seeding, commits or pushes.
- Keep endpoint TypeScript contracts synchronized in `docs/ts-contracts`; specifications/plans stay in `docs/specs`.
- Refresh 5 seconds, maximum configuration staleness 60 seconds, maximum 1,000 snapshot keys; current authorization fails closed on database outage.
- Evaluations: 100 explicit names or at most 1,000 active flags, 64 KiB body, appName 1–128, 50 context properties, 1,024 characters per value.
- Variants: unique ordinal names, integer weights totaling 1,000, JSON payload at most 16 KiB. Prerequisite/stage chains at most 20.
- Metrics: impressions batches at most 100, idempotency 7 days, hourly retention 90 days, query window 31 days, paging 50/default and 200/max.
- Approvals: 1–5; protected environments require non-author approval. Scheduled polling 5 seconds and at most 5 transient retries.
- Tests must exercise real PostgreSQL for migrations, mapping, locks, transactions and concurrency. Missing infrastructure is a reported blocker, never a passing test.

## Review focus

1. Concurrent flag/environment creation can race: task 3 must prove one association per pair after both transactions complete or a clean retry.
2. Revocation during a warm cache must deny access: tasks 2 and 7 exercise current identity and membership independently from snapshot freshness.
3. Malformed legacy ownership must stop upgrade without data loss: task 3 tests upgrade rollback and actionable Persian diagnostics.
4. Shared segments can acquire new protected references after review: task 9 invalidates the captured affected set rather than broadening authorization.
5. A crash between scheduled stages must not duplicate writes or erase approval conflicts: task 10 tests restart, two workers and predecessor revision propagation.

## Source and file map

Existing handlers below remain in their current `<Area>/Commands|Queries/<UseCase>/<UseCase>Command|Query.cs` paths. Controller changes stay in `src/WebApi/Controllers`; EF changes stay in `src/Infrastructure/Configurations` and generated migrations. Add only the following cohesive production units when their task requires them:

| Unit | Responsibility |
| --- | --- |
| `src/WebApi/Authorization/ProjectAccess.cs` | ASP.NET authorization requirements/handler and resource resolution; no service per resource |
| `src/WebApi/Handlers/ApiProblemHandler.cs` | Persian HTTP errors, model binding and conflict mapping |
| `src/Domain/Evaluation/FlagEvaluator.cs` | Pure constraints, strategy, variants, stable hashing and prerequisite evaluation |
| `src/Application/Features/Evaluations/EvaluateFlags.cs` | Request/response types and handler; preview shares evaluation path |
| `src/Infrastructure/Services/ConfigurationSnapshots.cs` | Bounded immutable snapshot loading/caching through existing cache capability |
| `src/Application/Features/Metrics/FlagMetrics.cs` | Usage query/report requests, handlers and aggregate persistence orchestration |
| `src/Domain/Aggregates/UsageAggregate/FlagUsage.cs` | Hourly counters, application observation and impression-batch deduplication entities |
| `src/Domain/Aggregates/HistoryAggregate/ConfigurationHistory.cs` | Safe append-only configuration history entity |
| `src/Application/Features/ChangeRequests/ConfigurationChanges.cs` | Versioned action payloads, validation, shared mutation/application and safe snapshots |
| `src/Infrastructure/Services/ScheduledChangeExecutor.cs` | Durable due-request claims/recovery, bounded retry and retention cleanup |
| `src/Application/Features/History/ConfigurationHistory.cs` | Scoped history queries and validated restore request/handler |
| `tests/FeatureFlag.Tests/FeatureFlag.Tests.csproj` | One test project; unit and real PostgreSQL HTTP/persistence tests |
| `examples/BackendConsumer/` and `examples/browser-consumer.ts` | Runnable backend caller/BFF and small browser example, no SDK/UI |

New services in this table are conditional on the mandatory reuse checks; do not create placeholder interfaces or empty implementations. Domain-only types can share a cohesive file. Configuration models/revisions must be introduced in task 3 before downstream tasks consume them.

## Task 1 — Establish executable regression checks

**Files:** create `tests/FeatureFlag.Tests/{FeatureFlag.Tests.csproj,TestApplication.cs,PostgresFixture.cs,FoundationTests.cs}`; modify `feature-flag.sln` and test-specific configuration only. Read existing startup/seeder before launching anything.

**Interfaces:** `PostgresFixture` supplies an isolated database and connection string; `TestApplication` hosts WebApi against it with bootstrap disabled, controlled clock and test identity setup. Integration mode explicitly fails when requested PostgreSQL is unavailable; unit tests remain separately selectable.

- [ ] Add xUnit, test SDK and ASP.NET testing packages compatible with net10 through central management without rewriting existing versions. Use a disposable PostgreSQL container if available; otherwise an explicitly supplied test-only server with generated database names and verified cleanup targets.
- [ ] Add `CreateFlagInitializesAllEnvironments`, `CreateEnvironmentInitializesExistingFlags`, `InactiveAccountCannotRefresh` and `StrategyConstraintRoundTrips` regression tests. Assert disabled associations, persisted constraints and rejected refresh; demonstrate they fail against the baseline.
- [ ] Run `dotnet restore feature-flag.sln`, then `dotnet test tests/FeatureFlag.Tests --filter FoundationTests`. Record dependency/infrastructure failures separately from expected behavioral failures.

## Task 2 — Enforce current identity, authorization and Persian failures

**Files:** modify `src/WebApi/Configuration/JwtConfiguration.cs`, all business controllers, `src/Infrastructure/Services/JwtTokenGenerator.cs`, User/RefreshToken domain and configuration files, Auth/User commands, `src/Application/Features/Projects/Commands/Create/CreateProjectCommand.cs`; add the authorization/problem units above and `tests/FeatureFlag.Tests/AuthorizationTests.cs`; update Auth/Users/Projects/Common TS contracts.

**Interfaces:** `ProjectAccess` resolves project ownership from the actual resource ID and checks read/write/owner permissions from spec 02. JWT validation checks persisted active/security-version state. Handlers obtain acting identity from the compatible current-user abstraction; body creator/reviewer fields cannot override it.

- [ ] Write failing `ViewerCannotPromote`, `NonmemberCannotReadOrMutateAnyResource`, `ListWithoutProjectDoesNotLeak`, `CreatorBecomesOwner`, `OnlyRootCanChangeRoot`, `LastActiveRootCannotBeRemoved`, `BodyIdentityCannotSpoofActor`, `InactiveRefreshRejected`, `RefreshRotatesOnceConcurrently` and `RevokedJwtRejected` tests.
- [ ] Implement the approved permission matrix on list/detail/mutation routes, including legacy tokens, shared segments and change requests. Normalize `sub`/NameIdentifier, persist session security version, revoke sessions on role/password/deactivation/logout and atomically consume refresh tokens. Preserve at least one active root transactionally.
- [ ] Map validation/auth/missing/conflict/unavailable outcomes to Persian 400/401/403/404/409/503 problems without leaking exception details. Test automatic malformed JSON, enum, missing-field and auth challenge responses.
- [ ] Run authorization/session HTTP tests with real PostgreSQL, then compile changed contracts; permission failures must never become evaluation defaults.

## Task 3 — Repair initialization, mappings and existing mutations

**Files:** existing CreateFeature/CreateEnvironment/UpdateEnvironment handlers; removal handlers for project members, tokens, strategies and segment constraints; AddFeatureStrategy handler; Feature/FeatureEnvironment/FeatureStrategy/Project/Environment/Segment aggregates; Constraint/Variant value objects; related EF configurations, `IApplicationDbContext.cs`, `ApplicationDbContext.cs` and migrations; `tests/FeatureFlag.Tests/{FoundationTests,PersistenceTests}.cs`; affected existing TS contracts.

**Interfaces:** configuration roots expose optimistic `long Revision`; changed endpoint writes carry expected revisions. Constraint has nullable segment/strategy owners with exactly-one check; Variant has nullable strategy/environment owners with exactly-one check. Segment has nullable ProjectId; null preserves shared legacy ownership.

- [ ] Add failing tests for creation in both orders, concurrent creation/feature-limit enforcement, fresh-context removals, environment rename, both rollout enum values, constraint/variant/payload mutation tracking and stale revision rejection.
- [ ] Use aggregate feature creation guards and initialize disabled associations for all environments; environment creation initializes all nonarchived flags. Load mutation collections explicitly. Fix Name persistence and GradualRollout parsing; reject unsupported Custom rather than defaulting it.
- [ ] Map real constraint/variant navigations, remove Ignore/duplicate ownership mappings, add JSON comparers and revisions. Keep current global name uniqueness. Remove unconditional sensitive logging and duplicate application/infrastructure registration.
- [ ] Generate reviewable migration(s): precheck invalid legacy owners, preserve valid rows, backfill disabled associations, add unique pairs and concurrency state. Verify fresh migration and upgrade from the current snapshot with populated legacy data; malformed data must roll back without deletion. Do not apply to shared databases.
- [ ] Run all foundation/persistence regressions and test concurrent writer outcomes. Update contracts for expected revisions and conflict handling.

## Task 4 — Complete targeting and variant configuration

**Files:** FeatureStrategy/StrategyParameters/Constraint/Variant, feature-environment commands/controller, segment commands/controller; add `UpsertFeatureStrategy` and `ReorderFeatureStrategies` request/handler files beside existing commands; `tests/FeatureFlag.Tests/ConfigurationValidationTests.cs`; FeatureEnvironments/Segments/Features TS contracts.

**Interfaces:** `UpsertFeatureStrategyCommand` carries feature/environment/strategy IDs, expected revision, strategy type/parameters, constraints, segment IDs, enabled status, order and variants. The typed payload is reused in task 9; separate services per strategy are forbidden.

- [ ] Add failing tests for all strategy payloads/operator cardinalities, missing/foreign segments, duplicate order entries, invalid variant names/weights/payload size, mutation after enumeration and unknown numeric enum values.
- [ ] Implement cohesive create/update/delete/reorder and variant replacement endpoints; validate IP/CIDR, numeric/date/SemVer values invariantly. Add SemverGte/Lte enum values without changing existing numeric values. Validate segment reference scope and reject unknown/custom strategies explicitly.
- [ ] Preserve legacy environment variants as fallback; track payload changes. Add protected-write hooks consumed by task 9 without duplicating mutation logic.
- [ ] Run configuration validation and round-trip tests and compile exact endpoint contracts.

## Task 5 — Implement deterministic evaluation

**Files:** new `src/Domain/Evaluation/FlagEvaluator.cs`, prerequisite state on Feature, `tests/FeatureFlag.Tests/{EvaluationTests,HashFixtureTests}.cs` and `tests/fixtures/assignment-v1.json`.

**Interfaces:** `FlagEvaluator.Evaluate(snapshot, context, now)` returns enabled/reason/selected variant and optional preview explanation; immutable typed input/output records live alongside the evaluator. Snapshot includes ordered strategies, segments, variants and prerequisite revisions, without EF-dependent behavior.

- [ ] Write parameterized failures covering every strategy and operator, inversion with missing/malformed context, ordinal/case-insensitive strings, IPv4/IPv6/CIDR boundaries, UTC/time windows and valid/prerelease/invalid SemVer.
- [ ] Pin length-prefixed UTF-8 SHA-256 tuple encoding with fixed cross-language fixture inputs/expected buckets. Test rollout 0/100, default/explicit/random stickiness, project/group correlation, variant boundaries totaling 1,000, payload and disabled fallback.
- [ ] Implement ordered OR strategies with AND constraints/segments, first-match variants, fail-closed malformed/missing references and current server time. No active strategy is false.
- [ ] Implement same-project prerequisite management/cycle rejection and depth limit 20; evaluation requires all prerequisites and requested variants. Test missing/archived parent, diamond dependencies and depth overflow.
- [ ] Run unit tests without database/network; later tasks consume these exact immutable inputs and outputs.

## Task 6 — Deliver authenticated evaluation and consumer journey

**Files:** new `src/Application/Features/Evaluations/EvaluateFlags.cs`, `src/WebApi/Controllers/EvaluationsController.cs`; examples above; `tests/FeatureFlag.Tests/EvaluationJourneyTests.cs`; new Evaluations.ts and example instructions.

**Interfaces:** `POST /api/v1/Evaluations` and `/preview` accept the spec's project/environment/appName/context/names/defaults shape; normal results omit rules. Preview requires project-write permission and permits simulation time. Response metadata carries revision/evaluatedAt/snapshotLoadedAt/stale/refresh/max-stale.

- [ ] Write failing HTTP tests for complete configure→enable→evaluate journey with two distinct contexts, false/missing defaults, disabled/unknown environment distinctions, unauthorized/foreign JWT, bounds and preview without metrics.
- [ ] Add mediator-backed endpoints calling task 5. Enforce request/property/name limits and project authorization before loading configuration; protect preview explanations.
- [ ] Add runnable .NET backend/BFF and browser call examples using only configured accepted JWTs. Forward allowed context, emit no-store and avoid returning targeting rules. Test the BFF/browser response shape in the journey test.
- [ ] Run journey tests; document exact launch/configuration steps, JWT issuer/audience requirements and safe default behavior. Compile examples and TypeScript contracts.

## Task 7 — Add bounded snapshot caching and reliable consumption

**Files:** new ConfigurationSnapshots unit, evaluation handler/DI configuration, backend example, `tests/FeatureFlag.Tests/SnapshotCacheTests.cs`; Evaluations.ts and DEVELOPMENT.md.

**Interfaces:** snapshot loader keys immutable configuration by project/environment, uses compatible `ICacheAdapter`, deduplicates concurrent loads and accepts cancellation/controlled time. Post-commit invalidation operates on affected keys; authorization is never cached with the snapshot.

- [ ] Recheck local and both owner graphs/source, installed cache API/lifetime and registrations before adding this service. Record queries, coverage and reuse findings.
- [ ] Write failures for single concurrent load, 1,000-key eviction, cross-project isolation, refresh at 5 seconds, fallback through 60 seconds, zero-stale configuration, cancellation, outage/no snapshot, recovery and warm-cache account/membership revocation.
- [ ] Implement refresh/load deduplication and permitted last-known-good configuration. Invalidate only after commit; retain cross-instance 5-second recheck. If current authorization cannot be established, fail closed even with a snapshot.
- [ ] Add example 2-second timeout, bounded jitter retry and principal/context/default-sensitive cache keys. Bound retention by JWT expiry/configuration staleness; auth failures clear cached results, time constraints reevaluate against current time.
- [ ] Run controlled-time tests and outage HTTP tests; verify public responses accurately mark staleness.

## Task 8 — Record and expose usage

**Files:** FlagUsage domain unit, FlagMetrics application unit, `src/WebApi/Controllers/MetricsController.cs`, context/configuration/migrations, evaluator completion hook; `tests/FeatureFlag.Tests/MetricsTests.cs`; new Metrics.ts.

**Interfaces:** normal evaluations record hourly boolean/variant counters and app last-seen; preview records none. `POST /api/v1/Metrics/impressions` accepts UUID batch and bounded nonnegative counts; GET metrics/applications applies project permissions and paging.

- [ ] Write failures for actual LastSeenAt rather than enable time, no preview metrics, hourly aggregation under concurrency, duplicate batch exactly-once, limits/date windows/paging, project isolation and telemetry failure preserving evaluation outcome.
- [ ] Persist aggregates/observations/deduplication without raw context/IP/user IDs. Enforce 100/batch, 7-day deduplication and 90-day aggregates; wire bounded cleanup into task 10's executor.
- [ ] Implement scoped queries with 31-day window and 50/default, 200/max paging; expose telemetry failure through structured logging/health without sensitive payloads.
- [ ] Verify real PostgreSQL upserts/concurrency/idempotency and TypeScript contracts.

## Task 9 — Apply reviewed changes atomically and record history

**Files:** existing ChangeRequest/Item domain/configuration/handlers/controller, ConfigurationChanges unit, ConfigurationHistory domain/application/controller units, project/environment protection settings, mutations from tasks 3–4; `tests/FeatureFlag.Tests/{ChangeRequestTests,HistoryTests}.cs`; ChangeRequests/Projects/Environments/History TS contracts.

**Interfaces:** a versioned discriminated action vocabulary shares task 4 payloads and domain methods; `ConfigurationChanges.ApplyAsync(requestId, actorId, cancellationToken)` validates/rechecks all targets and writes mutations/history/status in one transaction. Safe snapshots exclude credentials and evaluation context; restore creates a new reviewed mutation/revision.

- [ ] Write failing tests for unknown/malformed/cross-project actions, spoof/self approval, 1–5 approvals, edit invalidation, immutable applied/cancelled requests, rejection reason, stale revisions, partial failure rollback and idempotent apply.
- [ ] Implement project and Global scopes, approved actions and captured expected revisions. Global scope permits only declared shared-segment/global-protection actions, independent active RootAdmin approval and validated affected-resource sets.
- [ ] Add protected-write enforcement to every equivalent flag/strategy/variant/segment path. Policy weakening follows current/captured stricter gate; ordinary updates cannot disable global protection. Test new shared-segment references after review and approval-setting self-bypass attempts.
- [ ] Persist append-only actor/UTC/resource/revision/safe-before-after history in the same transaction for direct and reviewed configuration/protection/governance changes. Add scoped paged reads, preview and validated restore; verify no secrets and no edits to old history.
- [ ] Run transaction/permissions/history HTTP tests using real PostgreSQL; update every changed contract.

## Task 10 — Execute durable schedules and rollout stages

**Files:** new ScheduledChangeExecutor, schedule/query/domain state and EF migration, DI; `tests/FeatureFlag.Tests/SchedulingTests.cs`; ChangeRequests.ts.

**Interfaces:** executor calls the same `ConfigurationChanges.ApplyAsync` as manual application after a transactional due-work claim. Persist attempts, next attempt, Persian failure/suspension reason, scheduling actor, predecessor ID and resulting target revisions.

- [ ] Recheck local/upstream indexed scheduling APIs and compatible owner source; record why RabbitMQ delayed scheduling cannot satisfy the current broker-free durable database path before creating the executor.
- [ ] Write failures for future UTC validation, due work after restart, two-worker contention, crash recovery, revoked scheduler permission, deleted/revised targets, transient retry limit 5, graceful cancellation and visible suspension/failure.
- [ ] Implement PostgreSQL locking/lease claims, 5-second configurable polling, exponential persisted retry only for transient failures and shared atomic application. Never leave status Applied before actual target changes commit.
- [ ] Implement predecessor chains: same project/environment, acyclic maximum 20, strictly increasing times, first baseline revisions, successors consume predecessor output revisions. Test expected predecessor writes succeed, unrelated writes conflict, predecessor changes suspend successors and invalidate approvals.
- [ ] Add bounded 7-day impression-ID and 90-day aggregate cleanup. Run multi-context/worker restart and transaction tests against PostgreSQL.

## Task 11 — Complete deployment, localization and documentation

**Files:** Dockerfile, `.github/workflows/cicd.yml`, `src/WebApi/ProgramConfiguration/{ServiceCollections,ApplicationBuilder}.cs`, bootstrap/seeder and configuration, diagnostics controllers, docs root/.ai, all changed TS contracts; `tests/FeatureFlag.Tests/DeliveryTests.cs`.

**Interfaces:** production bootstrap is opt-in with externally supplied secrets, default disabled. Readiness checks the configured database; logging/CORS have explicit safe configuration. Examples and CI never seed shared systems or apply migrations automatically.

- [ ] Write failing checks for disabled default bootstrap, missing required configuration, production diagnostic exposure, Persian automatic failures, health/readiness and restricted CORS.
- [ ] Update Docker to net10 with actual central props/project layout; add restore/Release build/unit/PostgreSQL integration/TypeScript checks to CI. Remove production demo endpoints or restrict them to explicit development setup; update Diagnostics.ts.
- [ ] Review recorded vulnerable transitive package paths with owner/version evidence; fix within scope or document exact unresolved advisories. Preserve unrelated dependency/feed edits.
- [ ] Document fresh/upgrade migration procedure and invalid-data prechecks, examples, permissions, cache stale tradeoff, metrics semantics, history/approvals/schedules and operational failure recovery. Update .ai/context only with behavior actually verified.

## Task 12 — Verify the complete result and report remaining blockers

**Files:** plan checkbox/status updates and relevant documentation corrections only, plus any fixes justified by failures.

- [ ] Run `dotnet restore feature-flag.sln`, `dotnet build feature-flag.sln -c Release --no-restore` and `dotnet test feature-flag.sln -c Release --no-build` with real PostgreSQL integration configuration. Report actual counts, warnings and failures.
- [ ] Compile all TS contracts strictly with ES2020/ESNext and `verbatimModuleSyntax`; compile/run examples and verify fixture parity. Build the Docker image when a runtime is available; otherwise name the blocker.
- [ ] Recheck fresh and populated upgrade migrations, endpoint/contract coverage, Persian error paths, documentation links and `git diff --check`. Review the whole diff for secrets, missing authorization paths, service proliferation and accidental upstream/index artifacts.
- [ ] Link delivered behavior to every spec milestone/defect. Keep unfinished boxes open; no compile-only completion claim. Report user files preserved, environmental blockers and manual re-index reminder for feature-flag only unless an upstream actually changed.

## Plan evidence and execution handoff

Read-only main graph queries on 2026-10-26 located the create/refresh/apply handlers and configuration aggregates; inbound `InitializeEnvironment` trace had zero recorded callers. Generation reported `2026-10-26T08:50:37Z`. Coverage for eight relied-on code paths reported changed metadata; all eight files were read live. Those reads confirm initialization omission, inactive-refresh omission, ignored strategy navigations from prior inspection, and the apply-items TODO. Graph absence is not exhaustive proof. No indexing operation was invoked.

Upstream reuse evidence is recorded in the approved spec; concrete service tasks 2/7/9/10 must revalidate relevant owner APIs before implementation. No production service has been created by writing this plan.

Self-review: tasks 1–3 cover milestone 1, tasks 4–6 cover milestone 2, tasks 5/7/8 cover milestone 3, tasks 9–10 cover milestone 4; tasks 11–12 cover cross-cutting delivery. All five review-focus inputs have named test owners. External numeric/permission rules come from spec 02; the plan does not narrow them.

Recommended execution: native in this session, because identity, revisions, shared mutations and transaction boundaries are tightly coupled. Preserve the selected execution method once the user chooses it. No commits or shared-database changes are part of this authorization.
