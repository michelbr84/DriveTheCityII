# Challenges and the Dynamic City

| Field | Value |
|---|---|
| ID | SPEC-CHALLENGE |
| Document status | Draft |
| Release scope | v1.0 (native economy writes excluded; at most a separately gated, off-by-default experiment) |
| Phases | PHASE-10 (primary), PHASE-08 (first city-edit reactions for careers), PHASE-11 (presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module and the CityBridge read adapters |
| Related requirements | REQ-CHAL-001, REQ-CHAL-002, REQ-CHAL-003, REQ-CAREER-006, REQ-CITY-001, REQ-CITY-002, REQ-CITY-003, REQ-BUS-002, REQ-HOME-003, REQ-TIME-002 |
| Related documents | [SPEC-CITY](../architecture/CITY_INTEGRATION.md), [SPEC-MISSION](MISSION_FRAMEWORK.md), [SPEC-HOME](HOME_AND_SERVICE_POINTS.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-ECON](PROGRESSION_AND_ECONOMY.md), [SPEC-BUS](careers/BUS.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [PHASE-10](../planning/phases/PHASE-10-challenges-and-dynamic-city.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. Values are **[Proposal]**. Validation:
> **Not run**. What the mod may read and write in the city is defined only in [SPEC-CITY](../architecture/CITY_INTEGRATION.md).

## 1. Purpose

The project's central promise is to work and drive in **the city you built**. This document turns that promise into
three things: driving challenges on the player's own roads, job generation shaped by the city's buildings, lines, and
distances, and a safe reaction when the player edits the city between sessions of character mode.

## 2. Scope and non-goals

**In scope:** checkpoint routes, time trials, parking and precision challenges, results per profile, demand-informed
weighting of career offers, revalidation after city edits, and the gating rules for any native-economy experiment.

**Non-goals for v1.0:** online leaderboards, ghosts and replays, closing roads to traffic for events, police pursuits,
and any write to native economy or service state **[Owner requirement]** (REQ-CITY-001, REQ-CITY-003).

## 3. Challenges

### 3.1 Types (REQ-CHAL-001, REQ-CHAL-002)

| Type | Goal | Where | Scoring **[Proposal]** |
|---|---|---|---|
| Checkpoint route | Pass ordered checkpoints | City roads | Completion, time, contact penalties |
| Time trial | Fastest completion of a fixed route | City roads | Time against medal targets |
| Parking challenge | Stop inside a marked bay within tolerance and time | Validated curb spaces and parking lots | Position and heading error, time, contacts |
| Precision course | Slalom gates, narrow gates, reverse into a bay | Parking lots and loading yards | Gate touches, time, final bay accuracy |

**Parking tolerances [Proposal]:** car: lateral ±0.4 m, longitudinal ±0.6 m, heading ±8°. Rigid truck (reversing into a
loading bay): ±0.6 m and ±10°. A Severe contact ([SPEC-DRIVING](VEHICLE_DRIVING.md)) fails the attempt. Whether the
needed position precision is available depends on OPEN-007.

**Markers** (checkpoints, bays, and gates) are `MissionObject`s with **no physical collision**. Touching a gate is
detected by overlap and penalized. No collision proxies are created for challenges.

### 3.2 Where challenges come from

- **Generated set:** each district with a valid road loop gets a checkpoint route and a time trial. Each validated
  parking lot gets a parking challenge, and lots large enough get a precision course. Generation is deterministic,
  seeded by city and profile.
- **Player-authored routes:** in the career map, the player picks a start, up to 12 waypoints, and a finish on roads.
  The route is validated for connectivity (OPEN-019) and saved to the profile.
- Challenges for a vehicle class need that class to be available to the player. Career mode uses only career vehicles.
  Sandbox mode may run challenges, but records no results.

### 3.3 Run rules

- Challenges reuse the mission lifecycle ([SPEC-MISSION](MISSION_FRAMEWORK.md)). A run is `Accepted` when armed at the
  start marker, `Active` from crossing the start line, `Completed` at the finish, `Cancelled` on abort, and `Failed` when
  a time limit or failure rule is hit. An `Invalidated` run (route broken) resolves to a no-fault cancellation.
- **Timing** uses the career clock: `Paused` freezes it (REQ-TIME-002). The simulation speed stays at the
  character-mode setting (REQ-TIME-001).
- **The run becomes void** (no result, no penalty) when `Recovery` is used, when the player exits the vehicle, or when
  the player returns to `Building`.
- Native traffic is present and not controlled. Results are therefore **personal records**, not competitive rankings.

### 3.4 Results and rewards

- Per profile and per **route version**: best time or score, medal tier, attempt count, and date (REQ-CHAL-001).
- **Medal targets [Proposal]:** derived from the route's reference time for the class. Gold is 0.85×, silver 1.0×, and
  bronze 1.2× the reference.
- **Rewards:** XP for the career of the vehicle class, and a one-time cash reward the first time each tier is reached
  on a route version. Each has the TransactionId `CH-<ChallengeId>-<RouteVersion>-<Tier>` ([SPEC-ECON](PROGRESSION_AND_ECONOMY.md)),
  so a reward is never paid twice.
- **Route identity:** a route signature combines the ordered checkpoint references (stable road references plus
  offsets) and a fingerprint of the road segments along the route. If the player changes those roads, the route gets a
  new version. Old results stay visible as "Route changed since this record".

## 4. Demand-informed job generation (REQ-CHAL-003)

The built city shapes the work. Each career's `OfferGenerator` weights candidate access-point pairs **[Proposal]**:

`weight = categoryWeight(origin, destination, time of day) × distanceBandWeight × accessibilityWeight × noveltyWeight`

| Career | Origins weighted toward | Destinations weighted toward | Time-of-day modifiers |
|---|---|---|---|
| Taxi | Residential; transport hubs (train or bus terminals, if present) | Office, commercial, leisure, hubs | Morning: residential → office ×1.5. Evening: reverse ×1.5. Night: leisure ×1.3 |
| Deliveries | Commercial and industrial (warehouses) | Residential, commercial | Daytime ×1.2 |
| Freight | Industrial (by industry category, when readable) | Commercial, other industrial | None |
| Bus | Lines with more valid stops and longer routes | Stop demand from surrounding building density | Peak hours ×1.3 |

- **accessibilityWeight** favors access points with good road connectivity and penalizes dead ends that a class
  cannot turn in (OPEN-203).
- **noveltyWeight** lowers the weight of pairs used in the last 20 offers.
- **distanceBandWeight** follows the career's bands and the player's level.
- **Inputs, all read-only through CityBridge:** building category and district, access points, the road graph with
  route distances, transit lines and stops, in-game time of day, and the player's state. Which per-building attributes
  are actually readable is **OPEN-207**.
- **No double satisfaction** (REQ-CITY-003). Career passengers and cargo are separate mod-owned objects. A contract
  "from" a building is fiction layered on a real place. It never consumes, fulfils, or alters native demand, goods, or
  trips, and the UI never claims otherwise.

## 5. Reaction to city edits (REQ-CITY-002, REQ-CAREER-006)

The player edits the city only in `Building`. When the player returns to character mode, a **revalidation pass** runs
before control is handed over. Detection may be targeted (OPEN-021). The fallback is a full revalidation on every
return.

| Object | Reaction |
|---|---|
| Avatar spawn position | Revalidated by the spawn order of [SPEC-PLAYER](PLAYER_AND_CAMERAS.md) |
| Parked career vehicles | Revalidated. If a road was removed or something was built on top, the vehicle is relocated to the nearest parking access point or to storage, with a notice ([SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)) |
| Open offers | Discarded and regenerated |
| Accepted or active mission | Reroute, substitute, or no-fault cancellation ([SPEC-MISSION](MISSION_FRAMEWORK.md) §7.3) |
| Bus shift | Line replanning or no-fault cancellation ([SPEC-BUS](careers/BUS.md), REQ-BUS-002) |
| Challenge routes | Re-signed. Changed routes get a new version; broken routes become "Unavailable: route no longer connected" until regenerated or re-authored |
| Home, garage, service points | Fallbacks of [SPEC-HOME](HOME_AND_SERVICE_POINTS.md) (REQ-HOME-003) |
| Company | HQ reference revalidated. The managerial simulation is otherwise unaffected. |

**Guarantee:** after the pass, no mission, challenge, or service remains in an impossible state. Everything is valid,
rerouted, replaced, or cancelled with no fault and an explanation.

## 6. Native economy writes: gated experiment only

v1.0 does not write native economic or service state. Any future experiment ([SPEC-CITY](../architecture/CITY_INTEGRATION.md),
REQ-CITY-003) must meet **all** of the following **[Proposal]**:

1. It is a separate setting, **off by default**, labeled "Experimental: changes your city's simulation", with a warning
   to back up the save.
2. It covers one operation type only (for example, a delivery supplying one commercial building's goods need).
3. A reconciliation design prevents double satisfaction with native deliveries.
4. Turning it off stops all writes. Its effects are documented as non-reversible when they are.
5. It is validated only in a lab city, is never part of a phase gate, and starts only with explicit owner approval.

## 7. Failure handling

- A revalidation pass that fails internally degrades safely: offers are cleared, active missions resolve as no-fault
  cancellations, the error is logged, and the player stays in a stable state (REQ-RECOV-002).
- A route-validation timeout during challenge generation skips that challenge without blocking others.
- Corrupt challenge results are quarantined per [SPEC-SAVE](../architecture/SAVE_FORMAT.md). The challenges remain
  playable.

## 8. Data that must persist

Per profile, format in [SPEC-SAVE](../architecture/SAVE_FORMAT.md): player-authored route definitions, route versions and
signatures, results per route version, claimed reward tiers, the generation seed, and recent offer history for novelty.
Generated challenge lists may be recomputed.

## 9. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | Checkpoint routes and time trials run on city roads. Results are stored per profile and route version, and persist across save/load. | TEST-CHAL-001 | Not run |
| 2 | Parking and precision challenges measure tolerances as specified and void runs on recovery or exit. | TEST-CHAL-002 | Not run |
| 3 | Offer distributions change measurably when the lab city's building mix, lines, or road distances change. | TEST-CHAL-003 | Not run |
| 4 | After scripted edits (demolish a destination, cut a road, change a line), no impossible mission or challenge remains. | TEST-CAREER-006, TEST-CITY-002 | Not run |
| 5 | Career passengers and cargo never change native demand, goods, or citizens. The experiment setting is off by default. | TEST-CITY-003 | Not run |
| 6 | PHASE-10 gate: editing the city during a career corrupts neither profile nor save, and leaves no indefinitely impossible missions. | PHASE-10 gate | Not run |

## 10. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-007, OPEN-019, OPEN-020, OPEN-021, RISK-018. New:
**OPEN-203** (class-aware accessibility), **OPEN-206** (building identity stability), **OPEN-207** (readable building
attributes for weighting), **RISK-204** (career or challenge starvation in small or unusual cities).

## 11. Provenance

Challenges, demand-informed jobs, revalidation after edits, and the gated native-economy experiment are **[Owner
requirement]**. They also appear in [chat.md](../../chat.md) **[Historical input]**, which proposed editing in `Building`
and revalidating on return. The weighting formula, route versioning, medal derivation, and non-colliding markers are
original proposals. Motor Town's time-attack and autocross events are a behavioral reference only
([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)).
