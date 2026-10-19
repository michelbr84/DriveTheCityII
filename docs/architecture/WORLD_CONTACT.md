# World Contact

| Field | Value |
|---|---|
| ID | SPEC-CONTACT |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-03 (laboratory, decision), PHASE-04 (avatar adaptation), PHASE-05 (exit placement), PHASE-12 (validation across cities) |
| Responsible role | Coordinator (maintainer); Reviewer checks changes to level tracking, proxy budgets, or the traffic contact rule |
| Related requirements | REQ-WORLD-001, REQ-WORLD-002, REQ-WORLD-003, REQ-WORLD-004, REQ-PLAYER-002, REQ-PLAYER-003, REQ-PLAYER-004, REQ-DRIVE-002, REQ-TIME-003, REQ-TRANS-002 |
| Related documents | [ADR-0004](decisions/ADR-0004-physics-strategy.md), [SPEC-TIME](GAME_TIME_AND_PAUSE.md), [SPEC-ENTITY](ENTITY_LIFECYCLE.md), [SPEC-PERF](PERFORMANCE_BUDGET.md), [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md), [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md), [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md), [PHASE-03](../planning/phases/PHASE-03-surfaces-collisions-and-physics.md) |
| Last updated | 2026-10-18 |

> **A rendered road is not proof of a usable collision surface.** What the host can answer about surfaces and
> collisions is unknown (OPEN-007, OPEN-008). Everything below is required behavior plus **[Proposal]** designs and
> starting numbers for the PHASE-03 laboratory. Validation status: **Not run**.

## 1. Purpose

Give the avatar and the controlled vehicle a trustworthy answer to three questions every substep: *where is the valid
ground under me, on my level?*, *what blocks me?*, and *where can I be placed safely?* This spec belongs to the
WorldContact module ([SPEC-MODULES](MODULE_CONTRACTS.md)), which answers queries but never moves anything.

## 2. Scope and non-goals

In scope: surface determination (roads, sidewalks, terrain, slopes, curbs, bridges, tunnels, elevated and stacked
roads), level tracking, ground validity, building and static collisions, bounded collision proxies, camera clipping,
contact with native traffic and pedestrians, the laboratory design shared with ADR-0004.

Non-goals for v1.0 **[Owner requirement]**:

- No promise that engine wheel-physics components (Unity WheelCollider-style vehicles) work in the host context.
  They may be evaluated inside the "local physical controller" option of ADR-0004, nothing more.
- No structural physics, deformation, or simulator-grade tire models.
- No collision interiors for buildings: buildings are solid exteriors.

## 3. Query contract

Every query returns an answer with an explicit confidence: `Valid(data)`, `Invalid(reason)`, or `Unknown`. Callers
must treat `Unknown` as "hold the last valid pose", never as valid ground. Each query carries the entity class
(avatar, car, light delivery vehicle, rigid truck, bus), its shape, and its current **level context**.

## 4. Surface determination (REQ-WORLD-001)

**[Proposal]** algorithm per substep:

1. **Gather candidates** inside a vertical window around the entity's current contact height (for example 2 m above
   and 3 m below), never by a single top-down ray from the sky. Sources, by preference: network surfaces (road,
   sidewalk, parking, pedestrian path) with their identity; terrain; collision proxies' top faces where allowed.
2. **Filter** by the entity class's permitted surfaces (section 6) and by slope.
3. **Select** the candidate that is (a) connected to the current level context, and (b) closest to the previous
   contact height within the step tolerance. Connection means the same network piece, an adjacent connected piece,
   or continuous terrain.
4. **Reject jumps:** a candidate that differs from the previous contact height by more than the step tolerance, and
   is not connected, is not selected even if it is the only one found. The answer becomes `Unknown` and the entity
   holds its pose; repeated `Unknown` for 1 s triggers `Recovery`.

| Situation | Required behavior |
|---|---|
| Flat road and sidewalk | Contact on the network surface; sidewalk is valid for the avatar and, for vehicles, only via curb rules |
| Curb | Avatar steps up or down within its step height (0.25 m); vehicles mount slowly with a jolt or are blocked, per [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md) |
| Slope and ramp | Followed while slope is under the class limit (avatar 35°, vehicles per class); steeper → blocked, not climbed |
| Bridge over a road | Stays on the deck; the road below is not connected and is outside the step tolerance → never selected |
| Tunnel under terrain | Stays on the tunnel floor; the terrain surface above is outside the window or not connected → ignored |
| Elevated road edge | Railings or deck edges block (proxy or barrier rule); falling off is `Invalid` → `Recovery`, never a drop to the lower level |
| Stacked roads and interchanges | Level context selects the deck the entity is on; transitions happen only along connected ramps |
| Terrain off-road | Valid for the avatar; for vehicles valid with reduced grip **[Proposal]**, never inside buildings or water |
| Water, map edge, building interior | `Invalid` → blocked or `Recovery` |

All numbers in this table are starting values for the laboratory.

## 5. Level tracking

The level context is `(surface identity, elevation band, last connected piece)`. It changes only when the newly
selected surface is connected to the previous one. A hysteresis of 0.5 s or 2 m of travel **[Proposal]** prevents
flicker at junctions between decks. Level context is also an input to exit placement and Recovery, so an avatar
leaving a car on a bridge is placed on the bridge (REQ-TRANS-002).

## 6. Ground validity per class

| Class | Valid surfaces | Invalid |
|---|---|---|
| Avatar | Sidewalks, pedestrian paths, crossings, roads (with traffic risk feedback), terrain, parking | Water, building footprints, deck edges, inaccessible roofs |
| Car, light delivery vehicle | Road lanes, parking, terrain off-road (reduced grip) | Sidewalk except via curb rule, pedestrian-only paths, water, building footprints |
| Rigid truck, bus | Road lanes, parking and loading areas | As above, plus terrain off-road **[Proposal]** |

## 7. Buildings and static obstacles (REQ-WORLD-002)

- **Source of truth:** host collision geometry if it exists and can be queried (OPEN-008); otherwise collision proxies
  derived from available building data (footprint or bounds).
- **Response:** the avatar slides along facades; vehicles stop or deflect, and the impact (speed, direction) is
  reported to Vehicles for feedback and wear. No penetration is allowed beyond a small tolerance (0.05 m).
- **Sweep, not sample:** contact is tested with the entity shape swept over each substep ([SPEC-TIME](GAME_TIME_AND_PAUSE.md)),
  which prevents tunneling at speed.
- **Static props** (poles, trees, benches) are included only where data exists; small props may be ignored for
  vehicles in v1.0 if measurements show the cost is too high, recorded as a documented limitation.
- The candidate base mod is reported not to implement building collision **[Documented upstream]** and to collide
  with vehicles only **[Source-inspected, pending re-check in TASK-0002]** ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)).
  Building contact is therefore new work, not reuse.

## 8. Collision proxies (REQ-WORLD-003)

Used only if the laboratory shows host geometry is insufficient. Data-only proxies are preferred
([SPEC-ENTITY](ENTITY_LIFECYCLE.md) section 3).

| Bound | Initial laboratory cap **[Proposal]** | Behavior when reached |
|---|---|---|
| Activation radius | 60 m around a controlled vehicle; 25 m around the avatar | Nothing outside the radius is generated |
| Active proxy count | 256 | Evict the farthest proxies first; never evict within 10 m of the controlled entity |
| Proxy data memory | 8 MB | Same eviction; log `ProxyBudget` |
| Creation per frame | 16 | Queue the rest for later frames, nearest first |
| Lifetime outside radius | 5 s, with 10 m hysteresis | Released |
| Global teardown | Return to `Building`, `Loading`, `Unloading`, safe-disable | All proxies released; count must be zero |

If the proxies needed within 10 m of the entity alone exceed the cap, the controller limits speed in that area and
logs the event, rather than letting the entity pass through geometry. These caps are laboratory starting points, not
performance budgets; real budgets come from measurements ([SPEC-PERF](PERFORMANCE_BUDGET.md)).

## 9. Camera clipping (REQ-PLAYER-003)

- The third-person and chase rigs probe from the pivot (avatar head or vehicle roof) to the desired camera position
  against surfaces and proxies, and pull the camera in to the first hit minus a margin.
- The camera never ends inside a building or below the current level's surface; a minimum distance applies, below
  which the avatar fades out instead of the camera entering it.
- In tunnels and under elevated decks, the probe uses the level context so that the camera does not pop above the deck.
- Camera takeover itself is unverified (OPEN-111).

## 10. Contact with native traffic and pedestrians (REQ-WORLD-004, OPEN-032)

Hard constraint: **the mod never writes native agents' movement** (INV-CTRL-02). Candidate rules for the laboratory:

| Option | Description | Native state risk | Feel |
|---|---|---|---|
| R1 Solid for the player | The controlled vehicle collides with approximate boxes of nearby native vehicles and stops or deflects; native vehicles are not moved | None from contact itself; native vehicles may still drive into the player if they cannot perceive it | Good |
| R2 Pass-through with feedback | No physical contact; visual and audio feedback, and a career penalty if applicable | None | Weak |
| R3 Presence | The controlled vehicle registers its own presence where native AI looks for obstacles so that AI brakes or yields (the base mod reportedly does something similar **[Source-inspected, pending re-check]**) | Medium: stale entries if cleanup fails (INV-ENT-07) | Good when combined with R1 |
| R4 Push native agents | The mod displaces native vehicles or pedestrians | High: violates single authority | **Excluded** |

Pedestrians: the controlled vehicle either brakes automatically before pedestrians (P1) or passes through with
feedback (P2). Citizens are never harmed or removed. **Proposal for evaluation:** R1 + R3 for vehicles, P1 for
pedestrians, chosen in PHASE-03 with evidence and recorded here.

## 11. Laboratory design (shared with ADR-0004)

Run in a disposable lab city ([PHASE-03](../planning/phases/PHASE-03-surfaces-collisions-and-physics.md)), never the
owner's saves. Each scenario runs at an uncapped frame rate, a 60 FPS cap, a 30 FPS cap, and with injected stalls.

| Scenario | Content | Pass condition |
|---|---|---|
| S01 | Flat road, straight and curved | Stable contact; identical braking distance across frame-rate settings within tolerance |
| S02 | Sidewalk with curbs | Avatar steps; vehicle obeys curb rule |
| S03 | Ramp and steep slope | Climbs below the limit; blocked above |
| S04 | Bridge over a road | No switch to the lower road in 20 crossings at varied speeds |
| S05 | Tunnel under terrain | No pop to the terrain above; camera stays inside |
| S06 | Stacked interchange | Level follows the driven ramp only |
| S07 | Elevated road edge | Blocked or `Recovery`; never a drop to the lower level |
| S08 | Building facade at 10, 30, 60 km/h | No penetration beyond tolerance |
| S09 | Narrow alley between buildings | No snagging, no tunneling |
| S10 | Dense block for proxy load | Proxy count and memory within caps; no frame spike beyond the investigation trigger |
| S11 | Native traffic encounter | Contact rule candidates R1–R3 compared; no native vehicle left stuck |
| S12 | Pedestrian crossing | P1/P2 compared; no citizen state change |
| S13 | Pause, focus loss, alt-tab stall | No jump, no stuck input ([SPEC-TIME](GAME_TIME_AND_PAUSE.md)) |
| S14 | Third-person camera near walls, tunnels, decks | No clipping into geometry |

Findings go to [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md); decisions to ADR-0004 and this spec.

## 12. Failure handling

- `Unknown` ground → hold pose; persistent `Unknown` (1 s) or `Invalid` ground → `Recovery` ([SPEC-RECOVERY](ERROR_RECOVERY.md)).
- Proxy budget exceeded → eviction and speed limiting, logged; never silent pass-through.
- Penetration detected after a step (should not happen) → revert to the last valid pose and log.
- Contact queries failing repeatedly → WorldContact marked Degraded → forced `Building`, because no character state
  is safe without ground answers.

## 13. Invariants

| ID | Invariant | Test |
|---|---|---|
| INV-CON-01 | A controlled entity never changes level except along a connected surface. | TEST-WORLD-001 |
| INV-CON-02 | A controlled entity never ends a substep inside a building or static obstacle beyond tolerance. | TEST-WORLD-002, TEST-PLAYER-004 |
| INV-CON-03 | Proxy count, memory, and lifetime stay within caps; zero proxies after teardown. | TEST-WORLD-003 |
| INV-CON-04 | No native agent's movement is written as a result of contact. | TEST-WORLD-004 |
| INV-CON-05 | `Unknown` is never treated as valid ground. | TEST-WORLD-001 |

## 14. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| Correct surface and level on roads, sidewalks, slopes, bridges, tunnels, elevated and stacked roads. | TEST-WORLD-001 | Not run |
| No passing through buildings and static obstacles (vehicle and avatar). | TEST-WORLD-002, TEST-PLAYER-004 | Not run |
| Locally generated collision geometry is bounded and released. | TEST-WORLD-003 | Not run |
| One consistent contact rule with native traffic and pedestrians; no native corruption. | TEST-WORLD-004 | Not run |
| Third-person camera does not clip into buildings or terrain. | TEST-PLAYER-003 | Not run |
| Behavior is consistent under frame-rate variation and stalls. | TEST-TIME-003, TEST-DRIVE-002 | Not run |

## 15. Open questions and risks

- OPEN-007 (surface information), OPEN-008 (collision data and proxy cost), OPEN-009 (physics approach), OPEN-024
  (street-level rendering), OPEN-032 (traffic contact rule), OPEN-107 (update ordering), OPEN-111 (camera takeover).
- RISK-003 (no usable collision data), RISK-008 (performance), RISK-103 (substep cost spiral).

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
