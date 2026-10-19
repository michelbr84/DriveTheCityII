# Windows Setup and Environment Discovery

| Field | Value |
|---|---|
| ID | DOC-WINDOWS-SETUP |
| Document status | Draft |
| Release scope | Development environment (all phases) |
| Phases | PHASE-00 (TASK-0001 discovery; TASK-0003 isolated setup) |
| Responsible role | Coordinator (maintainer); the Implementer executes TASK-0001; the project owner authorizes any setup or runtime operation |
| Related requirements | REQ-SAVE-006, REQ-COMPAT-001 (indirectly: through safe test isolation) |
| Related documents | [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md), [LOCAL_ENVIRONMENT.example](LOCAL_ENVIRONMENT.example.md), [BUILD_AND_RUN](BUILD_AND_RUN.md), [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md), [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md), [TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md), [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md) |
| Last updated | 2026-10-18 |

> **State (2026-10-18):** TASK-0001 read-only discovery was performed with the owner's go-ahead. Nothing was
> installed, launched, configured, or changed. Registry entries, DLC data, logs, and saves were **not** read. Runtime
> facts (the in-game version string, whether code mods actually load) still need TASK-0003.

## 1. Purpose

This document describes the Windows workspace and how it is discovered and prepared safely. It records verified facts
in sanitized form. Machine-specific values belong in the git-ignored local file `.local/LOCAL_ENVIRONMENT.md`, created
from [LOCAL_ENVIRONMENT.example](LOCAL_ENVIRONMENT.example.md). Confirmed tool versions belong only in
[TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md).

## 2. Verified workspace facts

| Fact | Value | How it was observed |
|---|---|---|
| Project root | `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII` (authoritative, per the owner brief) | **Verified command** (2026-10-18): `Get-Location` |
| Superseded paths | The superseded project location named in the owner's follow-up brief (a folder on drive C: outside OneDrive) and the earlier drive-D example do not exist. No active document or Claude Code user configuration references them. | **Verified commands** (2026-10-18, read-only): `Test-Path`; path-substring search (no values printed). The literal paths are recorded only in the local file. |
| Synchronization | Project files carry the `ReparsePoint` attribute, which is consistent with OneDrive-managed files. Whether they are "always available" or "on demand" was not confirmed. | **Verified command** (2026-10-18): `(Get-Item …).Attributes`. The interpretation needs the owner's confirmation (OPEN-028). |
| Free disk space | Drive C: about 1.5 TB free | **Verified command** (2026-10-18): `Get-PSDrive -PSProvider FileSystem` |
| Git | `git version 2.55.0.windows.5`; the project root is **not** a repository | **Verified commands** (2026-10-18): `git --version`, `git rev-parse --show-toplevel` (OPEN-029) |
| Python / Node.js | Python 3.13.15; Node.js v24.19.0 | **Verified commands** (2026-10-18). Used only for documentation validation. |
| Operating system | Windows 11 Pro, build 10.0.26300 | Session environment report (not a command) |

## 3. Discovery results (TASK-0001, 2026-10-18)

| Item | Result | Evidence | Register |
|---|---|---|---|
| CS2 store platform | **Steam** (app 949230), `distPlatform: steam` | Steam library view (`libraryfolders.vdf`, one library); app manifest fields `appid`, `name`, `installdir`, `buildid`, `StateFlags`, `LastUpdated` only | OPEN-001 |
| CS2 installation folder | `C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II` (standard Steam library). Fully installed (StateFlags 4). Steam build 25127643, updated 2026-10-18. | App manifest | OPEN-001 |
| CS2 game version | **1.6.2f1** (`version` field of the game's `Launcher\launcher-settings.json`). The in-game string is to be confirmed in TASK-0003. | File field read | OPEN-001 |
| Engine | Unity **2022.3.71f1**, Mono scripting runtime (`MonoBleedingEdge` folder present) | `Cities2.exe` / `UnityPlayer.dll` version metadata | OPEN-406 |
| Edition and DLCs | Not determined. Not inspected; owner question. | — | OPEN-001 |
| User-data folder | `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II`, which matches the launcher's `gameDataPath`. `Saves` and `Logs` exist. Their contents were not listed or opened. | Top-level names only | OPEN-001, OPEN-035 |
| Installed mods | **None.** `.cache\Mods\local` and `.cache\Mods\pdx_mods` are empty, and there is no root-level `Mods` folder. | Top-level listing | OPEN-035 |
| Beta Test Driving Mod | **Not installed locally.** No folder matches ModId 145145 or its name. | Targeted name check | OPEN-005 |
| Third-party mod loaders | **None in the game folder.** No `BepInEx`, `winhttp.dll`, `doorstop_config.ini`, `version.dll`, or `MelonLoader`. | `Test-Path` | ADR-0005 |
| Official modding toolchain | **Not installed.** No `CSII_*` environment variables (process, user, or machine scope); no Unity Editor or Unity Hub. | Environment and folder checks | OPEN-002, OPEN-406 |
| .NET | No `dotnet` CLI or SDK. .NET Framework 4.8 runtime present. .NET Framework reference assemblies not found at their standard folder. | `Get-Command`, `Test-Path`, file metadata | OPEN-002, OPEN-003 |
| IDEs | Visual Studio Build Tools 2022 (17.14) and Visual Studio Community 2026 (18.10), with MSBuild; VS Code. Rider not found. | `vswhere`; `Get-Command` | OPEN-002 |
| Debugging tools | `dnx` and the .NET 10 SDK needed by `unity-devtools` are absent. Nothing installed. | `Get-Command` | OPEN-031 |
| Melty | Installed and running (app 1.0.132). Its tools are connected to this Claude Code session (verified with read-only calls). Its modding kit is present locally. | Melty MCP read-only calls | OPEN-016; [MELTY_EVALUATION](../research/MELTY_EVALUATION.md) |
| Motor Town | Installed through Steam (app 1369670). It is **not** required by this project and was not launched. | App manifest | OPEN-030 |

**Not done**, with the reason:

- Registry reads: no explicit consent.
- DLC inspection: owner question.
- Opening logs or saves: forbidden by TASK-0001.
- Launching the game or any launcher: not authorized.
- Reading Mod.props: the toolchain is not installed.

## 4. Safe testing prerequisites

These apply before **any** runtime operation (TASK-0003 onwards):

1. **Owner authorization** for each install, build, launch, or mod enablement ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)).
2. **Lab city only.** A disposable city created for testing. **Never the owner's primary saves** (RISK-028).
3. **Copies of saves.** Any save used for testing is a copy kept in `<LAB_SAVE_COPY_DIR>`. Originals are not opened by test procedures.
4. **Isolated mod set.** Only the mods under test are active (§8). The original Beta Test Driving Mod and Drive the City II are never active together (REQ-COMPAT-001).
5. **No game-file modification.** The installation folder is read-only for this project. Build references point to it; nothing is copied into or out of it. This excludes installing loaders such as BepInEx into the game folder ([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)).
6. **Version recorded first.** The game version (1.6.2f1 on 2026-10-18) and the mod revision are written down before each run ([TPL-TEST-REPORT](../templates/TEST_REPORT_TEMPLATE.md)).
7. **Sanitized evidence.** No account names, store IDs, or user-profile paths outside the project root ([DEBUGGING](DEBUGGING.md)).

## 5. Discovery commands (TASK-0001)

Each command below was run on 2026-10-18 and is a **Verified command (read-only)**. Raw outputs are summarized in
`.local/LOCAL_ENVIRONMENT.md`.

```powershell
Get-Location; git rev-parse --show-toplevel
Get-Command git, dotnet, node, npm, code, rider64, unity, dnx -ErrorAction SilentlyContinue
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -products * -format value -property displayName
Get-ChildItem Env: | Where-Object Name -like 'CSII_*' | Select-Object -ExpandProperty Name
Get-PSDrive -PSProvider FileSystem
(Get-Item -LiteralPath '<CS2_INSTALL_DIR>\Cities2.exe').VersionInfo
```

Steam's `libraryfolders.vdf` and the CS2 and Motor Town app manifests were parsed for non-account fields only. The
`version` field of `<CS2_INSTALL_DIR>\Launcher\launcher-settings.json` was read.

## 6. OneDrive considerations (OPEN-028, RISK-011)

- **Observation:** the project files are OneDrive-managed, as the reparse-point attributes show, and there is ample
  free space.
- **Risk:** synchronization may lock files during builds and upload large build outputs or private local files. It may also conflict with Git metadata once a repository exists.
- **Proposals (owner decision pending):**
  - keep build outputs and local files in git-ignored `artifacts/` and `.local/`, or redirect them to `<BUILD_OUTPUT_DIR>` outside OneDrive;
  - keep the project folder always available on the device;
  - never place saves, game binaries, or decompiled code in the project.
- **Before PHASE-01 code work**, the owner decides between keeping everything in the synced root and redirecting outputs.
- **Do not move the project root.** The path is fixed by the owner brief (CONF-001).

## 7. Recording results

| Result | Where |
|---|---|
| Raw values (paths, versions, outputs) | `.local/LOCAL_ENVIRONMENT.md` (git-ignored; created 2026-10-18) |
| Sanitized facts | §2 and §3 of this document |
| Tool versions | [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md), "Installed on this machine" column |
| Answers to open questions | [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md), with date and evidence |
| Task progress | [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md) status history |

## 8. Proposed isolated test setup (OPEN-035; owner approval pending)

**Proposed procedure (requires validation).** It is based on today's observations: no mods are installed, and the
user-data folder contains `playsets_backup` and the playset files under `.cache\Mods`.

1. **Starting point:** the normal mod set is currently empty. TASK-0003 records the playset files' state (names and
   timestamps only) before and after the session, so that any change becomes visible.
2. **Lab city:** create a new, disposable city in the game for testing. Never load a primary save. If an existing city
   is ever needed, the owner copies it to `<LAB_SAVE_COPY_DIR>` personally.
3. **Mod placement:** the baseline build is deployed only to the game's native local-mods location documented by the
   official toolchain (`CSII_LOCALMODSPATH`, recorded when the toolchain is installed), under a clearly named folder.
   - Disabling is done by the documented "." folder-name prefix or by removing that folder (SRC-008).
   - Nothing is placed in the game installation folder.
4. **No loader installs:** no BepInEx, Doorstop, or MelonLoader, and no game-folder changes
   ([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)).
5. **Return to normal:** remove the deployed folder, confirm that the mod caches match their pre-session state, and
   confirm that no primary save changed (by timestamp observation, without opening saves).
6. **Open points:**
   - whether a dedicated playset is preferable to the empty default set;
   - the exact local-mods folder on game 1.6.2f1, because the root-level `Mods` folder described by the official
     page does not exist yet (OPEN-036).

## 9. Open questions and risks

- **Answered or partly answered on 2026-10-18:** OPEN-001 (platform, path, version), OPEN-002 (installed side),
  OPEN-033 (Steam PC edition), OPEN-031 (presence), OPEN-030 (Motor Town is installed but not needed).
- **Still open:** OPEN-028 and OPEN-029 (owner decisions), OPEN-035 (approval of §8), OPEN-406 (Unity version for
  game 1.6.2f1), OPEN-036 (local-mods folder on 1.6.2f1).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
