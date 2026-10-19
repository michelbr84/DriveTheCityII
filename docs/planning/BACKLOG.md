# Backlog (Task Index)

| Field | Value |
|---|---|
| ID | DOC-BACKLOG |
| Document status | Draft |
| Scope | Index of all task files |
| Responsible role | Coordinator (maintainer) |
| Related documents | [tasks/README](tasks/README.md), [ROADMAP](../../ROADMAP.md), [phase documents](phases/README.md), [PROJECT_STATUS](../PROJECT_STATUS.md) |
| Last updated | 2026-10-18 |

This file is an **index**, not a second tracker. Each task file is the single source of truth for its own status. If
this index and a task file disagree, the task file wins, and this index must be corrected in the same change.

If the project later adopts an issue tracker, the project owner chooses one official registry and the other is
retired. There must never be two manually maintained backlogs.

## 1. Task index

| Task | Title | Type | Phase / WP | Task status | Depends on | Authorization needed |
|---|---|---|---|---|---|---|
| [TASK-0000](tasks/TASK-0000-documentation-bootstrap.md) | Documentation bootstrap | Documentation | Pre-PHASE-00 | In review | — | None beyond the owner brief of 2026-10-18 |
| [TASK-0001](tasks/TASK-0001-windows-environment-audit.md) | Windows environment audit | Research | PHASE-00 / WP-00-01 | In review | — | Owner go-ahead for read-only discovery outside the project root |
| [TASK-0002](tasks/TASK-0002-base-mod-source-audit.md) | Base mod source, license, and dependency audit | Research | PHASE-00 / WP-00-02 | In review | — (benefits from TASK-0001) | None beyond read-only web access (no cloning into the project) |
| [TASK-0003](tasks/TASK-0003-isolated-baseline-validation.md) | Isolated baseline validation | Research (runtime validation) | PHASE-00 / WP-00-03 | Planned | TASK-0001, TASK-0002 | Explicit owner authorization for any install, build, game launch, or mod enablement |
| [TASK-0004](tasks/TASK-0004-native-mod-skeleton.md) | Native mod skeleton (first implementation task) | Implementation | PHASE-01 / WP-01-02 to WP-01-05 | Planned | TASK-0003; WP-01-01 owner decisions; ADR-0005 accepted | Creating `src/` and `tests/`, toolchain installs, build, deployment, lab-city launch |

Status as recorded on 2026-10-18. Statuses change only in the task files.

## 2. Work not yet broken into task files

Later work lives as work packages (`WP-NN-MM`) inside the [phase documents](phases/README.md). A task file is created
only when a work package is about to start and meets the [Definition of Ready](../agents/DEFINITION_OF_READY.md).
Known candidates for upcoming task files:

| Candidate | Source | Note |
|---|---|---|
| Melty documentation review and owner-attended evaluation | PHASE-00, WP-00-04 | Optional; never blocks the project ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md), OPEN-016). |
| Motor Town loop study (observation only) | PHASE-00, WP-00-05 | Optional; first needs the owner's decision on OPEN-030. |
| Isolated test setup definition | PHASE-00, WP-00-06 | Feeds TASK-0003 (OPEN-035). |
| PHASE-00 gate report | PHASE-00, WP-00-07 | Only after TASK-0001 to TASK-0003 have evidence. |
| Owner decisions: project license, Git initialization, OneDrive layout | OPEN-027, OPEN-028, OPEN-029 | Decisions, not engineering tasks. Record the outcome in the register and in PROJECT_STATUS. |

## 3. How to add a task

1. Copy [TASK_TEMPLATE](../templates/TASK_TEMPLATE.md) to `tasks/TASK-NNNN-short-name.md`, using the next free
   number.
2. Fill in every section, then check it against the [Definition of Ready](../agents/DEFINITION_OF_READY.md) for its
   task type.
3. Add one row to section 1 of this index.
4. Reference the task from its phase work package.
