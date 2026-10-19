# PHASE-07 — Progression and garage

| Field | Value |
|---|---|
| ID | PHASE-07 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M3 — Content alpha |
| Responsible role | Coordinator (phase lead); Implementer; QA (new-profile journey, softlock search); Reviewer (save format for new persisted data); project owner (gate acceptance) |
| Related requirements | REQ-ECON-002, REQ-ECON-003, REQ-ECON-004, REQ-ECON-005 (first version), REQ-GARAGE-001 – REQ-GARAGE-006, REQ-DRIVE-003 (owned vehicles), REQ-UI-002 (first menus), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Make work produce choices and goals. Expand the PHASE-06 minimal slice into full progression and ownership: a viewable
transaction history, experience per career, reputation, and unlocks; vehicle rental, purchase, sale, and ownership tied
to the career profile of that city; a garage; fuel; wear and simplified maintenance at accessible exterior service
points; upgrades with measurable effects; and **financial recovery** so that no player is permanently blocked. No
repair, required upgrade, part, or service can stop the player from driving again.

## 2. Entry conditions

- PHASE-06 gate **Passed** (M2 reached) and the M2 fun checkpoint recorded with the owner's decision to continue.
- Persisted data added in this phase (owned vehicles, garage, wear, fuel) follows SPEC-SAVE and its schema-version
  rules.
- Owner authorization for the new-profile journey sessions.

## 3. Dependencies

- **Phases:** PHASE-06.
- **Open questions:** OPEN-034 (vehicle models usable for purchasable vehicles), OPEN-018 (access points reused for
  service points).
- **Conflicts:** CONF-007 (this phase completes what PHASE-06 started).

## 4. Required reading

[SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](../../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md),
[SPEC-HOME](../../gameplay/HOME_AND_SERVICE_POINTS.md) (service points), [SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md)
(class parameters), [SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md),
[SPEC-UI](../../experience/UI_AND_HUD.md), [SPEC-ONBOARD](../../experience/ONBOARDING.md),
[GAME_DESIGN](../../product/GAME_DESIGN.md), [PLAYER_JOURNEYS](../../product/PLAYER_JOURNEYS.md),
[ASSET_PROVENANCE](../../legal/ASSET_PROVENANCE.md).

## 5. Deliverables

- Economy rules from SPEC-ECON as pure logic, with logic tests.
- A transaction history view backed by the ledger.
- Experience per career, reputation, and unlocks that persist per career profile.
- Purchase, sale, and ownership of `CareerVehicle` (Owned), with a garage to store and retrieve vehicles.
- Fuel consumption and refuelling at service points.
- Wear, maintenance, and repair at accessible exterior service points.
- Upgrades whose effects are measurable by tests.
- Recovery paths: an always-available starter job, a basic rental, recovery transport, and explained alternatives.
- A first economic tutorial and the first career, garage, and ledger menus.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-07-01 | Economy rules and transaction history | REQ-ECON-002 | Pure logic first; history view reads the append-only ledger. |
| WP-07-02 | Experience, reputation, unlocks | REQ-ECON-003 | Per career and per profile. **[Proposal]** values; balancing is iterative. |
| WP-07-03 | Purchase, sale, ownership | REQ-GARAGE-002, OPEN-034 | Ownership bound to the career profile of that city and to the save identity. |
| WP-07-04 | Garage | REQ-GARAGE-002 | Store and retrieve owned vehicles; persistence and revalidation on load. |
| WP-07-05 | Fuel and refuelling | REQ-GARAGE-003 | Accessible service points; running out of fuel leads to recovery, never a dead end. |
| WP-07-06 | Wear, maintenance, repair | REQ-GARAGE-004 | Simplified model; exterior service points; no dedicated building type required in the city. |
| WP-07-07 | Measurable upgrades | REQ-GARAGE-005, REQ-DRIVE-003 | Effects such as braking distance, capacity, or grip that a test can measure under fixed conditions. |
| WP-07-08 | Financial and immobilization recovery | REQ-ECON-004, REQ-GARAGE-006, RISK-019 | Starter job, basic rental, recovery transport, explained alternatives. A softlock search with QA covers zero money, broken vehicle, empty fuel, and unreachable service point. |
| WP-07-09 | First economic tutorial | REQ-ECON-005 | Explains wallet, costs, income, and recovery. Finalized in PHASE-11. |
| WP-07-10 | Career, garage, and ledger menus | REQ-UI-002 | First versions; consolidated in PHASE-11. |
| WP-07-11 | New-profile journey and gate report | Gate | Recorded journey from a new profile; Reviewer and owner acceptance. |

## 7. Out of scope

Part-by-part mechanical assembly, structural damage, and a dealership interior (expansions). Other careers (PHASE-08).
Home base and company (PHASE-09). Final balancing. Writes to the municipal budget.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-ECON-002 | Logic, In-game | Ledger and transaction history | Not run |
| TEST-ECON-003 | Logic | Experience, reputation, unlocks persist per profile | Not run |
| TEST-ECON-004 | Logic, In-game | No permanent insolvency block | Not run |
| TEST-ECON-005 | In-game | First economic tutorial (final in PHASE-11) | Not run |
| TEST-GARAGE-001 | In-game | Rental at a service point; basic rental always available | Not run |
| TEST-GARAGE-002 | Integration, In-game | Purchase, sale, ownership per profile | Not run |
| TEST-GARAGE-003 | In-game | Fuel and refuelling | Not run |
| TEST-GARAGE-004 | In-game | Wear and simplified maintenance | Not run |
| TEST-GARAGE-005 | In-game | Measurable upgrade effects | Not run |
| TEST-GARAGE-006 | In-game | No permanent driving block; immobilization recovery | Not run |
| TEST-DRIVE-003 | In-game | Distinct parameters for owned vehicle variants | Not run |
| TEST-UI-002 | In-game | Career, garage, ledger menus (company in PHASE-09) | Not run |
| TEST-ART-002 | Review | Placeholder labeling | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *a new profile can rent, work, buy, and maintain a vehicle; every
transaction persists; no softlock.* Checked by: a recorded new-profile journey that reaches an owned, maintained
vehicle; ledger reconciliation after save and reload; and a softlock search in which every tested dead-end state has a
working recovery path. A Reviewer and the project owner must accept the gate.

## 10. Risks and open questions

RISK-019 (economic softlock), RISK-006 (duplicated money), RISK-005 (save leakage of owned vehicles), RISK-015 (vehicle
model provenance), RISK-012 (scope creep toward part-by-part maintenance), RISK-602 (human-operated sessions). Open
questions: section 3. All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **A softlock is found.** Add or fix a recovery path before the gate. A softlock is a gate failure, not a balancing
  note.
- **Owned vehicles do not survive save and reload reliably.** Treat it as a save-format defect: stop, fix through the
  critical-area process, and re-run the save tests.
- **Purchasable vehicle models are not permitted** (OPEN-034). Restrict the purchasable set to permitted models and
  record the provenance decision. If no permitted model exists, open a scope review for REQ-GARAGE-002.
- **Economy values feel wrong.** Tune the **[Proposal]** values. This is not a gate failure unless it creates a softlock
  or an exploit that duplicates money.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
