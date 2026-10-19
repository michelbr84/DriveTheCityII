# RUN-0004 — Presentation, mod-less load, and real-time soak

| Field | Value |
|---|---|
| Run ID | RUN-0004 |
| Phases | PHASE-11 (presentation and accessibility), PHASE-12 (validation) |
| Date | 2026-10-19 |
| Executed by | Claude Code agent (owner-authorized autonomous run of 2026-10-19); no human reviewer yet |
| Builds | Presentation and mod-less checks: `0.1.0-29ec438+dirty-20261019T093908Z`. Soak: final package `0.1.0-3b7980c-20261019T094752Z`. |
| Sessions | `20261019-053926` (presentation, mod-less), `20261019-055029` (soak) |

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

PENDING: results are added when the soak finishes.
