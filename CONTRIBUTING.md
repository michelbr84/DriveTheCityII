# Contributing to Drive the City II

| Field | Value |
|---|---|
| ID | DOC-CONTRIBUTING |
| Document status | Draft |
| Scope | Every change to this project, by the owner, AI agents, or future contributors |
| Responsible role | Coordinator (maintainer); project owner approves changes to review and license rules |
| Related documents | [AGENTS.md](AGENTS.md), [DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md), [ROLES_AND_OWNERSHIP](docs/agents/ROLES_AND_OWNERSHIP.md), [WORKFLOW_AND_HANDOFF](docs/agents/WORKFLOW_AND_HANDOFF.md), [DEFINITION_OF_DONE](docs/agents/DEFINITION_OF_DONE.md), [SECURITY.md](SECURITY.md) |
| Last updated | 2026-10-20 |

> **Current state (2026-10-20):** a development build (0.1.0) exists; see [PROJECT_STATUS](docs/PROJECT_STATUS.md).
> The source is hosted at <https://github.com/michelbr84/DriveTheCityII> under the [MIT License](LICENSE). Most work
> happens in the owner's own sessions. Issues and pull requests can be opened on GitHub; the owner reviews every change
> and may decline any of them.

## 1. Before you contribute

Read, explicitly and in this order: [README.md](README.md), [AGENTS.md](AGENTS.md),
[DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md), [PROJECT_STATUS](docs/PROJECT_STATUS.md), and the
[READING_GUIDE](docs/agents/READING_GUIDE.md) for your task type. Every change in the owner's sessions belongs to a task
file ([tasks](docs/planning/tasks/README.md)) that is Ready ([DEFINITION_OF_READY](docs/agents/DEFINITION_OF_READY.md)).
For an outside contribution, a GitHub issue that describes the change is enough to start; the owner decides whether it
becomes a task.

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

## 3. Version control conventions

The repository is Git, with `main` as the default branch on GitHub. So far, the owner's own sessions have committed
directly to `main`. For any other contribution, these conventions apply unless the owner changes them:

- **Branches and pull requests:** one branch per change (for example `task/TASK-NNNN-short-slug` or
  `fix/ISSUE-NNN-short-slug`), merged into `main` only through a pull request that the owner has reviewed.
- **Commits:** a short imperative subject (at most 72 characters), naming the task or issue ID when there is one. The
  body lists the specifications or documents changed and the `RUN-*` IDs of any evidence. A commit message never claims
  a test that was not run.
- **Checks before a pull request:** `tools\build.ps1` passes (unit tests and mod build). Say in the pull request
  whether you tested in game, and how.
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

The project is licensed under the [MIT License](LICENSE) (owner decision, 2026-10-20; OPEN-027). By submitting a
contribution, you agree that it is licensed under the same terms. Submit only work you have the right to license this
way: never game code (decompiled or otherwise), extracted game assets, or third-party code that has not gone through
section 7.

## 9. Security

Read [SECURITY.md](SECURITY.md) before any task that touches saves, credentials, installs, publishing, or third-party
code.
