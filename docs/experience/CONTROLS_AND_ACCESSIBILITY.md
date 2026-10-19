# Controls and Accessibility

| Field | Value |
|---|---|
| ID | SPEC-INPUT |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-02 (input separation, gamepad probe), PHASE-04, PHASE-05, PHASE-11 (remapping, gamepad, accessibility) |
| Responsible role | Coordinator (maintainer); Implementer (Player and UI modules); QA (input and accessibility checks) |
| Related requirements | REQ-INPUT-001, REQ-INPUT-002, REQ-INPUT-003, REQ-CTRL-003, REQ-CTRL-004, REQ-TIME-002, REQ-DRIVE-001, REQ-RECOV-001 |
| Related documents | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md), [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-UI](UI_AND_HUD.md), [SPEC-AUDIO](AUDIO.md), [SPEC-ONBOARD](ONBOARDING.md) |
| Last updated | 2026-10-18 |

> **Status:** proposal. No input code exists. **All default bindings below are [Proposal] candidates.** We make no
> claim about which keys or buttons Cities: Skylines II already uses. Conflicts with the game's bindings are verified
> under OPEN-014 (keyboard and mouse) and OPEN-025 (gamepad) before any default is accepted.

## 1. Purpose

This spec defines the mod's player actions, proposed default bindings, remapping, hold and toggle behavior,
sensitivity and inversion options, accessibility options, and how character-mode input stays separate from Building
mode. Which system receives input in each state is defined in
[SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md). This spec defines what the player presses and how it can be
adapted.

## 2. Scope and non-goals

**In scope:** all mod actions in character mode, the single entry action in Building mode, and mod menu navigation.

**Non-goals:**

- Rebinding or changing any of the game's own actions.
- Steering wheels and pedals (EXP-018 in [EXPANSIONS](../product/EXPANSIONS.md)).
- Screen-reader support. Its feasibility depends on the host UI technology (OPEN-015) and has not been investigated.
  It is not promised for v1.0.

## 3. Behavior and contracts

### 3.1 Action list and proposed defaults

Gamepad buttons are named by position (South, East, West, and North face buttons; left and right shoulder (LS/RS);
left and right trigger (LT/RT); stick press (L3/R3); D-pad), so that no brand layout is implied.

| Action | Purpose | Active in | Type | Default mode | Keyboard and mouse **[Proposal]** | Gamepad **[Proposal]** |
|---|---|---|---|---|---|---|
| Enter character mode | Leave Building mode for the avatar | `Building` | Button | Press | Hold `J` | Not bound by default (OPEN-025) |
| Move | Walk | `OnFoot` | 2D axis | — | `W` `A` `S` `D` | Left stick |
| Run | Run while moving | `OnFoot` | Button | Hold (toggle available) | `Left Shift` | L3 |
| Look | Free look and orbit the camera | `OnFoot`, `Driving` | 2D axis | — | Mouse | Right stick |
| Interact | Use a service point, accept, or confirm | `OnFoot` | Button | Press (hold for paid actions) | `E` | South |
| Enter or exit vehicle | Start a transition | `OnFoot` (near a vehicle), `Driving` (stopped) | Button | Press | `F` | North |
| Throttle | Accelerate | `Driving` | Axis or button | — | `W` | RT |
| Brake / reverse | Brake; reverse once stopped | `Driving` | Axis or button | — | `S` | LT |
| Steer | Steer progressively | `Driving` | Axis | — | `A` / `D` | Left stick, horizontal |
| Parking brake | Hold the vehicle in place | `Driving` | Button | Toggle (hold available) | `Space` | West |
| Switch camera | First and third person on foot; chase and driver view in a vehicle | `OnFoot`, `Driving` | Button | Press | `C` | R3 |
| Camera distance | Zoom the third-person camera | `OnFoot`, `Driving` | Axis or buttons | — | Mouse wheel | LS (closer) / RS (farther) |
| Recover | Request `Recovery` | `OnFoot`, `Driving` | Button | Hold about 1.5 s | Hold `R` | Hold D-pad down |
| Open mod menu | Career, garage, company, ledger, settings | `OnFoot`, `Driving` (stopped) | Button | Press | `Tab` | D-pad right |
| Map and route view | Toggle the route view | `OnFoot`, `Driving` | Button | Press | `M` | D-pad up |
| Return to Building | Leave character mode | Stable character-mode states (REQ-CTRL-004) | Button | Hold about 1 s | Hold `Backspace` | Hold D-pad left |
| Menu navigate, confirm, back | Navigate mod menus | `Menu` | Buttons | Press | Arrow keys or mouse, `Enter`, the menu key again | D-pad or left stick, South, East |

Design rules for the defaults **[Proposal]**:

- **Leave system keys alone.** `Escape` and the gamepad's system buttons (often labeled Menu/Start and View/Back) are
  not bound by default, so that the game's own pause and system menus stay reachable.
- **Hold for leaving and recovery.** Actions that change the mode or move the player (Return to Building, Recover,
  Enter character mode) use a hold, so they are never triggered by accident.
- **No gamepad default in Building mode.** Gamepad input in Building mode has not been investigated (OPEN-025), so
  Enter character mode has no gamepad default. The player can still bind one.
- **Shared keys only across exclusive states.** `W` is Move on foot and Throttle in a vehicle. This is safe only
  because the two states never coexist. Whether the game's binding system accepts the same key in two mod actions is
  part of OPEN-014.
- **Base mod coexistence.** The candidate base mod documents its own keys **[Documented upstream]**
  ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). Our controller and the original mod are never active together
  (REQ-COMPAT-001), so overlapping keys between the two are not a v1.0 concern.

### 3.2 Remapping (REQ-INPUT-001)

- Every action in section 3.1 can be remapped separately for keyboard and mouse and for gamepad. This includes the
  entry action, so that no unremappable key exists.
- The preferred path is the game's own key-binding system for mods. The official modding documentation describes
  one that supports keyboard, mouse, and gamepad bindings, and states that mod bindings which conflict with built-in
  bindings are disabled **[Documented upstream]** ([SOURCES](../research/SOURCES.md)). This is unverified in our
  environment. If it holds, every default must pass a conflict audit for each supported game version. That audit is
  part of OPEN-014.
- **Conflict checks within the mod:** two actions active in the same state cannot share a binding. The settings
  screen shows the conflicting action and offers to swap.
- **Reset:** per action and for all actions.
- **Persistence:** bindings are a per-user preference, not career data. They are not bound to a save **[Proposal]**.

### 3.3 Hold and toggle

| Action | Options |
|---|---|
| Run | Hold (default) or toggle |
| Parking brake | Toggle (default) or hold |
| Recover, Return to Building, Enter character mode | Hold duration adjustable from 0.5 to 2.0 s **[Proposal]**, or press then confirm |
| Paid interactions (rent, buy, repair) | Hold, or press then confirm |
| Throttle and brake on keyboard | Hold (digital input); optional throttle assist that ramps the input smoothly |

No action requires rapid repeated presses or simultaneous chords by default.

### 3.4 Sensitivity, dead zones, and inversion

| Setting **[Proposal]** | Range and default |
|---|---|
| Look sensitivity (mouse) | 0.1–3.0, default 1.0 |
| Look sensitivity (stick) | 0.1–3.0, default 1.0 |
| Invert look Y / invert look X | Separate for mouse and gamepad; default off |
| Steering sensitivity | 0.5–2.0, default 1.0 |
| Speed-sensitive steering assist | On by default for keyboard, off for gamepad |
| Stick and trigger dead zones | 0–30%, default 10% |
| Camera smoothing | Off, low (default), or high |

Steering and handling response curves belong to [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md).

### 3.5 Input separation from Building mode (REQ-CTRL-003)

- **Building context:** the mod listens only for **Enter character mode** and for its own menus and notices. It never
  moves the building camera and never uses building-tool inputs.
- **Character context:** mod actions are active. Building tools must not activate from mod keys. How the game's
  building inputs are suppressed or left inert in character mode is OPEN-014, and the mechanism is owned by
  SPEC-CONTROL.
- **Held inputs are cleared** on window focus loss (REQ-TIME-002), on every player-state change, and when a menu
  opens. For example, holding `W` while exiting a vehicle must not make the avatar walk until `W` is pressed again
  **[Proposal]**.
- **Pause:** while `Paused`, mod movement input is ignored and mission timers are frozen (REQ-TIME-002). Menus remain
  usable if the UI technology allows it.

### 3.6 Devices

- **Hot switching:** the last device used drives the prompt glyphs.
- **Gamepad (REQ-INPUT-002):** walking, driving, interactions, and menus must all be possible with a gamepad alone.
  An early probe in PHASE-02 checks feasibility (OPEN-025). The official UI documentation notes that a UI input
  package is not yet available to UI mods **[Documented upstream]**, so gamepad navigation of mod menus may need a
  separate input route. This is recorded under OPEN-025.
- **Disconnection while driving [Proposal]:** throttle is released, the brake brings the vehicle to a stop, the
  parking brake engages, and a notice asks the player to reconnect or switch device.

### 3.7 Accessibility (REQ-INPUT-003)

| Need | Option **[Proposal]** |
|---|---|
| Readability | UI scale from 75% to 200%, applied to all mod screens; follows the game's interface scale if the mod can read it (OPEN-303). Minimum text size and contrast rules apply to HUD text over the world. |
| Hearing | Captions for important audio cues, with size and background options ([SPEC-AUDIO](AUDIO.md)). Every important cue also has a visual notice. |
| Color vision | Status uses icon or shape plus text. Route lines use a pattern as well as a color. Markers differ by shape ([SPEC-ART](ART_AND_ANIMATION.md)). |
| Motor | Full remapping; hold and toggle alternatives; adjustable hold durations; no chords or mashing; steering and braking assists. |
| Motion comfort | Camera shake slider (0–100%, default 50%); head bob off by default; field-of-view slider for first person; camera transitions can use a fade instead of a camera swoop (RISK-306). These options affect only the mod's own cameras. |
| Time pressure | A relaxed-deadlines assist that extends time windows (economic effect defined in [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md)); pausing is always available. |
| Cognitive load | Hints can be re-read in Help and tips; objective text always states the next concrete step. |

## 4. Failure handling and recovery

- **Unbound action:** prompts show "Not bound — set in Settings" instead of an empty glyph. The settings screen lists
  all unbound actions.
- **Binding disabled by a conflict:** if the game's binding system disables a mod binding (see section 3.2), the mod
  should detect it and tell the player which action to rebind. Whether such detection is possible is part of
  OPEN-014. Until that is known, a conflict audit before each release is mandatory.
- **Lost input context:** if input routing fails (no input reaches the active controller), the player can still use
  Return to Building or the safe-disable switch. Otherwise `Recovery` falls back to `Building` (REQ-RECOV-002).
- **Stuck held input:** cleared by the focus and state-change rules in section 3.5.

## 5. Acceptance criteria

| Test | Criterion | Validation |
|---|---|---|
| TEST-INPUT-001 | Every action in section 3.1 can be remapped for keyboard and mouse and for gamepad, persists across sessions, and resets correctly | Not run |
| TEST-INPUT-002 | Walking, driving, interactions, and all mod menus are usable with a gamepad only | Not run |
| TEST-INPUT-003 | UI scaling, captions, color-independent indicators, hold and toggle alternatives, and motion-comfort options work as specified | Not run |
| TEST-CTRL-003 | Character-mode input never moves the building camera or activates building tools; Building-mode input never moves the avatar or vehicle | Not run |
| TEST-CTRL-004 | Return to Building works from every stable state through the hold action | Not run |
| TEST-TIME-002 | Focus loss clears all held mod inputs; pause stops mod-driven movement | Not run |

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-014 | Input capture without triggering building tools; integration with the game's binding system; conflict audit of defaults. |
| OPEN-025 | Gamepad support in the mod's input context, including menus. |
| OPEN-303 (new) | Whether mod UI can inherit the game's interface scale and display settings. |
| RISK-020 | Input conflicts with game shortcuts and tools. |
| RISK-306 (new) | Street-level cameras may cause motion discomfort. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
