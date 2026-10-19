# PHASE-06 — Taxi career MVP

| Field | Value |
|---|---|
| ID | PHASE-06 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M2 — Taxi-career MVP |
| Responsible role | Coordinator (phase lead); Implementer; QA (ride campaign, ledger reconciliation); Reviewer scoped to save format and entity lifecycle; project owner (fun checkpoint, gate and M2 acceptance) |
| Related requirements | REQ-TAXI-001, REQ-TAXI-002, REQ-TAXI-003, REQ-CAREER-001 – REQ-CAREER-005, REQ-ENTITY-002, REQ-TIME-004 (career timers), REQ-ECON-001, REQ-ECON-002 (minimal), REQ-GARAGE-001 (minimal), REQ-SAVE-005, REQ-UI-004 (first pass), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Validate the whole product at small scale with one complete, persistent, repeatable career:
**walk → rent → accept a fare → collect a career passenger → transport → drop off → get paid once → park → exit →
save → close → reopen and continue.** Pickups and drop-offs use validated access points, never building centers.
Passengers are mod-owned career passengers and are never presented as native citizens.

Following CONF-007, this phase includes a **minimal slice** of the systems the taxi loop needs: a basic rental at a
service point, a personal wallet, an append-only ledger with unique TransactionIds, and taxi experience. PHASE-07
expands them. At M2 the project owner holds the **fun checkpoint** (RISK-023) before more content is added.

## 2. Entry conditions

- PHASE-05 gate **Passed** (M1 reached).
- PHASE-01 save foundation in place and ADR-0003 accepted.
- Owner authorization for the ride campaign and the fun-checkpoint session.

## 3. Dependencies

- **Phases:** PHASE-05, PHASE-01 (save foundation). Pure mission and ledger logic may start earlier behind stable
  contracts ([ROADMAP](../../../ROADMAP.md) §5).
- **Open questions:** OPEN-018 (reachable access points), OPEN-019 (route and connectivity validation), OPEN-006
  (answered in PHASE-01; applied here), OPEN-034 (rental vehicle models).
- **Conflicts:** CONF-007 (minimal wallet, ledger, and rental slice in this phase).

## 4. Required reading

[SPEC-MISSION](../../gameplay/MISSION_FRAMEWORK.md), [SPEC-TAXI](../../gameplay/careers/TAXI.md),
[SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](../../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md)
(rental section), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md),
[SPEC-TIME](../../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
[SPEC-UI](../../experience/UI_AND_HUD.md), [ADR-0003](../../architecture/decisions/ADR-0003-career-save-isolation.md),
[SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md), [PLAYER_JOURNEYS](../../product/PLAYER_JOURNEYS.md).

## 5. Deliverables

- The shared mission lifecycle (`Offered` → `Accepted` → `Active` with legs → `Completed` / `Cancelled` / `Failed`;
  `Invalidated`; `Expired`) as pure logic with logic tests.
- A minimal wallet and an append-only ledger that rejects duplicate TransactionIds; settlement exactly once per
  terminal outcome.
- A minimal rental at one service point, spawning a Rental `CareerVehicle`.
- Access-point determination and route validation before an offer is made.
- Career passengers with represented boarding and alighting; a fare estimate before acceptance.
- Cancellation and failure with explained consequences; career timers from persisted time checkpoints.
- Save/load during an active mission that resumes or resolves it safely.
- A first route or map view of the destination.
- The ride campaign record and the fun-checkpoint record.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-06-01 | Mission framework core (pure logic) | REQ-CAREER-001, REQ-CAREER-004 | Lifecycle, legs, deterministic TransactionId, settlement. Logic tests run without CS2. May start before PHASE-05 ends. |
| WP-06-02 | Minimal wallet and ledger | REQ-ECON-001, REQ-ECON-002, CONF-007 | Separate from the municipal budget; append-only; duplicate rejection; persisted through the PHASE-01 foundation. |
| WP-06-03 | Minimal rental | REQ-GARAGE-001, REQ-ENTITY-002 | One service point; Rental career vehicle; return and despawn. Always available as a recovery path. |
| WP-06-04 | Access points and route validation | REQ-CAREER-002, REQ-CAREER-003, OPEN-018, OPEN-019 | Spike over a sample of building types; compare native pathfinding with a mod-side road-graph search on disconnected-road scenarios. |
| WP-06-05 | Career passengers | REQ-TAXI-002 | `MissionObject` passengers with boarding and alighting; never shown as citizens. |
| WP-06-06 | Fare estimate, payment, taxi experience | REQ-TAXI-003, REQ-TAXI-001 | **[Proposal]** values only; balancing is not a gate criterion. |
| WP-06-07 | Cancellation, failure, and career timers | REQ-CAREER-005, REQ-TIME-004 | Explained consequences that never make the career unrecoverable; timers from persisted checkpoints; pause freezes them. |
| WP-06-08 | Save/load during a mission | REQ-SAVE-005 | Resume or resolve safely; no duplicated reward; no lost entity. Critical area: save format. |
| WP-06-09 | First route view | REQ-UI-004 | Destination and route on city roads; full map in PHASE-11. |
| WP-06-10 | Ride campaign | Gate | **[Proposal]** 20 rides in different districts, including at least one cancellation and one save/load during an active mission. |
| WP-06-11 | M2 fun checkpoint | RISK-023 | Owner playtest of the full loop; notes recorded as evidence. Outcome options: proceed to PHASE-07; tune feel before adding content; open a scope review. |
| WP-06-12 | Gate report and M2 review | Gate | Reviewer and owner acceptance of PHASE-06 and milestone M2. |

## 7. Out of scope

Vehicle purchase, sale, ownership, garage, fuel, maintenance, and upgrades (PHASE-07). Reputation and unlocks beyond
taxi experience (PHASE-07). Native citizens as passengers (an expansion; see [EXPANSIONS](../../product/EXPANSIONS.md)).
Other careers (PHASE-08). Any write to the municipal budget. Final UI and tutorial (PHASE-11).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-TAXI-001 | In-game | Complete taxi loop including save and reload | Not run |
| TEST-TAXI-002 | In-game | Career passengers, boarding, alighting | Not run |
| TEST-TAXI-003 | Logic, In-game | Fare estimate before acceptance | Not run |
| TEST-CAREER-001 | Logic | Shared lifecycle and legs | Not run |
| TEST-CAREER-002 | Integration | Reachability validated before an offer | Not run |
| TEST-CAREER-003 | Integration | Access points, never building centers | Not run |
| TEST-CAREER-004 | Logic, Integration | Exactly-once settlement across save/load and replayed events | Not run |
| TEST-CAREER-005 | Logic, In-game | Cancellation and failure consequences | Not run |
| TEST-ENTITY-002 | Integration | Career mode uses only career vehicles | Not run |
| TEST-TIME-004 | Logic, Integration | Career timers (company earnings in PHASE-09) | Not run |
| TEST-ECON-001 | Logic | Wallet separate from the municipal budget | Not run |
| TEST-ECON-002 | Logic | Minimal append-only ledger (history view in PHASE-07) | Not run |
| TEST-GARAGE-001 | In-game | Minimal rental (full rental in PHASE-07) | Not run |
| TEST-SAVE-005 | In-game | Save/load during an active mission | Not run |
| TEST-UI-004 | In-game | First route view | Not run |
| TEST-ART-002 | Review | Placeholder labeling | Not run |
| TEST-TAXI-101 (ride campaign) | In-game | Proposed 20 rides; defined in [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md) | Not run |

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): **[Proposal]** *20 rides in different districts, including a
cancellation and a save/load during a mission, with each ride paid exactly once.* "Paid exactly once" is checked by
ledger reconciliation: the wallet balance equals the sum of unique TransactionIds, and no TransactionId appears twice.
In addition: the full loop including close and reopen is recorded; no orphaned rental vehicles or passengers remain;
the M2 fun checkpoint is recorded with the owner's decision; a Reviewer (save format, entity lifecycle) and the project owner
must accept the gate and milestone M2. A negative fun-checkpoint outcome does not invalidate the technical gate, but
content phases (PHASE-07 onward) wait for the owner's decision.

## 10. Risks and open questions

RISK-006 (duplicated rewards), RISK-005 (save corruption or leakage), RISK-023 (the core loop is not fun), RISK-018
(city edits invalidate missions; basic handling only), RISK-022 (time inconsistencies), RISK-019 (economic softlock;
starter rental), RISK-602 (human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **A duplicated payout is observed.** Stop the campaign. Fix the settlement or ledger logic, add a regression logic
  test for the exact sequence, and restart the campaign.
- **Access points are unreliable for some building types.** Offer missions only for building types with validated
  access points, and record the limitation. Building centers are never used as a fallback.
- **Route validation is too slow.** Cache validated routes per access point and revalidate on return from `Building`;
  re-measure.
- **Mid-mission save/load cannot resume safely.** Resolve the mission on load with a no-fault cancellation and an
  explanation (allowed by REQ-SAVE-005), then re-test.
- **The fun checkpoint is negative.** Add tuning work packages (driving feel, camera, pacing) before PHASE-07, or open a
  scope review. More content is not added on top of a loop the owner judges unconvincing.

## 12. Evidence record

None yet. Ride campaign runs, ledger reconciliations, and the fun-checkpoint notes will be recorded as `RUN-NNNN` under
[docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
