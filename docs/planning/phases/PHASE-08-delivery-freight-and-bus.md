# PHASE-08 — Deliveries, freight, and bus

| Field | Value |
|---|---|
| ID | PHASE-08 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M3 — Content alpha |
| Responsible role | Coordinator (phase lead); Implementer; QA (per-career campaigns); Reviewer; project owner (articulated-truck spike authorization, gate acceptance) |
| Related requirements | REQ-DELIV-001, REQ-FREIGHT-001, REQ-BUS-001, REQ-BUS-002, REQ-CAREER-006 (first pass), REQ-DRIVE-003 (remaining classes), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Reuse the mission framework for three more careers, each with its own loop, constraints, feedback, recovery, and
persistence:

- **Urban delivery:** a pickup, one or more drop-off stops, deadlines, cargo fragility, and a driving evaluation that
  affects payment.
- **Rigid-truck freight:** loading and unloading at validated industrial or commercial access points; cargo mass and
  capacity affect handling and eligibility. **Articulated trucks are not part of v1.0** (CONF-003).
- **Bus:** an existing city transit line's stops in order, with **mod-owned career passengers** boarding and alighting,
  vehicle capacity, and a trip evaluation. Native citizens are not carried in v1.0 (CONF-011).

City edits that affect a destination, line, or stop lead to replanning or a no-fault cancellation.

## 2. Entry conditions

- PHASE-06 gate **Passed**. PHASE-07 **Passed** before any content that depends on ownership (for example, buying a
  truck or bus). Career logic may start earlier behind stable contracts ([ROADMAP](../../../ROADMAP.md) §5).
- Owner authorization for the per-career campaigns. A **separate, explicit owner authorization** is needed before any
  articulated-truck spike.

## 3. Dependencies

- **Phases:** PHASE-06; PHASE-07 for ownership-dependent content; PHASE-03 parameter model for vehicle classes.
- **Open questions:** OPEN-020 (reading transit lines and stops), OPEN-018 (access points for loading bays and stops),
  OPEN-021 (detecting city edits; the fallback of full revalidation on return from `Building` is acceptable here),
  OPEN-034 (vehicle models).
- **Conflicts:** CONF-003 (articulated trucks), CONF-011 (native bus passengers).

## 4. Required reading

[SPEC-DELIVERY](../../gameplay/careers/DELIVERIES.md), [SPEC-FREIGHT](../../gameplay/careers/FREIGHT.md),
[SPEC-BUS](../../gameplay/careers/BUS.md), [SPEC-MISSION](../../gameplay/MISSION_FRAMEWORK.md),
[SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md),
[SPEC-CHALLENGE](../../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) (revalidation), [SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md),
[EXPANSIONS](../../product/EXPANSIONS.md), [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 5. Deliverables

- Handling parameters and capacity rules for the light delivery vehicle, rigid truck, and bus, using the PHASE-03
  parameter model.
- The delivery, freight, and bus careers with reachable contracts, completion, valid failure paths, reward, and
  persistence for each, plus a short first-contract introduction per career (design guidance from SPEC-ONBOARD; not a
  gate criterion, CONF-013).
- Findings on transit-line reading recorded in [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md).
- Replanning or no-fault cancellation when a destination, line, or stop changes.
- If authorized: an articulated-truck spike report filed under expansions, not v1.0.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-08-01 | Vehicle classes | REQ-DRIVE-003 | Light delivery vehicle, rigid truck, bus: distinct mass, acceleration, braking, turning, capacity. |
| WP-08-02 | Urban delivery career | REQ-DELIV-001 | Multi-stop legs, deadlines, fragility, driving evaluation. |
| WP-08-03 | Rigid-truck freight career | REQ-FREIGHT-001, OPEN-018 | Industrial and commercial loading access points; mass and capacity affect handling and eligibility. |
| WP-08-04 | Transit line reading spike | OPEN-020 | Lab city with modified lines; stop order and stop access positions. |
| WP-08-05 | Bus career with career passengers | REQ-BUS-001, CONF-011 | Mod-owned passengers; capacity; trip evaluation; never presented as native citizens. |
| WP-08-06 | Replanning and no-fault cancellation | REQ-BUS-002, REQ-CAREER-006, OPEN-021, RISK-018 | Destination demolished, line or stop changed during a shift. Full revalidation on return from `Building` is the baseline method. |
| WP-08-07 | First-contract introductions per career | [SPEC-ONBOARD](../../experience/ONBOARDING.md), CONF-013 | Short guided first contract; final onboarding in PHASE-11. Not a gate criterion. |
| WP-08-08 | Articulated-truck exploratory spike (optional) | CONF-003, [EXPANSIONS](../../product/EXPANSIONS.md) | **Non-gating.** Only with explicit owner authorization. Results go to the expansion record and the feasibility matrix. It never enters v1.0 scope silently. |
| WP-08-09 | Per-career campaigns and gate report | Gate | Each career: reachable contract, completion, failure, reward, persistence after reload, and an edit scenario. |

## 7. Out of scope

Articulated trucks in v1.0. Native citizens as bus passengers, native fares, and schedules (expansion). Physical cargo
simulation. Writes to the native economy or satisfaction of native demand (PHASE-10 governs the boundary). Company
contracts (PHASE-09).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-DELIV-001 | Logic, In-game | Delivery contracts with stops, deadlines, fragility, evaluation | Not run |
| TEST-FREIGHT-001 | In-game | Rigid-truck freight at validated access points; mass and capacity effects | Not run |
| TEST-BUS-001 | In-game | Line stops in order; career passengers; capacity; evaluation | Not run |
| TEST-BUS-002 | In-game | Line or stop change during a shift | Not run |
| TEST-CAREER-006 | Integration, In-game | Impossible missions detected and rerouted or cancelled (first pass) | Not run |
| TEST-DRIVE-003 | In-game | Remaining classes are distinct | Not run |
| TEST-ART-002 | Review | Placeholder labeling | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *each career has reachable contracts, completion,
valid failure paths, reward, and persistence; city edits lead to replanning or safe cancellation.* Per-career
introductions do not gate this phase (CONF-013). Each career has its own
recorded campaign. Each terminal outcome is settled exactly once (ledger reconciliation). Any articulated-truck spike
is excluded from the gate. A Reviewer and the project owner must accept the gate.

## 10. Risks and open questions

RISK-018 (city edits invalidate missions), RISK-006 (duplicated rewards), RISK-012 (scope creep: articulated trucks,
native passengers), RISK-003 (collision behavior of larger vehicles), RISK-008 (performance with more mission
objects), RISK-602 (human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **Transit lines cannot be read reliably** (OPEN-020). Open a scope review of REQ-BUS-001 with documented options. The
  owner decides; the bus career is not replaced silently with a different mechanic.
- **Larger vehicles behave badly with the PHASE-03 contact approach.** Tune class parameters first; if that fails,
  revisit the affected part of SPEC-CONTACT through an ADR update.
- **One career is not ready.** The gate covers all three careers. Keep the phase open for that career; do not pass the
  gate partially without a recorded owner decision.
- **Edits leave missions impossible.** Apply full revalidation on every return from `Building` until OPEN-021 is
  answered.

## 12. Evidence record

None yet. Per-career campaign runs will be recorded as `RUN-NNNN` under
[docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
