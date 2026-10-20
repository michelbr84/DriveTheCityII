# Compatibility Matrix

| Field | Value |
|---|---|
| ID | DOC-COMPAT-MATRIX |
| Document status | Draft |
| Release scope | v1.0 and every later release |
| Phases | PHASE-00, PHASE-01, PHASE-12, PHASE-13, and after every game update |
| Responsible role | QA (maintainer: adds rows only from evidence runs); Reviewer checks each row against its run; project owner approves the published compatibility declaration |
| Related requirements | REQ-COMPAT-001, REQ-REL-001, REQ-REL-004, REQ-SAVE-006 |
| Related documents | [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md), [TEST_STRATEGY](TEST_STRATEGY.md), [TEST_SCENARIOS](TEST_SCENARIOS.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md), [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md), [evidence](evidence/README.md) |
| Last updated | 2026-10-19 |

## 1. Purpose

This matrix is the **official record of tested combinations** of game version, platform or store edition, DLCs, mod
version, and other mods. It records only what was actually run, with evidence. What the project *supports*, and how
support levels are defined, is decided in [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md), which must cite
rows of this matrix. Development-tool versions belong in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md), not
here.

## 2. Current state

> **No combination has been tested.** As of 2026-10-18 nothing has been built or run, the installed game version and
> edition are not yet known (OPEN-001, OPEN-033), and no evidence run exists. The tested-combinations table below is
> intentionally empty. No compatibility with any game version, DLC, or mod may be claimed.

Version claims made by third parties are not entries in this matrix. For example, a game version declared in the
candidate base mod's publishing metadata or a version cited by a community reference is a **[Documented upstream]**
claim recorded in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) and [SOURCES](../research/SOURCES.md). It becomes a
row here only after our own run.

## 3. Tested combinations

| Game version | Platform / store edition | DLCs | Mod version / commit | Other mods (name and version) | Tests run (scope) | Result | Evidence | Date | Notes |
|---|---|---|---|---|---|---|---|---|---|
| 1.6.2f1 (Steam build 25127643) | Steam, PC; edition not recorded | Not recorded | Beta Test Driving Mod 0.3.54 at `eefd6bb` | Drive the City II (inert by co-load guard) | Load, takeover, drive, handback, co-load guard (TEST-COMPAT-101/102 partial) | Passed (reload not run) | [RUN-0001](evidence/RUN-0001/REPORT.md) | 2026-10-19 | Base mod declares 1.5.\*; works for the steps run. Both mods default to F8. |
| 1.6.2f1 (Steam build 25127643) | Steam, PC; edition not recorded | Not recorded | Drive the City II 0.1.0 development builds `dfcfea1`…`08ceb62` | None | Mod load, lab city save/load, character mode, driving, save while driving, traffic queueing, transition cycles (33 in total), performance | Passed for the scope run | [WORKLOG](evidence/WORKLOG.md), sessions under `%LOCALAPPDATA%\DriveTheCityII\test\sessions\` | 2026-10-19 | Development runs; the release-candidate run will be recorded separately. |
| 1.6.2f1 (Steam build 25127643) | Steam, PC; edition not recorded | Not recorded | Drive the City II 0.1.0 development builds `46e8b9d`…`29ec438` | None | M1 loop with real keys (RUN-0002), careers (RUN-0003), presentation and mod-less load (RUN-0004) | Passed for the scope run; freight, bus and challenge completion not observed | RUN-0002, RUN-0003, RUN-0004 | 2026-10-19 | Simulated driving labelled in each record |
| 1.6.2f1 (Steam build 25127643) | Steam, PC; edition not recorded | Not recorded | Drive the City II **0.1.0-3eaf045** (final private package) | None | M1 loop with real keys 17/17 (RUN-0002); real-time 2-hour soak (RUN-0004 §3) | See RUN-0004 §3 | RUN-0004 | 2026-10-19 | Private package; not published |
| 1.6.2f1 (Steam build 25127643) | Steam, PC; edition not recorded | Not recorded | Drive the City II **0.1.0-36ec810** (private package of 2026-10-20) | None | Traffic fixes switched off/on in game; career persistence 11/11; save/reload with a parked vehicle; M1 with real keys not run on this package | Passed for the scope run | RUN-0005 | 2026-10-20 | Private package; not published. The owner also played `0.1.0-af308eb` in their own city (no errors in the mod log) |

**Column rules:**

- **Game version:** exactly as the game reports it (how to read it reliably: OPEN-508). Never a range such as "1.x".
- **Platform / store edition:** as observed in TASK-0001 (OPEN-033), sanitized (no account or store IDs).
- **DLCs:** the installed and enabled DLC set at run time, or "none".
- **Mod version / commit:** our mod's version and commit. For the PHASE-00 baseline, the candidate base mod's pinned
  upstream revision.
- **Other mods:** every other enabled mod with its exact version, or "none (isolated mod set)".
- **Tests run (scope):** the TEST IDs executed. A row covers only those tests, not the whole mod.
- **Result:** Passed / Failed / Inconclusive for that scope. Failed rows stay in the table: an observed
  incompatibility is useful information.
- **Evidence:** the `RUN-NNNN` record(s). A row without a run is not allowed.
- **Date:** the date of the run.

**Row rules:**

1. A row is added only by QA from a reviewed run. It is never copied from another version.
2. A new game version, DLC set, or mod version needs a new row. Results are never carried over (see the regression
   policy in [TEST_STRATEGY](TEST_STRATEGY.md) §11).
3. Rows are never edited after review except to add a correction note with its date. A re-test creates a new row.

## 4. Planned combinations (candidates, not results)

These are the combinations the project intends to test. They are a plan only. Every value marked "to be recorded"
comes from future discovery.

| # | Purpose | Game version | Platform / edition | DLCs | Mod under test | Other mods | Tests | Phase | Status |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Candidate base-mod runtime baseline | Installed version, to be recorded (OPEN-001) | Owner's edition, to be recorded (OPEN-033) | Installed set, to be recorded | Beta Test Driving Mod at the revision pinned in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) | None | TEST-COMPAT-101 | PHASE-00 | Planned (needs TASK-0002 and owner authorization for TASK-0003) |
| 2 | Vanilla performance reference | Same as #1 | Same as #1 | Same as #1 | None | None | TEST-PERF-101 | PHASE-01 or PHASE-03 | Planned |
| 3 | Our mod alone (foundation) | Same as #1 | Same as #1 | Same as #1 | Drive the City II, first foundation build | None | TEST-CITY-004, TEST-SAVE-001 – 006, TEST-DIAG-001/002, TEST-RECOV-002 | PHASE-01 | Planned |
| 4 | Co-loading with the original base mod | Same as #1 | Same as #1 | Same as #1 | Drive the City II | Beta Test Driving Mod (pinned revision) | TEST-COMPAT-102 | PHASE-01 | Planned |
| 5 | DLC variation | Version current at PHASE-12 | Same as #1 | With and without each installed DLC, only if the game allows enabling and disabling DLCs (**[Unverified]**) | Drive the City II beta | None | Smoke suite | PHASE-12 | Planned |
| 6 | Commonly used driving, camera, traffic, and UI mods | Version current at PHASE-12 | Same as #1 | Installed set | Drive the City II beta | Candidates identified under OPEN-023 | Smoke suite, TEST-COMPAT-001 | PHASE-12 | Planned |
| 7 | Representative hardware | Version current at PHASE-12 | Editions available to testers | Installed sets | Drive the City II beta | None | Smoke suite, TEST-PERF-103/104 | PHASE-12 | Planned (testers and machines: OPEN-507) |
| 8 | After a game update | Each new version | Same as #1 | Installed set | Current Drive the City II build | None | TEST-COMPAT-103 | Every game update | Planned |
| 9 | Release candidate | Versions to be declared in [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) | Declared editions | Declared sets | Drive the City II release candidate | Documented dependencies only | TEST-REL-001 – 004, TEST-SAVE-006 | PHASE-13 | Planned |

## 5. How this matrix is used

- **PHASE-01 gate:** rows #3 and #4 exist with evidence.
- **PHASE-12 gate:** rows cover the representative cities, hardware, DLCs, and supported mods chosen for the beta.
- **PHASE-13 gate:** the compatibility declaration in [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) lists
  only combinations with Passed rows (TEST-COMPAT-001).
- **After release:** each game update adds a row through TEST-COMPAT-103, following
  [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md).

## 6. Open questions and risks

- OPEN-001 (installed edition, version, DLCs), OPEN-023 (conflicts with other mods), OPEN-033 (code-mod support of the
  installed edition), OPEN-035 (isolated testing setup).
- OPEN-507 (new) — testers and hardware for PHASE-12; OPEN-508 (new) — holding and reading the game version during a
  campaign.
- RISK-001 (base mod incompatible with the installed game), RISK-007 (game updates), RISK-016 (conflicts with other
  mods).

All entries are in the [register](../planning/RISKS_AND_OPEN_QUESTIONS.md).
