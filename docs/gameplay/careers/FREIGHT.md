# Rigid-Truck Freight Career

| Field | Value |
|---|---|
| ID | SPEC-FREIGHT |
| Document status | Draft |
| Release scope | v1.0 (rigid trucks only; articulated trucks are an expansion, CONF-003) |
| Phases | PHASE-08 (primary), PHASE-03 (payload handling targets), PHASE-07 (truck rental, purchase, upgrades), PHASE-10 (demand weighting, docking challenges) |
| Responsible role | Coordinator (maintainer); Implementer of the Career and Vehicles modules |
| Related requirements | REQ-FREIGHT-001, REQ-DRIVE-002, REQ-DRIVE-003, REQ-CAREER-001 to REQ-CAREER-006, REQ-GARAGE-005, REQ-SAVE-005, REQ-CITY-003 |
| Related documents | [SPEC-MISSION](../MISSION_FRAMEWORK.md), [SPEC-DRIVING](../VEHICLE_DRIVING.md), [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md), [SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), [ADR-0004](../../architecture/decisions/ADR-0004-physics-strategy.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [EXPANSIONS](../../product/EXPANSIONS.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. How payload acts on handling depends on the
> physics approach, which is undecided (ADR-0004, OPEN-009). The observable targets below hold for either approach.
> Values are **[Proposal]**; pay is in [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md) §8. Validation: **Not run**.
> Freight loads are mod-owned **career cargo**, never native goods.

## 1. Purpose

Freight is the heavy-vehicle career. A rigid truck changes how the city feels to drive: wider turns, longer braking,
and a noticeable difference between running empty and fully loaded. The work happens between the city's industrial and
commercial buildings (REQ-FREIGHT-001).

## 2. Scope and non-goals

**In scope:** the rigid-truck class; cargo types with mass; payload eligibility; payload effects on handling; loading
and unloading at validated industrial and commercial access points; docking precision; evaluation, failure, and
persistence.

**Non-goals for v1.0:** articulated trucks and trailers **[Owner requirement]** (an expansion, [EXPANSIONS](../../product/EXPANSIONS.md),
CONF-003; an exploratory spike in PHASE-08 only with explicit owner authorization and never gating). Also out of scope
**[Proposal]**: physical cargo objects in the truck bed, cargo securing mini-games, tachograph or driving-hours rules,
weigh stations, and supplying native industry or shops (REQ-CITY-003).

## 3. Vehicles

| Source | Terms | Unlock (SPEC-ECON §5.3) |
|---|---|---|
| Service truck (provided by the shipper) | Free to use; contracts pay 60%; returned at the end of the contract ([SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md) §8) | Freight unlocked |
| Premium rental | Per-shift fee plus deposit | Freight level 2 |
| Owned | Full pay; payload reinforcement upgrade available | Freight level 2 |

Class parameters (mass, payload 8 t, length 9.5 m, turning radius 9 m) are in [SPEC-DRIVING](../VEHICLE_DRIVING.md) §3.4.

**First-time guidance** (needed for the PHASE-08 gate): the first freight contract uses a service truck with General
cargo. Hints cover wide turns, the empty-versus-loaded braking difference, the docking zone, and the load-integrity
indicator. Presentation belongs to [SPEC-ONBOARD](../../experience/ONBOARDING.md).

## 4. Cargo and eligibility

| Cargo type **[Proposal]** | Units | Typical mass | Sensitivity | Notes |
|---|---|---|---|---|
| General (palletized) | 0.5 t pallets | 2–8 t | Medium (lateral threshold 0.45 g) | May have up to 3 drop-offs |
| Bulk (aggregates and similar) | One load | 5–8 t | Low (0.6 g) | Single drop-off; slow loading |
| Heavy item (machinery) | One item | 4–8 t | High (load shift above 0.35 g lateral) | Single drop-off; precision unloading required |

**Eligibility** (checked before offering and on acceptance):
- cargo mass ≤ the truck's payload (including upgrades);
- every access point accommodates the truck's length and approach (OPEN-203);
- the route avoids roads the class may not use, as far as the city exposes such restrictions (OPEN-203).

Ineligible offers are shown greyed out with the reason, such as "Needs 9 t payload: install payload reinforcement or
rent a truck" (REQ-UI-003).

## 5. Payload and handling (REQ-FREIGHT-001, REQ-DRIVE-003)

- **Effective mass** = curb mass + cargo mass. Acceleration time and stopping distance interpolate between the class's
  empty and full values ([SPEC-DRIVING](../VEHICLE_DRIVING.md) §3.4). Uphill acceleration drops in proportion to the
  grade.
- **Measurable target [Proposal]:** at full payload, the 0–50 km/h time is at least 30% longer and the stopping
  distance from 50 km/h at least 15% longer than empty, using the same scripted inputs. Both are monotonic in payload.
- **Load integrity** starts at 100. It drops with contacts (Light −3, Medium −15, Severe −40) and with lateral or
  longitudinal acceleration above the cargo's threshold (−4 per event). At 0, the load is **lost**.
- The HUD shows payload mass, load integrity, and a load-shift warning near the threshold.

## 6. Loading and unloading

- **Access points:** industrial **loading bays** and commercial **service entrances** found through CityBridge
  (OPEN-018). If a building has none that fits, a **curbside loading zone** on its frontage road is accepted when the
  road is wide enough for a stopped truck not to block all lanes in one direction **[Proposal]**. Otherwise the building
  is not eligible.
- **Docking zone:** stop with the truck's rear within 3 m of the bay marker, at under 0.5 m/s. Any heading is accepted
  for General and Bulk cargo. **Heavy items** require a heading within 15° of the bay direction (reversing in), because
  this is a precision unload.
- **Duration:** General 3 s per pallet (up to 20 s); Bulk 15 s; Heavy 20 s **[Proposal]**. Moving aborts the step,
  which restarts on the next stop.
- **Clean dock bonus:** reversing in with the heading within 15° and no contact gives +5% pay on that leg.

## 7. Contracts, deadlines, and evaluation

- **Structure:** a pickup at an industrial building, then 1–3 drop-offs (General) or 1 (Bulk, Heavy) at commercial or
  other industrial buildings. Route total 2–15 km. Pairs are weighted by city data ([SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md) §4).
- **Deadlines:** soft = route estimate × 1.6; hard = soft + 50%; on the career clock (OPEN-201). The pay factor between
  soft and hard follows the curve in [SPEC-DELIVERY](DELIVERIES.md) §6.
- **Evaluation** 0–100 = 40% load integrity + 30% punctuality + 20% driving cleanliness + 10% docking precision. The pay
  multiplier and reputation follow the table in SPEC-DELIVERY §7.
- **Pay:** per load plus per tonne-km of planned distance, with cargo-type factors (SPEC-ECON §8). Service-truck
  contracts pay 60%.

## 8. Outcomes, cancellation, and failure

| Situation | Result **[Proposal]** |
|---|---|
| All drop-offs resolved, at least one delivered | `Completed` (partial if a load was lost or late) |
| Load lost (integrity 0) on a single-drop contract, or all loads lost | `Failed`, reputation −5 |
| Hard deadline passed with nothing delivered | `Failed` |
| Player cancels before loading | `Cancelled`, reputation −1 |
| Player cancels after loading | The cargo is returned automatically. Fee: 10% of contract value, capped. Delivered loads are paid at 50%. Reputation −3. |
| Truck immobilized (fuel, breakdown) | Recovery per [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md) §9. A towed truck keeps its cargo. Deadlines run unless the cause was system-side. |
| Service truck failure caused by the system | A replacement truck is delivered with the cargo transferred; no fault |

## 9. City edits and invalidation

- **Pickup building demolished or its bay invalid before loading:** `Invalidated`, then a no-fault cancellation.
- **Drop-off invalid:** an alternate bay of the same building is used; otherwise a substitute drop-off of the same
  category within 1 km is proposed. **Accept:** pay is recalculated. **Decline,** or no substitute exists: the leg is
  `Skipped-NoFault` and the cargo is returned automatically, with no fee.
- **Route broken:** rerouted; if no route exists, the leg is handled as an invalid drop-off.

## 10. Data that must persist

Format in [SPEC-SAVE](../../architecture/SAVE_FORMAT.md): the contract terms (cargo type, mass, pickup and drop-off stable
references); each load's state (AtPickup, Loaded, Delivered, Lost, Returned, or Skipped) and integrity; the truck's
VehicleRecordId; evaluation accumulators; career-clock deadlines. The payload is re-applied to the truck's handling
parameters on load.

## 11. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | A rigid-truck contract loads and unloads at validated industrial and commercial access points. Mass affects handling (§5 target) and eligibility. No articulated vehicle is involved. | TEST-FREIGHT-001 | Not run |
| 2 | The rigid truck meets the class distinctness rule; loaded and empty measurements differ as specified. | TEST-DRIVE-003 | Not run |
| 3 | Lifecycle, cancellation, and exactly-once settlement behave as in SPEC-MISSION. | TEST-CAREER-001, TEST-CAREER-004, TEST-CAREER-005 | Not run |
| 4 | Demolishing the drop-off building yields a substitute or a no-fault skip. | TEST-CAREER-006 | Not run |
| 5 | Save/load with cargo aboard restores the payload, integrity, and handling, with no duplicate pay. | TEST-SAVE-005 | Not run |
| 6 | Career cargo never changes native industry, shop stock, or goods flows. | TEST-CITY-003 | Not run |

## 12. Open questions and risks

Central ([register](../../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-009 (how payload enters the physics), OPEN-018,
OPEN-034 (truck models), CONF-003. New: **OPEN-203** (truck-aware access point and route validation), **OPEN-207**
(industry categories for cargo types), **RISK-204** (cities without suitable industrial bays).

## 13. Provenance

Rigid trucks, mass and capacity affecting handling and eligibility, industrial and commercial access points, and the
exclusion of articulated trucks are **[Owner requirement]**. They also appear in [chat.md](../../../chat.md)
**[Historical input]**, which had articulated trucks as a separate prototype (CONF-003, resolved). The cargo types,
docking rules, and load integrity are original proposals. Motor Town's truck jobs are a behavioral reference only
([MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md)).
