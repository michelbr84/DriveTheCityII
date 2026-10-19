# Player Journeys — Drive the City II

| Field | Value |
|---|---|
| ID | DOC-JOURNEYS |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 – PHASE-13 (each journey states when it becomes playable) |
| Responsible role | Coordinator (maintainer); QA uses the journeys to build end-to-end scenarios |
| Related requirements | See each journey; coverage summary in section 5 |
| Related documents | [PRD](PRD.md), [GAME_DESIGN](GAME_DESIGN.md), [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md), [TRACEABILITY](../planning/TRACEABILITY.md), [SPEC-UI](../experience/UI_AND_HUD.md) |
| Last updated | 2026-10-18 |

> **Status:** these journeys describe intended behavior. None of them can be played yet. Every planned test listed
> here has validation status **Not run**.

## 1. Purpose

A journey is a complete player story from a starting situation to a stable end state, including the ways it can go
wrong. Journeys connect the [PRD](PRD.md) requirements to experiences that QA can play through end to end. They do
not replace the gameplay and architecture specifications that define the rules. Where a journey describes a rule
that is not in the PRD, the rule is marked **[Proposal]** and the named specification decides it.

## 2. How to read a journey

- **Playable from:** the phase whose deliverables first make the journey possible end to end
  ([ROADMAP](../../ROADMAP.md)). Earlier phases may support parts of it.
- **Requirements / Planned tests:** `TEST-X-NNN` is the primary test of `REQ-X-NNN`. `TEST-SAVE-*` entries are
  defined in [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md); all others are defined in
  [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md). Validation: **Not run** for all.
- **Failure branches** are numbered `Fn` inside each journey. Every branch ends in a stable state with a stated next
  step (REQ-UI-003).
- Money amounts are illustrative **[Proposal]** values.
- Testing always uses a lab city or a copy, never the owner's primary saves (RISK-028).

## 3. Journey index

| ID | Journey | Playable from | Key requirements |
|---|---|---|---|
| [JRN-001](#jrn-001--first-time-use-install-tutorial-first-job) | First-time use: install, tutorial, first job | PHASE-06 (no tutorial); PHASE-11 (complete) | REQ-ONBOARD-001, REQ-TAXI-001 |
| [JRN-002](#jrn-002--normal-career-session) | Normal career session | PHASE-06 (taxi); PHASE-09 (complete) | REQ-TAXI-001, REQ-GARAGE-003 |
| [JRN-003](#jrn-003--returning-to-city-building-mid-career-and-coming-back) | Returning to city building mid-career and coming back | PHASE-05 (no career); PHASE-10 (complete) | REQ-CTRL-004, REQ-CITY-002 |
| [JRN-004](#jrn-004--loading-an-older-save) | Loading an older save | PHASE-01 (foundation); PHASE-07 (complete) | REQ-SAVE-001 |
| [JRN-005](#jrn-005--blocked-vehicle-exit) | Blocked vehicle exit | PHASE-05 | REQ-TRANS-002 |
| [JRN-006](#jrn-006--destination-removed-while-en-route) | Destination removed while en route | PHASE-08 (basic); PHASE-10 (complete) | REQ-CAREER-006 |
| [JRN-007](#jrn-007--insolvency-and-bankruptcy) | Insolvency and bankruptcy | PHASE-07 (personal); PHASE-09 (company) | REQ-ECON-004 |
| [JRN-008](#jrn-008--vehicle-recovery-out-of-fuel-immobilized-or-stuck) | Vehicle recovery: out of fuel, immobilized, or stuck | PHASE-05 (stuck); PHASE-07 (fuel, wear) | REQ-RECOV-001, REQ-GARAGE-006 |
| [JRN-009](#jrn-009--saving-and-loading-during-a-mission) | Saving and loading during a mission | PHASE-06 | REQ-SAVE-005 |
| [JRN-010](#jrn-010--disabling-the-mod-and-clean-exit) | Disabling the mod and clean exit | PHASE-01 (skeleton); PHASE-13 (release) | REQ-SAVE-006 |
| [JRN-011](#jrn-011--company-owner-session) | Company-owner session | PHASE-09 | REQ-COMPANY-002 |
| [JRN-012](#jrn-012--time-trial-on-the-players-own-roads) | Time trial on the player's own roads | PHASE-10 | REQ-CHAL-001 |

## 4. Journeys

### JRN-001 — First-time use: install, tutorial, first job

- **Playable from:** PHASE-06 as a raw loop, with the minimal rental and wallet slice of CONF-007 and no tutorial.
  The economic tutorial arrives in PHASE-07. The complete guided journey arrives in PHASE-11.
- **Requirements:** REQ-REL-001, REQ-ONBOARD-001, REQ-ECON-005, REQ-TIME-001, REQ-SAVE-001, REQ-GARAGE-001,
  REQ-TRANS-001, REQ-CAREER-002, REQ-TAXI-001, REQ-TAXI-002, REQ-TAXI-003, REQ-CAREER-004, REQ-UI-001,
  REQ-UI-004, REQ-CTRL-004
- **Planned tests:** TEST-REL-001, TEST-ONBOARD-001, TEST-ECON-005, TEST-TIME-001, TEST-SAVE-001, TEST-GARAGE-001,
  TEST-TRANS-001, TEST-CAREER-002, TEST-TAXI-001, TEST-TAXI-002, TEST-TAXI-003, TEST-CAREER-004, TEST-UI-001,
  TEST-UI-004, TEST-CTRL-004

**Preconditions:** a supported CS2 configuration ([COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md); none is
tested yet). The mod is installed following [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md),
which is a pre-release draft; no tested installer exists. A city with a connected road network and the minimum content
for taxi work (OPEN-301). No career profile exists for this save.

**Steps:**

1. The player loads the city in Building mode. A one-time notice says the mod is active and shows how to enter
   character mode. How the entry point is presented is OPEN-302.
2. The player enters character mode. The mod records the current simulation speed and runs at normal speed
   (REQ-TIME-001). It creates a career profile bound to this save (REQ-SAVE-001) and places the avatar at a validated
   safe point near a rental service point.
3. The tutorial offers **Start** or **Skip**. The movement stage teaches walking, looking, running, and the camera
   switch, with prompts for the active input device ([SPEC-ONBOARD](../experience/ONBOARDING.md)).
4. The player walks to the rental desk with on-foot guidance. The economic tutorial explains that the wallet is
   separate from the city budget, what renting costs, and that a short wallet means the rental is paid from the first
   earnings **[Proposal]** (REQ-ECON-005).
5. The player rents a basic car (REQ-GARAGE-001). The ledger records the transaction.
6. The player enters the car through the interaction prompt (REQ-TRANS-001). The driving stage teaches throttle,
   brake, reverse, steering, the parking brake, and the camera.
7. The player accepts the tutorial fare, which shows a fare estimate (REQ-TAXI-003). It is a short, validated,
   reachable trip with no deadline **[Proposal]**.
8. The player drives to the pickup access point. A career passenger boards visibly (REQ-TAXI-002).
9. The player follows the route view to the destination (REQ-UI-004). The passenger alights. A settlement summary
   shows a single payment and experience (REQ-CAREER-004).
10. The tutorial asks the player to park and exit, then explains that career progress is saved with the city save.
    The player saves.
11. Optionally, the player returns to Building mode. The prior camera and simulation speed are restored
    (REQ-CTRL-004).

**Expected experience:** a new player finishes their first paid job in one short session without the console or
outside help. They understand three things: the wallet is theirs and not the city's; they can leave and come back
at any time; and nothing they did changed the city.

**Failure branches:**

- **F1 — The mod does not load or the game version is not supported.** City building is unaffected. If any mod UI can
  load, it shows a short message; the log records the version and the reason (REQ-DIAG-001, REQ-COMPAT-001). Next
  step: [TROUBLESHOOTING](../user/TROUBLESHOOTING.md).
- **F2 — The city lacks the minimum content** (for example, no connected roads or no eligible buildings). Character
  mode or the career is blocked with an explanation of what to build (OPEN-301, REQ-UI-003).
- **F3 — No safe spawn point exists near a rental point.** The mod uses another validated access point. If none
  exists, it explains why and stays in Building mode.
- **F4 — The player is lost or idle.** Route guidance stays visible. A contextual hint appears after a period of
  inactivity **[Proposal]**. Player-invoked `Recovery` is available.
- **F5 — The player skips the tutorial.** It can be replayed from the career menu. The economic explanation still
  appears the first time the player rents.
- **F6 — The tutorial fare is invalidated** (for example, the player edits the city). A new tutorial fare is offered
  without penalty (REQ-CAREER-006).
- **F7 — The player quits mid-tutorial without saving.** The next load resumes from the last saved tutorial stage,
  because tutorial progress is career data bound to the save.

### JRN-002 — Normal career session

- **Playable from:** PHASE-06 (taxi only); PHASE-07 (fuel, garage, ownership); PHASE-08 (all four careers);
  PHASE-09 (home base).
- **Requirements:** REQ-SAVE-001, REQ-SAVE-003, REQ-HOME-001, REQ-CAREER-002, REQ-TAXI-001, REQ-DELIV-001,
  REQ-GARAGE-003, REQ-GARAGE-004, REQ-ECON-002, REQ-ECON-003, REQ-UI-001, REQ-UI-002
- **Planned tests:** TEST-SAVE-001, TEST-SAVE-003, TEST-HOME-001, TEST-CAREER-002, TEST-TAXI-001, TEST-DELIV-001,
  TEST-GARAGE-003, TEST-GARAGE-004, TEST-ECON-002, TEST-ECON-003, TEST-UI-001, TEST-UI-002

**Preconditions:** an existing career profile in this save with an owned vehicle, a home base, and a garage location.

**Steps:**

1. The player loads the save. Career data is validated against the save identity, and stored references are
   revalidated (REQ-SAVE-001, REQ-SAVE-003).
2. The player enters character mode and starts at the home base access point (REQ-HOME-001). Offering a choice
   between "start at home" and "start at my parked vehicle" is a **[Proposal]** for SPEC-HOME.
3. The player walks to the owned vehicle at the garage location. The HUD shows fuel and condition.
4. The player opens the job board, compares offers from unlocked careers, and accepts a delivery contract whose
   access points were validated as reachable (REQ-CAREER-002, REQ-DELIV-001).
5. The player drives the stops in order. The HUD shows the objective, route, deadline, and cargo state.
6. Settlement: one payment, experience, and a reputation change. A level-up unlock is announced (REQ-ECON-003).
7. The player refuels at a fuel service point (REQ-GARAGE-003) and repairs moderate wear (REQ-GARAGE-004). Each
   appears in the ledger (REQ-ECON-002).
8. The player parks at the garage location, exits, saves, and returns to Building mode or quits.

**Expected experience:** natural stopping points after every settlement. The player always knows their balance,
where the next work is, and what their vehicle needs.

**Failure branches:**

- **F1 — The owned vehicle is missing or invalid on load** (for example, its parking location no longer exists). The
  vehicle is placed at the garage location or the nearest valid point, and a notice explains this (REQ-SAVE-003).
- **F2 — Low fuel.** The HUD warns at defined thresholds and suggests the nearest reachable fuel point. If the tank
  runs dry, see [JRN-008](#jrn-008--vehicle-recovery-out-of-fuel-immobilized-or-stuck).
- **F3 — No offers for a career.** The board explains why (for example, "No reachable loading bays in this city") and
  points to the always-available taxi work.
- **F4 — The destination becomes invalid.** See [JRN-006](#jrn-006--destination-removed-while-en-route).
- **F5 — A collision damages cargo.** The quality component drops, and the summary explains the deduction.

### JRN-003 — Returning to city building mid-career and coming back

- **Playable from:** PHASE-05 (switching modes without a career); PHASE-06 (with an active mission); PHASE-10
  (revalidation after edits).
- **Requirements:** REQ-CTRL-003, REQ-CTRL-004, REQ-TIME-001, REQ-TIME-004, REQ-ENTITY-003, REQ-CITY-002,
  REQ-CAREER-006
- **Planned tests:** TEST-CTRL-003, TEST-CTRL-004, TEST-TIME-001, TEST-TIME-004, TEST-ENTITY-003, TEST-CITY-002,
  TEST-CAREER-006

**Preconditions:** the player had Building mode at a fast simulation speed, then entered character mode. They are now
driving a rented taxi with a career passenger aboard.

**Steps:**

1. The player requests a return to Building. If the vehicle is moving, the HUD asks them to stop first; requiring a
   stop is a **[Proposal]** for SPEC-CONTROL. Once the vehicle is stopped (a stable state), the switch proceeds.
2. The mod hands the camera back to Building mode and restores the recorded simulation speed (REQ-CTRL-004,
   REQ-TIME-001). Character-mode input stops (REQ-CTRL-003).
3. The career vehicle stays where it was left. If it stopped in a traffic lane, it is moved to the nearest valid curb
   or parking position, or stored and restored on return, as defined in
   [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md). It never becomes an invisible traffic blocker.
4. The active mission is suspended. Its deadline does not run while the player is in Building mode **[Proposal]**,
   as decided in [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md). Company earnings follow persisted time
   checkpoints and are never duplicated by the switch (REQ-TIME-004).
5. The player builds a new road and saves in Building mode. Career data, including the suspended mission, is saved
   with that revision.
6. The player enters character mode again. The mod revalidates the avatar position, career vehicles, missions, home
   base, and service points (REQ-CITY-002). The player resumes at the vehicle, the route is recalculated (it may use
   the new road), and the mission continues.

**Expected experience:** switching modes costs nothing and loses nothing. The player trusts that they can build in
the middle of a shift.

**Failure branches:**

- **F1 — The road under the parked vehicle was demolished.** The vehicle is moved to the nearest valid point. If that
  fails, a no-fault recovery moves it to a service point at no cost **[Proposal]**, and a notice explains this.
- **F2 — The mission destination was removed.** See [JRN-006](#jrn-006--destination-removed-while-en-route).
- **F3 — The return is requested during an unstable state** (`EnteringVehicle`, `ExitingVehicle`, or `Recovery`).
  The request waits until the state is stable, with visible feedback, or it is refused with a reason.
- **F4 — The player loads an older save while in Building mode.** See [JRN-004](#jrn-004--loading-an-older-save).

### JRN-004 — Loading an older save

- **Playable from:** PHASE-01 (foundation tests in a lab city); PHASE-06 (wallet); PHASE-07 (ownership). PHASE-12
  repeats it across 30 proposed save/load cycles.
- **Requirements:** REQ-SAVE-001, REQ-SAVE-002, REQ-SAVE-003, REQ-SAVE-004, REQ-RECOV-002, REQ-DIAG-001
- **Planned tests:** TEST-SAVE-001, TEST-SAVE-002, TEST-SAVE-003, TEST-SAVE-004, TEST-RECOV-002, TEST-DIAG-001

**Preconditions:** the player saved the city as **Save A** (wallet 1,000; one rental). They kept playing, bought a
truck, and saved as a separate **Save B** (wallet 5,000). Both saves exist.

**Steps:**

1. The player loads Save A.
2. The mod reads only the career data bound to Save A's save identity (city identity, career profile ID, and save
   revision ID), as defined in [SPEC-SAVE](../architecture/SAVE_FORMAT.md) and
   [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md).
3. The wallet shows 1,000. There is no truck. The ledger ends at Save A's last transaction. Unlocks and reputation
   match Save A.

**Expected experience:** what the player sees always belongs to the save they loaded. Money from a newer session never
leaks into an older save (REQ-SAVE-001).

**Failure branches and variants:**

- **F1 — The career data has an older schema.** It migrates forward. The last valid copy is kept until the migrated
  data validates (REQ-SAVE-002, REQ-SAVE-004).
- **F2 — The career data has a newer, unknown schema** (the save was made with a newer mod version). The mod refuses
  to load it and does not delete or overwrite it. A message explains that the career data needs a newer mod version
  and that the city is unaffected. Whether character mode stays available without a career in this case is a
  **[Proposal]** for SPEC-SAVE.
- **F3 — The save predates the mod.** No career data exists, and the player is offered a new profile.
- **F4 — The career data is invalid or corrupt.** It is quarantined. The last valid copy is restored if one exists,
  and a notice offers a sanitized diagnostics export (REQ-SAVE-004, REQ-DIAG-001).
- **F5 — Another city has the same name.** No data is shared. City identity is never just the city name.

### JRN-005 — Blocked vehicle exit

- **Playable from:** PHASE-05
- **Requirements:** REQ-TRANS-002, REQ-TRANS-003, REQ-TRANS-004, REQ-WORLD-001, REQ-UI-003, REQ-RECOV-001
- **Planned tests:** TEST-TRANS-002, TEST-TRANS-003, TEST-TRANS-004, TEST-WORLD-001, TEST-UI-003, TEST-RECOV-001

**Preconditions:** the player is driving a career vehicle and stops in a narrow street. A wall is on the curb side,
and traffic is passing on the other side.

**Steps:**

1. The player presses **Exit vehicle**. If the vehicle is still moving, the prompt says "Stop the vehicle to get
   out".
2. The mod checks the preferred curb-side exit point. The wall blocks it.
3. The mod tries the alternate exit points defined in [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md)
   (street side, then front or rear). The street side is unsafe because of traffic.
4. If any candidate is valid (free space, headroom, correct elevation), the avatar exits there, and a short notice
   explains the alternative side.
5. If no candidate is valid, the player stays in `Driving` with full control. The message reads: "No safe place to
   get out here. Move to a spot with space beside the vehicle." The prompt also offers **Hold Recover** to move to
   the nearest safe point (REQ-UI-003).

**Expected experience:** the avatar is never placed inside a wall, in traffic, or on the wrong level. The camera
never detaches. The player always knows why the exit failed.

**Failure branches:**

- **F1 — On a bridge deck or elevated road.** Exit points must be on the same deck (REQ-WORLD-001). If the deck has
  no walkable space, the message suggests driving to a street-level road.
- **F2 — In a tunnel.** The same rule applies: same level only, with headroom checked.
- **F3 — The vehicle is removed or invalidated during `ExitingVehicle`.** The transition aborts safely, and the avatar
  is placed at a validated point or `Recovery` runs (REQ-TRANS-003).
- **F4 — Sandbox mode with a borrowed native vehicle.** After a successful exit, the vehicle is handed back to native
  AI (REQ-TRANS-004).

### JRN-006 — Destination removed while en route

- **Playable from:** PHASE-08 (basic detection on return from Building mode); PHASE-10 (complete reaction to city
  edits).
- **Requirements:** REQ-CAREER-006, REQ-CITY-002, REQ-BUS-002, REQ-CAREER-004, REQ-CAREER-005, REQ-HOME-003,
  REQ-UI-003
- **Planned tests:** TEST-CAREER-006, TEST-CITY-002, TEST-BUS-002, TEST-CAREER-004, TEST-CAREER-005,
  TEST-HOME-003, TEST-UI-003

**Preconditions:** an active mission with a career passenger aboard, or cargo loaded, heading for destination
building D.

**Triggers:**

- (a) the player returns to Building mode and demolishes D or the road that gives access to it;
- (b) D becomes invalid or unreachable through the simulation itself;
- (c) for the bus career, the line's remaining stops are moved or removed (REQ-BUS-002).

How the mod detects edits is OPEN-021. The fallback is a full revalidation every time the player returns from
Building mode.

**Steps:**

1. The mission is revalidated (REQ-CITY-002).
2. If D still has another valid access point, the route is updated and a notice says "Destination access changed —
   route updated".
3. If D is gone or unreachable, the mission becomes `Invalidated` and resolves to a no-fault cancellation. The career
   passenger alights at the nearest safe access point, or the cargo is handled as defined in
   [SPEC-DELIVERY](../gameplay/careers/DELIVERIES.md) and [SPEC-FREIGHT](../gameplay/careers/FREIGHT.md). There is no
   money or reputation penalty. Paying for the distance already driven is a **[Proposal]** for
   [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md). The settlement happens exactly once (REQ-CAREER-004).
4. New offers are generated from the edited city.

**Expected experience:** building never punishes the career. No mission stays impossible.

**Failure branches:**

- **F1 — Invalidation during a transition.** It is applied once the state is stable.
- **F2 — A destination that keeps flickering between valid and invalid.** A bounded number of reroutes is allowed,
  then the mission is cancelled without fault **[Proposal]**.
- **F3 — The edit disconnects the player's own position from the road network.** `Recovery` is offered.
- **F4 — The home base or a service point is removed.** The player is notified, and a fallback is used
  (REQ-HOME-003).
- **F5 — Bus line edited mid-shift.** The shift is re-planned over the remaining valid stops in order, or ended
  without fault (REQ-BUS-002).

### JRN-007 — Insolvency and bankruptcy

- **Playable from:** PHASE-07 (personal insolvency); PHASE-09 (company insolvency).
- **Requirements:** REQ-ECON-004, REQ-ECON-002, REQ-GARAGE-001, REQ-GARAGE-002, REQ-GARAGE-006, REQ-COMPANY-003,
  REQ-HOME-003, REQ-UI-003
- **Planned tests:** TEST-ECON-004, TEST-ECON-002, TEST-GARAGE-001, TEST-GARAGE-002, TEST-GARAGE-006,
  TEST-COMPANY-003, TEST-HOME-003, TEST-UI-003

**Preconditions (personal):** the wallet holds 40. The owned car needs a repair that costs 300 before it can be driven.

**Steps (personal):**

1. The repair screen shows the repair as unaffordable, with the reason and options (REQ-UI-003):
   - take the always-available taxi starter job with a basic rental paid from earnings (REQ-ECON-004,
     REQ-GARAGE-001);
   - sell a vehicle (REQ-GARAGE-002);
   - choose a cheaper minimal repair that makes the car drivable, if SPEC-GARAGE offers one.
2. The player takes the starter path, completes a few fares, and returns to repair the car. Every step is in the
   ledger (REQ-ECON-002).

Whether the wallet may go below zero is decided in [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md). The product
rule is that no involuntary charge can leave the player without an available action (RISK-019).

**Preconditions (company):** salaries are due at the next settlement checkpoint, and the company balance is short.

**Steps (company)** — rules belong to [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md); this is the intended
experience **[Proposal]**:

1. One checkpoint ahead, the company menu warns about the shortfall.
2. The player can transfer money from the personal wallet (an explicit, ledgered action), release an employee, or
   pause contracts.
3. If nothing is done, the company becomes dormant at the checkpoint: contracts are suspended, staff are released,
   and fleet records are kept. The personal wallet is never charged without the player's consent.

**Expected experience:** going broke is a setback, not a dead end. The way back is visible on the same screen.

**Failure branches:**

- **F1 — Broke, with no vehicle, far from a rental point.** The player can walk, or use a recovery transport to the
  nearest rental point, which is free when the player cannot pay **[Proposal]**.
- **F2 — The rental service point was demolished.** A fallback rental point is used (REQ-HOME-003). If none is
  reachable, the starter rental is delivered to the player's position **[Proposal]** (REQ-GARAGE-006).
- **F3 — Repeated failures.** The starter path always remains available.

### JRN-008 — Vehicle recovery: out of fuel, immobilized, or stuck

- **Playable from:** PHASE-04 and PHASE-05 (stuck avatar or vehicle); PHASE-07 (fuel and immobilization).
- **Requirements:** REQ-RECOV-001, REQ-RECOV-002, REQ-WORLD-001, REQ-GARAGE-003, REQ-GARAGE-004, REQ-GARAGE-006,
  REQ-ECON-004
- **Planned tests:** TEST-RECOV-001, TEST-RECOV-002, TEST-WORLD-001, TEST-GARAGE-003, TEST-GARAGE-004,
  TEST-GARAGE-006, TEST-ECON-004

**Case A — out of fuel.**

1. The HUD warns at two fuel thresholds **[Proposal]** and suggests the nearest reachable fuel point.
2. If the tank empties, the vehicle coasts to a stop. A panel offers options **[Proposal]**, with costs shown and
   deferrable when the player is short: a roadside fuel delivery, a tow to the nearest fuel point, or (for rentals)
   ending the rental and walking.
3. An active mission can continue after refuelling if its constraints still allow it. Otherwise it fails under the
   normal, bounded failure rules.

**Case B — immobilized by wear or damage.**

1. The vehicle cannot be driven. Options: a tow to the nearest reachable repair service point, or a basic rental to
   keep working (REQ-GARAGE-006).
2. If the repair is unaffordable, see [JRN-007](#jrn-007--insolvency-and-bankruptcy).

**Case C — physically stuck** (wedged, overturned, outside valid ground, or on the wrong level).

1. Automatic detection or a player-held **Recover** starts the `Recovery` state.
2. The vehicle and the avatar move to the nearest validated safe point on the correct level (REQ-RECOV-001,
   REQ-WORLD-001).
3. Recovery for stuck cases is free. To prevent it from becoming a shortcut, it never moves the player closer to the
   current destination than they were **[Proposal]** ([SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md)).

**Expected experience:** the player is moving again within a minute or two and understands what happened.

**Failure branches:**

- **F1 — No reachable repair point exists.** The vehicle is towed to the garage location or home base, or an
  explained alternative is offered (REQ-GARAGE-006).
- **F2 — `Recovery` cannot find any safe point.** As a last resort, the mod returns to Building mode safely. The
  career vehicle is stored, and the event is logged (REQ-RECOV-002).

### JRN-009 — Saving and loading during a mission

- **Playable from:** PHASE-06 (the proposed 20-ride gate includes a save/load during a mission).
- **Requirements:** REQ-SAVE-005, REQ-SAVE-003, REQ-CAREER-004, REQ-ENTITY-003, REQ-TIME-004
- **Planned tests:** TEST-SAVE-005, TEST-SAVE-003, TEST-CAREER-004, TEST-ENTITY-003, TEST-TIME-004

**Preconditions:** an active taxi mission. The career passenger is aboard, and the leg to the destination is
`Approaching`.

**Steps:**

1. The player saves.
2. The player completes the fare and is paid, then quits **without saving**.
3. Later, the player loads the save from step 1.
4. The mission is restored at the same leg with the passenger aboard. Alternatively, it is resolved safely under the
   rules of [SPEC-SAVE](../architecture/SAVE_FORMAT.md) and [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md) (for
   example, with the vehicle restored at a validated point). The payment from step 2 is absent because it was never
   saved.
5. The player completes the fare again and is paid exactly once in this save's history (REQ-CAREER-004).

**Expected experience:** what was saved is exactly what comes back.

**Failure branches and variants:**

- **F1 — Saving at the moment of settlement.** After loading, the job is either settled or not, never twice. The
  TransactionId makes the payout idempotent.
- **F2 — Saving during `EnteringVehicle` or `ExitingVehicle`.** After loading, the player is in a stable state; on
  foot next to the vehicle is the **[Proposal]** default.
- **F3 — A stored reference no longer resolves after loading.** The reference is revalidated. The mission is
  resolved without fault, and no entity is left orphaned (REQ-SAVE-003, REQ-ENTITY-003).
- The same rules apply to any save written while a mission is active, whoever starts it.

### JRN-010 — Disabling the mod and clean exit

- **Playable from:** PHASE-01 (skeleton: the city remains loadable); PHASE-13 (release candidate: the tested
  procedure).
- **Requirements:** REQ-DIAG-002, REQ-ENTITY-003, REQ-SAVE-006, REQ-CITY-004, REQ-REL-002, REQ-COMPAT-001
- **Planned tests:** TEST-DIAG-002, TEST-ENTITY-003, TEST-SAVE-006, TEST-CITY-004, TEST-REL-002, TEST-COMPAT-001

**Preconditions:** the player wants to stop using the mod, or to troubleshoot without it.

**Steps (intended clean-exit procedure; untested):**

1. The player finishes or cancels the active mission, or lets the safe-disable switch resolve it.
2. The player turns on the safe-disable switch in the mod settings (REQ-DIAG-002). The mod returns to `Building`,
   stops career systems, and parks or despawns its own entities according to their lifecycle (REQ-ENTITY-003).
3. The player saves to a **new** save file and keeps the original.
4. The player quits and disables or removes the mod through the game's mod management. The exact steps are documented
   in [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) only after they have been observed.
5. The player loads the new save without the mod. The city loads, no mod objects remain, and buildings, roads,
   budgets, and citizens are unchanged (REQ-CITY-004, REQ-SAVE-006).

**Expected experience:** leaving is safe and documented. No claim of safe uninstallation is made before this
procedure passes its tests (REQ-REL-002).

**Failure branches and variants:**

- **F1 — The mod is removed without the clean exit.** The city must still load. The observed result is documented
  (OPEN-022, RISK-017).
- **F2 — Re-enabling the mod with a save made while it was disabled.** Community documentation states that a mod's
  in-save components are dropped when the mod is absent **[Documented upstream]** (unverified; see
  [SOURCES](../research/SOURCES.md)). The player is told whether career data exists for this save revision, and is
  offered a new profile or an earlier save. The behavior is defined in SPEC-SAVE once OPEN-006 is answered.
- **F3 — The original Beta Test Driving Mod is also enabled.** The conflict is detected or documented, and the two
  controllers are never active together (REQ-COMPAT-001).

### JRN-011 — Company-owner session

- **Playable from:** PHASE-09
- **Requirements:** REQ-COMPANY-001, REQ-COMPANY-002, REQ-COMPANY-003, REQ-TIME-004, REQ-ECON-002, REQ-UI-002,
  REQ-SAVE-001
- **Planned tests:** TEST-COMPANY-001, TEST-COMPANY-002, TEST-COMPANY-003, TEST-TIME-004, TEST-ECON-002,
  TEST-UI-002, TEST-SAVE-001

**Preconditions:** the player has founded a company, owns two vehicles, and has hired one driver.

**Steps:**

1. The player enters character mode at the home base and opens the **Company** menu. Its header shows the label
   **Managerial simulation** (REQ-COMPANY-002).
2. The player reviews the last settlement report: contract income, salaries, and costs per checkpoint, each linked to
   ledger entries (REQ-ECON-002).
3. The player accepts a company contract and assigns it to a fleet record and an employee. The UI states that
   company work is computed and does not appear as vehicles in the city.
4. The player hires a second driver. The salary and the next due checkpoint are shown.
5. The player drives a personal job in a vehicle that is not assigned to the company.
6. Company income and costs settle once at each persisted checkpoint (REQ-COMPANY-003, REQ-TIME-004). Returning to
   Building mode and coming back, or saving and reloading, produces no duplicate settlement.

**Expected experience:** the company feels like a business layer above the player's own driving, and it never
pretends to be physical.

**Failure branches:**

- **F1 — Assigning the vehicle the player is driving.** Blocked, with the reason (REQ-UI-003).
- **F2 — A company contract's destination becomes invalid after a city edit.** The contract is re-planned or
  cancelled without fault.
- **F3 — Salaries cannot be paid.** See [JRN-007](#jrn-007--insolvency-and-bankruptcy).
- **F4 — An older save is loaded.** The company state matches that save
  ([JRN-004](#jrn-004--loading-an-older-save)).

### JRN-012 — Time trial on the player's own roads

- **Playable from:** PHASE-10
- **Requirements:** REQ-CHAL-001, REQ-CHAL-002, REQ-CITY-002, REQ-SAVE-001
- **Planned tests:** TEST-CHAL-001, TEST-CHAL-002, TEST-CITY-002, TEST-SAVE-001

**Steps:**

1. The player opens **Challenges** and picks a checkpoint route on their city's roads. The route may be generated or
   placed by the player **[Proposal]** ([SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md)).
2. The route is validated, a countdown runs, and the player drives through the checkpoints.
3. The result is stored per profile and shown against the player's best time (REQ-CHAL-001).

**Failure branches:**

- **F1 — A city edit breaks the route.** The route is flagged on return and must be repaired or regenerated before
  the next attempt (REQ-CITY-002).
- **F2 — The player gets stuck mid-run.** `Recovery` voids the attempt without penalty.

## 5. Coverage summary

| Required journey (owner brief) | Journey |
|---|---|
| First-time use (install → tutorial → first job) | JRN-001 |
| Normal career session | JRN-002 |
| Returning to city building mid-career and coming back | JRN-003 |
| Loading an older save (no newer-session money leaks) | JRN-004 |
| Blocked vehicle exit | JRN-005 |
| Removed destination (demolished while en route) | JRN-006 |
| Bankruptcy and insolvency | JRN-007 |
| Vehicle recovery (immobilized, out of fuel, stuck) | JRN-008 |
| Mid-mission save/load | JRN-009 |
| Mod disabled and clean exit | JRN-010 |
| Company-owner session | JRN-011 |

QA turns each journey into an end-to-end scenario in [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md).
[TRACEABILITY](../planning/TRACEABILITY.md) links requirements to tests. Journeys do not create requirements.

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-301 (new) | Minimum city content needed for character mode and for each career (JRN-001 F2, JRN-002 F3). |
| OPEN-302 (new) | How a first-time player discovers the entry point into character mode (JRN-001 step 1). |
| OPEN-006 | Persistence mechanism and save identity (JRN-004, JRN-009, JRN-010). |
| OPEN-021 | Detecting city edits (JRN-003, JRN-006). |
| OPEN-022 | Saves after the mod is disabled or removed (JRN-010). |
| RISK-005, RISK-006 | Save leakage and duplicated rewards (JRN-004, JRN-009, JRN-011). |
| RISK-018, RISK-019 | City edits invalidating missions; economic softlock (JRN-006, JRN-007). |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
