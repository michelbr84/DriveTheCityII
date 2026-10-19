# RUN-0004 — Presentation, mod-less load, and real-time soak

| Field | Value |
|---|---|
| Run ID | RUN-0004 |
| Phases | PHASE-11 (presentation and accessibility), PHASE-12 (validation) |
| Date | 2026-10-19 |
| Executed by | Claude Code agent (owner-authorized autonomous run of 2026-10-19); no human reviewer yet |
| Builds | Presentation and mod-less checks: `0.1.0-29ec438+dirty-20261019T093908Z`. Soak: final package `0.1.0-3eaf045-20261019T100642Z` (a first soak on `3b7980c` was stopped after 15 minutes: the test autopilot tripped the recovery-loop protection and then idled; the autopilot was fixed and the soak restarted from zero). |
| Sessions | `20261019-053926` (presentation, mod-less), `20261019-062523` (soak; an earlier start, `20261019-061251`, was stopped after 12 minutes to fix the soak script's vehicle maintenance) |

## 1. Presentation (`tools\check-presentation.ps1`)

Report: `%LOCALAPPDATA%\DriveTheCityII\test\campaigns\presentation-20261019-054556.md`.

| Check | Result | Detail |
|---|---|---|
| Brazilian Portuguese (mod language option) | **Passed** | HUD view model: "A pé", "Pausado", "V: mudar câmera", "F8: voltar à construção da cidade", "Tab: trabalhos, garagem e empresa", "$ 7.321,85", "Nível 10", tab "Trabalhos". Career messages: 174 of 222 keys have Portuguese text (unit-tested); payout lines and job titles are formatted in Portuguese by the mod. |
| HUD scale 150 % and high contrast | **Passed (visual)** | Screenshot `20261019-054340-hud-scale150-contrast.png`: larger opaque dark status card. The scripted text check read the wrong line (a screenshot result); the visual check decides. |
| Vehicle sound | **Passed (measured, not listened to)** | Driving: `playing=True group=World targetFreqHz=78.4 gain=0.22 listenerRms=0.18`. On foot: `playing=False gain=0.000`. The sound goes through the game's own mixer, so the game's volume sliders apply. Nobody has listened to it. |
| Transition cycles with sidewalk-first exits | **Passed** | 10/10, 0 failures, actors 2 → 2; 3 of 10 exits on a sidewalk (before the change: 0–1 of 10). |
| Options restored | **Passed** | Language, HUD scale and contrast reset to defaults at the end of the check. |

## 2. Mod-less load (BR-07, REQ-SAVE-006; `tools\check-nomod.ps1`)

| Step | Result |
|---|---|
| Save `DTC2-LAB-NOMOD` with the mod (career envelope, company, home) | **Passed** |
| Relaunch the same game with `-continuelastsave -disableCodeModding` | **Passed**: Modding.log shows Drive the City II **not** loaded |
| The lab save loads | **Passed**: SceneFlow.log `Starting game from 'DTC2-LAB-NOMOD …'` then `Loading completed`; screenshot `nomod\nomod-loaded.png` shows the city |
| Restore the Continue pointer and state files | **Passed**: `UserState.coc` and `continue_game.json` restored by `test-session.ps1 -Stop` |

## 3. Real-time soak (`tools\soak.ps1 -Minutes 120`)

Report: `%LOCALAPPDATA%\DriveTheCityII\test\campaigns\soak-20261019-062522.md`; memory samples every 30 s in
`soak-20261019-062522-memory.csv`. City: `DTC2-LAB-CAREERS-G` (benchmark city lab copy, 105,000 inhabitants, with a
career, a company with one driver and a fleet truck, and a home).

**Real time, not compressed:** 06:26:21 → 08:29:21 (122.9 minutes) on the final package `0.1.0-3eaf045`.

| Measure | Result |
|---|---|
| Game process | **No crash, no hang**; it ran the whole time and quit normally at the end |
| Activity (simulated driving) | 10 taxi fares started (2 paid; the rest failed or timed out because of the test autopilot's driving), 17 exits on foot, 6 returns to city building for 30 s, 5 saves while driving (0 failures), 2 reloads of the soak save (0 failures), 5 maintenance visits to a service point (repair or breakdown recovery) |
| Mod entities in saves | `leakcheck` after each reload and at the end: **0** (`excluded-signature cars=0 actors=0`) |
| Frame time (224,769 frames) | Average 32.8 ms (**30 fps**), p50 32.0 ms, p95 40.9 ms, p99 53.7 ms; 119 frames over 100 ms (0.05 %); the worst frame, 3.1 s, came during a save reload |
| Managed (mod and game C#) heap | 411 MB at the start, between about 390 and 490 MB during the run, 465 MB at the end: **no steady growth** |
| Process memory (whole game) | Private memory averaged per quarter: 13.8 → 14.4 → 14.7 → 14.8 GB (the first samples, 9.7 GB, were taken while the city was loading). Growth flattens out. A comparison soak with the mod disabled was **not run**, so this growth cannot be attributed to the mod or to the game. |
| Handles | 2,750 → 3,119 (max 3,127) |
| Company (managed simulation) | Periods 0 to 11 settled during the soak; 9 simulated jobs; the 10-period freight contract ended; company cash 8,008 → 4,496 cr (salaries above income, as the simulation allows) |
| Career time | Advanced only in character mode and paused in city building, as specified (career clock 7,586 s at the end) |

Verdict: **Passed for stability** (2 hours of real-time play with saves, reloads, mode changes and company settlement,
no crash, no mod entities in saves, steady managed memory). Not a gameplay-quality test: the fares were driven by the
test autopilot.
