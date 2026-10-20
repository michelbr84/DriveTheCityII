# Companies and Fleet

| Field | Value |
|---|---|
| ID | SPEC-COMPANY |
| Document status | Draft |
| Release scope | v1.0 (basic company with a managerial simulation); physical fleets are an expansion |
| Phases | PHASE-09 (primary), PHASE-11 (presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module; Reviewer review required for settlement or persisted data changes |
| Related requirements | REQ-COMPANY-001, REQ-COMPANY-002, REQ-COMPANY-003, REQ-TIME-004, REQ-ECON-002, REQ-ECON-004, REQ-SAVE-001, REQ-UI-002 |
| Related documents | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-ECON](PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-HOME](HOME_AND_SERVICE_POINTS.md), [SPEC-MISSION](MISSION_FRAMEWORK.md), [SPEC-UI](../experience/UI_AND_HUD.md), [EXPANSIONS](../product/EXPANSIONS.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. **The company's employees and fleet operations
> are a managerial simulation.** No employee or employee-driven vehicle ever exists physically in the city in v1.0, and
> the interface must never imply otherwise **[Owner requirement]**. Values are **[Proposal]** (SPEC-ECON §8).
> Validation: **Not run**.

## 1. Purpose

This document defines how an independent driver grows into a small transport business. The player founds a company,
buys or contributes vehicles, hires drivers, takes contracts, and pays salaries and costs. The company's income and
costs settle against persisted time checkpoints, without duplication.

## 2. Scope and non-goals

**In scope:** founding and naming; the company account and ledger; fleet records and assignments; hiring, salaries,
and firing; company contracts; deterministic period settlement; negative cash and contract failure; dissolution.

**Non-goals for v1.0 [Owner requirement]:** physical employee fleets circulating in the city, and multiplayer employees.
Also out of scope **[Proposal]**: company-owned buildings, stock, loans, rival companies, and custom routes driven by
employees. A physical fleet is an expansion ([EXPANSIONS](../product/EXPANSIONS.md)).

## 3. Behavior

### 3.1 Founding (REQ-COMPANY-001)

- **Requirements** (SPEC-ECON §5.3): total level, reputation, and the founding cost, paid from the wallet
  (`CF-<CompanyId>`).
- **Name:** 3–32 characters, validated for displayable characters. The player can rename it later for free.
- **Headquarters:** the player's garage location or a contracts office service point ([SPEC-HOME](HOME_AND_SERVICE_POINTS.md)).
  It is an exterior access point only.
- One company per career profile in v1.0 **[Proposal]**.

### 3.2 Company account

- A **separate account with its own append-only ledger**, under the same invariants as the wallet ledger
  ([SPEC-ECON](PROGRESSION_AND_ECONOMY.md) §4). TransactionIds use a `CO-` prefix.
- **Transfers** between the wallet and the company are explicit and confirmed. Each creates a paired entry in both
  ledgers (`CT-<TransferId>-W`, `CT-<TransferId>-C`) under one operation ID.
- The company **never** draws money from the personal wallet automatically, and the wallet never receives company money
  without a transfer the player confirmed.

### 3.3 Fleet records

Company vehicles are `Owned` vehicle records whose **holder is the company** ([SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)
§3). They are not a separate entity subtype. They are bought by the company or contributed from personal ownership as
capital, with no payment. Each has an
**assignment**:

| Assignment | World presence | Notes |
|---|---|---|
| Unassigned | `Stored` or `Parked`, like any vehicle | Available for the player to drive |
| Player-driven | Physical, while the player drives it | Player mission income goes to the wallet. Fuel and repairs bought while driving it are charged to the company account; if the company cannot pay, the player may choose to pay personally. |
| Managed (simulated) | **None.** The record is marked "On managed contract (simulated)". | Condition and fuel change abstractly through settlement. The vehicle cannot be driven until it is unassigned. |

### 3.4 Hiring and salaries

- The contracts office shows up to 3 **candidates** per settlement period. Each has a skill from 1 to 5, a reliability
  from 70% to 98%, and a salary demand **[Proposal]**.
- Hiring charges a fee (SPEC-ECON §8). Salaries are charged every period. Firing costs one period's salary as
  severance.
- Employees are abstract records (name, skill, reliability, assignment). They have no avatar and no world entity.

### 3.5 Company contracts

Company contracts come from the contracts office. Each has: a career type, a **job quota per period**, pay per job, a
required vehicle class, a duration in periods, a minimum reliability, and a penalty per missing job. They are **not**
missions ([SPEC-MISSION](MISSION_FRAMEWORK.md)). They are fulfilled only by the managerial simulation. Contract
availability and pay scale with the global reputation.

### 3.6 Period settlement (REQ-COMPANY-003, REQ-TIME-004)

- **Settlement period:** one in-game day of the city clock **[Proposal]**. Periods advance only while the city
  simulation runs, never while it is paused, and never while the game is closed. Whether the company should use the
  city clock or the career clock is **OPEN-201**. Clock definitions are owned by [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md).
- The company persists `LastSettledPeriodIndex`. On each check (every mode switch, on load, and periodically), every
  period index after it, up to the current completed period, is settled **in order and exactly once**. Each line has
  the TransactionId `CO-<CompanyId>-<PeriodIndex>-<Line>`, and duplicates are rejected.
- **Catch-up bound:** at most 30 periods per check **[Proposal]**. Remaining periods are settled in the next checks.
  Nothing is dropped and nothing is doubled.
- **Deterministic outcome:** for each employee with a vehicle on a contract:

  `capacityJobs = classBaseJobs × skillFactor × conditionFactor`

  `jobsDone = min(capacityJobs, remainingQuota)`, adjusted by a reliability draw from a pseudo-random generator seeded
  with (CompanyId, PeriodIndex, EmployeeId).

  Re-settling the same period in the same save line therefore gives the same result.
- **Lines per period:** contract income; salaries; abstract fuel cost per job; condition loss on vehicle records;
  automatic maintenance when enabled (charged to the company); shortfall penalties.
- **Report:** after a check, one summary notice ("Company report: 3 periods settled. Net +412 cr. Simulated jobs: 26.")
  links to the itemized company ledger.

Loading an older save restores that save's `LastSettledPeriodIndex` and company ledger only (REQ-SAVE-001).

## 4. Managerial simulation labeling (REQ-COMPANY-002)

These are binding UI wording rules. Copy and localization ([SPEC-L10N](../experience/LOCALIZATION.md)) must follow them:

- Company screens carry the heading **"Managed operations (simulated)"** and a one-line explanation: "Your employees'
  work is calculated, not shown in the city."
- Use "simulated jobs", "estimated", and "calculated" for employee results.
- **Never** show employee vehicles on the map, as world markers, or as spawned entities. Never offer "track", "follow",
  "call", or "locate" actions for them.
- **Forbidden phrasings** (examples): "Your driver is on the way", "Your truck arrived at …", "Watch your bus".
- Managed vehicles in the garage list show the badge "On managed contract (simulated)". A tooltip says that a physical
  fleet is not part of this version.

QA checks every company screen and notification against these rules.

## 5. Failure handling

| Failure | Behavior **[Proposal]** |
|---|---|
| Company cash cannot cover a period's costs | Costs are paid in order: salaries, then maintenance, then other costs. Anything unpaid becomes **company debt** and the company is **In arrears**: no new contracts, hires, or purchases. Each unpaid period lowers the reliability of unpaid employees by 10%. After 3 consecutive unpaid periods, unpaid employees resign. Nothing is charged to the personal wallet. |
| Contract shortfall | A penalty per missing job, charged at the end of each period, and −2 global reputation when a contract ends below 80% fulfilment |
| Player cancels a company contract | A penalty of 2 periods' worth of expected pay, after confirmation |
| Managed vehicle reaches condition 0 | Its jobs stop. With automatic maintenance on, it is repaired when cash allows; otherwise it is flagged in the report. |
| Dissolution (always available) | Contracts end without penalty. Employees are released. Each vehicle can be sold or moved to personal ownership (subject to garage capacity, otherwise sold). Company debt is written off with −10 reputation and no wallet charge, so the player can always restart as an independent driver (REQ-ECON-004). |
| Settlement error or invariant violation | The period stays unsettled, the error is logged, and settlement retries on the next check. The deterministic TransactionIds prevent double settlement. |

## 6. Data that must persist

Per profile, format in [SPEC-SAVE](../architecture/SAVE_FORMAT.md): the company record (name, HQ reference), the company
ledger, `LastSettledPeriodIndex`, the period clock checkpoint, employees, company contracts and their fulfilment
counters, fleet assignments, company debt and the arrears counter, and pending transfers with operation IDs.

## 7. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | A company can be founded and named. It can buy or receive a vehicle, hire one driver, take a contract, and pay salaries and costs. | TEST-COMPANY-001 | Not run |
| 2 | No company screen, notification, map, or world view implies physical employee circulation. No employee entity is ever spawned. | TEST-COMPANY-002 | Not run |
| 3 | Settlement across pauses, `Building` sessions, mode switches, and save/load produces exactly one set of entries per period and the same result when replayed. | TEST-COMPANY-003, TEST-TIME-004 | Not run |
| 4 | Arrears, contract shortfall, and dissolution behave as in §5. None of them charges the personal wallet without a confirmed transfer. | TEST-COMPANY-001 (failure branch) | Not run |
| 5 | PHASE-09 gate: progress from independent driver to a small company (second vehicle, one hire) and reload with no financial inconsistency. | PHASE-09 gate | Not run |

## 8. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): RISK-006 (duplicated money), RISK-012 (scope creep toward
physical fleets), RISK-022 (time inconsistencies). New: **OPEN-201** (clock basis for company periods), **RISK-205**
(players expecting a physical fleet).

## 9. Provenance

Name, contracts, fleet, hiring, salaries, costs, and the explicitly labeled managerial simulation are **[Owner
requirement]**. They are also in [chat.md](../archive/chat.md) **[Historical input]**. Motor Town's company feature (hiring
AI drivers for routes) is a behavioral reference only ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)). The
period model, deterministic seeding, arrears rules, and wording rules are original proposals.
