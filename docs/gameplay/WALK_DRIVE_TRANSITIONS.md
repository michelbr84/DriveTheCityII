# Walk–Drive Transitions

| Field | Value |
|---|---|
| ID | SPEC-TRANSITION |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-05 (primary), PHASE-06 (career vehicles and parking), PHASE-11 (animation and presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Player and Vehicles modules; Reviewer sign-off required because transitions transfer movement and camera authority (critical area) |
| Related requirements | REQ-TRANS-001, REQ-TRANS-002, REQ-TRANS-003, REQ-TRANS-004, REQ-CTRL-001, REQ-CTRL-002, REQ-ENTITY-001, REQ-ENTITY-002, REQ-ENTITY-003, REQ-RECOV-001, REQ-UI-003, REQ-ART-001 |
| Related documents | [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-PLAYER](PLAYER_AND_CAMERAS.md), [SPEC-DRIVING](VEHICLE_DRIVING.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-UI](../experience/UI_AND_HUD.md), [SPEC-ART](../experience/ART_AND_ANIMATION.md), [PHASE-05](../planning/phases/PHASE-05-walk-drive-integration.md) |
| Last updated | 2026-10-18 |

> **Status:** behavior specification only. Nothing is implemented or tested. The `EnteringVehicle` and
> `ExitingVehicle` state guards and the authority hand-over are defined normatively in
> [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md). This document defines what the player sees and which placement
> rules those states must enforce. Values are **[Proposal]**. Validation: **Not run**.

## 1. Purpose

This specification makes getting into and out of vehicles safe and predictable. The player must never end up inside a
wall, inside traffic, on the wrong level, with a detached camera, or with a duplicated vehicle. Ending a drive must
always leave the world clean.

## 2. Scope and non-goals

**In scope:** stationary entry and exit; vehicle selection by proximity; availability checks and prompts; the exit
placement search; blocked-exit feedback; recovery exits; vehicles removed during a transition; and the three
end-of-drive outcomes.

**Non-goals for v1.0 [Proposal]:** entering or exiting a moving vehicle, passenger seats for the avatar, carjacking,
door physics, and walking around inside a bus. Animated entry and exit are presentation (REQ-ART-001). A documented
substitute transition, such as a camera blend with a fade, is acceptable until animations exist.

## 3. Behavior

### 3.1 Entry: selection and availability

1. **Candidates.** Vehicles whose nearest door anchor (or bounding box when there are no anchors) is within 3.0 m of
   the avatar, and that lie within a 100° cone of the avatar's facing **[Proposal]**.
2. **Priority.** The vehicle of the player's active mission, then the active rental, then owned vehicles, then service
   vehicles assigned to the player. In Sandbox mode only, native vehicles come next. Ties go to the nearest.
3. **Availability checks**, all required:

| Check | Rule | Prompt when it fails (REQ-UI-003) |
|---|---|---|
| Stationary | Speed < 0.3 m/s [Proposal] | "Vehicle is moving" |
| Valid entity | Registered and valid in the entity registry ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)) | (not shown; the candidate is skipped) |
| Not controlled elsewhere | No other movement authority holds it (REQ-CTRL-001) | "Vehicle is busy" |
| Mode eligibility | Career mode: a `CareerVehicle` usable by this profile (owned, an active rental, or an assigned service vehicle). Sandbox: also eligible native vehicles. | "Not your vehicle. Rent or buy one at a service point." |
| Rental validity | The rental is active (not returned or expired) | "Rental ended. Renew at a rental desk." |
| Clear driver side or alternate door | At least one door anchor is reachable without crossing a facade | "Cannot reach a door" |

An immobilized vehicle (out of fuel, condition 0%) can still be entered. Driving inputs are then disabled and the
recovery options of [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md) are offered from the driver's seat.

4. **Prompt.** An available vehicle shows "Enter <vehicle name>" with the bound action. An unavailable one shows the
   reason from the table instead of the action.

### 3.2 Entry sequence

1. The player confirms. All checks run again at commit time; if any fails, the reason is shown and the avatar stays
   `OnFoot`.
2. State `EnteringVehicle` begins. Avatar input is frozen and the vehicle is locked against other claims.
3. The camera blends to the vehicle's chase camera (about 0.6 s **[Proposal]**). The avatar plays the entry animation
   or the substitute transition, then is hidden and attached to the vehicle.
4. Movement authority transfers to the player vehicle controller, following SPEC-CONTROL's hand-over rules. The
   parking brake stays engaged until the first throttle input.
5. State becomes `Driving`. If steps 2–4 do not finish within 3 s **[Proposal]**, the transition aborts (§4).

### 3.3 Exit request

Exit is allowed when the vehicle speed is below 0.5 m/s **[Proposal]**. The parking brake engages automatically. At
higher speed, the prompt reads "Stop the vehicle to exit."

### 3.4 Exit placement search

Candidate points are tried in this order **[Proposal]**:

1. **Curb-side door**, meaning the side facing the nearest sidewalk or curb. For right-hand traffic this is usually the
   passenger side. Offset about 1.0 m from the body.
2. **Driver door** (opposite side).
3. **Rear**, then **front**, about 1.0 m beyond the bumper.
4. **Ring search** around the vehicle at radii from 1.5 m to 6 m in 0.5 m steps, on the same level.
5. **Nearest validated pedestrian access point** within 25 m on the same level.

Each candidate must pass **every** check:

| Check | Rule **[Proposal]** |
|---|---|
| Free space | The avatar capsule (r 0.3 m, h 1.8 m) overlaps no building, static obstacle, vehicle, or other mod entity |
| Headroom | At least 2.0 m clear above the ground point |
| Correct level | The ground point lies within ±0.6 m of the vehicle's floor reference on the **same** level. Never the road under a bridge or the deck over a tunnel (REQ-WORLD-001). |
| Walkable surface | It is a walkable surface by SPEC-CONTACT, not water and not outside the map |
| Not in live traffic | Not inside a traffic lane with moving vehicles within 30 m of approach. Sidewalks and parking lanes are preferred; a road lane is accepted only when it is blocked by the player's own stopped vehicle. |
| Line of sight | The straight line from the door anchor to the candidate crosses no facade, barrier, or guard rail. This prevents exits through railings onto another level. |

The first passing candidate wins. State `ExitingVehicle` begins, movement authority returns to the avatar (SPEC-CONTROL),
the avatar is placed, and the camera blends to the on-foot camera.

### 3.5 Blocked exit

If no candidate passes, the player stays `Driving` and sees **[Proposal]**:

> "Exit blocked. Move to a curb or open area." Options: **Keep driving** (default) · **Recovery exit**.

**Recovery exit** asks [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) for the nearest validated safe pedestrian
point, on any level reachable from the vehicle's position. The vehicle is then dealt with by the rules below, so it
never becomes an invisible or orphaned blocker:

- A **career vehicle** standing in a through-traffic lane is relocated to the nearest valid parking access point, or
  stored at the garage (SPEC-GARAGE). The relocation is explained in a notice. It is free, because the system could not
  offer a normal exit.
- A **borrowed native vehicle** (Sandbox) is handed back (REQ-ENTITY-001).

The avatar is never placed inside a wall, in traffic, or at the wrong elevation to force an exit **[Owner requirement]**.

### 3.6 End-of-drive outcomes (REQ-TRANS-004)

| Outcome | When | What happens |
|---|---|---|
| **Park a career vehicle** | Career mode, owned or rented vehicle, exit at a valid parking position (parking lane, lot, curb parking, garage or service access point) | The vehicle stays as a parked `CareerVehicle` with the parking brake on. Its pose is recorded as a stable location reference. It is registered as stationary, so traffic treats it as parked. If the position is not a valid parking spot (for example, a through lane), the exit prompt offers **"Exit and auto-park"**: after exit the vehicle is relocated to the nearest valid parking access point **[Proposal]**. |
| **Hand back a borrowed native vehicle** | Sandbox mode only | Handback per [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md): route rebuild, lane-reservation cleanup, no stale route, no invisible blocker (REQ-ENTITY-001). The vehicle is never recorded as player property. |
| **Remove temporary objects** | Any end of drive | Mod-created temporary entities that no longer have an owner are removed: mission markers, a returned rental's vehicle (at a rental desk), and sandbox test vehicles. Every mod entity has a registry owner (REQ-ENTITY-003). |

Returning a rental at a rental desk ends the rental (SPEC-GARAGE) and removes the vehicle entity after the avatar has
exited and moved more than 5 m away **[Proposal]**.

### 3.7 Interaction with overlays and saving

- `Paused` or `Menu` during a transition freezes the transition timer. The transition resumes afterwards and
  re-validates before committing.
- Losing focus during a transition does not cancel it. Held inputs are cleared.
- Transitional states are never persisted. A save taken during `EnteringVehicle` or `ExitingVehicle` records the last
  stable state (INV-CTRL-08 in [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md);
  [SPEC-SAVE](../architecture/SAVE_FORMAT.md)). Deferring a player-requested save until the transition ends would be
  preferable **[Proposal]**, but it depends on what the host's save events allow (OPEN-101). Saves therefore only ever
  capture stable states.
- Returning to `Building` is not offered during a transition. It becomes available as soon as a stable state is
  reached (REQ-CTRL-004).

## 4. Failure handling and recovery

| Failure | Required result (REQ-TRANS-003) |
|---|---|
| Vehicle removed or invalidated during `EnteringVehicle` | Abort. The avatar stays at its pre-entry position, which is re-validated; if it is invalid, `Recovery` runs. The camera returns to the avatar. Notice: "Vehicle no longer available." No authority was transferred, or it is transferred back. |
| Vehicle removed or invalidated during `ExitingVehicle` | Complete the exit at the already-validated candidate if one exists; otherwise run `Recovery`. The avatar is never left attached to a missing vehicle. |
| Vehicle removed while `Driving` | Emergency exit: run the §3.4 search from the vehicle's last known pose. If nothing passes, use the Recovery exit. Active missions resolve per [SPEC-MISSION](MISSION_FRAMEWORK.md). A loss the player did not cause gives a no-fault cancellation. |
| Transition timeout (3 s) | Abort and roll back to the previous stable state, with a diagnostics entry. |
| Duplicate-entity check fails (a second avatar or a cloned vehicle) | Critical error: keep one registered instance, remove the duplicate, log it. Covered by the SPEC-ENTITY registry invariants. |
| Camera has no owner after a transition | Watchdog: return camera ownership to the current state's owner within one frame, and log it. |

## 5. Data that must persist

Only stable states are saved (§3.7). For parked career vehicles: the stable location reference, pose, and parking-brake
state (vehicle record in SPEC-GARAGE). Borrowed native vehicles are never persisted as mod data. A sandbox borrow is
handed back before a save completes, or the save records nothing about it **[Proposal]**, to be confirmed in
[SPEC-SAVE](../architecture/SAVE_FORMAT.md). Transition progress itself is never persisted.

## 6. Acceptance criteria and the 50-cycle validation

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | Entry works by proximity, priority, availability checks, and prompts. A moving vehicle cannot be entered. | TEST-TRANS-001 | Not run |
| 2 | Exit placement passes every check in every scenario below. Blocked exits show feedback or a Recovery exit. Zero placements inside walls, traffic, or the wrong level. | TEST-TRANS-002 | Not run |
| 3 | Vehicle removal injected during entry, during exit, and while driving always aborts or recovers safely, with no lost control, no duplicates, and no detached camera. | TEST-TRANS-003 | Not run |
| 4 | Each of the three end-of-drive outcomes behaves as in §3.6 and leaves no orphan entity or lane blocker. | TEST-TRANS-004, TEST-ENTITY-001, TEST-ENTITY-003 | Not run |
| 5 | **PHASE-05 gate (proposed):** 50 successful enter → drive → park → exit cycles across the scenario matrix, with no duplication, camera loss, or traffic blocking. | PHASE-05 gate | Not run |

**Proposed scenario matrix** for the 50 cycles (each row at least 3 cycles; QA defines the exact procedures in
[TEST_SCENARIOS](../quality/TEST_SCENARIOS.md)):

1. Flat street with curb parking (both traffic sides)
2. Wall on the curb side (driver-door exit expected)
3. Both sides blocked (blocked feedback and Recovery exit expected)
4. Bridge deck with a sidewalk (same-level exit)
5. Bridge or elevated road without a sidewalk (Recovery exit; no drop to the lower road)
6. Tunnel approach
7. Road on an 8% grade (parking brake holds; exit on the uphill side)
8. Parking lot
9. Under an overpass (headroom check)
10. Rental return at a rental desk
11. Sandbox native vehicle with handback
12. Injected vehicle removal during entry, during exit, and while driving
13. Pause, focus loss, and a save request during each transition

## 7. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-007, OPEN-008 (surface and obstacle data for the
placement checks), OPEN-011 (handback), OPEN-012 (spawn and removal of career vehicles), OPEN-018 (which positions count
as parking or pedestrian access points), RISK-003, RISK-004, RISK-014. New: **OPEN-205**, whether parked career
vehicles stay physical in the world while the player is in `Building` or are stored virtually (this affects the
"park" outcome and city edits under parked vehicles).

## 8. Provenance

Stationary-first transitions, proximity selection, alternate exit points, blocked-exit feedback, the
never-inside-a-wall rule, the three outcomes, and the 50-cycle target come from the owner brief **[Owner requirement]**.
They are also in the historical plan in [chat.md](../../chat.md) **[Historical input]**. Candidate ordering, the check
thresholds, auto-park, and the scenario matrix are original proposals.
