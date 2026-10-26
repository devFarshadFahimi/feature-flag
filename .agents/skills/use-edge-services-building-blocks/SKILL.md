---
name: use-edge-services-building-blocks
description: Use or diagnose EdgeServicesBuildingBlock packages in consuming services, especially shared authentication, authorization, and notification contracts. Verify package ownership and compatibility before changing integrations.
---

# Use EdgeServices building blocks

Reuse published building blocks through their actual extension points. This skill describes consumer integration; it does not authorize editing sibling repositories, publishing packages, changing feeds, or replacing package references with project references.

## Establish ownership and version

Read the consumer's `AGENTS.md` and dependency guidance. In this blog, start with `.ai/dependencies.md`, `src/Directory.Packages.props`, and the affected `.csproj`. Distinguish direct references from transitive dependencies.

The owner is `edge-services-building-block`, normally at `D:/SourceCodes/Mine/EdgeServices/edge-services-building-block`, with Codebase Memory project ID `D-SourceCodes-Mine-EdgeServices-edge-services-building-block`. Resolve another checkout location when necessary rather than treating that machine path as required. Read applicable owner instructions.

Use the repository's Codebase Memory skill for structural discovery: verify project/index status, search exact symbols in `*.cs`, trace relevant relationships, and read snippets. Check coverage for every evidence path; read live source for stale, partial, excluded, skipped, or untracked evidence. Consume relevant pages. If tools are unavailable, state that and use `rg` and direct source reads.

Compare resolved `obj/project.assets.json` entries with central versions. Empty `targets`/`libraries` do not establish resolution. Inspect the matching cached package's `.nuspec`, target framework, XML documentation, and assembly or matching source revision when behavior matters. A cached package proves artifact availability, not successful consumer restore or equivalence to the owner's checkout.

Names beginning `AddBuildingBlock...` or `AddFramework...` do not establish ownership. `AddBuildingBlockConsulService` belongs to BusinessMaker; use the BusinessMaker skill for that integration. Follow declarations and namespaces.

## Source entry points

Paths below are relative to the **owner repository**:

| Task | Source to inspect |
| --- | --- |
| Authentication, authorization, OpenIddict client setup, endpoint auth overloads | `src/EdgeServicesBuildingBlock.Shared/Extensions/SecurityExtensions.cs` |
| Authentication configuration fields | `src/EdgeServicesBuildingBlock.Shared/Models/JwtOptions.cs` |
| Shared email message | `src/EdgeServicesBuildingBlock.Contract/Notifications/EmailNotification.cs` |
| Shared SMS message | `src/EdgeServicesBuildingBlock.Contract/Notifications/SmsNotification.cs` |
| Package IDs, dependencies, excluded compile files | `src/EdgeServicesBuildingBlock.Shared/EdgeServicesBuildingBlock.Shared.csproj` and `src/EdgeServicesBuildingBlock.Contract/EdgeServicesBuildingBlock.Contract.csproj` |
| Framework and current owner dependencies | `src/Directory.build.props` and `src/Directory.Packages.props` |

For other capabilities, locate the actual declaration and inspect registration, configuration, lifetime, and consumer usage. Files excluded by the owner's project are not automatically public package APIs.

## Authentication and authorization

In the blog, registration starts at `src/WebApi/ProgramConfiguration/ServiceCollections.cs`; middleware ordering starts at `src/WebApi/ProgramConfiguration/ApplicationBuilder.cs`. Preserve endpoint metadata and public/private mapping unless the task changes access behavior.

The inspected owner implementation has these behaviors; recheck them against the package used by the consumer:

- `AddAuthenticationAndAuthorization(builder)` calls OpenIddict client setup, JWT authentication, and authorization registration. Avoid also registering this wrapper when customizing its individual calls.
- `AddFrameworkOpenIddict` enables client-credentials and password flows, disables token storage, and uses System.Net.Http. Client setup does not establish an identity server or token issuer.
- `AddFrameworkAuthentication` binds `Authentication` to `JwtOptions`, uses `Bearer`, and disables inbound claim mapping. Defaults are false booleans and empty strings. Registration base64-decodes `TokenDecryptionKey`; missing or invalid configuration can fail. Never copy token keys or credentials into examples.
- Passing `JwtBearerEvents` replaces the default events object. Preserve required audience checks explicitly when supplying custom events. The default token-validated event compares `aud` with `Authentication:Audience` using ordinal comparison.
- When diagnosing token acceptance, inspect actual `TokenValidationParameters`, including the custom `SignatureValidator`. Do not assert signature validation merely because `ValidateIssuerSigningKey` is set. Report a demonstrated package issue separately from the authorized consumer change.
- `AddFrameworkAuthorization` runs its customization callback before adding default `Read` and `Write` policies; do not assume the callback replaces them. The inspected scope assertion splits both `scope` and `scp` on spaces and compares ignoring case. Inspect authentication requirements separately.
- `RequireAuthorization` has policy-plus-roles and roles-only overloads. A single positional string can select roles. For the owner's policy overload on `RouteHandlerBuilder`, use named `policy` and verify overload resolution; framework route-group overloads can differ.
- Preserve `UseAuthentication()` before `UseAuthorization()`. Registration alone does not verify endpoint protection.

For access changes, verify unauthenticated, incorrect-audience, missing-scope, and authorized cases relevant to the endpoint. Compilation does not establish token acceptance or authorization behavior.

## Notification contracts and transport boundaries

The inspected owner records are in `EdgeServicesBuildingBlock.Contract.Notifications`:

```csharp
public record EmailNotification(string ReceiverEmail, string Title, string Content, string TemplateCode);
public record SmsNotification(string DestinationPhoneNumber, string Title, string Content, string Pattern);
```

Confirm matching artifact shapes before use. Preserve field names, constructor order, and serialization compatibility; do not recreate or silently extend these shared records inside a consumer.

Records specify payloads, not routing, delivery, templates, retries, or provider behavior. The blog has no active sender of either record at this baseline. Find the actual sender, receiving service, bus abstraction, registrations, and configuration before choosing publish/send semantics. Package availability and commented code do not establish a working feature. Keep orchestration behind Application interfaces and integration implementations in Infrastructure; verify cancellation and lifetimes using actual transport APIs.

If a payload needs owner changes, explain shared-contract compatibility, release/version integration, and producer/consumer verification. A sibling source edit does not change the installed package.

## Verification and maintenance

Follow the consumer's required restore/build and behavior checks. For blog service changes, use the Release WebApi restore/build workflow in `AGENTS.md` and appropriate runtime or regression verification. Report exact successes, failures, and environmental blockers. Builds are not required solely for this skill's prose.

Inspection baseline: **2026-10-11**. Blog central declarations request Shared and Contract **0.3.4-preview**. Cached artifacts target `net10.0` and record repository commit `bd6e91d99404f0b6989d99f06cc74f056e6b6bf8`; Shared XML documents the auth extension signatures. Inspected blog assets had no resolved package libraries, so resolved compatibility was not established. Owner source was read directly after coverage reported changed metadata. Its BusinessFramework dependency declaration differs from cached artifacts. Source-to-package implementation equivalence remains unverified; treat behaviors above as source guidance until checked against the consumer's package.

Recheck versions and source when using this skill. Follow the repository protection rule: report stale instructions during ordinary development; edit this skill only when the user explicitly requests repository-skill maintenance.
