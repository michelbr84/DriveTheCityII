# Product Requirements Document (PRD)

| Field | Value |
|---|---|
| ID | DOC-PRD |
| Document status | In review (requirements captured from the owner brief; awaiting owner review of wording and IDs) |
| Release scope | v1.0 (expansions are listed separately) |
| Phases | PHASE-00 – PHASE-13 |
| Responsible role | Coordinator (maintainer); project owner approves scope changes |
| Related documents | [VISION](VISION.md), [GAME_DESIGN](GAME_DESIGN.md), [PLAYER_JOURNEYS](PLAYER_JOURNEYS.md), [EXPANSIONS](EXPANSIONS.md), [ROADMAP](../../ROADMAP.md), [TRACEABILITY](../planning/TRACEABILITY.md) |
| Last updated | 2026-10-18 |

> **Status reminder:** this document defines targets. Nothing in it is implemented, built, or tested. Current state:
> pre-development, Phase 00 not completed (see [PROJECT_STATUS](../PROJECT_STATUS.md)).

## 1. Purpose and authority

This PRD is the single source of truth for **what Drive the City II v1.0 must deliver** and for the stable
requirement identifiers (`REQ-*`) used throughout the documentation. Specifications describe how requirements are
met. The [ROADMAP](../../ROADMAP.md) decides when they are met. [TRACEABILITY](../planning/TRACEABILITY.md) links
each requirement to its specification, phase, planned test, and evidence.

## 2. Product summary

Drive the City II (working title) is a **Cities: Skylines II (CS2) code mod for Windows** that turns the player's own
city into a place to live and work. You build your city normally, then switch into a visible character, walk to a
vehicle, drive through the road network you built, take transportation jobs, earn money and experience, rent or buy
vehicles, and eventually run a small transport company. Then you return to city building at any time.

CS2 is the world. The mod is the playable experience inside it.

## 3. Roles of the external components

| Component | Role | Constraint |
|---|---|---|
| Cities: Skylines II | Host game and sole authority for the city world and its native simulation. | We do not export the city to another engine or run a second synchronized simulation. **[Owner requirement]** |
| Beta Test Driving Mod | Candidate source-code foundation for direct vehicle control. | Reuse depends on the license audit and baseline tests ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). Reported capabilities are not proof. **[Owner requirement]** |
| Motor Town: Behind The Wheel | Behavioral and design reference for careers, ownership, progression, and businesses. | We implement original systems and assume no transferable code, assets, physics, or API ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)). **[Owner requirement]** |
| Streets of SimCity | Inspiration for driving through a player-built city. | No reproduction of protected content or branding. **[Owner requirement]** |
| Melty | Optional complementary application and workflow, still to be evaluated. | The project and documentation must work without it. No assumed folder, workspace, or compatibility features ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)). **[Owner requirement]** |

The core distribution requires only CS2 and explicitly documented mod dependencies. It does not require a running
Motor Town or Melty (REQ-REL-004).

## 4. Core experience

Build a city → enter character mode → walk to a vehicle → enter and drive → accept and complete transportation work →
receive money and experience → park and exit → save → close and reopen → continue, without corrupting the city or
duplicating rewards. **[Owner requirement]**

Detailed journeys, including failure and recovery paths, are in [PLAYER_JOURNEYS](PLAYER_JOURNEYS.md).

## 5. Requirement conventions

- **Priority:** `Must` = mandatory for v1.0 · `Should` = v1.0 target that may move to an expansion only through a
  recorded scope review · expansion items live in [EXPANSIONS](EXPANSIONS.md) and have no `REQ-*` ID.
- **Basis:** `Owner` = stated or required by the owner brief (2026-10-18) · `Derived` = a proposal written to make an
  owner requirement testable; the owner may revise it.
- **Primary test:** `REQ-X-NNN` is verified primarily by `TEST-X-NNN`. Tests are defined in
  [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md) and [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md).
  Validation status is **Not run** for every requirement.
- Infeasibility discovered later triggers a scope review ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §8). It is
  never a silent removal.

## 6. Requirements catalog

### 6.1 On-foot character and cameras (PLAYER)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-PLAYER-001 | In character mode the player controls a visible, mod-owned avatar. The mod does not take over a native citizen who has a household, job, or schedule. | Must | Owner | [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md) | PHASE-04 |
| REQ-PLAYER-002 | The avatar can walk, run, turn, use free look, and cross small elevation changes (curbs, gentle slopes, low steps) on valid walkable surfaces. | Must | Owner | [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md) | PHASE-04 |
| REQ-PLAYER-003 | The player can switch between first-person and third-person cameras. The third-person camera avoids clipping into buildings and terrain. | Must | Owner | [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md) | PHASE-04 |
| REQ-PLAYER-004 | The avatar cannot pass through building facades or static obstacles and cannot fall below valid ground. A detected invalid position triggers `Recovery`. | Must | Owner | [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) | PHASE-04 |

### 6.2 Driving and vehicle classes (DRIVE)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-DRIVE-001 | The player can drive a car with throttle, brake, reverse, progressive steering, a parking brake, a speedometer, and a chase camera. | Must | Owner | [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md) | PHASE-02 |
| REQ-DRIVE-002 | Handling is semi-realistic and consistent: the same inputs on the same surface produce the same behavior across the tested frame-rate range. Simulator-grade or structural physics is not required. | Must | Owner | [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md), [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md) | PHASE-03 |
| REQ-DRIVE-003 | At least four vehicle classes (car, light delivery vehicle, rigid truck, bus) have distinct handling parameters (such as mass, acceleration, braking, and turning) and distinct capacity rules. | Must | Owner | [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md) | PHASE-03, PHASE-07, PHASE-08 |
| REQ-DRIVE-004 | A hood or driver-position camera is offered, with documented limitations. A detailed cockpit is shown only for vehicle models that have a suitable interior. | Should | Owner | [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md), [SPEC-ART](../experience/ART_AND_ANIMATION.md) | PHASE-11 |

### 6.3 World contact (WORLD)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-WORLD-001 | The mod determines the valid driving or walking surface on roads, sidewalks, slopes, bridges, tunnels, and elevated or stacked roads. It never moves the controlled entity to a different level (for example, from a bridge deck to the road below). | Must | Owner | [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) | PHASE-03 |
| REQ-WORLD-002 | Controlled vehicles and the avatar collide usefully with buildings and static obstacles: they do not pass through them. | Must | Owner | [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) | PHASE-03, PHASE-04 |
| REQ-WORLD-003 | Any collision geometry generated locally by the mod is bounded in count, memory, and lifetime. It is removed when no longer needed and when the mod unloads. | Must | Owner | [SPEC-CONTACT](../architecture/WORLD_CONTACT.md), [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) | PHASE-03 |
| REQ-WORLD-004 | Contact between the controlled vehicle and native traffic or pedestrians follows one defined, consistent rule that never corrupts the state of native agents. The rule is chosen in PHASE-03 (see OPEN-032). | Must | Derived | [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) | PHASE-03 |

### 6.4 Control authority (CTRL)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-CTRL-001 | Exactly one movement authority writes to any controlled entity at a time. Native AI and the player controller never write the same vehicle simultaneously. | Must | Owner | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md) | PHASE-02 |
| REQ-CTRL-002 | Player control uses the explicit states `Building`, `OnFoot`, `EnteringVehicle`, `Driving`, `ExitingVehicle`, and `Recovery`, plus the `Paused`, `Menu`, `Loading`, and `Unloading` overlays. Each has defined entry and exit guards and a single camera owner. | Must | Owner | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md) | PHASE-01, PHASE-02, PHASE-04, PHASE-05 |
| REQ-CTRL-003 | Character-mode input does not move the building camera or activate city-building tools. Building-mode input does not move the avatar or a controlled vehicle. | Must | Owner | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md) | PHASE-02 |
| REQ-CTRL-004 | The player can return to `Building` from any stable character-mode state. Control, camera, and simulation speed are restored, and career state is preserved. | Must | Owner | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) | PHASE-05 |

### 6.5 Walking/driving transitions (TRANS)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-TRANS-001 | The player can enter a nearby, available vehicle through proximity selection and an interaction prompt. Entry initially requires the vehicle to be stationary. | Must | Owner | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md) | PHASE-05 |
| REQ-TRANS-002 | Exiting places the avatar at a validated safe position (free space, headroom, correct elevation, preferred curb side). Alternate exit points are tried when one is blocked. When no safe point exists, the player gets clear feedback or `Recovery` is triggered. The avatar is never placed inside walls, traffic, or at the wrong elevation. | Must | Owner | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md) | PHASE-05 |
| REQ-TRANS-003 | If a vehicle is removed or invalidated during `EnteringVehicle` or `ExitingVehicle`, the transition aborts safely without losing control, duplicating entities, or leaving the camera detached. | Must | Owner | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-05 |
| REQ-TRANS-004 | Ending a drive distinguishes three outcomes: parking a career vehicle, handing back a borrowed native vehicle (sandbox mode only), and removing temporary objects created by the mod. | Must | Owner | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-05 |

### 6.6 Entity lifecycle (ENTITY)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-ENTITY-001 | Handing back a borrowed native vehicle rebuilds its navigation and clears any lane reservation or blocking state. It neither resumes a stale route nor leaves invisible traffic blockers. | Must | Owner | [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-02 |
| REQ-ENTITY-002 | Career mode uses only career vehicles (rental, owned, or mod-provided service vehicles). It never silently makes a citizen's vehicle or a native city-service vehicle the player's property. | Must | Owner | [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-02, PHASE-06 |
| REQ-ENTITY-003 | Every entity created by the mod is registered with an owner and a lifecycle. All are despawned or persisted correctly on mission end, mode change, save, unload, and mod disable, leaving no orphans. | Must | Owner | [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-01, PHASE-05 |

### 6.7 Time and focus (TIME)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-TIME-001 | Entering character mode records the prior simulation speed. Active exploration runs at normal speed **[Proposal]**. Returning to `Building` restores the recorded speed. | Must | Owner | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) | PHASE-03 |
| REQ-TIME-002 | Pause freezes player motion, mission timers, and career clocks. Losing window focus clears held inputs. | Must | Owner | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) | PHASE-03 |
| REQ-TIME-003 | Per-frame motion is bounded, so frame stalls or low frame rates never produce large position jumps or tunneling through geometry. | Must | Owner | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) | PHASE-03 |
| REQ-TIME-004 | Career timers and company earnings are computed from persisted time checkpoints. Mode switches, pauses, and save/load cannot duplicate or skip time-based income or penalties. | Must | Owner | [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) | PHASE-06, PHASE-09 |

### 6.8 Mission framework (CAREER)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-CAREER-001 | All careers use one shared mission lifecycle: `Offered`, `Accepted`, `Active` (with ordered legs), `Completed`, `Cancelled`, `Failed`, `Invalidated`, `Expired`. | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) | PHASE-06 |
| REQ-CAREER-002 | A mission is offered only after its access points have been validated as reachable through the road network from the player's position: the eligible vehicle's position, or the road access point nearest the player when on foot. | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-06 |
| REQ-CAREER-003 | Pickups and drop-offs use validated access points (curbside, stop, or loading bay), never building centers. | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-06 |
| REQ-CAREER-004 | Each terminal mission outcome settles exactly once through a unique TransactionId, including after save/load, reload, or replayed events (idempotent payout). | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | PHASE-06 |
| REQ-CAREER-005 | The player can cancel an accepted mission. Cancellation and failure have explicit, explained consequences that never make the career unrecoverable. | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) | PHASE-06 |
| REQ-CAREER-006 | Missions made impossible by city edits or entity loss are detected, then rerouted or invalidated with a no-fault cancellation. No mission remains impossible indefinitely. | Must | Owner | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) | PHASE-08, PHASE-10 |

### 6.9 Careers (TAXI, DELIV, FREIGHT, BUS)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-TAXI-001 | Complete taxi loop: on foot → rent a vehicle → accept a fare → pick up a career passenger → transport → drop off → single payment → park → exit → save → reload and continue. | Must | Owner | [SPEC-TAXI](../gameplay/careers/TAXI.md) | PHASE-06 |
| REQ-TAXI-002 | Taxi passengers are mod-owned career passengers, with represented boarding and alighting. They are never presented as native citizens. | Must | Owner | [SPEC-TAXI](../gameplay/careers/TAXI.md) | PHASE-06 |
| REQ-TAXI-003 | Fares are derived from route distance and service quality **[Proposal]**. The player sees a fare estimate before accepting. | Should | Derived | [SPEC-TAXI](../gameplay/careers/TAXI.md) | PHASE-06 |
| REQ-DELIV-001 | Urban delivery contracts include a pickup, one or more drop-off stops, deadlines, cargo fragility, and a driving evaluation that affects payment. | Must | Owner | [SPEC-DELIVERY](../gameplay/careers/DELIVERIES.md) | PHASE-08 |
| REQ-FREIGHT-001 | Rigid-truck freight contracts load and unload at validated industrial or commercial access points. Cargo mass and capacity affect handling and eligibility. Articulated trucks are not part of v1.0. | Must | Owner | [SPEC-FREIGHT](../gameplay/careers/FREIGHT.md) | PHASE-08 |
| REQ-BUS-001 | The bus career follows an existing city transit line's stops in order, with mod-owned career passengers boarding and alighting, vehicle capacity, and a trip evaluation. Native citizens are not carried in v1.0. | Must | Owner | [SPEC-BUS](../gameplay/careers/BUS.md) | PHASE-08 |
| REQ-BUS-002 | Line or stop changes during a shift cause safe re-planning or a no-fault cancellation. | Must | Owner | [SPEC-BUS](../gameplay/careers/BUS.md), [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-08, PHASE-10 |

### 6.10 Economy and progression (ECON)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-ECON-001 | The player has a personal wallet separate from the municipal budget. No career transaction changes city finances in v1.0. | Must | Owner | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | PHASE-06 |
| REQ-ECON-002 | Every money movement is recorded in a persistent, append-only ledger with unique TransactionIds, which the player can view as transaction history. | Must | Owner | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | PHASE-06 (minimal), PHASE-07 |
| REQ-ECON-003 | Experience per career, reputation, and unlocks are earned from completed work and persist per career profile. | Must | Owner | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | PHASE-07 |
| REQ-ECON-004 | Insolvency cannot permanently block progression. An always-available starter job and a basic rental path (or a clearly explained recovery) exist when the player has no money or no usable vehicle. | Must | Owner | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | PHASE-07 |
| REQ-ECON-005 | An economic tutorial explains the wallet, costs, income, and recovery options. | Must | Owner | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), [SPEC-ONBOARD](../experience/ONBOARDING.md) | PHASE-07, PHASE-11 |

### 6.11 Vehicles, ownership, and garage (GARAGE)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-GARAGE-001 | Vehicles can be rented at a service point. A basic rental is always available as a recovery path. | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | PHASE-06 (minimal), PHASE-07 |
| REQ-GARAGE-002 | Vehicles can be purchased and sold, and ownership is tied to the career profile of that city. | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | PHASE-07 |
| REQ-GARAGE-003 | Vehicles consume fuel and can be refuelled at accessible service points. | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | PHASE-07 |
| REQ-GARAGE-004 | Vehicles wear, and simplified maintenance and repair are available at accessible exterior service points. No dedicated building type is required in the city. | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md) | PHASE-07 |
| REQ-GARAGE-005 | Upgrades produce verifiable effects (for example braking distance, capacity, or grip) that tests can measure. | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | PHASE-07 |
| REQ-GARAGE-006 | No repair, required upgrade, part, or service can permanently block driving. Immobilized vehicles and inaccessible services have a defined recovery (such as transport to a service point, a basic rental, or an explained alternative). | Must | Owner | [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) | PHASE-07 |

### 6.12 Home base, services, and company (HOME, COMPANY)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-HOME-001 | The player can choose a residential base and a garage location linked to city buildings. They are used through exterior access points and provide a safe spawn and return location. | Must | Owner | [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md) | PHASE-09 |
| REQ-HOME-002 | Service interactions (job board, rental, repair, contracts) happen at accessible exterior service points, optionally represented by mission characters. No navigable interiors are required. | Must | Owner | [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md) | PHASE-09 |
| REQ-HOME-003 | If a home base or service point becomes invalid (demolished or unreachable), the player is notified and a fallback is used. | Must | Owner | [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) | PHASE-09, PHASE-10 |
| REQ-COMPANY-001 | The player can found a basic transportation company with a name, contracts, fleet records, hiring, salaries, and costs. | Must | Owner | [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md) | PHASE-09 |
| REQ-COMPANY-002 | Employees and fleet operations are an explicitly labeled managerial simulation. The UI never implies that they physically circulate in the city. | Must | Owner | [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md) | PHASE-09 |
| REQ-COMPANY-003 | Company income and costs settle against persisted time checkpoints and the ledger, with no duplication across mode switches, pauses, or save/load. | Must | Owner | [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) | PHASE-09 |

### 6.13 Challenges and the dynamic city (CHAL, CITY)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-CHAL-001 | Checkpoint routes and time trials run on the player's city roads, with results stored per profile. | Must | Owner | [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) | PHASE-10 |
| REQ-CHAL-002 | Parking and precision-driving challenges are available. | Must | Owner | [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) | PHASE-10 |
| REQ-CHAL-003 | Job generation uses city data (building types, accessibility, transit lines, route distances), so the built city shapes the available work. | Must | Owner | [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-10 |
| REQ-CITY-001 | v1.0 reads city data (roads, building access, lines, stops) and does not write native economic or service state. | Must | Owner | [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-01, PHASE-10 |
| REQ-CITY-002 | After the player edits the city in `Building`, returning to character mode revalidates the avatar position, career vehicles, missions, home base, and service points. | Must | Owner | [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-10 |
| REQ-CITY-003 | Career passengers and cargo are separate from native citizens and goods. The mod never satisfies native demand that the native simulation also satisfies (no double satisfaction). Any write to the native economy is a separately gated optional experiment that is off by default. | Must | Owner | [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-10 |
| REQ-CITY-004 | Activating, using, and deactivating the mod causes no unintended persistent change to the city (buildings, roads, budgets, citizens). | Must | Owner | [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | PHASE-01 |

### 6.14 Persistence and save safety (SAVE)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-SAVE-001 | Career data is bound to a save identity (city identity, career profile ID, save revision ID). Loading an older save never shows money, vehicles, or progress from a newer session. | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md) | PHASE-01 |
| REQ-SAVE-002 | Persisted data carries a schema version. Older schemas migrate forward through a tested migration path. Data with an unknown newer schema is refused without data loss. | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md) | PHASE-01, PHASE-12 |
| REQ-SAVE-003 | Persisted references use stable identifiers, not raw runtime entity IDs, and are revalidated on load. | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) | PHASE-01 |
| REQ-SAVE-004 | Writes are atomic where the storage allows. Invalid or corrupt data is quarantined and never overwrites the last valid copy. | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md) | PHASE-01 |
| REQ-SAVE-005 | Saving and loading during an active mission either resumes it or resolves it safely, without duplicated rewards or lost entities. | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) | PHASE-06 |
| REQ-SAVE-006 | What happens to a save when the mod is disabled or removed is documented and tested, and the city remains loadable (the clean-exit procedure). | Must | Owner | [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) | PHASE-01, PHASE-13 |

### 6.15 Recovery and diagnostics (RECOV, DIAG)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-RECOV-001 | A `Recovery` state, invoked by the player or triggered automatically, moves the avatar or vehicle to the nearest validated safe point after it becomes stuck, goes out of bounds, or enters an invalid state. | Must | Owner | [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) | PHASE-04, PHASE-05 |
| REQ-RECOV-002 | Errors in mod systems are contained. A failing subsystem degrades to a safe state (for example, a return to `Building`) without crashing the game or corrupting the save, and the error is logged. | Must | Owner | [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) | PHASE-01 |
| REQ-DIAG-001 | The mod writes size-bounded logs that identify the mod version and build, and can export a sanitized diagnostics bundle containing no personal data. | Must | Owner | [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md), [DEBUGGING](../engineering/DEBUGGING.md) | PHASE-01 |
| REQ-DIAG-002 | The mod offers settings, including a safe-disable switch that returns control to `Building` and stops career systems without data loss. | Must | Owner | [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) | PHASE-01 |

### 6.16 Interface, onboarding, input, and localization (UI, ONBOARD, INPUT, L10N)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-UI-001 | The HUD shows speed, the current objective, route guidance, the wallet, and interaction prompts appropriate to the current state. | Must | Owner | [SPEC-UI](../experience/UI_AND_HUD.md) | PHASE-02 (minimal), PHASE-11 |
| REQ-UI-002 | Menus exist for career and jobs, garage, company, ledger, and settings. | Must | Owner | [SPEC-UI](../experience/UI_AND_HUD.md) | PHASE-07, PHASE-09, PHASE-11 |
| REQ-UI-003 | Blocked or failed actions (exit blocked, unaffordable, invalid mission, unavailable service) give clear feedback with a next step. | Must | Owner | [SPEC-UI](../experience/UI_AND_HUD.md) | PHASE-05, PHASE-11 |
| REQ-UI-004 | A map or route view shows the destination and the route on the city's roads. | Must | Owner | [SPEC-UI](../experience/UI_AND_HUD.md) | PHASE-06, PHASE-11 |
| REQ-ONBOARD-001 | A tutorial lets a new player start exploring, rent a vehicle, and complete a first job without the console or developer assistance. | Must | Owner | [SPEC-ONBOARD](../experience/ONBOARDING.md) | PHASE-11 |
| REQ-INPUT-001 | All mod actions can be remapped. | Must | Owner | [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md) | PHASE-11 |
| REQ-INPUT-002 | Character mode supports a gamepad for walking, driving, interactions, and menus. | Must | Owner | [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md) | PHASE-11 |
| REQ-INPUT-003 | Accessibility options include UI scaling, captions for important audio cues, indicators that do not rely on color alone, and hold/toggle alternatives. | Must | Owner | [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md) | PHASE-11 |
| REQ-L10N-001 | All player-facing text is externalized and available in English and Brazilian Portuguese (PT-BR). | Must | Owner | [SPEC-L10N](../experience/LOCALIZATION.md) | PHASE-01 (externalization), PHASE-11 |

### 6.17 Art, audio, and performance (ART, AUDIO, PERF)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-ART-001 | The avatar has animations for idle, walk, run, and vehicle entry/exit (or a documented substitute transition), using only own or authorized assets. | Must | Owner | [SPEC-ART](../experience/ART_AND_ANIMATION.md) | PHASE-04, PHASE-11 |
| REQ-ART-002 | Placeholder visuals are labeled as placeholders in builds and documentation until they are replaced. | Must | Owner | [SPEC-ART](../experience/ART_AND_ANIMATION.md) | PHASE-04 – PHASE-11 |
| REQ-AUDIO-001 | Footstep, engine, tire, and impact sounds come from own or authorized sources. | Must | Owner | [SPEC-AUDIO](../experience/AUDIO.md) | PHASE-11 |
| REQ-PERF-001 | Frame time, memory, and simulation speed are measured against a recorded baseline using a repeatable protocol. A recurring frame-time regression above 10% triggers an investigation (initial proposal, not a budget). | Must | Owner | [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) | PHASE-03, PHASE-12 |
| REQ-PERF-002 | Street-level visual enhancements are optional and measured, and are off by default if they exceed the agreed budget. | Should | Owner | [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) | PHASE-11 |

### 6.18 Compatibility and release (COMPAT, REL)

| ID | Requirement | Priority | Basis | Specification | Phase |
|---|---|---|---|---|---|
| REQ-COMPAT-001 | Supported game version, DLC, and mod combinations are documented. Known conflicting driving or camera mods are detected or documented. This mod and the original Beta Test Driving Mod never load conflicting controllers at the same time. | Must | Owner | [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) | PHASE-01, PHASE-12 |
| REQ-REL-001 | A player can install and run the mod on a supported configuration without any development tools. | Must | Owner | [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) | PHASE-13 |
| REQ-REL-002 | Install, update, and clean-exit procedures are tested before any claim of safe uninstallation is made. | Must | Owner | [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) | PHASE-13 |
| REQ-REL-003 | Third-party notices and license/provenance audits are complete for every incorporated component and asset before distribution. | Must | Owner | [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) | PHASE-00 (candidate audit), PHASE-01 (each incorporation from then on), PHASE-13 |
| REQ-REL-004 | The core distribution requires only Cities: Skylines II and documented dependencies. It does not require Motor Town or Melty at runtime. | Must | Owner | [ADR-0001](../architecture/decisions/ADR-0001-host-game-and-runtime.md), [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md) | PHASE-13 |

**Totals:** 93 requirements: 90 `Must` and 3 `Should` (REQ-DRIVE-004, REQ-TAXI-003, REQ-PERF-002).

## 7. Explicit v1.0 exclusions

These are **not** mandatory for v1.0 **[Owner requirement]**. They are tracked in [EXPANSIONS](EXPANSIONS.md) and
must not enter v1.0 silently:

- Interiors in every building (v1.0 uses exterior access and service points).
- Native citizens as taxi or bus passengers (a separate, optional experiment).
- Physical employee fleets circulating in the city (v1.0 companies are a managerial simulation).
- Articulated-truck physics (v1.0 freight uses rigid trucks; see CONF-003).
- Writes to the municipal economy (at most a separately gated, off-by-default experiment).
- Complex police pursuits or combat.
- Part-by-part mechanical assembly.
- Structural vehicle deformation.
- Multiplayer.
- A detailed cockpit for every vehicle.

## 8. Quality expectations that apply to every requirement

- **Evidence over assertion.** A requirement counts as met only after a recorded evidence run of its tests
  ([TEST_STRATEGY](../quality/TEST_STRATEGY.md)). Compiling is not passing.
- **Save safety first.** No feature may ship with a known path to save corruption, duplicated rewards, or permanent
  loss of control ([PHASE-12](../planning/phases/PHASE-12-beta-and-validation.md) gate).
- **Honest presentation.** Placeholder art, abstract simulations, and unvalidated experiments are labeled as such in
  the game and in the documentation.
- **Recoverability.** Every failure state the player can reach has a documented way out
  ([SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md)).

## 9. v1.0 success criteria

v1.0 is releasable when all of the following hold (verified in [PHASE-12](../planning/phases/PHASE-12-beta-and-validation.md)
and [PHASE-13](../planning/phases/PHASE-13-release-and-maintenance.md)):

1. Every `Must` requirement has a Passed validation with evidence, or an owner-approved scope decision recorded in the
   register.
2. An independent tester installs the mod on a supported configuration and completes the core experience (section 4)
   without developer tools or assistance.
3. There are no known critical defects involving save corruption, duplicated rewards, permanent loss of control, or
   mandatory content that cannot be completed.
4. Third-party notices, the license audit, and the compatibility declaration are complete.

## 10. Open questions affecting scope

The register ([RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md)) holds the full list. The items that
could force a scope review are:

- OPEN-006 (persistence mechanism), together with OPEN-101 (host lifecycle events) and OPEN-103 (stable native
  references), on which REQ-SAVE-001 and REQ-SAVE-003 depend
- OPEN-007 and OPEN-008 (surface and collision data)
- OPEN-009 (physics approach)
- OPEN-010 (avatar representation)
- OPEN-011 (native vehicle handback)
- OPEN-020 (transit line data)
- OPEN-025 (gamepad support)
- OPEN-027 (project license)

## 11. Change control

Only the project owner approves adding, removing, or re-prioritizing requirements. A proposed change is recorded as a
`CONF-*` or `OPEN-*` entry with its rationale. Once approved, this file is updated, retired IDs are marked Withdrawn
(never deleted or reused), and [TRACEABILITY](../planning/TRACEABILITY.md) is updated in the same change.
