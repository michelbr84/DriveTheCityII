# PHASE-03 — Surfaces, collisions, and physics

| Field | Value |
|---|---|
| ID | PHASE-03 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M1 — Playable walking/driving sandbox |
| Responsible role | Coordinator (phase lead); Implementer; QA (laboratory evidence, performance baseline); Reviewer (ADR-0004 evidence; control authority); project owner (ADR-0004 acceptance, gate acceptance) |
| Related requirements | REQ-DRIVE-002, REQ-DRIVE-003 (car class), REQ-WORLD-001, REQ-WORLD-002 (vehicle), REQ-WORLD-003, REQ-WORLD-004, REQ-TIME-001, REQ-TIME-002, REQ-TIME-003, REQ-PERF-001 (baseline) — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Do not mistake convincing visual movement for reliable physics. This phase builds a **surface and collision
laboratory**, finds out what the mod can actually query at runtime, compares two movement approaches on a single car,
and decides [ADR-0004](../../architecture/decisions/ADR-0004-physics-strategy.md) with evidence. It also implements the
time and focus rules: normal simulation speed during exploration **[Proposal]**, a defined pause behavior, input
clearing on focus loss, and bounded per-frame motion. The first performance baseline is recorded here.

A rendered road is not proof of a usable collision surface. WheelCollider compatibility and simulator-grade or
structural physics are not promised.

## 2. Entry conditions

- PHASE-02 gate **Passed** (one car driven with a single movement authority and clean handback).
- A laboratory city is available and approved (OPEN-603). It is never one of the owner's primary saves.
- Owner authorization for laboratory sessions, including frame-rate variation and benchmark runs.
- ADR-0004 is still **Proposed**. Neither approach has been chosen before this phase's experiment.

## 3. Dependencies

- **Phases:** PHASE-02.
- **Open questions:** OPEN-007 (surface information), OPEN-008 (building and static collision data), OPEN-009 (physics
  approach), OPEN-013 (simulation speed and pause control), OPEN-024 (street-level rendering; first captures), OPEN-032
  (contact rule with native traffic and pedestrians). New: OPEN-603 (source of laboratory cities).

## 4. Required reading

[SPEC-CONTACT](../../architecture/WORLD_CONTACT.md), [ADR-0004](../../architecture/decisions/ADR-0004-physics-strategy.md),
[SPEC-TIME](../../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-PERF](../../architecture/PERFORMANCE_BUDGET.md),
[SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md), [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md),
[SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md), [BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md),
[TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md), [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md),
[FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md).

## 5. Deliverables

- A laboratory scenario set: flat road, sidewalk and curb, ramp, slope, bridge, tunnel, stacked or elevated roads, and
  buildings close to the road. It is described in TEST_SCENARIOS so that QA can repeat it.
- Findings on queryable surfaces (height, normal, surface type, level) and on collision data for buildings and static
  objects, recorded in [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md).
- If they are needed: bounded `CollisionProxy` generation with measured count, memory, and lifetime, and removal on
  unload.
- ADR-0004 decided with experiment evidence, and the affected specifications updated (SPEC-CONTACT, SPEC-DRIVING).
- The chosen contact rule with native traffic and pedestrians (REQ-WORLD-004), documented in SPEC-CONTACT.
- Time and focus rules implemented as specified in SPEC-TIME.
- Car-class handling parameters, and a parameter model that later classes extend.
- A recorded performance baseline per BENCHMARK_PROTOCOL.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-03-01 | Laboratory city and scenario set | OPEN-603 | Disposable lab city; scenario list and expected results agreed with QA before any spike. |
| WP-03-02 | Surface query spike | OPEN-007, REQ-WORLD-001 | Correct level on bridges, in tunnels, and on stacked roads; never switching to the road below. |
| WP-03-03 | Building and static collision spike | OPEN-008, REQ-WORLD-002, REQ-WORLD-003, RISK-003 | Native data if available; otherwise bounded local proxies near the player. Measure cost; prove cleanup on unload. |
| WP-03-04 | Physics experiment | OPEN-009, ADR-0004, REQ-DRIVE-002 | Minimal enhanced-kinematic controller and minimal local physical controller on the same car. Compare against the ADR-0004 criteria: identical input on identical surface across the tested frame-rate range, contact quality, cost, and integration with traffic. Keep only one movement authority and remove the losing prototype. |
| WP-03-05 | Contact rule with native agents | OPEN-032, REQ-WORLD-004 | Compare candidate rules (for example, blocking, pass-through with feedback, avoidance) for safety to native agents; record the choice in SPEC-CONTACT. |
| WP-03-06 | Time and focus rules | OPEN-013, REQ-TIME-001, REQ-TIME-002, REQ-TIME-003 | Record and restore prior speed; pause freezes motion and timers; focus loss clears held input; clamp per-frame displacement. |
| WP-03-07 | Vehicle class parameter model | REQ-DRIVE-003 | Car parameters now; light delivery vehicle, rigid truck, and bus are added in PHASE-07 and PHASE-08 using the same model. |
| WP-03-08 | Performance baseline and street-level captures | REQ-PERF-001, OPEN-024, RISK-008 | Follow BENCHMARK_PROTOCOL. A recurring frame-time regression above 10% is an investigation trigger, not a budget. |
| WP-03-09 | ADR-0004 decision and gate report | Gate | Evidence package for the Reviewer; owner accepts ADR-0004 and the gate. |

## 7. Out of scope

The on-foot avatar (PHASE-04; it reuses this phase's WorldContact results). Other vehicle classes beyond the parameter
model. Structural damage, deformation, simulator-grade physics. Final street-level visual improvements (PHASE-11).
Fixed frame-rate promises.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-DRIVE-002 | In-game | Consistent handling across the tested frame-rate range | Not run |
| TEST-DRIVE-003 | In-game | Car-class parameters (other classes later) | Not run |
| TEST-WORLD-001 | In-game | Correct surface and level on all laboratory scenarios | Not run |
| TEST-WORLD-002 | In-game | Vehicle does not pass through buildings or static obstacles | Not run |
| TEST-WORLD-003 | Integration | Proxy count, memory, lifetime bounded; removed on unload | Not run |
| TEST-WORLD-004 | In-game | Chosen contact rule; native agents not corrupted | Not run |
| TEST-TIME-001 | In-game | Prior speed recorded and restored | Not run |
| TEST-TIME-002 | In-game | Pause freezes motion and timers; focus loss clears input | Not run |
| TEST-TIME-003 | In-game | No large jumps or tunneling under stalls or low frame rates | Not run |
| TEST-PERF-001 | In-game | Baseline measurement recorded ([BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md)) | Not run |

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *no passing through buildings, no level switching on bridges or
tunnels, predictable behavior after pause, focus loss, and frame-rate swings; ADR-0004 decided with evidence.* In
addition: proxy cleanup is demonstrated on unload; the contact rule with native agents is chosen and recorded; a
performance baseline exists; a Reviewer must check the ADR-0004 evidence, and the project owner must accept ADR-0004 and the gate.

## 10. Risks and open questions

RISK-003 (no usable collision data), RISK-008 (performance at street level), RISK-022 (time and pause inconsistencies),
RISK-004 (control conflicts), RISK-007 (game updates), RISK-602 (human-operated sessions). Open questions: section 3.
All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **No usable collision data and proxies are too costly.** Open a scope review for REQ-WORLD-002 with documented
  options (for example, soft boundaries with automatic `Recovery`). The owner decides; the requirement is not dropped
  silently.
- **Neither physics approach meets the ADR-0004 criteria.** Keep the approach with the better evidence as the single
  movement authority, document its limits in SPEC-DRIVING, and open a scope review of the REQ-DRIVE-002 wording.
- **Simulation speed or pause cannot be controlled in a supported way** (OPEN-013). Adapt SPEC-TIME (for example,
  rules that only observe pause) through a recorded decision, and re-test REQ-TIME-001 and REQ-TIME-002.
- **Frame-rate variation breaks consistency.** Tighten the per-frame clamp or move to a fixed-step update for movement,
  and re-run the affected laboratory scenarios.

## 12. Evidence record

None yet. Laboratory runs, the physics experiment, and the baseline benchmark will be recorded as `RUN-NNNN` under
[docs/quality/evidence/](../../quality/evidence/README.md). The ADR-0004 decision will link its runs.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
