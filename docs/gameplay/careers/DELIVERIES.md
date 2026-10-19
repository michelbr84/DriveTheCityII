# Urban Delivery Career

| Field | Value |
|---|---|
| ID | SPEC-DELIVERY |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-08 (primary), PHASE-07 (van rental and purchase, upgrades), PHASE-10 (demand weighting), PHASE-11 (presentation and tutorial) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module |
| Related requirements | REQ-DELIV-001, REQ-CAREER-001 to REQ-CAREER-006, REQ-DRIVE-003, REQ-GARAGE-005, REQ-SAVE-005, REQ-CITY-003 |
| Related documents | [SPEC-MISSION](../MISSION_FRAMEWORK.md), [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-DRIVING](../VEHICLE_DRIVING.md), [SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [PHASE-08](../../planning/phases/PHASE-08-delivery-freight-and-bus.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. Pay values live in
> [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md) §8; everything here is **[Proposal]**. Validation: **Not run**. Parcels
> are mod-owned **career cargo**, never native goods.

## 1. Purpose

Urban deliveries add planning to driving. The player collects parcels, chooses the order of several drop-offs, races
soft deadlines, and handles fragile cargo carefully. Smooth driving pays measurably better (REQ-DELIV-001).

## 2. Scope and non-goals

**In scope:** single-pickup contracts with one or more drop-offs; deadlines; fragility; a driving evaluation that
affects pay; car-based (tier 1) and van-based (tier 2+) work.

**Non-goals for v1.0 [Proposal]:** carrying parcels on foot to doors, multiple pickups in one contract, signatures or
customer dialogues, and cargo physics inside the vehicle. Deliveries never fulfil native commercial or residential
demand (REQ-CITY-003).

## 3. The loop

1. Accept a delivery contract at the job board (or by dispatch while on duty in an eligible vehicle). The contract
   shows parcels, drop-off count, fragility, deadlines, and the estimated pay range.
2. Drive to the **pickup** (a validated loading bay or curbside access point of a commercial or industrial building)
   and stop. Parcels load.
3. Deliver to each **drop-off** (validated curbside or service access points of residential or commercial buildings)
   **in any order**. The drop-offs form one unordered leg group ([SPEC-MISSION](../MISSION_FRAMEWORK.md) §4). The map
   suggests an efficient order but does not require it.
4. When every drop-off is resolved, the mission settles once, with an itemized breakdown per parcel.

**First-time guidance** (needed for the PHASE-08 gate): the first delivery contract is a guided two-drop route. It has
contextual hints for loading, choosing the drop-off order, the integrity indicator, and soft versus hard deadlines.
Presentation and sequencing belong to [SPEC-ONBOARD](../../experience/ONBOARDING.md).

## 4. Contracts and constraints

| Contract type | Drop-offs | Vehicle | Deadline slack **[Proposal]** | Unlock (SPEC-ECON §5.3) |
|---|---|---|---|---|
| Courier | 1 | Car or van | 1.5× the route estimate | Start |
| Route | 2–6 | Van (car allowed for 2 drops if slots allow) | 1.5× | Start (car); level 2 (van) |
| Express | 1–3 | Car or van | 1.25× | Level 3 |
| Fragile | 1–4 | Van | 1.5× | Level 3 |

- **Capacity** (eligibility): total parcel size ≤ vehicle slots (car 2, van 10, cargo racks +2; [SPEC-DRIVING](../VEHICLE_DRIVING.md)).
  Parcels have size 1 or 2.
- **Distance bands:** courier 0.8–5 km; route total 3–12 km **[Proposal]**.
- All access points are validated, reachable, and class-appropriate before offering (REQ-CAREER-002/003, OPEN-203).

## 5. Cargo, fragility, and integrity

Each parcel has a fragility class and an **integrity** from 100 down to 0 **[Proposal]**:

| Damage source | Standard | Fragile | Very fragile |
|---|---|---|---|
| Light contact | −5 | −10 | −20 |
| Medium contact | −20 | −35 | −60 |
| Severe contact | −50 | −80 | −100 |
| Harsh acceleration event (longitudinal or lateral above the threshold) | — (threshold 0.6 g) | −5 (threshold 0.45 g) | −8 (threshold 0.35 g) |

The comfort suspension upgrade reduces acceleration damage by 20% ([SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md) §7).
At integrity 0, the parcel is **destroyed**. A HUD indicator shows the worst parcel's integrity and flashes on damage,
with an icon as well as color (REQ-INPUT-003).

## 6. Loading, drop-offs, and deadlines

- **Loading:** stopped within 15 m of the pickup access point; 2 s per parcel. Moving aborts the remaining loading;
  loaded parcels stay loaded.
- **Drop-off:** stopped within 20 m of the drop-off access point; 3 s per parcel. Parcels for that address are unloaded
  automatically.
- **Deadlines:** each contract has a **soft deadline** (route estimate × slack) and a **hard deadline** (soft + 50%).
  Both run on the career clock (OPEN-201).

| Delivered | Parcel pay factor **[Proposal]** |
|---|---|
| Before the soft deadline | 100% (Express: +10%) |
| Between soft and hard | Falls linearly from 100% to 50% |
| After the hard deadline | Not deliverable: the parcel fails |

## 7. Driving evaluation (REQ-DELIV-001)

Score 0–100 = 50% average final integrity + 30% punctuality (share of parcels delivered before the soft deadline) + 20%
driving cleanliness (100 minus contact and harsh-event penalties, as in the taxi table in [SPEC-TAXI](TAXI.md) §6)
**[Proposal]**.

| Score | Pay multiplier | Reputation |
|---|---|---|
| ≥ 80 | ×1.15 | +2 |
| 50–79 | ×1.0 | +1 |
| < 50 | ×0.85 | 0 |

**Per-parcel pay** = base × integrity factor (≥ 80: 100%; 50–79: 70%; 1–49: 30%; 0: none) × deadline factor.
**Total** = Σ parcel pay × evaluation multiplier. The breakdown lists each parcel.

## 8. Outcomes, cancellation, and failure

| Situation | Result **[Proposal]** |
|---|---|
| All drop-offs resolved, at least one parcel delivered | `Completed`, possibly partial (destroyed or late parcels earn nothing) |
| Every parcel destroyed or failed, none delivered | `Failed`, reputation −5 |
| Player cancels before loading | `Cancelled`, reputation −1, no fee |
| Player cancels after loading | Undelivered parcels are returned automatically to the shipper. Return fee = 10% of the contract value, capped by SPEC-ECON. Delivered parcels are paid at 50%. Reputation −3. |
| Abandonment (generic rule in SPEC-MISSION §7.2) | `Failed` |
| Out of fuel or breakdown | Not a failure by itself. Recovery per SPEC-GARAGE §9. Deadlines keep running. |
| System-triggered recovery or service-vehicle replacement | Deadlines are extended by its duration |

## 9. City edits and invalidation

- **Drop-off access point invalid:** an alternate access point of the same building is used. Otherwise that address's
  parcels are marked `Skipped-NoFault`: no pay, no penalty, returned automatically.
- **Pickup invalid before loading:** the mission is `Invalidated`, then a no-fault cancellation.
- **Route to an address broken:** rerouted; if no route exists, that address is skipped with no fault.
- Every change is explained to the player in a notice ([SPEC-MISSION](../MISSION_FRAMEWORK.md) §10).

## 10. Data that must persist

Format in [SPEC-SAVE](../../architecture/SAVE_FORMAT.md): the contract terms; each parcel (size, fragility, integrity,
state: AtPickup, Loaded, Delivered, Destroyed, Skipped, or Returned; destination stable reference); the leg group state;
evaluation accumulators; deadlines as career-clock values. On load, loaded parcels are restored in the vehicle (they
have no individual world entities), and a loading or unloading step that was in progress restarts.

## 11. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | A multi-drop contract with a fragile parcel completes. Deadlines and integrity change pay as specified, and the evaluation changes the multiplier. | TEST-DELIV-001 | Not run |
| 2 | Contracts are offered only with validated, reachable, class-appropriate access points. | TEST-CAREER-002, TEST-CAREER-003 | Not run |
| 3 | Cancellation before and after loading follows §8. Settlement happens exactly once. | TEST-CAREER-004, TEST-CAREER-005 | Not run |
| 4 | Demolishing a drop-off building mid-route skips it with no fault, with a notice. | TEST-CAREER-006 | Not run |
| 5 | Save/load mid-route preserves parcel states and integrity, with no duplicate pay. | TEST-SAVE-005 | Not run |
| 6 | The comfort suspension's damage reduction is measurable in a scripted harsh-maneuver test. | TEST-GARAGE-005 | Not run |

## 12. Open questions and risks

Central ([register](../../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-018, OPEN-019, RISK-018. New: **OPEN-201**
(deadline clock), **OPEN-203** (van-appropriate access points), **OPEN-207** (building data for pickup and drop-off
weighting), **RISK-204** (too few commercial pickups in some cities).

## 13. Provenance

Multiple stops, pickup, deadlines, fragility, and a driving evaluation are **[Owner requirement]**. They also appear in
[chat.md](../../../chat.md) **[Historical input]**. The contract types, integrity model, deadline curve, and unordered
leg group are original proposals. Motor Town's cargo jobs are a behavioral reference only
([MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md)).
