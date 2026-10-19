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

## Known gaps at this point (next work)

- AI traffic does not yet react to the player's car (car not registered in lane object buffers).
- The game's building hover highlight still shows in character mode.
- No HUD, career, persistence of career data, settings UI, or localization yet.
- Base mod runtime baseline (TASK-0003) not yet run in game.
