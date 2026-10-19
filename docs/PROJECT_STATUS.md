# Project Status

| Field | Value |
|---|---|
| ID | DOC-STATUS |
| Document status | Draft (living document) |
| Scope | Demonstrated state of the whole project |
| Responsible role | Coordinator (maintainer); every completed task updates it |
| Related documents | [ROADMAP](../ROADMAP.md), [BACKLOG](planning/BACKLOG.md), [TRACEABILITY](planning/TRACEABILITY.md), [RISKS_AND_OPEN_QUESTIONS](planning/RISKS_AND_OPEN_QUESTIONS.md), [HANDOFF_TO_PHASE_00](agents/HANDOFF_TO_PHASE_00.md) |
| Last updated | 2026-10-18 (after TASK-0001 and TASK-0002) |

> **Overall state: PRE-DEVELOPMENT, PHASE-00 IN PROGRESS.**
>
> - The read-only audits TASK-0001 and TASK-0002 were executed on 2026-10-18 and are **In review**.
> - The runtime baseline (TASK-0003) has **not** started and needs the owner's authorization.
> - No mod code exists. Nothing has been built, launched, or tested in the game. **PHASE-00 is not completed**, and no
>   milestone has been reached.

This file records only what has been demonstrated, with evidence. Intentions belong in the [ROADMAP](../ROADMAP.md)
and the task files.

## 1. Documentation progress

| Item | State | Evidence |
|---|---|---|
| Documentation baseline (TASK-0000) | Written on 2026-10-18; **In review** (awaiting the owner) | [TASK-0000](planning/tasks/TASK-0000-documentation-bootstrap.md), [DOCUMENTATION_VALIDATION_REPORT](quality/DOCUMENTATION_VALIDATION_REPORT.md) |
| Requirements | 93 requirements (90 Must, 3 Should) in the [PRD](product/PRD.md); In review | PRD |
| Architecture decisions | ADR-0001 to ADR-0005 **Proposed**. ADR-0005 (added 2026-10-18) proposes the native mod-loading and distribution route; ADR-0004 (physics) is deliberately undecided. | [decisions](architecture/decisions/README.md) |
| Next playable milestone | M1 refined on 2026-10-18: the playable loop and its six validation areas | [ROADMAP](../ROADMAP.md) §3.1, [PHASE-05](planning/phases/PHASE-05-walk-drive-integration.md) §13 |
| Traceability | Every requirement has a specification, phase, and planned test; **0 have evidence** | [TRACEABILITY](planning/TRACEABILITY.md) |

## 2. Source review (TASK-0002, read-only, 2026-10-18; In review)

The candidate base mod was audited at the pinned commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, reading every text
file in full ([BASE_MOD_AUDIT](research/BASE_MOD_AUDIT.md) §9). Key findings, all **[Source-inspected]**:

| Finding | Consequence |
|---|---|
| A **native CS2 code mod** (the game's mod entry point). No BepInEx, no Harmony, no package or mod dependencies. | Fits the native route ([ADR-0005](architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)) |
| MIT at the pin. The **first four commits were under a restrictive "source-available" license**; MIT applies from `af35523…`. The collision layer is described upstream as ported from another project of unknown license. A decompiled-folder exclusion was added together with that code. | Collision stays concept-only. Owner questions Q-01 to Q-07 are in [REUSE_AND_LICENSE_AUDIT](legal/REUSE_AND_LICENSE_AUDIT.md) (OPEN-404, OPEN-405, OPEN-411). |
| Declares game version **1.5.\***, while the installed game is 1.6.2f1. Last commit 2026-06-17. | Compatibility unknown until TASK-0003 (RISK-001) |
| Possesses **any** live car; kinematic driving with a chase camera; collision with **moving cars only**; handback without a stop or verification; legacy input with no suppression; no save handling. | Reusable as a baseline and as techniques, **not as a fork** **[Proposal]** |
| **No on-foot mode, avatar, enter/exit transitions, state machine, career vehicles, or persistence** | All must be built (PHASE-03 to PHASE-05) |

A Coordinator spot-check against the raw pinned files confirmed 8 of 8 key claims. That check is not an independent
human review.

## 3. Environment readiness (TASK-0001, read-only, 2026-10-18; In review)

| Item | State | Evidence |
|---|---|---|
| Project root | `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII`; OneDrive-managed (RISK-011, OPEN-028). The superseded project paths do not exist, and nothing references them. | Verified commands |
| Version control | Git 2.55.0 installed; the project root is **not** a repository (OPEN-029) | Verified commands |
| CS2 | **Steam, version 1.6.2f1**, Unity 2022.3.71f1 (Mono), standard Steam library; fully installed | [WINDOWS_SETUP](engineering/WINDOWS_SETUP.md) §3 |
| Installed mods and loaders | **None.** Both mod caches are empty, and there is no BepInEx, Doorstop, or MelonLoader in the game folder. The base mod is not installed. | WINDOWS_SETUP §3 |
| Official modding toolchain | **Not installed:** no `CSII_*` variables, no Unity Editor, no .NET SDK | [TOOLCHAIN_MATRIX](engineering/TOOLCHAIN_MATRIX.md) |
| Other development tools | Visual Studio Build Tools 2022 (17.14) and Visual Studio Community 2026 (18.10) with MSBuild; VS Code; Node.js 24; Python 3.13. The .NET Framework 4.8 reference assemblies were not found. | TOOLCHAIN_MATRIX |
| Isolated test setup | **Proposed**, awaiting owner approval (OPEN-035) | WINDOWS_SETUP §8 |
| Melty | Installed and **connected** to Claude Code; inspected read-only. CS2 is supported by Melty only through BepInEx 5, which does not fit the native route. **Not adopted** for distribution (Proposed). | [MELTY_EVALUATION](research/MELTY_EVALUATION.md) §9 |
| Motor Town | Installed (Steam); **not required** by any task or at runtime | [MOTOR_TOWN_REFERENCE](research/MOTOR_TOWN_REFERENCE.md) §5a |

Installed is not the same as working. Nothing above has been exercised in the game.

## 4. Runtime validation

| Item | State |
|---|---|
| Builds | None. Neither the base mod nor our mod has been built; our mod does not exist yet. |
| In-game runs | None. The game, Melty, and Motor Town were not launched by this project. |
| Evidence runs (`RUN-NNNN`) | None ([evidence](quality/evidence/README.md)) |
| Requirements validated | 0 of 93 (all **Not run**) |
| Tested game/mod combinations | None ([COMPATIBILITY_MATRIX](quality/COMPATIBILITY_MATRIX.md)) |
| Observed defects | None, because no runtime assessment exists ([KNOWN_ISSUES](quality/KNOWN_ISSUES.md)) |

### Phases and milestones

| Milestone | Phases | Phase status | Milestone status |
|---|---|---|---|
| M0 — Verified foundation | PHASE-00, PHASE-01 | **PHASE-00 in progress** (TASK-0001 and TASK-0002 In review; TASK-0003 not started); PHASE-01 not started | Not reached |
| M1 — Playable walking/driving sandbox | PHASE-02 to PHASE-05 | Not started (refined definition: ROADMAP §3.1) | Not reached |
| M2 — Taxi-career MVP | PHASE-06 | Not started | Not reached |
| M3 — Content alpha | PHASE-07 to PHASE-09 | Not started | Not reached |
| M4 — Refined and validated beta | PHASE-10 to PHASE-12 | Not started | Not reached |
| M5 — Distributable v1.0 | PHASE-13 | Not started | Not reached |

The phase documents keep their status "Not started" until the gate process changes it. PHASE-00 is in progress only in
the sense that its first two work packages have been executed.

## 5. Blockers and decisions awaiting the project owner

| Item | Why it matters | Reference |
|---|---|---|
| Review of TASK-0001 and TASK-0002 (to move them to Done) | TASK-0003 starts only after both are Done | Task files |
| Authorization for TASK-0003: install a .NET SDK and the .NET Framework 4.8 reference assemblies, obtain the pinned source outside the project, build, deploy locally, launch a **lab city** | The PHASE-00 gate needs runtime evidence | [TASK-0003](planning/tasks/TASK-0003-isolated-baseline-validation.md) |
| Approval of the isolated test setup | Protects the owner's saves and normal mod set | OPEN-035, WINDOWS_SETUP §8 |
| Acceptance of ADR-0005 (native route; no BepInEx; Melty not a channel) | Fixes how the mod loads and ships | [ADR-0005](architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md), CONF-014 |
| Base-mod reuse questions Q-01 to Q-07 (license history, collision provenance, attribution) | Decides between selective re-implementation and reference-only use | REUSE_AND_LICENSE_AUDIT; OPEN-404, OPEN-405, OPEN-411 |
| Project license, Git, and output location | Required before code (TASK-0004) | OPEN-027, OPEN-029, OPEN-028 |
| Owner-only facts: CS2 edition, DLCs, Unity license | Toolchain and compatibility records | OPEN-001, OPEN-406 |
| Melty: optionally ask Melty for a native CS2 route; Melty exclusivity | Optional; does not block the project | MELTY_EVALUATION §9, CONF-012 |

## 6. Next authorized work

- **Done in this session (authorized):** TASK-0001 and TASK-0002, read-only. Both are **In review**.
- **Authorized next:** nothing. TASK-0003 needs explicit authorization for installs, builds, deployment, and a game
  launch.
- **Recommended sequence:**
  1. Owner review of TASK-0001 and TASK-0002.
  2. Owner approval of the isolated setup and authorization of [TASK-0003](planning/tasks/TASK-0003-isolated-baseline-validation.md),
     whose baseline needs only a .NET SDK and the 4.8 reference assemblies.
  3. PHASE-00 gate report.
  4. Owner decisions WP-01-01.
  5. [TASK-0004](planning/tasks/TASK-0004-native-mod-skeleton.md), the first implementation task: the native mod
     skeleton.

## 7. Status history

| Date | Event | Evidence |
|---|---|---|
| 2026-10-18 | Workspace verified (only `chat.md` present; not a Git repository). Documentation baseline written, with read-only source review and raw-text checks of key facts. Documentation validated. TASK-0000 moved to In review. | [TASK-0000](planning/tasks/TASK-0000-documentation-bootstrap.md), [DOCUMENTATION_VALIDATION_REPORT](quality/DOCUMENTATION_VALIDATION_REPORT.md) |
| 2026-10-18 | TASK-0001 (Windows environment discovery) and TASK-0002 (base-mod source audit at the pinned commit) executed read-only and moved to In review. Melty inspected read-only through its connection. ADR-0005 proposed. M1 refined. TASK-0004 planned. | [TASK-0001](planning/tasks/TASK-0001-windows-environment-audit.md), [TASK-0002](planning/tasks/TASK-0002-base-mod-source-audit.md), [MELTY_EVALUATION](research/MELTY_EVALUATION.md) §9 |
