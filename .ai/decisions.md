# Decisions

These are the user's repository policies recorded on 2026-10-26. They do not approve implementing archived proposals.

| Decision | Consequence |
| --- | --- |
| Prefer a small implementation | Extend existing commands/queries and domain methods; no speculative architecture or service proliferation. |
| Shared packages first | Before any new service, search both upstream indexed projects and verify suitability/version; document why reuse cannot satisfy the need. |
| User owns indexing | Agents use read-only graph queries and source fallback; the user manually re-indexes after tasks. |
| Persian error text | All authored errors, exception messages, validation messages and error titles/details use Persian; preserve protocol identifiers. |
| One specification directory | `docs/specs/` holds numbered specs/designs/plans, including work requested through Superpowers. No parallel `superpowers` tree. |
| Contracts accompany endpoints | Maintain commented TypeScript contracts for each endpoint-bearing feature in `docs/ts-contracts/`. |
| Architecture docs are easy to find | Place application/architecture guidance at the root of `docs/`, agent guidance in `.ai/`. |
| Practical Clean Code | Favor names, cohesion, explicit dependencies, focused functions and meaningful tests; avoid ceremony. |

Documentation organization chosen for this task: one TypeScript file per existing API area, shared protocol types in `Common.ts`, diagnostic endpoints in `Diagnostics.ts`. `AmazingDeals.ts` in the requested tree was an example name; no AmazingDeals endpoint exists, so no fictional contract is added. The existing SDK proposal is preserved under spec 01 to avoid losing material while correcting its status.

Add future significant decisions here with date, reason, and affected source/spec. Do not convert an observation into a product requirement without evidence.

## Product scope clarification — 2026-10-26

The user excludes backend/frontend SDK access, application credentials and an administration UI. They explicitly confirmed ordinary JWT-protected evaluation endpoints, with browsers consuming results through their own application's backend. Do not implement SDK packages, Unleash protocol feeds or an API-key system. Existing legacy token endpoints are not a design precedent for new application credentials.

The requested work includes usage visibility, full targeting, reliable consumption, variants, approvals/scheduling/history/advanced rollout and all defects in the inspection. The user approved the full design in [spec 02](../docs/specs/02-feature-management-completion.specs.md) on 2026-10-26 and authorized implementation. The historical audit's excluded-feature recommendations do not override this clarification. The related [implementation plan](../docs/specs/02-feature-management-completion.plan.md) tracks delivery separately; design approval does not claim implemented behavior.
