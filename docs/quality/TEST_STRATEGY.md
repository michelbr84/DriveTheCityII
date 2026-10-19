# Test Strategy

| Field | Value |
|---|---|
| ID | DOC-TEST-STRATEGY |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-00 – PHASE-13 |
| Responsible role | QA (maintainer); Reviewer verifies gate evidence independently; project owner accepts gates and authorizes runtime operations |
| Related requirements | All 93 `REQ-*` in the [PRD](../product/PRD.md); test definitions in [TEST_SCENARIOS](TEST_SCENARIOS.md) and [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md) |
| Related documents | [QA_CHECKLIST](QA_CHECKLIST.md), [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md), [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md), [KNOWN_ISSUES](KNOWN_ISSUES.md), [evidence](evidence/README.md), [TRACEABILITY](../planning/TRACEABILITY.md), [ROADMAP](../../ROADMAP.md), [MODULE_CONTRACTS](../architecture/MODULE_CONTRACTS.md), [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md) |
| Last updated | 2026-10-18 |

> **Current state (2026-10-18):** no test has been executed. No build exists, nothing has been run, and no lab city
> has been created. Every test in this project has the validation status **Not run**. This document is a plan.

## 1. Purpose

This strategy defines how Drive the City II proves that a requirement is met. It covers the test layers, environments,
the lab city policy, the scenario matrix, entry and exit rules for test runs, evidence recording, roles, regression
handling after game updates, and the mapping of tests to the fourteen phases.

A requirement counts as met only after a recorded evidence run of its tests, reviewed by someone who did not implement
the change ([PRD](../product/PRD.md) §8).

## 2. Principles

1. **Compiling is not passing.** A successful build proves that code compiles. It proves nothing about runtime
   behavior. Documentation is not passing either: a finished specification does not complete a phase.
2. **Evidence over assertion.** A result exists only as a `RUN-NNNN` record under [evidence](evidence/README.md).
   Without a run, the status is **Not run**, never "done" or "should work".
3. **Test the contract, not the implementation.** Tests are written against the requirement and the specification
   contract (states, invariants, settlement rules). They must stay valid whichever physics approach
   ([ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md)) or storage option
   ([ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md)) is eventually chosen.
4. **Save safety and control first.** Tests for control authority, the entity lifecycle, and persistence run earliest
   and are re-run on every change to those areas. They are the core of the regression suites (§11).
5. **Lab only.** Runtime tests use lab cities and copies. They never use the owner's primary saves (§5, RISK-028).
6. **Honest results.** A failed or inconclusive run is recorded as such. Failed cycles in a campaign are never
   excluded from the count. A defect found during testing goes to [KNOWN_ISSUES](KNOWN_ISSUES.md).
7. **Reproducibility.** Every run records the game version, mod commit, mod set, lab city copy, and environment, so
   that someone else can repeat it.

## 3. Test layers

| Layer | What it verifies | Where it runs | Automation | Earliest phase |
|---|---|---|---|---|
| **Logic** | Pure rules without the game: the mission lifecycle, settlement and TransactionIds, the ledger, wallet and economy rules, XP/reputation/unlocks, fare and payment formulas, company managerial settlement, time-checkpoint arithmetic, save-format serialization, schema migration, validation, and the quarantine decision. | A future logic test project that references the Career and Persistence logic through ports, with test doubles in place of CityBridge and the other adapters ([MODULE_CONTRACTS](../architecture/MODULE_CONTRACTS.md)). No CS2 required. | Fully automatable. The test runner and framework are not chosen (OPEN-501, OPEN-003). **No production test code exists or is created in this baseline.** | PHASE-01 (save format), PHASE-06 (career) — earlier behind stable contracts ([ROADMAP](../../ROADMAP.md) §5) |
| **Integration** | Technical contracts inside the running game: control-state transitions and guards, movement authority and camera ownership, input routing, entity registration, spawn/despawn and handback, persistence hooks and load-time revalidation, error containment, diagnostics. | The in-game lab environment (§4), with diagnostic readouts from our Diagnostics module (OPEN-502). | Mostly manual execution with instrumented, logged observations. Scripted in-game drivers are an open question (OPEN-503). | PHASE-00 (baseline), PHASE-01 |
| **In-game** | The player's experience: drivability, handling consistency, collisions, traffic interaction, transitions as felt, cameras, UI and HUD, localization, accessibility, onboarding, audio, and fun. | The in-game lab environment; representative cities and hardware in PHASE-12; a clean configuration in PHASE-13. | Manual. Sessions are recorded (video or notes) and judged against the expected result. | PHASE-02 |
| **Review** (auxiliary) | Requirements verified by structured inspection rather than execution: license and provenance audits, notices, labeling of placeholders, dependency declarations, package contents. | Documents, packages, and asset records. | Manual checklist; evidence is the completed checklist in a run record. | PHASE-00 (per incorporation), PHASE-13 |

Rules for the layers:

- A Logic pass never replaces the Integration or In-game test of the same requirement. Test doubles can diverge from
  the real game (RISK-503), so every port contract used by logic tests is also exercised in-game.
- Logic tests are the right place for exhaustive sequences: replayed events, conflicting terminal outcomes, and
  randomized event orders (for example TEST-CAREER-101, TEST-CAREER-103, TEST-ECON-101).
- Performance is measured only by the [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md), never by impressions during
  functional tests.
- Documentation checks (links, IDs, language) are not product tests. They are recorded as `DV-NNN` entries in
  [DOCUMENTATION_VALIDATION_REPORT](DOCUMENTATION_VALIDATION_REPORT.md), not as `RUN-NNNN` records.

## 4. Test environments

| Environment | Purpose | Prerequisites | Status |
|---|---|---|---|
| Documentation workspace | Writing and validating documents only. | The project root. | In use for TASK-0000. No product tests run here. |
| Logic test environment | Running Logic tests on a development machine. | A logic test project and runner (OPEN-501); a confirmed target framework (OPEN-003); the toolchain recorded in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md). Installation requires owner authorization. | Not set up. |
| In-game lab environment | Integration and In-game tests, benchmarks, and save-safety tests. | Owner authorization for setup and runtime operations ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)); the installed edition and version known (OPEN-001, OPEN-033); an isolated mod set and lab cities (OPEN-035); a game version that can be held constant during a campaign (OPEN-508). | Not set up. First defined by [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md) and [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md). |
| Beta environments | PHASE-12 validation across representative cities, hardware, DLC sets, and supported mods. | Available testers and machines (OPEN-507); a compatibility plan ([COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md)). | Not planned in detail. |
| Clean release environment | PHASE-13 install, update, and clean-exit tests on a configuration without development tools (REQ-REL-001). | A supported configuration without SDKs or IDEs; an independent tester (OPEN-507). | Not planned in detail. |

Machine-specific values (paths, hardware, account-related information) are never written into active documents.
They go into the git-ignored local file described in
[LOCAL_ENVIRONMENT.example](../engineering/LOCAL_ENVIRONMENT.example.md). Run records contain only sanitized values
([evidence](evidence/README.md)).

## 5. Lab city policy

A **lab city** is a disposable city used only for validation.

1. **Never primary saves.** Tests never open, modify, overwrite, or delete the owner's primary saves. If a procedure
   seems to need a primary save, stop and ask the project owner (RISK-028).
2. **Copies only.** A lab city is either built for testing or copied, with the owner's permission, from a save the
   owner designates. Each lab city has a frozen **master copy**. Every run starts from a fresh working copy of that
   master. A run never writes to the master.
3. **Identified.** Each master copy has a lab name, a version (incremented whenever it is rebuilt), and a content hash.
   The run record cites all three. The storage location is recorded only in the git-ignored local file.
4. **Outside the project.** Lab saves are game data. They are not stored in the project folder or committed, because
   of size, OneDrive synchronization (RISK-011), and possible personal data (RISK-030).
5. **Isolated mod set.** Runs use an isolated mod set or profile that contains only the mods listed in the run record
   (OPEN-035). The owner's active mod set is not changed without authorization.
6. **Feature coverage.** The lab cities together must contain the surfaces and features listed in the
   [TEST_SCENARIOS](TEST_SCENARIOS.md) lab city checklist (bridges, tunnels, stacked roads, a disconnected road
   island, transit lines, and so on). Missing features are recorded as a gap in the run, not silently skipped.
7. **Disposal.** Working copies are discarded after the run, unless they are needed as evidence. In that case they go to
   the git-ignored `artifacts/` area with a reference from the run record.
8. **Fault injection only in labs.** Simulated crashes, full disks, read-only storage, and corrupted files
   ([SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md)) are applied only to lab copies on storage the owner approved for that
   purpose (OPEN-506).

## 6. Scenario matrix approach

Behavior that depends on place and conditions is tested over a **scenario matrix** defined in
[TEST_SCENARIOS](TEST_SCENARIOS.md):

- **Surfaces:** flat road, sidewalk, curb, ramp or slope, bridge, tunnel, stacked or elevated roads, building frontage,
  terrain edge, junctions.
- **Traffic density:** empty, moderate, congested (native vehicles and pedestrians).
- **Frame-rate conditions:** unconstrained, sustained reduced frame rate, single stall, fluctuating (method: OPEN-504).
- **Vehicle classes:** car, light delivery vehicle, rigid truck, bus.
- **Exit conditions:** free curb side, preferred side blocked, all exits blocked, passing traffic.
- **Time and overlay state:** normal, paused, menu open, focus lost, saving or loading.

A full combination is not required. Each matrix-bound test declares its required cells. The baseline rule
**[Proposal]** is: every surface at empty and congested traffic at an unconstrained frame rate, plus the reduced, stall,
and fluctuating frame-rate conditions on the surfaces where contact can fail (bridge, tunnel, stacked roads, building
frontage, flat road as a control). Every run records which cells it executed.

## 7. Mapping to phases

| Phase | Test focus | Main layers | Gate tests (examples) |
|---|---|---|---|
| PHASE-00 | Candidate base-mod build and runtime baseline in the lab; environment readiness. | Integration, Review | TEST-COMPAT-101; license and dependency review for incorporation (TEST-REL-003 partial) |
| PHASE-01 | Mod identity, diagnostics, activation and unload, save foundation, invalid data. | Logic, Integration | TEST-SAVE-001 – 004, TEST-SAVE-006, TEST-SAVE-104, TEST-CITY-004, TEST-DIAG-001/002, TEST-RECOV-002, TEST-COMPAT-102 |
| PHASE-02 | One-car driving, input and camera separation, handback. | Integration, In-game | TEST-DRIVE-001, TEST-DRIVE-101, TEST-CTRL-001, TEST-CTRL-003, TEST-ENTITY-001 |
| PHASE-03 | Surfaces, collisions, physics comparison, time and focus. | Integration, In-game, Benchmark | TEST-WORLD-001 – 004, TEST-DRIVE-002, TEST-TIME-001 – 003, TEST-PERF-001 |
| PHASE-04 | Avatar, walking, cameras, obstacles, recovery. | In-game, Integration | TEST-PLAYER-001 – 004, TEST-PLAYER-101, TEST-RECOV-001 |
| PHASE-05 | Enter, drive, park, exit; vehicle removal. | Integration, In-game | TEST-TRANS-001 – 004, TEST-TRANS-101 (50 cycles), TEST-CTRL-004, TEST-ENTITY-003 |
| PHASE-06 | Taxi loop, minimal wallet, ledger, rental. | Logic, Integration, In-game | TEST-TAXI-001 – 003, TEST-TAXI-101 (20 rides), TEST-CAREER-001 – 005, TEST-CAREER-101, TEST-SAVE-005 |
| PHASE-07 | Progression, garage, fuel, maintenance, upgrades, financial recovery. | Logic, In-game | TEST-ECON-002 – 005, TEST-ECON-102, TEST-GARAGE-001 – 006 |
| PHASE-08 | Deliveries, freight, bus. | Logic, In-game | TEST-DELIV-001, TEST-FREIGHT-001, TEST-BUS-001/002, TEST-CAREER-006 |
| PHASE-09 | Home base, service points, company. | Logic, Integration, In-game | TEST-HOME-001 – 003, TEST-COMPANY-001 – 003, TEST-TIME-004 |
| PHASE-10 | Challenges, demand-informed jobs, city edits. | Integration, In-game | TEST-CHAL-001 – 003, TEST-CITY-001 – 003, TEST-CITY-101/102 |
| PHASE-11 | Presentation, onboarding, input, accessibility, localization, audio. | In-game, Review | TEST-UI-001 – 004, TEST-ONBOARD-001, TEST-INPUT-001 – 003, TEST-L10N-001, TEST-ART-001/002, TEST-AUDIO-001 |
| PHASE-12 | Beta: representative cities, hardware, DLCs, supported mods, long sessions. | All | TEST-SAVE-109 (30 cycles), TEST-SAVE-114 and TEST-PERF-107 (two-hour sessions), TEST-COMPAT-001, TEST-PERF-001 |
| PHASE-13 | Release: install, update, clean exit, audits, independent tester. | In-game, Review | TEST-REL-001 – 004, TEST-REL-101, TEST-SAVE-006 (re-run) |

The authoritative list of tests per phase is in each [phase document](../planning/phases/README.md). Every test entry
in [TEST_SCENARIOS](TEST_SCENARIOS.md) names its phase(s). The requirement → test → evidence chain is maintained in
[TRACEABILITY](../planning/TRACEABILITY.md).

## 8. Proposed acceptance quantities

These numbers are **[Proposal]** acceptance criteria from the owner brief. They are not results, estimates, or
promises. The project owner may revise them.

| Quantity | Phase | Test | Counting rule **[Proposal]** |
|---|---|---|---|
| 50 successful walk–drive transition cycles across the scenario matrix | PHASE-05 | TEST-TRANS-101 | One cycle = `OnFoot` → `EnteringVehicle` → `Driving` → park → `ExitingVehicle` → `OnFoot`. It succeeds only with no duplicated entity, no camera loss, no lost control, and no traffic blocker. Any failed cycle is recorded. After a fix in a critical area, the count restarts on the new build. |
| 20 taxi rides in different districts, including a cancellation and a save/load during a mission | PHASE-06 | TEST-TAXI-101 | The cancellation and the mid-mission save/load are part of the 20. Every terminal outcome settles exactly once, verified against the ledger. |
| 30 save/load cycles | PHASE-12 | TEST-SAVE-109 | One cycle = save → leave the city (main menu or full game restart) → load → verify the invariants (wallet, ledger, entity counts, mission state). Both reload types are included. |
| Sessions of at least two hours | PHASE-12 | TEST-SAVE-114, TEST-PERF-107 | A continuous session mixing careers, mode switches, and saves. The number of sessions and the activity mix are set in the [PHASE-12](../planning/phases/PHASE-12-beta-and-validation.md) document. |
| Recurring frame-time regression above 10% | PHASE-03 onwards | TEST-PERF-001 | An **investigation trigger** against a recorded baseline. It is not a performance budget and not a frame-rate promise ([BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md)). |

These quantities give limited statistical confidence for rare defects (RISK-502). Long sessions, soak tests, and
logged anomalies supplement them.

## 9. Entry and exit criteria for a test run

**Entry criteria (all must hold before a run starts):**

- The test ID exists in [TEST_SCENARIOS](TEST_SCENARIOS.md) or [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md), with its
  preconditions met.
- The build under test is identified (mod commit, or for PHASE-00 the pinned upstream revision of the candidate base
  mod).
- The game version, store edition, DLC set, and mod set are recorded. The game version is held constant for the run
  (OPEN-508).
- A fresh working copy of the named lab city master is in place (§5).
- The project owner has authorized the runtime operations involved (launching the game, enabling mods, fault
  injection).
- Diagnostics logging is enabled once it exists, and screen recording is ready when the test needs it.
- A run number `RUN-NNNN` is assigned ([evidence](evidence/README.md)).

**Suspension criteria (stop immediately and record why):**

- A primary save, the game installation, or files outside the lab scope would be written.
- Personal data appears in captured output and cannot be sanitized.
- The game version changed during the run.
- The environment no longer matches the recorded one (for example, another mod was enabled).

**Exit criteria (a run is complete only when):**

- Every procedure step was executed, or the run was stopped with a recorded reason.
- Each test in the run has a result: **Passed**, **Failed**, or **Inconclusive**, with observed behavior described.
- Logs, recordings, and data are stored and sanitized as required by [evidence](evidence/README.md).
- Every failure that reveals a defect has an `ISSUE-NNN` in [KNOWN_ISSUES](KNOWN_ISSUES.md).
- A Reviewer is named. Gate evidence needs a Reviewer who did not implement the change.
- Lab working copies are discarded or archived as artifacts.

## 10. Roles

| Role | Testing responsibility |
|---|---|
| Project owner | Authorizes environments, runtime operations, and fault injection; accepts or rejects phase gates; judges the M2 fun checkpoint (TEST-TAXI-102). |
| Coordinator | Schedules runs, keeps [TRACEABILITY](../planning/TRACEABILITY.md) and [PROJECT_STATUS](../PROJECT_STATUS.md) consistent with evidence, and routes failures to tasks. |
| Implementer | Writes the code and the future logic tests. May run tests during development. These runs count as gate evidence only after an independent review. |
| QA | Executes test runs, records `RUN-NNNN` evidence, files `ISSUE-NNN` entries, and maintains this strategy and the test definitions. |
| Reviewer | Checks evidence against the test definition and the phase gate. Must not be the implementer of the change under test. Self-review is recorded as self-review and is not independent approval ([ROLES_AND_OWNERSHIP](../agents/ROLES_AND_OWNERSHIP.md)). |

One agent may play several roles one after another, but never both implementer and independent reviewer of the same
change. Tests that require a **new player** or an **independent tester** (TEST-ONBOARD-001, TEST-REL-001) need a person
who has not worked on the project (OPEN-507, RISK-501). No real agents, plugins, or MCP servers are configured for
testing.

## 11. Regression policy

**Triggers for a regression run:**

1. A CS2 update, hotfix, or DLC release (RISK-007).
2. Any change to a critical area: control authority, the entity lifecycle, or the save format.
3. A change to the pinned upstream base-mod revision or to another dependency.
4. A toolchain change recorded in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
5. A defect fix (re-run the failing test plus the suite of the affected area).

**Suites [Proposal]:**

| Suite | Contents | When |
|---|---|---|
| Smoke suite | Whichever of these tests the current build supports: TEST-CITY-004, TEST-SAVE-001, TEST-SAVE-006, TEST-DIAG-001, TEST-CTRL-001, TEST-CTRL-004, TEST-TRANS-001, TEST-TRANS-002, TEST-ENTITY-001, TEST-ENTITY-003, TEST-CAREER-004, TEST-PERF-101/102 | After every game update (TEST-COMPAT-103) and before every gate review |
| Critical-area suite | All TEST-CTRL-*, TEST-TRANS-*, TEST-ENTITY-*, and TEST-SAVE-* tests that the build supports | After any change to SPEC-CONTROL, SPEC-ENTITY, or SPEC-SAVE behavior |
| Logic suite | All Logic-layer tests | On every logic change (automatable once OPEN-501 is resolved) |
| Phase gate suite | All tests listed in the phase document | At the gate review |

**After a game update:**

- Do not update the lab environment in the middle of a campaign. Finish or abort the campaign first (OPEN-508).
- Run the smoke suite on the new version (TEST-COMPAT-103) before any further feature testing.
- Add a new row to [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md). Results for earlier versions stay as they were.
  They are not carried over to the new version.
- If the smoke suite fails, record `ISSUE-NNN` entries and follow
  [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md). Make no compatibility claim for the new version
  until the suite passes.
- Re-measure the benchmark baseline. A game update can shift frame time without any change in the mod.

## 12. What cannot be automated (or not yet)

- **Feel and fun:** handling quality, camera comfort, whether walking and driving are enjoyable (RISK-023). These rely
  on recorded human judgment.
- **Visual correctness:** clipping, animation quality, readability of UI and route guidance, placeholder labeling as
  seen by players.
- **Language quality:** whether PT-BR text is natural and correct needs a fluent reviewer. Only key completeness can be
  checked automatically.
- **Accessibility judgments:** whether captions, indicators, and scaling actually help.
- **New-player onboarding:** requires a person without prior knowledge of the project.
- **Store, launcher, and platform flows:** installing, updating, and removing through the distribution platform.
- **Behavior after real game updates:** depends on external releases we do not control.
- **In-game scenario execution in general:** until OPEN-503 is answered, every Integration and In-game test is run
  manually, with instrumented observations.

## 13. Defects and results

- A failed test caused by a defect → an `ISSUE-NNN` entry in [KNOWN_ISSUES](KNOWN_ISSUES.md), linked to the run.
- An anticipated problem that has not been observed → the [risk register](../planning/RISKS_AND_OPEN_QUESTIONS.md).
- A feature that is not implemented yet → not a defect. The test stays **Not run**.
- A requirement proven infeasible → a scope review (a `CONF-*` entry and a proposal to the owner), never a silent
  removal ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §8).

## 14. Open questions and risks raised by this document

New entries proposed for the central [register](../planning/RISKS_AND_OPEN_QUESTIONS.md):

- **OPEN-501** — Logic test harness: test runner, framework, and how pure logic is referenced without game assemblies.
- **OPEN-502** — Test observability: which diagnostic readouts (entity registry, state trace, ledger dump, save identity,
  city snapshot, telemetry) the Diagnostics module must expose for tests.
- **OPEN-503** — Whether in-game scenarios can be driven by a scripted, development-only driver.
- **OPEN-504** — The tested frame-rate range for REQ-DRIVE-002 and a reproducible way to induce reduced frame rates and
  stalls.
- **OPEN-505** — Benchmark tooling for frame time, memory, and simulation speed, and a repeatable camera path or drive.
- **OPEN-506** — Safe methods for save fault injection (forced termination, full or read-only storage, corruption).
- **OPEN-507** — Availability of independent testers and hardware configurations for PHASE-11 to PHASE-13.
- **OPEN-508** — Holding the game version constant during a campaign and reading the exact version for evidence.
- **OPEN-509** — Proposed requirement: an introduction for each career (the PHASE-08 gate asks for one; no `REQ-*`
  covers it). Raised in [TEST_SCENARIOS](TEST_SCENARIOS.md).
- **RISK-501** — Insufficient tester independence (self-validation bias).
- **RISK-502** — The proposed quantities give limited confidence for rare defects.
- **RISK-503** — Logic-test doubles diverge from host-game behavior.

Existing entries this strategy depends on: OPEN-001, OPEN-003, OPEN-006, OPEN-033, OPEN-035, RISK-007, RISK-011,
RISK-013, RISK-023, RISK-028, RISK-030.
