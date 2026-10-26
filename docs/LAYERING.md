# Layering

## Current compile-time dependencies

```text
Domain          -> BusinessFramework domain packages
Application     -> Domain + framework/shared packages
Infrastructure  -> Application + Domain + EF/Npgsql/integration packages
WebApi          -> Infrastructure (+ transitive Application/Domain) + HTTP packages
```

The WebApi project references Infrastructure directly as the composition root. Do not describe that as an Application-to-Infrastructure dependency.

| Layer | Owns | Keep out |
| --- | --- | --- |
| Domain | Aggregate factories, state transitions, invariants, value objects, domain events | Controllers, DbContext, HTTP requests, external IO |
| Application | Commands/queries and handlers, DTOs, mappings, persistence/integration interfaces | Concrete Infrastructure services, HTTP result mapping |
| Infrastructure | Application interface implementations, PostgreSQL/EF configurations/migrations, hashing/token IO support | Controller decisions, duplicate business rules |
| WebApi | Routes, binding, versioning, authorization, route/body consistency, middleware/composition | Database orchestration and aggregate rules |

Use `IApplicationDbContext` for existing persistence use cases. Do not add generic repositories or per-entity services around its DbSets. Existing packages can expose HTTP-related Result types to Application; preserve the established package contract while keeping controller concerns in WebApi.

Domain operations should own guarded transitions. For example, `ChangeRequest.Approve` requires InReview and rejects self-approval; handlers load state and save. Relationship collections must actually be loaded before operations that inspect them. Do not duplicate guards in controllers or bypass them through public setters.

For a new external integration, search both shared-package owners first. Only after establishing no suitable capability should an Application interface and minimal Infrastructure implementation be introduced. Use constructor injection, keep lifetimes compatible with scoped dependencies, and register at the existing composition points.
