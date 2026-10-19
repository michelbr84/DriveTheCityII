# City Integration

| Field | Value |
|---|---|
| ID | SPEC-CITY |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 (read-only boundary, no persistent change), PHASE-06 (access points, connectivity), PHASE-08 (lines and stops), PHASE-10 (edits, demand-informed jobs, gated experiment) |
| Responsible role | Coordinator (maintainer); Reviewer checks every new write path into host state; the project owner approves any native-economy experiment |
| Related requirements | REQ-CITY-001, REQ-CITY-002, REQ-CITY-003, REQ-CITY-004, REQ-CAREER-002, REQ-CAREER-003, REQ-CAREER-006, REQ-BUS-001, REQ-BUS-002, REQ-CHAL-003, REQ-HOME-003, REQ-ECON-001 |
| Related documents | [SPEC-MODULES](MODULE_CONTRACTS.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-BUS](../gameplay/careers/BUS.md), [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md), [ADR-0003](decisions/ADR-0003-career-save-isolation.md), [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md) |
| Last updated | 2026-10-18 |

> The read-before-write boundary and the separation of career content from native content are
> **[Owner requirement]**. The algorithms and numbers are **[Proposal]**. What city data can be read is unverified
> (OPEN-018 to OPEN-021, OPEN-103). Validation status: **Not run**.

## 1. Purpose

Define exactly what the mod may read from and write to the city, how it turns city data into reachable mission
locations, how it reacts when the player edits the city, and how it keeps career passengers, cargo, and money apart
from the native simulation. All host access goes through CityBridge; Career sees only the ports in
[SPEC-MODULES](MODULE_CONTRACTS.md).

## 2. Scope and non-goals

In scope: read/write boundary, access-point validation, connectivity checks, revalidation after edits, no-fault
cancellation and reroute triggers, career/native separation, no double satisfaction, the optional native-economy write
experiment, no unintended persistent change.

Not in scope: mission rules and payouts ([SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md)), how jobs are generated
from demand ([SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md)), entity handling
([SPEC-ENTITY](ENTITY_LIFECYCLE.md)).

## 3. Read-before-write boundary (v1.0)

| City data or state | Reads allowed | Writes allowed | Writes forbidden |
|---|---|---|---|
| Road network geometry, lanes, connectivity | Yes (OPEN-019) | None | Any change |
| Buildings: identity, type, footprint, frontage, access candidates | Yes (OPEN-018) | None | Any change, including occupancy or condition |
| Parking spaces, curbs, loading bays | Yes | None (career vehicles do not claim native parking slots unless proven safe, OPEN-012) | Native occupancy records |
| Transit lines, stop order, stop positions | Yes (OPEN-020) | None | Line, stop, schedule, or vehicle assignment |
| Native vehicles | Position, class, speed | Sandbox only: borrow and handback ([SPEC-ENTITY](ENTITY_LIFECYCLE.md)) | Any write in career mode |
| Native citizens and pedestrians | Position (for contact rules) | None | Trips, households, wallets, health |
| Lane presence and reservations | Yes | Only the mod's own entries for the vehicle it controls, removed on release (OPEN-011, OPEN-032) | Entries of native agents |
| Municipal budget, taxes, demand, goods, services | Yes (for job generation, REQ-CHAL-003) | None; the optional experiment of section 9 only | Default: all |
| Simulation speed and pause | Yes | TimePolicy at mode boundaries ([SPEC-TIME](GAME_TIME_AND_PAUSE.md)) | Any other change |
| Camera and input | — | Player, in character mode only | Any change while in `Building` |
| Save data | Per [ADR-0003](decisions/ADR-0003-career-save-isolation.md) | Persistence only | City data in the save |

## 4. Access points (REQ-CAREER-003)

An **access point** is a validated, reachable curbside, stop, or loading location linked to a building or stop; never
a building center. **[Proposal]** validation pipeline:

1. **Candidates** from city data: frontage lane positions along the building's road side, parking or curb lanes,
   loading bays for industrial and commercial buildings, stop positions for transit stops (OPEN-018, OPEN-020).
2. **Filter:** the lane must be usable by the mission's vehicle class; not inside a junction area; not on a segment
   where stopping is unsafe (highway, ramp); same level as the building frontage ([SPEC-CONTACT](WORLD_CONTACT.md)).
3. **Clearance:** room for the vehicle's length plus margin; a walkable point on the curb side for boarding or exit.
4. **Reachability:** a route exists from the player's current position (or mission start) for that vehicle class
   (section 5).
5. **Record:** `AccessPointRef = (PlaceRef, kind, lane reference, offset, level, validation stamp)`. The validation
   stamp is the `CityRevisionStamp` at the time of validation.

Rules: a building with no valid candidate yields no mission; it is never replaced by its center. The same pipeline
serves home base, garage, and service points ([SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md)).

## 5. Connectivity and routing (REQ-CAREER-002, OPEN-019)

- Options compared in PHASE-06: querying native pathfinding versus a mod-side search over the road graph read through
  CityBridge. Criteria: correctness on disconnected-road and one-way scenarios, cost per query, behavior during
  simulation pause.
- Result is `Reachable(route)`, `Unreachable`, or `Unknown`. Only `Reachable` allows an offer.
- Route results are cached per `(from area, to access point, class, CityRevisionStamp)` and discarded when the stamp
  changes.
- Route guidance shown to the player ([SPEC-UI](../experience/UI_AND_HUD.md), REQ-UI-004) uses the same route.

## 6. Revalidation after edits (REQ-CITY-002, REQ-CAREER-006)

The city can change in two ways: the player edits it in `Building`, and the native simulation changes it during play
(growth replacing buildings, abandonment, disasters). Detection of edits is unverified (OPEN-021).

| Trigger | Scope | Fallback when edits cannot be detected |
|---|---|---|
| Return from `Building` to character mode (TR-01) | Spawn point, stowed career vehicles' locations, home base, service points, active missions (all legs), bus shift line, active challenges | Full revalidation on every return |
| Leg activation | That leg's access point and route | Always performed |
| Arrival at an access point | Access point still present and usable | Always performed |
| Periodic during character mode | Targets of active legs (every 30 s **[Proposal]**) | Always performed |

Order of work: what is needed to spawn the player comes first; the rest is amortized over following frames but
completes before any affected interaction (RISK-106).

Outcomes per item:

- **Valid** → keep.
- **Repairable** → move to an equivalent access point of the same place (same building or stop, same kind), or
  reroute; notify the player if it changes what they see.
- **Invalid** → Career marks the mission `Invalidated` (no-fault cancellation, [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md));
  home base or service point → notification and fallback ([SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md),
  [SPEC-RECOVERY](ERROR_RECOVERY.md)); bus shift → re-plan or no-fault cancellation (REQ-BUS-002).

No mission may stay impossible indefinitely: a leg that remains unreachable for 120 s of career-active time
**[Proposal]** is invalidated even if each individual check returned `Unknown`.

## 7. Separation of career content from native content (REQ-CITY-003, REQ-ECON-001)

- Career passengers and career cargo are **MissionObjects** owned by the mod. They are never presented as native
  citizens or goods (REQ-TAXI-002, REQ-BUS-001).
- The mod does not create, move, delete, or modify native citizens, households, companies, goods, or trips.
- Bus v1.0 follows the city's lines and stops with career passengers only; native passengers are a separate experiment
  ([EXPANSIONS](../product/EXPANSIONS.md)).
- Money: the personal wallet and ledger are separate from the municipal budget. No career transaction changes city
  finances in v1.0.

## 8. No double satisfaction

Demand-informed jobs read native signals (for example, commercial demand) to choose plausible destinations. They must
never satisfy that native demand: delivering career cargo to a shop does not add to the shop's native stock, a taxi
ride does not complete a citizen's trip, and the native simulation keeps satisfying its own demand as if the mod did
not exist. Any mission text that implies otherwise is a defect (honest presentation, REQ-COMPANY-002 spirit).

## 9. Optional native-economy write experiment (gated)

Allowed only as a separate experiment, never as a v1.0 dependency **[Owner requirement]**:

| Gate | Rule |
|---|---|
| Default | Off. Hidden outside development builds. |
| Scope | Exactly one operation type, chosen by the project owner (OPEN-109). |
| Environment | Lab city copies only; never the owner's primary saves (RISK-028). |
| Enablement | Explicit opt-in per city with a warning that the city will be changed. |
| Logging | Every write logged with before and after values. |
| Kill switch | Disabled automatically on any invariant failure; the safe-disable switch also disables it. |
| Evidence | Results recorded in [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md); no promotion to the product without a PRD change approved by the owner. |

## 10. No unintended persistent change (REQ-CITY-004)

Verification method **[Proposal]** for PHASE-01 and PHASE-13: in a lab city, record a **city fingerprint** (budget
balance, population, building count, road segment count, native vehicle count, line count) after loading. Run a
scripted session with the mod enabled (enter and leave character mode, drive, complete a mission, save), reload, and
compare with a control session without the mod over the same simulated duration. Then disable the mod and reload
(OPEN-022). Differences beyond normal simulation variance are investigated. Mod-spawned entities captured in a host
save are handled by the orphan sweep after load ([SPEC-ENTITY](ENTITY_LIFECYCLE.md)).

## 11. Failure handling

| Failure | Response |
|---|---|
| City query returns `Unknown` | No offer is made; existing missions keep their last validation until the 120 s limit |
| Route becomes `Unreachable` mid-mission | Reroute; if impossible, `Invalidated` (no-fault) |
| Line or stop removed during a bus shift | Re-plan from the next valid stop, or no-fault cancellation |
| Home base or service point invalid | Notify; nearest valid alternative of the same type; final fallback per [SPEC-RECOVERY](ERROR_RECOVERY.md) |
| CityBridge capability missing after a game update | Dependent careers disabled with a message; `Building` unaffected |

## 12. Invariants

| ID | Invariant | Test |
|---|---|---|
| INV-CITY-01 | No access point is ever a building center; every access point passed the pipeline of section 4. | TEST-CAREER-003 |
| INV-CITY-02 | No offer is made without a `Reachable` result. | TEST-CAREER-002 |
| INV-CITY-03 | No write to native economic or service state in v1.0 with the experiment off. | TEST-CITY-001 |
| INV-CITY-04 | Career passengers and cargo never alter native citizens or goods. | TEST-CITY-003 |
| INV-CITY-05 | After a `Building` session, every career reference is revalidated before use. | TEST-CITY-002 |

## 13. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| v1.0 reads city data and writes no native economic or service state. | TEST-CITY-001 | Not run |
| Returning from `Building` revalidates avatar, vehicles, missions, home base, service points. | TEST-CITY-002 | Not run |
| Career content stays separate; no double satisfaction; experiment off by default. | TEST-CITY-003 | Not run |
| Activating, using, and deactivating the mod leaves no unintended persistent change. | TEST-CITY-004 | Not run |
| Offers require validated reachable access points; edits lead to reroute or no-fault cancellation. | TEST-CAREER-002, TEST-CAREER-003, TEST-CAREER-006, TEST-BUS-002 | Not run |

## 14. Open questions and risks

- OPEN-018 (access points), OPEN-019 (routing), OPEN-020 (lines and stops), OPEN-021 (edit detection), OPEN-103 (stable
  native references), OPEN-109 (experiment operation choice).
- RISK-018 (edits invalidate missions), RISK-106 (revalidation cost spikes), RISK-007 (game updates).

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
