# Motor Town Design Reference

| Field | Value |
|---|---|
| ID | DOC-MOTORTOWN |
| Document status | Draft |
| Release scope | v1.0 design input (expansions noted separately) |
| Phases | PHASE-06 – PHASE-10 (career design); PHASE-00 (OPEN-030 decision) |
| Responsible role | Coordinator (maintainer); the gameplay specification authors consult it; the project owner decides OPEN-030 |
| Related requirements | REQ-TAXI-001, REQ-BUS-001, REQ-DELIV-001, REQ-FREIGHT-001, REQ-GARAGE-001 … REQ-GARAGE-006, REQ-COMPANY-001 … REQ-COMPANY-003, REQ-CHAL-001, REQ-ECON-001 … REQ-ECON-005, REQ-REL-004 |
| Related documents | [SOURCES](SOURCES.md) (SRC-003), [GAME_DESIGN](../product/GAME_DESIGN.md), [EXPANSIONS](../product/EXPANSIONS.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) |
| Last updated | 2026-10-18 |

## 1. Purpose and boundaries

Motor Town: Behind The Wheel is a **behavioral and design reference** for transportation careers, vehicle ownership,
progression, and businesses **[Owner requirement]**. This document maps loops that can be observed from public
information to **original** Cities: Skylines II adaptations.

Boundaries:

- No Motor Town code, assets, physics, data, audio, user interface, or API is assumed to be available or transferable, and none will be used.
- Motor Town is never required at runtime (REQ-REL-004), and the project does not need it to be running or installed ([ADR-0001](../architecture/decisions/ADR-0001-host-game-and-runtime.md)).
- No Motor Town branding, names, logos, or screenshots appear in the mod, its listing, or project evidence (RISK-015). The name appears in internal documentation only, to identify the reference.
- Nothing in this document claims parity with Motor Town or describes its internals.

## 2. What the developer store page documents

Source: Steam store page (SRC-003), accessed 2026-10-18 through a summarizing fetch tool. It is a live page with no
revision marker. All rows are **[Documented upstream]** and are not verified by playing the game.

| Topic | Page content (summarized) | Note |
|---|---|---|
| Developer / publisher | P3 Games / P3 Games | — |
| Release status | Released Dec 17, 2021, labeled **Early Access**. The Early Access text says "Core driving physics and game mode are completed" and estimates "1 ~ 2 Years to finish". | The estimate is old; the current state is unknown. |
| Platform | Windows 10 64-bit in the system requirements; a free demo is listed | — |
| Passenger work | "Taxi, Bus, Trucks and much more!"; passenger jobs include hitchhikers, taxi, and bus | — |
| Cargo work | Pickup trucks, trucks, semi-trailers | — |
| Future services | "Tow Truck, Ambulance and more will be added"; police jobs listed as planned | **Future tense; may be stale.** Current availability is not verified. |
| Police | Police chase players who break traffic rules | — |
| Vehicles | "50+ Cars, Trucks and Trailers"; buy or rent cars; replace parts | — |
| Companies | "Create a company and make your own bus/truck route"; "Hire an AI driver or other player to work for you" | — |
| Racing | Race events, time attack, autocross, "Online leaderboards" | — |
| Modes and world | Single-player plus online PvP and co-op tags; "Open world (24 x 16 Km) with Day & Night cycle" | — |
| Progression | No explicit level or progression system is described | Only a quoted review mentions "progression". |

## 3. Loop mapping: observable loop → original CS2 adaptation

| # | Observable Motor Town loop (from §2) | Our original CS2 adaptation | Our requirements / specs | What we deliberately do differently or do not import |
|---|---|---|---|---|
| 1 | Taxi and passenger jobs | Taxi career: rent a vehicle, accept a fare with an estimate, collect a **career passenger** at a validated access point, transport, drop off, settle once | REQ-TAXI-001 … REQ-TAXI-003 · [SPEC-TAXI](../gameplay/careers/TAXI.md) | Passengers are mod-owned, never native citizens. There is no hitchhiker mechanic. Fares follow our own proposal. |
| 2 | Bus jobs | Follow an **existing city transit line's** stops in order, with career passengers, capacity, and a trip evaluation | REQ-BUS-001, REQ-BUS-002 · [SPEC-BUS](../gameplay/careers/BUS.md) | The player does not create native routes. Native riders are not carried in v1.0. Line edits cause re-planning or a no-fault cancellation. |
| 3 | Cargo with pickups and trucks | Urban deliveries (multi-stop, deadlines, fragility) and **rigid-truck** freight at industrial or commercial access points | REQ-DELIV-001, REQ-FREIGHT-001 · [SPEC-DELIVERY](../gameplay/careers/DELIVERIES.md), [SPEC-FREIGHT](../gameplay/careers/FREIGHT.md) | No semi-trailers or articulated-truck physics in v1.0 (CONF-003). Cargo never satisfies native demand (REQ-CITY-003). |
| 4 | Buy or rent vehicles; replace parts | Rental at service points (always available as recovery), purchase, sale, garage, fuel, simplified wear and maintenance, upgrades with measurable effects | REQ-GARAGE-001 … REQ-GARAGE-006 · [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | No part-by-part mechanical assembly. No repair or part can permanently block driving. |
| 5 | Earning money and investing | Personal wallet, append-only ledger, XP per career, reputation, unlocks; insolvency recovery | REQ-ECON-001 … REQ-ECON-005 · [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) | Our own economy with illustrative **[Proposal]** values. It is separate from the municipal budget. |
| 6 | Create a company; own routes; hire drivers | A basic company with contracts, hiring, salaries, and costs, computed as an explicitly labeled **managerial simulation** | REQ-COMPANY-001 … REQ-COMPANY-003 · [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md) | Employees never physically drive in the city. No other players are hired. No native routes are created. |
| 7 | Race events, time attack, autocross | Checkpoint routes, time trials, and parking and precision challenges on the player's own roads | REQ-CHAL-001, REQ-CHAL-002 · [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) | No online leaderboards; results are stored per profile. |
| 8 | Open world with day and night | The player's own CS2 city is the world; CS2 remains its only authority | REQ-CITY-001 · [SPEC-CITY](../architecture/CITY_INTEGRATION.md) | We build no map and no second simulation. |
| 9 | Driving physics | Consistent, semi-realistic handling per vehicle class; the approach is chosen by experiment in PHASE-03 | REQ-DRIVE-002, REQ-DRIVE-003 · [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md) | Motor Town physics are not imported, measured, or claimed to be equivalent. |
| 10 | Police chases; police jobs (planned) | None in v1.0 | — | Complex police pursuits are excluded from v1.0 ([PRD](../product/PRD.md) §7). The base mod's police chase is not reused. |
| 11 | Tow truck, ambulance (announced) | Possible post-1.0 urban services | [EXPANSIONS](../product/EXPANSIONS.md) | Not v1.0. |
| 12 | Online PvP and co-op | None | — | Multiplayer is excluded from v1.0. |

## 4. Using the reference responsibly

- **Design input only.** A specification may cite this document as inspiration for a loop's *shape* (accept → travel → service → settle → invest). Rules, numbers, names, and presentation are written fresh for this project and labeled **[Proposal]**.
- **No feature promises.** A row in §3 is a design intention backed by a `REQ-*` ID, not a commitment to match Motor Town behavior.
- **Observation notes.** If the owner plays Motor Town on their own initiative, any notes they share are recorded as owner observations in the relevant specification. They are never treated as technical facts about CS2.
- **No captured media in the project.** Screenshots or recordings of Motor Town are not stored in the project (OPEN-407, [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md)).
- **Naming review.** Before release, player-facing names and the store description are reviewed so that they do not imply affiliation with Motor Town or P3 Games (RISK-015).

## 5. Owner decision: is a Motor Town installation needed? (OPEN-030)

**Question:** Should Motor Town be installed or run at any point in this project?

**Facts:**

- Our core distribution never requires it (REQ-REL-004).
- The public store page (§2) is sufficient to justify the v1.0 career scope, which is already fixed by the owner brief.
- Melty's homepage does not list Motor Town. Its connected catalog has Motor Town only as an uncatalogued custom entry, while it lists CS2 (2026-10-18). Selecting Motor Town in Melty is therefore not evidence of any technical integration ([MELTY_EVALUATION](MELTY_EVALUATION.md) §9).
- **Observed on 2026-10-18 (TASK-0001):** Motor Town is already installed on the owner's machine through Steam (app 1369670). It was not launched and nothing was read from it beyond the store's app manifest.
- Installing or launching games is not authorized without the owner (CONF-009).

**[Proposal]:** treat a Motor Town installation as **not needed** for any phase gate. Playing it remains a personal
choice of the owner and may inform design notes. **Status:** partly answered on 2026-10-18. It is installed, but no
task needs it; whether design notes from play sessions are wanted remains the owner's choice.

## 5a. How Motor Town stays a reference, never a runtime dependency

- **Code and packages:**
  - no project document, build step, test, or package references Motor Town files, assemblies, saves, or processes;
  - [ADR-0001](../architecture/decisions/ADR-0001-host-game-and-runtime.md) and
    [ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md) make CS2 the only runtime;
  - the release checklist runs the mod with Motor Town absent (TEST-REL-004).
- **Listings:** wherever Drive the City II is listed, CS2 is the only required game. Motor Town is never declared as a
  required or "companion" game. Melty's validator showed that declaring it as a companion would make it required and
  launched alongside ([MELTY_EVALUATION](MELTY_EVALUATION.md) §9, M-10).
- **Credit:** Motor Town may be named in a description only as a design inspiration, with no logos, assets, or
  implied endorsement (RISK-015).
- **Design work:** loops are adapted through the mapping in §3 into our own specifications; nothing is imported.

## 6. Open questions and risks

- **Central:** OPEN-030 (the installation need), RISK-012 (scope creep from attractive reference features), RISK-015 (branding and IP misuse), CONF-009.
- **New:** OPEN-407 (whether captures of third-party games or CS2 may be kept as evidence or used in public material).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
