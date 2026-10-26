---
name: conventional-commits
description: Use when drafting, reviewing, or creating Git commit messages, including squash and revert commits, in any repository. Apply the user's Conventional Commits types, subject rules, and breaking-change conventions.
---

# Conventional commit messages

Apply these conventions to every commit message written or reviewed, across repositories and languages. Base the message on the actual changes; for a commit being created, inspect the staged diff. This skill defines message conventions and does not itself authorize committing, staging, amending, pushing, or installing hooks.

## Message format

```text
<type>(<optional-scope>): <short summary>

<optional body explaining why the change is necessary>

<optional footers>
```

Omit the parentheses when there is no useful scope: `docs: update contribution guide`.

## Choose the type

| Type | Use for |
| --- | --- |
| `feat` | A new feature or capability |
| `fix` | A bug fix |
| `docs` | Documentation-only changes, including XML comments and Swagger descriptions |
| `style` | Formatting, whitespace, or punctuation changes without a logic change |
| `refactor` | Code changes that neither fix a bug nor add a feature |
| `perf` | Performance improvements |
| `test` | Adding or correcting tests |
| `build` | Build-system or external dependency changes, such as NuGet, npm, or MSBuild |
| `ci` | CI/CD configuration or script changes |
| `chore` | Other maintenance that does not modify source or test files, such as `.gitignore` |
| `revert` | Reverting a previous commit |

Choose the type by the purpose of the change. A bug fix accompanied by regression tests is `fix`; a new feature accompanied by tests is `feat`. Keep unrelated features, fixes, and refactors in separate atomic commits when practical.

Under Conventional Commits, `feat` corresponds to a SemVer MINOR release and `fix` to PATCH. A breaking change with any type corresponds to MAJOR. Other types have no implicit SemVer effect; actual release automation follows the repository's configuration.

## Subject rules

1. Use a lowercase type and an optional concise lowercase scope identifying the affected area, such as `api`, `auth`, `ui`, `db`, or `nuget`.
2. Write the summary in the imperative mood: `add`, `fix`, `prevent`, or `extract`; avoid `added`, `adds`, or `updating`. It should complete “If applied, this commit will ...”.
3. Start the summary with a lowercase word. Preserve the correct casing of identifiers and proper names inside it, such as `GetAuthorityProfiles` or `AutoMapper`.
4. End the subject without a period.
5. Aim for 50 characters or fewer and never exceed 72 characters for the entire subject, including type, scope, punctuation, and spaces. Do not pad a shorter clear subject.
6. Describe the concrete change. Avoid vague subjects such as `Fixed bug`, `update stuff`, or `WIP`. Do not use WIP messages for commits destined for main branches; use a meaningful final squash message.

## Body and footers

Separate the subject from an optional body with a blank line. Explain the reason, context, or important consequences rather than repeating the subject. Separate footers from the body with another blank line.

Use issue references only when a real issue identifier is provided or verified. For example:

```text
fix(auth): prevent expired tokens from being accepted

Validate expiration before granting access to protected endpoints.

Closes #1234
```

When reverting, use `revert` and identify the actual reverted commit in the body when known. Do not invent a commit hash or issue reference.

## Breaking changes

Mark every backward-incompatible change, regardless of type, using `!` immediately before the colon or a `BREAKING CHANGE:` footer, or both. Explain the incompatibility and migration in the body or footer when needed.

```text
feat(api)!: rename pagination query parameters

BREAKING CHANGE: Replace page and size with pageNumber and pageSize
in API requests.
```

Both `feat!: ...` and `feat(api)!: ...` are valid. Keep the footer token exactly `BREAKING CHANGE:`. A dependency upgrade is not automatically a breaking change; assess its effect on consumers.

## Examples

```text
feat(api): add pagination to GetAuthorityProfiles endpoint
fix(middleware): prevent null references in the global error handler
refactor(controller): extract mapping into BaseApiController
docs(xml): add summary tags for AuthorityProfileDto
build(nuget): upgrade AutoMapper to version 13.0.1
```

## Before returning or using a message

Check that the type matches the actual change, the scope is useful or omitted, the summary starts lowercase and uses the imperative mood, the subject has no trailing period and is at most 72 characters, and breaking changes are marked. Confirm that body and footer separators are blank lines and references are real.

If asked only for a commit message, return the message in a plain text code block. If asked to review one, identify the violations and provide a corrected version grounded in the supplied changes.

Commitlint and Git hooks can enforce this convention mechanically. Set them up only when requested; installing this skill alone does not enforce messages written by other tools or contributors.
