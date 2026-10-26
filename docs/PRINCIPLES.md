# Practical Clean Code principles

The following applies ideas associated with Robert C. Martin's *Clean Code* and SOLID to this repository. These are paraphrased engineering guidelines, not quotations or rigid metrics.

- **Names reveal intent.** Name use cases and domain transitions explicitly (`ApproveChangeRequest`, `SetFeatureLimit`). Avoid generic managers/helpers when a domain verb fits.
- **Functions do one coherent job.** Keep a handler's load → domain operation → save flow easy to read. Extract a function when it clarifies meaning or removes real duplication, not to satisfy an arbitrary line count.
- **One reason to change.** Keep HTTP binding in WebApi, orchestration in Application, state rules in Domain and persistence in Infrastructure. A class per operation is already the local handler convention; an additional service per handler is unnecessary.
- **Depend on the appropriate boundary.** Reuse `IApplicationDbContext`, framework contracts and existing interfaces. Add abstractions only for real policy/IO boundaries or demonstrated variation; do not wrap every class in an interface.
- **Cohesion and encapsulation.** Keep related state and invariants together. Call aggregate operations rather than duplicating their rules in controllers. Load the data those invariants need.
- **Remove meaningful duplication.** Reuse upstream capabilities first. Similar-looking handlers with different rules do not automatically require a generic CRUD framework. A little repetition is preferable to a misleading abstraction.
- **Clear errors.** Make failures explicit and actionable in Persian, preserve stable error codes/status semantics, and avoid broad catch blocks that silently hide faults. Respect cancellation.
- **Comments explain reasons.** Prefer clear code over narration. Document package/version constraints, unusual mapping behavior and client obligations. Remove stale claims and commented-out scaffolding when the task authorizes touching it.
- **Tests describe behavior.** Exercise business boundaries, persistence behavior, authorization and serialization when changed. Avoid brittle tests coupled to private implementation or repetitive assertions that add no confidence.
- **Leave touched code clearer.** Improve names and remove obsolete local clutter within scope; do not turn a focused task into a solution-wide refactor.

Apply open/closed, substitution and interface-segregation ideas where there is actual extensibility or polymorphism. They do not require factories, strategies, adapters or an inheritance tree for ordinary commands/queries. Four simple use cases should remain four understandable flows.
