# PHASE-05 — Walk–drive integration

| Field | Value |
|---|---|
| ID | PHASE-05 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M1 — Playable walking/driving sandbox |
| Responsible role | Coordinator (phase lead); Implementer; QA (campaign evidence); Reviewer scoped to control authority and entity lifecycle (critical areas); project owner (gate and M1 acceptance) |
| Related requirements | REQ-TRANS-001, REQ-TRANS-002, REQ-TRANS-003, REQ-TRANS-004, REQ-CTRL-002, REQ-CTRL-004, REQ-ENTITY-003, REQ-RECOV-001, REQ-UI-003 (first pass), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 (M1 validation plan, §13) |

## 1. Objective

Join walking and driving into one loop: **Building → walk → enter → drive → park → exit → back to Building**. The
player selects a nearby, available, stationary vehicle through a prompt and enters it. Exiting places the avatar at a
validated safe position, tries alternate exit points, and gives feedback or triggers `Recovery` when none is safe. A
vehicle removed or invalidated during `EnteringVehicle` or `ExitingVehicle` never causes lost control, duplicated
entities, or a detached camera. Ending a drive distinguishes three outcomes: parking a career vehicle, handing back a
borrowed native vehicle (sandbox mode only), and removing temporary mod objects. Completing this phase completes M1.

## 2. Entry conditions

- PHASE-02 and PHASE-04 gates **Passed** (driving with clean handback; avatar with recovery).
- Career-vehicle spawning and removal works at spike level (OPEN-012 from PHASE-02), or a recovery plan for it exists.
- Owner authorization for the transition campaign sessions.

## 3. Dependencies

- **Phases:** PHASE-02, PHASE-04 (and PHASE-03 through PHASE-04).
- **Open questions:** OPEN-012 (safe spawning and removal of career vehicles), OPEN-011 (handback, reused), OPEN-034
  (vehicle models for career vehicles).

## 4. Required reading

[SPEC-TRANSITION](../../gameplay/WALK_DRIVE_TRANSITIONS.md), [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md),
[SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md), [SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md),
[SPEC-TIME](../../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-UI](../../experience/UI_AND_HUD.md),
[SPEC-ART](../../experience/ART_AND_ANIMATION.md), [SPEC-PLAYER](../../gameplay/PLAYER_AND_CAMERAS.md),
[SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md), [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 5. Deliverables

- The `EnteringVehicle` and `ExitingVehicle` states with guards, movement-authority and camera-owner transfer, and
  abort paths.
- Proximity selection, availability checks, and an interaction prompt.
- Safe exit placement (free space, headroom, correct elevation, preferred curb side, alternates) with blocked-exit
  feedback.
- Handling of vehicle removal or invalidation during transitions.
- The three drive-ending outcomes, with entity registry checks proving that no orphans remain.
- Return to `Building` from any stable character-mode state, restoring control, camera, and simulation speed.
- A scenario matrix and a recorded transition campaign.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-05-01 | Proximity selection and entry | REQ-TRANS-001 | Nearby, available, stationary vehicles only; interaction prompt. |
| WP-05-02 | Transition states and authority transfer | REQ-CTRL-002 | `EnteringVehicle` and `ExitingVehicle` guards; one movement authority and one camera owner at every step. Critical area: coordinated with any other control-authority work. |
| WP-05-03 | Safe exit placement | REQ-TRANS-002, REQ-UI-003 | Candidate exit points in order; validation of space, headroom, elevation, and curb side; feedback with a next step, or `Recovery`. |
| WP-05-04 | Vehicle removal during transitions | REQ-TRANS-003 | Abort safely when the vehicle is despawned, invalidated, or edited away mid-transition. |
| WP-05-05 | Ending a drive | REQ-TRANS-004, OPEN-012 | Park a career vehicle; hand back a borrowed native vehicle (sandbox only); remove temporary mod objects. |
| WP-05-06 | Return to `Building` and orphan checks | REQ-CTRL-004, REQ-ENTITY-003 | Restore control, camera, and recorded simulation speed; registry check after every mode change. |
| WP-05-07 | Vehicle recovery | REQ-RECOV-001 | Stuck or out-of-bounds vehicles move to the nearest validated safe point. |
| WP-05-08 | Scenario matrix and transition campaign | Gate | Proposed dimensions: free or blocked curb side, wall next to the door, slope, bridge deck, nearby traffic, vehicle removed mid-transition, `Paused` or `Menu` overlay mid-transition, sandbox and career vehicle. Defined with QA in TEST_SCENARIOS. |
| WP-05-09 | Gate report and M1 review | Gate | Reviewer (critical areas) and owner acceptance of PHASE-05 and milestone M1. |

## 7. Out of scope

Entering moving vehicles. Passenger seats. Vehicle interiors and cockpits (PHASE-11 decides the hood or driver camera).
Careers, rental, and payment (PHASE-06). Save/load during transitions beyond the PHASE-01 guarantees (career save/load
in PHASE-06). Final transition animations (PHASE-11).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-TRANS-001 | In-game | Proximity selection, prompt, stationary entry | Not run |
| TEST-TRANS-002 | In-game | Safe exit, alternates, blocked feedback or `Recovery` | Not run |
| TEST-TRANS-003 | Integration, In-game | Vehicle removed during a transition | Not run |
| TEST-TRANS-004 | Integration | Three drive-ending outcomes | Not run |
| TEST-CTRL-002 | Integration | All player states and overlays with guards | Not run |
| TEST-CTRL-004 | In-game | Return to `Building` from any stable state | Not run |
| TEST-ENTITY-003 | Integration | No orphans after mission end, mode change, unload, disable | Not run |
| TEST-RECOV-001 | In-game | Vehicle recovery (avatar part in PHASE-04) | Not run |
| TEST-UI-003 | In-game | Blocked-exit feedback (full coverage in PHASE-11) | Not run |
| TEST-ART-002 | Review | Placeholder labeling of transition visuals | Not run |
| TEST-TRANS-101 (transition campaign) | In-game | Proposed 50 cycles across the scenario matrix; defined in [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md) | Not run |

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): **[Proposal]** *50 successful transition cycles across the
scenario matrix, with no duplication, camera loss, or traffic blocking.* A cycle is walk → enter → drive → park or
hand back → exit. **[Proposal]** The campaign is recorded in one or more runs; any critical failure (duplicated entity,
lost control, detached camera, avatar placed inside geometry, traffic blocker) is fixed and the campaign restarts from
zero. In addition: the registry shows no orphans after the campaign; return to `Building` restores the recorded
simulation speed; a Reviewer scoped to the critical areas and the project owner must accept the gate and milestone M1.

## 10. Risks and open questions

RISK-004 (control conflicts), RISK-018 (entities invalidated by edits; first contact here), RISK-020 (input
conflicts), RISK-014 (concurrent critical edits), RISK-013 (unverified work reported as complete), RISK-602
(human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **Exit placement is unreliable in some geometry.** Refuse the exit with clear feedback in that geometry and offer
  `Recovery`, record the limitation in SPEC-TRANSITION, and extend the scenario matrix. If REQ-TRANS-002 cannot be met
  in common city layouts, open a scope review.
- **Vehicle removal cannot be detected in time.** Revalidate the vehicle on every transition step and abort on the
  first failed check; re-run the removal scenarios.
- **Career-vehicle parking leaves residual entities.** Block the gate until the registry check passes; fix the
  despawn path (SPEC-ENTITY).
- **The campaign keeps failing on one scenario class.** Isolate it, fix it, and restart the campaign. Do not exclude
  the scenario from the matrix without a recorded owner decision.

## 12. Evidence record

None yet. Campaign runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md),
with a cycle log that QA can reconcile against the registry counts.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |

## 13. M1 playable-loop validation plan (refined 2026-10-18)

The milestone loop ([ROADMAP](../../../ROADMAP.md) §3.1):

> Load city → enter on-foot mode → walk → enter a supported vehicle → drive, steer, and brake → stop → exit → continue
> walking → return to city management.

It runs in the lab city ([WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) §8), in sandbox mode. All targets are
**[Proposal]**. Validation status is **Not run** for everything.

### 13.1 What must be demonstrated, and how

| Area | What M1 must show | Evidence method | Tests (existing IDs) |
|---|---|---|---|
| 1. Real player-controlled movement | The avatar walks and runs, and the vehicle accelerates, steers, brakes, reverses, and stops **only** in response to the player's input. No native AI moves the controlled entity. | Recorded input log next to position samples; video capture kept local (OPEN-407) | TEST-PLAYER-001, TEST-PLAYER-002, TEST-PLAYER-003, TEST-DRIVE-001, TEST-DRIVE-002, TEST-CTRL-001 |
| 2. Collision and vehicle interaction | The avatar and the vehicle do not pass through buildings or static obstacles and stay on the correct level on bridges and in tunnels. Contact with native traffic follows the PHASE-03 rule. Entry uses proximity and a prompt, and exit finds a safe point. | Lab scenario matrix S1–S8 ([TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md)) | TEST-WORLD-001, TEST-WORLD-002, TEST-WORLD-004, TEST-PLAYER-004, TEST-TRANS-001, TEST-TRANS-002 |
| 3. Correct input ownership when changing modes | In every state, exactly one owner of movement, camera, and input. Driving input never moves the building camera or triggers city tools; Building input never moves the avatar or vehicle. Rapid or conflicting switches are handled. | State-transition log; tool-activation check | TEST-CTRL-002, TEST-CTRL-003, TEST-CTRL-004, TEST-CTRL-101, TEST-CTRL-102 |
| 4. Continued city simulation | While on foot or driving, the city keeps simulating at the recorded speed: the in-game clock advances, and native traffic around the player keeps moving. A borrowed car rejoins native traffic after handback. On return, the prior speed is restored. The mod leaves no unintended persistent change. | Clock and simulation-speed samples before, during, and after; traffic observation window; before/after city comparison | TEST-TIME-001, TEST-TIME-002, TEST-TIME-003, TEST-ENTITY-001, TEST-DRIVE-101, TEST-CITY-004 |
| 5. Recovery when an interaction cannot complete | A blocked exit gives feedback and alternatives. A vehicle removed mid-transition aborts safely. A stuck avatar or vehicle reaches a safe point through `Recovery`. A subsystem fault degrades to `Building`. | Forced-failure scenarios in the lab city | TEST-TRANS-003, TEST-UI-003, TEST-RECOV-001, TEST-RECOV-101, TEST-RECOV-002 |
| 6. Save/load behavior required by M1 | A save made at any point of the loop, including autosaves, produces a city that loads. After a reload the player is in `Building` or a safe `OnFoot` state, never inside a transition. No borrowed vehicle stays frozen or possessed, and no mod object is orphaned. A city saved with the mod still loads with the mod disabled. M1 persists **no** career data. | Save at each loop step, reload, and inspect the registry and the vehicle | TEST-SAVE-113, TEST-SAVE-115, TEST-SAVE-108, TEST-ENTITY-003, TEST-TRANS-004 |
| Whole loop | The loop completes repeatedly across the scenario matrix without duplication, lost control, a detached camera, or traffic blockers. | Campaign | TEST-TRANS-101 (proposed 50 cycles) |

### 13.2 Existing capability versus work to implement

**Nothing of this loop exists in Drive the City II yet.** The only existing code is the candidate base mod, audited
at source level by TASK-0002 at the pinned commit `eefd6bb…` ([BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) §9).
It is unverified on game 1.6.2f1, because it declares 1.5.*. The audit recommends using it as a runtime baseline and a
source of techniques, **not** as a fork: selective re-implementation, with code reuse limited to small pure functions
and collision kept concept-only (owner decision after TASK-0003).

| Loop step | Candidate base mod (source level, unverified at runtime) | Work for Drive the City II |
|---|---|---|
| Load city; return to city management | A possess and release toggle on a live vehicle while the city runs. It does not change simulation speed and has no state machine. | `Building` state and overlays, plus clean return with speed restoration (SPEC-CONTROL, SPEC-TIME) |
| Enter on-foot mode; walk | Not present | Avatar, walking and running, cameras, surface and level tracking (PHASE-03, PHASE-04; OPEN-010) |
| Enter a supported vehicle | Takes over the selected or nearest **any** live car, including service vehicles, directly and with no on-foot approach. Career vehicles cannot be spawned. | Proximity selection, availability check, borrowable-class filter (OPEN-415), prompt, entry transition (PHASE-05) |
| Drive, steer, brake, stop | Kinematic driving: speed ramps, yaw rate, reverse, and lane-height snapping. Native navigation is frozen and the pose is overwritten after the native move step. There is a chase camera, and collision covers **moving cars only**: no buildings, pedestrians, or parked cars. | Re-implement under one movement authority; add building and surface contact (PHASE-02, PHASE-03; ADR-0004); decide the time base (OPEN-414) |
| Exit; continue walking | Release marks path and lane state for rebuilding, with no stop requirement and no verification. No avatar exists. | Stationary handback with verification (REQ-ENTITY-001), safe exit placement, alternates, blocked feedback, `Recovery` (PHASE-05) |
| Input ownership | Legacy key polling in every context, with bindings in player preferences. It does not suppress game input and has no gamepad support. | Input routing per state and remappable bindings via the documented mechanism (OPEN-014, OPEN-025) |
| Save/load | No save integration, and no handback on save, load, or dispose: a save while possessing may persist a frozen car (OPEN-412, RISK-413) | M1 save behavior above, with handback-ready borrowed vehicles; foundation from PHASE-01 (ADR-0003) |

### 13.3 Concrete prerequisites before M1 work starts

1. PHASE-00 gate passed, including TASK-0003 evidence that the native route builds and loads on the installed game.
2. PHASE-01 foundation: TASK-0004 skeleton, co-load guard, diagnostics, safe-disable, and the save foundation decision.
3. Owner decisions: license (OPEN-027), Git (OPEN-029), output location (OPEN-028), and acceptance of ADR-0005.
4. Toolchain installed with owner authorization: the Unity Editor version that matches game 1.6.2f1 (OPEN-406), the
   .NET SDK, and possibly a Unity license.
5. PHASE-03 decisions: physics approach (ADR-0004) and traffic contact rule (OPEN-032).
6. PHASE-04 decision: avatar representation with recorded asset provenance (OPEN-010).
