# Vehicle Ownership and Garage

| Field | Value |
|---|---|
| ID | SPEC-GARAGE |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-06 (basic rental only), PHASE-07 (primary), PHASE-08 (van, truck, bus service vehicles), PHASE-09 (player garage location) |
| Responsible role | Coordinator (maintainer); Implementer of the Vehicles and Career modules; Reviewer review for changes to vehicle records or spawning |
| Related requirements | REQ-GARAGE-001 to REQ-GARAGE-006, REQ-ENTITY-002, REQ-ENTITY-003, REQ-DRIVE-003, REQ-ECON-004, REQ-HOME-002, REQ-SAVE-003 |
| Related documents | [SPEC-ECON](PROGRESSION_AND_ECONOMY.md), [SPEC-DRIVING](VEHICLE_DRIVING.md), [SPEC-HOME](HOME_AND_SERVICE_POINTS.md), [SPEC-TRANSITION](WALK_DRIVE_TRANSITIONS.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. Prices and rates are in
> [SPEC-ECON](PROGRESSION_AND_ECONOMY.md) §8; the values here are **[Proposal]**. Which vehicle models may be used is
> open (OPEN-034). Validation: **Not run**.

## 1. Purpose

This document defines how the player gets vehicles to work with and keeps them working: rental, purchase, sale,
ownership, fuel, wear and simplified maintenance, upgrades with measurable effects, and recovery when a vehicle or a
service is unavailable. The governing rule **[Owner requirement]**: no repair, required upgrade, part, or service can
permanently block driving (REQ-GARAGE-006).

## 2. Scope and non-goals

**In scope:** `CareerVehicle` records and their subtypes Rental, Owned, and Service; vehicle storage; fuel; condition;
maintenance; upgrades; immobilization; recovery from inaccessible services.

**Non-goals for v1.0 [Owner requirement]:** part-by-part mechanical assembly, structural damage, dealership interiors,
and a dedicated workshop building type in the city. Also out of scope **[Proposal]**: transferring vehicles between
cities, vehicle customization beyond the upgrade slots, and insurance products.

## 3. Vehicle records

Every career vehicle exists first as a **vehicle record**. The world entity is created from the record when needed and
removed when it is not ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)).

| Field | Content |
|---|---|
| VehicleRecordId | Stable ID (never a runtime entity ID; REQ-SAVE-003) |
| Class | Car, light delivery vehicle, rigid truck, or bus ([SPEC-DRIVING](VEHICLE_DRIVING.md)) |
| Model reference | Depends on OPEN-034. Placeholders are labeled (REQ-ART-002). |
| Kind | One of the three `CareerVehicle` subtypes of SPEC-ENTITY: **Rental** (Basic or Premium), **Owned**, **Service** (provided by an operator or shipper) |
| Holder (Owned only) | **Player** (personal) or **Company** (PHASE-09, [SPEC-COMPANY](COMPANIES_AND_FLEET.md)). A company vehicle is an Owned vehicle with a different holder, not a fourth subtype. |
| Condition | 0–100 |
| Fuel | 0–100% of tank |
| Upgrades | At most one per slot (§7) |
| Odometer | km driven |
| Location state | `InUse` · `Parked`(stable location) · `Stored`(storage) · `Immobilized`(location, reason) · `InService` · `Removed` (sold or returned) |

Ownership belongs to the **career profile in this city save** (REQ-GARAGE-002). Loading an older save shows that save's
vehicles only (REQ-SAVE-001).

## 4. Rental (REQ-GARAGE-001)

| Rental | Classes | Terms (values in SPEC-ECON §8) | Where |
|---|---|---|---|
| **Basic** | Car | No deposit, fuel included, revenue share of positive settlements while in use. Condition is never charged. One active rental at a time. | Any rental desk. If no desk is reachable, **delivered** to the nearest valid curb access point near the player within about 2 career-clock minutes **[Proposal]**. |
| **Premium** | Light delivery vehicle, rigid truck | Per-shift fee plus deposit. A shift lasts up to 2 career-clock hours; each started extra hour adds 25% **[Proposal]**. The deposit is refunded minus the cost of condition lost and fuel used. | Rental desks with that class unlocked |

- The rental vehicle spawns at the desk's vehicle access point, after a free-space check. If that point is blocked, an
  alternate is tried. If all are blocked, the player sees "Bay occupied, try again shortly" and nothing is charged.
- **Return:** at any rental desk (the vehicle is removed after the avatar walks away, per SPEC-TRANSITION), or remotely
  from the menu while not driving, for a towing fee. A basic rental can always be returned remotely free of charge.
- Rentals persist across save/load. Charges use operation IDs and are applied exactly once (SPEC-ECON §4).
- **The basic rental is always available as a recovery path.** It cannot be blocked by insolvency, reputation, deferred
  charges, or missing service points.

## 5. Purchase, storage, and sale (REQ-GARAGE-002)

- **Purchase** happens at a vehicle-sales service point, or from the garage menu with delivery to storage. It requires
  the unlock (SPEC-ECON §5.3), sufficient funds, and no open deferred charge. The price is charged once under a
  `VP-<VehicleRecordId>` transaction.
- **Storage:** stored vehicles have no world entity. Before the player chooses a garage (PHASE-09,
  [SPEC-HOME](HOME_AND_SERVICE_POINTS.md)), owned vehicles are stored at the **default storage**: the lot of the
  nearest vehicle-sales service point. Garage capacity is defined in SPEC-HOME. Owned vehicles beyond capacity may
  remain `Parked` in the world.
- **Retrieval:** the player walks to the storage's vehicle access point and selects the vehicle; it spawns there.
  **"Deliver to me"** brings it to the nearest valid curb access point near the player for a towing-rate fee.
- **Sale** happens at a vehicle-sales point or from the garage menu. The vehicle must be `Stored` or `Parked` and not
  attached to a mission. The value is set by SPEC-ECON §8, plus 30% of installed upgrade prices **[Proposal]**. The
  record becomes `Removed` and the entity is despawned.
- Whether parked vehicles stay physical in the world while the player is in `Building` is open (OPEN-205). The proposed
  default is that parked vehicles are converted to `Stored at location` records and respawned on return.

## 6. Fuel, wear, and maintenance

### 6.1 Fuel (REQ-GARAGE-003)

- Consumption per km = class base × (1 + 0.5 × payload fraction) × driving factor (up to +15% for sustained hard
  throttle) **[Proposal]**. Idle consumption is small.
- HUD warnings at 20% and 10%. At 0%, a **limp reserve** allows up to 2 km at no more than 30 km/h. After that the
  vehicle is `Immobilized` (OutOfFuel).
- **Refuel** at a fuel service point: stop within 8 m of the access point and interact. Choose a full tank or 25% steps.
  If funds are short, the player can buy what they can afford. The fill takes about 5 s **[Proposal]**.

### 6.2 Wear and condition (REQ-GARAGE-004)

| Source | Condition loss **[Proposal]** |
|---|---|
| Distance | 1 point per 5 km (car); classes scale |
| Contact events ([SPEC-DRIVING](VEHICLE_DRIVING.md)) | Light −0.5 · Medium −3 · Severe −10 |

| Condition | Effect (applied through handling parameters, so it is measurable) **[Proposal]** |
|---|---|
| 50–100 | None |
| 20–49 | 0–50 km/h time +10%, stopping distance +8%, HUD warning |
| 1–19 | 0–50 km/h time +25%, stopping distance +15%, top speed capped at 60 km/h, persistent warning |
| 0 | `Immobilized` (Breakdown) |

### 6.3 Maintenance

At any repair service point, which is an exterior access point; no dedicated building type is needed (REQ-GARAGE-004).
The player buys condition points: all, or as many as they can afford, at the class rate. Partial repair is always
possible. There are no parts in v1.0, so a repair can never be blocked by missing parts.

## 7. Upgrades (REQ-GARAGE-005)

At most one upgrade per slot. Upgrades are never required by any contract, challenge, or unlock: capacity needs can
always be met by renting a larger class. Effects change the documented parameters, so tests can measure them on the lab
road with scripted input **[Proposal]**:

| Upgrade | Slot | Classes | Effect | Measured by |
|---|---|---|---|---|
| Performance brake pads | Brakes | All owned | Stopping distance from 50 km/h −10% | Scripted braking test |
| Grip tires | Tires | Car, van | Lateral grip +8% | Constant-radius cornering test (30 m) |
| Engine tune | Engine | Car, van, truck | 0–50 km/h time −8% | Scripted acceleration test |
| Extended tank | Fuel | All owned | Range +25% | Consumption over a fixed route |
| Comfort suspension | Suspension | Car, van | Comfort and cargo penalties from harsh acceleration −20% | Scripted harsh-maneuver test |
| Cargo racks | Cargo | Van | +2 parcel slots | Capacity check |
| Payload reinforcement | Cargo | Rigid truck | +1 t payload; loaded stopping distance +3% | Capacity check plus braking test |

## 8. Service vehicles

Some careers provide the vehicle. The **bus** comes from a depot operator, and **starter freight** uses a shipper's
truck. Service vehicles are never owned or sold. Fuel and maintenance cost the player nothing, and contact events affect
only the mission evaluation. At the end of the shift, the vehicle is returned at the depot, or at any valid parking
access point ("the operator collects it"), after which it is removed. If a service vehicle becomes immobilized, the
operator delivers a replacement and the mission continues with no fault (SPEC-MISSION).

## 9. Immobilization and inaccessible-service recovery (REQ-GARAGE-006)

| Situation | Recovery options shown (values in SPEC-ECON §8) |
|---|---|
| Out of fuel (owned or premium rental) | **Roadside fuel** (fee, arrives in about 1 career-clock minute) · **Tow** to the nearest fuel point (fee) · **Leave it** and take the basic rental |
| Breakdown (condition 0) | **Tow** to the nearest repair point (fee) · **Roadside quick fix** to condition 20 (fee) · **Leave it** and take the basic rental |
| Wallet cannot pay | The fees become deferred charges (SPEC-ECON §7). The service is never refused. |
| No fuel or repair point reachable (none designated, demolished, or disconnected) | **Mobile service:** roadside fuel or repair at the normal price plus a call fee. If even the vehicle's position is unreachable, the vehicle is recovered to storage for a towing fee. |
| Basic rental immobilized | A free replacement basic rental is delivered and the old vehicle is removed |
| Service vehicle immobilized | The operator replaces it; no fault |
| Immobilized vehicle standing in a through lane | Relocated free to the nearest valid parking access point, so it never blocks traffic ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)) |

A vehicle left immobilized stays `Immobilized` at its location until towed or repaired. It never disappears without a
record.

## 10. Data that must persist

Per profile and bound to the save identity (format in [SPEC-SAVE](../architecture/SAVE_FORMAT.md)): all vehicle records
(including `Removed` ones, kept as history), active rentals and their terms, pending service operations with operation
IDs, and the storage assignment. World entities are recreated from records on load. Raw entity IDs are never persisted.

## 11. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | Rental works at a desk. The basic rental is obtainable with zero money, with deferred charges open, and with every rental desk demolished. | TEST-GARAGE-001 | Not run |
| 2 | Purchase and sale charge exactly once, change ownership in this city's profile only, and persist across save/load. | TEST-GARAGE-002 | Not run |
| 3 | Fuel drains as specified; refuelling works at a service point; the limp reserve works. | TEST-GARAGE-003 | Not run |
| 4 | Condition thresholds change measured acceleration and braking; repair at an exterior service point restores them. | TEST-GARAGE-004 | Not run |
| 5 | Each upgrade's effect is measurable within its stated tolerance. | TEST-GARAGE-005 | Not run |
| 6 | Every immobilization and inaccessible-service case in §9 has a working recovery. No state blocks driving permanently. | TEST-GARAGE-006, TEST-ECON-004 | Not run |
| 7 | PHASE-07 gate: a new profile can rent, work, buy, and maintain a vehicle with every transaction persisted. | PHASE-07 gate | Not run |

## 12. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-012 (spawning and removal), OPEN-018 (access
points), OPEN-034 (vehicle models and provenance), RISK-015, RISK-019. New: **OPEN-203** (class-aware access-point
validation for trucks and buses), **OPEN-205** (parked vehicles while in `Building`), **RISK-203** (exploits through
recovery services).

## 13. Provenance

Rental, purchase, sale, per-city ownership, fuel, simplified maintenance at exterior points, measurable upgrades, and
the no-permanent-block rule are **[Owner requirement]** and appear in [chat.md](../archive/chat.md) **[Historical input]**.
The rental tiers, condition thresholds, upgrade list, mobile service, and default storage are original proposals.
Motor Town's ownership loop is a behavioral reference only ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)).
