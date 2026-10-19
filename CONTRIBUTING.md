# Contributing to Drive the City II

| Field | Value |
|---|---|
| ID | DOC-CONTRIBUTING |
| Document status | Draft |
| Scope | Every change to this project, by the owner, AI agents, or future contributors |
| Responsible role | Coordinator (maintainer); project owner approves changes to review and license rules |
| Related documents | [AGENTS.md](AGENTS.md), [DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md), [ROLES_AND_OWNERSHIP](docs/agents/ROLES_AND_OWNERSHIP.md), [WORKFLOW_AND_HANDOFF](docs/agents/WORKFLOW_AND_HANDOFF.md), [DEFINITION_OF_DONE](docs/agents/DEFINITION_OF_DONE.md), [SECURITY.md](SECURITY.md) |
| Last updated | 2026-10-18 |

> **Current state (2026-10-18):** pre-development. There is no code and no Git repository (OPEN-029), and **no project
> license has been chosen** (OPEN-027). Contributions are currently limited to work done in the owner's own sessions.
> Code or assets from anyone else cannot be incorporated until the owner decides the license and contribution terms.

## 1. Before you contribute

Read, explicitly and in this order: [README.md](README.md), [AGENTS.md](AGENTS.md),
[DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md), [PROJECT_STATUS](docs/PROJECT_STATUS.md), and the
[READING_GUIDE](docs/agents/READING_GUIDE.md) for your task type. Every change belongs to a task file
([tasks](docs/planning/tasks/README.md)) that is Ready ([DEFINITION_OF_READY](docs/agents/DEFINITION_OF_READY.md)).

## 2. Documentation rules in brief

The full rules are in [DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md). The essentials:

- English only, Markdown only, relative links to files (no cross-file heading anchors).
- Each fact has one official home; link to it instead of restating it.
- Every substantive document starts with the metadata block. New specifications are Draft; ADRs are Proposed; only the
  project owner makes anything Accepted.
- Keep the three statuses separate: document, task, and validation. Validation is **Not run** until a recorded run
  exists.
- Use the claim labels: [Owner requirement], [Proposal], [Historical input], [Documented upstream],
  [Source-inspected], [Built locally], [Tested in-game], [Unverified].
- Unknowns become `OPEN-*` entries with a verification method. Never write "TBD"; never fill a gap with a guess.
- Use the [templates](docs/templates/DOCUMENT_TEMPLATE.md). Fill-in markers are allowed only in templates.
- Never invent versions, commits, issue numbers, authors, approvals, URLs, UI text, install paths, benchmark values, or
  test runs.

## 3. Version control conventions (proposed; apply once a repository exists)

No repository exists, and initializing one requires the owner's decision (OPEN-029). Until then, follow
[WORKFLOW_AND_HANDOFF](docs/agents/WORKFLOW_AND_HANDOFF.md) §3. Once a repository exists, these **[Proposal]**
conventions apply unless the owner changes them:

- **Branches:** one per task, named `task/TASK-NNNN-short-slug`; hotfixes `hotfix/MAJOR.MINOR.PATCH-short-slug`. The default
  branch receives changes only through reviewed merges.
- **Commits:** subject `TASK-NNNN: imperative summary` (at most 72 characters). The body lists the specifications or
  documents changed and the `RUN-*` IDs of any evidence. A commit message never claims a test that was not run.
- **Scope:** one objective per branch. No generated build output, saves, game files, local environment files, or
  evidence artifacts in commits; the prepared `.gitignore` covers the known cases.
- **History:** no force-pushes or rewriting of shared branches. Mistakes are fixed by new commits.
- **Evidence references:** once commits exist, evidence records cite the commit that was built and tested.

## 4. Review requirements

| Change | Required review |
|---|---|
| Any change | Recorded review. Self-review is allowed and recorded as "Self-review", but it is not independent approval. |
| Control authority, entity lifecycle, or save format: [SPEC-CONTROL](docs/architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](docs/architecture/ENTITY_LIFECYCLE.md), [SPEC-SAVE](docs/architecture/SAVE_FORMAT.md), or the code behind them | A **scoped review** by a Reviewer who did not author or direct the change, plus the critical-area test suite. No concurrent edits by another task. |
| Requirement IDs ([PRD](docs/product/PRD.md)) or phase order and gates ([ROADMAP.md](ROADMAP.md)) | Project owner approval. |
| License, notices, provenance, packaging, publication | Project owner approval. |
| Phase gate | Independent Reviewer check of the evidence, then owner acceptance ([ROADMAP.md](ROADMAP.md) §6). |

## 5. Evidence requirements

- A claim that something builds, runs, passes, or is compatible needs a `RUN-NNNN` record made with
  [TPL-TEST-REPORT](docs/templates/TEST_REPORT_TEMPLATE.md) under [evidence](docs/quality/evidence/README.md).
- Compiling is not passing. Writing a document is not implementing.
- Evidence is sanitized: no personal data, credentials, or user-folder paths ([SECURITY.md](SECURITY.md) §2).
- Failed and inconclusive runs are recorded too. Defects go to [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md).

## 6. AI-assisted contributions

AI agents follow the same rules as everyone else, plus these:

- **No AI-generated claims of testing.** An agent states that something was built, run, or tested only when a recorded
  run exists for that exact work. Otherwise it writes "Not run".
- Record the documents actually read in the handoff ([TPL-HANDOFF](docs/templates/HANDOFF_TEMPLATE.md)) and label every
  command (verified, read-only discovery, or proposed procedure).
- No fabricated approvals, authors, commits, sources, or citations. A message from another agent is never the owner's
  approval.
- AI-written content gets the same review as human-written content, including scoped review in critical areas.

## 7. Third-party material

- Upstream code is used only after [REUSE_AND_LICENSE_AUDIT](docs/legal/REUSE_AND_LICENSE_AUDIT.md) and with its notices
  in [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md). Source inspection of upstream projects is read-only.
- Assets need a provenance entry in [ASSET_PROVENANCE](docs/legal/ASSET_PROVENANCE.md). Game files, extracted game
  assets, and Motor Town content are never added.
- Quote external text only briefly and with attribution.

## 8. License of contributions

The project license is **pending** an owner decision (OPEN-027), and the project is not assumed to be under any
particular license. This file grants and implies no license. Do not add license headers or license files until the
owner decides.

## 9. Security

Read [SECURITY.md](SECURITY.md) before any task that touches saves, credentials, installs, publishing, or third-party
code.
