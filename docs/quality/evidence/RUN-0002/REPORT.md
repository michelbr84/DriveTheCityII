# RUN-0002 — M1 playable loop with real keyboard input

| Field | Value |
|---|---|
| Run ID | RUN-0002 |
| Milestone | M1 ([ROADMAP](../../../../ROADMAP.md) §3.1, [PHASE-05](../../../planning/phases/PHASE-05-walk-drive-integration.md) §13) |
| Date | 2026-10-19 |
| Executed by | Claude Code agent (owner-authorized autonomous run of 2026-10-19); no human reviewer yet |
| Result | **Passed: 17 of 17 steps** (second campaign run; the first run's 2 failures were measurement errors in the script, see Observations). **Repeated on the final package `0.1.0-3eaf045`: 17 of 17** (`campaigns\m1-20261019-060703.md`: 9.1 m/s, stop, 36° turn, 0 impacts, save, quit, relaunch, reload). |

## Configuration

| Item | Value |
|---|---|
| Game | Cities: Skylines II 1.6.2f1 (Steam), Unity 2022.3.71f1, Mono |
| Machine | Windows 11 Pro 10.0.26300, RTX 5090 Laptop GPU, 3840x2400 |
| Mod build | Drive the City II `0.1.0-08ceb62+dirty-20261019T075032Z` (career code included; committed as `46e8b9d`) |
| Mod set | Drive the City II only, in `<user data>\Mods\DriveTheCityII` |
| City | `DTC2-LAB-Bench-S1` (lab copy of the game's built-in benchmark city); save target `DTC2-LAB-M1` |
| Input | **Real keyboard input**: Windows `SendInput` hardware scan codes sent to the verified foreground game window (`tools\realinput.ps1`). The test bridge only reads state, measures, and performs setup (load, rent a car, place the avatar beside it). |
| Script | `tools\campaign-m1.ps1` |
| Sessions | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261019-035050` (play, save, quit) and `...\20261019-035254` (relaunch and reload) |
| Report | `%LOCALAPPDATA%\DriveTheCityII\test\campaigns\m1-20261019-035050.md` |

## Steps and results

| # | Step | Input | Result |
|---|---|---|---|
| 1 | Load the disposable lab city | bridge `load` | **Passed** (`city='DTC2-LAB-Bench' lab=True`, 33 fps) |
| 2 | Enter character mode | real **F8** | **Passed** (`player=OnFoot`) |
| 3 | Walk 2.5 s | real **W** | **Passed**: 4.0 m (walk speed 1.6 m/s) |
| 4 | Rent a vehicle | Career panel action `garage:rentbasic` via the bridge | Basic rental `V-R1` (car.basic) placed at the curb |
| 5 | Enter the vehicle | real **E** | **Passed** (`player=Driving`) |
| 6 | Accelerate 3 s | real **W** | **Passed**: peak 9.1 m/s, 20.9 m, 0 impacts (in-game `track` measurement while the key was held) |
| 7 | Brake to a stop | real **S** | **Passed**: stopped about 2 s after S was pressed; holding S afterwards engaged reverse (designed behavior) |
| 8 | Steer while moving | real **D** then **A** with **W** held | **Passed**: heading change 37°, peak 7.5 m/s, 0 impacts |
| 9 | Stop | real **Space** (handbrake) | **Passed**: 0.00 m/s |
| 10 | Park and exit | real **Space**, **E** | **Passed**: avatar on the sidewalk (`aGround=Pedestrian`) |
| 11 | Walk after exit | real **W** | **Passed** |
| 12 | Return to city management | real **F8** | **Passed**: `player=Building`, `actors=0` (all mod entities removed) |
| 13 | Save | bridge `save DTC2-LAB-M1` | **Passed**; career envelope written (`revision=1`, 1,770 bytes) |
| 14 | Close the game | bridge `quit` | **Passed**: process exited; test-session state files restored |
| 15 | Relaunch and reload the save | new session, bridge `load DTC2-LAB-M1` | **Passed**: `Persistence: load status=Ok ... restored revision 1`; `Career: restored (clock 48s, balance 30000 cents)`; the rented car `V-R1` is still owned |
| 16 | No mod entity persisted in the save | bridge `leakcheck` | **Passed**: `excluded-signature cars=0 actors=0` |
| 17 | Continue safely, then return | real **F8** twice | **Passed**: `OnFoot`, then `Building` |

## Observations

- **First campaign (session 20261019-034508): 13 of 15 steps passed.** The two failures were in the script, not in
  the mod:
  - speed was read about 1.5 s after the keys were released;
  - the scripted right turn at speed drove the car onto the sidewalk and into a building at 7.6 m/s. The mod stopped
    the car (`Impact 7.6 m/s with Building`).
  - The script now measures inside the game while keys are held (`track` op), and the steering step uses a gentler turn.
- Holding **S** after the car has stopped engages reverse after 0.35 s, as designed (SPEC driving controls). Players
  release S to stay stopped, or use Space.
- The save and reload preserved career data across a full process restart (M1 persistence requirement); mod actors
  were never written into the city save.
- Not covered by this run: gamepad (no device), audio (none implemented), night lighting, low-FPS conditions. See
  [KNOWN_ISSUES](../../KNOWN_ISSUES.md).
