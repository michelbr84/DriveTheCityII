# Task Template

| Field | Value |
|---|---|
| ID | TPL-TASK |
| Document status | Draft |
| Used for | Task files `docs/planning/tasks/TASK-NNNN-short-name.md` |
| Responsible role | Coordinator (maintainer) |
| Related documents | [tasks](../planning/tasks/README.md), [BACKLOG](../planning/BACKLOG.md), [DEFINITION_OF_READY](../agents/DEFINITION_OF_READY.md), [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md), [TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md) |
| Last updated | 2026-10-18 |

## How to use

- Use the next free task number; add a row to [BACKLOG](../planning/BACKLOG.md). The task file is the single source of
  truth for its own status.
- A task has **one** objective. Check it against [DEFINITION_OF_READY](../agents/DEFINITION_OF_READY.md) for its type
  before setting it to Ready.
- "Authorization required" names the exact Tier 2 operations; the owner's grant is recorded in the status history with
  its date. No grant, no operation.
- Status history rows record only real events.

## Template

```markdown
# TASK-<NNNN> — <Short imperative title>

| Field | Value |
|---|---|
| ID | TASK-<NNNN> |
| Task status | Planned |
| Task type | <Research / Documentation / Implementation / Release> |
| Phase | <PHASE-NN / WP-NN-MM> |
| Responsible role | <Implementer (executes); Reviewer (reviews); QA (evidence) — as applicable> |
| Depends on | <TASK-NNNN, OPEN-NNN answers, or "—"> |
| Authorization required | <None beyond Tier 1 / list of Tier 2 operations, e.g. "read-only access to the game installation folder"> |
| Created | <YYYY-MM-DD> |
| Last updated | <YYYY-MM-DD> |

## 1. Objective
<One verifiable objective.>

## 2. Background
<Why now; related REQ-*, OPEN-*, RISK-*, CONF-* IDs; what earlier tasks found.>

## 3. Required reading
<Ordered list with relative links, following READING_GUIDE sequence B and the task-type list.>

## 4. Allowed scope (files/areas/operations)
- Files: <exact paths this task may create or edit>
- Areas: <modules or critical areas this task holds, e.g. "SPEC-SAVE (held by this task)">
- Operations: <Tier 1 operations; Tier 2 operations only as authorized>

## 5. Forbidden actions and protected contracts
- <Contracts that must not change, e.g. "SPEC-CONTROL invariants; PRD requirement IDs">
- <Operations that remain forbidden, e.g. "no primary saves; no installs; no Git operations">

## 6. Procedure
1. <Step, labeled as read-only discovery, proposed procedure, or verified command where commands appear>

## 7. Validation
<TEST-* IDs and layer, or documentation checks; where results are recorded (RUN-NNNN or DV-NNN).>

## 8. Acceptance criteria
- [ ] <Observable criterion with the evidence that will prove it>

## 9. Outputs (documents to update)
<Documents this task updates, e.g. SOURCES, TOOLCHAIN_MATRIX, register entries, PROJECT_STATUS, CHANGELOG.>

## 10. Recovery / rollback
<How to undo or contain partial work; what to do if the procedure fails midway.>

## 11. Handoff notes
<Short handoff per TPL-HANDOFF when the task stops or ends: documents read, work done, evidence, open issues,
next step, authorization boundaries.>

## 12. Status history
| Date | Status | Event | Recorded by (role) |
|---|---|---|---|
| <YYYY-MM-DD> | Planned | <Task created> | <Coordinator> |
```
