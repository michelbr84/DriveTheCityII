# Taxi Career

| Field | Value |
|---|---|
| ID | SPEC-TAXI |
| Document status | Draft |
| Release scope | v1.0 (the PHASE-06 MVP career) |
| Phases | PHASE-06 (primary, M2), PHASE-07 (levels, unlock effects), PHASE-10 (demand weighting), PHASE-11 (presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module; QA owns the 20-ride acceptance run |
| Related requirements | REQ-TAXI-001, REQ-TAXI-002, REQ-TAXI-003, REQ-CAREER-001 to REQ-CAREER-006, REQ-SAVE-005, REQ-ECON-001, REQ-ECON-002, REQ-GARAGE-001, REQ-ENTITY-002, REQ-CITY-003 |
| Related documents | [SPEC-MISSION](../MISSION_FRAMEWORK.md), [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-TRANSITION](../WALK_DRIVE_TRANSITIONS.md), [SPEC-DRIVING](../VEHICLE_DRIVING.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [SPEC-UI](../../experience/UI_AND_HUD.md), [PHASE-06](../../planning/phases/PHASE-06-taxi-career-mvp.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. Fare values live in
> [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md) §8; every value here is **[Proposal]**. Validation: **Not run**.
> **Taxi passengers are mod-owned career passengers, never native citizens [Owner requirement].**

## 1. Purpose

The taxi is the first complete career and the proof of the whole product at small scale (M2). It must validate the full
chain: walking, renting, accepting work, collecting a career passenger, driving through the player's city, dropping
off, getting paid exactly once, parking, exiting, saving, and continuing after a reload.

## 2. Scope and non-goals

**In scope:** fares from the job board, in-vehicle dispatch and street hails; one passenger group per fare (1–3
passengers); boarding and alighting; ride quality; fare estimate and settlement; cancellation, failure, recovery, and
persistence.

**Non-goals for v1.0:** native citizens as passengers (a separate experiment; [PRD](../../product/PRD.md) §7);
ride-sharing or several fares at once; meter haggling; passenger dialogue trees; a dedicated taxi building. A roof sign
or livery on the rental car is optional presentation ([SPEC-ART](../../experience/ART_AND_ANIMATION.md)).

## 3. The core loop (REQ-TAXI-001)

1. **On foot** in character mode, walk to a rental desk (or use the delivered basic rental; [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md)).
2. **Rent** a car-class vehicle (basic rental in PHASE-06).
3. **Enter** it ([SPEC-TRANSITION](../WALK_DRIVE_TRANSITIONS.md)) and switch **Taxi duty** on.
4. **Accept a fare** after seeing the estimate (job board, dispatch, or street hail). The mission becomes `Accepted`,
   then `Active` because the player is already in an eligible vehicle.
5. **Pick up:** drive to the pickup access point. The career passenger is waiting at the curb and boards.
6. **Transport:** drive to the drop-off along any route. Ride quality is tracked.
7. **Drop off:** stop at the drop-off access point. The passenger alights.
8. **Paid once:** settlement adds the fare, tip, XP, and (from PHASE-07) reputation under one TransactionId.
9. **Park** at a valid parking position, **exit**, **save**, quit, **reopen**, and continue with the same wallet,
   ledger, and rental.

**First-time guidance:** the first fare of a new profile is a short guided ride. It has contextual hints for duty
mode, the estimate card, the pickup zone, ride quality, and the settlement breakdown. It also serves the PHASE-11 goal
of a first job without developer help (REQ-ONBOARD-001; presentation in [SPEC-ONBOARD](../../experience/ONBOARDING.md)).

## 4. Fare generation and offering

| Source | Behavior **[Proposal]** |
|---|---|
| Job board | Lists 3–6 validated taxi offers ([SPEC-MISSION](../MISSION_FRAMEWORK.md) §5) |
| In-vehicle dispatch | While on duty with no fare, a new validated offer appears every 30–90 career-clock seconds. It is accepted or declined from the HUD and expires after 20 s with no consequence. |
| Street hail | While on duty with no fare, a career passenger may wave from a validated curb access point within 100 m ahead. Stopping beside them shows the offer card. The destination is validated before the hail appears. |

**Fare terms:**
- pickup and drop-off are validated **curbside pedestrian access points** of real buildings (REQ-CAREER-003). They are
  never on roads without curb stopping (highways, elevated roads);
- pickup within 1.5 km route distance of the vehicle;
- trip length 0.8–6 km, rising to 10 km at higher taxi levels;
- origin and destination are different buildings;
- passenger group size 1–3 (within car capacity);
- a **preference**: *Standard*, *In a hurry* (tighter time target, higher tip potential), or *Comfort* (smoothness
  weighted double).

**Estimate before accepting** (REQ-TAXI-003): estimated fare range (base plus 0–25% tip), planned route distance,
estimated time to pickup and to destination, preference, and patience. Origins and destinations are weighted by city
data (REQ-CHAL-003; [SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md) §4).

## 5. Pickup and boarding (REQ-TAXI-002)

- The career passenger group is spawned at the pickup access point's waiting spot when the vehicle comes within 200 m,
  or at acceptance if it is already closer **[Proposal]**. This limits live entities. They are `MissionObject`s
  ([SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md)), labeled as career passengers in the UI, and never registered
  as citizens.
- **Patience:** 6 career-clock minutes from acceptance until the vehicle arrives (4 for *In a hurry*). The HUD shows a
  patience indicator.
- **Boarding:** the leg is `Servicing` once the vehicle is within 15 m of the pickup access point at under 0.5 m/s. The
  passengers walk to the nearest rear door and board, taking about 4 s. If the representation cannot animate that, a
  documented substitute is used, such as a short fade at the door (OPEN-202, REQ-ART-001). Moving above 0.5 m/s aborts
  boarding: the passengers step back and boarding restarts when the vehicle stops again.

## 6. Transport and ride quality

Ride quality starts at 100 points **[Proposal]**:

| Event (from [SPEC-DRIVING](../VEHICLE_DRIVING.md) ContactEvents and measured acceleration) | Points |
|---|---|
| Light / Medium / Severe contact | −2 / −8 / −20 |
| Harsh longitudinal acceleration or braking above 0.4 g | −1 each |
| Harsh cornering above 0.45 g lateral | −1 each |
| Trip time above 1.3× the estimate (*In a hurry*: above 1.1×) | −10 |
| *In a hurry* trip below 0.9× the estimate | +5 |
| *Comfort* preference | Harsh-event penalties doubled |

**Rating:** ≥ 90 → 5★ · 75–89 → 4★ · 55–74 → 3★ · 35–54 → 2★ · < 35 → 1★. The tip is 25% / 15% / 5% / 0 / 0. The
passenger occasionally reacts to events through short captioned remarks, such as "Easy on the brakes!"
([SPEC-AUDIO](../../experience/AUDIO.md) captions, REQ-INPUT-003).

**Three Severe contacts** in one ride: the passenger demands to get out at the next safe curb. The ride ends as
`Failed`.

## 7. Drop-off and settlement

- **Alighting:** within 15 m of the drop-off access point, under 0.5 m/s, for about 3 s. Then the passengers walk
  toward the building's pedestrian access point and are removed once out of view, or after 10 s.
- **Fare** = (flag + per-km rate × **planned** route distance) × (1 + tip). Using the planned distance makes detours
  unprofitable. Values are in SPEC-ECON §8.
- Settlement follows SPEC-MISSION §8: one ledger entry `MS-<MissionId>-COMPLETED`, applied in the same commit as XP
  (taxi) and reputation. While the basic rental is in use, its revenue share is charged as a separate `RentalCharge`
  derived from the settlement (SPEC-ECON §4), which is also idempotent.

## 8. Cancellation, failure, and recovery

| Situation | Result **[Proposal]** |
|---|---|
| Player cancels before pickup | `Cancelled`, no fee, reputation −1. The passengers walk away. |
| Player cancels with passengers aboard | The player must stop at a valid curb. The passengers alight, then the mission is `Cancelled`: no fare, reputation −3, no fee. |
| Patience runs out before pickup | `Failed`, no fee, reputation −1 (lighter than the generic −5, because of the uncertain city traffic) |
| Three Severe contacts | `Failed`, no fare, reputation −5 |
| Player leaves the vehicle with passengers aboard | Passengers wait 2 career-clock minutes. Walking more than 50 m away shows a warning. When the time runs out, they alight at the vehicle and the mission is `Failed`. |
| Vehicle immobile with passengers aboard for more than 3 career-clock minutes (out of fuel, breakdown) | Passengers alight at a nearby curb and the mission is `Failed`, unless the cause was system-side (then it is `Invalidated`, a no-fault cancellation) |
| `Recovery` with passengers aboard | Passengers stay aboard and the vehicle moves to a safe point. Recovery triggered by the system extends patience and time targets by its duration; recovery requested by the player does not. |
| Return to `Building` during a fare | The mission is suspended with the passengers stored in the vehicle ([SPEC-MISSION](../MISSION_FRAMEWORK.md) §9) |
| Pickup access point invalidated before pickup | `Invalidated`, then a no-fault cancellation with nothing owed either way |
| Drop-off invalidated after a city edit | The passenger proposes a new destination: the nearest eligible access point within 400 m of the original. **Accept:** the fare is recalculated on the new planned route, with no penalty. **Decline:** the passenger alights at the next valid curb; no-fault, paid in proportion to planned distance covered. |

## 9. Data that must persist

Format in [SPEC-SAVE](../../architecture/SAVE_FORMAT.md): the mission record (terms, preference, planned distance,
pickup and drop-off stable references, leg states), the passenger group record (size and state: waiting, aboard, or
alighting), ride-quality accumulators, patience and time checkpoints on the career clock, the duty toggle, and the
rental link. On load: passengers who were aboard are respawned in the vehicle; a boarding or alighting step that was in
progress restarts; passengers who were waiting are respawned at the curb when the vehicle comes within range.

## 10. PHASE-06 acceptance (M2)

The **proposed** gate: **20 rides** in different districts of a lab city, including **at least one player
cancellation** and **at least one save/load during an active ride** (one of them with passengers aboard). Each ride is
paid **exactly once**: the ledger shows exactly one settlement per completed or cancelled ride, and the wallet equals
the sum of the ledger entries. The loop also includes park, exit, save, quit, and reopen.

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | The full §3 loop completes and continues after a reload. | TEST-TAXI-001 | Not run |
| 2 | Passengers are mod-owned, with represented boarding and alighting. No native citizen is created, moved, or counted. | TEST-TAXI-002 | Not run |
| 3 | A fare estimate is shown before acceptance. Settlement uses planned distance and the rating tip. | TEST-TAXI-003 | Not run |
| 4 | Lifecycle, validation, access points, cancellation, and exactly-once settlement behave as in SPEC-MISSION. | TEST-CAREER-001 to TEST-CAREER-005 | Not run |
| 5 | Save/load mid-fare resumes or resolves with no duplicate payment or passenger. | TEST-SAVE-005 | Not run |
| 6 | The wallet is separate from the city budget; the ledger records every payment. | TEST-ECON-001, TEST-ECON-002 | Not run |
| 7 | The basic rental is obtainable in the MVP slice. | TEST-GARAGE-001 | Not run |

## 11. Open questions and risks

Central ([register](../../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-018, OPEN-019, RISK-006, RISK-023 (fun
checkpoint at M2). New: **OPEN-201** (clock basis for patience and time targets), **OPEN-202** (career passenger
representation).

## 12. Provenance

The loop, mod-owned passengers, accessible pickup points, validation before offering, and single payment are **[Owner
requirement]**. They also appear in [chat.md](../../archive/chat.md) **[Historical input]**. Dispatch, street hails,
preferences, the ride-quality model, and the passenger's proposed new destination are original proposals. Motor Town's
taxi job is a behavioral reference only ([MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md)).
