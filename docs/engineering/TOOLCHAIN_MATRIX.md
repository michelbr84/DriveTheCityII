# Toolchain Matrix

| Field | Value |
|---|---|
| ID | DOC-TOOLCHAIN |
| Document status | Draft |
| Release scope | Development environment (all phases) |
| Phases | PHASE-00 (TASK-0001, TASK-0003); updated whenever a tool version changes |
| Responsible role | Coordinator (maintainer); the Implementer records observations; the project owner authorizes installs |
| Related requirements | REQ-REL-001 (players need no development tools), REQ-DIAG-001 (version identification) |
| Related documents | [WINDOWS_SETUP](WINDOWS_SETUP.md), [BUILD_AND_RUN](BUILD_AND_RUN.md), [DEPENDENCIES](DEPENDENCIES.md), [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md), [SOURCES](../research/SOURCES.md) |
| Last updated | 2026-10-19 (private .NET SDK installed for the implementation run) |

> **Confirmed for this project: none of the mod toolchain.** The installed side was recorded by TASK-0001 on
> **2026-10-19:** a private, user-scoped **.NET SDK 10.0.402** (official zip, SHA-512 verified) is installed at
> `%LOCALAPPDATA%\DriveTheCityII\tools\dotnet`, with `ilspycmd` 11.1 as a scoped tool for reading game assemblies. The
> mod builds with it against the game's own assemblies ([BUILD_AND_RUN](BUILD_AND_RUN.md) §0, ADR-0005). The official
> toolchain and the Unity Editor are still not installed and were not needed.
>
> 2026-10-18: the official toolchain, Unity Editor, and .NET SDK are **not installed**. Visual Studio 2022 Build Tools
> and Visual Studio 2026 are present. Installing anything needs the owner's authorization (TASK-0003).

## 1. Rules

- This file is the **single home** for development-tool versions. Other documents link here instead of repeating versions.
- The columns are independent:
  - **Documented requirement:** what a source states, with its `SRC-*` ID and the game version that source was verified for.
  - **Installed on this machine:** observed by a read-only command or owner statement, with a date. "Not checked" means nobody has looked.
  - **Confirmed for this project:** a version proven to work for this project, either by a recorded isolated build and run (`RUN-NNNN`, TASK-0003 or later) or by an official requirement that matches the installed game version. Until then: **No**.
- Never pick a version to fill a gap. Never change the mod's target framework to satisfy a tool (RISK-021; historical input).

## 2. Mod development toolchain (SDK and tool dependencies)

| Component | Purpose | Documented requirement (source; verified-for game version) | Installed on this machine | Confirmed for this project |
|---|---|---|---|---|
| Cities: Skylines II | Host game; source of build-time reference assemblies | — | **1.6.2f1**, Steam (launcher-settings `version`; Steam build 25127643); runtime Unity 2022.3.71f1 (TASK-0001, 2026-10-18) | No |
| Official modding toolchain (installer) | Installs and updates the components below | In-game Options → Modding (SRC-008; 1.1.12f1) | **Not installed** (no `CSII_*` variables, no Unity Editor; TASK-0001, 2026-10-18) | No |
| Unity Editor | Required by the toolchain | 2022.3.7f1, "required to be installed" (SRC-008; **1.1.12f1 only**; the page says the version tracks game updates) | **Not found** (no Unity Hub or Editor folders; 2026-10-18). The installed game runs Unity 2022.3.71f1 | No (OPEN-406) |
| Unity license | Legal prerequisite of the Unity Editor | Personal or commercial license, "a legal requirement we can not automate" (SRC-008; 1.1.12f1) | Unknown (owner question) | No (OPEN-406; owner action) |
| .NET SDK | Compiles the C# project | Minimum 6, "8 is recommended"; the toolchain installs 8 (SRC-008; 1.1.12f1) | **10.0.402, private** at `%LOCALAPPDATA%\DriveTheCityII\tools\dotnet` (2026-10-19); not on the system PATH | Yes (used for every build since 2026-10-19) |
| C# mod template | Project scaffolding | NuGet template through `dotnet new` (SRC-008); the name `csiimod` comes from the community (SRC-005) | Not installed (no .NET SDK) | No |
| IDE | Build and publish profiles | Visual Studio 2022 (17.8) or Rider (2021.3.3) (SRC-008; 1.1.12f1) | Visual Studio Build Tools 2022 **17.14.37628.2** and Visual Studio Community 2026 **18.10.12201.205**, MSBuild present (vswhere, 2026-10-18); Rider not found | No |
| Node.js | UI mod build only | Minimum 18; the toolchain installs 20.11 (SRC-008; 1.1.12f1) | `v24.19.0` observed 2026-10-18 (see §4) | No (not tried for UI builds) |
| UI IDE | UI editing | VS Code (1.86) or Rider (SRC-008; 1.1.12f1) | VS Code present (`code` on PATH; version not recorded); Rider not found | No |
| UI mod template | UI scaffolding | `npx create-csii-ui-mod` (SRC-008; 1.1.12f1) | Not installed | No |
| ModPostProcessor | IL post-processing and Burst after the build | Runs as an AfterBuild step via Mod.targets (SRC-008; 1.1.12f1) | Not installed (toolchain absent) | No (OPEN-403) |
| ModPublisher | Publishing to Paradox Mods | Invoked through IDE publish profiles (SRC-008; 1.1.12f1) | Not installed (toolchain absent) | No (OPEN-017) |
| `CSII_*` environment variables | Paths and versions for Mod.props/Mod.targets | Names listed in SRC-008 (for example `CSII_TOOLPATH`, `CSII_MANAGEDPATH`, `CSII_LOCALMODSPATH`, `CSII_UNITYVERSION`, `CSII_ENTITIESVERSION` "typically 1.0.14") | **None** in process, user, or machine scope (2026-10-18) | No |

**Baseline-only footnote (TASK-0002, 2026-10-18):** building the pinned **base mod** for TASK-0003 needs only a .NET SDK
that can build SDK-style `net48` projects, the .NET Framework 4.8 reference assemblies, and the game's
`Cities2_Data\Managed` assemblies (the 18 DLLs listed in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.5). It
needs no Unity Editor, post-processor, `CSII_*` variables, or Node.js. The full toolchain above is needed for **our** mod
(TASK-0004). The base mod declares `GameVersion` 1.5.*, while the installed game is 1.6.2f1.

## 3. Mod runtime target (what the compiled mod is built for)

These are properties of the **mod assembly**, not tools to install. An SDK that builds a project and the framework the
project targets are different things: a newer .NET SDK can compile a project that targets an older .NET Framework.

| Item | Documented requirement | Evidence level | Confirmed for this project |
|---|---|---|---|
| Target framework | `net48`: community, "`Mod.props` fixes `net48` to match the game's runtime" (SRC-005; 1.6.2f1). Base mod project file `net48` (SRC-001, raw). Another community mod uses `net472` (SRC-013). **No official page states it.** | Community-documented + source-inspected, not officially documented | No (OPEN-003) |
| C# language version | 9 (community SRC-005: Mod.props pins C# 9; base mod `LangVersion` 9.0, raw) | Community-documented + source-inspected | No (OPEN-003) |
| .NET Framework 4.8 reference assemblies | Needed to compile a `net48` project. The base mod declares no package for them; whether the official toolchain provides them is unknown. On this machine (2026-10-18): the .NET Framework 4.8 **runtime** is present (`clr.dll` 4.8.10008.0), but the reference assemblies were **not found** at their standard folder. | **[Unverified]** | No (OPEN-403, TASK-0003) |
| Game reference assemblies | Referenced from the installed game at build time and never redistributed ([DEPENDENCIES](DEPENDENCIES.md)) | Source-inspected (base mod, raw) | No |
| Harmony | Not planned ([DEPENDENCIES](DEPENDENCIES.md)). The community pins `Lib.Harmony` 2.2.2 (SRC-005). The base mod does not use Harmony (TASK-0002). | Community-documented; source-inspected | Not applicable |
| BepInEx | Not used. CS2 code mods load through the game's own mod entry point; the base mod uses that entry point and no BepInEx (TASK-0002). Melty's catalog offers BepInEx 5 only, which this project does not adopt (ADR-0005). | Source-inspected; Melty service response | Not applicable |

## 4. Documentation and workspace tools (not the mod toolchain)

| Component | Purpose in this project | Installed on this machine | Confirmed for this project |
|---|---|---|---|
| Git | Future version control (no repository yet; OPEN-029) | `git version 2.55.0.windows.5`: **Verified command**, `git --version`, coordinator, 2026-10-18 | Not applicable: documentation and workspace tooling, not a mod-toolchain confirmation |
| Python | Documentation validation scripts run outside the project | `Python 3.13.15`: **Verified command**, `python --version`, coordinator, 2026-10-18 | Not applicable: documentation tooling only |
| Node.js | Documentation validation; possibly UI builds later | `v24.19.0`: **Verified command**, `node --version`, coordinator, 2026-10-18 | **No** for UI mod builds: the toolchain installs 20.11, and compatibility with 24.x is unverified |
| Claude Code | Agent host used for the documentation session | Present (this session ran in it); version not recorded | Not a build dependency |

## 5. Optional tooling (not installed; owner authorization required)

| Component | Documented requirement | Status |
|---|---|---|
| Community agent plugin `cs2-modding` (knowledge) | Claude Code plugin marketplace (SRC-005) | Not installed. The installation commands are documented upstream and were not executed. |
| Community agent plugin `unity-devtools` | .NET 10 SDK; a Unity Mono **development** build of the game (SRC-005) | Not installed. It is unlikely to work on the retail game (OPEN-031). |
| Community agent plugin `coherent-gameface` | Node 22.4+; a reachable CDP endpoint (SRC-005) | Not installed. Enabling the endpoint in CS2 is undocumented (OPEN-031). |
| Melty | Windows application (SRC-004) | Installed (app 1.0.132) and connected to Claude Code by the owner. It was used **read-only** on 2026-10-18 for evaluation. It is not a build or runtime dependency ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)). |
| BepInEx 5 (v5.4.23.5) | Melty's only loader for CS2; Melty installs it into the game folder (Melty `game_info`, 2026-10-18) | Not installed (no footprint in the game folder). **Not adopted** for this project ([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)). |

## 6. Updating this matrix

1. **Observation** (TASK-0001): fill in "Installed on this machine" with the value, the date, and the command or owner statement. Raw outputs go to `.local/LOCAL_ENVIRONMENT.md`.
2. **Confirmation** (TASK-0003 or later): set "Confirmed for this project" to **Yes** only with a link to the recorded run and the game version used.
3. **Game update:** re-check the official toolchain notifications and re-confirm the affected rows. Record the change in [CHANGELOG.md](../../CHANGELOG.md) and the tested combination in [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md).
4. **Installs** happen only with explicit owner authorization ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)).

## 7. Open questions and risks

- **Central:** OPEN-001, OPEN-002, OPEN-003, OPEN-017, OPEN-029, OPEN-031, RISK-021.
- **New:** OPEN-403 (the build path and post-processor), OPEN-406 (the Unity Editor version and license).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
