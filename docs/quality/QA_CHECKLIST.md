# QA Checklist

| Field | Value |
|---|---|
| ID | DOC-QA-CHECKLIST |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-00 – PHASE-13 |
| Responsible role | QA (maintainer); Reviewer uses it at review; project owner uses the phase-gate lists at gate acceptance |
| Related requirements | All `REQ-*` in the [PRD](../product/PRD.md) |
| Related documents | [TEST_STRATEGY](TEST_STRATEGY.md), [TEST_SCENARIOS](TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md), [ROADMAP](../../ROADMAP.md), [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md), [WORKFLOW_AND_HANDOFF](../agents/WORKFLOW_AND_HANDOFF.md), [evidence](evidence/README.md) |
| Last updated | 2026-10-18 |

> **Nothing on this page is checked.** No phase gate has been reviewed and no runtime check has been performed. The
> boxes are templates.

## 1. How to use

- Copy the relevant list into the task's handoff, the review notes, or the run record, and tick items there. Do not
  tick boxes in this file.
- A ticked item needs a reference: a document, a `RUN-NNNN`, or an `ISSUE-NNN`.
- "Not applicable" is allowed with a one-line reason.
- The phase-gate lists summarize the gates in the [ROADMAP](../../ROADMAP.md). The detailed gate is in each
  [phase document](../planning/phases/README.md); if they differ, the phase document and the ROADMAP win and this list
  is corrected.

## 2. Checklists per change type

### 2.1 Documentation change

- [ ] Written in English; product names and the language name PT-BR are the only exceptions.
- [ ] Metadata block present and `Last updated` set to the actual date.
- [ ] The single source of truth was edited, not a duplicate ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §3).
- [ ] Relative links point to existing files; no links to heading anchors in other files.
- [ ] IDs are used, not invented: no new `REQ-*` outside the PRD; new `OPEN-*`/`RISK-*` entries are in the register.
- [ ] Claim labels are accurate; **[Built locally]** and **[Tested in-game]** appear only with a cited run.
- [ ] No invented versions, commands, UI elements, paths, benchmark values, runs, or approvals.
- [ ] Every unknown is an `OPEN-*` entry with impact, verification method, and resolving phase or task.
- [ ] No personal data.
- [ ] [CHANGELOG](../../CHANGELOG.md) and, if status changed, [PROJECT_STATUS](../PROJECT_STATUS.md) updated.

### 2.2 Logic change (Career, economy, mission, or save-format logic)

- [ ] The change is traced to `REQ-*` IDs and the governing spec section.
- [ ] Logic-layer tests exist or were updated for the changed rule, including duplicate, replay, and save/load cases
      (for example TEST-CAREER-101, TEST-CAREER-103, TEST-ECON-101).
- [ ] The logic suite passes in a recorded run. Compiling alone is not passing.
- [ ] Ledger invariants still hold: unique TransactionIds, append-only ledger, wallet equals ledger sum.
- [ ] If the persisted shape changed: the schema version is bumped, a migration exists, and TEST-SAVE-002 and
      TEST-SAVE-106 are updated (this also makes it a critical-area change, §2.4).
- [ ] The matching Integration or In-game test is scheduled, because test doubles can diverge from the game (RISK-503).
- [ ] Balancing values remain labeled **[Proposal]** unless measured.

### 2.3 Runtime change outside the critical areas (gameplay, UI, presentation)

- [ ] Tests for the affected requirements were run in the lab and recorded as `RUN-NNNN`.
- [ ] The smoke suite ([TEST_STRATEGY](TEST_STRATEGY.md) §11) still passes.
- [ ] Player-facing text is externalized in English and PT-BR (TEST-L10N-001).
- [ ] Placeholders are labeled (TEST-ART-002); new assets have provenance entries.
- [ ] Performance-sensitive changes were measured with the [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md).
- [ ] Observed defects are filed in [KNOWN_ISSUES](KNOWN_ISSUES.md).

### 2.4 Critical-area change (control authority, entity lifecycle, save format)

- [ ] No other active task edits the same critical specification or module ([AGENTS.md](../../AGENTS.md), RISK-014).
- [ ] The governing spec ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md),
      or [SPEC-SAVE](../architecture/SAVE_FORMAT.md)) was updated through a reviewed change, with
      [TRACEABILITY](../planning/TRACEABILITY.md) updated.
- [ ] The critical-area suite (all supported TEST-CTRL, TEST-TRANS, TEST-ENTITY, and TEST-SAVE tests) was run and
      recorded.
- [ ] Save changes: tested only on lab copies; invariants I1–I6 checked ([SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md) §3).
- [ ] Control changes: the state trace shows a single movement authority and a single camera owner throughout.
- [ ] Entity changes: registry counts return to baseline; no orphans after mission end, mode change, save, unload, and
      disable.
- [ ] Reviewed by a Reviewer who did not implement the change. A self-review is recorded as self-review.
- [ ] No migration path, quarantine, or last-valid-copy protection was weakened.

### 2.5 Release candidate

- [ ] Every `Must` requirement has a Passed run, or an owner-approved scope decision in the register ([PRD](../product/PRD.md) §9).
- [ ] Zero open Critical issues in [KNOWN_ISSUES](KNOWN_ISSUES.md).
- [ ] TEST-REL-001 – 004 and TEST-REL-101 passed on the clean release environment.
- [ ] TEST-SAVE-006 re-run on the release candidate; the clean-exit procedure matches the evidence.
- [ ] License audit, asset provenance, and third-party notices complete (TEST-REL-003).
- [ ] The compatibility declaration lists only combinations with Passed rows in
      [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md).
- [ ] An independent tester completed the intended experience without developer help.
- [ ] Release notes and user guides contain no untested claims.
- [ ] The project owner approved the release.

### 2.6 Before every test run

- [ ] The test is defined, and its preconditions are met.
- [ ] Owner authorization for the runtime operations is recorded.
- [ ] Fresh working copy of the named lab city master; no primary save involved.
- [ ] Game version, edition, DLCs, mod build, and mod set recorded; game version will stay constant during the run.
- [ ] Diagnostics logging and recording ready; `RUN-NNNN` assigned.
- [ ] After the run: results recorded per test, artifacts hashed, sanitization checklist completed, issues filed,
      Reviewer named.

## 3. Phase-gate checklists

Each list summarizes the ROADMAP exit gate for one phase. Every phase also requires: evidence recorded under
[evidence](evidence/README.md), review by a non-implementer Reviewer, and acceptance by the project owner
([ROADMAP](../../ROADMAP.md) §6).

### PHASE-00 — Audit and Windows environment

- [ ] Windows, installed game, and toolchain discovery recorded (TASK-0001; OPEN-001, OPEN-033 answered).
- [ ] Candidate source, license, and dependency audit complete with a pinned revision (TASK-0002).
- [ ] Isolated test setup approved by the project owner (OPEN-035).
- [ ] Reproducible base-mod build recorded (build evidence, not a runtime pass).
- [ ] Runtime baseline recorded, including vehicle control and handback observation (TEST-COMPAT-101).
- [ ] List of limitations recorded; [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md) holds confirmed versions only.
- [ ] Confirmed: documentation alone did not pass this phase.

### PHASE-01 — Foundation and save safety

- [ ] Independent mod identity; no controller conflict with the original base mod (TEST-COMPAT-102).
- [ ] Install, open, save, close, reopen, and disable with no unwanted city change (TEST-CITY-004, TEST-SAVE-006).
- [ ] Invalid data never overwrites the last valid copy (TEST-SAVE-004, TEST-SAVE-104, TEST-SAVE-102, TEST-SAVE-111).
- [ ] Save identity and no leakage (TEST-SAVE-001, TEST-SAVE-101, TEST-SAVE-107); schema and references
      (TEST-SAVE-002, TEST-SAVE-003).
- [ ] ADR-0003 decided with evidence from the persistence spike (OPEN-006).
- [ ] Bounded logs and safe-disable (TEST-DIAG-001, TEST-DIAG-002); error containment (TEST-RECOV-002).

### PHASE-02 — Basic driving

- [ ] Validation route with curves, junctions, and slopes driven (TEST-DRIVE-001, TEST-DRIVE-101).
- [ ] Single movement authority (TEST-CTRL-001); input and camera separation (TEST-CTRL-003).
- [ ] Handed-back vehicle rejoins native traffic with no stale route or invisible blocker (TEST-ENTITY-001, TEST-ENTITY-102).
- [ ] Career mode uses only career vehicles (TEST-ENTITY-002, first part).

### PHASE-03 — Surfaces, collisions, and physics

- [ ] No passing through buildings (TEST-WORLD-002); no level switching on bridges or in tunnels (TEST-WORLD-001).
- [ ] Collision geometry bounded and cleaned up, if used (TEST-WORLD-003, TEST-PERF-105).
- [ ] One documented contact rule with native agents (TEST-WORLD-004).
- [ ] Predictable behavior after pause, focus loss, and frame-rate swings (TEST-TIME-001 – 003, TEST-DRIVE-002).
- [ ] ADR-0004 decided with evidence.
- [ ] First performance baseline recorded (TEST-PERF-101 – 103, TEST-PERF-106).

### PHASE-04 — On-foot character

- [ ] Representative urban circuit completed with no clipping, no falling, and no permanent loss of control
      (TEST-PLAYER-101, TEST-PLAYER-001 – 004).
- [ ] `Recovery` works for the avatar (TEST-RECOV-001).
- [ ] Avatar representation decided (OPEN-010); animations or documented substitutes in place; placeholders labeled
      (TEST-ART-001, TEST-ART-002).

### PHASE-05 — Walk–drive integration

- [ ] **[Proposal]** 50 successful transition cycles across the scenario matrix (TEST-TRANS-101).
- [ ] No duplication, camera loss, or traffic blocking (TEST-TRANS-001 – 004, TEST-TRANS-102, TEST-TRANS-103).
- [ ] Vehicle removal during transitions handled (TEST-TRANS-003); return to `Building` from every stable state
      (TEST-CTRL-004).
- [ ] No orphan entities (TEST-ENTITY-003); blocked-exit feedback (TEST-UI-003, first part).

### PHASE-06 — Taxi career MVP

- [ ] **[Proposal]** 20 rides in different districts, including a cancellation and a save/load during a mission, each
      paid exactly once (TEST-TAXI-101).
- [ ] Full loop including save, restart, and reload (TEST-TAXI-001); career passengers (TEST-TAXI-002).
- [ ] Shared lifecycle and exactly-once settlement (TEST-CAREER-001 – 005, TEST-CAREER-101 – 103); mid-mission
      save/load (TEST-SAVE-005).
- [ ] Minimal wallet, ledger, and rental (TEST-ECON-001, TEST-ECON-002, TEST-GARAGE-001).
- [ ] M2 fun checkpoint judged by the project owner (TEST-TAXI-102).

### PHASE-07 — Progression and garage

- [ ] A new profile can rent, work, buy, and maintain a vehicle (TEST-GARAGE-001 – 005).
- [ ] Every transaction persists (TEST-ECON-002, TEST-ECON-003).
- [ ] No softlock (TEST-ECON-004, TEST-ECON-102, TEST-GARAGE-006, TEST-GARAGE-101).

### PHASE-08 — Deliveries, freight, and bus

- [ ] Each career has reachable contracts, a tutorial, completion, failure, reward, and persistence (TEST-DELIV-001,
      TEST-FREIGHT-001, TEST-BUS-001, TEST-ONBOARD-102, TEST-SAVE-110).
- [ ] City edits lead to replanning or safe cancellation (TEST-CAREER-006, TEST-BUS-002).
- [ ] Vehicle classes distinct (TEST-DRIVE-003).

### PHASE-09 — Home, services, and company

- [ ] From independent driver to a small company with a second vehicle and one hire (TEST-COMPANY-001).
- [ ] Reload with no financial inconsistency (TEST-COMPANY-003, TEST-TIME-004).
- [ ] No implied physical fleet (TEST-COMPANY-002).
- [ ] Home base and service points work and fall back safely (TEST-HOME-001 – 003).

### PHASE-10 — Challenges and dynamic city

- [ ] Editing the city during a career corrupts neither profile nor save (TEST-CITY-002, TEST-CITY-101, TEST-CITY-102,
      TEST-HOME-101).
- [ ] No mission stays impossible indefinitely (TEST-CAREER-006, TEST-CAREER-104).
- [ ] Challenges and city-informed jobs (TEST-CHAL-001 – 003).
- [ ] Native economy writes stay a gated, off-by-default experiment (TEST-CITY-001, TEST-CITY-003).

### PHASE-11 — Presentation and accessibility

- [ ] A new player completes their first work without console or developer help (TEST-ONBOARD-001).
- [ ] HUD, menus, feedback, and route view (TEST-UI-001 – 004).
- [ ] Remapping, gamepad, and accessibility options (TEST-INPUT-001 – 003, TEST-INPUT-101).
- [ ] English and PT-BR complete (TEST-L10N-001, TEST-L10N-101); audio and art provenance (TEST-AUDIO-001, TEST-ART-001).
- [ ] Economic tutorial (TEST-ECON-005); optional visual enhancements measured (TEST-PERF-002, TEST-PERF-108).

### PHASE-12 — Beta and validation

- [ ] **[Proposal]** 30 save/load cycles (TEST-SAVE-109) and sessions of at least two hours (TEST-SAVE-114,
      TEST-PERF-107).
- [ ] Representative cities, hardware, DLCs, and supported mods covered in [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md)
      (TEST-COMPAT-001).
- [ ] Migration from every prior schema (TEST-SAVE-106); performance compared with the baseline (TEST-PERF-001).
- [ ] No known critical save corruption, duplicated rewards, permanent loss of control, or uncompletable mandatory
      content (zero open Critical issues).

### PHASE-13 — Release and maintenance

- [ ] Release-candidate checklist (§2.5) complete.
- [ ] Install, update, and clean exit tested (TEST-REL-001, TEST-REL-002, TEST-SAVE-006).
- [ ] Package and license audit complete (TEST-REL-003, TEST-REL-101); runtime needs only CS2 and documented
      dependencies (TEST-REL-004).
- [ ] An independent tester installed the mod on a supported configuration and completed the intended experience.
- [ ] Maintenance procedure in place ([POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md)).
