# TASK-0003 — Isolated baseline validation

| Field | Value |
|---|---|
| ID | TASK-0003 |
| Task status | In review |
| Task type | Research (runtime validation) |
| Phase | [PHASE-00](../phases/PHASE-00-audit-and-windows.md) — WP-00-03, using the isolated setup from WP-00-06 |
| Responsible role | Implementer (build and procedure); QA (evidence record); project owner (authorizes each operation; may operate the game); Reviewer (checks the evidence against the PHASE-00 gate) |
| Depends on | [TASK-0001](TASK-0001-windows-environment-audit.md) Done; [TASK-0002](TASK-0002-base-mod-source-audit.md) Done; owner-approved isolated test setup (OPEN-035); explicit owner authorization (next row) |
| Authorization required | Explicit, recorded owner authorization for **each** of: installing toolchain components (including accepting any third-party license terms, which only the owner can do); obtaining the pinned source in an authorized location (OPEN-601); building; installing the build into the isolated mod set; launching the game; enabling mods; creating and using the lab city; probing debugging tools. Each authorization is recorded in the status history with its date and scope. |
| Created | 2026-10-18 |
| Last updated | 2026-10-19 |

> **Update 2026-10-19:** the owner's brief of 2026-10-19 authorized autonomous execution (installs from official
> sources, building, local deployment, test sessions with a disposable city). The baseline ran as
> [RUN-0001](../../quality/evidence/RUN-0001/REPORT.md). The isolated setup actually used: a lab copy of the game's
> built-in benchmark city, test sessions that back up and restore `UserState.coc` and `continue_game.json`, and
> autosave disabled for the session only (`tools\test-session.ps1`). Reload with the mod disabled was not exercised.
>
> *Original note (2026-10-18):* it depended on TASK-0001/0002 and on authorizations that had not been given.

## 1. Objective

Produce the runtime evidence that the PHASE-00 gate requires, using a **disposable lab city and copies only**:

| Item | What this task records |
|---|---|
| [OPEN-005](../RISKS_AND_OPEN_QUESTIONS.md) | Does the pinned, unmodified base mod build reproducibly, load, drive a vehicle, and hand it back cleanly? |
| [OPEN-011](../RISKS_AND_OPEN_QUESTIONS.md) | Observation of takeover and handback: does the released vehicle resume a sensible route, and does traffic keep moving? |
| [OPEN-022](../RISKS_AND_OPEN_QUESTIONS.md) | Initial observation: does a lab save made with the base mod enabled load after the mod is disabled, and what remains? |
| [OPEN-031](../RISKS_AND_OPEN_QUESTIONS.md) | If separately authorized: which debugging capabilities work against the retail game. |
| [OPEN-003](../RISKS_AND_OPEN_QUESTIONS.md), [OPEN-033](../RISKS_AND_OPEN_QUESTIONS.md), [OPEN-001](../RISKS_AND_OPEN_QUESTIONS.md) | Confirmation by build of the target framework; confirmation that the edition loads code mods; the version string shown in game. |

**No conclusion is recorded without evidence.** A negative result (the build fails, the mod crashes, handback leaves a
blocker) is a valid, useful outcome when it is recorded properly.

## 2. Background

- Reported capabilities of the base mod are not proof of behavior in this installation **[Owner requirement]**.
- The historical phase-0 prompt asked one session to discover, build, and run. That work is split across TASK-0001,
  TASK-0002, and this task (CONF-010). Installations wait for this task and need authorization (CONF-006).
- Observations here are about a **third-party** mod. They inform the reuse decision and the PHASE-02 design. They never
  count as validation of Drive the City II requirements.

## 3. Required reading

TASK-0001 and TASK-0002 outputs: [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) (isolated setup),
[TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) (build plan,
observation list), [DEPENDENCIES](../../engineering/DEPENDENCIES.md), [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md),
[REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md). Also [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md),
[DEBUGGING](../../engineering/DEBUGGING.md), [TEST_STRATEGY](../../quality/TEST_STRATEGY.md),
[evidence README](../../quality/evidence/README.md), [TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md),
[SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md) and [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md)
(observation criteria), [TOOLS_AND_PERMISSIONS](../../agents/TOOLS_AND_PERMISSIONS.md), [SECURITY.md](../../../SECURITY.md).

## 4. Allowed scope

- **Operations:** only those the owner authorized, within the recorded scope.
- **Locations:** the authorized source location (outside the OneDrive project root unless the owner decides otherwise;
  OPEN-601, OPEN-028); the build output location; the isolated mod set; the lab city and its copies.
- **Files to write:** a new `RUN-NNNN` evidence record under [docs/quality/evidence/](../../quality/evidence/README.md)
  (sanitized), [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) (limitations list), [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md)
  (commands relabeled as verified with their date), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md),
  [COMPATIBILITY_MATRIX](../../quality/COMPATIBILITY_MATRIX.md) (a third-party baseline entry, if that document's
  conventions allow it; never a support claim for our mod), [FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md),
  the register, [PROJECT_STATUS](../../PROJECT_STATUS.md) (runtime validation), [CHANGELOG.md](../../../CHANGELOG.md),
  the backlog mirror, and this file.

## 5. Forbidden actions and protected contracts

- Using the owner's primary saves. Only the lab city and its copies are used (RISK-028).
- Modifying game installation files, or placing source or build files inside the game installation (RISK-601).
- Enabling the base mod in the owner's normal mod set or playset, or alongside other driving or camera mods.
- Changing the base mod's source behavior. Only minimal build-configuration deviations are allowed, and each is
  recorded with its reason. A functional change means the run is no longer a baseline.
- Changing the base mod's default settings except where the TASK-0002 observation list says so.
- Running scripts from the repository that were not reviewed in TASK-0002.
- Vendoring the source into the tracked project, committing anything, or publishing anything.
- Installing tools not listed in TOOLCHAIN_MATRIX; installing or launching Melty or Motor Town.
- Attaching debuggers that require modifying the game, or leaving the base mod enabled after the session.
- **Protected contract:** evidence is sanitized (no user names, profile paths, account or store IDs) per
  [SECURITY.md](../../../SECURITY.md) and RISK-030.

## 6. Procedure

Every step is a **Proposed procedure (requires validation)**. Placeholders (`<AUTHORIZED_SOURCE_DIR>`,
`<PINNED_COMMIT>`, `<ISOLATED_MOD_SET>`, `<LAB_CITY>`) are filled in from the approved setup and never with guesses.

1. **Preconditions.** Confirm TASK-0001 and TASK-0002 are Done, the isolated setup is approved, and the authorization is
   recorded. Record the game version and the state of the isolated mod set before starting. If the game updates during
   the task, stop and restart from step 1.
2. **Toolchain.** Compare required and installed versions in TOOLCHAIN_MATRIX. For every missing component, ask the
   owner to authorize the installation (the owner performs or approves it and accepts any license terms). After
   installation, record the versions as verified commands with their date.
   - **Scope for the baseline alone (TASK-0002, BASE_MOD_AUDIT §9.13):** the pinned base mod needs a .NET SDK that can
     build SDK-style `net48` projects, the .NET Framework 4.8 reference assemblies, and the game's managed assemblies.
     It does **not** need the Unity Editor, the post-processor, `CSII_*` variables, or Node.js.
   - **On 2026-10-18 (TASK-0001):** the .NET SDK and the reference assemblies are missing, and the official toolchain is
     not installed. Installing the full toolchain is only needed for our own mod (TASK-0004) and for confirming the
     native local-mods folder (OPEN-036).
   - **Inputs from TASK-0001:** game 1.6.2f1 on Steam, while the base mod declares 1.5.*. No mods are installed. There
     is no loader in the game folder.
3. **Obtain the pinned source** in the authorized location per UPSTREAM_STRATEGY, and verify the commit:
   ```powershell
   git clone https://github.com/northernst11-bot/BetaTestDrivingMod '<AUTHORIZED_SOURCE_DIR>'
   git -C '<AUTHORIZED_SOURCE_DIR>' checkout <PINNED_COMMIT>
   git -C '<AUTHORIZED_SOURCE_DIR>' rev-parse HEAD
   ```
   The last command must print the pinned commit.
4. **Build twice from a clean state** using the build plan from TASK-0002. Record the exact command, toolchain versions,
   warnings, errors, the output file list, and file hashes (for example, `Get-FileHash`). Record every build-configuration
   deviation (for example, a reference-path override that keeps files out of the game installation).
5. **Install into the isolated mod set** following the approved setup. Confirm that no other driving or camera mod is
   active in that set, and record the set's mod list.
6. **Launch the lab city** (owner or authorized operator). Record the version shown in game (OPEN-001), whether the mod
   loaded (log lines), and whether code mods load on this edition (OPEN-033).
7. **Drive.** Take over a vehicle using the controls documented upstream for the base mod. Drive a short route with a
   straight, a curve, a junction, and a slope. Observe the camera, and whether driving input triggers building tools or
   moves the building camera (OPEN-014 observation).
8. **Hand back.** Release the vehicle. Observe it and the traffic around it for an observation window declared before
   the run. Record whether it resumes a sensible route, stops, or leaves a queue or invisible blocker (OPEN-011).
9. **Capture evidence:** sanitized screenshots or recordings (kept local, outside version control, until OPEN-407 decides whether game captures may be committed or published) and the relevant log excerpts.
10. **Save, disable, reload.** Save the lab city under a new copy name. Disable the base mod in the isolated set. Reload
    the copy and record whether it loads and what changed (OPEN-022, initial).
11. **Debugging probe (only if separately authorized).** Check which tools from DEBUGGING can attach to the retail
    game without modifying it (OPEN-031).
12. **Second run (if needed).** If any step was Inconclusive, repeat steps 6–10 once and record both runs.
13. **Restore.** Return the isolated mod set to its pre-task state, remove the base mod from it unless the owner wants
    it kept, and confirm with the owner that the normal mod set and primary saves were not touched.
14. **Record the evidence** as `RUN-NNNN` with [TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md): date, game
    version, base-mod commit, sanitized environment, scenario, procedure, expected, observed, a result per step
    (Passed / Failed / Inconclusive), log references, and the Reviewer.
15. **Update the documents** listed in section 9 and write the handoff for WP-00-07 (the PHASE-00 gate report).

## 7. Validation

- QA checks that the RUN record contains every field of the template and that each step has an observed result.
- The Reviewer checks the RUN record against PHASE-00 gate criteria 1–3 and 7, and confirms that every build deviation
  is listed and non-functional.
- Labels: findings move from documented or source-inspected to locally observed only through the linked RUN record.
- The owner confirms that the rollback (step 13) left the normal setup untouched.

## 8. Acceptance criteria

These are the PHASE-00 gate inputs:

1. Two clean builds of the pinned commit succeed with recorded toolchain versions, **or** the build failure is recorded
   with its errors and the attempted deviations (which triggers the PHASE-00 recovery).
2. Game version (as shown in game), edition, sanitized DLC list, toolchain versions, and the base-mod commit are
   recorded.
3. A `RUN-NNNN` record covers load, takeover, drive, handback, traffic observation, disable, and reload, with a result
   for each step.
4. The limitations list in BASE_MOD_AUDIT is updated from observed behavior.
5. OPEN-005 is answered; OPEN-011, OPEN-022, OPEN-031, OPEN-003, and OPEN-033 are updated with what was observed.
6. Rollback is complete and confirmed by the owner. No primary save, game installation file, or normal mod set was
   altered.

Task Done does not mean PHASE-00 Passed. The gate also needs WP-00-07, a Reviewer check, and owner acceptance.

## 9. Outputs

`RUN-NNNN` evidence record · [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) · [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md)
· [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md) · [COMPATIBILITY_MATRIX](../../quality/COMPATIBILITY_MATRIX.md)
(third-party baseline entry, if applicable) · [FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md) ·
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md) · [PROJECT_STATUS](../../PROJECT_STATUS.md) ·
[CHANGELOG.md](../../../CHANGELOG.md) · this file.

## 10. Recovery / rollback

- **Before step 5:** record the isolated mod set's contents and keep a backup copy of the lab city save. Primary saves
  are never in scope, so they need no backup from this task.
- **The game fails to start or crashes:** remove or disable the base mod in the isolated set following the approved
  setup, restart, and record the failure as a result.
- **The lab city becomes corrupted:** discard it and restore the backup copy. It is disposable by definition.
- **Anything outside the approved scope seems affected** (normal mod set, primary saves, game files): stop immediately,
  inform the owner, do not attempt a silent repair, and record the incident.
- **Installed toolchain components:** they stay installed unless the owner asks for removal; record either way.
- **Source copy and build outputs:** removed or kept according to the owner's decision. They never enter tracked
  project folders.

## 11. Handoff notes

The handoff (to be written at completion) goes to the Coordinator for WP-00-07. It must state the build result, the
runtime result per step, the limitations found, a draft reuse recommendation (reuse, partial reuse, or
re-implementation of vehicle control), what was restored, which questions remain open, and the documents actually read.

## 12. Status history

| Date | Event |
|---|---|
| 2026-10-18 | Created by the documentation bootstrap (TASK-0000); not started. |
| 2026-10-19 | Authorized by the owner brief of 2026-10-19 (execution authority). Executed as RUN-0001: two clean builds, load, co-load guard, takeover, drive, handback on 1.6.2f1 with real input; baseline removed. Moved to **In review** (no independent reviewer). |
