# Specifications, designs and plans

This is the **only** specification directory, including work performed with Superpowers. Do not create `docs/superpowers/`, `docs/plans/`, or a second design/spec tree.

Use sequential two-digit prefixes beginning at `01`: `01-topic.specs.md`, `02-next-topic.specs.md`. Use the next unused number for a new specification, preserve existing numbers, and reference the number in subsequent work. A related implementation plan uses the same number (`01-topic.plan.md`) and stays here. Extend the number width if the collection eventually exceeds 99; do not reuse numbers.

Each new spec should state status, intent, scope/non-goals, existing source and upstream reuse findings, acceptance criteria, endpoint/contract changes, Persian error requirements, and verification. Separate user-approved requirements from assumptions and current limitations. Keep architecture/application guidance at the docs root.

| Number | Document | Status |
| --- | --- | --- |
| 01 | [SDK evaluation proposal](01-sdk-evaluation-proposal.specs.md) | Archived legacy proposal; unimplemented and not approved for development |
| 02 | [Feature management completion](02-feature-management-completion.specs.md), [implementation plan](02-feature-management-completion.plan.md) | Design approved 2026-10-26; implementation pending; no SDKs/application credentials/admin UI |

The original root README content was moved into spec 01 without discarding its examples. Its claims and old imports/paths are not evidence of implemented functionality. A request to implement spec 01 requires revisiting its scope and shared-package reuse before development.
