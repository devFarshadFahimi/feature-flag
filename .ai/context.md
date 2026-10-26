# Implementation context

## Baseline: 2026-10-26

`feature-flag.sln` contains `src/Domain`, `src/Application`, `src/Infrastructure`, and `src/WebApi`, targeting `net10.0` through `src/Directory.build.props`. Application uses BusinessMaker custom commands/queries and generated handlers, EF Core through `IApplicationDbContext`, and Mapster DTO projection. Infrastructure uses Npgsql/PostgreSQL, BCrypt, local JWT/refresh-token implementations, auditing interception, and EF-backed data-protection keys.

Implemented API areas: Auth, Users, Projects, Features, Environments, FeatureEnvironments, Segments, ApiTokens, and ChangeRequests. `TestController` plus demo controllers in `ApiControllerBase.cs` are also discoverable HTTP surfaces; see [Diagnostics.ts](../docs/ts-contracts/Diagnostics.ts). Most business controllers use `[Authorize]`; this alone does not enforce administrator role or project membership.

## Source starting points

| Concern | Source |
| --- | --- |
| Bootstrap / middleware | `src/WebApi/Program.cs`, `ProgramConfiguration/ServiceCollections.cs`, `ProgramConfiguration/ApplicationBuilder.cs` |
| JWT validation / role policies | `src/WebApi/Configuration/JwtConfiguration.cs` |
| Request shapes / handlers | `src/Application/Features/<Area>/Commands|Queries/<UseCase>/*.cs` |
| Mediator / mapping registration | `src/Application/DependencyInjections.cs`, `Common/Mappers/MappingInitializer.cs` |
| Database interface / implementation | `src/Application/Common/Services/IApplicationDbContext.cs`, `src/Infrastructure/ApplicationDbContext.cs` |
| EF model / migrations | `src/Infrastructure/Configurations/`, `src/Infrastructure/Migrations/` |
| Invariants and events | `src/Domain/Aggregates/`, `src/Domain/ValueObjects/` |

## Known limitations — observations, not authorized fixes

- `ApplyChangeRequestCommandHandler` marks a request Applied but has a TODO for applying its items. Scheduling persists a date/status; no scheduler implementation is present in `src`.
- `UpdateEnvironmentCommand` accepts `Name` but only updates `SortOrder`.
- Token creation stores a GUID-derived value as `TokenHash` and returns only the token ID. There is no usable-secret delivery contract or SDK API-key middleware in the inspected source.
- Feature-environment operations require an existing association. `Feature.InitializeEnvironment` exists, but the current create-feature handler does not call it and no initialization endpoint exists.
- `CreateFeatureCommandHandler` calls `Feature.Create` directly, bypassing `Project.AddFeature` feature-limit and duplicate checks. EF configuration also has a global unique feature-name index as well as a per-project filtered index. Do not describe uniqueness as only per-project.
- Several collection mutations load aggregates through `FindAsync` without loading the relevant collection (members, strategies, tokens, constraints). Verify persistence and relationship loading before copying them.
- `FeatureMappingConfig` emits `customParameters`, but the standalone feature-environment query omits that key. `ProjectResponse.CreatedAt` is declared although the local Project class does not explicitly declare it; verify framework audit mapping before relying on its value.
- Two different `UserResponse` records exist. Auth login/refresh/me use four fields; administrative Users queries declare seven fields. `UserMappingConfig` imports the Auth response, so verify administrative projection behavior rather than assuming that config covers both.
- Application and Infrastructure registration is called twice in `ServiceCollections.AddServices`. Inspect lifetimes/duplicate registrations when changing DI.
- Existing messages and upstream exception fallbacks include English text; Persian-only error policy is not yet fully implemented. Automatic model-binding/auth errors need runtime verification/localization too.
- `ApplicationDbContext` enables sensitive-data logging unconditionally. Startup seeds an admin from `DatabaseSeeder`; use disposable data and do not copy seed credentials into docs.
- Root Dockerfile uses .NET 7 images and references a missing `src/Framework/Framework.csproj`; it does not match the current .NET 10 solution.
- The old root README was a proposed SDK/evaluation implementation, not current source. It is preserved at [01-sdk-evaluation-proposal.specs.md](../docs/specs/01-sdk-evaluation-proposal.specs.md), explicitly marked unimplemented and unapproved.
- No test project or active CI workflow file was found in the inspected repository file inventory. Builds and runtime behavior were not established by this documentation inspection.

Graph coverage reported metadata changes on cited C# files and untracked central props; live source was used to verify these observations. `bin`/`obj` are excluded from the code graph; package asset files were read directly. See [dependency evidence](../docs/UPSTREAM-DEPENDENCIES.md).
