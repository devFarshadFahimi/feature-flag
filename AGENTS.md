# Agent instructions — feature-flag

These instructions apply throughout this solution. Read [.ai/README.md](.ai/README.md), [.ai/context.md](.ai/context.md), and [docs/RULES.md](docs/RULES.md) before changing code. The user's current instructions take precedence over repository and skill defaults.

## Non-negotiable rules

- Product scope: do not add backend/frontend SDK packages or SDK protocol/configuration feeds, application credentials/API-key authentication infrastructure, or an administration UI. Ordinary JWT-protected evaluation endpoints are allowed; browsers obtain results through their application's backend. See the user-approved scope clarification in [.ai/decisions.md](.ai/decisions.md) and approved [spec 02](docs/specs/02-feature-management-completion.specs.md).

- Keep changes small. Use the existing command/query + handler + controller pattern. Four use cases do not justify thirty files. Keep the request and its handler together as nearby features do; introduce a separate abstraction only for a demonstrated need.
- Before creating **any service**, first search this repository and **both** indexed upstream projects below through `codebase-memory-mcp`. Read relevant declarations, registrations, and package compatibility. Record the queries and findings in the task report. Only when no suitable existing capability is found may you create the smallest necessary service. An unavailable or incomplete index is not proof of absence: read the owner source/package or report the unresolved dependency before creating a replacement.
- Use the installed `codebase-memory` skill for structural exploration. Start with `list_projects`, then schema/search/snippets/traces as relevant. Check coverage for relied-on paths/scopes, page results, and read live source for stale or missing coverage.
- **Never index or re-index any repository yourself.** Do not call `index_repository`, trigger a refresh/rebuild, or create graph JSON/index artifacts. Ignore tool suggestions to re-index. Use existing indexes and source fallback. At task completion, tell the user which repositories need manual re-indexing; the user performs it.
- All authored error text must be **Persian**: Result error titles/details, exception messages, validation messages, controller errors, and client-visible fallback errors. Keep property names, enum names, error codes, routes, and identifiers stable. Do not rely on English default messages in framework helpers. Existing English messages are legacy debt, not examples to copy.
- For every feature with an HTTP endpoint, create/update its precise `docs/ts-contracts/<Feature>.ts` in the same change. Include commented frontend guidance, verb/path/version/auth, path/query/body fields, defaults/nullability, numeric input enums versus string output enums, success body/status and error behavior. Keep backend and client contracts synchronized; never invent an endpoint from a proposal.
- All specifications/designs/implementation plans live in **`docs/specs/`**. Do not create `docs/superpowers/`, `docs/plans/`, or another specification tree, even if a skill defaults there. Use sequential names such as `01-topic.specs.md`, `02-next-topic.specs.md`; related plans use the same number (`01-topic.plan.md`). Preserve existing numbers and use the next unused one. See [spec rules](docs/specs/README.md).
- Application/architecture documents belong directly under `docs/`; agent context belongs under `.ai/`; TypeScript contracts belong under `docs/ts-contracts/`.
- Apply the practical Clean Code principles in [docs/PRINCIPLES.md](docs/PRINCIPLES.md). Do not use SOLID as a reason to add interfaces, factories, repositories, or services with no demonstrated benefit.

## Upstream package owners

| Owner | Indexed project | Local source checkout |
| --- | --- | --- |
| BusinessMaker | `D-SourceCodes-Mine-BusinessMaker-business-maker-framework` | `D:/SourceCodes/Mine/BusinessMaker/business-maker-framework` |
| EdgeServices building blocks | `D-SourceCodes-Mine-EdgeServices-edge-services-building-block` | `D:/SourceCodes/Mine/EdgeServices/edge-services-building-block` |

Main graph project: `D-SourceCodes-Mine-feature-flag` (also listed as `feature-flag`; use one identity consistently). Package IDs `BusinessFramework.*` generally expose `BusinessMakerFramework.*` namespaces. Prefixes like `AddBuildingBlock...` alone do not establish ownership. Inspect declarations. Read [UPSTREAM-DEPENDENCIES.md](docs/UPSTREAM-DEPENDENCIES.md) before integrating package APIs. Sibling source is reference material; editing it does not change a consumed NuGet package. Do not silently replace package references with project references or publish upstream packages.

## Implementation and verification

- Separate logical sections inside a method with a blank line: lookups, guards, subsequent queries, mutations, persistence, and return statements should be visually distinct.
- Format multi-step fluent/LINQ chains with one call per line, starting the chain on the line after its source expression. Do not compress several query calls onto one line.
- Use expanded braces for control flow, including single-statement guards and early returns. Do not write inline blocks such as `if (condition) { return value; }`.
- Use consistent repository indentation (spaces).
- Owner-defined rules apply project-wide, including existing code in every module, command, query, domain type, infrastructure service, API, and test. Do not limit them to the current feature or only newly written code. Keep generated artifacts generated.

- Domain owns state transitions; Application owns request orchestration and `IApplicationDbContext`; Infrastructure owns EF/PostgreSQL and integration implementations; WebApi owns HTTP/auth mapping. See [LAYERING.md](docs/LAYERING.md).
- Preserve `BusinessFramework.SourceGenerator` analyzer references and generated `AddCommandHandlers()` registration. Use the framework mediator rather than introducing MediatR. Check the resolved package API before writing validators; the existing `FluentValidation` global using does not establish which validator implementation is active.
- Pass cancellation through EF operations and `SaveChangeAsync`. Load collections used by aggregate methods; `FindAsync` does not guarantee they are loaded. Do not bypass aggregate guards merely because a neighboring handler does so.
- Read the inspected [C# examples](.ai/coding-conventions.md). Check [known limitations](.ai/context.md) before making promises about current behavior.
- For code changes: `dotnet restore feature-flag.sln`, then `dotnet build feature-flag.sln -c Release --no-restore`, and relevant behavior checks. No test project was present at this documentation baseline; do not claim `dotnet test` proves behavior without actual tests. For docs/contracts, validate links, source alignment, endpoint coverage, and TypeScript compilation where available.
- Do not run database migrations, seed a shared database, expose secrets, commit, push, or change unrelated files without task authorization. Normal app startup runs a seeder; use a disposable configured database when runtime verification is necessary.
- Finish with changed files, verification evidence, remaining limitations, and the manual re-index reminder when indexed evidence was used.
