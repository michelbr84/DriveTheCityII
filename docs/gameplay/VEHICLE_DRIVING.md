# Vehicle Driving

| Field | Value |
|---|---|
| ID | SPEC-DRIVING |
| Document status | Draft |
| Release scope | v1.0 (detailed cockpit is a Should; see §3.7) |
| Phases | PHASE-02 (one car), PHASE-03 (handling consistency, classes), PHASE-07 (upgrades and wear effects), PHASE-08 (van, rigid truck, bus), PHASE-11 (hood/driver camera) |
| Responsible role | Coordinator (maintainer); Implementer of the Vehicles module; Reviewer review for anything touching movement authority |
| Related requirements | REQ-DRIVE-001, REQ-DRIVE-002, REQ-DRIVE-003, REQ-DRIVE-004, REQ-WORLD-001, REQ-WORLD-002, REQ-WORLD-004, REQ-CTRL-001, REQ-ENTITY-002, REQ-TIME-003, REQ-UI-001 |
| Related documents | [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md), [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-TRANSITION](WALK_DRIVE_TRANSITIONS.md), [SPEC-ART](../experience/ART_AND_ANIMATION.md), [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) |
| Last updated | 2026-10-18 |

> **Status:** behavior specification only. Nothing is implemented, built, or tested. **The physics implementation is
> not chosen.** ADR-0004 stays Proposed until the PHASE-03 laboratory compares the candidates (OPEN-009). All
> parameter values are illustrative **[Proposal]**. Validation: **Not run**.

## 1. Purpose

This specification defines what driving must feel like and how it is controlled. It covers the control set, the
handling targets that any physics approach must meet, the four vehicle classes, the cameras, the difference between
sandbox and career vehicles, and how contact events feed into careers. It describes observable behavior. The movement
authority and the integration with the host game are owned by the architecture specifications listed above.

## 2. Scope and non-goals

**In scope:** direct player control of one vehicle at a time. That includes throttle, brake, reverse, progressive
steering, a parking brake, a speedometer, and a chase camera (REQ-DRIVE-001); consistent semi-realistic handling
(REQ-DRIVE-002); the car, light delivery vehicle, rigid truck, and bus classes (REQ-DRIVE-003); a hood or driver camera
(REQ-DRIVE-004); and contact events for careers.

**Non-goals for v1.0 [Owner requirement]:** simulator-grade or structural physics, structural deformation,
articulated trucks (CONF-003), police pursuits, part-by-part mechanics, a detailed cockpit for every vehicle, and
multiplayer. Also out of scope **[Proposal]**: manual gearboxes and clutch, tire temperature, damage visuals beyond
simple indicators, and entering or exiting a moving vehicle (see [SPEC-TRANSITION](WALK_DRIVE_TRANSITIONS.md)).

## 3. Behavior

### 3.1 Controls

| Control | Behavior | Illustrative value **[Proposal]** |
|---|---|---|
| Throttle | Analog on a gamepad trigger. On a keyboard, a digital press ramps up so that taps are gentle. | Keyboard ramp 0→100% in 0.4 s |
| Brake | Analog or ramped. Holding the brake at a standstill engages reverse after a short delay ("brake-to-reverse"). An explicit reverse toggle can be bound instead (SPEC-INPUT). | Reverse after 0.3 s at < 0.5 km/h |
| Reverse | Throttle drives backward. Reverse speed is capped per class. | Car cap 25 km/h |
| Progressive steering | Steering angle eases toward the input target. The maximum angle falls with speed (speed-sensitive steering), and the wheel self-centers on release. | Full lock at 0 km/h; about 35% of lock at 100 km/h |
| Parking brake | Toggle. Holds the vehicle on slopes. It engages automatically on exit, on entering `Paused` while stopped, and on save. | — |
| Horn | Cosmetic only. Native agents do not react in v1.0. | — |
| Speedometer | Always visible while `Driving`, in km/h or mph per setting ([SPEC-UI](../experience/UI_AND_HUD.md)). | — |
| Camera cycle | Chase camera, then hood/driver camera, then back (§3.7). | — |

All bindings are remappable and gamepad-capable (REQ-INPUT-001, REQ-INPUT-002). Driving input never reaches city tools
(REQ-CTRL-003). Losing focus releases throttle, brake, and steering immediately. A stationary vehicle keeps its
parking-brake state.

### 3.2 Handling targets (independent of the physics approach)

Both candidate approaches in ADR-0004 (an enhanced kinematic controller or a local physical controller) must produce
these observable properties. The comparison itself happens in PHASE-03; this document does not choose. Targets
**[Proposal]**:

1. **Consistency across frame rates** (REQ-DRIVE-002). A scripted input sequence (accelerate to 50 km/h, hold, brake to
   a stop) on the flat lab road gives a stopping distance within ±5% and a lateral path deviation under 0.5 m over 100 m
   at 30, 60, and an uncapped frame rate.
2. **Predictable weight.** Heavier classes accelerate, brake, and turn in measurably more slowly (§3.4). Payload
   changes these values in a monotonic, explained way.
3. **Surface fidelity.** The vehicle follows the valid surface of its current level. Bridges, tunnels, and stacked
   roads never cause a level switch (REQ-WORLD-001; [SPEC-CONTACT](../architecture/WORLD_CONTACT.md)).
4. **Grip limit.** Exceeding lateral grip produces understeer or a controlled slide that the player can recover from
   by easing off. It never produces a spin-out loop or a launch into the air.
5. **Slopes.** Uphill acceleration drops and downhill braking distance grows in proportion to the grade. The parking
   brake holds on the steepest road grade the game allows.
6. **No large jumps.** Per-frame motion is bounded (REQ-TIME-003). Pause and resume never add an impulse.
7. **Wear and upgrades act through parameters.** Condition, upgrades, and load change the documented parameters
   (§3.4; [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)), never ad-hoc special cases. This keeps their effects
   measurable (REQ-GARAGE-005).

[Source-inspected] material in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) describes the base mod's own kinematic
approach and frame-time clamping. That is evidence for the PHASE-03 comparison, not a decision.

### 3.3 Vehicle state shown to the player

Speed, the current gear direction (D/R), parking brake, fuel level, and condition warnings appear on the HUD
(REQ-UI-001). Fuel and condition are defined in SPEC-GARAGE. Payload or passenger count appears when a career mission
uses them.

### 3.4 Vehicle classes

Each class has its own parameter set. Values are illustrative **[Proposal]**, chosen only to make "distinct" testable.
Final values come from PHASE-03 and PHASE-08 tuning.

| Parameter | Car | Light delivery vehicle | Rigid truck (empty → max payload) | Bus (empty → full) |
|---|---|---|---|---|
| Curb mass | 1.3 t | 2.2 t | 7 t | 11 t |
| Capacity | 3 career passengers or 2 parcel slots | 10 parcel slots, 1.2 t | 8 t payload | 60 career passengers (≈ 4.5 t) |
| Top speed | 150 km/h | 120 km/h | 90 km/h | 80 km/h |
| 0–50 km/h | 4.5 s | 7 s | 12 s → 17 s | 14 s → 18 s |
| Stopping distance from 50 km/h (dry, flat) | 14 m | 17 m | 22 m → 27 m | 24 m → 28 m |
| Minimum turning radius | 5.5 m | 6.5 m | 9 m | 11 m |
| Length (used for access-point and parking validation) | 4.5 m | 6 m | 9.5 m | 12 m |
| Reverse speed cap | 25 km/h | 20 km/h | 12 km/h | 10 km/h |

**Distinctness rule [Proposal]:** every non-car class differs from the car by at least 15% in at least two of: 0–50 km/h
time, stopping distance, and minimum turning radius. It also has its own capacity rule (REQ-DRIVE-003). TEST-DRIVE-003
measures this.

**Payload effects:** effective mass is curb mass plus payload. Acceleration time and stopping distance interpolate
between the empty and full columns. Freight eligibility rules are in [SPEC-FREIGHT](careers/FREIGHT.md). Which
vehicle models represent each class depends on OPEN-034.

### 3.5 Sandbox mode versus career mode

| Aspect | Sandbox mode | Career mode |
|---|---|---|
| Which vehicles | Any eligible stationary native vehicle (`BorrowedNativeVehicle`) or a mod-spawned test vehicle | Only `CareerVehicle`s: Rental, Owned, or Service (REQ-ENTITY-002) |
| Ownership | Never. Borrowing grants nothing. | Per [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md) |
| Missions, wallet, XP | Disabled. No ledger entries. | Enabled |
| End of drive | Handback to native AI with route rebuild and lane cleanup (REQ-ENTITY-001) | Park, return, or store (SPEC-TRANSITION) |
| Labeling | The HUD shows a persistent "Sandbox" badge | No badge |

Career mode never silently turns a citizen's or service vehicle into player property **[Owner requirement]**. Whether
Sandbox mode is exposed to players in release builds, or kept as a development and QA mode, is OPEN-208.

### 3.6 Contact with the world and with traffic

- **Static world:** buildings and static obstacles stop the vehicle. It never passes through them (REQ-WORLD-002). The
  geometry source is defined in SPEC-CONTACT.
- **Native traffic and pedestrians:** one consistent rule, chosen in PHASE-03 (REQ-WORLD-004, OPEN-032). This document
  does not pre-empt it. Whatever the rule, it must never corrupt native agents' state. Pedestrians are never injured in
  v1.0 **[Proposal]**.
- **Contact events for gameplay:** every contact produces a `ContactEvent` with a category (static, vehicle,
  pedestrian-proximity) and a severity from the closing speed: Light < 10 km/h, Medium 10–30 km/h, Severe > 30 km/h
  **[Proposal]**. Careers consume these events for ride quality, cargo damage, and evaluations. SPEC-GARAGE consumes
  them for condition loss. There is no structural deformation.

### 3.7 Cameras

| Camera | Behavior | Limits **[Proposal]** |
|---|---|---|
| Chase (default) | Behind and above the vehicle. Distance and height grow slightly with speed. Spring-damped lag. Uses the same anti-clipping as the on-foot camera ([SPEC-PLAYER](PLAYER_AND_CAMERAS.md)). Free look orbits; the view recenters after 2 s without input. Per-class offsets keep a bus or truck fully framed. | Pitch −10° to +45° |
| Hood / driver position (REQ-DRIVE-004, Should) | Fixed at the class's estimated driver eye point. | Yaw ±100°, pitch ±60° |
| Detailed cockpit | Only for vehicle models that have a suitable interior, as listed in [SPEC-ART](../experience/ART_AND_ANIMATION.md) **[Owner requirement]**. Never advertised for other models. | — |

**Documented limitations of the hood/driver camera:**
- Models without an interior show no cabin. The view is an unobstructed windshield-level camera.
- Some models may show roof or mirror geometry from inside; these are listed per model in SPEC-ART.
- Near-plane clipping may expose engine geometry on short-hood vehicles.

The setting menu shows these limitations in plain language (SPEC-UI).

## 4. Failure handling and recovery

| Failure | Behavior |
|---|---|
| Vehicle stuck (wedged, flipped, or on an invalid surface) | Prompt for vehicle `Recovery` after 4 s without progress despite input, or immediately on an invalid surface. Recovery places the vehicle at the nearest validated lane position on the correct level ([SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md)). Mission timers keep their rules (SPEC-MISSION). |
| Vehicle entity removed or invalidated while `Driving` | Emergency exit as defined in SPEC-TRANSITION §4. Missions resolve per [SPEC-MISSION](MISSION_FRAMEWORK.md). |
| Movement authority conflict detected (native AI writes the controlled vehicle) | Treated as a critical error: control is released safely and diagnostics are logged ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md)). |
| Out of fuel or condition 0% | Limp reserve, then immobilization recovery ([SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)). |
| Frame stall | Bounded step only (SPEC-TIME). No impulse is carried into the next frame. |

## 5. Data that must persist

Owned by the vehicle record ([SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)), stored per
[SPEC-SAVE](../architecture/SAVE_FORMAT.md): class, model reference, fuel, condition, installed upgrades, and the last
parked pose as a stable location reference. Camera choice and steering-assist preferences are profile settings.
Transient dynamics (velocity, steering angle) are not saved. A loaded vehicle starts stationary with the parking brake
on.

## 6. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | A car can be driven with throttle, brake, reverse, progressive steering, and parking brake, with a speedometer and chase camera, on the PHASE-02 validation route. | TEST-DRIVE-001 | Not run |
| 2 | The scripted braking/path test meets the §3.2 tolerances at three frame-rate settings. | TEST-DRIVE-002 | Not run |
| 3 | Four classes meet the distinctness rule and their capacity rules. Payload changes the truck and bus measurements monotonically. | TEST-DRIVE-003 | Not run |
| 4 | The hood/driver camera works for every class with its documented limitations. The cockpit appears only on listed models. | TEST-DRIVE-004 | Not run |
| 5 | No level switching on lab bridges and tunnels; no passing through buildings. | TEST-WORLD-001, TEST-WORLD-002 | Not run |
| 6 | Native-traffic contact follows the chosen rule and leaves no corrupted native agents. | TEST-WORLD-004 | Not run |
| 7 | Career mode cannot drive a native vehicle; Sandbox mode hands back cleanly. | TEST-ENTITY-002, TEST-ENTITY-001 | Not run |

## 7. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-007, OPEN-008, OPEN-009 (physics approach),
OPEN-011 (handback), OPEN-014 and OPEN-025 (input, gamepad), OPEN-032 (contact rule), OPEN-034 (vehicle models), RISK-003,
RISK-004, RISK-008, RISK-020, RISK-023.

New, raised here: **OPEN-208**, whether Sandbox mode ships to players or stays a development and QA mode. Career
evaluations in v1.0 deliberately use only contacts, acceleration, and timing. Traffic-law scoring (speed limits,
signals) is not proposed, because it would add a city-data dependency the owner brief does not require.

## 8. Provenance

The control list, semi-realistic consistency, the four classes, the hood-camera limitation, and the cockpit rule come
from the owner brief **[Owner requirement]** and the historical plan in [chat.md](../../chat.md) **[Historical
input]**. Brake-to-reverse, speed-sensitive steering values, the distinctness rule, and the contact severity bands are
original proposals. Motor Town is only a behavioral reference ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)).
No handling model or data is taken from it.
