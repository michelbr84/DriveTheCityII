# UI and HUD

| Field | Value |
|---|---|
| ID | SPEC-UI |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 (technology spike, OPEN-015), PHASE-02 (minimal HUD), PHASE-05, PHASE-06, PHASE-07, PHASE-09, PHASE-11 (complete) |
| Responsible role | Coordinator (maintainer); Implementer (UI module); QA (presentation checks) |
| Related requirements | REQ-UI-001, REQ-UI-002, REQ-UI-003, REQ-UI-004, REQ-COMPANY-002, REQ-ART-002, REQ-CITY-003, REQ-TAXI-002, REQ-DRIVE-004, REQ-DIAG-002, REQ-L10N-001, REQ-INPUT-003 |
| Related documents | [SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md), [SPEC-ONBOARD](ONBOARDING.md), [SPEC-L10N](LOCALIZATION.md), [SPEC-AUDIO](AUDIO.md), [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [PLAYER_JOURNEYS](../product/PLAYER_JOURNEYS.md) |
| Last updated | 2026-10-18 |

> **Status:** presentation design only. No UI exists. The UI technology is not chosen (OPEN-015). The sketches below
> are wireframes of **our** mod's screens, not descriptions of the CS2 interface.

## 1. Purpose

This spec defines what the player sees and how the mod communicates: the HUD for each player state, menus, the
map and route view, interaction prompts, feedback for blocked actions, honesty labels, and notifications. Behavior
rules (what is allowed, costs, timers) live in the gameplay and architecture specifications. This spec defines how
those rules are shown.

## 2. Scope and non-goals

**In scope:** mod-owned HUD, menus, prompts, notifications, and the route view in character mode, plus the minimal
mod presence in Building mode (entry point, notifications, settings).

**Non-goals:**

- Changing or overlaying CS2's own city-building panels, beyond inserting our entry point wherever the chosen
  technology allows it.
- Claiming any CS2 UI element, button, or menu exists before it has been observed.
- A full-screen custom map renderer, if a lighter route view is enough (section 3.4).

**Technology note:** the official CS2 wiki documents a UI-modding stack and named insertion points for mod UI
**[Documented upstream]**. The candidate base mod builds its UI in more than one way **[Source-inspected]**. Both
are recorded in [SOURCES](../research/SOURCES.md) and [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md). Neither has
been verified in our environment. The choice is made in the PHASE-01 spike for OPEN-015, and this spec stays
technology-neutral until then.

## 3. Behavior and presentation

### 3.1 Principles

1. **State-appropriate:** show only what helps in the current player state (REQ-UI-001).
2. **Actionable:** every refusal says why and what to do next (REQ-UI-003).
3. **Honest:** the labels in section 3.7 are mandatory and are never hidden by default.
4. **Color-independent:** every status uses shape or icon plus text, not color alone (REQ-INPUT-003).
5. **Localizable:** no hard-coded text; layouts tolerate longer PT-BR strings ([SPEC-L10N](LOCALIZATION.md)).
6. **Device-aware:** prompts show the binding for the last-used device ([SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md)).

### 3.2 HUD per player state

States and overlays are defined in [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md).

| State / overlay | Shown **[Proposal]** | Not shown |
|---|---|---|
| `Building` | Entry point into character mode (OPEN-302); career notifications (for example, salaries due) as unobtrusive notices | Avatar or vehicle HUD |
| `OnFoot` | Objective line; direction indicator to the target; compact wallet; interaction prompt when in range; run toggle state if toggle mode is on | Speed, fuel |
| `EnteringVehicle` / `ExitingVehicle` | A short transition indicator; a cancel hint if cancelling is allowed (SPEC-TRANSITION) | Interaction prompts |
| `Driving` | Speed (unit per settings); reverse and parking-brake indicators; fuel and condition (from PHASE-07); objective, route guidance, and deadline; passenger or cargo status; compact wallet; camera mode label when it changes | On-foot prompts |
| `Recovery` | Status line with the reason ("Moving you to a safe spot…") and progress | Everything else except critical notices |
| `Paused` | Mod HUD hidden or dimmed; mission timers shown as frozen when visible (REQ-TIME-002) | Live timers |
| `Menu` | The open mod menu, captured input | World HUD |
| `Loading` / `Unloading` | Nothing from the mod. Results of load-time checks (migration, quarantine) appear after loading completes ([PLAYER_JOURNEYS](../product/PLAYER_JOURNEYS.md) JRN-004) | — |

**Driving HUD wireframe [Proposal]:**

```text
+--------------------------------------------------------------------------+
| [!] Deadline 02:40                          Wallet 1,240   [Taxi L3]     |
| Objective: Drop off passenger at Harbor St (curbside)                    |
|                                                                          |
|                         (world view)                                     |
|                                                                          |
|  [P] Parking brake    [R] Reverse                 Fuel [######----] 60%  |
|  48 km/h              Next: turn right in 120 m   Condition  Good        |
+--------------------------------------------------------------------------+
```

Bracketed items are icons with text. In the real UI, "[P]" and "[R]" are indicator icons, not key names.

### 3.3 Menus

All menus open over character mode, can be navigated with keyboard, mouse, or gamepad (REQ-INPUT-002), and close
when the player returns to Building mode.

| Menu | Contents **[Proposal]** | Requirement | Available from |
|---|---|---|---|
| **Career and jobs** | Job board grouped by career; offer cards; career levels, experience, reputation; challenges entry; tutorial replay | REQ-UI-002, REQ-TAXI-003 | PHASE-06 (job board), PHASE-07 |
| **Garage** | Owned and rented vehicles; fuel, condition, upgrades with measured effects; buy, sell, repair; rent or return | REQ-UI-002, REQ-GARAGE-001 – REQ-GARAGE-006 | PHASE-07 |
| **Company** | Header label "Managerial simulation"; contracts; employees; fleet records; checkpoint reports; balance and transfers | REQ-UI-002, REQ-COMPANY-001, REQ-COMPANY-002 | PHASE-09 |
| **Ledger** | Append-only transaction history: in-game time, type, description, amount, running balance, short TransactionId reference; filters by type and career | REQ-ECON-002 | PHASE-06 (minimal), PHASE-07 |
| **Settings** | Input and remapping, accessibility, audio, units, language override, gameplay assists, diagnostics export, safe-disable switch | REQ-DIAG-002, REQ-INPUT-001, REQ-INPUT-003 | PHASE-01 (safe-disable), PHASE-11 |
| **Help and tips** | Previously shown hints and tutorial explanations | REQ-ONBOARD-001 | PHASE-11 |

Whether settings live in a mod menu, in the game's options area, or in both depends on OPEN-015. The official modding
documentation describes an attribute-driven settings UI for mods **[Documented upstream]**; this is unverified locally.

**Job offer card [Proposal]:**

```text
+-----------------------------------------------+
| TAXI  -  Career passenger                     |
| From: Oak Ave (curbside)  ->  To: Market Sq   |
| Route 3.4 km  |  Est. 6 min  |  Est. pay 112  |
| No deadline                                   |
| [Accept]                    [Details]         |
+-----------------------------------------------+
```

**Settlement summary [Proposal]:** title (completed, cancelled, failed, or no-fault), base pay, distance component,
quality component (with the reasons: smoothness, punctuality, cargo care), penalties and fees, total, experience and
reputation change, and a ledger reference. The total always equals the ledger entry (REQ-CAREER-004).

### 3.4 Map and route view

- The route is drawn along the city's roads, never as a straight line (REQ-UI-004). It shows the player position,
  the destination access point (curbside, stop, or bay) distinct from the building, and a legend.
- **In-world guidance [Proposal]:** a direction indicator and next-maneuver text in the HUD, plus a marker at the
  access point. A full minimap is optional, and is added only if the PHASE-11 performance measurements allow it.
- **On foot:** the same view, routed on walkable paths to service points.
- Rerouting (after a wrong turn or a city edit) updates the route and shows a short notice.
- How routes are computed and validated is OPEN-019. The view only displays the route that the Career module and
  CityBridge provide.

### 3.5 Interaction prompts

Format: `[binding] Verb object`, for example `[E] Rent basic car (cost 90)` or `[Hold R] Recover`. Hold actions show
a progress ring. A prompt appears only when the action is possible right now. If the target is in range but the
action is unavailable, the prompt is shown dimmed with its reason (for example, `Enter vehicle — vehicle must be
stopped`).

### 3.6 Feedback for blocked or failed actions (REQ-UI-003)

| Situation | Message (English source; illustrative) | Next step offered | Rule owner |
|---|---|---|---|
| Exit blocked | "No safe place to get out here." | "Move to a spot with space beside the vehicle" or hold Recover | [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md) |
| Vehicle moving | "Stop the vehicle to get out." | Brake to a stop | SPEC-TRANSITION |
| Unaffordable | "Not enough money: 300 needed, 40 available." | Starter job, sell a vehicle, cheaper option | [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md) |
| Invalid mission | "This job is no longer possible: the destination was removed. No penalty." | New offers | [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) |
| Service unavailable | "This repair point can't be reached. Using the nearest alternative." | Route to the fallback | [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md) |
| Career unavailable in this city | "Bus shifts need a bus line with at least two reachable stops." | What to build | [SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md), OPEN-301 |
| Return to Building not possible yet | "Finishing the current action first…" | Waits automatically, or explains the refusal | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md) |
| Career data refused (newer schema) | "This save's career data needs a newer mod version. Your city is not affected." | Update the mod; load another save | [SPEC-SAVE](../architecture/SAVE_FORMAT.md) |

Strings are externalized under the `dtc2.feedback.*` keys ([SPEC-L10N](LOCALIZATION.md)). The English wording above
is illustrative and is reviewed with the PT-BR translation.

### 3.7 Honesty labels (mandatory)

| Label | Where it appears | Requirement |
|---|---|---|
| "Managerial simulation" | Company menu header and every fleet or employee figure | REQ-COMPANY-002 |
| "Career passenger" / "Career cargo" | Offer cards, HUD, summaries; never "citizen" or "resident" | REQ-TAXI-002, REQ-CITY-003 |
| "Placeholder" | About page list; a persistent tag in development and beta builds that contain placeholders | REQ-ART-002 |
| "Experimental — off by default" | Any gated experiment, such as native-economy writes | REQ-CITY-003 |
| "Sandbox — no career progress" | Persistent HUD tag whenever sandbox mode is active | REQ-ENTITY-002 |
| "Driver view (no cockpit model)" | Camera label for vehicles without a suitable interior | REQ-DRIVE-004 |

### 3.8 Notifications

| Priority | Examples | Presentation **[Proposal]** |
|---|---|---|
| Critical | Career data quarantined; safe-disable triggered by an error | Modal dialog with an explanation and a diagnostics option; at most one at a time |
| Warning | Low fuel; deadline soon; salaries due next checkpoint; destination changed | Toast with an icon and a caption, about 6 s; repeated only when the state worsens |
| Info | New offers; level-up; unlock; settlement saved | Batched toast; also kept in the notification history |

Rules: notifications never require a mouse; they never cover the speed and objective lines; they are kept in a
history in the Help and tips menu; important audio cues have a matching visual notice ([SPEC-AUDIO](AUDIO.md)).

## 4. Failure handling and recovery

- **UI subsystem failure:** gameplay must not depend on rich UI to stay safe. If the HUD fails, the mod keeps a
  minimal text notice path, and the safe-disable switch remains reachable (REQ-RECOV-002, REQ-DIAG-002). The error
  is logged.
- **Missing string:** fall back to English and log the missing key. Raw keys are shown only in pseudo-localization
  builds.
- **State change while a menu is open:** a forced return to Building, `Recovery`, or unload closes mod menus and
  releases captured input. No modal may survive into a state where it blocks input.
- **Stale data:** menus read from module outputs on open and on change notifications. Stale balances are never shown
  after settlement.

## 5. Acceptance criteria

| Test | Criterion | Validation |
|---|---|---|
| TEST-UI-001 | The HUD shows speed, objective, route guidance, wallet, and state-appropriate prompts in each state of section 3.2 | Not run |
| TEST-UI-002 | Career and jobs, garage, company, ledger, and settings menus open, are navigable by keyboard, mouse, and gamepad, and close on a state change | Not run |
| TEST-UI-003 | Each situation in section 3.6 shows its reason and next step | Not run |
| TEST-UI-004 | The route view draws the route on roads to the access point and updates after a reroute | Not run |
| TEST-COMPANY-002 | The "Managerial simulation" label is present on every company screen and figure | Not run |
| TEST-ART-002 | Placeholder labels are present wherever placeholders ship | Not run |
| TEST-L10N-001 | All UI text comes from externalized keys in English and PT-BR | Not run |
| TEST-INPUT-003 | Indicators are distinguishable without color; UI scaling applies to all mod screens | Not run |

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-015 | Which UI technology a CS2 code mod can use for the HUD and menus. |
| OPEN-019 | Route computation and validation, which the route view depends on. |
| OPEN-025 | Gamepad input, including navigation of mod menus. |
| OPEN-302 (new) | How a first-time player discovers the entry point into character mode. |
| OPEN-303 (new) | Whether mod UI can inherit the game's interface scale, units, and display settings. |
| OPEN-306 (new) | Wallet currency presentation, so it is never confused with the city budget. |
| RISK-303 (new) | PT-BR text expansion and translation lag breaking layouts. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
