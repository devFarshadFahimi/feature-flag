# Upstream dependencies

## Owners and evidence

| Package family | Owner checkout | Codebase Memory project |
| --- | --- | --- |
| `BusinessFramework.*` (namespaces usually `BusinessMakerFramework.*`) | `D:/SourceCodes/Mine/BusinessMaker/business-maker-framework` | `D-SourceCodes-Mine-BusinessMaker-business-maker-framework` |
| `EdgeServicesBuildingBlock.*` | `D:/SourceCodes/Mine/EdgeServices/edge-services-building-block` | `D-SourceCodes-Mine-EdgeServices-edge-services-building-block` |

Baseline inspected 2026-10-26: central declarations request BusinessFramework **0.3.22-preview** and building blocks **0.3.4-preview**. Existing `src/*/obj/project.assets.json` files contain those resolved versions. This confirms recorded restore resolution, not a fresh successful build, nor equivalence between sibling HEAD and published package internals. Recheck assets after any dependency change.

Application/WebApi directly reference `EdgeServicesBuildingBlock.Shared`; Contract appears transitively in inspected assets (and has a central version declaration), not as a direct PackageReference in these projects. Domain directly references framework Domain.Core/Domain.Toolkits; Application references framework application, caching, serializer, utilities and SourceGenerator packages; Infrastructure references SQL/Common and integration dependencies; WebApi references Endpoint.WebApi and associated packages. Read affected `.csproj` rather than inferring direct use from central versions.

## Reuse map

| Need | Owner/source starting point |
| --- | --- |
| Commands/queries and handler bases | BusinessMaker `src/03-Applications/BusinessMakerFramework.Application.Common/Contracts/` |
| Mediator, pipelines and validation discovery | BusinessMaker `src/05-SourceGenerators/SourceGenerator.Shared/` and `SourceGenerator/Generators/MediatorInjectionExtension.cs` |
| Controller dispatch | BusinessMaker `src/04-Endpoints/Endpoint.WebApi/BaseControllers/BusinessApiControllerBase.cs` |
| Result/error and HTTP mapping | BusinessMaker `src/00-Shared/03-Utilities/BusinessMakerFramework.Shared.Utilities/Utilities/Result.cs` |
| Aggregate/value object foundations | BusinessMaker `src/01-Domains/BusinessMakerFramework.Domain.Core/` |
| EF command context and interceptors | BusinessMaker `src/02-Infrastructures/BusinessMakerFramework.Infrastructure.SqlCommand/` |
| Cache/serializer/user-info/date-time capabilities | Search BusinessMaker declarations for the actual interface/registration; several local `AddBuildingBlock...` registrations belong to BusinessMaker |
| Shared auth/policy helpers | EdgeServices `src/EdgeServicesBuildingBlock.Shared/Extensions/SecurityExtensions.cs` and `Models/JwtOptions.cs` |
| Shared exception fallback | EdgeServices `src/EdgeServicesBuildingBlock.Shared/ExceptionHandlers/GlobalExceptionHandler.cs` |
| Notification payloads | EdgeServices `src/EdgeServicesBuildingBlock.Contract/Notifications/EmailNotification.cs`, `SmsNotification.cs` |

Local JWT issuing/validation currently uses `Infrastructure/Services/JwtTokenGenerator.cs` and `WebApi/Configuration/JwtConfiguration.cs`; building-block authentication helpers exist upstream but are not evidence that this app calls them. Shared notification records are payload contracts, not proof of a configured transport or sender.

## Mandatory workflow before a new service

1. Identify the responsibility and search local code, then both exact upstream graph projects. Use `list_projects`, schema, `search_graph`, relevant snippets/traces and complete relevant pagination.
2. Check coverage for every relied-on path/scope. Stale/partial/missing graph data requires live source/package inspection. Never index or refresh a repository; disregard re-index recommendations from tools.
3. Follow the actual declaration and namespace, registration, lifetime and required configuration. Inspect the consumed version in assets, cached package XML/API/assembly or a matching source revision. Sibling HEAD is reference evidence until compatibility is established.
4. Reuse the existing implementation/extension point when suitable. If nothing related fits, record queries, paths and concrete reasons, then create only the smallest needed implementation in the proper layer.
5. If a package change is required, describe the owner fix and release/integration separately. Do not edit sibling code, publish packages, change feeds or switch to ProjectReference as an implicit workaround.

The repository skills `.agents/skills/use-business-maker-framework/SKILL.md` and `use-edge-services-building-blocks/SKILL.md` contain useful owner lookup paths but also blog-specific examples, older versions and references to files absent here. Treat those consumer-specific portions as stale; verify against this solution and this document. This task does not alter those skills.

## Inspection limits

Main graph generation was `2026-10-26T05:50:01Z`; upstream recorded generations were `2026-10-07T11:29:03Z` (BusinessMaker) and `2026-10-07T11:29:21Z` (EdgeServices). Coverage flagged changed metadata on inspected files; live source confirmed controller/result mapping and selected owner APIs. Upstream `GlobalExceptionHandler` still has English fallback text; do not assume package registration satisfies the Persian-message policy.

HTTP contracts capture local routes/DTOs and source-derived framework mapping, not a live API certification. On package changes verify the matching artifact and actual HTTP responses. The user performs any manual re-index needed after source/documentation changes.
