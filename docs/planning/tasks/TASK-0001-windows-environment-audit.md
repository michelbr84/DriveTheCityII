# TASK-0001 — Windows environment audit

| Field | Value |
|---|---|
| ID | TASK-0001 |
| Task status | In review |
| Task type | Research |
| Phase | [PHASE-00](../phases/PHASE-00-audit-and-windows.md) — WP-00-01, plus the read-only part of WP-00-06 |
| Responsible role | Implementer (performs read-only discovery); Reviewer (checks sanitization and labels); project owner (grants consent, answers owner-only questions) |
| Depends on | [TASK-0000](TASK-0000-documentation-bootstrap.md) (documentation baseline). Independent of [TASK-0002](TASK-0002-base-mod-source-audit.md); both may run in parallel. |
| Authorization required | Project owner go-ahead **before any discovery outside the project root** (game, launcher, user-data, and toolchain locations), recorded in the status history with its scope. Read-only operations only. Reading Windows registry uninstall entries needs separate, explicit consent. |
| Created | 2026-10-18 |
| Last updated | 2026-10-18 (executed; In review) |

> **Why Ready:** this is a research task. Its answers are unknown by design. It is ready because the questions, a safe
> read-only method, the allowed scope, and the required consent are defined
> ([DEFINITION_OF_READY](../../agents/DEFINITION_OF_READY.md)). Nothing in this file has been run yet.

## 1. Objective

Answer, with dated evidence and without changing anything, the environment questions that every later decision
depends on:

| Question | What this task records |
|---|---|
| [OPEN-001](../RISKS_AND_OPEN_QUESTIONS.md) | CS2 edition, version, store platform, DLCs, and the locations of the game and its user data (mods, logs, saves). Private values go to the local file only. |
| [OPEN-002](../RISKS_AND_OPEN_QUESTIONS.md) | Installed side only: which toolchain components are present and at which versions. Required versions come from official documentation via [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), not from this task. |
| [OPEN-028](../RISKS_AND_OPEN_QUESTIONS.md) | Observations for the owner's decision: OneDrive sync behavior of the project root and available disk space. |
| [OPEN-033](../RISKS_AND_OPEN_QUESTIONS.md) | Whether the observed edition and platform support code mods according to official documentation. |
| [OPEN-035](../RISKS_AND_OPEN_QUESTIONS.md) | Read-only part: how mods, mod sets, and saves are organized, and a **proposed** isolated test setup for owner approval. |
| [OPEN-031](../RISKS_AND_OPEN_QUESTIONS.md) | Presence only of debugging tools named in [DEBUGGING](../../engineering/DEBUGGING.md). Whether they work is TASK-0003. |

## 2. Background

- State on 2026-10-18: pre-development; nothing built or run; PHASE-00 not completed. Observed read-only by the
  coordinator that day: Git `2.55.0.windows.5` is installed; Python and Node.js exist; the project root is **not** a Git
  repository and has no parent repository; it lies inside a OneDrive-synced folder (RISK-011). No game, launcher,
  Melty, SDK, Unity, or .NET discovery has been done.
- The historical plan ([chat.md](../../../chat.md), **[Historical input]**) asked one session to install games, Melty,
  and tools, and then to build and run a baseline. The owner brief replaced that with three tasks: this read-only
  discovery, the read-only source audit (TASK-0002), and an authorized baseline (TASK-0003). See CONF-006, CONF-009,
  CONF-010.
- The official toolchain is **[Documented upstream]** as being installed and managed from within the game (see
  [SOURCES](../../research/SOURCES.md)). This task never opens the game to inspect it.

## 3. Required reading

[AGENTS.md](../../../AGENTS.md), [DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md), [PROJECT_STATUS](../../PROJECT_STATUS.md),
[HANDOFF_TO_PHASE_00](../../agents/HANDOFF_TO_PHASE_00.md), [TOOLS_AND_PERMISSIONS](../../agents/TOOLS_AND_PERMISSIONS.md),
[DEFINITION_OF_DONE](../../agents/DEFINITION_OF_DONE.md), [PHASE-00](../phases/PHASE-00-audit-and-windows.md),
[WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md),
[LOCAL_ENVIRONMENT.example](../../engineering/LOCAL_ENVIRONMENT.example.md), [SOURCES](../../research/SOURCES.md)
(toolchain sources), [DEBUGGING](../../engineering/DEBUGGING.md), [SECURITY.md](../../../SECURITY.md), and the register
entries named in section 1.

## 4. Allowed scope

- **Operations:** read-only commands listed in section 6; reading public official documentation; asking the owner
  questions; reading only the locations the owner names or confirms (game installation, user-data folder, launcher
  library view, toolchain folders) and tool resolution through `PATH`.
- **Files to write:** only the outputs listed in section 9. WINDOWS_SETUP is sanitized; TOOLCHAIN_MATRIX receives the
  installed side only; private values go to the git-ignored local file created from
  [LOCAL_ENVIRONMENT.example](../../engineering/LOCAL_ENVIRONMENT.example.md) under `.local/`.

## 5. Forbidden actions and protected contracts

- No installs, updates, repairs, or uninstalls of anything (CONF-006). A missing tool is recorded as "not found".
- No launching of CS2, any launcher, Melty, Motor Town, or IDEs. No opening of in-game menus.
- No changes to settings of any kind: game, launcher, OneDrive, environment variables, `PATH`, registry, Windows.
- No enabling, disabling, moving, or deleting mods. No changes to playsets or mod sets.
- No opening, copying, or listing the contents of save files. Record only whether a saves location exists.
- No reading of credentials, tokens, account or login files, or launcher authentication data.
- No recursive searches of drives or the user profile, and no scanning of folders the owner did not name.
- No copying of game files into the project. No Git initialization (OPEN-029 is an owner decision).
- **Protected contract:** tracked documents contain only sanitized values. User names, full user-profile paths (other
  than the documented project root), account IDs, and store IDs go only to the local file
  ([DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md) §11, RISK-030). Unknown values are never guessed.

## 6. Procedure

Placeholders such as `<CS2_INSTALL_DIR>`, `<CS2_USER_DATA_DIR>`, `<GAME_EXECUTABLE>`, and `<LOCAL_MODS_DIR>` are filled
in from what the owner names; their real values are never written into tracked files.

1. **Record consent.** Add a status-history row with the date, "In progress", and the exact scope of the owner's
   go-ahead (which locations, and whether registry reading is allowed).
2. **Confirm the workspace.** *Read-only discovery (not yet run):*
   ```powershell
   Get-Location
   git rev-parse --show-toplevel
   ```
   Expect the documented project root, and "not a git repository" as observed on 2026-10-18.
3. **Ask the owner** what only the owner knows: store platform, edition, owned DLCs, whether the official modding
   toolchain was ever installed, whether a Paradox account is used in game, which active mod set must stay untouched,
   and the preferred lab-city approach (OPEN-603). Answers go to the local file; sanitized summaries to WINDOWS_SETUP.
4. **Detect installed tools (OPEN-002).** *Read-only discovery (not yet run):*
   ```powershell
   Get-Command git, dotnet, node, npm, code -ErrorAction SilentlyContinue | Select-Object Name, Version
   git --version
   dotnet --list-sdks
   dotnet --list-runtimes
   node --version
   ```
   Run `dotnet` commands only if `Get-Command` found `dotnet`. Do not record the `Source` paths in tracked files.
5. **Detect Visual Studio, if present.** *Read-only discovery (not yet run):* Microsoft's `vswhere` utility, if it
   exists in its standard installer folder, lists installed Visual Studio products:
   ```powershell
   $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
   if (Test-Path $vswhere) { & $vswhere -products * -format json }
   ```
   For Rider, the Unity Editor, or other tools, ask the owner where they are installed, or record "not checked".
6. **Check toolchain environment variables by name only.** *Read-only discovery (not yet run):*
   ```powershell
   Get-ChildItem Env: | Where-Object Name -like 'CSII_*' | Select-Object -ExpandProperty Name
   ```
   Variable names go to TOOLCHAIN_MATRIX; values (paths) go only to the local file.
7. **Game installation (OPEN-001).** *Read-only discovery (not yet run),* only for the owner-named location:
   ```powershell
   Test-Path -LiteralPath '<CS2_INSTALL_DIR>'
   (Get-Item -LiteralPath '<CS2_INSTALL_DIR>\<GAME_EXECUTABLE>').VersionInfo | Select-Object FileVersion, ProductVersion
   ```
   A file version may differ from the version string shown in game. Record both as observed; the in-game version is
   confirmed in TASK-0003 when a launch is authorized.
8. **User data and local mods (OPEN-001, OPEN-035).** *Read-only discovery (not yet run),* top level only, no
   recursion, no file contents:
   ```powershell
   Get-ChildItem -LiteralPath '<CS2_USER_DATA_DIR>' -Directory -Name
   (Get-ChildItem -LiteralPath '<LOCAL_MODS_DIR>' -Directory).Count
   ```
   Record the count in WINDOWS_SETUP and any mod names only in the local file.
9. **Installed-program entries (only with explicit registry consent).** *Read-only discovery (not yet run):*
   ```powershell
   Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
     'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue |
     Where-Object DisplayName -match 'Unity|\.NET|Visual Studio|Rider|Node' |
     Select-Object DisplayName, DisplayVersion
   ```
10. **Disk space and OneDrive (OPEN-028).** *Read-only discovery (not yet run):*
    ```powershell
    Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{n='FreeGB';e={[math]::Round($_.Free/1GB,1)}}
    (Get-Item -LiteralPath '.\chat.md').Attributes
    ```
    The attribute output may hint at OneDrive on-demand placeholders; that interpretation requires validation. Ask the
    owner to describe the sync status shown by OneDrive. Do not change any OneDrive setting.
11. **Debugging tools (OPEN-031).** Check presence only of the tools named in DEBUGGING, with `Get-Command` or the
    owner's answer. Do not install, configure, or attach anything.
12. **Code-mod support (OPEN-033).** Compare the observed edition and platform with the official documentation listed in
    SOURCES. If the answer needs a launch, mark it Inconclusive and hand it to TASK-0003.
13. **Isolated test setup (OPEN-035, WP-00-06).** From documentation and the step 8 observations, draft the isolated
    setup in WINDOWS_SETUP as a **Proposed procedure (requires validation)**: separate mod set or profile, disposable
    lab city, save copies, and how to return to the owner's normal setup. Ask the owner to approve or amend it.
14. **Record and hand off.** Relabel each command that was run as **Verified command** with its date and summarize its
    sanitized output; update the section 9 documents; set the status to In review; write the handoff (section 11) with
    [TPL-HANDOFF](../../templates/HANDOFF_TEMPLATE.md).

## 7. Validation

- The Reviewer checks that every recorded value has a method, a date, and a source (command, owner answer, or
  documentation), and that unknowns stay open with a reason.
- Sanitization check. *Proposed procedure (requires validation):* search the changed tracked files for drive-letter
  paths, user-profile segments, and account identifiers, and review every hit. Only the documented project root may
  appear.
- The local file is covered by the `.local/` rule in the project's `.gitignore` (checked by reading that file, because
  the root is not a Git repository yet).
- The implementer states in the handoff that nothing was installed, launched, changed, or read outside the consented
  scope. The Reviewer confirms the statement is consistent with the recorded commands.

## 8. Acceptance criteria

1. OPEN-001 is answered, or partly answered with the remaining part (for example, the in-game version string) assigned
   to TASK-0003.
2. For OPEN-002, every toolchain component listed in TOOLCHAIN_MATRIX is marked "present (version)", "not found", or
   "not checked (reason)".
3. OPEN-028 observations are recorded and the owner decision is requested, not made by the agent.
4. OPEN-033 is answered or marked Inconclusive with its next step.
5. The OPEN-035 read-only findings and a proposed isolated setup are recorded for owner approval; OPEN-031 presence is
   recorded.
6. Tracked documents are sanitized; the local file exists and is covered by an ignore rule.
7. PROJECT_STATUS, the register, and CHANGELOG reflect what actually happened; the handoff is written; a Reviewer
   checked the work.

## 9. Outputs

[WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) · [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md) · the
local environment file under `.local/` · [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md) (OPEN-001, 002, 028,
031, 033, 035) · [PROJECT_STATUS](../../PROJECT_STATUS.md) (environment readiness) · [CHANGELOG.md](../../../CHANGELOG.md)
· [BACKLOG](../BACKLOG.md) status mirror · this file's status history and handoff.

## 10. Recovery / rollback

- The task is read-only, so there is nothing to roll back on the machine. If any command prompts for input, requests
  elevation, or appears to change state, stop, do not confirm, record what happened, and inform the owner.
- If personal data reaches a tracked file, remove it in the same session and note the correction in the handoff without
  repeating the data.
- If the owner declines part of the discovery, record "not checked (owner declined)" and continue with the rest.
- If a tool is missing, record it. Installing it is a TASK-0003 decision that needs owner authorization.

## 11. Handoff notes

The handoff (to be written at completion) must give TASK-0003 the installed-versus-required toolchain gap, the proposed
isolated setup and its approval status, and any items left Inconclusive. It must give TASK-0002 the observed game
version for comparison with the base mod's declared version. It must list the owner decisions now due (OPEN-028,
OPEN-029) and the documents actually read.

### Handoff (2026-10-18)

- **Documents read:** AGENTS.md, CLAUDE.md, README, PROJECT_STATUS, PRD (skim), ROADMAP, HANDOFF_TO_PHASE_00, this file,
  TASK-0002, PHASE-00, PHASE-01, PHASE-05, WINDOWS_SETUP, TOOLCHAIN_MATRIX, LOCAL_ENVIRONMENT.example,
  MELTY_EVALUATION, CS2_INTEGRATION_FINDINGS, MOTOR_TOWN_REFERENCE, PACKAGING_AND_PUBLISHING, and the register entries
  in §1.
- **Results:** [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) §2–§3 and §8, and
  [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md) (installed side). Private values are in
  `.local/LOCAL_ENVIRONMENT.md`, which is git-ignored. Key facts:
  - Steam CS2 **1.6.2f1** in the standard Steam library, running Unity 2022.3.71f1 (Mono);
  - **no mods** installed and **no loader** in the game folder;
  - official toolchain, Unity, and .NET SDK **not installed**; Visual Studio 2022 Build Tools and 2026 present;
  - Melty installed and connected; Motor Town installed but not needed.
- **Acceptance criteria:**
  1. OPEN-001 partly answered (edition, DLCs, and the in-game version string go to the owner and TASK-0003).
  2. TOOLCHAIN_MATRIX rows all marked.
  3. OPEN-028 observations recorded; decision requested from the owner.
  4. OPEN-033 answered from documentation; runtime confirmation in TASK-0003.
  5. OPEN-035 setup proposed and OPEN-031 presence recorded.
  6. Sanitization checked: tracked files contain the project root, the standard Steam path, and `%USERPROFILE%`-style
     paths only.
  7. A Reviewer check is still needed: the Coordinator self-checked, which is not independent.
- **For TASK-0003:**
  - the installed-versus-required gap: .NET SDK and the 4.8 reference assemblies are enough for the baseline build;
    the Unity Editor and the full toolchain are needed for TASK-0004;
  - the proposed isolated setup awaits approval;
  - the game is 1.6.2f1 while the base mod declares 1.5.*;
  - the local-mods folder is unknown (OPEN-036).
- **Owner decisions now due:** OPEN-028, OPEN-029, OPEN-035 approval, and owner-only questions (edition, DLCs, Unity
  license, preferred lab-city approach).
- **Not done, with the reason:** registry reads (no explicit consent); opening logs or saves (forbidden); launching
  anything (not authorized).
- **Statement:** nothing was installed, launched, changed, or read outside the consented scope. Melty tools were used
  read-only. `consult_melty` and `request_game` were not called.

## 12. Status history

| Date | Event |
|---|---|
| 2026-10-18 | Created by the documentation bootstrap (TASK-0000); not started. |
| 2026-10-18 | In review. Discovery executed read-only; results recorded; handoff written. Awaiting a Reviewer check (Coordinator self-check only) and owner answers. |
| 2026-10-18 | In progress. Owner go-ahead given in the session message of 2026-10-18: read-only discovery of the CS2 installation path and version, installed modding tools and development dependencies, mod loaders and modding entry points, existing Beta Test Driving Mod files, and the state of the Melty connection. Registry reading was **not** explicitly consented, so it is not done. No launches, installs, setting changes, or save access. |
