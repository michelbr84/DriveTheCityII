# Implementation Work Log

| Field | Value |
|---|---|
| ID | DOC-WORKLOG |
| Document status | Draft (living log) |
| Responsible role | Coordinator / Implementer |
| Last updated | 2026-10-19 |

Chronological, factual log of the implementation run authorized by the owner on 2026-10-19 (owner brief
"Autonomous implementation, Melty MCP, and private playable delivery"). Evidence folders live outside the project
under `%LOCALAPPDATA%\DriveTheCityII\test\sessions\<id>\` (bridge log, game logs, screenshots). Labels follow
[DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md). "Simulated input" means the test bridge injected values into our
controller; "real input" means Windows SendInput keyboard events delivered to the focused game window.

## Recovery instructions (read first after a context reset)

1. Workspace: `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII` (Git repository, branch `main`, local only).
2. Toolchain: private .NET SDK 10.0.402 at `%LOCALAPPDATA%\DriveTheCityII\tools\dotnet`; dot-source `tools\env.ps1`.
3. Build + tests + package: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\build.ps1`.
4. Install into the game: `tools\deploy.ps1 -Install` (ownership manifest, `-Rollback`, `-Uninstall`, `-Status`).
5. Test session: `tools\test-session.ps1 -Start` / `-Stop` (backs up and restores UserState.coc and
   continue_game.json; enables the bridge; autosave disabled for the session only).
6. Drive the game: `tools\bridge.ps1 -Commands "cmd1;cmd2"`; real keys: `tools\realinput.ps1 -Steps "focus;hold W 1000"`.
7. Lab city: the game's built-in benchmark city loaded with `benchcity` and saved as `DTC2-LAB-Bench-S1` (and `-S2`).
8. Game API research notes (not in the project): `%LOCALAPPDATA%\DriveTheCityII\refcache\notes\01..05-*.md`.

## Log

| When (2026-10-19) | What happened | Evidence |
|---|---|---|
| 01:20 | Workspace verified; Git initialized locally (existing global identity kept); baseline commit `09e80ac`. | git log |
| 01:25 | Melty: one focused re-check (`game_info`, `modding_kit`): unchanged, BepInEx-only, 0 CS2 mashups. `consult_melty` not called (it would upload the full conversation). | MELTY_EVALUATION §9 update pending |
| 01:30 | .NET SDK 10.0.402 installed user-scoped from the official zip (SHA-512 verified); ilspycmd 11.1 installed as a scoped tool; game assemblies decompiled into the local reference cache. | `%LOCALAPPDATA%\DriveTheCityII\tools`, `refcache` |
| 01:35 | Base mod cloned outside the project at pin `eefd6bb`; **builds with 0 warnings/0 errors against 1.6.2f1** [Built locally]. Not run in game yet. | build output `build\upstream-baseline` |
| 01:51 | First launch failed: launching `Cities2.exe` directly left the Steam platform uninitialized (fatal in AssetDatabase). Fixed by passing `SteamAppId`/`SteamGameId` to the child process only. | session 20261019-015116 |
| 01:55 | Mod loads on 1.6.2f1; bridge online; main menu detected. [Tested in-game] | session 20261019-015339 |
| 01:59 | Built-in benchmark city loaded as lab city `DTC2-LAB-Bench`, saved, returned to menu, reloaded. [Tested in-game] | session 20261019-015807 |
| 02:15 | Character mode: avatar spawned on a sidewalk, walk 1.6 m/s, run 4.6 m/s, stop; career car spawned, entered, driven (0→11 m/s in 4 s), braked, reversed, exited; returned to Building with camera, input, tools restored and 0 mod entities left. Simulated input. [Tested in-game] | session 20261019-021355 (screenshots) |
| 02:17 | **Defect:** game process died after saving while driving. Cause analysis: `Deleted` added to mod actors inside the save bypassed the game's reference cleanup. Fixed: actors tagged `Temp` only during serialization and restored afterwards. | session 20261019-021355 logs |
| 02:31 | **Defect:** game process died ~1 s after spawning a taxi model. Cause (code-inspected): `TransportPathfindSetup` reads the `Owner` of every taxi without a check; mod vehicles have no owner. Fixed: mod career vehicles are role-neutral (service-role components stripped). | session 20261019-022932 |
| 02:39 | Save while driving: actor excluded from the saved city and restored; no crash; reload clean; leak check after reload = 0 mod cars in the save. [Tested in-game] | sessions 20261019-023754, -024201 |
| 02:44 | **Real keyboard input** verified: S reverse, Space handbrake, E exit (sidewalk side), W walk, F8 return to Building. [Tested in-game, real input] | session 20261019-024201 |
| 02:52 | HUD (React UI module) renders in Building, on foot and driving (status card, prompts, speed, gear); Tab panel opens. [Tested in-game, screenshots] | session 20261019-025104 (`hud-*.png`) |
| 02:56 | **Traffic presence:** player's car registered in lane object buffers. On the busiest lane, 3 then 5 AI cars stopped behind the player's stopped car; `trafficwatch` 45 s with a car ahead of moving traffic: 3 stopped behind, **0 pass-throughs**, min gap 1.7 m. [Tested in-game, simulated input] | sessions 20261019-025451, -025728, -030051 |
| 03:10 | Place index: 54,088 lanes, 5,802 places (50 service points, 4 depots, 14 transit stops), 1 bus line. Road router: 2.3 km route in 12 ms (first) / 2 ms (cached). Route marker drawn. Lab save `DTC2-LAB-Bench-S3` written with the persistence envelope; reload restored it ("restored revision 1"). [Tested in-game] | sessions 20261019-030929, -031333 |
| 03:17 | **TASK-0003 / RUN-0001:** upstream base mod built at the pin and run in a lab session on 1.6.2f1; Drive the City II's co-load guard kept it inactive ("Another vehicle-control mod is active"). | [RUN-0001](RUN-0001/REPORT.md), session 20261019-031603 |
| 03:25 | **Defect (performance):** 20 transition cycles passed (20/20) but at **13 fps**; the road graph was rebuilt 2,589 times because a city-revision signal changed every frame. Fixed: rebuild only when the lane count changes or after 2 minutes. | session 20261019-032125 |
| 03:30 | After the fix: Building 31 fps, walking 37 fps, transition cycles 10/10 and 3/3 at 30 fps, **0 failures**, actors 2 → 2 (no leak), managed heap +6 MB over 32 s. [Tested in-game, simulated input via test autopilot] | session 20261019-032643 |
| 03:33 | First-person and hood views; Esc opens the game's pause menu while in character mode. [Tested in-game, screenshots] | session 20261019-033233 |
| 03:46 | **M1 campaign #1 (real keys):** 13/15 steps passed, including save, quit, relaunch, reload, and continue. The 2 failures were measurement errors in the script (speed read after the key was released; the scripted right turn drove the car into a building at 7.6 m/s, and the impact was handled correctly). Script fixed; `track` bridge op added to measure while keys are held. | sessions 20261019-034508, -034704; `campaigns\m1-20261019-034508.md` |
| 03:50 | Career core merged (93 career unit tests; **143/143 tests pass**); careers compiled into the mod; panel money actions reuse their operation ID for repeated clicks; career clock restored from the save. Commit `46e8b9d`. [Built locally, unit-tested] | `dotnet test` output |
| 03:52 | **M1 campaign #2 (real keys, career basic rental):** walk 4.0 m; enter (E); accelerate to 9.1 m/s over 20.9 m (W); brake to a stop (S); steer 37° (D/A) with 0 impacts; handbrake, exit to the sidewalk (Space, E); walk; return to Building (F8); save; quit. Relaunch phase: see the campaign report. | sessions 20261019-035050, -035254; `campaigns\m1-*.md` |

| 03:53 | M1 campaign #2 relaunch phase: reload `DTC2-LAB-M1` restored the career (`restored revision 1`, clock 48 s, wallet 300 cr, rental kept); leak check 0; real F8 continue and return. **RUN-0002: 17/17.** | [RUN-0002](RUN-0002/REPORT.md) |
| 03:55-04:20 | First taxi fares with the test autopilot: two failures by passenger patience (autopilot stuck; career failure path correct), then a full fare: pickup, boarding, drop-off, **49.01 cr, +12 XP**, rental share -12.25 cr. Offer generation measured at 1.1 s in one frame (ISSUE-007), fixed to 55-80 ms. | sessions 20261019-035427, -040328, -041354, -042215 |
| 04:35 | Career persistence campaign: save mid-fare, reload resumes the same stage, no double payout, older save restores its exact balance (one script check was wrong; data correct). | [RUN-0003](RUN-0003/REPORT.md) §2 |
| 04:41-05:00 | Career coverage runs: pause stops career time; service point teleport; repair 90 → 100 %; buy, refuel 30 → 100 % charged once (double click refused: "This refuel was already paid"); sell; delivery load, drop-off, parcel destroyed by impact, partial pay; company founded, hired, fleet truck, freight contract. Defects found: freight/bus jobs refused while driving a car; deadlines from free-flow route times; command-result events dropped (provided truck/bus never spawned); challenge routes marked changed (unstable access points). All fixed. | RUN-0003 §3; `campaigns\careers-*` |
| 05:08-05:38 | Freight in game: truck provided, boarded, loaded; the test autopilot then hit a car at 9.7 m/s and the load was lost (correct rule outcome) in two runs. Bus: line accepted, bus provided and boarded; the autopilot got stuck before the first stop. Completion of freight and bus runs is **not verified in game** (unit-tested). | sessions 20261019-050655, -052326 |
| 05:39-05:55 | Presentation checks: PT-BR HUD ("A pé", "Nível 10", "$ 7.321,85"); HUD 150 % with high contrast (screenshot); vehicle sound playing through the game's World mixer group while driving (listener RMS 0.18), silent on foot; 10/10 transition cycles with 3 sidewalk exits; home set and "go home" work. **Mod-less load (BR-07): a save with career data loads with `-disableCodeModding`; the Continue pointer was restored.** | session 20261019-053926 (`nomod\`), `campaigns\presentation-*` |
| 05:58 | Final package `DriveTheCityII-0.1.0-3b7980c-20261019T094752Z` (151 unit tests pass) installed; challenge run and 2-hour real-time soak started on it. | MORNING_REPORT |

## Known gaps at this point (next work)

- Freight and bus runs completed only in unit tests; the in-game test autopilot could not finish them.
- Gamepad untested (no device); enter/exit is a slide, not an animation; no footstep sound.
