# Build and Run

| Field | Value |
|---|---|
| ID | DOC-BUILD-RUN |
| Document status | Draft |
| Release scope | Development workflow (v1.0 and later) |
| Phases | PHASE-00 (TASK-0003 baseline), PHASE-01 (first own build), all later phases |
| Responsible role | Coordinator (maintainer); the Implementer executes; the project owner authorizes builds, installs, and launches |
| Related requirements | REQ-DIAG-001, REQ-REL-001, REQ-REL-004, REQ-COMPAT-001, REQ-SAVE-006 |
| Related documents | [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md), [WINDOWS_SETUP](WINDOWS_SETUP.md), [DEPENDENCIES](DEPENDENCIES.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md), [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md), [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md) |
| Last updated | 2026-10-18 |

> **No build exists.** No project, solution, or source code exists in this repository folder. Nothing has been
> compiled, deployed, or run. Every procedure below is either documented upstream or a **Proposed procedure
> (requires validation)**.

> **Update (TASK-0002, 2026-10-18):** the concrete baseline build plan, its inputs, expected deviations, and the
> rule not to patch around compile errors are in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.13. The baseline needs
> only a .NET SDK and the .NET Framework 4.8 reference assemblies, and both are missing on this machine
> ([TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md)).

## 1. Purpose

This document describes the intended build and run flow, how the candidate base mod differs from it, and the
procedures that TASK-0003 and PHASE-01 will validate. Confirmed tool versions are recorded only in
[TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md).

## 2. Intended future flow, per the official toolchain

**[Documented upstream]** (SRC-008, official wiki, verified for game 1.1.12f1 only):

1. The toolchain is installed from the game's **Options → Modding**: Unity Editor, .NET SDK, and the related tools. Missing or outdated components are flagged in the main menu.
2. A C# mod project is created from the official template through `dotnet new`. The template name `csiimod` comes from a community source (SRC-005). UI modules use `npx create-csii-ui-mod`.
3. The project imports the toolchain's Mod.props/Mod.targets, which are added automatically and use the `CSII_*` environment variables.
4. Building in Visual Studio 2022 or Rider runs Roslyn, then an AfterBuild step runs **ModPostProcessor** (IL post-processing and Burst).
5. The output is deployed to the local mods folder, described upstream as `…\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\<ModName>`. The game "will load local mods automatically". A folder whose name starts with "." is disabled. Mods are delay-loaded until login is viable.
6. Publishing uses IDE publish profiles that run ModPublisher with `Properties/PublishConfiguration.xml` ([PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md)).

The verification tag is older than current game builds. Each step must be re-confirmed against the installed game
(OPEN-002, OPEN-406).

## 3. How the base mod's project differs

The following is **[Source-inspected]** (raw text verified 2026-10-18, V-04):

| Aspect | Official flow (documented upstream) | Beta Test Driving Mod project file |
|---|---|---|
| Game assembly location | `CSII_MANAGEDPATH` and the other toolchain variables | Hard-coded relative `ManagedPath` `..\..\Cities2_Data\Managed`, which expects the project to sit two folders below a directory containing `Cities2_Data` |
| Mod.props / Mod.targets | Imported automatically | **Not imported** |
| Post-processing | ModPostProcessor runs after the build | Not wired in by the project file |
| Output | Deployed to the local mods folder | `bin\` only; no deploy step |
| Packages | — | None |
| Target framework / C# | Not stated officially; the community says Mod.props sets `net48` and C# 9 | Set explicitly: `net48`, C# 9.0 |

**Whether the published Paradox Mods build of the base mod was produced with the official post-processor is
unknown** (OPEN-403). A plain SDK build of this project might differ from the published build (RISK-403).

## 4. Proposed procedure: base-mod baseline build (TASK-0003)

**Proposed procedure (requires validation).** It may be run only after TASK-0001 and TASK-0002 are complete and the
owner has authorized it. Placeholders come from `.local/LOCAL_ENVIRONMENT.md`.

1. Obtain the source at the pinned revision (`eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, unless TASK-0002 recommends another) into `<AUTHORIZED_SOURCE_DIR>`, as in [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md) step 3. That folder is **outside this project root**, and the source is **not vendored** into the project ([UPSTREAM_STRATEGY](UPSTREAM_STRATEGY.md)). The download method needs owner approval.
2. Do **not** move the project under the game folder to satisfy the relative `ManagedPath`, because the game installation is never modified. Instead, override the property at build time:

   ```powershell
   # Proposed procedure (requires validation) — not run
   dotnet build "<AUTHORIZED_SOURCE_DIR>\BetaTestDrivingMod.csproj" -p:ManagedPath="<CS2_MANAGED_DIR>"
   ```

   Record the exact SDK version, the full output, and every warning. If the build fails because .NET Framework 4.8 reference assemblies are missing, stop and record the result. Installing anything requires separate authorization ([TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md) §3).
3. List the output files (the assembly, the `.mjs` module, and the icon and image files that the project copies) and record their sizes and hashes in the run record. Never commit or redistribute them.
4. Deploy a copy into the approved isolated mod set (`<ISOLATED_MOD_SET>`, for example a folder under `<CS2_LOCAL_MODS_DIR>`) for the lab session only. Confirm beforehand that no subscribed copy of the same mod is active (REQ-COMPAT-001).
5. Run the baseline checks BL-01 … BL-12 of [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §7 in the lab city (§6 below).
6. Afterwards, disable the folder (using the documented "." prefix) or remove it, and confirm that the lab save still loads.
7. **Optional, separately authorized:** if step 2 fails or the mod does not load, a second experiment may compile the same sources inside a project created from the official template, so that post-processing applies. This creates a derived project and therefore follows [UPSTREAM_STRATEGY](UPSTREAM_STRATEGY.md) and the license audit.

## 5. Proposed procedure: Drive the City II's own build (PHASE-01)

**Proposed procedure (requires validation).**

1. **Project creation.** Create the mod project from the official template, so that Mod.props/Mod.targets and the post-processor come from the toolchain. Do not hand-copy them.
2. **Target framework and C#.** Keep the settings that the toolchain provides. Do not override them without an ADR (OPEN-003).
3. **Pure career logic.** Keep it in a separate project with no game references, so that it can be tested without CS2 ([CODING_STANDARDS](CODING_STANDARDS.md)).
4. **Build stamping.** Each build stamps a version and a build identifier that the logs can report (REQ-DIAG-001).
5. **Outputs.** Build outputs go to git-ignored `artifacts/` or to `<BUILD_OUTPUT_DIR>`, per the OneDrive decision (OPEN-028). Binaries are never committed.
6. **Deployment.** Deployment to the local mods folder happens only for lab sessions. Publishing is a separate, owner-authorized release step.

## 6. Proposed procedure: lab run

**Proposed procedure (requires validation).**

1. The safe-testing prerequisites in [WINDOWS_SETUP](WINDOWS_SETUP.md) §4 apply: owner authorization, lab city, save copies, an isolated mod set, and no game-file changes.
2. Record the game version and the mod revision before launching. The owner launches the game.
3. Execute the scenario from [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md) or the baseline list. Capture sanitized logs and recordings ([DEBUGGING](DEBUGGING.md)).
4. Close the game normally. Then verify that the lab save loads **with and without** the mod (REQ-SAVE-006).
5. Record the run as `RUN-NNNN` under [evidence](../quality/evidence/README.md) using [TPL-TEST-REPORT](../templates/TEST_REPORT_TEMPLATE.md).

## 7. Validation checklist

All items: **Not run**.

| # | Check | Applies to | Validation |
|---|---|---|---|
| BR-01 | Toolchain components and versions recorded in TOOLCHAIN_MATRIX | Both | Not run |
| BR-02 | Build completes; the command, SDK version, and warnings are recorded | Baseline, own | Not run |
| BR-03 | No file under `<CS2_INSTALL_DIR>` was created or modified | Both | Not run |
| BR-04 | The output file list matches expectations; no game assemblies are copied to the output (`Private=false`) | Both | Not run |
| BR-05 | Post-processing ran, or its absence is recorded (OPEN-403) | Both | Not run |
| BR-06 | The mod loads in the lab city; the log shows its version and build ID | Both (build ID: own only) | Not run |
| BR-07 | Disabling or removing the mod leaves the lab save loadable | Both | Not run |
| BR-08 | The original base mod and our mod are never active together | Own | Not run |
| BR-09 | The run is recorded as evidence with the game version and revision | Both | Not run |

## 8. Open questions and risks

- **Central:** OPEN-001, OPEN-002, OPEN-003, OPEN-005, OPEN-022, OPEN-028, OPEN-035, RISK-001, RISK-011, RISK-021, RISK-028, RISK-029.
- **New:**
  - OPEN-403: the build path and post-processor.
  - OPEN-406: the Unity Editor version and license.
  - RISK-403: the baseline build may diverge from the published build.

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
