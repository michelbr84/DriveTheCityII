# Expansions — Drive the City II

| Field | Value |
|---|---|
| ID | DOC-EXPANSIONS |
| Document status | Draft |
| Release scope | Post-1.0 and explicit v1.0 exclusions (nothing here is a v1.0 requirement) |
| Phases | None scheduled. Optional exploratory spikes require owner authorization and never gate a phase. |
| Responsible role | Coordinator (maintainer); project owner decides whether an expansion enters scope |
| Related requirements | None by design: expansions have no `REQ-*` IDs ([PRD](PRD.md) section 5). Boundary requirements: REQ-CITY-001, REQ-CITY-003, REQ-COMPANY-002, REQ-BUS-001, REQ-FREIGHT-001, REQ-DRIVE-004, REQ-HOME-002 |
| Related documents | [PRD](PRD.md) section 7, [VISION](VISION.md), [ROADMAP](../../ROADMAP.md) section 8, [FEASIBILITY_MATRIX](../research/FEASIBILITY_MATRIX.md), [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md) |
| Last updated | 2026-10-18 |

> **Status:** these are ideas and exclusions, not plans. None of them is designed, scheduled, or promised.

## 1. Purpose

This document keeps attractive ideas out of v1.0 without losing them (RISK-012). It records every explicit v1.0
exclusion from the owner brief and every post-1.0 idea from the historical plan, with what each one needs before it
can be considered.

## 2. Entry gate (applies to every expansion)

An expansion enters scope only when **all** of the following are true:

1. **Owner decision:** the project owner approves a PRD change that adds `REQ-*` IDs for it. Change control is
   described in [PRD](PRD.md) section 11. [TRACEABILITY](../planning/TRACEABILITY.md) is updated in the same change.
2. **Prerequisites met:** the v1.0 systems it builds on have Passed validation with recorded evidence.
3. **Feasibility shown:** a bounded spike, authorized by the owner, has recorded its result in
   [FEASIBILITY_MATRIX](../research/FEASIBILITY_MATRIX.md) and, where relevant, an ADR.
4. **Safety preserved:** the expansion's tests include save-safety, no-duplication, and clean-exit checks. It does
   not weaken any v1.0 invariant.
5. **Honest presentation:** until it is validated, anything shipped from it is off by default and labeled
   experimental ([SPEC-UI](../experience/UI_AND_HUD.md)).

Before v1.0 ships, an expansion may only be explored as a non-gating spike that the owner authorizes explicitly (for
example, CONF-003 for articulated trucks). It never displaces a `Must` requirement.

## 3. Explicit v1.0 exclusions **[Owner requirement]**

### EXP-001 — Interiors in every building

- **Description:** navigable interiors for homes, shops, offices, and service buildings.
- **Why not v1.0:** CS2 buildings are exterior assets, and interiors would need new art, collision, lighting, and
  transitions for every building type. v1.0 uses exterior access and service points (REQ-HOME-002).
- **Prerequisites:** stable on-foot movement and world contact (PHASE-03, PHASE-04); an authorized interior asset
  pipeline; a way to attach an interior to a building instance.
- **Validation needed:** a spike with one building type covering the transition in and out, collision, performance,
  and save/load of the player's position.
- **Related:** RISK-003, RISK-008, RISK-015; OPEN-008, OPEN-010. See also EXP-016 (selected interiors only).
- **Gate:** Section 2, plus an owner decision on which building types get interiors first (EXP-016 is the smaller first step).

### EXP-002 — Native citizens as taxi and bus passengers

- **Description:** carrying the city's own citizens, including native fares and schedules, instead of mod-owned
  career passengers. The historical plan called this the bus career's "second integration level".
- **Why not v1.0:** it writes into native citizen and transit state. That breaks the v1.0 read-only boundary
  (REQ-CITY-001) and risks double satisfaction of demand (REQ-CITY-003). v1.0 uses career passengers (REQ-TAXI-002,
  REQ-BUS-001; CONF-011).
- **Prerequisites:** a validated read model of citizens' trips and of the transit system; the PHASE-10 economy-write
  experiment framework; a reconciliation design so that a trip is never served twice.
- **Validation needed:** a lab-city experiment showing that native trips handed to the player complete or return
  cleanly, with no stuck citizens and unchanged simulation health after save/load and mod removal.
- **Related:** RISK-018; OPEN-020, OPEN-021, OPEN-022.
- **Gate:** Section 2, plus a recorded PHASE-10 economy-write experiment result and a reconciliation ADR. Ships off by default and labeled experimental until validated.

### EXP-003 — Physical employee fleets

- **Description:** company employees driving real vehicles through the city.
- **Why not v1.0:** v1.0 companies are an explicitly labeled managerial simulation (REQ-COMPANY-002). Physical fleets
  need spawning, AI driving, persistence, and traffic integration for many mod-owned vehicles.
- **Prerequisites:** reliable career-vehicle spawning and removal (OPEN-012); an AI-driving strategy for mod vehicles;
  a performance budget for extra entities (SPEC-PERF).
- **Validation needed:** a bounded fleet in a lab city across long sessions, with entity counts, lane cleanup,
  frame-time measurement, and save/load stability.
- **Related:** RISK-004, RISK-008; OPEN-012.
- **Gate:** Section 2, plus a measured performance budget for the extra entities (PHASE-12 baselines). Until then, the managerial-simulation label stays.

### EXP-004 — Articulated trucks

- **Description:** tractor-trailer combinations with articulated physics, coupling, and reversing.
- **Why not v1.0:** v1.0 freight uses rigid trucks (REQ-FREIGHT-001). Articulation needs its own physics prototype; it
  cannot be a rigid truck with a different look **[Historical input]** (CONF-003).
- **Prerequisites:** the PHASE-03 physics decision (ADR-0004); stable rigid-truck handling and collision; a trailer
  model with authorized assets.
- **Validation needed:** an articulation prototype at junctions, on slopes, at loading bays, and in reverse, with
  consistent behavior across frame rates (REQ-DRIVE-002 criteria).
- **Related:** CONF-003; OPEN-009; RISK-003.
- **Gate:** Section 2, plus an accepted ADR-0004 and a recorded articulation prototype. A PHASE-08 spike is allowed only with owner authorization and never gates PHASE-08 (CONF-003).

### EXP-005 — Municipal-economy writes

- **Description:** career work that changes the city's economy, for example by satisfying real demand for goods or
  moving money into or out of the city budget.
- **Why not v1.0:** v1.0 reads the city and does not write native economic or service state (REQ-CITY-001). At most,
  a single-operation experiment that is separately gated and off by default may exist (REQ-CITY-003, PHASE-10). This
  expansion is the full, default-on integration.
- **Prerequisites:** the PHASE-10 experiment with recorded evidence; a reconciliation model so that native and mod
  deliveries never satisfy the same demand twice; clean-exit behavior for written state.
- **Validation needed:** budget and demand stability over long sessions, save/load cycles, and mod removal in a lab
  city.
- **Related:** RISK-017; OPEN-021, OPEN-022.
- **Gate:** Section 2, plus the recorded PHASE-10 experiment and an ADR for the reconciliation model. Default-on behavior requires an explicit owner decision.

### EXP-006 — Complex police pursuits and combat

- **Description:** police chases of the player, wanted levels, or any combat.
- **Why not v1.0:** this is outside the product fantasy (VISION pillar 4: work, not combat). It also requires
  retargeting native service vehicles, which conflicts with the single-authority and read-only boundaries. The
  candidate base mod describes a police-chase feature as hidden and disabled in its public build
  **[Documented upstream]** ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). It is not part of our scope, and it is
  not a reuse candidate for v1.0.
- **Prerequisites:** an owner decision that this fits the product at all; EXP-005-level write safety for native
  service vehicles.
- **Validation needed:** native police state returns to normal after every chase, with save/load and mod removal.
- **Related:** RISK-004, RISK-015. Non-combat police missions are a separate idea (EXP-014).
- **Gate:** Section 2, plus an owner decision that pursuits fit the product. Combat requires a separate scope review.

### EXP-007 — Part-by-part mechanical assembly

- **Description:** individual engine, gearbox, brake, and tire parts that are installed, swapped, and repaired
  separately.
- **Why not v1.0:** v1.0 uses simplified wear and maintenance (REQ-GARAGE-004). No missing part may ever block
  driving (REQ-GARAGE-006), which a parts economy would make much harder to guarantee.
- **Prerequisites:** a stable garage and economy (PHASE-07); a parts catalog with guaranteed availability rules.
- **Validation needed:** a softlock analysis showing that every parts state has a recovery path; measurable effects
  per part (REQ-GARAGE-005 style).
- **Related:** RISK-019.
- **Gate:** Section 2, plus a softlock analysis reviewed by QA.

### EXP-008 — Structural vehicle deformation

- **Description:** visible and physical body damage that changes shape and handling.
- **Why not v1.0:** REQ-DRIVE-002 excludes structural physics. Deformable meshes for vehicle models we may not own
  (OPEN-034) are a large art and physics problem.
- **Prerequisites:** the ADR-0004 physics decision; own or authorized deformable vehicle assets.
- **Validation needed:** a performance and consistency spike; persistence of damage state across save/load.
- **Related:** RISK-008, RISK-015; OPEN-009, OPEN-034.
- **Gate:** Section 2, plus an accepted ADR-0004 that supports deformation and authorized deformable assets.

### EXP-009 — Multiplayer

- **Description:** several players driving or working in the same city session.
- **Why not v1.0:** CS2's city simulation is single-player, and our authority model assumes one local player. It
  needs a **separate study of synchronization and simulation authority**: who owns the city, how native entities
  and career state replicate, and how saves are reconciled. It must not be assumed to be provided by any launcher or
  platform **[Historical input]**. Melty advertises multiplayer for its own mashups **[Documented upstream]**; that
  does not apply to this project ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)).
- **Prerequisites:** a completed v1.0; the synchronization and authority study; an owner decision on the network
  model and the hosting cost.
- **Validation needed:** a two-client prototype in a lab city with deterministic career settlement and no save
  divergence.
- **Related:** RISK-004, RISK-005, RISK-006.
- **Gate:** Section 2, plus a completed synchronization and authority study recorded as an ADR, and an owner decision on hosting.

### EXP-010 — Detailed cockpit for every vehicle

- **Description:** modeled interiors with working instruments for all vehicles.
- **Why not v1.0:** most available vehicle models may lack suitable interiors. v1.0 offers a hood or driver-position
  camera with documented limitations, and a detailed cockpit only where a suitable interior exists (REQ-DRIVE-004,
  [SPEC-ART](../experience/ART_AND_ANIMATION.md)).
- **Prerequisites:** own or authorized interior models per vehicle (OPEN-034); a camera clipping solution.
- **Validation needed:** per-vehicle checks for clipping, readability, and performance.
- **Related:** RISK-015; OPEN-034.
- **Gate:** Section 2, applied per vehicle model: a cockpit is added for a model only when its interior asset is authorized and passes the clipping checks.

## 4. Post-1.0 ideas

The historical plan lists these **[Historical input]**, grouped as urban services, immersion, and advanced logistics
([ROADMAP](../../ROADMAP.md) section 8). Each needs "its own validation" **[Historical input]**.

### EXP-011 — Towing missions

- **Description:** a tow-truck career that recovers broken-down career vehicles or mission-generated incidents.
- **Why not v1.0:** it needs vehicle-to-vehicle attachment physics and a new mission family. Recovery in v1.0 is a
  service, not gameplay (REQ-GARAGE-006).
- **Prerequisites:** the physics decision; stable mission framework and career-vehicle lifecycle.
- **Validation needed:** an attachment-stability spike; towing a native vehicle would also need EXP-005-level write
  safety.
- **Related:** OPEN-009, OPEN-012.
- **Gate:** Section 2, plus an attachment-physics spike result.

### EXP-012 — Ambulance missions

- **Description:** emergency transport with urgency and patient-comfort scoring.
- **Why not v1.0:** a credible version would interact with the city's native health services. A career-only version
  is possible but would need careful honesty labeling (VISION pillar 2).
- **Prerequisites:** a decision between career-only and native-integrated versions; for the native version, EXP-002
  and EXP-005 groundwork.
- **Validation needed:** no interference with native health-service dispatch.
- **Related:** RISK-018; OPEN-021.
- **Gate:** Section 2, plus an owner decision between the career-only and native-integrated versions.

### EXP-013 — Fire missions

- **Description:** driving fire vehicles to incidents.
- **Why not v1.0:** this requires reading, and probably writing, native fire state, plus new effects and
  interactions.
- **Prerequisites:** EXP-005-level write safety; a decision on how to represent incidents honestly.
- **Validation needed:** native fire-service state remains consistent across save/load and mod removal.
- **Related:** RISK-017.
- **Gate:** Section 2, plus EXP-005-level write safety for any native fire-service state.

### EXP-014 — Police missions (non-combat)

- **Description:** patrol, traffic-control, or escort missions without pursuits or combat (pursuits are EXP-006).
- **Why not v1.0:** this is a new mission family, and representing traffic enforcement fairly needs reliable traffic
  reading.
- **Prerequisites:** a stable mission framework; reliable reading of traffic and signals.
- **Validation needed:** the missions are completable in varied cities; native traffic is not affected.
- **Related:** RISK-004.
- **Gate:** Section 2, plus a design review confirming the missions stay non-combat (otherwise EXP-006 applies).

### EXP-015 — Vehicle dealership

- **Description:** a physical dealership where the player walks among vehicles, inspects them, and test-drives them.
- **Why not v1.0:** v1.0 purchasing happens at an exterior service point or through a menu (REQ-GARAGE-002,
  REQ-HOME-002).
- **Prerequisites:** a reliable display of spawned vehicles; possibly EXP-016 interiors.
- **Validation needed:** spawned display vehicles are cleaned up completely (REQ-ENTITY-003 style tests).
- **Related:** OPEN-012, OPEN-034.
- **Gate:** Section 2, plus display-vehicle cleanup tests.

### EXP-016 — Own interiors for the garage and the home

- **Description:** a small number of mod-authored interiors attached to the player's chosen home and garage only.
  This is far narrower than EXP-001.
- **Why not v1.0:** it needs authorized interior assets and an interior transition system. v1.0 uses exterior points
  (REQ-HOME-001).
- **Prerequisites:** stable on-foot movement; an asset pipeline; ASSET_PROVENANCE entries.
- **Validation needed:** a transition spike with safe return to the exterior after save/load and city edits.
- **Related:** RISK-009, RISK-015; OPEN-010.
- **Gate:** Section 2, plus authorized interior assets recorded in ASSET_PROVENANCE.

### EXP-017 — Expanded avatar animation set

- **Description:** additional animations beyond idle, walk, run, and entry/exit, such as interactions, sitting, or
  gestures at service points.
- **Why not v1.0:** REQ-ART-001 covers the minimum set. More animations depend on the avatar decision (OPEN-010).
- **Prerequisites:** the PHASE-04 avatar representation; an authorized animation source.
- **Validation needed:** a provenance check and performance cost per animation.
- **Related:** RISK-009.
- **Gate:** Section 2, applied per animation: each one needs a provenance entry before use.

### EXP-018 — Advanced steering-wheel support

- **Description:** steering wheels and pedals with force feedback and calibration.
- **Why not v1.0:** v1.0 supports keyboard and mouse plus gamepad (REQ-INPUT-002). Device support in the mod's input
  context is not yet verified even for gamepads (OPEN-025).
- **Prerequisites:** OPEN-025 answered; an input path that exposes wheel axes and force feedback.
- **Validation needed:** a test with at least one wheel model, covering axis mapping, calibration, and coexistence
  with the game's input.
- **Related:** OPEN-014, OPEN-025.
- **Gate:** Section 2, plus OPEN-025 answered with evidence.

### EXP-019 — Physical cargo

- **Description:** cargo as physical objects that are loaded, secured, and can shift or fall.
- **Why not v1.0:** v1.0 cargo is a mission object with abstract mass, fragility, and capacity (REQ-DELIV-001,
  REQ-FREIGHT-001).
- **Prerequisites:** the physics decision; collision with vehicle beds; persistence of object state.
- **Validation needed:** stability across frame rates and save/load; bounded entity counts.
- **Related:** OPEN-009; RISK-008.
- **Gate:** Section 2, plus an accepted ADR-0004 and a measured entity budget.

## 5. Other ideas recorded for completeness

### EXP-020 — Other vehicle types (rail, watercraft)

- **Description:** driving trains or boats. **[Proposal]** This is not part of the owner brief. The candidate base mod
  contains an older, unregistered control stack with train and watercraft navigation code **[Source-inspected]**
  ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). That is not evidence that it works.
- **Why not v1.0:** this is outside the four v1.0 careers.
- **Prerequisites:** a stable road-vehicle foundation; an owner decision.
- **Validation needed:** a separate feasibility spike per vehicle type.
- **Related:** RISK-012.
- **Gate:** Section 2, plus an owner decision to widen the product beyond road vehicles.

### EXP-021 — Parkour, climbing, and swimming

- **Description:** on-foot traversal beyond walking and running.
- **Why not v1.0:** the historical plan places it explicitly outside the on-foot milestone **[Historical input]**.
  REQ-PLAYER-002 covers walking, running, and small elevation changes.
- **Prerequisites:** reliable building collision (OPEN-008) and water surface data.
- **Validation needed:** no clipping or falling below valid ground (REQ-PLAYER-004 style tests).
- **Related:** RISK-003.
- **Gate:** Section 2, plus OPEN-008 answered with evidence.

### EXP-022 — Entering and exiting moving vehicles

- **Description:** getting in or out while the vehicle is moving, or animated door interactions while it is moving.
- **Why not v1.0:** entry and exit start stationary for safety (REQ-TRANS-001).
- **Prerequisites:** a stable PHASE-05 transition system; the avatar animation decision.
- **Validation needed:** the 50-cycle transition matrix extended to moving cases.
- **Related:** RISK-009.
- **Gate:** Section 2, plus the PHASE-05 transition gate passed with evidence.

## 6. Change log of this list

Entries are never deleted. If an expansion is promoted to scope, its entry links to the PRD change that replaced it.
If it is rejected, it is marked Rejected with the owner's decision date.
