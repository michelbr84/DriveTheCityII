# PHASE-09 — Home, services, and company

| Field | Value |
|---|---|
| ID | PHASE-09 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M3 — Content alpha |
| Responsible role | Coordinator (phase lead); Implementer; QA (driver-to-company journey, financial reconciliation); Reviewer (time checkpoints, save format); project owner (gate and M3 acceptance) |
| Related requirements | REQ-HOME-001, REQ-HOME-002, REQ-HOME-003 (first pass), REQ-COMPANY-001, REQ-COMPANY-002, REQ-COMPANY-003, REQ-TIME-004 (company earnings), REQ-UI-002 (company menu), REQ-ART-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Give the character identity and long-term progression. The player chooses a **residential base** and a **garage
location** linked to city buildings, used through exterior access points, which provide a safe spawn and return point.
Service interactions (job board, rental, repair, contracts) happen at accessible exterior **service points**,
optionally represented by mission characters. No navigable interiors are required.

The player can found a basic **transportation company** with a name, contracts, fleet records, hiring, salaries, and
costs. Employees and fleet operations are an explicitly labeled **managerial simulation**: the interface never implies
that they physically circulate in the city. Company income and costs settle against persisted time checkpoints and the
ledger, without duplication across mode switches, pauses, or save/load.

## 2. Entry conditions

- PHASE-07 gate **Passed** (ownership, garage, recovery paths).
- PHASE-08 is not a prerequisite. Company contracts use whichever careers have passed their gates, and the company
  menu lists the careers that are available.
- Owner authorization for the journey sessions.

## 3. Dependencies

- **Phases:** PHASE-07 (and PHASE-06 for the mission framework and ledger).
- **Open questions:** OPEN-018 (access points for home and service points), OPEN-010 (reuse of the avatar
  representation for optional mission characters), OPEN-021 (edit detection; first contact for invalid home bases).
- **Risks shaping the design:** RISK-022 (time and pause inconsistencies), RISK-006 (duplicated money).

## 4. Required reading

[SPEC-HOME](../../gameplay/HOME_AND_SERVICE_POINTS.md), [SPEC-COMPANY](../../gameplay/COMPANIES_AND_FLEET.md),
[SPEC-TIME](../../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md),
[SPEC-GARAGE](../../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md),
[SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md),
[SPEC-UI](../../experience/UI_AND_HUD.md), [PLAYER_JOURNEYS](../../product/PLAYER_JOURNEYS.md),
[EXPANSIONS](../../product/EXPANSIONS.md) (physical fleets are an expansion).

## 5. Deliverables

- Home base and garage location selection, linked to buildings through validated exterior access points, used as the
  safe spawn and return location.
- Service points for job board, rental, repair, and contracts, with optional mission characters.
- Notification and fallback when a home base or service point becomes invalid (first pass; full city-edit handling is
  PHASE-10).
- Company founding and records as pure logic: name, contracts, fleet records, hiring, salaries, costs.
- A labeled managerial simulation and its interface wording.
- Settlement of company income and costs from persisted time checkpoints.
- A company menu.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-09-01 | Home base and garage location | REQ-HOME-001 | Selection flow; safe spawn and return; persistence bound to save identity. |
| WP-09-02 | Service points | REQ-HOME-002 | Job board, rental, repair, contracts at exterior access points; optional mission characters. |
| WP-09-03 | Invalid home or service point (first pass) | REQ-HOME-003, RISK-018 | Notify and fall back (for example, to the basic rental service point). Completed with city-edit detection in PHASE-10. |
| WP-09-04 | Company records (pure logic) | REQ-COMPANY-001 | Founding cost, contracts, fleet records, hiring, salaries, running costs. Logic tests without CS2. |
| WP-09-05 | Managerial simulation and labeling | REQ-COMPANY-002 | Outcome calculation for employee work; UI text that names it a managerial simulation; no physical vehicles spawned for employees. |
| WP-09-06 | Time-checkpoint settlement | REQ-COMPANY-003, REQ-TIME-004, RISK-022 | Income and costs settle per persisted checkpoint; pause, mode switch, and save/load neither duplicate nor skip periods. |
| WP-09-07 | Company menu | REQ-UI-002 | Contracts, fleet records, staff, finances; consolidated in PHASE-11. |
| WP-09-08 | Driver-to-company journey and gate report | Gate | Second vehicle, one hire, reload; Reviewer and owner acceptance of the phase and milestone M3. |

## 7. Out of scope

Navigable interiors. Physical employee fleets circulating in the city (expansion). A full social simulation of
residents. Writes to the municipal economy. Challenges and demand-informed job generation (PHASE-10).

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-HOME-001 | In-game | Home base and garage location; safe spawn and return | Not run |
| TEST-HOME-002 | In-game | Service interactions at exterior service points | Not run |
| TEST-HOME-003 | In-game | Invalid home or service point: notification and fallback (first pass) | Not run |
| TEST-COMPANY-001 | Logic, In-game | Company founding and records | Not run |
| TEST-COMPANY-002 | Review, In-game | Managerial simulation labeled; no implied physical fleet | Not run |
| TEST-COMPANY-003 | Logic, Integration | Settlement against checkpoints and ledger; no duplication | Not run |
| TEST-TIME-004 | Logic, Integration | Company earnings across pause, mode switch, save/load | Not run |
| TEST-UI-002 | In-game | Company menu | Not run |
| TEST-ART-002 | Review | Placeholder labeling | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *progress from independent driver to a small company (second
vehicle, one hire) and reload with no financial inconsistency or implied physical fleet.* Checked by a recorded
journey, ledger and checkpoint reconciliation after pause, mode switch, and reload, and a review of interface wording
for REQ-COMPANY-002. A Reviewer and the project owner must accept the gate and milestone M3 (together with PHASE-07 and PHASE-08).

## 10. Risks and open questions

RISK-022 (time inconsistencies), RISK-006 (duplicated money), RISK-018 (home or service point invalidated), RISK-012
(scope creep toward physical fleets or interiors), RISK-019 (company debt as a softlock), RISK-602 (human-operated
sessions). Open questions: section 3. All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **Checkpoint settlement duplicates or skips income.** Stop, fix the settlement logic, add a logic test for the exact
  sequence (for example, pause → save → load → resume), and re-run the journey.
- **Company debt can trap the player.** Apply the PHASE-07 recovery rules to the company (for example, selling assets
  or dissolving the company) so that the personal career can always continue.
- **Home bases cannot be linked to suitable access points in some layouts.** Offer only buildings with validated access
  points and record the limitation.
- **Interface wording implies a physical fleet.** Treat it as a REQ-COMPANY-002 failure and correct it before the gate.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
