# TASK-0004 — Native mod skeleton (first implementation task)

| Field | Value |
|---|---|
| ID | TASK-0004 |
| Task status | In review (implemented and run in game on 2026-10-19; awaiting owner review) |
| Task type | Implementation |
| Phase | [PHASE-01](../phases/PHASE-01-foundation-and-save-safety.md): WP-01-02, WP-01-03, WP-01-04, and the activation and unload part of WP-01-05 |
| Responsible role | Implementer (builds); Reviewer (scoped to control authority and entity lifecycle for the state skeleton and registry); QA (evidence runs); project owner (authorizations and decisions) |
| Depends on | [TASK-0003](TASK-0003-isolated-baseline-validation.md) evidence that the native route builds and loads on the installed game; owner decisions in WP-01-01 (OPEN-027 license, OPEN-028 output location, OPEN-029 Git, OPEN-604 identifiers); acceptance of [ADR-0005](../../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md) |
| Authorization required | Creating `src/` and `tests/`; Git initialization (if decided); installing the official toolchain components still missing (Unity Editor, .NET SDK) if TASK-0003 did not already do it; building; deploying to the native local-mods location; launching the game with a **lab city** |
| Created | 2026-10-18 |
| Last updated | 2026-10-19 |

> **Update 2026-10-19 (owner brief of 2026-10-19 granted execution authority):** the readiness blockers were resolved
> by owner decisions in that brief (local Git allowed, native route approved: [ADR-0005](../../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)
> Accepted, user-scoped installs allowed, output under `%LOCALAPPDATA%\DriveTheCityII`). The skeleton was built and
> then extended far beyond this task's scope in [TASK-0005](TASK-0005-implementation-run-2026-10-19.md).
> Evidence for this task's own objectives: the mod loads natively on 1.6.2f1 and logs its version and build stamp;
> bounded log `DriveTheCityII.log`; co-load guard ([RUN-0001](../../quality/evidence/RUN-0001/REPORT.md)); activation and
> unload through the control state machine (TR-01..TR-13 unit-tested; transition cycles 33/33 in game).
> The original text below is kept as the plan of record.

## 1. Objective

Create the smallest native CS2 code mod that belongs to Drive the City II and proves the foundation that every later
phase builds on. It contains **no gameplay**. The mod must:

- load through the game's native mod system on the installed version (1.6.2f1 when last observed);
- identify itself in a bounded log;
- expose settings with a **safe-disable** switch;
- refuse to run its controllers when the original Beta Test Driving Mod is active (the co-load guard);
- register, then cleanly unregister, its systems on load and unload;
- leave a lab city unchanged.

## 2. Background

- Loading route: native code mod built with the official toolchain (ADR-0005, Proposed).
- Architecture: module skeleton per [SPEC-MODULES](../../architecture/MODULE_CONTRACTS.md) (CityBridge, Player,
  WorldContact, Vehicles, Career, Persistence, UI, Diagnostics). Only empty contracts exist in this task; the Career
  logic-test harness is created (OPEN-501).
- The player-state skeleton follows [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md) with the `Building` state only.
  The other states are declared but not enterable.
- The candidate base mod's entry-point and registration pattern is a **reference** only; no code is copied before
  the license decision (OPEN-027) and a [REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md) approval.

## 3. Required reading

[AGENTS.md](../../../AGENTS.md), [PROJECT_STATUS](../../PROJECT_STATUS.md), [PHASE-01](../phases/PHASE-01-foundation-and-save-safety.md),
[SPEC-MODULES](../../architecture/MODULE_CONTRACTS.md), [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md),
[SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md), [SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md),
[ADR-0005](../../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md), [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md),
[CODING_STANDARDS](../../engineering/CODING_STANDARDS.md), [DEBUGGING](../../engineering/DEBUGGING.md),
[TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md),
[COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md), [TEST_STRATEGY](../../quality/TEST_STRATEGY.md).

## 4. Allowed scope (files, areas, operations)

- **Create:** `src/` (mod project and module skeleton) and `tests/` (Career logic-test project). Authorized updates go
  to [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md),
  [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md), and the evidence records.
- **Operations, each only after recorded authorization:** build with the official toolchain; deploy to the native
  local-mods location (OPEN-036); launch the game into a **lab city**; disable and remove the deployed folder.

## 5. Forbidden actions and protected contracts

- No gameplay: no avatar, no driving, and no entity spawning or borrowing.
- No third-party loaders and no changes in the game installation folder (ADR-0005).
- No copied upstream code before the license decision. No primary saves.
- Save-format work belongs to WP-01-06 and is a separate task (critical area). This task persists nothing in saves.

## 6. Procedure (Proposed procedure, requires validation)

1. Confirm the prerequisites in the metadata. Record the authorizations in §12.
2. Create the mod project from the official template, using the identifiers decided in OPEN-604. Record the template
   version and the target framework that the toolchain sets (closes OPEN-003 in practice).
3. Add the module folders and empty contracts, with dependency checks that keep Career free of host types
   (RISK-104).
4. Implement Diagnostics: bounded log with version stamp, sanitized export (REQ-DIAG-001).
5. Implement Settings with safe-disable (REQ-DIAG-002), using the game's settings mechanism per the official docs.
6. Implement the co-load guard against the original base mod (REQ-COMPAT-001). Record the detection method.
7. Implement the `Building`-only state skeleton and an empty entity registry. Prove registration and unregistration on
   load and unload (REQ-ENTITY-003 baseline).
8. Build, deploy, and run in the lab city. Then disable and reload, and remove and reload (first pass of OPEN-022).
9. Record `RUN-NNNN` evidence and run the Career logic tests.

## 7. Validation

TEST-DIAG-001, TEST-DIAG-002, TEST-COMPAT-001 (co-load part), TEST-RECOV-002 (a forced fault degrades to `Building`),
TEST-CITY-004 (before/after comparison of the lab city), TEST-ENTITY-003 (registry empty after unload). All are
**Not run**.

## 8. Acceptance criteria

1. The mod loads on the installed game version through the native route, and its log shows the version stamp.
2. Safe-disable stops all mod systems without errors; re-enabling works.
3. With the original base mod active, our controllers refuse to start and say so in the log and in the UI.
4. The lab city shows no unintended persistent change after load, use, disable, and removal.
5. The Career test project runs without CS2 assemblies.
6. A Reviewer has checked the evidence, and the owner has accepted it.

## 9. Outputs

The mod skeleton in `src/`, the logic tests in `tests/`, `RUN-NNNN` records, and updates to TOOLCHAIN_MATRIX,
BUILD_AND_RUN, COMPATIBILITY_POLICY, PROJECT_STATUS, and CHANGELOG.

## 10. Recovery / rollback

Remove the deployed folder from the local-mods location; the lab city is disposable. With Git, revert the branch.
Without Git (OPEN-029), the owner keeps a copy of the project folder before the task starts.

## 11. Handoff notes

To be written at completion: evidence links, the confirmed toolchain versions, the local-mods folder used (OPEN-036),
and whether the native route behaved as ADR-0005 expects.

## 12. Status history

| Date | Status | Event | Recorded by (role) |
|---|---|---|---|
| 2026-10-18 | Planned | Created during the TASK-0001/TASK-0002 audit session as the concrete next implementation task; not started. | Coordinator |
