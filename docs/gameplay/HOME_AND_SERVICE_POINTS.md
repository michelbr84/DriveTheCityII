# Home Base and Service Points

| Field | Value |
|---|---|
| ID | SPEC-HOME |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-06 (minimal rental desk and job board), PHASE-07 (fuel, repair, sales points), PHASE-09 (primary: home, garage, player-chosen locations, mission characters), PHASE-10 (revalidation after city edits) |
| Responsible role | Coordinator (maintainer); Implementer of the Career and CityBridge-facing modules |
| Related requirements | REQ-HOME-001, REQ-HOME-002, REQ-HOME-003, REQ-GARAGE-004, REQ-CAREER-003, REQ-CITY-001, REQ-CITY-002, REQ-RECOV-001, REQ-UI-003 |
| Related documents | [SPEC-CITY](../architecture/CITY_INTEGRATION.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-PLAYER](PLAYER_AND_CAMERAS.md), [SPEC-MISSION](MISSION_FRAMEWORK.md), [SPEC-COMPANY](COMPANIES_AND_FLEET.md), [SPEC-UI](../experience/UI_AND_HUD.md), [EXPANSIONS](../product/EXPANSIONS.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. **No interiors.** Every interaction happens at an
> exterior access point. Values are **[Proposal]**. Validation: **Not run**.

## 1. Purpose

This document gives the player a place in the city (a residential base and a garage) and defines where services
happen: job board, rental, repair, fuel, vehicle sales, and contracts. All of these are tied to real buildings of the
player's own city through validated exterior access points. When the city changes, they degrade gracefully instead of
breaking.

## 2. Scope and non-goals

**In scope:** choosing and changing a home base and a garage; the service point types; automatic designation and
player relocation of service points; optional mission characters; fallbacks for invalid locations.

**Non-goals for v1.0 [Owner requirement]:** navigable interiors in any building and a social simulation of residents.
Also out of scope **[Proposal]**: owning or modifying buildings, needs such as sleep or hunger, furniture, and business
hours. Interiors for the home and garage are an expansion candidate ([EXPANSIONS](../product/EXPANSIONS.md)).

## 3. Behavior

### 3.1 Linking to city buildings

Every home, garage, and service point is a record of:
- a **stable building reference**, never a raw entity ID (REQ-SAVE-003);
- one or more **access points**: a pedestrian access point (a door or curb on the sidewalk) and, where needed, a
  **vehicle access point** (curb, lay-by, lot entrance, or loading bay). They are found and validated through CityBridge
  (OPEN-018; [SPEC-CITY](../architecture/CITY_INTEGRATION.md)). They are never the building center (REQ-CAREER-003).

The mod only **reads** building data. Choosing a home does not change residents, zoning, land value, or any other
native state (REQ-CITY-001, REQ-CITY-004).

### 3.2 Residential base (REQ-HOME-001)

- **Choosing:** in character mode, the player stands at the pedestrian access point of an eligible building and uses
  **"Make this my home"**. Alternatively, in the career menu's map, the player picks a building with a validated access
  point. Eligible buildings: residential and mixed-use buildings **[Proposal]**.
- **What a home provides:**
  - spawn candidate 2 when entering character mode ([SPEC-PLAYER](PLAYER_AND_CAMERAS.md));
  - the fallback target for some recovery choices;
  - a **"Return home"** action while on foot with no `Accepted` or `Active` mission: a short fade, no cost, and a 60 s
    cooldown **[Proposal]**;
  - a **home menu** at the door: career summary, ledger, garage overview, and settings shortcuts. This is a menu, not an
    interior.
- **Changing:** allowed at any time, free, once per 10 career-clock minutes **[Proposal]**.
- **Before a home is chosen** (new profile, or before PHASE-09 content exists), spawn candidates 3–4 of SPEC-PLAYER apply.

### 3.3 Garage location (REQ-HOME-001)

- The player chooses a building or lot with a validated **vehicle access point** (parking lot, commercial or industrial
  parcel, or the home's curb frontage) and uses **"Use as my garage"**.
- The garage provides vehicle storage (`Stored` vehicles have no world entity; [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md))
  with a base **capacity of 2** stored vehicles, expandable to 4 and then 6 through purchases **[Proposal]** (prices in
[SPEC-ECON](PROGRESSION_AND_ECONOMY.md) §8). It is also
  the delivery point for purchases and the place where company vehicles are listed (PHASE-09).
- Retrieving a stored vehicle spawns it at the garage's vehicle access point after a free-space check, with alternates
  and feedback as in SPEC-GARAGE §4.
- Before a garage is chosen, the default storage of SPEC-GARAGE §5 is used.

### 3.4 Service points (REQ-HOME-002)

| Type | Offers | Typical eligible buildings **[Proposal]** | Interaction |
|---|---|---|---|
| Job board | Offers for all unlocked careers ([SPEC-MISSION](MISSION_FRAMEWORK.md)) | Office, commercial | On foot, 2.5 m radius |
| Rental desk | Basic and premium rentals, returns | Commercial | On foot; returns by vehicle at the vehicle access point |
| Fuel | Refuel | Commercial along major roads | Drive-in, 8 m radius, stopped |
| Repair | Maintenance, upgrades | Industrial, commercial | Drive-in, 8 m radius, stopped |
| Vehicle sales | Purchase, sale, default storage | Commercial | On foot |
| Bus depot | Bus shifts, service buses ([SPEC-BUS](careers/BUS.md)) | Any building near a line terminus with a vehicle access point; the city's own depot is used only as a read-only location if one exists | On foot |
| Contracts office | Company contracts and hiring ([SPEC-COMPANY](COMPANIES_AND_FLEET.md)) | Office | On foot |

Careers may also offer **in-vehicle dispatch** (for example, taxi fares while on duty). Accepting work never requires an
interior.

**Designation:**
1. **Automatic:** when a career profile is created, and after each revalidation, the mod designates at least one
   service point of each unlocked type within reach. Preference goes to buildings of the listed categories, spread
   across districts, near the home if one is set. Selection is deterministic (seeded by profile and city), so it is
   stable across reloads.
2. **Player-chosen:** the player can move a service point of any type to another eligible building with a validated
   access point ("player-chosen exterior locations"). This is free, and limited to 3 player-chosen points per type
   **[Proposal]**.

A building may host several service types. Service points never claim to be the building's real function. For example,
a fuel point at a generic commercial building is shown as a "mobile fuel stand" marker.

### 3.5 Mission characters (optional)

A service point may show a **mission character**: a mod-owned figure standing at the access point, such as a rental
clerk or a dispatcher. It is purely presentational. It uses the same representation path as career passengers (OPEN-202)
and is **never a native citizen**. When no suitable representation exists, the service point uses a marker and the menu
alone, and the experience is complete without the character.

### 3.6 Feedback

Markers on the map and in the world show type and status (available, relocated, unavailable). Interaction prompts name
the service. Unavailable services explain why and offer the next step, such as "Repair point unreachable. Use mobile
service or go to <nearest alternative>" (REQ-UI-003).

## 4. Failure handling: invalid homes and service points (REQ-HOME-003)

**Validation runs** on load, on return from `Building` (REQ-CITY-002), and on every interaction attempt.

| Invalid case | Detection | Fallback |
|---|---|---|
| Home building demolished or replaced | Building reference no longer resolves (identity stability across rebuilds: OPEN-206) | Notify: "Your home at <address> no longer exists." The home becomes a **temporary base** at the nearest valid pedestrian access point of an eligible building, and the player is prompted to choose a new home. Spawn uses the temporary base. |
| Home access point unreachable (blocked path, road removed) | Access point fails validation; alternates of the same building are tried first | Use an alternate access point of the same building if one exists; otherwise apply the temporary-base rule |
| Garage invalid | As above, for the vehicle access point | `Stored` vehicles are **never lost**: they move to the default storage (SPEC-GARAGE §5) and the player is notified. `Parked` vehicles at the garage are revalidated individually (SPEC-GARAGE). |
| Service point invalid | As above | Automatic re-designation of the same type nearby, with a notice and the new marker. Player-chosen points revert to automatic until the player picks again. Mobile service covers fuel and repair if no designation is possible (SPEC-GARAGE §9). |
| No eligible building of a type anywhere | No candidate passes validation | The type is offered as a **virtual service** from the menu at the nearest valid curb access point, explained to the player. Job board and rental are always available this way, so careers are never blocked by missing buildings. |
| Interaction in progress when invalidated | Revalidation during an interaction | The menu closes safely. Operations not yet confirmed are discarded; confirmed operations complete exactly once (operation IDs, [SPEC-ECON](PROGRESSION_AND_ECONOMY.md)). |

The player never spawns or returns into an invalid location. If every candidate fails, Recovery's final fallback
applies ([SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md)).

## 5. Data that must persist

Per profile, format in [SPEC-SAVE](../architecture/SAVE_FORMAT.md): the home, garage, and player-chosen service points as
stable building references plus access-point descriptors; the garage capacity level; the temporary-base flag; the
designation seed. Automatic designations may be recomputed. Persisting them avoids markers jumping between sessions.

## 6. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | The player can choose a home and a garage linked to city buildings. Spawn and return use their exterior access points. | TEST-HOME-001 | Not run |
| 2 | Job board, rental, repair, and contracts work at exterior service points. No interior is needed. Mission characters, when present, are mod-owned. | TEST-HOME-002 | Not run |
| 3 | Demolishing or disconnecting the home, the garage, and each service type produces a notice and the specified fallback. Stored vehicles are preserved. | TEST-HOME-003 | Not run |
| 4 | After a city edit, returning to character mode revalidates the home, garage, and service points before use. | TEST-CITY-002 | Not run |
| 5 | Choosing a home or service point changes no native building, citizen, or budget data. | TEST-CITY-004 | Not run |

## 7. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-018 (finding access points), OPEN-021 (detecting
city edits), RISK-018. New: **OPEN-202** (representation of mod-owned figures such as mission characters and career
passengers), **OPEN-206** (stable identity of zoned buildings across level-up, abandonment, and rebuilds), **RISK-204**
(cities with too few eligible buildings).

## 8. Provenance

A home base and garage linked to city buildings, exterior service interactions, optional mission characters, and
fallbacks are **[Owner requirement]**. They are also in [chat.md](../archive/chat.md) **[Historical input]**. Automatic
designation, virtual services, the temporary base, and garage capacity are original proposals.
