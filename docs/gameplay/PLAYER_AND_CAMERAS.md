# Player Character and Cameras

| Field | Value |
|---|---|
| ID | SPEC-PLAYER |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-04 (primary), PHASE-05, PHASE-11 |
| Responsible role | Coordinator (maintainer); Implementer of the Player module; Reviewer review for any change that touches state or camera ownership |
| Related requirements | REQ-PLAYER-001, REQ-PLAYER-002, REQ-PLAYER-003, REQ-PLAYER-004, REQ-RECOV-001, REQ-WORLD-001, REQ-WORLD-002, REQ-CTRL-003, REQ-TIME-002, REQ-TIME-003, REQ-ART-001, REQ-ART-002 |
| Related documents | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-TRANSITION](WALK_DRIVE_TRANSITIONS.md), [SPEC-HOME](HOME_AND_SERVICE_POINTS.md), [SPEC-ART](../experience/ART_AND_ANIMATION.md), [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md), [PHASE-04](../planning/phases/PHASE-04-on-foot-character.md) |
| Last updated | 2026-10-18 |

> **Status:** behavior specification only. Nothing described here is implemented, built, or tested. Every numeric
> value is an illustrative **[Proposal]** to be tuned in PHASE-04. Validation status of every criterion: **Not run**.

## 1. Purpose

This specification defines what the player experiences in the `OnFoot` state. It covers who the avatar is, how it
moves, which surfaces it may use, how the first- and third-person cameras behave, and when an invalid position hands
control to `Recovery`. Ownership of movement and camera, and the state-machine guards, are defined in
[SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md). Surface and collision queries are defined in
[SPEC-CONTACT](../architecture/WORLD_CONTACT.md). This document only describes the behavior those contracts must
produce.

## 2. Scope and non-goals

**In scope (v1.0):** one visible, mod-owned avatar; walking, running, turning, free look; crossing curbs, gentle slopes,
and low steps; first- and third-person cameras with anti-clipping; detection of invalid positions; player-invoked
recovery; feedback for all of the above.

**Out of scope for v1.0 [Owner requirement]:** parkour, climbing, jumping, swimming, and generic building interiors.
Also out of scope **[Proposal]**: crouching, combat, ragdolls, fall damage, talking to native citizens, and avatar
appearance customization. Customization is a candidate expansion ([EXPANSIONS](../product/EXPANSIONS.md)).

## 3. Behavior

### 3.1 Avatar identity

- The avatar is an `Avatar` entity ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)). It is created and owned by the
  mod. It is **never** a native citizen with a household, job, school, or schedule, and it is never added to any
  native statistic: population, happiness, traffic counts, or the census **[Owner requirement]** (REQ-PLAYER-001).
- There is exactly one avatar per active career profile. It exists only while the player state is `OnFoot`,
  `EnteringVehicle`, `ExitingVehicle`, or `Recovery`. While `Driving`, it is hidden and attached to the vehicle. On
  return to `Building`, its last valid position is recorded and the entity is removed, as SPEC-ENTITY specifies.
- Its visual representation (own or authorized model, or a runtime representation) is still open (OPEN-010). Until a
  final model exists, the avatar is labeled as a placeholder (REQ-ART-002). Animation requirements are in
  [SPEC-ART](../experience/ART_AND_ANIMATION.md).
- Native pedestrians and vehicles are not scripted to react to the avatar. Contact with them follows the single contact
  rule selected under OPEN-032. Whatever rule is chosen, v1.0 has no avatar injury and never changes native agents'
  state **[Proposal]**.

### 3.2 Entering character mode: spawn selection

When the player switches from `Building` to character mode, the spawn point is the first valid candidate from this
ordered list **[Proposal]**:

1. The avatar position stored in this save, if it passes revalidation (still walkable, same level, not inside a newly
   placed building). See REQ-CITY-002.
2. The home base pedestrian access point ([SPEC-HOME](HOME_AND_SERVICE_POINTS.md)), once homes exist (PHASE-09).
3. The validated pedestrian access point nearest to the Building-mode camera focus, within 300 m.
4. The validated pedestrian access point nearest to the camera focus anywhere in the city.
5. If there is none, entry is refused with an explanation ("No walkable location found near the view. Move the camera
   over a street and try again."). The player stays in `Building`.

The camera blends from the building view to the avatar camera. The prior simulation speed is recorded for restoration
([SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md)).

### 3.3 Locomotion

| Behavior | Rule | Illustrative value **[Proposal]** |
|---|---|---|
| Walk | Default movement. Analog input scales speed. | 1.4 m/s |
| Run | Hold or toggle (accessibility: REQ-INPUT-003). No stamina system in v1.0. | 3.8 m/s |
| Acceleration / deceleration | Eased so that small taps give small steps; no sliding after release. | 0–walk in 0.25 s; stop in 0.15 s |
| Turning | Avatar turns toward the camera-relative movement direction. | Up to 540°/s blend |
| Free look | Holding (or toggling) free look orbits the camera without turning the avatar. Releasing it recenters the camera behind the avatar. | Recenter in 0.4 s |
| Collision shape | A vertical capsule used for all contact queries. | Radius 0.3 m, height 1.8 m |

Movement is camera-relative in third person and view-relative in first person. Gamepad and keyboard bindings are
defined in [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md). Character-mode input never moves the building
camera or activates city tools (REQ-CTRL-003).

### 3.4 Surfaces and elevation

| Situation | Behavior | Illustrative value **[Proposal]** |
|---|---|---|
| Curbs and low steps | Climbed automatically, with no input. | Step-up up to 0.35 m |
| Gentle slopes, ramps | Walkable. | Up to 35° |
| Steeper slopes | Treated as walls; the avatar slides along them. | > 35° |
| Drop-offs (bridge edges, retaining walls, elevated walkways) | **Ledge guard:** the avatar stops at the edge, as if at a wall. It never walks off a drop it cannot step down. | Drops > 0.6 m block movement |
| Stacked levels (bridge over road, tunnel under park) | The avatar stays on its current level. Level changes happen only through connected walkable geometry such as ramps and stairs (REQ-WORLD-001). | — |
| Road surfaces | Walkable. Crossing is allowed; contact with traffic follows OPEN-032. | — |
| Water | The shoreline acts as a boundary. No swimming. | Water deeper than 0.3 m blocks movement |
| Map boundary | Acts as a boundary. | Playable-area edge |

The ledge guard is an original design choice. With no falling, there is no fall damage to design, and accidental falls
from bridges cannot become a common cause of recovery. Which surface data makes these rules possible is open
(OPEN-007).

### 3.5 Obstacles

Building facades, walls, fences, and static props are solid (REQ-PLAYER-004, REQ-WORLD-002). The avatar slides along
them and never pushes or moves them. Whether collision data exists for every obstacle type is open (OPEN-008). Where
it does not, the bounded local proxies described in [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) apply.

### 3.6 Cameras

| Camera | Behavior | Illustrative value **[Proposal]** |
|---|---|---|
| Third person (default) | Over-the-shoulder orbit. The mouse or right stick orbits; free look decouples the camera from facing. | Distance 3.2 m, 0.5 m shoulder offset, pitch −70° to +75° |
| First person | Eye-level view. The avatar body is hidden (its shadow may remain). Head yaw follows the view. | Eye height 1.65 m, pitch ±80° |
| Switching | One action toggles first and third person, with a blend. The choice is remembered per profile. | Blend 0.25 s |

**Anti-clipping (third person)** (REQ-PLAYER-003):

1. Each frame, a sphere is swept from the avatar's head pivot to the desired camera position.
2. If the sweep hits a building, terrain, or static obstacle, the camera is placed just short of the hit point. It
   pulls in immediately and eases back out over about 0.3 s, so it does not pump.
3. Below 0.8 m from the pivot, the avatar fades to transparent so the view never shows the inside of the avatar mesh.
4. Below 0.4 m, the camera switches to a temporary first-person view until space clears. The player's saved preference
   is not changed.
5. The camera is never placed inside a building volume or below the local ground plus 0.3 m.

The camera owner while `OnFoot` is the Player module's avatar camera. Ownership transfer to the vehicle camera or back
to the building camera is governed by [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md).

### 3.7 Invalid position detection

The Player module keeps a ring buffer of **last valid positions**: 10 samples at 0.5 s intervals, taken only while
the avatar stands on valid ground **[Proposal]**. Detection rules **[Proposal]**:

| Condition | Detection | Response |
|---|---|---|
| Below valid ground | Feet more than 0.5 m below the surface of the current level for more than 0.2 s | Automatic `Recovery` |
| No support | No valid surface under the capsule for more than 1.0 s | Automatic `Recovery` |
| Inside geometry | Penetration into a building or static obstacle deeper than 0.15 m, lasting more than 0.5 s | Automatic `Recovery` |
| Unexpected level change | Current level differs from the previous sample without connected walkable geometry | Snap back to the last valid sample if it is under 2 s old; otherwise `Recovery` |
| Out of bounds | Outside the playable area, or in water deeper than the boundary | Automatic `Recovery` |
| Stuck | Movement held for 4 s with less than 0.25 m displacement, and no identifiable blocking contact | Prompt: "Stuck? Use Recovery." (not automatic) |
| Player request | Recovery action from the menu or a bound key, with confirmation | `Recovery` (cooldown 15 s; deferred during transitions) |

`Recovery` itself, including how the nearest validated safe point is chosen and the cooldowns, is defined in
[SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) (REQ-RECOV-001).

### 3.8 Pause, focus, and frame-rate behavior

- `Paused` and `Menu` freeze the avatar. Movement does not continue under an open menu (REQ-TIME-002).
- Losing window focus clears all held inputs. The avatar does not keep running when the player alt-tabs away.
- Per-frame displacement is bounded (REQ-TIME-003). After a frame stall, the avatar never jumps more than one capped
  step, so it cannot tunnel through a facade. The cap is defined in [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md).

### 3.9 Player-facing feedback

| Event | Feedback (presentation in [SPEC-UI](../experience/UI_AND_HUD.md)) |
|---|---|
| Nearby interactable (vehicle, service point) | Context prompt with the action name and the bound key or button |
| Movement blocked by ledge guard | Brief edge indicator; no text spam |
| Camera pulled in by a wall | Smooth pull-in with avatar fade; no message |
| Automatic recovery | Short notice: "Moved to a safe location" plus the reason category |
| Entry to character mode refused | Explanation and a next step (REQ-UI-003) |

## 4. Failure handling and recovery

- **Player subsystem error** (exception, missing data): contained per REQ-RECOV-002. The safe state is a return to
  `Building` with the speed restored and the error logged. The avatar is never left without an owner.
- **Avatar entity lost** (removed by an external cause): the Player module detects the missing entity on the next
  update. It recreates the avatar at the last valid position, or enters `Recovery` if that position has become invalid.
  No duplicate avatars may exist (SPEC-ENTITY registry check).
- **Representation unavailable** (model or animation fails to load): the avatar falls back to the labeled placeholder
  representation. Movement and contact continue to work (REQ-ART-002).
- **No safe point found by `Recovery`**: follow SPEC-RECOVERY. The final fallback is a return to `Building` with a
  clear message. The player never stays inside geometry.

## 5. Data that must persist

Persisted through the Persistence module. The format, identity binding, and migration belong to
[SPEC-SAVE](../architecture/SAVE_FORMAT.md):

- the avatar's last valid position, as world position plus a stable surface or level reference (never a raw runtime
  entity ID; REQ-SAVE-003), and its facing;
- the player's camera preference (first or third person) and accessibility toggles that are profile-scoped;
- whether the save was made in character mode, which decides spawn candidate 1 in §3.2.

The ring buffer of recent valid positions is runtime-only and is not saved.

## 6. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | The avatar is mod-owned. No native citizen, household, or statistic changes when character mode starts or ends. | TEST-PLAYER-001 | Not run |
| 2 | Walk, run (hold and toggle), turn, and free look work with keyboard/mouse; curbs, ramps up to the slope limit, and low steps are crossed without input. | TEST-PLAYER-002 | Not run |
| 3 | First/third-person switching works. Across the PHASE-04 circuit, the third-person camera never renders from inside a building or terrain. | TEST-PLAYER-003 | Not run |
| 4 | The avatar never passes through facades or falls below ground. Every injected invalid position triggers `Recovery`. | TEST-PLAYER-004, TEST-RECOV-001 | Not run |
| 5 | On bridges, under overpasses, and in tunnel approaches, the avatar never switches level. | TEST-WORLD-001 | Not run |
| 6 | Pause, menus, and focus loss stop movement and clear held inputs. A forced frame stall produces no jump through geometry. | TEST-TIME-002, TEST-TIME-003 | Not run |
| 7 | Character-mode input never moves the building camera or activates a city tool. | TEST-CTRL-003 | Not run |
| 8 | PHASE-04 gate: a representative urban circuit (sidewalks, ramps, a bridge, obstacles) completed with no clipping, no falling, and no permanent loss of control. | PHASE-04 gate, TEST-PLAYER-002/004 | Not run |

Test definitions live in [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md).

## 7. Open questions and risks

Central entries ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-007 (surface data), OPEN-008 (obstacle
collision data), OPEN-010 (avatar representation and animation), OPEN-024 (street-level rendering), OPEN-032 (contact
with traffic and pedestrians), RISK-003, RISK-008, RISK-009. This specification adds no new entries. The locomotion and
camera values above are tuning proposals that PHASE-04 playtests will settle, not open questions.

## 8. Provenance

The capability list (walk, run, turn, free look, small elevation changes, first/third person, camera anti-clipping,
recovery, and the exclusion of parkour, swimming, and interiors) comes from the owner brief **[Owner requirement]**. It
matches the historical plan in [chat.md](../../chat.md) **[Historical input]**. The spawn order, ledge guard,
temporary first-person fallback, and detection thresholds are original proposals of this document.
