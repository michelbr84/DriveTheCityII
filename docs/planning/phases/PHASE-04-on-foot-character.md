# PHASE-04 — On-foot character

| Field | Value |
|---|---|
| ID | PHASE-04 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M1 — Playable walking/driving sandbox |
| Responsible role | Coordinator (phase lead); Implementer; QA (evidence); Reviewer (control authority; asset provenance); project owner (asset decisions, gate acceptance) |
| Related requirements | REQ-PLAYER-001, REQ-PLAYER-002, REQ-PLAYER-003, REQ-PLAYER-004, REQ-WORLD-002 (avatar), REQ-RECOV-001 (avatar), REQ-ART-001 (first set), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Control a real character, not just a flying camera. The player controls a visible, **mod-owned `Avatar`**. The mod
never takes over a native citizen who has a household, job, or schedule. The avatar walks, runs, turns, uses free look,
and crosses small elevation changes (curbs, gentle slopes, low steps) on valid surfaces. First- and third-person cameras
are available, and the third-person camera avoids clipping. The avatar cannot pass through facades or fall below valid
ground. `Recovery` returns it to the nearest validated safe point. This phase reuses the WorldContact results of
PHASE-03, adapted to the character's size and movement.

## 2. Entry conditions

- PHASE-03 gate **Passed**: surface and collision findings documented, ADR-0004 accepted, time and focus rules in place.
- The asset provenance process is ready ([ASSET_PROVENANCE](../../legal/ASSET_PROVENANCE.md)) before any model or
  animation is used.
- Owner authorization for laboratory sessions.

## 3. Dependencies

- **Phases:** PHASE-03 (and, through it, PHASE-02 control authority).
- **Open questions:** OPEN-010 (avatar representation and animation), OPEN-024 (street-level rendering at walking
  distance), OPEN-007 and OPEN-008 (answers reused for walking surfaces and obstacles).

## 4. Required reading

[SPEC-PLAYER](../../gameplay/PLAYER_AND_CAMERAS.md), [SPEC-CONTACT](../../architecture/WORLD_CONTACT.md),
[SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
[SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md), [SPEC-ART](../../experience/ART_AND_ANIMATION.md),
[SPEC-INPUT](../../experience/CONTROLS_AND_ACCESSIBILITY.md), [ASSET_PROVENANCE](../../legal/ASSET_PROVENANCE.md),
[SPEC-PERF](../../architecture/PERFORMANCE_BUDGET.md), [BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md),
[TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 5. Deliverables

- A decision on avatar representation, with provenance recorded for every asset used. Placeholders are labeled.
- The `OnFoot` state with guards, movement, a camera owner, and transitions to and from `Building`.
- First- and third-person cameras with anti-clipping.
- Ground validity and obstacle checks for the avatar; manual and automatic `Recovery`.
- Idle, walk, and run animations, or a documented substitute.
- Street-level captures and a performance comparison against the PHASE-03 baseline.
- A representative urban circuit in the lab city, described in TEST_SCENARIOS.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-04-01 | Avatar representation spike | OPEN-010, RISK-009, REQ-PLAYER-001 | Compare own or authorized assets with a runtime representation available in the game. Check license and provenance for each option. Record the decision in SPEC-PLAYER and propose an ADR if the architecture is affected. |
| WP-04-02 | `OnFoot` state and movement | REQ-PLAYER-002, REQ-CTRL-002 | Walk, run, turn, free look, curbs, slopes, low steps, using WorldContact with character dimensions. |
| WP-04-03 | Cameras | REQ-PLAYER-003 | First and third person; anti-clipping against buildings and terrain; single camera owner. |
| WP-04-04 | Obstacles and ground validity | REQ-PLAYER-004, REQ-WORLD-002 | No passing through facades or static obstacles; no falling below valid ground. |
| WP-04-05 | Avatar recovery | REQ-RECOV-001 | Player-invoked and automatic `Recovery` to the nearest validated safe point. |
| WP-04-06 | First animation set and placeholder labels | REQ-ART-001, REQ-ART-002 | Idle, walk, run; or a documented substitute. Placeholders labeled in builds and documentation. |
| WP-04-07 | Street-level captures and performance | OPEN-024, REQ-PERF-001, RISK-008 | Measured against the PHASE-03 baseline per BENCHMARK_PROTOCOL. |
| WP-04-08 | Urban circuit and gate report | Gate | Sidewalks, ramps, a bridge, and obstacles. Reviewer and owner acceptance. |

## 7. Out of scope

Entering and exiting vehicles (PHASE-05). Parkour, swimming, climbing, and generic building interiors. Native citizens
as player characters. Final animation and art polish (PHASE-11). Gamepad support beyond what PHASE-02 probed.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-PLAYER-001 | In-game | Visible mod-owned avatar; no native citizen taken over | Not run |
| TEST-PLAYER-002 | In-game | Walk, run, turn, free look, small elevation changes | Not run |
| TEST-PLAYER-003 | In-game | First/third-person switch; third-person anti-clipping | Not run |
| TEST-PLAYER-004 | In-game | No facade pass-through; no fall below ground; `Recovery` on invalid position | Not run |
| TEST-WORLD-002 | In-game | Avatar part (vehicle part passed in PHASE-03) | Not run |
| TEST-RECOV-001 | In-game | Avatar recovery (vehicle recovery in PHASE-05) | Not run |
| TEST-ART-001 | In-game | First animation set or documented substitute | Not run |
| TEST-ART-002 | Review, In-game | Placeholder labeling | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *complete a representative urban circuit with no clipping, no
falling, and no permanent loss of control.* In addition: every asset used has a provenance record; placeholders are
labeled; recovery works when invoked and when triggered automatically; a Reviewer and the project owner must accept the gate.

## 10. Risks and open questions

RISK-009 (avatar visuals or animation infeasible), RISK-003 (collision data), RISK-008 (performance), RISK-015 (asset
provenance and branding), RISK-602 (human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **No authorized visual avatar is feasible.** Open a scope review for REQ-PLAYER-001 and REQ-ART-001 (Must). Offer the
  owner documented options, such as a simple original placeholder model that is labeled as a placeholder. The
  requirement is not dropped silently.
- **Third-person camera clipping cannot be eliminated.** Limit camera distance near geometry and fall back to the
  first-person camera in tight spaces, then re-test.
- **Small elevation changes are unreliable.** Restrict the avatar to validated walkable surfaces and use `Recovery` at
  their edges, recording the limitation in SPEC-PLAYER.
- **Performance regresses beyond the investigation trigger.** Investigate per BENCHMARK_PROTOCOL before the gate.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
