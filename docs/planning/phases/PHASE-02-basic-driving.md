# PHASE-02 — Basic driving

| Field | Value |
|---|---|
| ID | PHASE-02 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M1 — Playable walking/driving sandbox |
| Responsible role | Coordinator (phase lead); Implementer; QA (evidence); Reviewer scoped to control authority and entity lifecycle (critical areas); project owner (gate acceptance) |
| Related requirements | REQ-DRIVE-001, REQ-CTRL-001, REQ-CTRL-002, REQ-CTRL-003, REQ-ENTITY-001, REQ-ENTITY-002, REQ-UI-001 (minimal) — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Let the player directly drive **one car** without fighting the native AI. In sandbox mode, the player takes over a
native vehicle (a `BorrowedNativeVehicle`) and drives it with throttle, brake, reverse, progressive steering, a parking
brake, a speedometer, and a chase camera. Driving input never moves the building camera or triggers city-building
tools. Exactly one movement authority writes to the vehicle at any time. Releasing the car (**handback**) rebuilds its
navigation and clears lane state, so it rejoins traffic without a stale route or an invisible blocker.

The movement model in this phase is provisional. [ADR-0004](../../architecture/decisions/ADR-0004-physics-strategy.md)
is decided in PHASE-03. Whether vehicle control reuses, adapts, or re-implements base-mod code follows the PHASE-00
recommendation and the license decision.

## 2. Entry conditions

- PHASE-01 gate **Passed** (module skeleton, diagnostics, safe-disable, save foundation, co-load guard).
- The PHASE-00 reuse recommendation is recorded. If base-mod code is reused, its license entry is complete and its
  notices are in [THIRD_PARTY_NOTICES](../../../THIRD_PARTY_NOTICES.md).
- Owner authorization for lab-city runtime sessions in this phase.

## 3. Dependencies

- **Phases:** PHASE-01; PHASE-00 findings (base-mod takeover and handback approach, limitations list).
- **Open questions:** OPEN-011 (clean takeover and handback), OPEN-012 (career-vehicle spawning; first spike), OPEN-014
  (input integration), OPEN-025 (early gamepad probe), OPEN-034 (vehicle models for career vehicles).
- **Decisions:** [ADR-0002](../../architecture/decisions/ADR-0002-control-authority.md) (control authority) guides the
  implementation; changes to it need a Reviewer scoped to the critical area.

## 4. Required reading

[SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
[ADR-0002](../../architecture/decisions/ADR-0002-control-authority.md), [SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md),
[SPEC-INPUT](../../experience/CONTROLS_AND_ACCESSIBILITY.md), [SPEC-UI](../../experience/UI_AND_HUD.md) (minimal HUD),
[SPEC-TIME](../../architecture/GAME_TIME_AND_PAUSE.md) (awareness only), [SPEC-MODULES](../../architecture/MODULE_CONTRACTS.md),
[BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md), [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md),
[TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 5. Deliverables

- `Building` ↔ `Driving` transitions (sandbox) with entry and exit guards, a single camera owner, and full restoration
  of the building camera and input on exit.
- Takeover of a `BorrowedNativeVehicle` that suspends native movement authority while the player drives.
- Driving controls and a minimal HUD (speed, prompts).
- A handback protocol with route reconstruction and lane-state cleanup, plus an observation method that QA can repeat.
- A spike result for spawning and removing a mod-owned `CareerVehicle` (no ownership of native vehicles).
- A validation route in the lab city (curves, junctions, slopes), described in TEST_SCENARIOS.
- Findings recorded in [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md).

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-02-01 | Control authority for `Building` ↔ `Driving` | REQ-CTRL-001, REQ-CTRL-002 | Guards, authority handover, camera owner. Diagnostics counters that show the active movement authority, so "one writer" is checkable from logs. |
| WP-02-02 | Takeover of a borrowed native vehicle (sandbox only) | REQ-CTRL-001, REQ-ENTITY-002, OPEN-011 | Suspend native movement while driving. Sandbox mode never grants ownership. |
| WP-02-03 | Driving controls and minimal HUD | REQ-DRIVE-001, REQ-UI-001 | Throttle, brake, reverse, progressive steering, parking brake, speedometer. Provisional movement model. |
| WP-02-04 | Chase camera | REQ-DRIVE-001, REQ-CTRL-002 | Takes and returns camera ownership; restores the prior building camera. |
| WP-02-05 | Input separation and early gamepad probe | REQ-CTRL-003, OPEN-014, OPEN-025, RISK-020 | No building tool or camera reacts while driving. The probe only records whether gamepad input is readable; full support is PHASE-11. |
| WP-02-06 | Handback protocol | REQ-ENTITY-001, OPEN-011, RISK-004 | Rebuild navigation, clear lane reservations and blocking state. Observe the released vehicle and the traffic around it for a pre-declared window. |
| WP-02-07 | Career-vehicle spawn spike | REQ-ENTITY-002, OPEN-012, OPEN-034 | Spawn and remove one mod-owned vehicle; entity counts before and after; check model-use rules (OPEN-034). |
| WP-02-08 | Validation route and gate report | Gate | Route with curves, junctions, and slopes in the lab city. Reviewer (critical areas) and owner acceptance. |

## 7. Out of scope

Building and static-object collisions, surface correctness on bridges, tunnels, and stacked roads, and the physics
decision (all PHASE-03). The on-foot avatar (PHASE-04) and entering or exiting vehicles on foot (PHASE-05). Careers,
economy, rentals. Full gamepad and remapping support (PHASE-11). Time and pause rules beyond not breaking them (PHASE-03).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-DRIVE-001 | In-game | Car controls, speedometer, chase camera | Not run |
| TEST-CTRL-001 | Integration, In-game | One movement authority per controlled vehicle | Not run |
| TEST-CTRL-002 | Integration | `Building` and `Driving` states with guards and camera owner | Not run |
| TEST-CTRL-003 | In-game | Input separation between modes | Not run |
| TEST-ENTITY-001 | In-game | Handback: no stale route, no invisible blocker | Not run |
| TEST-ENTITY-002 | Integration | Sandbox borrowing never grants ownership; career uses only career vehicles (spike level) | Not run |
| TEST-UI-001 | In-game | Minimal HUD (speed, prompts) | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *drive a validation route (curves, junctions, slopes); the
handed-back vehicle rejoins native traffic with no stale route or invisible blocker.* In addition:

1. Recorded runs show no city-building tool or building-camera movement while driving (TEST-CTRL-003).
2. Diagnostics show a single movement authority throughout each run (TEST-CTRL-001).
3. After handback, the released vehicle and the traffic behind it keep moving during the observation window
   (TEST-ENTITY-001).
4. Reviewer check scoped to control authority and entity lifecycle, and owner acceptance.

## 10. Risks and open questions

RISK-004 (AI and player control conflict), RISK-020 (input conflicts), RISK-001 and RISK-007 (game-version coupling of
takeover code), RISK-016 (other driving or camera mods), RISK-014 (concurrent critical edits), RISK-602 (human-operated
sessions). Open questions: section 3. All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **Handback cannot be made clean.** Fallback **[Proposal]**: sandbox mode stops borrowing native vehicles and uses
  only mod-spawned vehicles, which are removed instead of handed back. Because REQ-ENTITY-001 is a Must requirement,
  this fallback needs a recorded scope review and owner decision.
- **Input cannot be separated reliably.** Route mod input through the game's supported binding mechanism (OPEN-014) or
  suspend building tools explicitly while in `Driving`. If neither works, open a scope review.
- **Reused base-mod code is unstable** (for example, native crashes). Re-implement that subsystem independently,
  keeping only the concepts the license permits, and record the change in [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md).
- **Career-vehicle spawning fails** (OPEN-012). This does not block this gate. It must be solved by PHASE-05, which
  ends drives by parking career vehicles.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md) with
[TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
