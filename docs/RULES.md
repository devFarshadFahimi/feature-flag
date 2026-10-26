# Implementation rules

The root [AGENTS.md](../AGENTS.md) is the entry point and applies to every task.

1. **Smallest adequate change.** Prefer existing handlers/domain methods; do not multiply services, layers, DTO wrappers or files for hypothetical reuse. Keep related request/handler code together.
2. **Reuse before creating services.** Search local code and both upstream Codebase Memory projects, read material declarations and registrations, check coverage and resolved versions, and record findings. A zero-result or stale index alone does not authorize a replacement.
3. **User-controlled indexing.** Never index/re-index, refresh/rebuild a graph, or produce index artifacts. Use read-only existing graph tools and source fallback. Ask the user to manually re-index affected repositories at the end of an indexed task.
4. **Persian errors.** All newly authored or changed error messages, Result titles/details, exceptions, validators, HTTP mismatch messages and client-visible fallback errors must be Persian. Stable codes, identifiers and protocol names remain unchanged. Verify automatic binding/auth/package fallback messages when those paths change. Report existing English text as debt; do not claim it has already been localized.
5. **Correct layer.** Follow [LAYERING.md](LAYERING.md), existing custom mediator contracts, generator registration and actual aggregate invariants.
6. **Precise client contracts.** Every endpoint-bearing feature has a named TypeScript contract with commented frontend guidance. Update route, binding, enum representation, defaults/nulls, auth, responses/errors and examples together with backend changes. Avoid fictional features and universal response envelopes.
7. **One numbered specification tree.** All specs/designs/plans, including Superpowers output, go in `docs/specs/`. Use the next sequential two-digit number beginning with `01`, preserve references, and keep related plans under the same number. Never create a second `superpowers/specs` or `plans` directory.
8. **Application docs at docs root.** Keep architecture, layering, principles, development, rules and dependency guidance directly under `docs/`; keep `.ai/` concise and linked.
9. **Practical Clean Code.** Use [PRINCIPLES.md](PRINCIPLES.md); abstraction must remove a real problem. Prefer focused behavioral verification over tests that reproduce implementation.
10. **Evidence before completion.** Check actual source/package API, relevant compilation/behavior, contracts and links. Distinguish source inspection, compilation and live runtime evidence. Preserve unrelated changes and never include credentials or token values in documentation.

These rules describe desired development behavior. They do not assert the existing implementation already meets every rule.
