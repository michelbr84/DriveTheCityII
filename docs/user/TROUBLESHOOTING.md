# Troubleshooting

| Field | Value |
|---|---|
| ID | DOC-TROUBLESHOOTING |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 |
| Phases | PHASE-01 (diagnostics, safe-disable), PHASE-04 – PHASE-05 (Recovery), PHASE-11 (final wording), PHASE-12 (verification) |
| Responsible role | Coordinator (maintainer); QA verifies each action; project owner decides the support channel |
| Related requirements | REQ-RECOV-001, REQ-RECOV-002, REQ-DIAG-001, REQ-DIAG-002, REQ-CTRL-004, REQ-TRANS-002, REQ-CAREER-005, REQ-CAREER-006, REQ-HOME-003, REQ-ECON-004, REQ-GARAGE-006, REQ-SAVE-001, REQ-SAVE-002, REQ-SAVE-004, REQ-COMPAT-001, REQ-PERF-002 |
| Related documents | [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [DEBUGGING](../engineering/DEBUGGING.md), [SPEC-UI](../experience/UI_AND_HUD.md), [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md), [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md), [QUICK_START](QUICK_START.md), [SECURITY.md](../../SECURITY.md) |
| Last updated | 2026-10-18 |

> **STATUS: NOT YET INSTALLABLE.** No build exists. The recovery actions below are **planned** designs from the
> specifications; none has been implemented or tested. **No support or issue-reporting channel exists yet.**

## 1. Purpose

Lists the recovery tools the mod is designed to offer and the planned action for each foreseeable problem, so that
every failure the player can reach has a documented way out ([PRD](../product/PRD.md) §8). Action names and bindings
are deliberately generic until the real interface exists; proposed bindings are in
[SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md).

## 2. Planned recovery tools

| Tool | What it does (intended) | Requirement |
|---|---|---|
| **Recovery** | Invoked by the player or triggered automatically when the avatar or vehicle is stuck, out of bounds, or in an invalid state. Moves it to the nearest validated safe point. | REQ-RECOV-001 |
| **Return to Building mode** | Available from any stable character-mode state. Restores the building camera, controls, and the previous simulation speed; career progress is kept. | REQ-CTRL-004 |
| **Safe-disable switch** | A mod setting that returns control to Building mode and stops career systems without data loss. Use it when something keeps going wrong. | REQ-DIAG-002 |
| **Diagnostics bundle** | Exports size-bounded logs that identify the mod version and build, sanitized so they contain no personal data. | REQ-DIAG-001 |
| **Starter job and basic rental** | Always available, so a lack of money or vehicle never ends a career. | REQ-ECON-004, REQ-GARAGE-001 |

## 3. Problems and planned actions

| Problem | Planned action |
|---|---|
| The avatar is stuck, inside geometry, or below the ground | Use Recovery. Automatic Recovery should trigger on its own for invalid positions (REQ-PLAYER-004). |
| The vehicle is stuck, overturned, or on the wrong level (for example under a bridge) | Use Recovery; the vehicle is moved to a validated safe point. |
| You cannot get out of the vehicle | The mod tells you the exit is blocked and tries other exit points. Move to open space and try again; if no exit exists, Recovery runs (REQ-TRANS-002). |
| Controls do nothing, or city-building tools react while you walk or drive | Return to Building mode. Check the mod's bindings for conflicts and remap them. Paradox's modding documentation states that mod bindings that conflict with the game's own bindings are disabled **[Documented upstream]**. If it persists, use the safe-disable switch and report it. |
| The camera is detached or stuck | Return to Building mode. If that fails, use the safe-disable switch. |
| A job became impossible after you edited the city | The mod should reroute it or cancel it with no penalty (REQ-CAREER-006). You can also cancel it yourself; consequences are explained first (REQ-CAREER-005). |
| Your home base or a service point was demolished or is unreachable | You are notified and a fallback location is used (REQ-HOME-003). |
| No money and no vehicle | Take the starter job and the basic rental (REQ-ECON-004). |
| The vehicle cannot move, or no repair or fuel point is reachable | A recovery option such as transport to a service point or a basic rental is offered (REQ-GARAGE-006). |
| Money, vehicles, or progress from a different save appear after loading | This must not happen (REQ-SAVE-001). Do **not** save over that city. Export the diagnostics bundle and report it. |
| The mod says career data was refused, quarantined, or could not be migrated | Do not delete anything; keep the save and any mod data as they are. The last valid copy is preserved (REQ-SAVE-002, REQ-SAVE-004). Report it with the diagnostics bundle. |
| The original Beta Test Driving Mod is also active | Disable one of them. The two must not run their controllers together (REQ-COMPAT-001). |
| The game was updated and the mod misbehaves | Check the compatibility declaration ([COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md)). Use the safe-disable switch or keep the mod disabled until a tested update is announced. |
| Frame rate drops at street level | Turn off optional street-level visual enhancements (REQ-PERF-002). |
| A mod system fails | It should degrade to a safe state, such as a return to Building mode, without crashing the game or corrupting the save, and log the error (REQ-RECOV-002). |

## 4. Protecting your saves

- Keep separate copies of important cities before trying the mod or a new version.
- When something looks wrong, **do not overwrite your main save**. Save to a new copy, if you save at all.
- To stop using the mod, follow the clean-exit procedure in
  [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md) §5. Until that procedure has passed its tests, no
  claim is made that removing the mod is harmless.

## 5. Reporting a problem (once a channel exists)

The project owner has not yet chosen where reports go ([OPEN-702](../planning/RISKS_AND_OPEN_QUESTIONS.md)). When a
channel exists, it will be named here. A useful report contains:

- the mod version and build ID (from the log or diagnostics bundle), the game version, DLCs, and other active mods;
- the steps that led to the problem, what you expected, and what happened;
- the sanitized diagnostics bundle, and a screenshot or recording if helpful.

Never include passwords, account names, store IDs, or full user-folder paths. Security problems follow
[SECURITY.md](../../SECURITY.md) instead of a public report. Until a channel exists, testers record defects through the
project's evidence workflow ([KNOWN_ISSUES](../quality/KNOWN_ISSUES.md)).

## 6. Verification before publication (validation: Not run)

Each row of section 3 is exercised in a lab city in PHASE-12 (TEST-RECOV-001, TEST-RECOV-002, TEST-DIAG-001,
TEST-DIAG-002, TEST-CTRL-004, TEST-TRANS-002, TEST-CAREER-006, TEST-ECON-004, TEST-GARAGE-006), and the wording is
replaced with the text actually shown in the game.
