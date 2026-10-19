# PHASE-10 — Challenges and dynamic city

| Field | Value |
|---|---|
| ID | PHASE-10 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M4 — Refined and validated beta |
| Responsible role | Coordinator (phase lead); Implementer; QA (edit-during-career campaign); Reviewer (city-integration boundary, save format); project owner (authorization of any native-economy experiment, gate acceptance) |
| Related requirements | REQ-CHAL-001, REQ-CHAL-002, REQ-CHAL-003, REQ-CITY-001, REQ-CITY-002, REQ-CITY-003, REQ-CAREER-006, REQ-BUS-002, REQ-HOME-003, REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Use the project's main differentiator: working in the city the player built. Add **checkpoint routes, time trials,
and parking and precision challenges** on the player's own roads. Generate jobs from city data (building types,
accessibility, transit lines, route distances), so that the built city shapes the available work. Handle **city edits
safely**: after the player edits the city in `Building`, returning to character mode revalidates the avatar position,
career vehicles, missions, home base, and service points, and reroutes or cancels without fault.

Reading city data is not the same as changing the simulation. v1.0 does not write native economic or service state.
Career passengers and cargo never satisfy native demand. Any native-economy write is a **separately gated, optional
experiment that is off by default**.

## 2. Entry conditions

- PHASE-08 and PHASE-09 gates **Passed** (M3 reached).
- Owner authorization for the edit-during-career campaign.
- A **separate, explicit owner authorization** before any native-economy write experiment (WP-10-07).

## 3. Dependencies

- **Phases:** PHASE-08, PHASE-09.
- **Open questions:** OPEN-021 (detecting city edits), OPEN-019 (route validation cost at city scale), OPEN-020 (transit
  data reused for job generation), OPEN-018 (access points reused).

## 4. Required reading

[SPEC-CHALLENGE](../../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md),
[SPEC-MISSION](../../gameplay/MISSION_FRAMEWORK.md), [SPEC-HOME](../../gameplay/HOME_AND_SERVICE_POINTS.md),
[SPEC-BUS](../../gameplay/careers/BUS.md), [SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md),
[SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
[FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md), [EXPANSIONS](../../product/EXPANSIONS.md).

## 5. Deliverables

- Checkpoint routes and time trials on city roads, with results stored per profile.
- Parking and precision-driving challenges.
- Demand-informed job generation that reads city data only.
- City-edit detection, or the documented fallback of full revalidation on every return from `Building`.
- Revalidation and safe reaction for the avatar position, career vehicles, missions, bus shifts, home base, and service
  points.
- An audit showing that career passengers and cargo never satisfy native demand.
- If authorized: a report on the optional native-economy write experiment, which stays off by default.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-10-01 | Checkpoint routes and time trials | REQ-CHAL-001 | Routes built from validated road segments; results per profile; invalidated routes handled like missions. |
| WP-10-02 | Parking and precision challenges | REQ-CHAL-002 | Uses the PHASE-03 contact rules and the PHASE-05 parking outcome. |
| WP-10-03 | Demand-informed job generation | REQ-CHAL-003, REQ-CITY-001 | Read-only use of building types, accessibility, lines, and route distances. |
| WP-10-04 | City-edit detection spike | OPEN-021 | Compare event-style detection with full revalidation on return from `Building`; measure cost. |
| WP-10-05 | Revalidation and reaction | REQ-CITY-002, REQ-CAREER-006, REQ-BUS-002, REQ-HOME-003, RISK-018 | Reroute or no-fault cancellation; relocate the avatar or vehicle through `Recovery` when its position became invalid. |
| WP-10-06 | No double satisfaction audit | REQ-CITY-003 | Show that no mod action fulfils native demand also fulfilled by the native simulation. |
| WP-10-07 | Native-economy write experiment (optional) | REQ-CITY-003, [EXPANSIONS](../../product/EXPANSIONS.md) | **Non-gating, off by default.** Only with explicit owner authorization, in a lab city, limited to a single operation type, reversible, with before/after comparison. Results go to the feasibility matrix. Never enabled in a default configuration. |
| WP-10-08 | Edit-during-career campaign and gate report | Gate | Demolish a destination, remove a road, change a line, move a stop, demolish the home base, during each career. |

## 7. Out of scope

Writes to the municipal economy in any default configuration. Native passengers or goods. Police pursuits and combat.
Online leaderboards or multiplayer. Final presentation of challenge UI (PHASE-11).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-CHAL-001 | In-game | Checkpoints and time trials; results per profile | Not run |
| TEST-CHAL-002 | In-game | Parking and precision challenges | Not run |
| TEST-CHAL-003 | Integration | Job generation reflects city data | Not run |
| TEST-CITY-001 | Integration | No native economic or service writes in the default configuration | Not run |
| TEST-CITY-002 | In-game | Revalidation after edits in `Building` | Not run |
| TEST-CITY-003 | Integration, Review | No double satisfaction; any economy experiment off by default | Not run |
| TEST-CAREER-006 | In-game | No mission impossible indefinitely | Not run |
| TEST-BUS-002 | In-game | Line or stop change (full coverage) | Not run |
| TEST-HOME-003 | In-game | Invalid home or service point after edits | Not run |
| TEST-ART-002 | Review | Placeholder labeling | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *editing the city during a career corrupts neither profile nor
save and leaves no indefinitely impossible missions; native economy writes stay a gated, off-by-default experiment.*
Checked by the recorded edit-during-career campaign, save reload after each edit, and a configuration review showing
that every native-economy write path is disabled by default. The optional experiment is not a gate criterion. A
Reviewer and the project owner must accept the gate.

## 10. Risks and open questions

RISK-018 (city edits invalidate missions and entities), RISK-005 (save corruption after edits), RISK-008 (revalidation
cost in large cities), RISK-012 (scope creep toward economy integration), RISK-007 (game updates changing city data),
RISK-602 (human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **Edits cannot be detected.** Use full revalidation on every return from `Building` as the permanent method and
  measure its cost. If it is too slow for large cities, revalidate incrementally around active missions first.
- **A mission stays impossible after an edit.** Add a time-bounded invalidation rule (no-fault cancellation) so that
  nothing remains impossible indefinitely.
- **Job generation produces unreachable work.** Filter offers through the PHASE-06 route validation before display.
- **The economy experiment affects the city unexpectedly.** Stop it, restore the lab city from its copy, record the
  result, and keep the experiment disabled. It never blocks the gate.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
