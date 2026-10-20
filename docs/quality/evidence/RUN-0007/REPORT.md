# RUN-0007 — Parked cars (ISSUE-035), small props for long vehicles (ISSUE-040), borrowed cars in saves (ISSUE-041), invisible garage vehicles (ISSUE-043); release 0.2.0

| Field | Value |
|---|---|
| Run ID | RUN-0007 |
| Phases | PHASE-03 to PHASE-05 (driving, sandbox), PHASE-07 (bus career), PHASE-12 (validation) |
| Date | 2026-10-20 |
| Executed by | Claude Code agent (owner request of 2026-10-20: fix ISSUE-035 and ISSUE-040 and release a new version); no human reviewer yet |
| Input | **Simulated**: test-bridge commands (`input`, `player interact/exit`) and the TEST AUTOPILOT (`career drive`); career rules unchanged. Test setup steps (`tpveh`, `tpav`, `gotoparked`, `spawncar`, `softprops`) are labelled. No real keys. |
| Builds | `0.1.0-b3e4beb+dirty-20261020T163238Z` (§1, §3.1), `…T164847Z` (first save test), `…T165650Z` (§2, depot start), `0.2.0-b3e4beb+dirty-…T171214Z` (§3.3 A, B), `0.2.0-be8703c+dirty-…T174503Z` (§3.3 C, §3.4); release `0.2.0-2994688-20261020T180952Z` (§4) |
| Sessions | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261020-123308`, `-124859`, `-125702`, `-131240`, `-134529`, `-141036` (release) |
| Lab cities | `DTC2-LAB-Bench-S1`, `DTC2-LAB-CAREERS-H`; saves written by this run: `DTC2-LAB-PARKFIX-A`, `-B`, `-C`, `DTC2-LAB-BUSSHIFT-A` |

## 1. ISSUE-035 — entering parked cars (sandbox)

New diagnostics: `parked [radius]` (parked native cars near the avatar and whether sandbox can borrow them),
`gotoparked [radius]` (test setup: avatar beside the nearest borrowable one; it now checks that the avatar really moved),
`carinfo index:version` and `cars [radius] [x z]` (a car's state; entity numbers change on load, so cars are found again
by model and place).

| Step | Result |
|---|---|
| `parked 80` in the lab city | 3 parked cars: two in garages (invisible) refused, one in a street parking space borrowable |
| Avatar beside it (`gotoparked`), `player interact` | **Passed**: Driving after 2.5 s. Before: `parked(lane=957327) stopped`; after: `moving tripSource dtcActor`, `CarCurrentLane` flags EndReached, ParkingSpace, FixedLane |
| Drive (`input throttle=0.5 t=3`), brake to reverse | **Passed**: 3.9 m/s forward, then reverse engaged; the car renders on the road (capture `20261020-123532-parked-borrow-driving.png`) |
| Stop, `player exit` | **Passed**: handed back (no exclusion, no tag); **3 s later the game parked it** in the same parking lane; `leakcheck` 0 |
| Borrow again, drive 90 m, leave it on the road | **Passed**: 4 s later parked on the kerbside parking lane where it stood (capture `20261020-123711-parked-left-on-road.png`); `leakcheck` 0 |

## 2. ISSUE-041 — a borrowed car saved while driven

| Step | Result |
|---|---|
| Old behaviour: save `DTC2-LAB-PARKFIX-B` while driving the borrowed car, reload | **Defect found**: the car (`Car08`) stood at the same place as `moving+tripSource` 10 s and 90 s after the load, simulation running at speed 1 with focus. Cause in the game code: `VehicleSpawnSystem` never releases a vehicle whose `TripSource` has no source. `leakcheck` reported 0 because it only counted the full-length timer |
| Fix, repair on load: reload `DTC2-LAB-PARKFIX-B` | **Passed**: log "Load: 1 city car(s) left out of the traffic AI by an earlier save were handed back (ISSUE-041)"; `Car08` parked on a parking lane 4.7 m away; `leakcheck` 0 |
| Fix, new save while driving (`DTC2-LAB-PARKFIX-C`) | **Passed**: log "1 borrowed car(s) saved as city traffic and taken back"; the player kept driving it after the save (3.8 m further, still `tripSource+dtcActor`) |
| Reload `DTC2-LAB-PARKFIX-C` | **Passed**: `Car08` an ordinary parked car on a parking lane; `leakcheck` 0 (new check: any source-less exclusion without a mod tag); no repair needed |

## 3. ISSUE-040 — small props and long vehicles

### 3.1 Same pole, switch off and on (`softprops off|on`, bus `Bus01` 12.7 m, `spawncar bus`)

Target: a street lamp, `SmallProp` 0.30 × 0.30 m, 5 m tall, on the kerb at (−130.7, −1080.3). The bus is placed
10.35 m before it, facing it (`tpveh -134.15 442.3 -1070.54 160.6`), then `input throttle=0.8 t=5`.

| Round | Result |
|---|---|
| Bus, switch **off** | **Stopped by the lamp**: front bumper at (−130.8, −1080.2) |
| Bus, switch **on** | **Drove past the lamp** by 3.5 m and stopped at the house behind it ("Impact 3.0 m/s with Building") |
| Car (borrowed pickup, 6.0 m), switch on | **Stopped by the lamp**: "Impact 3.5 m/s with SmallProp" |

An earlier round at a 1 m prop on the edge of a house lot gave "Impact 2.1 m/s with SmallProp" (off) and "Impact 2.2 m/s
with Building" (on): the prop and the lot's mesh overlap there. A 0.3 m, 1.2 m tall post did not stop the bus either
way, so it was not used for the comparison.

### 3.2 Why not a rear-axle pivot

Turning about the rear axle is how a real bus moves, but for the same path it sweeps a wider area (a 12.7 m bus in a
10 m turn: about 5.7 m against 4.2 m for the centre pivot). The stuck bus of RUN-0005 was touching posts and poles
0.2–0.3 m wide at the corner (`stepdiag`: ground supported, contact with a static object), objects the game's own
buses sweep over when they follow the same lanes. The vehicle model is therefore unchanged.

### 3.3 Whole bus shift with the test autopilot (`DTC2-LAB-CAREERS-H`, Line 1, 14 stops, switch on)

| Part | Build | Start | Result |
|---|---|---|---|
| Depot start | `…T165650Z` | the shift's own start | Stuck at the depot turnaround (−270, −4565) between parked cars, 4 recoveries in about 5 minutes, still on the first leg: the same place and behaviour as every RUN-0005 run (first recovery at 68 s there, at 63 s here). Aborted; recorded as ISSUE-042 |
| Junction A/B | `…T165650Z` | `tpveh -634.567 381.02 -4243.96 -155` (the RUN-0005 stuck spot) | Switch off and on, 90 s each: 2 recoveries each. The bus was held by a `Motorbike01` parked at the kerb beside the stop (`props` with model and owner); a parked vehicle stays solid by design |
| A | `…T171214Z` | `tpveh -634.0 380.9 -4262.0 174.4` (the RUN-0005 harness pose) | 15 minutes: stops 1–3 served (183 s, 257 s, 307 s), 2 recoveries, no impact |
| B | same | continuation | Stops 4–11 served, 4 recoveries; contacts: another bus crossing (4.1 m/s) and an invisible garage motorbike (12.2 m/s, then three recoveries in a row at the same place: ISSUE-043). Aborted; city saved as `DTC2-LAB-BUSSHIFT-A` (leg 11) |
| C | `…T174503Z` (ISSUE-043 fixed) | reload of `DTC2-LAB-BUSSHIFT-A` | Stops 12–14 served, 3 recoveries (queue, deadlock with a stopped vehicle, one wheel off the road on a bend); **"Job completed. Settled: 225.60 cr, +52 XP"** |

No contact with a `SmallProp` in the whole shift. In RUN-0005 both runs from the harness pose stayed on the first
leg for 15 minutes (3 and 4 recoveries) and no stop of this line was ever served by the autopilot.

### 3.4 ISSUE-043 — invisible garage vehicles

At (181, −1172) `cars` listed `Motorbike01 parked:garage+unspawned` at 443.8–445.0 m, 1.6–2.8 m above the road; the
autopilot log reported "Impact 12.2 m/s with StaticObject … size=0.6x2.2". After the fix (unspawned objects ignored),
`props 6` at the same place: 0 static objects (only the street lamp, a small prop), and part C drove past it.

## 4. Release checks (package `DriveTheCityII-0.2.0-2994688-20261020T180952Z`)

| Check | Result |
|---|---|
| Build in a fresh clone of commit `2994688` (tag `v0.2.0`) | **Passed**: 176 unit tests, 0 errors |
| Archive | `DriveTheCityII-0.2.0.zip`, SHA-256 `a6bc27d89e41e07a5e035b0d77972a501c4ae3e50d8d5a1425909fce9912a41d` |
| Installed by hand-extraction, test session | **Passed**: `bridge online: mod 0.2.0+2994688`, session `20261020-141036` |
| Career persistence campaign | **Passed 11/11** (`career-persist-20261020-141734.md`) |
| Parked vehicle borrowed on the release build | **Passed**: a parked motorbike ridden 3.9 m/s and left; parked again by the game; `leakcheck` 0 |
| GitHub release `v0.2.0` | **Passed**: API shows one asset with the same digest, `releases/latest` → `v0.2.0`; anonymous download identical to the package |
| Paradox Mods `NewVersion` | Two failed attempts ("Game is not found", then an SSL connection error), the third identical attempt "New mod version published"; the page then said "pending publication" |

## 5. Limits of this run

- Simulated input only; no real keys and no gamepad.
- A building car park (connection lane) was not tried for ISSUE-035.
- The bus shift needed test setup: a start past the depot (ISSUE-042) and one reload after the ISSUE-043 fix.
- Installing the Paradox Mods copy in game (subscription) is still **Not run**.
