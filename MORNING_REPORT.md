# Morning Report — Drive the City II private build

| Field | Value |
|---|---|
| ID | DOC-MORNING-REPORT |
| Date | 2026-10-19 |
| Prepared by | Claude Code agent (autonomous run under the owner brief of 2026-10-19) |
| Game | Cities: Skylines II 1.6.2f1 (Steam), Windows 11 |
| Final package | `DriveTheCityII-0.1.0-3b7980c-20261019T094752Z` (installed) |

**Summary.** A private, playable native CS2 mod exists and is installed. You can leave city building, walk, take a
car, drive it through real traffic, take jobs, earn and spend money, and go back to building. Your career is saved
inside each city save. The core loop (M1) passed with real keyboard input, 17 of 17 steps. Taxi, delivery, garage,
home, company setup, persistence and Portuguese were verified in game. Freight, bus and challenge runs started in game
but were not completed there. They are covered by unit tests and need your playtest. Nothing was published or uploaded.

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
| Package folder | `%LOCALAPPDATA%\DriveTheCityII\packages\DriveTheCityII-0.1.0-3b7980c-20261019T094752Z\` (`DriveTheCityII.dll`, `.pdb`, `.mjs`); `packages\LATEST.txt` points to it |
| Installed to | `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DriveTheCityII\` |
| Ownership manifest | `%LOCALAPPDATA%\DriveTheCityII\deploy\installed.json` (file hashes); earlier installs kept under `deploy\backups\` |
| Install / status / rollback / remove | `tools\deploy.ps1 -Install`, `-Status`, `-Rollback`, `-Uninstall` (run with `powershell -NoProfile -ExecutionPolicy Bypass -File …`) |
| Rebuild from source | `tools\build.ps1` (private .NET SDK under `%LOCALAPPDATA%\DriveTheCityII\tools\dotnet`) |
| Source | This folder, local Git repository (never pushed). The final code is commit `3b7980c`; later commits only change documentation. |
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
| Freight career (rigid truck) | Yes | Yes | Unit tests | **Partly**: the shipper's truck is provided and boarded; cargo loads; the load is lost on a crash | A completed freight delivery was not seen in game (ISSUE-017) |
| Bus career (real city line) | Yes | Yes | 8 unit tests | **Partly**: line job accepted; the operator's bus is provided and boarded | Served stops were not seen in game (ISSUE-017) |
| Garage: basic rental, rent, buy, sell, refuel, repair, upgrades, roadside help, tow | Yes | Yes | 13 unit tests | **Rental, buy, sell, refuel, repair: yes.** Upgrades, roadside help and tow: not run in game | Try upgrades and roadside help |
| Progression: XP per career, levels, reputation, unlocks | Yes | Yes | Unit tests | XP and reputation changes seen; unlocks exercised with test seeding | Balance needs playtesting |
| Home and service points (CS2 has no fuel stations) | Yes | Yes | Unit tests | **Yes**: set home, go home, service point markers and actions | — |
| Company (managed, simulated operations) | Yes | Yes | 7 unit tests | **Setup yes**: founded, hired, fleet truck bought, contract accepted. Period settlements: see the soak (§3) | — |
| Challenges (time trial, checkpoints, parking) | Yes | Yes | Unit tests | **Partly**: generated, armed, markers, route and HUD card drawn | A finished run with a medal was not seen in game |
| City changes (demolished places, cut roads, changed lines) | Yes | Yes | Unit tests | Not run in game | Try demolishing a job destination mid-job |
| HUD, Tab panel, onboarding help | Yes | Yes | — | **Yes** (screenshots) | — |
| Navigation: target markers and route line | Yes | Yes | — | **Yes** | No minimap |
| Options: 12 remappable keys, mouse, field of view, units, HUD scale, high contrast, language, vehicle sound, safe-disable | Yes | Yes | — | Settings load; scale, contrast and language verified | Remapping a key in Options was not exercised |
| English and Brazilian Portuguese | Yes | Yes | 8 localization unit tests | **Yes**: Portuguese HUD and panel | 48 of 222 career messages stay English (their values are not passed separately) |
| Vehicle sound (procedural engine, rolling noise, impact) | Yes | Yes | — | **Measured**: plays through the game's mixer while driving, silent on foot | Nobody has listened to it; no footstep sound |
| Gamepad | Bindings only | Yes | — | Not run (no device) | Test with a controller |
| Performance | — | — | — | 30–37 fps in the benchmark city; job board 55–80 ms; soak results in §3 | 50–130 ms hitches when the road graph rebuilds (ISSUE-009) |
| Packaging, install, rollback | Yes | Yes | — | **Yes**: used for every build | — |

## 3. Test evidence

| Evidence | Result |
|---|---|
| Unit tests (`tests\DriveTheCityII.Core.Tests`) | **151 passed, 0 failed** (final package build) |
| [RUN-0001](docs/quality/evidence/RUN-0001/REPORT.md) upstream baseline | Passed (load, takeover, drive, handback; co-load guard) |
| [RUN-0002](docs/quality/evidence/RUN-0002/REPORT.md) M1 loop, real keys | **17/17** |
| [RUN-0003](docs/quality/evidence/RUN-0003/REPORT.md) careers in game | Taxi, persistence, garage, delivery, home, company: passed; freight, bus, challenges: partly |
| [RUN-0004](docs/quality/evidence/RUN-0004/REPORT.md) presentation, mod-less load, soak | Presentation and mod-less load passed; soak: SOAK_RESULT |
| Transition cycles | 20/20, 10/10, 3/3, 10/10, 0 failures |
| [WORKLOG](docs/quality/evidence/WORKLOG.md) | Timeline with session folders |
| Raw evidence | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\` (logs, screenshots) and `test\campaigns\` (reports) |

## 4. Limitations and known issues

Full list: [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md). The ones you will notice:

- Freight and bus jobs work up to driving the provided truck or bus. A full run was not completed in game (ISSUE-017).
- Getting in and out of a car is a short slide, not an animation. No footstep sound.
- When you stop in the middle of a wide road, you may step out onto the road (ISSUE-010). Park at the curb.
- Rare 50–130 ms stutters when the road graph rebuilds (ISSUE-009).
- The career pauses while the game window is not focused (ISSUE-011, by design).
- Gamepad untested. Remapping keys in Options untested.
- Some career messages stay English in Portuguese mode (values embedded in the English text).
- The lab cities used for testing are **test-seeded** (money, XP and reputation were granted). Start a real career in
  your own city for fair progression.

## 5. Actions for you

1. **Play it** (see START_HERE): take a taxi fare, then try a delivery. Freight unlocks at Delivery level 3 (or total
   level 6); bus shifts at Taxi level 3 (or reputation 60). Judge the fun, the driving feel and the sound; that is the
   M2 "fun checkpoint".
2. **Try what the automation could not finish**: a full freight run, a bus line, a challenge to the finish, and key
   remapping in Options. If you have a gamepad, try it.
3. **Optional cleanup**: the test saves are named `DTC2-LAB-*` in the game's save list; delete them when you no longer
   need them. The real-time soak and test sessions never touched your own saves; the Continue pointer was restored
   after every session.
4. **Decisions still yours**: whether the project folder should stay under OneDrive (RISK-011); and whether anything
   should ever be published. ADR-0005 keeps it private, and nothing was uploaded to Melty, Paradox Mods or anywhere
   else.
