# Handoff Template

| Field | Value |
|---|---|
| ID | TPL-HANDOFF |
| Document status | Draft |
| Used for | Task handoffs (in the task file's "Handoff notes") and phase-level handoffs (`docs/agents/HANDOFF_TO_<TARGET>.md`) |
| Responsible role | Coordinator (maintainer); every role writes handoffs |
| Related documents | [WORKFLOW_AND_HANDOFF](../agents/WORKFLOW_AND_HANDOFF.md), [READING_GUIDE](../agents/READING_GUIDE.md), [TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md), [HANDOFF_TO_PHASE_00](../agents/HANDOFF_TO_PHASE_00.md) |
| Last updated | 2026-10-18 |

## How to use

- Write a handoff at the end of every task, whenever work stops as Blocked, at the end of a session that leaves work
  unfinished, and at each role change between people or sessions.
- Record only real events: documents actually opened in this session, runs that exist, decisions the owner actually
  made.
- **A handoff is a record, not an authorization.** Permissions come only from the project owner, recorded in the task
  file.

## Template

```markdown
# Handoff — <TASK-NNNN or PHASE-NN> — <YYYY-MM-DD>

| Field | Value |
|---|---|
| Date | <YYYY-MM-DD> |
| From (role) | <Coordinator / Implementer / Reviewer / QA; note role switches, e.g. "Implementer → QA (same session)"> |
| To (role) | <next role, or "Project owner (decision needed)"> |
| Task | <TASK-NNNN — title, with relative link> |
| Task status at handoff | <Planned / Ready / In progress / Blocked / In review / Done> |

## Documents read
| Document | Last updated (as seen) | Depth |
|---|---|---|
| <relative link> | <YYYY-MM-DD> | <Full / Sections N–M / Skim> |

## Work done
- <What changed, file by file; operations performed and under which authorization>

## Evidence
- <RUN-NNNN references with result, or "None — Not run"; DV-NNN for documentation checks>

## Open issues
- <Blockers; OPEN-*, RISK-*, CONF-*, ISSUE-* entries touched or proposed (new entries in full for the Coordinator)>
- <Decisions needed from the project owner>

## Next step
<The single next action, who should take it, and its required reading.>

## Authorization boundaries
- May do: <Tier 1 operations and any task-scoped grants that remain valid>
- May not do: <operations explicitly out of bounds for the next session>
- Needs owner authorization: <Tier 2 operations the next step will require>
```
