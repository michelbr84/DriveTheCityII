# TASK-0000 — Documentation bootstrap

| Field | Value |
|---|---|
| ID | TASK-0000 |
| Task status | In review |
| Task type | Documentation |
| Phase | Pre-PHASE-00 (documentation baseline; completes no development phase) |
| Responsible role | Coordinator (executed, reconciled, and self-validated); Reviewer and project owner (pending review) |
| Depends on | — |
| Authorization required | Owner brief of 2026-10-18: Markdown documentation under the project root, bounded read-only checks of existing tools, read-only public research, and justified `.gitignore`/`.gitattributes`/`.editorconfig`. Nothing else. |
| Created | 2026-10-18 |
| Last updated | 2026-10-18 |

## 1. Objective

Create the complete English documentation baseline for Drive the City II before any technical implementation and
before any Melty session. It must honestly distinguish requirements, proposed designs, externally documented facts,
and locally verified results. Every unknown becomes a question with a validation procedure instead of a guess.

## 2. Background

- The project root held only `chat.md`: a Portuguese export of an earlier assistant planning conversation, truncated
  at the start and with its web citations stripped. It is kept unchanged as a historical input (SRC-006).
- The optional inputs `ROADMAP_Drive_the_City_II.md` and `ESTRUTURA_DOCUMENTAL_Drive_the_City_II.md` were not present.
- The owner brief of 2026-10-18 (SRC-007) defines the manifest, the scope, the technical contracts, the phases, and
  the limits. It supersedes the historical input where they differ (see CONF-001 to CONF-013 in the
  [register](../RISKS_AND_OPEN_QUESTIONS.md)).

## 3. Required reading (actually read)

- The owner brief of 2026-10-18 (in full).
- [chat.md](../../../chat.md) (in full, as a historical input).
- Public sources inspected read-only, listed in [SOURCES](../../research/SOURCES.md).

## 4. Allowed scope (files/areas/operations)

- Create Markdown files of the manifest under the project root, plus `.gitignore`, `.gitattributes`, and
  `.editorconfig`.
- Run read-only checks of existing tools: `git --version`, `git rev-parse --show-toplevel`, `python --version`,
  `node --version`.
- Fetch public pages read-only (web fetch and `curl` of raw text). Nothing fetched was executed.
- Run validation scripts kept in the session scratchpad (outside the project).

## 5. Forbidden actions and protected contracts

All of the following were respected:

- no code, solution, Unity project, or build;
- no installs, game or Melty launches, mod enablement, or settings changes;
- no access to saves, credentials, or unrelated folders;
- no cloning into the project;
- no Git initialization, commit, push, or publishing;
- no change to `chat.md`;
- no license chosen;
- no invented versions, results, approvals, or commands presented as tested.

## 6. Procedure (as executed)

1. Verified that the working directory resolves to `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII`.
   Listed its contents (only `chat.md`). Confirmed that it is not a Git repository and has no parent repository.
   Recorded the tool versions present.
2. Read the historical input and the brief. Wrote a shared conventions brief (IDs, terminology, structures, ID ranges)
   in the session scratchpad.
3. A research subagent inspected the five seed sources and nine further sources read-only. The Coordinator re-checked
   ten key facts against raw text with `curl` (license, commit, project file, README limitations, publish
   configuration, notice, the Melty site and terms, the agent-tooling license).
4. Wrote the canonical backbone: [DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md), the [PRD](../../product/PRD.md)
   (93 requirements), the [ROADMAP](../../../ROADMAP.md), and the [register](../RISKS_AND_OPEN_QUESTIONS.md).
5. Seven writing subagents produced the architecture, gameplay, product/experience, research/engineering/legal,
   quality, planning, and agents/release/user/templates groups in parallel. Each had explicit file ownership and the
   same conventions.
6. The Coordinator wrote the root entry points, [INDEX](../../INDEX.md), [PROJECT_STATUS](../../PROJECT_STATUS.md),
   [BACKLOG](../BACKLOG.md), the generated [TRACEABILITY](../TRACEABILITY.md), this task, the
   [handoff](../../agents/HANDOFF_TO_PHASE_00.md), and the validation report.
7. Reconciled the writers' reports:
   - merged 76 contributed register entries, marking 3 duplicates as merged;
   - aligned the PHASE-08 gate with the owner brief (CONF-013);
   - rewrote past-tense gate-acceptance wording in the phase plans as requirements;
   - clarified PRD wording (REQ-ENTITY-002, REQ-CAREER-002) and phase columns (REQ-CTRL-002, REQ-REL-003);
   - aligned the transition-save and company-vehicle semantics between the gameplay and architecture specs.
8. Validated the result ([DOCUMENTATION_VALIDATION_REPORT](../../quality/DOCUMENTATION_VALIDATION_REPORT.md)).

## 7. Validation

Documentation checks only. This is not gameplay QA. The method, tools, results, and limitations are in the
[DOCUMENTATION_VALIDATION_REPORT](../../quality/DOCUMENTATION_VALIDATION_REPORT.md). Runtime validation: **Not run**
(nothing was built or launched).

## 8. Acceptance criteria

| # | Criterion (owner brief §11) | Result |
|---|---|---|
| 1 | Every manifest file exists with project-specific content | See report DV-001 |
| 2 | Local links and anchors resolve | See report DV-002 and DV-003 |
| 3 | Unique IDs and cross-references; 14 phases; 6 milestones; every mandatory requirement covered | See report DV-004 to DV-007 |
| 4 | English throughout; canonical path; no conflicting old path instructions | See report DV-008 and DV-009 |
| 5 | Consistent scope, bus semantics, company simulation, save contracts, phase gates, Melty's optional role | See report DV-010 |
| 6 | Unknowns recorded with a resolution method; no empty placeholders | See report DV-011 |
| 7 | No feature reported as implemented; no fictitious evidence; no unverified command presented as proven | See report DV-012 |
| 8 | No unauthorized operations; changed files reviewed against the initial state | See report DV-013 |
| 9 | Private data and generated artifacts excluded from prospective version control without blanket rules | See report DV-014 |
| 10 | Validation report written | This task's output |

Acceptance of this task requires a review. Self-validation by the Coordinator is recorded, but it is not independent
approval ([ROLES_AND_OWNERSHIP](../../agents/ROLES_AND_OWNERSHIP.md)).

## 9. Outputs (documents to update)

- 118 manifest documents, plus `.gitignore`, `.gitattributes`, and `.editorconfig` (121 new files). No pre-existing
  file was modified.
- [PROJECT_STATUS](../../PROJECT_STATUS.md), [CHANGELOG](../../../CHANGELOG.md), and
  [HANDOFF_TO_PHASE_00](../../agents/HANDOFF_TO_PHASE_00.md) reflect only the work actually completed.

## 10. Recovery / rollback

- All outputs are new files. Removing them would restore the original state, with `chat.md` untouched.
- Removal is only done at the project owner's explicit request. Without version control (OPEN-029, RISK-701), make a
  copy of the folder before any bulk change.

## 11. Handoff notes

See [HANDOFF_TO_PHASE_00](../../agents/HANDOFF_TO_PHASE_00.md). The baseline does not authorize Phase 00 work. The
next tasks, [TASK-0001](TASK-0001-windows-environment-audit.md) and
[TASK-0002](TASK-0002-base-mod-source-audit.md), need the project owner's go-ahead.

## 12. Status history

| Date | Status | Event | Recorded by (role) |
|---|---|---|---|
| 2026-10-18 | In progress | Workspace verified; brief and historical input read; work started. | Coordinator |
| 2026-10-18 | In review | Baseline written, reconciled, and validated; awaiting review by the project owner. | Coordinator |
