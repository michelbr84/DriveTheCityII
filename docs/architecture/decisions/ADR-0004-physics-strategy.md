# ADR-0004: Physics Strategy for Controlled Vehicles and the Avatar

| Field | Value |
|---|---|
| ID | ADR-0004 |
| ADR status | Proposed |
| Date proposed | 2026-10-18 |
| Decision owner role | Coordinator with Reviewer (evidence evaluation); project owner accepts |
| Related | REQ-DRIVE-002, REQ-DRIVE-003, REQ-WORLD-001 to REQ-WORLD-004, REQ-TIME-003, REQ-PERF-001, [SPEC-CONTACT](../WORLD_CONTACT.md), [SPEC-TIME](../GAME_TIME_AND_PAUSE.md), [SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md), [PHASE-03](../../planning/phases/PHASE-03-surfaces-collisions-and-physics.md), OPEN-007, OPEN-008, OPEN-009, OPEN-032, RISK-003, RISK-008 |

## Context

The owner wants consistent, semi-realistic driving with useful collisions and distinct vehicle classes, not
simulator-grade or structural physics **[Owner requirement]**. A rendered road is not proof of a usable collision
surface, and it is unknown what surface and collision information the host provides at runtime (OPEN-007, OPEN-008).
The earlier plan proposed comparing an enhanced kinematic controller with a local physical controller on a single car
and keeping only one movement authority **[Historical input]** ([chat.md](../../../chat.md)).

The candidate base mod is reported to use a kinematic model with clamped time steps and vehicle-only collision, and to
document that building collision is not implemented **[Source-inspected, pending re-check in TASK-0002]**,
**[Documented upstream]** ([BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md)). That is a reference point, not evidence
for either option in our context.

## Decision drivers (evaluation criteria)

| Criterion | What is measured |
|---|---|
| C1 Consistency across frame rates | Braking distance, top speed, and slalom path deviation at uncapped, 60, and 30 FPS caps and with injected stalls (REQ-DRIVE-002, REQ-TIME-003) |
| C2 Surface fidelity | Contact height error and level-switch count on slopes, curbs, bridges, tunnels, stacked roads (REQ-WORLD-001) |
| C3 Collision usefulness | Penetration count and depth against facades and static obstacles; plausibility of the response (REQ-WORLD-002) |
| C4 Performance cost | Mod update time and frame-time change in B3 and B4 configurations ([SPEC-PERF](../PERFORMANCE_BUDGET.md)) |
| C5 Integration risk with traffic | Native vehicles left stuck, stale presence entries, contact incidents per route (REQ-WORLD-004, OPEN-032) |
| C6 Maintainability | Dependence on host internals, code size, tuning effort for the four vehicle classes, exposure to game updates (RISK-007) |

## Options considered

| Option | Description | Expected strengths | Expected risks |
|---|---|---|---|
| A. Enhanced kinematic controller | The mod integrates a vehicle model (speed, steering, grip per surface, load, class parameters) in fixed substeps and resolves contact through WorldContact sweeps; it writes the pose directly | Deterministic, frame-rate independent by construction; no dependency on engine physics in the host context; simpler to keep a single authority | Collision response and weight transfer must be authored; may feel less dynamic |
| B. Local physical controller | A local rigid-body simulation around the controlled vehicle (engine physics if usable in the host context, or our own force integrator), with colliders from host geometry or proxies | More dynamic feel; natural collision response | Requires physics colliders for the surroundings (proxies at higher cost); engine physics availability and determinism unverified; **no promise that WheelCollider-style components work in the host** |
| Reference R. Base mod's model as found | Used only as a measurement baseline if the base mod builds and runs (OPEN-005) | Already exists | Not a candidate by itself; known gaps (no building collision) |

A hybrid is not an option to pre-plan: whichever option wins remains the single movement authority. Elements of the
loser may be reused only as sub-components under that authority, and only if recorded here.

## Decision

**Pending — PHASE-03 experiment.** No option is selected or preferred before the experiment. Until the decision,
specifications describe both options neutrally.

## Consequences

- Until decided: PHASE-02 may drive with a minimal controller behind the Vehicles module interface, explicitly labeled
  as temporary; it does not count as a choice.
- Either outcome keeps one movement authority ([ADR-0002](ADR-0002-control-authority.md)), bounded per-frame motion
  ([SPEC-TIME](../GAME_TIME_AND_PAUSE.md)), and bounded proxies ([SPEC-CONTACT](../WORLD_CONTACT.md)).
- If option B is chosen, collision proxies become physics colliders and their budget must be re-measured.
- If neither option qualifies, REQ-DRIVE-002 or REQ-WORLD-002 goes to an owner scope review; it is not dropped silently.

## Validation / experiment

Run in the PHASE-03 laboratory ([SPEC-CONTACT](../WORLD_CONTACT.md) section 11, scenarios S01 to S14) in a lab city:

1. Implement both options minimally for **one car class**, behind the same Vehicles interface, with the same input
   mapping and the same WorldContact queries. Only one option is active at a time.
2. Run every scenario at each frame-rate condition, at least three times per option; record `RUN-*` evidence
   ([evidence](../../quality/evidence/README.md)).
3. Record raw measurements for C1 to C6; write findings to [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md).
4. Prepare a blind or at least side-by-side driving session for the owner on the same routes (feel is part of
   RISK-023).

**Decision gate:**

- **Must pass (both options are checked):** C1 within the tolerance set before the runs; zero level switches in C2
  scenarios; no penetration beyond the tolerance in [SPEC-CONTACT](../WORLD_CONTACT.md); zero native vehicles left
  stuck in C5 runs. An option that fails any must-pass criterion is not eligible.
- **Then compare** eligible options with weights agreed with the owner **before** the runs. Proposed starting weights:
  C1 25, C2 20, C3 20, C4 15, C5 10, C6 10 **[Proposal]**.
- The Reviewer checks the evidence; the project owner accepts the outcome, and this ADR records the result, the runs,
  and the rejected option's reasons.

Validation status of every step: **Not run**.

## Affected specifications

[SPEC-CONTACT](../WORLD_CONTACT.md), [SPEC-TIME](../GAME_TIME_AND_PAUSE.md), [SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md),
[SPEC-PLAYER](../../gameplay/PLAYER_AND_CAMERAS.md) (avatar contact reuse), [SPEC-PERF](../PERFORMANCE_BUDGET.md),
[SPEC-MODULES](../MODULE_CONTRACTS.md) (Vehicles).

## Follow-ups

- Before the runs: fix tolerances and weights with the owner and record them here.
- After the decision: answer OPEN-009 in the register, update every affected spec, and extend tuning to the four
  vehicle classes (REQ-DRIVE-003) in PHASE-03, PHASE-07, and PHASE-08.
