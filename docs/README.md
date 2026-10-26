# Documentation

This documentation describes the inspected implementation as of 2026-10-26. Source and resolved package behavior take precedence when a detail changes.

| Document | Purpose |
| --- | --- |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Components, execution flow, API inventory |
| [IMPLEMENTATION-GAPS.md](IMPLEMENTATION-GAPS.md) | Source-verified gaps against the Unleash-like product goal and implementation priorities |
| [Spec 02](specs/02-feature-management-completion.specs.md) | Proposed completion design: ordinary JWT evaluation, targeting, variants, metrics and governance; excludes SDKs, application credentials and admin UI |
| [LAYERING.md](LAYERING.md) | Dependency direction and code placement |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Setup, build, run, validation |
| [PRINCIPLES.md](PRINCIPLES.md) | Practical Clean Code principles |
| [RULES.md](RULES.md) | Required implementation/documentation rules |
| [UPSTREAM-DEPENDENCIES.md](UPSTREAM-DEPENDENCIES.md) | Shared package ownership and reuse workflow |
| [specs/README.md](specs/README.md) | Single numbered specification/design/plan collection |
| [ts-contracts/README.md](ts-contracts/README.md) | Frontend endpoint contracts and wire conventions |
| [Agent context](../.ai/README.md) | Source navigation, examples, decisions and known gaps |

Architecture and application guidance stays here at the docs root. Specs, designs and implementation plans go only in `specs/`, including Superpowers work. Frontend contracts go in `ts-contracts/`.
