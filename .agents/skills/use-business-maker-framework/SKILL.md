---
name: use-business-maker-framework
description: Implement or diagnose consumer features using BusinessFramework packages and BusinessMakerFramework namespaces, including custom requests, mediator registration, validators, results, endpoints, and framework integrations.
---

# Use BusinessMaker framework

Reuse the framework's contracts and extension points when developing this service. This skill covers published-package consumption; owner changes and package releases require a task authorizing that work.

## Establish ownership and version

- Package IDs start with `BusinessFramework`; source namespaces commonly start with `BusinessMakerFramework`. Follow declarations to identify ownership. Extensions named `AddBuildingBlock...` can belong to BusinessMaker, including Consul and Microsoft serialization; the prefix alone is insufficient.
- Read [dependency guidance](../../../.ai/dependencies.md), the consuming `.csproj`, and [central versions](../../../src/Directory.Packages.props). The blog declared `BusinessFrameworkPackagesVersion=0.3.19` at inspection on **2026-10-11**.
- Establish the resolved version from `obj/project.assets.json` libraries/targets, then inspect the matching installed package, XML API documentation, or matching owner revision. Requested dependency versions do not prove resolution. At inspection, Application/WebApi assets had empty libraries; cached Application.Common packages were different preview versions. Owner source observations below are therefore references, **not a verified API guarantee for 0.3.19**.
- Owner checkout: `D:/SourceCodes/Mine/BusinessMaker/business-maker-framework`; graph project: `D-SourceCodes-Mine-BusinessMaker-business-maker-framework`. Read applicable owner `AGENTS.md` files when present. If the checkout is unavailable, locate its equivalent or inspect matching package artifacts.
- Use the repository [Codebase Memory skill](../codebase-memory/SKILL.md) for structural lookup: verify project/index status, search exact symbols with `file_pattern="*.cs"`, trace material relationships, read snippets, and check every relied-on path's coverage. Page relevant results and read live source for gaps or changed metadata. If MCP is unavailable, state that limitation and inspect source/package evidence directly.

Owner paths below are relative to that checkout. Read the entries relevant to the task.

| Task | Source starting point |
| --- | --- |
| Request and handler contracts | `src/03-Applications/BusinessMakerFramework.Application.Common/Contracts/CommandRequestHandler.cs`, `QueryRequestHandler.cs`, `Interfaces/ICommandRequest.cs`, `Interfaces/IQueryRequest.cs` |
| Registration discovery | `src/05-SourceGenerators/SourceGenerator/Generators/MediatorInjectionExtension.cs` |
| Validation rules | `src/05-SourceGenerators/SourceGenerator.Shared/FluentValidations/Abstractions/AbstractValidator.cs` and `Extensions/RuleBuilderExtensions.cs` |
| Mediator and lifetime resolution | `src/05-SourceGenerators/SourceGenerator.Shared/Contracts`, `MediatorEngine`, and `Extensions/DependencyInjections.cs` |
| HTTP mapping | `src/04-Endpoints/Endpoint.WebApi/MinimalApiAbstraction/BaseEndpoint.cs`; follow `ToHttpResult` to its declaration |
| Domain and result semantics | `src/01-Domains/BusinessMakerFramework.Domain.Core/Entities/AggregateRoot.cs` and `src/00-Shared/03-Utilities/BusinessMakerFramework.Shared.Utilities` |

## Commands, queries, and registration

Read the current [consumer patterns](../../../.ai/patterns.md) and nearby feature source before implementing a request.

- A status-only write uses `ICommandRequest` and `CommandRequestHandler<TCommand>` with `Task<Result>`. A typed command uses `ICommandRequest<T>` and `CommandRequestHandler<TCommand,T>` with `Task<Result<T>>`. Inspect the matching handler base and `Result` APIs before choosing success/error factories.
- A read uses `IQueryRequest<T>` and `QueryRequestHandler<TQuery,T>` with `Task<T>`. A null query response does not itself establish HTTP 404 behavior.
- Use `BusinessMakerFramework.SourceGenerator.Shared.Contracts.IMediator`. Keep requests, handlers, service interfaces, and validators in Application; HTTP mapping belongs in WebApi and storage implementations in Infrastructure.
- Preserve the `BusinessFramework.SourceGenerator` analyzer assets in [Application.csproj](../../../src/Application/Application.csproj) and `AddCommandHandlers()` in [Application registration](../../../src/Application/DependencyInjections.cs). Do not replace missing generated registrations with a new dispatch framework.
- The inspected generator discovers class declarations implementing custom `IRequestHandler` interfaces and semantically matches request/response types. It emits transient registrations for ordinary concrete handlers. Validator discovery scans the compilation for a matching `IRequestValidator<TRequest>` and returns the first matching class. Do not assume multiple validators or validators in another assembly are all discovered.
- If registration fails, inspect analyzer execution, generated output, exact interface/type matching, accessibility, and the compilation containing the handler/validator. Open generic handlers use separate closed-usage discovery; inspect that branch. Confirm generator details against the installed version.

## Use the custom validator API

Import `BusinessMakerFramework.SourceGenerator.Shared.FluentValidations.Abstractions` and `.Extensions`; derive from its `AbstractValidator<TRequest>`. Do not assume third-party FluentValidation overloads or chaining APIs apply.

The inspected owner source exposes `Must` with `Func<TProperty,bool>` and `MustAsync` with either `Func<TProperty,Task<bool>>` or `Func<TProperty,CancellationToken,Task<bool>>`. Both async overloads accept an optional `withMessage` string. Verify these signatures in the resolved package before using them; forward cancellation into new async database operations when supported.

[CreateTagCommandValidation.cs](../../../src/Application/Features/Tags/Commands/CreateTag/CreateTagCommandValidation.cs) demonstrates the custom namespaces and one-argument async predicate. Its synchronous query start, omitted cancellation, and normalization assumptions are not a recommended uniqueness algorithm. Establish stored normalization and concurrent-write behavior for duplicate checks; validation alone does not enforce atomic uniqueness.

## Results, HTTP responses, and lifetimes

- Preserve `BaseEndpoint`, local `MapAPI`/`MapPublicAPI` routing, authorization, version metadata, and response shapes. Existing creates use `CreatedSendAsync`, reads use `SendAsync`, and updates/deletes use `NoContentSendAsync`.
- Inspect `BaseEndpoint` and applicable `ToHttpResult` before promising status codes. The inspected raw-query helper evaluates `Results.Ok(result) ?? Results.NotFound()`; this does not test whether query data is null. Verify and cover missing-record HTTP behavior when it matters.
- Application requests singleton mediator and notification publisher lifetimes. Transient generated handlers do not make scoped dependencies safe by themselves. Inspect mediator/provider resolution and the complete dependency graph before introducing scoped dependencies, mutable shared state, or request-specific data. A lifetime change requires coordinated verification.
- Inject Application service interfaces. The blog's Mongo query helpers and `Infrastructure/Persistance` clients are local consumer code. Preserve collection naming, serialization, and cancellation. Use domain methods for guarded transitions and explicitly persist their resulting state.

## Verify the consumer change

Follow [workflow guidance](../../../.ai/workflow.md) and root `AGENTS.md`. For service changes run the required WebApi restore and Release build, then relevant behavioral tests. Compilation does not prove validator discovery, DI resolution, or HTTP status mapping: exercise those paths when changed. Report unavailable resolved-package evidence or environmental failures plainly.

If an API needs an owner change, explain owner fix, package release/version integration, and consumer verification separately. Editing sibling source does not change the installed package. Do not silently replace package references with project references, publish packages, or change feeds.

This repository skill and its supporting files are protected during unrelated tasks. Report outdated guidance without modifying it; update it only when the user explicitly requests repository skill maintenance.
