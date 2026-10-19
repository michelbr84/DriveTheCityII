# Test Scenarios

| Field | Value |
|---|---|
| ID | DOC-TEST-SCENARIOS |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-00 – PHASE-13 |
| Responsible role | QA (maintainer); Reviewer checks that each test still matches its requirement after PRD or spec changes |
| Related requirements | All `REQ-*` in the [PRD](../product/PRD.md) except `REQ-SAVE-*` (see [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md)) |
| Related documents | [TEST_STRATEGY](TEST_STRATEGY.md), [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md), [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md), [QA_CHECKLIST](QA_CHECKLIST.md), [TRACEABILITY](../planning/TRACEABILITY.md), [evidence](evidence/README.md) |
| Last updated | 2026-10-18 |

> **Validation status of every test in this file: Not run.** Nothing has been built or executed. These are planned
> test definitions written against requirements and specification contracts, not records of results.

## 1. How to read this document

- `TEST-<AREA>-NNN` with a number below 101 is the **primary test** of `REQ-<AREA>-NNN`. Numbers from 101 upwards are
  additional tests (soaks, edge cases, gate campaigns).
- Each entry gives: ID and title · Layer · Requirement(s) · Phase · Preconditions · Procedure · Expected result ·
  Validation status. Layers (Logic, Integration, In-game, and the auxiliary Review layer) are defined in
  [TEST_STRATEGY](TEST_STRATEGY.md) §3.
- Thresholds, tolerances, distances, and timings are **not** invented here. A test refers to the value defined in the
  relevant specification. Where the specification has not defined it yet, the test cannot pass until it does.
- A test whose feature does not exist in the build stays **Not run**. That is not a failure.
- Where a test needs a diagnostic readout (entity registry, state trace, ledger dump, telemetry, city snapshot), the
  readout is a planned Diagnostics capability that is still an open question (OPEN-502).
- Runs are recorded as `RUN-NNNN` following [evidence](evidence/README.md). This file never contains results.

## 2. Index

| Area | Requirements | Primary tests | Additional tests | Main specification |
|---|---|---|---|---|
| PLAYER | REQ-PLAYER-001 – 004 | TEST-PLAYER-001 – 004 | TEST-PLAYER-101 | [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md) |
| DRIVE | REQ-DRIVE-001 – 004 | TEST-DRIVE-001 – 004 | TEST-DRIVE-101 | [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md) |
| WORLD | REQ-WORLD-001 – 004 | TEST-WORLD-001 – 004 | — | [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) |
| CTRL | REQ-CTRL-001 – 004 | TEST-CTRL-001 – 004 | TEST-CTRL-101, 102 | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md) |
| TRANS | REQ-TRANS-001 – 004 | TEST-TRANS-001 – 004 | TEST-TRANS-101 – 103 | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md) |
| ENTITY | REQ-ENTITY-001 – 003 | TEST-ENTITY-001 – 003 | TEST-ENTITY-101, 102 | [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) |
| TIME | REQ-TIME-001 – 004 | TEST-TIME-001 – 004 | — | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) |
| CAREER | REQ-CAREER-001 – 006 | TEST-CAREER-001 – 006 | TEST-CAREER-101 – 104 | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) |
| TAXI | REQ-TAXI-001 – 003 | TEST-TAXI-001 – 003 | TEST-TAXI-101, 102 | [SPEC-TAXI](../gameplay/careers/TAXI.md) |
| DELIV | REQ-DELIV-001 | TEST-DELIV-001 | — | [SPEC-DELIVERY](../gameplay/careers/DELIVERIES.md) |
| FREIGHT | REQ-FREIGHT-001 | TEST-FREIGHT-001 | — | [SPEC-FREIGHT](../gameplay/careers/FREIGHT.md) |
| BUS | REQ-BUS-001 – 002 | TEST-BUS-001 – 002 | TEST-BUS-101 | [SPEC-BUS](../gameplay/careers/BUS.md) |
| ECON | REQ-ECON-001 – 005 | TEST-ECON-001 – 005 | TEST-ECON-101, 102 | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) |
| GARAGE | REQ-GARAGE-001 – 006 | TEST-GARAGE-001 – 006 | TEST-GARAGE-101, 102 | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) |
| HOME | REQ-HOME-001 – 003 | TEST-HOME-001 – 003 | TEST-HOME-101 | [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md) |
| COMPANY | REQ-COMPANY-001 – 003 | TEST-COMPANY-001 – 003 | TEST-COMPANY-101 | [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md) |
| CHAL | REQ-CHAL-001 – 003 | TEST-CHAL-001 – 003 | — | [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) |
| CITY | REQ-CITY-001 – 004 | TEST-CITY-001 – 004 | TEST-CITY-101, 102 | [SPEC-CITY](../architecture/CITY_INTEGRATION.md) |
| SAVE | REQ-SAVE-001 – 006 | Defined in [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md) | Defined there | [SPEC-SAVE](../architecture/SAVE_FORMAT.md) |
| RECOV | REQ-RECOV-001 – 002 | TEST-RECOV-001 – 002 | TEST-RECOV-101 | [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) |
| DIAG | REQ-DIAG-001 – 002 | TEST-DIAG-001 – 002 | TEST-DIAG-101 | [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) |
| UI | REQ-UI-001 – 004 | TEST-UI-001 – 004 | — | [SPEC-UI](../experience/UI_AND_HUD.md) |
| ONBOARD | REQ-ONBOARD-001 | TEST-ONBOARD-001 | TEST-ONBOARD-101, 102 | [SPEC-ONBOARD](../experience/ONBOARDING.md) |
| INPUT | REQ-INPUT-001 – 003 | TEST-INPUT-001 – 003 | TEST-INPUT-101 | [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md) |
| L10N | REQ-L10N-001 | TEST-L10N-001 | TEST-L10N-101 | [SPEC-L10N](../experience/LOCALIZATION.md) |
| ART | REQ-ART-001 – 002 | TEST-ART-001 – 002 | — | [SPEC-ART](../experience/ART_AND_ANIMATION.md) |
| AUDIO | REQ-AUDIO-001 | TEST-AUDIO-001 | — | [SPEC-AUDIO](../experience/AUDIO.md) |
| PERF | REQ-PERF-001 – 002 | TEST-PERF-001 – 002 | TEST-PERF-101 – 109 in [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md) | [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) |
| COMPAT | REQ-COMPAT-001 | TEST-COMPAT-001 | TEST-COMPAT-101 – 103 | [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) |
| REL | REQ-REL-001 – 004 | TEST-REL-001 – 004 | TEST-REL-101 | [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) |

**Counts:** this file defines 87 primary tests (one per non-SAVE requirement) and 34 additional tests, 121 in total.
[SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md) defines the 6 primary TEST-SAVE tests and their additional tests.
[BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md) defines the benchmark scenarios TEST-PERF-101 and up.

## 3. Standard setups

Preconditions refer to these setups by name.

| Setup | Meaning |
|---|---|
| **Logic harness** | The future logic test project (OPEN-501). It runs Career and Persistence logic against test doubles for every port, with a controllable in-game clock. No CS2. |
| **Lab** | The in-game lab environment ([TEST_STRATEGY](TEST_STRATEGY.md) §4–§5): a fresh working copy of a named lab city master, an isolated mod set, the mod build identified by commit, diagnostics logging on, and screen recording available. Owner authorization for runtime operations is in place. |
| **Career lab** | Lab, plus a career profile created fresh for the run or loaded from a named fixture save (a lab save prepared in a known state, for example "zero money, no vehicle"). |
| **Sandbox** | Lab with Sandbox mode enabled. Native vehicles may be borrowed temporarily. Ownership is never granted. |
| **Control copy** | A second working copy of the same lab master, run for the same in-game interval with the mod absent or idle. Used to separate mod effects from the natural evolution of the simulation. Whether the simulation is deterministic is unverified, so comparisons are judged against the variation between control copies. |

## 4. Lab city feature checklist

The lab cities together must provide the following. A run that needs a missing feature records the gap and stays
**Inconclusive** for that part. Building the lab cities is future work under owner authorization (OPEN-035).

| Feature | Used by |
|---|---|
| Straight flat road segment long enough for braking and acceleration measurements | TEST-DRIVE-002, TEST-GARAGE-005 |
| Curves, junctions, and (if available) a roundabout | TEST-DRIVE-101, TEST-ENTITY-102 |
| Sidewalks, curbs, low steps, gentle and steep slopes | TEST-PLAYER-002, TEST-PLAYER-101 |
| Ramps, a bridge over a road, a bridge over water or terrain | TEST-WORLD-001, TEST-TRANS-103 |
| A road tunnel with entry, interior, and exit | TEST-WORLD-001, TEST-TIME-003 |
| Stacked or elevated roads (one road directly above another) | TEST-WORLD-001, TEST-TRANS-103 |
| Dense building frontage with static props | TEST-WORLD-002, TEST-PLAYER-004, TEST-PERF-105 |
| Quiet streets and a congested arterial | Traffic cells T0–T2 |
| A disconnected road island (not reachable through the network) | TEST-CAREER-002 |
| Residential, commercial, office, industrial, and service buildings | TEST-CAREER-003, TEST-FREIGHT-001, TEST-CHAL-003 |
| At least one bus line with several stops | TEST-BUS-001, TEST-BUS-002 |
| Distinct districts | TEST-TAXI-101 |
| A map edge or out-of-bounds area reachable by driving | TEST-RECOV-001 |

## 5. Scenario matrix

### 5.1 Dimensions

| Dimension | Cells |
|---|---|
| Surface (S) | S1 flat road (straight and curved) · S2 sidewalk or pedestrian path · S3 curb and low step · S4 ramp or slope · S5 bridge deck · S6 tunnel · S7 stacked or elevated roads · S8 building frontage and static obstacles · S9 terrain off the road network · S10 junction or roundabout |
| Traffic (T) | T0 empty (no moving native agents nearby) · T1 moderate flow · T2 congested queue. Includes native pedestrians. Density is chosen by location in the lab city and described in the run. |
| Frame rate (F) | F1 unconstrained · F2 sustained reduced frame rate · F3 single injected stall · F4 fluctuating frame rate. Induction method and the tested range: OPEN-504. |
| Vehicle class (V) | V1 car · V2 light delivery vehicle · V3 rigid truck · V4 bus. Only classes present in the build are tested. |
| Exit condition (E) | E1 preferred curb side free · E2 preferred side blocked, alternate available · E3 every exit point blocked · E4 traffic passing on the road side |
| Time and overlay (O) | O1 normal · O2 paused · O3 menu open · O4 window focus lost · O5 saving or loading |

### 5.2 Required cells per matrix-bound test **[Proposal]**

| Test | Required cells |
|---|---|
| TEST-PLAYER-002, TEST-PLAYER-101 | S1–S4, S8, S9 at T0 and T1, F1 |
| TEST-PLAYER-004 | S8, S5 edges at F1 and F3 |
| TEST-DRIVE-002 | S1, S4, S5 × F1–F4 (V1 first; V2–V4 when available) |
| TEST-DRIVE-101 | S1, S4, S10 × T0, T1, F1 |
| TEST-WORLD-001 | S1–S7, S9 at F1; S5, S6, S7 also at F2–F4 |
| TEST-WORLD-002 | S8 × available V classes × F1, F3; avatar on S8 |
| TEST-WORLD-004 | S1, S10 × T1, T2 |
| TEST-CTRL-001 | S1, S10 × T0, T2 × F1, F2 |
| TEST-TRANS-002 | E1–E4 × S1, S5, S6, S7, S8 |
| TEST-TRANS-101 | Spread over S1–S8, T0–T2, E1–E4, F1 and F2; every surface row appears at least once; the distribution is recorded |
| TEST-TRANS-103 | S5, S6, S7 × E1–E3 |
| TEST-TIME-002 | O2, O4 during OnFoot and Driving |
| TEST-TIME-003 | S5, S8 × F2, F3 |
| TEST-ENTITY-001, TEST-ENTITY-102 | S1, S10 × T1, T2 |

Tests not listed here are not matrix-bound. They run in the setup their preconditions name.

## 6. Test definitions

### 6.1 On-foot character and cameras (PLAYER)

#### TEST-PLAYER-001 — Visible mod-owned avatar without citizen takeover
- **Layer:** In-game · **Requirement:** REQ-PLAYER-001 · **Phase:** PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab; build with the avatar; entity registry readout.
- **Procedure:** 1) In `Building`, note a sample of citizens near the entry point who have a household, job, or schedule. 2) Enter character mode. 3) Walk in first- and third-person views. 4) Return to `Building` and re-check the sampled citizens.
- **Expected result:** A visible avatar appears and responds to input. The registry lists it as an `Avatar` owned by the mod. No native citizen is taken over, and the sampled citizens' household, job, and schedule are unaffected.

#### TEST-PLAYER-002 — Walking, running, turning, free look, and small elevation changes
- **Layer:** In-game · **Requirement:** REQ-PLAYER-002 · **Phase:** PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2.
- **Procedure:** 1) Walk and run on each required surface. 2) Turn in place and while moving. 3) Use free look while walking. 4) Cross curbs, low steps, and gentle slopes. 5) Try a step or slope above the limits defined in SPEC-PLAYER.
- **Expected result:** Movement is smooth and responsive. Small elevation changes within the SPEC-PLAYER limits are crossed without jitter or teleporting. Obstacles beyond the limits block the avatar cleanly. Free look follows the behavior defined in SPEC-PLAYER.

#### TEST-PLAYER-003 — First- and third-person cameras without clipping
- **Layer:** In-game · **Requirement:** REQ-PLAYER-003 · **Phase:** PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab.
- **Procedure:** 1) Switch cameras repeatedly while idle, walking, and running. 2) Stand with your back to a facade (S8), under a bridge (S5), in a tunnel (S6), and on a slope against terrain. 3) Rotate the third-person camera through a full circle in each place.
- **Expected result:** Switching works in every state, and only one camera owner is active. The third-person camera never shows building interiors or the underside of terrain. It pulls in or applies the avoidance defined in SPEC-PLAYER.

#### TEST-PLAYER-004 — No passing through obstacles or falling below ground; invalid position triggers Recovery
- **Layer:** Integration · **Requirement:** REQ-PLAYER-004 · **Phase:** PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2; a development-only way to force an invalid position (OPEN-502).
- **Procedure:** 1) Walk and run into facades at several angles. 2) Push along walls and against static props. 3) Walk along bridge edges. 4) Repeat steps 1–3 with an injected stall (F3). 5) Force an invalid position below ground.
- **Expected result:** The avatar never passes through facades or static obstacles and never falls below valid ground. The forced invalid position is detected, `Recovery` places the avatar at a validated safe point, and the event is logged.

#### TEST-PLAYER-101 — Representative urban circuit (PHASE-04 gate)
- **Layer:** In-game · **Requirement:** REQ-PLAYER-001 – 004, REQ-RECOV-001 · **Phase:** PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab; a circuit defined in the [PHASE-04](../planning/phases/PHASE-04-on-foot-character.md) document covering the required cells.
- **Procedure:** 1) Walk and run the full circuit, crossing roads with moderate traffic. 2) Repeat in the other camera view. 3) Record video of both passes.
- **Expected result:** The circuit is completed with no clipping, no falling, and no permanent loss of control. Every automatic `Recovery` is logged and counted in the run record, not hidden.

### 6.2 Driving and vehicle classes (DRIVE)

#### TEST-DRIVE-001 — Driving controls and instruments
- **Layer:** In-game · **Requirement:** REQ-DRIVE-001 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Sandbox (PHASE-02) or Career lab with a car (later phases); telemetry readout.
- **Procedure:** 1) Accelerate, brake, reverse. 2) Steer with partial and full input. 3) Apply the parking brake on a slope (S4). 4) Compare the speedometer with telemetry speed. 5) Drive a curve with the chase camera.
- **Expected result:** Each control has the effect defined in SPEC-DRIVING. Steering is progressive. The parking brake holds the car on the slope. The speedometer agrees with telemetry within the SPEC-DRIVING tolerance. The chase camera follows without detaching.

#### TEST-DRIVE-002 — Handling consistency across frame-rate conditions
- **Layer:** In-game · **Requirement:** REQ-DRIVE-002 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2; frame-rate induction and the tested range defined (OPEN-504); telemetry readout; reproducible inputs (scripted per OPEN-503, or carefully repeated manual inputs).
- **Procedure:** 1) At each frame-rate condition, perform a fixed maneuver set: acceleration from standstill to a reference speed, braking to a stop, a constant-steer circle, and a lane change. 2) Record telemetry. 3) Compare the outcomes across conditions.
- **Expected result:** Stopping distance, time to reference speed, and turning radius agree across conditions within the tolerance set in SPEC-DRIVING / [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md). If inputs could not be reproduced, the result is Inconclusive, not Passed.

#### TEST-DRIVE-003 — Four vehicle classes with distinct handling and capacity
- **Layer:** Logic + In-game · **Requirement:** REQ-DRIVE-003 · **Phase:** PHASE-03, PHASE-07, PHASE-08 · **Validation status:** Not run
- **Preconditions:** Logic harness for parameter sets; Lab with each available class.
- **Procedure:** 1) Logic: check that car, light delivery vehicle, rigid truck, and bus parameter sets differ as SPEC-DRIVING defines (mass, acceleration, braking, turning) and that each has its capacity rule. 2) In-game: run the TEST-DRIVE-002 maneuver set for each class at F1 on S1. 3) Try to exceed each capacity limit.
- **Expected result:** Measured handling differs between classes in the direction SPEC-DRIVING defines, beyond run-to-run variation. Capacity limits are enforced for every class.

#### TEST-DRIVE-004 — Hood or driver-position camera with documented limitations
- **Layer:** In-game + Review · **Requirement:** REQ-DRIVE-004 (Should) · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Lab with each available vehicle class; the limitations list in SPEC-DRIVING / [SPEC-ART](../experience/ART_AND_ANIMATION.md).
- **Procedure:** 1) Switch to the hood or driver camera in each class. 2) Drive a short route. 3) Compare what you see with the documented limitations. 4) Check vehicles without a suitable interior.
- **Expected result:** The camera is available. The observed limitations match the documentation. No detailed cockpit is shown for a model without a suitable interior.

#### TEST-DRIVE-101 — Validation route and return to native traffic (PHASE-02 gate)
- **Layer:** In-game + Integration · **Requirement:** REQ-DRIVE-001, REQ-ENTITY-001, REQ-CTRL-001 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Sandbox; a validation route with curves, junctions, and slopes defined in the [PHASE-02](../planning/phases/PHASE-02-basic-driving.md) document.
- **Procedure:** 1) Borrow a native car. 2) Drive the full route. 3) Stop and hand the car back. 4) Observe it and the surrounding traffic for the observation window set in the phase document.
- **Expected result:** The route is completed without losing control or leaving the road unintentionally. After handback, the expectations of TEST-ENTITY-001 hold.

### 6.3 World contact (WORLD)

#### TEST-WORLD-001 — Correct surface and level on every surface type
- **Layer:** Integration · **Requirement:** REQ-WORLD-001 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2; telemetry with height and level readout.
- **Procedure:** 1) Drive and walk over each required surface. 2) On bridges, stop, reverse, and steer close to the edge. 3) Drive along both levels of stacked roads. 4) Enter, cross, and leave tunnels. 5) Repeat steps 2–4 at F2–F4.
- **Expected result:** Height follows the current surface. The entity never switches level (bridge deck ↔ road below, tunnel ↔ surface above). Level changes happen only through connected ramps.

#### TEST-WORLD-002 — Useful collision with buildings and static obstacles
- **Layer:** In-game · **Requirement:** REQ-WORLD-002 · **Phase:** PHASE-03, PHASE-04 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2.
- **Procedure:** 1) Drive into facades at low and high speed, at shallow and perpendicular angles. 2) Scrape along a wall. 3) Hit the static obstacles that SPEC-CONTACT covers. 4) Walk into the same obstacles with the avatar. 5) Repeat steps 1 and 3 with an injected stall.
- **Expected result:** Neither vehicles nor the avatar pass through. The response (stop, deflect) matches SPEC-CONTACT. Nothing gets stuck inside geometry. If something is stuck, `Recovery` is available.

#### TEST-WORLD-003 — Locally generated collision geometry is bounded and cleaned up
- **Layer:** Integration · **Requirement:** REQ-WORLD-003 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; applies only if the CollisionProxy approach is adopted (ADR-0004, SPEC-CONTACT). Otherwise the run records "not applicable" with the reason. Proxy count and memory readout.
- **Procedure:** 1) Record proxy count and memory. 2) Drive and walk through the densest district. 3) Return to `Building`. 4) Unload the city. 5) Disable the mod (safe-disable, then between sessions).
- **Expected result:** Count, memory, and lifetime stay within the SPEC-CONTACT / [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) bounds. Proxies are removed when no longer needed, and the count is zero after steps 3, 4, and 5. Performance impact is measured separately (TEST-PERF-105).

#### TEST-WORLD-004 — One consistent contact rule with native traffic and pedestrians
- **Layer:** Integration · **Requirement:** REQ-WORLD-004 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; the rule chosen in PHASE-03 (OPEN-032) is recorded in SPEC-CONTACT; matrix cells per §5.2.
- **Procedure:** 1) Make contact with moving, stopped, and queued native vehicles. 2) Approach native pedestrians at crossings. 3) Drive away. 4) Observe the native agents involved for the observation window.
- **Expected result:** Every contact follows the single documented rule. Afterwards the native agents continue normally: none are frozen, duplicated, or removed, and no queue remains stuck.

### 6.4 Control authority (CTRL)

#### TEST-CTRL-001 — Exactly one movement authority per controlled entity
- **Layer:** Integration · **Requirement:** REQ-CTRL-001 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Sandbox; state-trace readout that logs the movement authority per controlled entity; matrix cells per §5.2.
- **Procedure:** 1) Borrow a native vehicle, drive, stop, hand back. 2) Trigger `Recovery` while driving. 3) Repeat with a career vehicle once available. 4) Inspect the trace and the recorded video for position oscillation.
- **Expected result:** At every moment the trace shows exactly one movement authority for the entity. Native AI and the player controller never write the same vehicle in the same frame. No jitter is visible.

#### TEST-CTRL-002 — Explicit states, guards, and a single camera owner
- **Layer:** Logic + Integration · **Requirement:** REQ-CTRL-002 · **Phase:** PHASE-01, PHASE-02, PHASE-05 · **Validation status:** Not run
- **Preconditions:** Logic harness (if the state machine is implemented as pure logic); Lab with state-trace readout.
- **Procedure:** 1) Logic: feed every request in every state and compare with the SPEC-CONTROL transition table. 2) In-game: pass through every state and overlay. 3) Request transitions the guards must refuse (for example, entering a vehicle from `Building`). 4) Record the camera owner in each state.
- **Expected result:** Only transitions in the SPEC-CONTROL table happen. Refused requests change nothing and give feedback. Each state has exactly one camera owner. Overlays suspend and then restore the underlying state.

#### TEST-CTRL-003 — Character-mode and building-mode input stay separate
- **Layer:** Integration · **Requirement:** REQ-CTRL-003 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Lab; the list of mod bindings (SPEC-INPUT) and a list of the game's city-building shortcuts observed in the installed version.
- **Procedure:** 1) In `OnFoot` and `Driving`, press every mod binding and every listed building shortcut; scroll and click. 2) In `Building` (with a parked career vehicle in the world, when available), press every mod movement binding.
- **Expected result:** Character-mode input never moves the building camera or activates a building tool. Building-mode input never moves the avatar or a vehicle.

#### TEST-CTRL-004 — Return to Building from any stable state
- **Layer:** Integration · **Requirement:** REQ-CTRL-004 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Career lab with an active mission (once careers exist); state-trace readout.
- **Procedure:** 1) Note the building camera position, simulation speed, wallet, and mission state. 2) Enter character mode. 3) Return to `Building` from `OnFoot`, from `Driving` (stopped), and from `Driving` while moving if SPEC-CONTROL allows it. 4) Re-check the values from step 1.
- **Expected result:** Building control and camera are restored. Simulation speed is restored (see TEST-TIME-001). The wallet is unchanged, and the mission state follows SPEC-MISSION. No orphan entities remain (see TEST-ENTITY-003).

#### TEST-CTRL-101 — Rapid and conflicting mode-switch requests
- **Layer:** Integration · **Requirement:** REQ-CTRL-002, REQ-TRANS-003 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab; state-trace readout.
- **Procedure:** 1) Press enter and exit repeatedly and quickly near a vehicle. 2) Toggle character mode during `EnteringVehicle` and `ExitingVehicle`. 3) Open the menu and pause during transitions. 4) Press conflicting bindings at the same time.
- **Expected result:** Guards serialize or refuse the requests. Exactly one player state is active at any time. No avatar or vehicle is duplicated. A camera owner is always defined.

#### TEST-CTRL-102 — Overlays applied during every player state
- **Layer:** Integration · **Requirement:** REQ-CTRL-002, REQ-ENTITY-003 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab; state-trace and registry readouts.
- **Procedure:** For each player state: 1) Apply `Paused`, then resume. 2) Apply `Menu`, then close it. 3) Load another lab save (`Loading`). 4) Quit to the main menu (`Unloading`).
- **Expected result:** After steps 1–2 the same state and camera owner return. On loading and unloading, mod entities are persisted or despawned as SPEC-ENTITY defines. No unhandled errors are logged.

### 6.5 Walking/driving transitions (TRANS)

#### TEST-TRANS-001 — Entering a nearby stationary vehicle through the prompt
- **Layer:** In-game · **Requirement:** REQ-TRANS-001 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab with an available career vehicle (or a borrowed native vehicle in Sandbox).
- **Procedure:** 1) Approach the vehicle from each side and from the front and back. 2) Note where the prompt appears. 3) Enter. 4) Repeat with two eligible vehicles close together.
- **Expected result:** The prompt appears only for eligible vehicles within the SPEC-TRANSITION range. Entering goes through `EnteringVehicle` to `Driving`. With two vehicles, the indicated vehicle is the one entered.

#### TEST-TRANS-002 — Exit at a validated safe position
- **Layer:** Integration · **Requirement:** REQ-TRANS-002 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2.
- **Procedure:** For each required cell: 1) Park. 2) Exit. 3) Record the exit position, elevation, and any feedback.
- **Expected result:** E1: the avatar exits on the preferred curb side. E2: an alternate point is used. E3: the player gets clear feedback and stays in the vehicle, or `Recovery` runs, as SPEC-TRANSITION defines. E4: the avatar is never placed into passing traffic. In no cell is the avatar inside a wall or at the wrong elevation.

#### TEST-TRANS-003 — Vehicle removed or invalidated during a transition
- **Layer:** Integration · **Requirement:** REQ-TRANS-003 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab or Sandbox; a development-only way to remove a vehicle (OPEN-502).
- **Procedure:** During `EnteringVehicle` and again during `ExitingVehicle`: 1) remove the vehicle through the development tool; 2) in Sandbox, let a native vehicle be despawned by the game where that can be arranged; 3) load another save mid-transition.
- **Expected result:** The transition aborts safely to `OnFoot` or `Recovery`. The avatar stays visible and controllable, the camera stays attached, and no entity is duplicated. The event is logged.

#### TEST-TRANS-004 — Three distinct end-of-drive outcomes
- **Layer:** Integration · **Requirement:** REQ-TRANS-004 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Career lab and Sandbox; registry readout.
- **Procedure:** 1) Career mode: park a career vehicle and exit. 2) Sandbox: exit a borrowed native vehicle. 3) After a drive that created temporary mod objects (markers), end the drive. 4) Inspect the registry after each step.
- **Expected result:** Step 1: the vehicle is parked and stays registered as a CareerVehicle. Step 2: the vehicle is handed back (see TEST-ENTITY-001). Step 3: the temporary objects are removed. Career mode never takes the handback path.

#### TEST-TRANS-101 — 50-cycle transition soak across the scenario matrix (PHASE-05 proposal)
- **Layer:** Integration + In-game · **Requirement:** REQ-TRANS-001 – 004, REQ-CTRL-002, REQ-ENTITY-003 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab; matrix spread per §5.2; registry readout; video recording.
- **Procedure:** 1) Run 50 complete cycles: `OnFoot` → `EnteringVehicle` → `Driving` → park → `ExitingVehicle` → `OnFoot`. 2) For each cycle, record its matrix cell, outcome, registry counts before and after, and any `Recovery`.
- **Expected result:** **[Proposal]** 50 successful cycles with no duplication, camera loss, lost control, or traffic blocking. Registry counts return to their baseline after each cycle. Counting rules are in [TEST_STRATEGY](TEST_STRATEGY.md) §8.

#### TEST-TRANS-102 — Entry refused for moving or unavailable vehicles
- **Layer:** Integration · **Requirement:** REQ-TRANS-001, REQ-UI-003 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Career lab and Sandbox.
- **Procedure:** Try to enter: 1) a moving vehicle; 2) a vehicle reserved by a mission; 3) a native vehicle in career mode; 4) a vehicle behind a barrier; 5) a vehicle on another level (for example on a bridge above).
- **Expected result:** No entry happens. Either no prompt appears or the prompt explains why entry is refused. The player state does not change.

#### TEST-TRANS-103 — Exit on the correct level on bridges, tunnels, and stacked roads
- **Layer:** Integration · **Requirement:** REQ-TRANS-002, REQ-WORLD-001 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2.
- **Procedure:** In each cell, park and exit; record the elevation and level of the vehicle and the avatar.
- **Expected result:** The avatar exits on the same level as the vehicle. If no safe point exists on that level, the player gets feedback or `Recovery` runs; the avatar is never moved to another level.

### 6.6 Entity lifecycle (ENTITY)

#### TEST-ENTITY-001 — Handback rebuilds navigation and leaves no blockers
- **Layer:** Integration · **Requirement:** REQ-ENTITY-001 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Sandbox; matrix cells per §5.2; observation window set in the PHASE-02 document.
- **Procedure:** 1) Borrow a native vehicle while it is following a route. 2) Drive it to a different road and lane. 3) Stop and hand it back. 4) Observe the vehicle and the traffic at both the takeover and the handback locations for the observation window.
- **Expected result:** The vehicle continues on a newly computed route. It neither freezes nor tries to resume its stale path. No vehicle waits behind an empty spot at either location, and no invisible blocker remains.

#### TEST-ENTITY-002 — Career mode uses only career vehicles
- **Layer:** Integration · **Requirement:** REQ-ENTITY-002 · **Phase:** PHASE-02, PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab and Sandbox; ownership records readout.
- **Procedure:** 1) In career mode, try to use a parked citizen car, a moving citizen car, and a native service vehicle. 2) Check the ownership and garage records. 3) In Sandbox, borrow a native vehicle, exit, and check the records again.
- **Expected result:** Career mode offers no way into native vehicles, or explains the refusal. Ownership records contain only career vehicles (Rental, Owned, Service). Sandbox mode never grants ownership.

#### TEST-ENTITY-003 — Every mod entity is registered; no orphans
- **Layer:** Integration · **Requirement:** REQ-ENTITY-003 · **Phase:** PHASE-01, PHASE-05 · **Validation status:** Not run
- **Preconditions:** Career lab; registry readout with counts per entity category.
- **Procedure:** Record registry and entity counts, then perform each event and record the counts again: mission start and end, mode change, save, load, unload to the main menu, safe-disable, and disabling the mod between sessions.
- **Expected result:** Every mod entity has a registry entry with an owner and a lifecycle. After each event, the counts match what SPEC-ENTITY prescribes (despawned or persisted). There are zero orphans.

#### TEST-ENTITY-101 — Entity balance over repeated cycles
- **Layer:** Integration · **Requirement:** REQ-ENTITY-003 · **Phase:** PHASE-05, PHASE-12 · **Validation status:** Not run
- **Preconditions:** Career lab; registry readout.
- **Procedure:** 1) Run repeated mission cycles and mode switches (the count is set in the phase document). 2) Sample registry counts after each cycle.
- **Expected result:** Mod entity counts do not grow from cycle to cycle and return to the baseline after each cycle.

#### TEST-ENTITY-102 — Handback in congested traffic and at junctions
- **Layer:** Integration · **Requirement:** REQ-ENTITY-001 · **Phase:** PHASE-02 · **Validation status:** Not run
- **Preconditions:** Sandbox; matrix cells per §5.2.
- **Procedure:** Repeat TEST-ENTITY-001 with the handback inside a queue and at a junction.
- **Expected result:** The same expectations as TEST-ENTITY-001. The queue behind the handback point clears like neighboring queues, and the junction does not deadlock.

### 6.7 Time and focus (TIME)

#### TEST-TIME-001 — Simulation speed recorded and restored
- **Layer:** Integration · **Requirement:** REQ-TIME-001 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; a supported way to read simulation speed (OPEN-013).
- **Procedure:** For each simulation speed setting observed in the installed game, and for the paused state: 1) set it in `Building`; 2) enter character mode and note the speed; 3) return to `Building`.
- **Expected result:** Active exploration runs at the speed SPEC-TIME defines (normal speed, **[Proposal]**). On return, the recorded prior speed (or paused state) is restored exactly.

#### TEST-TIME-002 — Pause freezes motion and timers; focus loss clears held input
- **Layer:** Integration + In-game · **Requirement:** REQ-TIME-002 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Career lab with a timed mission (or a test timer before careers exist); matrix cells per §5.2.
- **Procedure:** 1) While driving with a running timer, pause for a while, then resume. 2) While holding throttle and steering, switch focus away from the game window, release the keys there, and switch back.
- **Expected result:** While paused, the vehicle, mission timers, and career clocks do not advance. After focus returns, no input is still held: the vehicle does not keep accelerating or steering.

#### TEST-TIME-003 — Per-frame motion is bounded
- **Layer:** Integration · **Requirement:** REQ-TIME-003 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Lab; matrix cells per §5.2; per-frame displacement telemetry; stall injection (OPEN-504).
- **Procedure:** 1) Drive at high speed toward a facade and along a bridge edge. 2) Inject a stall just before contact. 3) Repeat at a sustained reduced frame rate.
- **Expected result:** Per-frame displacement never exceeds the SPEC-TIME bound. No tunneling through geometry and no level switch occur.

#### TEST-TIME-004 — Time-based income and penalties come from persisted checkpoints
- **Layer:** Logic + Integration · **Requirement:** REQ-TIME-004 · **Phase:** PHASE-06, PHASE-09 · **Validation status:** Not run
- **Preconditions:** Logic harness with a controllable clock; Career lab with a timed mission and, from PHASE-09, a company.
- **Procedure:** 1) Logic: run sequences of elapsed time with pauses, mode switches, serialize/deserialize, and reloads of older snapshots; compare accrued amounts with values computed from in-game time. 2) In-game: run a timed mission and the company; pause; switch modes; save; reload; reload an older save.
- **Expected result:** Time-based income and penalties are never duplicated or skipped. Accrued values match the expected values. Loading an older save restores that save's checkpoint, with no accrual from the newer session.

### 6.8 Mission framework (CAREER)

#### TEST-CAREER-001 — One shared mission lifecycle
- **Layer:** Logic + Integration · **Requirement:** REQ-CAREER-001 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with the careers available in the build; mission state-trace readout.
- **Procedure:** 1) Logic: feed every event in every state and compare with the SPEC-MISSION table, including leg order `Approaching` → `Servicing` → `Done`. 2) In-game: run one mission of each available career and compare the traces.
- **Expected result:** All careers use the same states and transitions. Invalid transitions (for example `Completed` → `Active`, `Expired` → `Accepted`) are impossible.

#### TEST-CAREER-002 — Offers only after reachability validation
- **Layer:** Logic + Integration · **Requirement:** REQ-CAREER-002 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness with a route-validation test double; Career lab with a disconnected road island.
- **Procedure:** 1) Logic: make the double report one access point as unreachable and generate offers. 2) In-game: request offers repeatedly from the main network and inspect their access points.
- **Expected result:** No offer includes an access point that is not reachable from the player's position through the road network. No offer targets the island.

#### TEST-CAREER-003 — Pickups and drop-offs at validated access points
- **Layer:** Integration · **Requirement:** REQ-CAREER-003 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab with the building types from the lab checklist.
- **Procedure:** 1) Generate missions for a sample of residential, commercial, office, industrial, and service buildings and stops. 2) Inspect each pickup and drop-off marker.
- **Expected result:** Every marker is at a curbside, stop, or loading position that a vehicle can reach. None is at a building center or inside a building footprint.

#### TEST-CAREER-004 — Each terminal outcome settles exactly once
- **Layer:** Logic + Integration · **Requirement:** REQ-CAREER-004 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab; ledger readout.
- **Procedure:** 1) Logic: settle a completion, then deliver the same completion again; settle after serialize/deserialize. 2) In-game: complete a mission, save right after payment, reload, and inspect the ledger. 3) Load a save made just before completion and complete the mission again in that timeline.
- **Expected result:** Each timeline has exactly one ledger entry per terminal-outcome TransactionId. Duplicates are rejected and logged. The wallet always equals the ledger sum.

#### TEST-CAREER-005 — Cancellation and failure are explained and never unrecoverable
- **Layer:** Logic + In-game · **Requirement:** REQ-CAREER-005 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab, including a fixture with a wallet close to zero.
- **Procedure:** 1) Cancel accepted missions at each stage (before pickup, during a leg). 2) Fail missions through each failure cause SPEC-MISSION defines. 3) Repeat with the near-zero wallet.
- **Expected result:** The consequence is explained before confirmation or at the moment of failure, and it is applied exactly once. The career always remains recoverable: the starter job and basic rental stay available (see TEST-ECON-004).

#### TEST-CAREER-006 — Impossible missions detected, rerouted, or invalidated without fault
- **Layer:** Integration · **Requirement:** REQ-CAREER-006 · **Phase:** PHASE-08, PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with active missions; a development-only way to remove a career vehicle.
- **Procedure:** 1) In `Building`, demolish a destination, delete a road segment on the route, and remove a stop. 2) Remove a career vehicle through the development tool. 3) Return to character mode.
- **Expected result:** Each impossibility is detected on revalidation. The mission is rerouted when an alternative exists. Otherwise it becomes `Invalidated` and resolves to a no-fault cancellation with feedback. No mission stays impossible indefinitely.

#### TEST-CAREER-101 — Replayed completion event
- **Layer:** Logic · **Requirement:** REQ-CAREER-004, REQ-ECON-002 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness.
- **Procedure:** 1) Complete a mission. 2) Replay its completion event several times, including after a serialize/deserialize round trip and after a simulated reload.
- **Expected result:** Exactly one settlement and one ledger entry. Every replay is rejected by TransactionId and logged. The wallet is unchanged by the replays.

#### TEST-CAREER-102 — Conflicting terminal events in the same update
- **Layer:** Logic · **Requirement:** REQ-CAREER-001, REQ-CAREER-004 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness.
- **Procedure:** Deliver pairs of terminal events for the same mission in one update: complete + cancel, complete + fail, fail + invalidate, in both orders.
- **Expected result:** Exactly one terminal outcome, chosen by the precedence SPEC-MISSION defines, and exactly one settlement.

#### TEST-CAREER-103 — Randomized lifecycle sequences keep the invariants
- **Layer:** Logic · **Requirement:** REQ-CAREER-001, REQ-CAREER-004, REQ-ECON-002 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness with randomized (property-based) sequence generation and recorded seeds.
- **Procedure:** Generate random sequences of accept, leg progress, cancel, fail, invalidate, expire, duplicate events, and save/load; check the invariants after every step.
- **Expected result:** For every sequence: at most one terminal outcome per mission; no transition out of a terminal state; at most one ledger entry per TransactionId; wallet equals the ledger sum. Failing seeds are recorded in the run.

#### TEST-CAREER-104 — An offer that becomes invalid before acceptance
- **Layer:** Integration · **Requirement:** REQ-CAREER-002, REQ-CAREER-006 · **Phase:** PHASE-06, PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** 1) Receive an offer. 2) In `Building`, demolish its destination. 3) Return and try to accept it.
- **Expected result:** The offer is revalidated at acceptance and withdrawn or marked `Expired` with an explanation. It never becomes `Active` with an invalid access point.

### 6.9 Careers (TAXI, DELIV, FREIGHT, BUS)

#### TEST-TAXI-001 — Complete taxi loop with save and reload
- **Layer:** In-game · **Requirement:** REQ-TAXI-001 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab with a new profile and a rental service point.
- **Procedure:** 1) On foot, walk to the rental point and rent a vehicle. 2) Accept a fare. 3) Drive to the pickup; the passenger boards. 4) Drive to the destination; the passenger alights. 5) Receive payment. 6) Park and exit. 7) Save. 8) Close the game completely, restart, and load. 9) Accept another fare.
- **Expected result:** Every step works. The ledger has exactly one payment for the fare. After reloading, the wallet, ledger, rental state, vehicle state, and taxi XP match what SPEC-SAVE says must persist. The next fare can be accepted.

#### TEST-TAXI-002 — Passengers are mod-owned career passengers
- **Layer:** Integration · **Requirement:** REQ-TAXI-002, REQ-CITY-003 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab; Control copy; city snapshot readout.
- **Procedure:** 1) Complete several fares and watch boarding and alighting. 2) Check how passengers appear in the HUD and in any information display. 3) Compare native citizen trip indicators with the Control copy.
- **Expected result:** Passengers are registered `MissionObject`s. Boarding and alighting are represented (animation or the documented substitute). Passengers are never presented as native citizens, and no native trip is consumed.

#### TEST-TAXI-003 — Fare estimate before acceptance
- **Layer:** Logic + In-game · **Requirement:** REQ-TAXI-003 (Should) · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab.
- **Procedure:** 1) Logic: at equal service quality, check that the fare grows with route distance; check each quality modifier SPEC-TAXI defines **[Proposal]**. 2) In-game: read the estimate before accepting; complete the fare; compare the final fare with the estimate and its breakdown.
- **Expected result:** The estimate is visible before acceptance. Any difference between estimate and final fare is explained by the service-quality rules.

#### TEST-TAXI-101 — 20-ride taxi campaign (PHASE-06 proposal)
- **Layer:** Integration + In-game · **Requirement:** REQ-TAXI-001, REQ-CAREER-004, REQ-CAREER-005, REQ-SAVE-005 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab with distinct districts; ledger and registry readouts.
- **Procedure:** 1) Complete 20 rides in different districts (record the district of each). 2) Include at least one cancellation and at least one save and load during an active ride. 3) After the campaign, reconcile the ledger with the ride log.
- **Expected result:** **[Proposal]** Every terminal outcome is settled exactly once. The wallet equals the ledger sum. No passengers or markers are left behind. Counting rules are in [TEST_STRATEGY](TEST_STRATEGY.md) §8.

#### TEST-TAXI-102 — M2 fun checkpoint playtest
- **Layer:** In-game · **Requirement:** REQ-TAXI-001 (experience quality; RISK-023) · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab; the M2 build after TEST-TAXI-101 passed; the project owner as player.
- **Procedure:** 1) The owner plays the taxi career freely. 2) Notes are recorded on handling, walking, transitions, pacing, and feedback. 3) The owner gives an explicit continue or rethink judgment.
- **Expected result:** A recorded judgment by the owner. Only a positive judgment passes. A negative judgment is not a defect: it triggers a scope review before more content is added ([ROADMAP](../../ROADMAP.md) §2).

#### TEST-DELIV-001 — Urban delivery contract with deadlines, fragility, and driving evaluation
- **Layer:** Logic + In-game · **Requirement:** REQ-DELIV-001 · **Phase:** PHASE-08 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with a light delivery vehicle.
- **Procedure:** 1) Logic: compute the payment for combinations of on-time or late, smooth or rough evaluation, and fragile or robust cargo. 2) In-game: run the same multi-stop contract twice, once smoothly and on time, once with hard braking and impacts or late.
- **Expected result:** The contract shows its pickup, drop-off stops, deadlines, and fragility before acceptance. The driving evaluation is recorded, and the payment differs as SPEC-DELIVERY defines, with an explanation.

#### TEST-FREIGHT-001 — Rigid-truck freight at validated access points; mass affects handling and eligibility
- **Layer:** Logic + In-game · **Requirement:** REQ-FREIGHT-001 · **Phase:** PHASE-08 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with industrial and commercial buildings and a rigid truck.
- **Procedure:** 1) Logic: check that contracts exceeding the truck's capacity or class are ineligible. 2) In-game: load at an industrial access point, unload at a commercial one. 3) Measure braking and acceleration loaded and empty. 4) Look for any articulated-truck offer.
- **Expected result:** Loading and unloading happen only at validated access points. The loaded truck handles measurably heavier, as SPEC-FREIGHT defines. Ineligible contracts are refused with an explanation. No articulated truck is offered.

#### TEST-BUS-001 — Bus shift on a city line with career passengers
- **Layer:** Integration + In-game · **Requirement:** REQ-BUS-001 · **Phase:** PHASE-08 · **Validation status:** Not run
- **Preconditions:** Career lab with a bus line of several stops (OPEN-020); Control copy for native ridership.
- **Procedure:** 1) Start a shift on the line. 2) Serve the stops in order and watch career passengers board and alight. 3) Fill the bus to capacity. 4) Finish the trip and read the evaluation. 5) Compare native ridership with the Control copy.
- **Expected result:** Stops follow the line's order; a skipped stop is handled as SPEC-BUS defines. Passengers are career passengers; no native citizen is carried. Capacity is enforced. The trip is evaluated.

#### TEST-BUS-002 — Line or stop changes during a shift
- **Layer:** Integration · **Requirement:** REQ-BUS-002 · **Phase:** PHASE-08, PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with an active shift and passengers on board.
- **Procedure:** In `Building`, change the line in each of these ways (one run each): remove a stop, move a stop, add a stop, delete the line. Return to character mode after each.
- **Expected result:** The shift is re-planned onto the changed line where possible. Otherwise it ends with a no-fault cancellation and feedback. Passengers on board are resolved as SPEC-BUS defines, and no passenger object is lost or duplicated.

#### TEST-BUS-101 — Full-capacity stop
- **Layer:** Integration · **Requirement:** REQ-BUS-001 · **Phase:** PHASE-08 · **Validation status:** Not run
- **Preconditions:** Career lab; a stop with more waiting career passengers than free seats.
- **Procedure:** Arrive at the stop full or nearly full; complete the stop; continue.
- **Expected result:** Boarding stops at capacity. Remaining passengers are handled as SPEC-BUS defines. Passenger counts stay consistent in the HUD and the registry.

### 6.10 Economy and progression (ECON)

#### TEST-ECON-001 — Personal wallet separate from the municipal budget
- **Layer:** Integration · **Requirement:** REQ-ECON-001 · **Phase:** PHASE-06 · **Validation status:** Not run
- **Preconditions:** Career lab; Control copy; the city's financial figures as shown by the game.
- **Procedure:** 1) Record the city's treasury and financial categories. 2) Perform earning and spending transactions (fares, rental, and later fuel and repairs). 3) Compare with the Control copy over the same in-game interval.
- **Expected result:** No city financial entry is attributable to the mod. The treasury difference is within the variation between control copies. Wallet changes match the ledger.

#### TEST-ECON-002 — Append-only ledger and transaction history
- **Layer:** Logic + Integration · **Requirement:** REQ-ECON-002 · **Phase:** PHASE-06 (minimal), PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab; ledger readout.
- **Procedure:** 1) Logic: try to modify or delete an entry and to append a duplicate TransactionId; serialize and deserialize the ledger. 2) In-game: perform varied transactions; open the transaction history; compare it with the ledger readout; save, load, and compare again.
- **Expected result:** Modifications, deletions, and duplicates are rejected. Order and IDs survive the round trip. Every money movement appears once in the history, which matches the ledger before and after save/load.

#### TEST-ECON-003 — XP per career, reputation, and unlocks persist per profile
- **Layer:** Logic + Integration · **Requirement:** REQ-ECON-003 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab.
- **Procedure:** 1) Logic: award XP and reputation for completed, cancelled, and failed work and check the SPEC-ECON rules and unlock thresholds. 2) In-game: earn XP in two careers and reach an unlock; save and load. 3) Load a different lab city with its own profile.
- **Expected result:** XP is tracked per career, reputation and unlocks follow SPEC-ECON, and everything persists after reload. The other city's profile shows none of it (see TEST-SAVE-112).

#### TEST-ECON-004 — Insolvency never blocks progression
- **Layer:** Logic · **Requirement:** REQ-ECON-004 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness.
- **Procedure:** For each state: zero money and no vehicle; the lowest balance SPEC-ECON allows; an owned vehicle immobilized with zero money; an active rental with zero money. List the available actions.
- **Expected result:** In every state, the starter job and a basic rental (or a clearly explained recovery) are available.

#### TEST-ECON-005 — Economic tutorial covers wallet, costs, income, and recovery
- **Layer:** In-game · **Requirement:** REQ-ECON-005 · **Phase:** PHASE-07, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with a new profile.
- **Procedure:** Follow the economic tutorial and tick each topic as it is presented: wallet, costs, income, recovery options.
- **Expected result:** All four topics are presented, and what the tutorial says matches the actual rules in the build.

#### TEST-ECON-101 — Ledger invariants under randomized transaction sequences
- **Layer:** Logic · **Requirement:** REQ-ECON-002, REQ-CAREER-004 · **Phase:** PHASE-06, PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness with recorded seeds.
- **Procedure:** Generate random sequences of income, costs, refunds, duplicates, and save/load round trips.
- **Expected result:** TransactionIds stay unique, the ledger stays append-only, and the wallet equals the ledger sum after every step (RISK-006 indicator).

#### TEST-ECON-102 — Recovering from insolvency in-game
- **Layer:** In-game · **Requirement:** REQ-ECON-004, REQ-GARAGE-006 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Career lab with a fixture: zero money, no usable vehicle, the avatar far from any service point.
- **Procedure:** Without the console or developer help, find and use the recovery path until money is being earned again.
- **Expected result:** The player can resume earning through the starter job and basic rental. Each step is explained in the game.

### 6.11 Vehicles, ownership, and garage (GARAGE)

#### TEST-GARAGE-001 — Rental at a service point; basic rental always available
- **Layer:** Integration + In-game · **Requirement:** REQ-GARAGE-001 · **Phase:** PHASE-06 (minimal), PHASE-07 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** 1) Rent and return a vehicle at a service point. 2) Try again with zero money. 3) In `Building`, make the usual rental point inaccessible, then return.
- **Expected result:** Renting and returning work and are recorded in the ledger. With zero money, the basic rental (or an explained alternative) is offered. With the point inaccessible, the fallback SPEC-GARAGE defines is offered.

#### TEST-GARAGE-002 — Purchase and sale tied to the city's career profile
- **Layer:** Logic + Integration · **Requirement:** REQ-GARAGE-002 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with enough money; a second lab city with its own profile.
- **Procedure:** 1) Buy a vehicle; save; load. 2) Sell it. 3) Load the second lab city.
- **Expected result:** Ownership is recorded for this profile and city and survives reload. Purchase and sale each appear once in the ledger. After sale, ownership and the entity are gone. The second city shows no trace of the vehicle.

#### TEST-GARAGE-003 — Fuel consumption and refuelling
- **Layer:** Logic + In-game · **Requirement:** REQ-GARAGE-003 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with an accessible fuel service point.
- **Procedure:** 1) Logic: check the consumption model against SPEC-GARAGE. 2) In-game: drive and watch the fuel level; refuel and pay.
- **Expected result:** Fuel decreases with use as SPEC-GARAGE defines. Refuelling restores it, and the payment is recorded once in the ledger.

#### TEST-GARAGE-004 — Wear, maintenance, and repair at exterior service points
- **Layer:** Logic + In-game · **Requirement:** REQ-GARAGE-004 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with a repair service point at an ordinary building exterior.
- **Procedure:** 1) Logic: accrue wear and apply maintenance and repair. 2) In-game: drive until wear is visible in the UI; repair at the exterior service point.
- **Expected result:** Wear and repair follow SPEC-GARAGE. The service works at an exterior point without any dedicated building type in the city.

#### TEST-GARAGE-005 — Upgrades have measurable effects
- **Layer:** In-game · **Requirement:** REQ-GARAGE-005 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Career lab with the straight flat measurement road; telemetry readout; the reference speed and conditions from SPEC-GARAGE.
- **Procedure:** 1) Measure braking distance, capacity, and cornering grip several times before the upgrade. 2) Install the upgrade. 3) Repeat the measurements.
- **Expected result:** The effect has the direction and size SPEC-GARAGE defines and exceeds run-to-run variation.

#### TEST-GARAGE-006 — No repair, upgrade, part, or service can permanently block driving
- **Layer:** Logic + In-game · **Requirement:** REQ-GARAGE-006 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with fixtures for an immobilized vehicle and an inaccessible service point.
- **Procedure:** 1) Logic: enumerate block states (immobilized vehicle, empty tank far from service, unaffordable repair, inaccessible service point, required upgrade unavailable) and list the recovery actions. 2) In-game: reproduce the immobilized vehicle and the inaccessible service.
- **Expected result:** Every state has a defined recovery (transport to a service point, basic rental, or an explained alternative). No state leaves the player unable to drive.

#### TEST-GARAGE-101 — Fuel runs out during a mission far from a service point
- **Layer:** In-game · **Requirement:** REQ-GARAGE-003, REQ-GARAGE-006, REQ-CAREER-005 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Career lab with a fixture: active mission, nearly empty tank, far from fuel.
- **Procedure:** Continue driving until the tank is empty; follow the offered recovery.
- **Expected result:** The mission is resolved as SPEC-MISSION and SPEC-GARAGE define. Recovery is offered and works. No softlock.

#### TEST-GARAGE-102 — Selling or returning a vehicle that is in use
- **Layer:** Logic · **Requirement:** REQ-GARAGE-001, REQ-GARAGE-002, REQ-ECON-002 · **Phase:** PHASE-07 · **Validation status:** Not run
- **Preconditions:** Logic harness.
- **Procedure:** Try to sell an owned vehicle that has an active mission, and to return a rental in the middle of a mission.
- **Expected result:** The action is refused with an explanation or resolved as SPEC-GARAGE defines. There is no duplicate refund and no orphan vehicle record.

### 6.12 Home base, services, and company (HOME, COMPANY)

#### TEST-HOME-001 — Residential base and garage location with safe spawn and return
- **Layer:** Integration + In-game · **Requirement:** REQ-HOME-001 · **Phase:** PHASE-09 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** 1) Choose a residential base and a garage location linked to city buildings. 2) Leave and re-enter character mode. 3) Travel elsewhere and return. 4) Save and load.
- **Expected result:** The avatar spawns at a valid exterior access point of the base. Vehicles are stored at the garage location as SPEC-HOME defines. Both choices persist.

#### TEST-HOME-002 — Service interactions at exterior service points
- **Layer:** In-game · **Requirement:** REQ-HOME-002 · **Phase:** PHASE-09 · **Validation status:** Not run
- **Preconditions:** Career lab with job board, rental, repair, and contract service points.
- **Procedure:** Visit each service point and use its service.
- **Expected result:** Every service works from an exterior point. No interior is needed. Prompts appear only at valid points. Any mission character is presented as SPEC-HOME defines.

#### TEST-HOME-003 — Invalid home base or service point triggers notice and fallback
- **Layer:** Integration · **Requirement:** REQ-HOME-003 · **Phase:** PHASE-09, PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with a home base and service points chosen.
- **Procedure:** 1) In `Building`, demolish the home base building. 2) Make a service point unreachable by deleting its road. 3) Return to character mode.
- **Expected result:** The player is notified. The fallback SPEC-HOME defines is used. The avatar never spawns in an invalid or unreachable place.

#### TEST-HOME-101 — Home base invalidated between sessions
- **Layer:** Integration · **Requirement:** REQ-HOME-003, REQ-CITY-002 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with a home base.
- **Procedure:** 1) Demolish the base in `Building`. 2) Save and close the game. 3) Restart, load, and enter character mode.
- **Expected result:** The same notice and fallback as TEST-HOME-003, triggered by revalidation at load or entry.

#### TEST-COMPANY-001 — Founding and running a basic company
- **Layer:** Logic + In-game · **Requirement:** REQ-COMPANY-001 · **Phase:** PHASE-09 · **Validation status:** Not run
- **Preconditions:** Logic harness; Career lab with enough money and reputation per SPEC-COMPANY.
- **Procedure:** 1) Found a company and name it. 2) Take a contract. 3) Add a second vehicle to the fleet records. 4) Hire one employee. 5) Let salaries and costs settle.
- **Expected result:** Name, contracts, fleet records, hiring, salaries, and costs all exist and behave as SPEC-COMPANY defines. Every money movement is in the ledger.

#### TEST-COMPANY-002 — Managerial simulation is labeled; no implied physical fleet
- **Layer:** Review + In-game · **Requirement:** REQ-COMPANY-002 · **Phase:** PHASE-09 · **Validation status:** Not run
- **Preconditions:** Career lab with a company that has employees and vehicles.
- **Procedure:** 1) Inspect every company screen and message in English and PT-BR. 2) Watch the city and the map for anything representing employees or fleet vehicles.
- **Expected result:** Every relevant screen labels the simulation as managerial. No vehicle, marker, or text implies that employees physically circulate in the city.

#### TEST-COMPANY-003 — Company settlement without duplication
- **Layer:** Logic + Integration · **Requirement:** REQ-COMPANY-003, REQ-TIME-004 · **Phase:** PHASE-09 · **Validation status:** Not run
- **Preconditions:** Logic harness with a controllable clock; Career lab with a company.
- **Procedure:** 1) Logic: settle across pauses, mode switches, serialize/deserialize, and reloads of older snapshots. 2) In-game: run the company; pause; switch modes; save; reload; reload an older save.
- **Expected result:** Income and costs equal the values expected from in-game time. Ledger entries are unique. An older save shows no accrual from the newer session.

#### TEST-COMPANY-101 — Long-run company settlement soak
- **Layer:** Logic · **Requirement:** REQ-COMPANY-003 · **Phase:** PHASE-09, PHASE-12 · **Validation status:** Not run
- **Preconditions:** Logic harness with recorded seeds.
- **Procedure:** Simulate long periods of in-game time with randomized pauses, mode switches, hires, contract changes, and save/load round trips.
- **Expected result:** No drift between accrued totals and the expected values. TransactionIds stay unique.

### 6.13 Challenges and the dynamic city (CHAL, CITY)

#### TEST-CHAL-001 — Checkpoint routes and time trials with results per profile
- **Layer:** In-game + Integration · **Requirement:** REQ-CHAL-001 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab; a second lab city with its own profile.
- **Procedure:** 1) Run a checkpoint route and a time trial. 2) Save and load. 3) Load the second city.
- **Expected result:** Routes run on the city's roads. Times are measured and stored for this profile, survive reload, and do not appear in the other city's profile.

#### TEST-CHAL-002 — Parking and precision challenges
- **Layer:** In-game · **Requirement:** REQ-CHAL-002 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** Attempt each available parking and precision challenge several times, with both clean and sloppy attempts.
- **Expected result:** The challenges are available. Scoring follows SPEC-CHALLENGE and gives consistent results for equivalent attempts.

#### TEST-CHAL-003 — Job generation reflects city data
- **Layer:** Integration · **Requirement:** REQ-CHAL-003 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab; two lab cities or districts that differ in building types and transit lines.
- **Procedure:** 1) Sample offers in each. 2) In `Building`, remove an industrial area or a bus line and sample again.
- **Expected result:** The offer mix reflects building types, accessibility, transit lines, and route distances as SPEC-CHALLENGE / SPEC-CITY define. For example, removing industry reduces freight offers.

#### TEST-CITY-001 — v1.0 reads city data and writes no native economic or service state
- **Layer:** Review + Integration · **Requirement:** REQ-CITY-001 · **Phase:** PHASE-01, PHASE-10 · **Validation status:** Not run
- **Preconditions:** Source access for review; Career lab; Control copy.
- **Procedure:** 1) Review every CityBridge write path against the writes SPEC-CITY allows. 2) Run careers in the lab and compare economic and service indicators with the Control copy.
- **Expected result:** No write to native economic or service state exists or is observed. The optional native-economy experiment is absent or disabled by default.

#### TEST-CITY-002 — Revalidation after city edits
- **Layer:** Integration · **Requirement:** REQ-CITY-002 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with the avatar placed, a parked career vehicle, an active mission, a home base, and service points.
- **Procedure:** 1) Switch to `Building`. 2) Edit next to each item (demolish, delete a road, move a stop). 3) Return to character mode.
- **Expected result:** Each item is revalidated: the avatar is moved to a safe point if needed; the career vehicle is relocated or recovered; the mission is rerouted or invalidated; the home base and service points fall back. The player gets feedback for each change.

#### TEST-CITY-003 — Career passengers and cargo stay separate; no double satisfaction
- **Layer:** Integration + Review · **Requirement:** REQ-CITY-003 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab; Control copy; the settings list.
- **Procedure:** 1) Run taxi, bus, delivery, and freight missions. 2) Compare the native demand and trip indicators the game shows with the Control copy. 3) Check the default of every native-economy experiment setting.
- **Expected result:** Career deliveries satisfy no native demand, and native citizens and goods are unaffected. Any native-economy experiment is off by default and separately gated.

#### TEST-CITY-004 — No unintended persistent change to the city
- **Layer:** Integration · **Requirement:** REQ-CITY-004 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Lab; two Control copies (to measure natural variation) and one test copy.
- **Procedure:** 1) Run the Control copies without the mod for a fixed in-game interval. 2) Run the test copy for the same interval with the mod enabled and character mode used, then disable the mod. 3) Save all copies. 4) Compare buildings, roads, lines, zoning, citizens, and finances. 5) Reload the test copy without the mod.
- **Expected result:** Structural state (buildings, roads, lines, zoning) matches the controls. Statistical differences (population, finances) stay within the variation between the two controls. The test copy loads without the mod.

#### TEST-CITY-101 — Destination demolished during a mission
- **Layer:** Integration + In-game · **Requirement:** REQ-CITY-002, REQ-CAREER-006 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with an active mission (passenger or cargo on board).
- **Procedure:** 1) Switch to `Building` and demolish the mission's destination. 2) Return to character mode.
- **Expected result:** The mission is rerouted to a valid alternative or invalidated with a no-fault cancellation. The passenger or cargo is resolved as SPEC-MISSION defines. The player gets feedback, and no settlement is duplicated.

#### TEST-CITY-102 — Road removed under a parked career vehicle or the avatar
- **Layer:** Integration · **Requirement:** REQ-CITY-002, REQ-RECOV-001 · **Phase:** PHASE-10 · **Validation status:** Not run
- **Preconditions:** Career lab with a parked career vehicle and the avatar standing on a road.
- **Procedure:** 1) In `Building`, delete the road under the vehicle and under the avatar's position. 2) Return to character mode.
- **Expected result:** The vehicle is moved to a valid access point or recovered as SPEC-CITY / SPEC-RECOVERY define. The avatar is placed at a validated point. Nothing floats, falls, or remains in an invalid place.

### 6.14 Persistence and save safety (SAVE)

All TEST-SAVE tests are defined in [SAVE_SAFETY_TESTS](SAVE_SAFETY_TESTS.md).

### 6.15 Recovery and diagnostics (RECOV, DIAG)

#### TEST-RECOV-001 — Recovery moves a stuck or invalid entity to a safe point
- **Layer:** Integration + In-game · **Requirement:** REQ-RECOV-001 · **Phase:** PHASE-04, PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab with a map edge reachable by driving; a development-only way to force an invalid state.
- **Procedure:** 1) Wedge the vehicle between obstacles. 2) Drive out of bounds. 3) Force an invalid position. 4) Invoke manual `Recovery` once in each case, and also let automatic detection trigger it.
- **Expected result:** The avatar or vehicle moves to the nearest validated safe point. The player state returns to the appropriate state. Each recovery is logged. No recovery loop occurs.

#### TEST-RECOV-002 — Errors are contained
- **Layer:** Integration · **Requirement:** REQ-RECOV-002 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Lab; a development-only fault-injection switch per subsystem (OPEN-502).
- **Procedure:** 1) In character mode, inject a fault in Career, WorldContact, UI, and Persistence, one at a time. 2) After each, save, reload, and inspect the log.
- **Expected result:** The failing subsystem degrades to a safe state (for example, a return to `Building`). The game does not crash, the save reloads intact, and the error is logged with context.

#### TEST-RECOV-101 — No safe point nearby
- **Layer:** Integration · **Requirement:** REQ-RECOV-001 · **Phase:** PHASE-05 · **Validation status:** Not run
- **Preconditions:** Lab with a fixture position surrounded by invalid ground.
- **Procedure:** Trigger `Recovery` at the fixture position.
- **Expected result:** The fallback chain SPEC-RECOVERY defines is followed (for example, a more distant point, the home base, or a return to `Building`). The entity is never placed in an invalid spot.

#### TEST-DIAG-001 — Bounded logs with version and build; sanitized diagnostics bundle
- **Layer:** Integration + Review · **Requirement:** REQ-DIAG-001 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Lab; the log size bound from SPEC-MODULES / [DEBUGGING](../engineering/DEBUGGING.md).
- **Procedure:** 1) Start the game and read the log header. 2) Play a long session and watch the log size. 3) Export a diagnostics bundle. 4) Search the bundle for user names, profile paths, account IDs, store IDs, and machine names.
- **Expected result:** The log identifies the mod version and build. Its size stays within the bound. The bundle contains no personal data.

#### TEST-DIAG-002 — Safe-disable switch
- **Layer:** Integration · **Requirement:** REQ-DIAG-002 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Career lab with an active mission while driving; registry readout.
- **Procedure:** 1) Turn on safe-disable. 2) Inspect the player state, career systems, and registry. 3) Save and reload. 4) Turn safe-disable off.
- **Expected result:** Control returns to `Building`, and career systems stop. No career data is lost; the mission state is preserved or resolved as SPEC-MODULES defines. Entities are despawned or persisted correctly. Turning the switch off restores normal operation.

#### TEST-DIAG-101 — Log bound under an error storm
- **Layer:** Integration · **Requirement:** REQ-DIAG-001 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Lab; fault injection that repeats an error continuously.
- **Procedure:** Run the error storm for an extended period; watch the log size and frame time.
- **Expected result:** The size bound holds. Repeated errors are deduplicated or rate-limited as specified. Any frame-time impact is measured with the [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md).

### 6.16 Interface, onboarding, input, and localization (UI, ONBOARD, INPUT, L10N)

#### TEST-UI-001 — HUD content appropriate to each state
- **Layer:** In-game · **Requirement:** REQ-UI-001 · **Phase:** PHASE-02 (minimal), PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** Pass through `OnFoot`, `Driving`, and each mission leg; note the HUD elements shown in each.
- **Expected result:** Speed, current objective, route guidance, wallet, and interaction prompts appear as SPEC-UI defines for each state. No stale prompt remains after a state change.

#### TEST-UI-002 — Menus for career and jobs, garage, company, ledger, and settings
- **Layer:** In-game · **Requirement:** REQ-UI-002 · **Phase:** PHASE-07, PHASE-09, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with data in every area; ledger and registry readouts.
- **Procedure:** Open each menu from character mode and compare its contents with the readouts.
- **Expected result:** Every menu exists and shows data consistent with the readouts.

#### TEST-UI-003 — Clear feedback with a next step for blocked or failed actions
- **Layer:** In-game · **Requirement:** REQ-UI-003 · **Phase:** PHASE-05, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with fixtures for each case.
- **Procedure:** Trigger each case: exit blocked; unaffordable purchase or rental; invalid mission; unavailable service. Repeat in PT-BR.
- **Expected result:** Each case shows a clear message with a next step, as SPEC-UI defines, in both languages.

#### TEST-UI-004 — Map or route view on the city's roads
- **Layer:** In-game · **Requirement:** REQ-UI-004 · **Phase:** PHASE-06, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with an active mission.
- **Procedure:** 1) Open the map or route view. 2) Follow the route. 3) Force a reroute (TEST-CITY-101 setup) and reopen the view.
- **Expected result:** The destination and route are shown on the city's roads, never as a straight line across buildings. The route updates after a reroute.

#### TEST-ONBOARD-001 — A new player completes the first job without help
- **Layer:** In-game · **Requirement:** REQ-ONBOARD-001 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with a new profile; a tester who has not worked on the project (OPEN-507); a silent observer.
- **Procedure:** 1) The tester starts with only the in-game tutorial. 2) The observer counts any help given and notes points of confusion.
- **Expected result:** The tester starts exploring, rents a vehicle, and completes a first job without the console or developer help (zero interventions). Points of confusion are recorded for improvement.

#### TEST-ONBOARD-101 — Interrupted, skipped, and replayed tutorial
- **Layer:** In-game · **Requirement:** REQ-ONBOARD-001, REQ-ECON-005 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with a new profile.
- **Procedure:** 1) Save and quit in the middle of the tutorial, then reload. 2) Skip the tutorial. 3) Replay it from the menu if SPEC-ONBOARD offers that.
- **Expected result:** The tutorial resumes or restarts as SPEC-ONBOARD defines. It never gets stuck, and tutorial rewards are not granted twice.

#### TEST-ONBOARD-102 — Introduction to each career (PHASE-08 gate)
- **Layer:** In-game · **Requirement:** REQ-ONBOARD-001 (extended to every career; no dedicated requirement exists, see OPEN-509) · **Phase:** PHASE-08, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab with a profile that has never used delivery, freight, or bus.
- **Procedure:** Start each career for the first time and follow its introduction until the first contract is completed.
- **Expected result:** Each career explains its loop, constraints, and failure consequences before or during the first contract, as SPEC-ONBOARD and the career specification define. The first contract can be completed without outside help.

#### TEST-INPUT-001 — Every mod action can be remapped
- **Layer:** In-game · **Requirement:** REQ-INPUT-001 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Lab; the list of mod actions in SPEC-INPUT.
- **Procedure:** 1) Remap each action to another input and use it. 2) Restart the game and check the bindings **[Proposal: bindings persist]**.
- **Expected result:** Every listed action can be remapped and the new binding works.

#### TEST-INPUT-002 — Gamepad support in character mode
- **Layer:** In-game · **Requirement:** REQ-INPUT-002 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab; a gamepad (models used are recorded in the run); OPEN-025 resolved.
- **Procedure:** Using only the gamepad: walk the PHASE-04 circuit, complete a taxi job, and navigate every mod menu.
- **Expected result:** Everything can be done with the gamepad alone. Prompts match the active device as SPEC-INPUT defines.

#### TEST-INPUT-003 — Accessibility options
- **Layer:** In-game · **Requirement:** REQ-INPUT-003 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab.
- **Procedure:** 1) Set UI scaling to its minimum and maximum and inspect HUD and menus. 2) Trigger each important audio cue listed in SPEC-INPUT / [SPEC-AUDIO](../experience/AUDIO.md) and check its caption. 3) Capture the HUD in grayscale and check that indicators are still distinguishable. 4) Switch each hold action to toggle.
- **Expected result:** Scaling works without overlap. Every listed cue has a caption. No indicator relies on color alone. Each hold action has a working toggle alternative.

#### TEST-INPUT-101 — Remapping conflicts with game bindings
- **Layer:** In-game · **Requirement:** REQ-INPUT-001, REQ-CTRL-003 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Lab.
- **Procedure:** Remap mod actions to inputs already used by the game, both in character mode and in building mode.
- **Expected result:** The conflict is reported to the player as SPEC-INPUT defines. No binding silently stops working. **[Documented upstream]** The official key-binding documentation states that conflicting mod bindings are disabled (see [SOURCES](../research/SOURCES.md)); this is unverified by us, so the test checks that the player is informed.

#### TEST-L10N-001 — All player-facing text externalized in English and PT-BR
- **Layer:** Logic + In-game · **Requirement:** REQ-L10N-001 · **Phase:** PHASE-01 (externalization), PHASE-11 · **Validation status:** Not run
- **Preconditions:** Source and string tables; Career lab; OPEN-026 resolved.
- **Procedure:** 1) Check that every string key exists in English and PT-BR and that no player-facing text is hard-coded. 2) In-game, switch the language and traverse the HUD, menus, tutorial, and feedback messages.
- **Expected result:** No missing keys, no hard-coded text, and no untranslated placeholders in either language. Language quality is judged separately by a fluent reviewer.

#### TEST-L10N-101 — PT-BR text at the UI scaling extremes
- **Layer:** In-game · **Requirement:** REQ-L10N-001, REQ-INPUT-003 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Career lab in PT-BR.
- **Procedure:** At minimum and maximum UI scaling, open every menu and trigger the longest messages.
- **Expected result:** No truncation or overlap hides meaning.

### 6.17 Art, audio, and performance (ART, AUDIO, PERF)

#### TEST-ART-001 — Avatar animations from own or authorized assets
- **Layer:** In-game + Review · **Requirement:** REQ-ART-001 · **Phase:** PHASE-04, PHASE-11 · **Validation status:** Not run
- **Preconditions:** Lab; [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md).
- **Procedure:** 1) Observe idle, walk, run, and vehicle entry and exit (or the documented substitute transition). 2) Check that each asset used has a provenance entry.
- **Expected result:** All animations (or the documented substitute) are present. Every asset has a provenance entry showing it is own or authorized.

#### TEST-ART-002 — Placeholders are labeled
- **Layer:** Review + In-game · **Requirement:** REQ-ART-002 · **Phase:** PHASE-04 – PHASE-11 · **Validation status:** Not run
- **Preconditions:** The build; the placeholder list in SPEC-ART.
- **Procedure:** 1) Compare the placeholder list with the build. 2) Check the in-build labeling as SPEC-ART defines it. 3) Check the documentation.
- **Expected result:** Every placeholder is labeled in the build and in the documentation until it is replaced.

#### TEST-AUDIO-001 — Sounds from own or authorized sources
- **Layer:** Review + In-game · **Requirement:** REQ-AUDIO-001 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** Lab; ASSET_PROVENANCE.
- **Procedure:** 1) Trigger footsteps, engine, tire, and impact sounds. 2) Check each sound's provenance entry.
- **Expected result:** All four sound types play. Each has a provenance entry showing it is own or authorized.

#### TEST-PERF-001 — Measured against a recorded baseline with the repeatable protocol
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-03, PHASE-12 · **Validation status:** Not run
- **Preconditions:** [BENCHMARK_PROTOCOL](BENCHMARK_PROTOCOL.md) calibrated; a recorded baseline.
- **Procedure:** 1) Run TEST-PERF-101 – 104 (and the other relevant benchmark scenarios) per the protocol. 2) Compare with the baseline. 3) If a recurring frame-time regression above 10% appears, open an investigation.
- **Expected result:** Frame time, memory, and simulation speed are recorded against the baseline for every relevant change. Every trigger leads to a recorded investigation. The test checks that the protocol is followed; it does not check a frame-rate number.

#### TEST-PERF-002 — Optional, measured street-level visual enhancements
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-002 (Should) · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** BENCHMARK_PROTOCOL; the budget agreed in SPEC-PERF.
- **Procedure:** 1) Run TEST-PERF-108. 2) Check the default value of every enhancement setting.
- **Expected result:** Each enhancement can be switched off and has been measured. Any enhancement that exceeds the agreed budget is off by default.

### 6.18 Compatibility and release (COMPAT, REL)

#### TEST-COMPAT-001 — Supported combinations and known conflicts documented
- **Layer:** Review + Integration · **Requirement:** REQ-COMPAT-001 · **Phase:** PHASE-01, PHASE-12 · **Validation status:** Not run
- **Preconditions:** [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md); [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md); OPEN-023 findings.
- **Procedure:** 1) Check that every combination declared as supported has a matrix row with Passed evidence. 2) Run TEST-COMPAT-102. 3) Check that known conflicting driving and camera mods are detected or documented.
- **Expected result:** Every supported combination is backed by evidence. Known conflicts are detected or documented. This mod and the original Beta Test Driving Mod never run conflicting controllers at the same time.

#### TEST-COMPAT-101 — Candidate base-mod baseline (PHASE-00)
- **Layer:** Integration · **Requirement:** REQ-COMPAT-001 (foundation), REQ-REL-003 (incorporation audit) · **Phase:** PHASE-00 · **Validation status:** Not run
- **Preconditions:** [TASK-0002](../planning/tasks/TASK-0002-base-mod-source-audit.md) complete with a pinned revision; owner authorization for [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md); Lab.
- **Procedure:** Follow TASK-0003: 1) Build the pinned revision twice and record the build evidence separately (a build is not a runtime pass). 2) Install it in the isolated mod set. 3) Load the lab city, take control of a vehicle, drive a short route, and release it. 4) Observe the handback and record every limitation.
- **Expected result:** A reproducible build and a recorded runtime observation, including failures and limitations. Passed, Failed, and Inconclusive are all valid outcomes: a failed baseline is evidence for the reuse decision (OPEN-005).

#### TEST-COMPAT-102 — Co-loading with the original Beta Test Driving Mod
- **Layer:** Integration · **Requirement:** REQ-COMPAT-001 · **Phase:** PHASE-01 · **Validation status:** Not run
- **Preconditions:** Lab; both mods in the isolated mod set.
- **Procedure:** 1) Enable both mods and load the lab city. 2) Try to activate both controllers on the same vehicle and on different vehicles.
- **Expected result:** Conflicting controllers never run at the same time. The conflict is detected and the player is informed as [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) defines. No crash occurs.

#### TEST-COMPAT-103 — Smoke regression after a game update
- **Layer:** Integration · **Requirement:** REQ-COMPAT-001 · **Phase:** PHASE-01 onwards (every game update) · **Validation status:** Not run
- **Preconditions:** Lab on the updated game version; the smoke suite in [TEST_STRATEGY](TEST_STRATEGY.md) §11.
- **Procedure:** Run the smoke suite on fresh lab copies.
- **Expected result:** The results form a new row in [COMPATIBILITY_MATRIX](COMPATIBILITY_MATRIX.md). Every failure has an `ISSUE-NNN`. No compatibility claim is made for the new version until the suite passes.

#### TEST-REL-001 — Install and run without development tools
- **Layer:** In-game · **Requirement:** REQ-REL-001 · **Phase:** PHASE-13 · **Validation status:** Not run
- **Preconditions:** Clean release environment; an independent tester (OPEN-507); the release candidate package.
- **Procedure:** The tester follows [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) and [QUICK_START](../user/QUICK_START.md), starts the game, and enters character mode.
- **Expected result:** Installation and play succeed without any development tool.

#### TEST-REL-002 — Install, update, and clean exit tested before any safe-uninstall claim
- **Layer:** In-game · **Requirement:** REQ-REL-002 · **Phase:** PHASE-13 · **Validation status:** Not run
- **Preconditions:** Clean release environment; two consecutive candidate packages.
- **Procedure:** 1) Install the older package; play and save. 2) Update to the newer package; load the save. 3) Follow the clean-exit procedure. 4) Load the city without the mod (see TEST-SAVE-006).
- **Expected result:** Every procedure works as documented. Only after this passes may the documentation claim a safe uninstallation.

#### TEST-REL-003 — Notices and audits complete before distribution
- **Layer:** Review · **Requirement:** REQ-REL-003 · **Phase:** PHASE-00 (per incorporation), PHASE-13 · **Validation status:** Not run
- **Preconditions:** [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md), [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), the package.
- **Procedure:** For every incorporated component and asset, check its audit entry, provenance entry, and notice, and that the package contains the required notices.
- **Expected result:** No incorporated component or asset lacks an audit, a provenance entry, or a required notice.

#### TEST-REL-004 — Runtime requires only CS2 and documented dependencies
- **Layer:** Review + In-game · **Requirement:** REQ-REL-004 · **Phase:** PHASE-13 · **Validation status:** Not run
- **Preconditions:** The package and its dependency declaration; a configuration without Motor Town and Melty.
- **Procedure:** 1) Review the declared dependencies. 2) Install on the configuration and play the core loop.
- **Expected result:** Only Cities: Skylines II and the documented dependencies are needed. The mod runs without Motor Town or Melty.

#### TEST-REL-101 — Package content audit
- **Layer:** Review · **Requirement:** REQ-REL-003, REQ-REL-004 · **Phase:** PHASE-13 · **Validation status:** Not run
- **Preconditions:** The release candidate package; [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md).
- **Procedure:** List every file in the package and classify it.
- **Expected result:** No game binaries or extracted game assets (RISK-029), no personal data (RISK-030), no undocumented third-party files, and no development-only artifacts that the packaging document excludes.

## 7. Open questions referenced

OPEN-501 (logic harness), OPEN-502 (diagnostic readouts), OPEN-503 (scripted in-game driver), OPEN-504 (frame-rate
range and stall injection), and OPEN-507 (independent testers) are new entries proposed by
[TEST_STRATEGY](TEST_STRATEGY.md) §14. **OPEN-509** (new, raised here) proposes a requirement for a per-career
introduction: the [ROADMAP](../../ROADMAP.md) PHASE-08 gate asks for "a tutorial" for each career, but no `REQ-*`
covers it, so TEST-ONBOARD-102 maps to REQ-ONBOARD-001 provisionally. Existing entries used here: OPEN-005, OPEN-013, OPEN-020, OPEN-023, OPEN-025,
OPEN-026, OPEN-032, OPEN-035. All are in the [register](../planning/RISKS_AND_OPEN_QUESTIONS.md).
