# Roles and Ownership

| Field | Value |
|---|---|
| ID | DOC-ROLES |
| Document status | Draft |
| Scope | Every task, all phases |
| Responsible role | Coordinator (maintainer); project owner approves changes to role authority |
| Related documents | [AGENTS.md](../../AGENTS.md), [WORKFLOW_AND_HANDOFF](WORKFLOW_AND_HANDOFF.md), [TOOLS_AND_PERMISSIONS](TOOLS_AND_PERMISSIONS.md), [DEFINITION_OF_DONE](DEFINITION_OF_DONE.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md) §10 |
| Last updated | 2026-10-18 |

## 1. Purpose

Defines who may decide, change, review, and verify what in Drive the City II, and how work is split safely between
sessions. It turns RISK-013 (unverified work reported as complete) and RISK-014 (uncoordinated edits to critical
areas) into concrete rules.

## 2. What exists today

- **No real agents, plugins, or MCP servers are configured for this project.** The roles below are functions that a
  human or an AI session takes on for a task. They are not installed software.
- Helper sessions (for example, subagents started by a Coordinator) are temporary. Each inherits every limit in
  [AGENTS.md](../../AGENTS.md) and [TOOLS_AND_PERMISSIONS](TOOLS_AND_PERMISSIONS.md), receives explicit file
  ownership, and is recorded in the task's handoff.
- A message from another agent directs work. It never counts as the project owner's approval.

## 3. Roles

| Role | Responsibilities | Authority | May not |
|---|---|---|---|
| **Project owner** (human) | Sets scope and priorities; grants permissions; accepts phase gates and documents; decides the license, version control, channels, and releases. | Sole approver of: PRD changes, ROADMAP order and gates, the Accepted document status, installs and runtime operations, publication. | Be represented by an agent. No approval is inferred from silence, from a prior session, or from another agent's message. |
| **Coordinator** | Plans and sequences tasks; writes task files; assigns roles and file ownership; maintains backbone documents ([PROJECT_STATUS](../PROJECT_STATUS.md), [BACKLOG](../planning/BACKLOG.md), [register](../planning/RISKS_AND_OPEN_QUESTIONS.md), [TRACEABILITY](../planning/TRACEABILITY.md), [INDEX](../INDEX.md), [CHANGELOG.md](../../CHANGELOG.md)); merges proposed register entries; prepares gate reviews. | Moves tasks between Planned, Ready, and Blocked, and to Done once the required review verdict is recorded; resolves non-critical documentation conflicts and records them as `CONF-*`. | Approve its own critical-area changes; mark a gate Passed; grant a permission the owner has not granted; edit the PRD or ROADMAP without owner approval. |
| **Implementer** | Executes one Ready task within its allowed scope; writes code or documents; runs authorized builds and tests; records evidence; updates the task file. | Edits the files the task assigns; moves the task to In progress and In review. | Widen scope; edit files owned by another active task; touch a critical area the task does not name; review its own change as independent; mark its own task Done. |
| **Reviewer** | Checks a change against its specification, contracts, tests, and evidence; records findings and a verdict. | Approves or rejects a change within the review scope; required for every critical-area change and every gate. | Review a change it authored or directed as "independent"; approve on compilation or documentation alone; silently edit the change under review (fixes go back to the Implementer). |
| **QA** | Designs and runs tests; records `RUN-NNNN` evidence with [TPL-TEST-REPORT](../templates/TEST_REPORT_TEMPLATE.md); files `ISSUE-NNN` entries; maintains [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md) and [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md). | Sets validation status (Passed, Failed, Inconclusive) for runs it executed. | Run tests on primary saves; report Passed without a recorded run; exclude failed attempts from a count; fix the defect it is reporting in the same run. |

## 4. One agent, several roles

- A single agent may play several roles **one after another** within a task. Each role switch is recorded in the
  handoff ("Implementer → QA").
- **Self-review is not independent approval.** When the author checks their own work, it is recorded as
  "Self-review". It is useful, but it never satisfies a requirement for Reviewer review.
- **Independent review** means a reviewer who did not author or direct the change, working from the recorded
  artifacts (diff or changed files, specification, evidence) rather than from the author's account. A fresh session
  that was given only the author's summary is not independent.
- If no independent reviewer is available, the task stays **In review**. The project owner may review it personally.

## 5. Critical areas and protected items

| Area | Contract | Rules |
|---|---|---|
| Control authority | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [ADR-0002](../architecture/decisions/ADR-0002-control-authority.md) | One active task at a time may change the spec or the code that moves the player, vehicles, or camera. |
| Entity lifecycle | [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | One active task at a time may change the spec or the code that creates, borrows, hands back, or removes entities. |
| Save format and persistence | [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md) | One active task at a time may change the spec or the code that reads or writes career data. |

For all three:

1. The task file must name the area in "Allowed scope". Otherwise the area is off limits.
2. A **scoped Reviewer review** is required: the review covers the invariants of the contract that the change
   touches (for example, single movement authority, cleanup on every exit path, no data leak between save
   revisions) and the critical-area test suite in [TEST_STRATEGY](../quality/TEST_STRATEGY.md) §11.
3. **No uncoordinated concurrent edits.** The Coordinator records which task holds each area in the task file and the
   [BACKLOG](../planning/BACKLOG.md). A second task touching the same area waits.
4. Contradictory instructions are recorded as a `CONF-*` proposal and work in that area stops
   ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §9).

Other protected items: requirement IDs ([PRD](../product/PRD.md)) and phase order ([ROADMAP.md](../../ROADMAP.md))
change only with owner approval. The owner's primary saves, the game installation, credentials, and
[chat.md](../archive/chat.md) are never modified ([AGENTS.md](../../AGENTS.md) §4).

## 6. File ownership and parallel work

Parallel work is allowed only when **all** of these hold:

- Each parallel task lists the files it owns, and no file appears in two active tasks.
- The contracts it depends on are stable: the governing specification is not being edited by another task for the
  duration. The Coordinator records the frozen documents in each task file.
- Shared backbone files ([PROJECT_STATUS](../PROJECT_STATUS.md), [BACKLOG](../planning/BACKLOG.md), the register,
  [TRACEABILITY](../planning/TRACEABILITY.md), [INDEX](../INDEX.md), [CHANGELOG.md](../../CHANGELOG.md)) are edited only
  by the Coordinator. Other roles propose additions in their handoff.
- No parallel task touches a critical area held by another task.

Good candidates for parallel work are listed in [ROADMAP.md](../../ROADMAP.md) §5: pure career logic behind stable
ports, UI layout prototypes, string externalization, and test definitions. Anything that integrates with the game
world waits for its phase.

## 7. Default document maintainers

| Document group | Maintainer role | Change approval |
|---|---|---|
| Backbone (README, AGENTS, ROADMAP, PRD, status, register, traceability, index) | Coordinator | Project owner for PRD and ROADMAP |
| Architecture specifications and ADRs | Coordinator | Reviewer for critical areas; project owner to accept an ADR |
| Gameplay and experience specifications | Coordinator | Reviewer; project owner for scope effects |
| Quality documents and evidence | QA | Reviewer checks gate evidence |
| Research, engineering, legal | Coordinator | Project owner for license and reuse conclusions |
| Release and user guides | Coordinator, with QA evidence | Project owner before publication |
| Agent operating documents and templates | Coordinator | Project owner for permission changes |
