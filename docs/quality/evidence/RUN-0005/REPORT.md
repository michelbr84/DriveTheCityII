# RUN-0005 — Standing queues (ISSUE-033), bus turn-rounds (ISSUE-034), first human session

| Field | Value |
|---|---|
| Run ID | RUN-0005 |
| Phases | PHASE-06 to PHASE-08 (careers, traffic presence), PHASE-12 (validation) |
| Date | 2026-10-20 |
| Executed by | Claude Code agent (owner request of 2026-10-20: "continue com a ISSUE-033 e a ISSUE-034"); no human reviewer yet |
| Input | §1: **human input** (the owner's own session), read from the mod log only. §2–§4: **simulated** — test-bridge commands and the TEST AUTOPILOT (`career drive`); career rules run unchanged. Test setup steps (`tpveh`, `trafficsetup`, `parkfix`, `trafficfix`, `career abtest`) are labelled. |
| Builds | `0.1.0-bd5ca0f+dirty-20261020T051630Z` (§2, §3.1 before), `…T053006Z` (§3.1 after), `…T055431Z` (§3.1 switched), `…T060800Z` (§3.3 episode 1), `…T063508Z` (§3.3 junction test, §3.4; 172 unit tests). |
| Sessions | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261020-011700`, `-013027`, `-015441`, `-020810`, `-023545`; final package `-030033` (M1, not run), `-030303` (persistence) |

## 1. The owner's own session (human input, 2026-10-20 00:13–00:55)

The owner played the installed package `0.1.0-af308eb` in their own city with real keyboard and mouse. This was not a
test session (bridge disabled, own save). Only the mod's log was read; it was overwritten when the next test session
started, so the figures below are the excerpts recorded at the time (no personal data).

| What | From the log |
|---|---|
| Load | Own city loaded; "no career data in this save (new career)"; career created. No errors; the only warnings are 4 × "CameraRig: another controller became active; re-acquiring" at 00:47 (re-acquired each time). |
| Character mode | Entered 7 times; walking, F8 back to city building. |
| Sandbox driving | 4 native cars borrowed and driven, handed back on exit. 24 impacts logged, up to 29 m/s (map edge, static objects): fast driving. |
| Entering vehicles | **168 × "No vehicle close enough"** (ISSUE-035): parked cars cannot be borrowed. |
| Character mode start | 5 × "The spawn point is blocked" in 9 s (ISSUE-038). |
| Recovery | Backspace 3 times in 38 s on foot, each successful; the third ended character mode (ISSUE-036). |
| Career | Duty on; basic rental taken; company founding refused (level 10 and reputation 60 needed, as designed). Delivery job accepted, parcels loaded at 137 Ashbourne Street, then a 19.9 m/s collision and a 15.5 m/s impact destroyed a parcel: job failed, 0 cr, reputation −5 (rules as designed). A second delivery accepted before quitting. |
| Garage | "Accept a starter taxi or courier job" clicked 3 times: "That option is not available here." (ISSUE-037) |
| Bus career | "0 bus lines indexed" all session (ISSUE-039, open question to the owner). |
| Saves | Own city saved twice by hand and autosaved every 5 minutes; the career envelope was written each time; saving while driving excluded and restored the mod actor as designed. |

## 2. ISSUE-034 — bus turn-rounds

Session `20261020-011700`. Line 1 shift accepted; the operator's bus boarded at the depot.

**Route comparison on one pose** (test setup: `tpveh -689.8 380.9 -4330.7 84.5`, the bus on the eastbound lane of a
two-lane street, facing away from the stop 163 m behind it). `routetest <start penalty> <junction U-turn penalty>`
computes the route from the same pose without traffic costs:

| Penalties (s) | Route | Junction U-turns | Starts against heading |
|---|---|---|---|
| 120 / 0 (before the fix) | 426 m | **1** | no |
| 400 / 0 (the fix prepared on 2026-10-19) | 426 m | **1** | no |
| 120 / 400 | 433 m | 0 | no |
| 400 / 400 (now, for vehicles over 10.5 m) | 433 m, round the block | 0 | no |

The live guidance (`routeinfo`, with traffic costs) gave the same 433 m route with no U-turn. From the depot the route
had no U-turn under any setting (1354 m).

**Drive:** `career drive 400 1` — the bus went round the block and served the stop: "reached leg 1 after 154 s;
threePointTurns=0 … recoveries=2". Both recoveries were kerb strikes on tight right turns at junctions (vehicles turn
about the body centre, so the ends of a 12.7 m bus swing outward onto the corner, ISSUE-040); the recovery put the bus
on the next road each time. [simulated input via test autopilot]

## 3. ISSUE-033 — standing queues

### 3.0 Diagnostics corrected

The earlier chain readings stopped at a vehicle "with no blocker". Both such heads (sessions 20261019-200131 and
-212310) had motion data but no `Blocker` component — the signature of a **trailer**, whose tractor holds the blocker
(`Controller`). The game's own deadlock check (`StuckMovingObjectSystem`) follows that link; our `blockchain` and
`blockers` did not, so "the chain does not involve the player's vehicle" was not established. New diagnostics follow
the game's rule: `jams` (every stopped native car in the city and what its chain ends at), `ghosts` (lane entries far
from their vehicle), `blockchain` (with head details), `trafficwatch … parked`.

### 3.1 A parked player vehicle holds a queue indefinitely

Session `20261020-011700`, before the fix. The player's rental was left in the street at (187, −1442) (lane 1165186)
at 01:20:32 to board the bus. Stopped native cars whose chain ends at it (`jams`, `MOD303113`):

| Time | 01:25:07 | 01:27:11 | 01:28:11 | 01:29:11 |
|---|---|---|---|---|
| Cars queued behind the parked rental | 12 | 45 | 61 | **81** |

The vehicle is registered in the lane as traffic with zero speed; native cars wait behind standing traffic without
limit and the queue spills back through junctions. Removing the player's vehicles (return to city building) clears
it — the observation recorded on 2026-10-19.

**Fix:** a parked vehicle of the player's loses the game's motion data (`Moving`) and gets it back when driven; the
game then treats it as a stationary obstacle (`CarNavigationSystem.CheckBlocker`: change lane, or edge past).

Session `20261020-013027`, after the fix, same lane and spot (t = 0.13), rental parked at 01:47:26: 0 cars queued
behind it at 01:47:34, 01:48:34, 01:49:34 and 01:50:34 (the 3 chains listed then end at the bus waiting at the depot).
The street was quiet at that time (no vehicle within 60 m for 30 s), so the controlled comparison below decides.

**Same spot, fix switched** (session `20261020-015441`; test setup `trafficsetup` parks a car 45 m ahead of moving
traffic; `parkfix on|off` switches the fix; `trafficwatch 90 parked` watches the lane for 90 s):

| Spot | Window | Fix | Cars seen in the lane | Stopped behind at the end | Chains ending at the car |
|---|---|---|---|---|---|
| (−101, 378) | 1 | off | 6 | 6 | 3 |
| (−101, 378) | 2 | **on** | 10 | **0** | **0** |
| (−474, −266) | 1 | **on** | 1 | 0 | 0 |
| (−474, −266) | 2 | off | 2 | 1 | 1 |
| (−474, −266) | 3 | **on** | 3 | 0 | 0 |

No car passed through the parked car on its own lane (`ghostPasses=0` in every window): they changed lane.

### 3.2 A borrowed car left stale lane entries

A native car keeps lane entries on its lane-change lane and on lanes its body overlaps; only the game's navigation
system removes them, and it no longer runs for a borrowed car. Borrowing now removes those entries first
(`ActorWriter.ClearExtraLaneRegistrations`). Not separately reproduced in game; `ghosts` found no stale entry for our
vehicles in any reading. Each reading also listed 16–18 entries of native vehicles (not ours) about 50 m from their
lane point; what they are was not investigated.

### 3.3 Loops through the vehicle being driven

The game clears a deadlock only when a chain of "blocked by" links loops back. The player's vehicle never said what it
waited behind, so a loop through it (ours behind car A, A waiting for a crossing car, the crossing car waiting for ours —
the ISSUE-025 case) stood until the player moved. **Fix:** after 5 s stopped behind a vehicle, ours writes that vehicle
as its blocker (`WaitingBehind`, unit-tested); the game's check then sees the loop.

**A/B during a rush-hour shift** (sessions `20261020-020810` and `-023545`, `career abtest on`: after 40 s stood still
in traffic an episode starts, alternately switching the fix on or leaving it off, with the same 360 s window). It
produced one episode before it was stopped:

- Episode 1 (fix on), at (−735, −4412), the bus stopped in a right turn: `blockchain` showed
  `660895@(−737,−4415) v0.0 Crossing/0 > ME` — a car stopped across the bus's front, waiting for the bus, while the bus
  waited for it. It was **not** resolved in 360 s, and the log showed why: the bus still reported nothing
  (`waitingBehind=0`). The choice of the vehicle waited behind skipped vehicles overlapping the bus by more than 1 m,
  and this car's centre was 3 m from the bus's centre. **Defect in the fix, found by this test and corrected**
  (overlapping vehicles count; unit test `ReportsACarStoppedAcrossTheFrontOfABus`, which fails before the change;
  172 tests).
- The shift itself was slow evidence: in the narrow downtown grid the test autopilot failed most tight turns with the
  12.7 m bus (kerb strikes, 20+ recoveries, a loop of 14 recoveries at one junction). Harness steps, each labelled in
  the session notes: the bus was placed on its route 18 m past the junction at (−634, −4243) twice
  (`tpveh -634.0 380.9 -4262.0 174.4`, 02:26 and 02:43). Cause: our vehicles turn about the body centre, so the ends
  of a 12.7 m bus swing about 1.8 m outward in a 10 m turn and hit corner props (recorded as ISSUE-040).

**Controlled junction test** (session `20261020-023545`; test setup: the bus placed mid-junction where episode 1 formed
its loop, `tpveh -735.2 381.0 -4412.3 -125`, standing still; `trafficfix off|on` switches the fix):

| Round | Fix off, 90 s | Fix still off, +30 s (control) | Fix on, +20 s | Fix on, +60 s |
|---|---|---|---|---|
| 1 | car 339548 across the bus's front waiting for it (`Crossing > ME`); 21 chains end at the bus; 15 blocked within 40 m | — | car gone, next car passing at 13 m/s; **0** chains; 1 blocked | 2 chains (new arrivals) |
| 2 | car 666102 the same; 18 chains; 18 blocked | — | **2** chains; traffic moving (a truck passing at 7.6 m/s) | 6 chains |
| 3 | car 659554 the same; 23 chains | the same car still there; 20 chains | car gone, next car passing at 15 m/s; **0** chains | — |

With the fix, the bus reports the car in front, the game's own deadlock check sees the loop and takes that car off
the road, and the queue behind dissolves within 20 s. Without it the same situation stood for the whole window.
The bus itself still blocks the junction while it stands there (new arrivals queue behind it), as any stopped vehicle
does.

### 3.4 Parked vehicle without motion data: rendering and re-boarding

Same session. The bus left parked mid-junction: `trafficwatch 2 parked` → `motionData=False`, no car queued. Screenshots
`20261020-025614-parked-bus-nomotion-a.png` and `-025619-…-b.png` (session `captures`): the bus is drawn normally, on
its wheels, in the same place 5 s apart. Re-boarded: `motionData=True`; on a clear straight it accelerated to 3 m/s in
5 s (7.7 m). [simulated input]

## 4. Final package checks

Package `DriveTheCityII-0.1.0-36ec810-20261020T065957Z` (commit `36ec810`, 172 unit tests passed), installed;
`deploy -Status` OK.

| Check | Result | Evidence |
|---|---|---|
| M1 loop with real keys (`tools\campaign-m1.ps1`) | **Not run.** The city loaded, then the real-input helper could not bring the game window to the front: a Steam window (`steamwebhelper`) held the foreground and Windows refused the switch. The focus protection was not bypassed. | session `20261020-030033`, `campaigns\m1-20261020-030033.md` |
| Career persistence (`tools\campaign-career-persist.ps1`): save A, taxi fare to the passenger, save B while driving, reload B (same stage, no mod entity in the save), finish the fare (one settlement +29.08 cr, rental share −7.27 cr), save and reload C (no second payout), reload A (exact older balance) | **Passed 11/11** [simulated input via test autopilot] | session `20261020-030303`, `campaigns\career-persist-20261020-030923.md` |
| Save with a parked vehicle without motion data, reload | **Passed**: rental parked (`motionData=False`), saved as `DTC2-LAB-CAREER-P`, reloaded: `excluded-signature cars=0 actors=0`; the rental called back and boarded (`motionData=True`) and drove at 4.2 m/s | session `20261020-030303` |

## 5. Not done in this run

- A full bus shift was not completed on the new build (the autopilot's turning in narrow junctions, ISSUE-040).
- M1 with real keys was not run on the final package (see §4).
- The 2-hour soak was not repeated on the new build.
- No human has played with these fixes yet.
- The playtest issues ISSUE-035 to ISSUE-039 are recorded, not fixed.
