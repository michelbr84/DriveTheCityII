# Morning Report — Drive the City II private build

| Field | Value |
|---|---|
| ID | DOC-MORNING-REPORT |
| Date | 2026-10-19 (updated 2026-10-20) |
| Prepared by | Claude Code agent (autonomous run under the owner brief of 2026-10-19; follow-up request of 2026-10-20) |
| Game | Cities: Skylines II 1.6.2f1 (Steam), Windows 11 |
| Final package | `DriveTheCityII-0.1.0-36ec810-20261020T065957Z` (installed 2026-10-20; replaces `0.1.0-af308eb` after the standing-queue and bus turn-round fixes) |

**Update 2026-10-20.** Your own session (00:13–00:55) was read from the mod log: no errors; its problems are recorded as
ISSUE-035 to ISSUE-039 (the main one: parked cars cannot be entered in sandbox mode — 168 key presses failed). The
standing queues (ISSUE-033) had two causes, both fixed and verified in game with each fix switched off and on: a
vehicle of yours parked in the street held traffic indefinitely (a queue grew to 81 cars in four minutes), and a car
stopped across your vehicle's front while each waited for the other was never cleared by the game. Buses no longer
plan U-turns at junctions (ISSUE-034). On the new package, career persistence passed 11/11; M1 with real keys was not
run (a Steam window held the foreground). Details: [RUN-0005](docs/quality/evidence/RUN-0005/REPORT.md).

**Summary.** A private, playable native CS2 mod exists and is installed. You can leave city building, walk, take a
car, drive it through real traffic, take jobs, earn and spend money, and go back to building. Your career is saved
inside each city save. The core loop (M1) passed with real keyboard input, 17 of 17 steps. Taxi, delivery, garage,
home, company, persistence and Portuguese were verified in game, and a 2-hour real-time soak ran without a crash.
Later, a freight job (one of two drops delivered before the deadline) and a whole 14-stop bus shift were completed and
paid in game with simulated input. A challenge to the finish was not completed in game and needs your playtest.
No mod package was published on any platform. (On 2026-10-20 you pushed the source to your GitHub repository; it was
then prepared for public visibility under the MIT License, see the [CHANGELOG](CHANGELOG.md).)

How to read the columns:

- **Implemented**: the code exists in this build.
- **Build verified**: it compiles into the packaged mod.
- **Automated tests verified**: covered by passing unit tests (engine-free Core, 151 tests) or by a scripted in-game
  campaign; the cell says which.
- **In-game verified**: observed in the running game, in a disposable `DTC2-LAB` city. "Real keys" means Windows
  hardware key events sent to the game window. "Simulated" means the test autopilot or bridge commands, which use the
  same input path and game rules.
- **Remaining work**: what is missing or not verified.

## 1. Package and installation

| Item | Value |
|---|---|
| Package folder | `%LOCALAPPDATA%\DriveTheCityII\packages\DriveTheCityII-0.1.0-36ec810-20261020T065957Z\` (`DriveTheCityII.dll`, `.pdb`, `.mjs`); `packages\LATEST.txt` points to it. The earlier packages `0.1.0-af308eb` and `0.1.0-3eaf045` stay in `packages\`; to go back to one, use `deploy.ps1 -Install -Package DriveTheCityII-0.1.0-af308eb-20261020T033944Z` (`-Rollback` would restore the install before this one, which was a test build) |
| Installed to | `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DriveTheCityII\` |
| Ownership manifest | `%LOCALAPPDATA%\DriveTheCityII\deploy\installed.json` (file hashes); earlier installs kept under `deploy\backups\` |
| Install / status / rollback / remove | `tools\deploy.ps1 -Install`, `-Status`, `-Rollback`, `-Uninstall` (run with `powershell -NoProfile -ExecutionPolicy Bypass -File …`) |
| Rebuild from source | `tools\build.ps1` (private .NET SDK under `%LOCALAPPDATA%\DriveTheCityII\tools\dotnet`) |
| Source | This folder, local Git repository (never pushed). The package is commit `36ec810`. The whole bus shift of 2026-10-19 ran on `94004de` (code identical to the previous package `af308eb`). |
| How to play | [START_HERE](START_HERE.md) |

## 2. Feature status

| Feature | Implemented | Build verified | Automated tests verified | In-game verified | Remaining work |
|---|---|---|---|---|---|
| Native mod load, build stamp in log, co-load guard against the original Beta Test Driving Mod | Yes | Yes | — | **Yes**: every session; guard in RUN-0001 | — |
| Character mode on/off (F8 or the top-left button); camera, tools and input taken over and restored | Yes | Yes | State machine unit tests (TR-01..TR-13) | **Yes, real keys** (RUN-0002) | Gamepad not tested |
| Walking and running with animation, ground following, collisions | Yes | Yes | Motion unit tests | **Yes**: real W (4.0 m in 2.5 s); running simulated | — |
| Entering and leaving vehicles; sidewalk-first exits | Yes | Yes | Transition unit tests | **Yes**: real E; 43/43 transition cycles; 3 of 10 mid-road stops exited onto a sidewalk | Enter/exit is a slide, not an animation; some mid-lane exits land on the road (ISSUE-010) |
| Driving: throttle, brake, reverse, steering, handbrake, collisions with buildings, objects and vehicles | Yes | Yes | Vehicle controller unit tests | **Yes, real keys**: 9.1 m/s, stop, 37° turn; impacts stop the car | Driving feel needs your judgement |
| Cameras: chase, hood, third and first person, mouse look | Yes | Yes | — | **Yes** (screenshots) | — |
| City traffic reacts to the player's car | Yes | Yes | — | **Yes**: 3–5 AI cars queued behind, 0 drove through | AI does not overtake the player |
| Recovery (Backspace), "you seem stuck" hint, recovery-loop protection | Yes | Yes | Unit tests | **Yes** | — |
| Save safety: no mod entities in saves, save while driving, relaunch and reload, save loads without the mod | Yes | Yes | Envelope, sidecar and quarantine unit tests | **Yes**: leak check 0; RUN-0002; mod-less load (RUN-0004) | Corrupted career data inside a real save: unit-tested only |
| Career persistence and exactly-once money | Yes | Yes | Persistence and ledger unit tests | **Yes**: mid-fare save and reload resumes the same stage; no double payout after reload; an older save restores its exact balance; a double-click refuel was refused | — |
| Taxi career | Yes | Yes | 18 unit tests | **Yes (simulated driving)**: fares completed (49.01 cr and 29.13 cr), patience failures handled | Street hails (taxi duty) are stored but not generated |
| Delivery career | Yes | Yes | 12 unit tests (with freight) | **Yes (simulated)**: load, drop-off, parcel destroyed by an impact, partial pay | — |
| Freight career (rigid truck) | Yes | Yes | Unit tests | **Yes (simulated)**: truck provided at a clear spot, cargo loaded, delivered at the first drop; job settled as completed, +32.15 cr, +20 XP | Delivered 1 of 2 drops before the hard deadline; a full-length heavy job not finished yet (ISSUE-017) |
| Bus career (real city line) | Yes | Yes | 8 unit tests | **Yes (simulated)**: a whole Line 1 shift, 14 of 14 stops served, settled as completed, +222.50 cr, +47 XP | Two labelled harness steps in that run; standing queues near the depot (ISSUE-033); guidance may ask a bus to turn in a narrow street (ISSUE-034) |
| Garage: basic rental, rent, buy, sell, refuel, repair, upgrades, roadside help, tow | Yes | Yes | 13 unit tests | **Rental, buy, sell, refuel, repair: yes.** Upgrades, roadside help and tow: not run in game | Try upgrades and roadside help |
| Progression: XP per career, levels, reputation, unlocks | Yes | Yes | Unit tests | XP and reputation changes seen; unlocks exercised with test seeding | Balance needs playtesting |
| Home and service points (CS2 has no fuel stations) | Yes | Yes | Unit tests | **Yes**: set home, go home, service point markers and actions | — |
| Company (managed, simulated operations) | Yes | Yes | 7 unit tests | **Yes**: founded, hired, fleet truck bought, contract accepted; 12 period settlements during the 2-hour soak | Company balance needs playtesting (it lost money in the soak) |
| Challenges (time trial, checkpoints, parking) | Yes | Yes | Unit tests | **Partly**: generated, armed, markers, route and HUD card drawn | A finished run with a medal was not seen in game |
| City changes (demolished places, cut roads, changed lines) | Yes | Yes | Unit tests | Not run in game | Try demolishing a job destination mid-job |
| HUD, Tab panel, onboarding help | Yes | Yes | — | **Yes** (screenshots) | — |
| Navigation: target markers and route line | Yes | Yes | — | **Yes** | No minimap |
| Options: 12 remappable keys, mouse, field of view, units, HUD scale, high contrast, language, vehicle sound, safe-disable | Yes | Yes | — | Settings load; scale, contrast and language verified | Remapping a key in Options was not exercised |
| English and Brazilian Portuguese | Yes | Yes | 8 localization unit tests | **Yes**: Portuguese HUD and panel | 48 of 222 career messages stay English (their values are not passed separately) |
| Vehicle sound (procedural engine, rolling noise, impact) | Yes | Yes | — | **Measured**: plays through the game's mixer while driving, silent on foot | Nobody has listened to it; no footstep sound |
| Gamepad | Bindings only | Yes | — | Not run (no device) | Test with a controller |
| Performance and stability | — | — | 2-hour real-time soak | **Yes**: 30 fps average over 2 hours (p95 41 ms), no crash; job board 55–80 ms | 50–130 ms hitches when the road graph rebuilds (ISSUE-009); whole-game memory grew about 1 GB without a mod-off comparison |
| Packaging, install, rollback | Yes | Yes | — | **Yes**: used for every build | — |

## 3. Test evidence

| Evidence | Result |
|---|---|
| Unit tests (`tests\DriveTheCityII.Core.Tests`) | **172 passed, 0 failed** (final package build) |
| [RUN-0001](docs/quality/evidence/RUN-0001/REPORT.md) upstream baseline | Passed (load, takeover, drive, handback; co-load guard) |
| [RUN-0002](docs/quality/evidence/RUN-0002/REPORT.md) M1 loop, real keys | **17/17**, repeated **17/17** on `0.1.0-3eaf045`; on `0.1.0-af308eb`: 16/17 then **17/17** (the miss was a key-timing fault of the test script). **Not run on the final package `0.1.0-36ec810`**: a Steam window held the foreground and the focus was not forced |
| [RUN-0003](docs/quality/evidence/RUN-0003/REPORT.md) careers in game | Taxi, persistence, garage, delivery, home, company: passed; **freight and a whole bus shift completed and paid** (§4, simulated input); challenges: partly |
| [RUN-0004](docs/quality/evidence/RUN-0004/REPORT.md) presentation, mod-less load, soak | Presentation and mod-less load passed; **2-hour real-time soak passed for stability**: no crash, 30 fps average, 5 saves and 2 reloads without failure, leak check 0, managed heap steady (whole-game memory +1 GB, flattening; no mod-off comparison) |
| [RUN-0005](docs/quality/evidence/RUN-0005/REPORT.md) standing queues, bus turn-rounds, your first session | ISSUE-033 fixed: parked vehicle off/on 6 and 1 → 0 cars stopped behind; junction deadlock 18–23 → 0–2 queued 20 s after the fix (control: still 20 after 30 s). ISSUE-034 fixed: 426 m route with a junction U-turn → 433 m round the block. Final package: career persistence **11/11**, save/reload with a parked vehicle passed |
| Transition cycles | 20/20, 10/10, 3/3, 10/10, 0 failures |
| [WORKLOG](docs/quality/evidence/WORKLOG.md) | Timeline with session folders |
| Raw evidence | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\` (logs, screenshots) and `test\campaigns\` (reports) |

## 4. Limitations and known issues

Full list: [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md). The ones you will notice:

- In sandbox mode only cars waiting in traffic (at a light, in a queue) can be entered, not parked cars (ISSUE-035).
  Use the free basic rental (Tab → Garage) instead.
- Freight and a whole bus shift were completed in game once each, by the test autopilot (ISSUE-017). The standing
  queues around your vehicle (ISSUE-033, ISSUE-025) are fixed as of 2026-10-20; a vehicle you leave in the street is
  now passed by traffic, which changes lane round it. Stopping in the middle of a junction still holds the cross
  traffic while you stand there.
- A bus is hard to steer through narrow downtown junctions and clips corner poles (ISSUE-040).
- Pressing Backspace three times within a minute on foot returns you to city building (ISSUE-036); the garage option
  "Accept a starter taxi or courier job" does nothing (ISSUE-037) — use the Jobs tab.
- Getting in and out of a car is a short slide, not an animation. No footstep sound.
- When you stop in the middle of a wide road, you may step out onto the road (ISSUE-010). Park at the curb.
- Rare 50–130 ms stutters when the road graph rebuilds (ISSUE-009).
- The career pauses while the game window is not focused (ISSUE-011, by design).
- After a failed freight or bus job you are still in the released employer vehicle; get out and bring your own car
  (Tab, Garage, "Bring here") before the next job or challenge (ISSUE-018).
- Over the 2-hour soak the whole game's memory grew by about 1 GB and was flattening out; a comparison without the mod was
  not run, so the cause is unknown.
- Gamepad untested. Remapping keys in Options untested.
- Some career messages stay English in Portuguese mode (values embedded in the English text).
- The lab cities used for testing are **test-seeded** (money, XP and reputation were granted). Start a real career in
  your own city for fair progression.

## 5. Actions for you

1. **Play it** (see START_HERE): take a taxi fare, then try a delivery. Freight unlocks at Delivery level 3 (or total
   level 6); bus shifts at Taxi level 3 (or reputation 60). Judge the fun, the driving feel and the sound; that is the
   M2 "fun checkpoint".
2. **Try what the automation could not finish**: a challenge to the finish, a full-length heavy freight run, and key
   remapping in Options. Drive a bus line yourself to judge it. If you have a gamepad, try it.
   **Question (ISSUE-039):** does your own city have bus lines drawn with the transport tool? The mod found none there,
   so the bus career had no offers in it.
3. **Optional cleanup**: the test saves are named `DTC2-LAB-*` in the game's save list; delete them when you no longer
   need them. The real-time soak and test sessions never touched your own saves; the Continue pointer was restored
   after every session.
4. **Decisions still yours**: whether the project folder should stay under OneDrive (RISK-011); and whether anything
   should ever be published. ADR-0005 keeps it private, and nothing was uploaded to Melty, Paradox Mods or anywhere
   else.
