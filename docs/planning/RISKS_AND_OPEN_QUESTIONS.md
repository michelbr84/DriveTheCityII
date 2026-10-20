# Risks, Open Questions, and Recorded Conflicts

| Field | Value |
|---|---|
| ID | DOC-RISKS |
| Document status | Draft |
| Scope | Whole project (v1.0 and the documentation baseline) |
| Responsible role | Coordinator (maintainer); every role may propose entries; project owner decides scope-affecting resolutions |
| Related documents | [PRD](../product/PRD.md), [ROADMAP](../../ROADMAP.md), [FEASIBILITY_MATRIX](../research/FEASIBILITY_MATRIX.md), [PROJECT_STATUS](../PROJECT_STATUS.md), [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) |
| Last updated | 2026-10-18 |

## 1. How to use this register

- **Risks (`RISK-*`)** are things that could go wrong. Each has a likelihood, an impact, a mitigation, and an indicator.
- **Open questions (`OPEN-*`)** are unknowns that must be answered with evidence. Each has an impact, a verification
  method, and a resolving phase or task.
- **Conflicts (`CONF-*`)** are contradictions found between planning inputs, with their resolution.
- Numbering has deliberate gaps: `001–099` are central entries. Ranges `101+`, `201+`, … were assigned to document
  groups during the 2026-10-18 baseline and are listed in section 5.
- Status values: risks — `Open`, `Mitigating`, `Closed`; questions — `Open`, `Answered (date, evidence)`;
  conflicts — `Resolved`, `Proposed resolution`, `Open`.
- An observed defect is **not** a risk. It goes to [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md).
- Likelihood and impact use L, M, H and are **[Proposal]** judgments made before any runtime evidence exists.

## 2. Risks

### RISK-001 — Base mod incompatible with the installed game version
- Description: The Beta Test Driving Mod may not build or run against the owner's installed CS2 version (games and mods drift with updates). Its publish configuration declares `GameVersion 1.5.*`, and its latest commit is dated 2026-06-17 (raw text verified). Compatibility with the installed game is unknown. · Likelihood: M · Impact: H
- Reassessment proposal (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): likelihood **M → H**, because the declared `GameVersion` 1.5.* differs from the installed 1.6.2f1, there has been no update since 2026-06-17, and the code depends heavily on internals. Impact **H → M**, because TASK-0002 recommends concept-level reuse, so an incompatibility removes only the runtime baseline. Pending Reviewer and owner agreement.
- Mitigation: Pin an exact upstream revision; audit read-only first (TASK-0002); validate in an isolated lab setup (TASK-0003); keep the option to re-implement vehicle control independently. · Indicator: build errors, missing types, or runtime exceptions in the baseline. · Owner role: Coordinator · Phase: PHASE-00 · Status: Open

### RISK-002 — License or provenance prevents or constrains reuse of the base mod
- Description: The license, copyright holders, or embedded third-party material may not allow the planned reuse, or may impose obligations. · Likelihood: L · Impact: H
- Reassessment proposal (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): likelihood **L → M**; impact H for code reuse, L for concept-only reuse. Reasons: the restrictive pre-relicense history, the decompiled-folder evidence, and the CHANGELOG claim that the collision layer was ported from another project (RISK-411). Mitigation added: limit code reuse to the "reusable code" list in BASE_MOD_AUDIT §9, and keep collision concept-only.
- Mitigation: Complete [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) before incorporating any code; preserve notices; fall back to selective re-implementation of the behavior, attributing the original when in doubt (a re-implementation written after reading the upstream code is not clean-room; see [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md)). · Indicator: missing or ambiguous LICENSE, third-party files without notices. · Owner role: Coordinator · Phase: PHASE-00 · Status: Open

### RISK-003 — No usable building or surface collision data
- Description: Rendered roads and buildings may not provide queryable surfaces or collision volumes, causing clipping, tunneling, or level switching on bridges. The base mod's README states "Building collision and crash fire effects are not added in this build" (raw text verified 2026-10-18). Its collision handling covers other vehicles only (source-inspected). · Likelihood: H · Impact: H
- Mitigation: PHASE-03 laboratory and spikes (OPEN-007, OPEN-008); bounded local collision proxies; scope review if infeasible. · Indicator: lab scenarios fail consistently. · Owner role: Coordinator · Phase: PHASE-03 · Status: Open

### RISK-004 — AI and player control conflict on the same vehicle
- Description: Native AI and the player controller writing the same vehicle can cause jitter, stuck traffic, stale lane reservations, or invisible blockers. · Likelihood: M · Impact: H
- Mitigation: Single movement authority ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md)); explicit handback protocol ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)); PHASE-02 route tests. · Indicator: vehicles freezing or queues forming at a handback location. · Owner role: Reviewer (critical area) · Phase: PHASE-02 · Status: Open

### RISK-005 — Save corruption or leakage between save revisions
- Description: Career data may be lost or corrupted, or data from a newer session may appear when an older save is loaded. · Likelihood: M · Impact: H
- Mitigation: Save identity and revision binding, schema versions, atomic writes, quarantine, last valid copy ([SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md)). · Indicator: any TEST-SAVE failure. · Owner role: Reviewer (critical area) · Phase: PHASE-01 · Status: Open

### RISK-006 — Duplicated rewards or money
- Description: Replayed events, reloads, or timer bugs may pay a mission or company income more than once. · Likelihood: M · Impact: H
- Mitigation: Deterministic TransactionIds, append-only ledger that rejects duplicates, persisted time checkpoints ([SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md)). · Indicator: ledger balance does not equal the sum of unique transactions. · Owner role: QA · Phase: PHASE-06 · Status: Open

### RISK-007 — Game updates break internal integrations
- Description: CS2 updates may change internal systems the mod relies on, breaking the mod between releases. · Likelihood: H · Impact: H
- Mitigation: Keep all host-game access inside CityBridge; record tested versions in [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md); follow [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md). · Indicator: a game update announcement; failures in smoke tests after updating. · Owner role: Coordinator · Phase: all · Status: Open

### RISK-008 — Performance degradation at street level
- Description: A street-level camera, local collision proxies, or extra entities may degrade frame time or simulation speed. · Likelihood: M · Impact: M
- Mitigation: Measured baselines ([BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md)); bounded proxies; optional visual enhancements ([SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md)). · Indicator: recurring frame-time regression above 10%. · Owner role: QA · Phase: PHASE-03, PHASE-12 · Status: Open

### RISK-009 — Avatar visuals or animation infeasible
- Description: A visible, animated avatar may not be achievable with authorized assets or available runtime representations. · Likelihood: M · Impact: H
- Mitigation: PHASE-04 spike comparing options (OPEN-010); documented substitute transitions; scope review if no option works. · Indicator: no authorized model or animation path found. · Owner role: Coordinator · Phase: PHASE-04 · Status: Open

### RISK-010 — Melty adds no value or conflicts with the workflow
- Description: Melty may not access the project folder, may impose packaging or launch constraints, or may conflict with the mod workflow. As of 2026-10-18, melty.gg lists neither CS2 nor Motor Town, and its Terms claim exclusivity for published content (CONF-012). · Likelihood: H · Impact: L (Melty is optional)
- Mitigation: Evaluate per [MELTY_EVALUATION](../research/MELTY_EVALUATION.md); keep the workspace and documentation independent of Melty. · Indicator: evaluation questions answered negatively. · Owner role: Coordinator · Phase: PHASE-00 · Status: Mitigating (2026-10-18): Melty is not adopted for distribution (ADR-0005, Proposed); the project stays independent of it

### RISK-011 — OneDrive synchronization interferes with development
- Description: The project root is inside OneDrive. Sync may lock files during builds, upload large artifacts or private local files, or conflict with Git metadata. · Likelihood: M · Impact: M
- Observation (2026-10-18): OneDrive manages the project files (reparse-point attributes); the sync mode was not confirmed.
- Mitigation: Keep build outputs and local files in git-ignored `artifacts/` and `.local/` and consider placing them outside the synced folder (OPEN-028); never store saves or game binaries in the project. · Indicator: sync conflicts, "file in use" errors, unexpectedly large uploads. · Owner role: Project owner · Phase: PHASE-00 · Status: Open

### RISK-012 — Scope creep from expansions into v1.0
- Description: Attractive post-1.0 features (interiors, native passengers, physical fleets) may slip into v1.0 and delay the foundation. · Likelihood: M · Impact: M
- Mitigation: [EXPANSIONS](../product/EXPANSIONS.md) separation; PRD change control. · Indicator: tasks referencing items without `REQ-*` IDs. · Owner role: Coordinator · Phase: all · Status: Open

### RISK-013 — Unverified work reported as complete
- Description: Agents or contributors may describe compiled-but-untested or planned work as finished. · Likelihood: M · Impact: H
- Mitigation: Three status dimensions, claim labels, evidence runs, [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md). · Indicator: a status change without a linked `RUN-*`. · Owner role: Reviewer · Phase: all · Status: Open

### RISK-014 — Uncoordinated concurrent edits to critical areas
- Description: Parallel changes to control authority, the entity lifecycle, or the save format may conflict and break invariants. · Likelihood: M · Impact: H
- Mitigation: Explicit file ownership and coordination rules ([AGENTS.md](../../AGENTS.md), [ROLES_AND_OWNERSHIP](../agents/ROLES_AND_OWNERSHIP.md)). · Indicator: two active tasks touching the same critical spec. · Owner role: Coordinator · Phase: all · Status: Open

### RISK-015 — Intellectual property or branding misuse
- Description: Using Motor Town, SimCity, or game assets, names, or branding beyond reference and fair description could create legal problems. · Likelihood: L · Impact: H
- Mitigation: Original systems and assets only; [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md); review of player-facing naming before release. · Indicator: any asset without a recorded provenance. · Owner role: Project owner · Phase: all · Status: Open

### RISK-016 — Conflicts with other installed mods
- Description: Other driving, camera, traffic, or UI mods may conflict with ours. · Likelihood: M · Impact: M
- Mitigation: Compatibility policy, conflict detection where possible, isolated test mod sets (OPEN-023, OPEN-035). · Indicator: failures that only appear with certain mod combinations. · Owner role: QA · Phase: PHASE-01, PHASE-12 · Status: Open

### RISK-017 — Removing the mod leaves saves unloadable
- Description: Saves created with the mod may fail to load, or may lose city data, after the mod is disabled or removed. · Likelihood: M · Impact: H
- Mitigation: Clean-exit procedure, tested early (REQ-SAVE-006, OPEN-022); no safe-uninstall claims before testing. · Indicator: a lab save fails to open without the mod. · Owner role: QA · Phase: PHASE-01, PHASE-13 · Status: Open

### RISK-018 — City edits invalidate missions or entities
- Description: Demolitions or road and line changes may leave missions, vehicles, or home bases in impossible states. · Likelihood: H · Impact: M
- Mitigation: Revalidation on return from Building mode, no-fault cancellation, rerouting ([SPEC-CITY](../architecture/CITY_INTEGRATION.md)). · Indicator: missions that never complete after an edit. · Owner role: Coordinator · Phase: PHASE-08, PHASE-10 · Status: Open

### RISK-019 — Economic softlock
- Description: The player may end up with no money, no usable vehicle, or no reachable service, and be unable to continue. · Likelihood: M · Impact: H
- Mitigation: Starter job, basic rental, recovery transport, explained alternatives ([SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md)). · Indicator: playtest reaching a state with no available action. · Owner role: QA · Phase: PHASE-07 · Status: Open

### RISK-020 — Input conflicts with game shortcuts and tools
- Description: Mod key bindings may trigger city-building tools or game shortcuts, or be swallowed by the game. · Likelihood: M · Impact: M
- Mitigation: Input routing by state ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md)); remapping ([SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md)); OPEN-014. · Indicator: a tool activates while driving. · Owner role: Implementer · Phase: PHASE-02 · Status: Open

### RISK-021 — Toolchain version mismatch
- Description: Using SDK, Unity, or reference versions that differ from what the game and official toolchain expect may break builds or runtime behavior. · Likelihood: M · Impact: M
- Mitigation: Record confirmed versions only in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md); never upgrade a target framework just to satisfy a tool (historical input). · Indicator: build warnings about framework or assembly versions. · Owner role: Implementer · Phase: PHASE-00, PHASE-01 · Status: Open

### RISK-022 — Time and pause inconsistencies
- Description: Pausing, mode switching, or save/load may duplicate or skip time-based income and penalties, or enable exploits. · Likelihood: M · Impact: M
- Mitigation: Persisted time checkpoints and settlement rules ([SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md)). · Indicator: company balance changes while paused, or a jump after load. · Owner role: QA · Phase: PHASE-03, PHASE-09 · Status: Open

### RISK-023 — The core loop is not fun
- Description: Walking and driving inside CS2 may feel unconvincing even if technically correct. · Likelihood: M · Impact: H
- Mitigation: Fun checkpoint at M2 before multiplying content; playtest notes recorded as evidence. · Indicator: owner or tester feedback at M2. · Owner role: Project owner · Phase: PHASE-06 · Status: Open

### RISK-024 — Unknown project capacity for a long roadmap
- Description: Team size, availability, and timeline are not defined. Fourteen phases may outlast available effort. · Likelihood: M · Impact: M
- Mitigation: Milestones produce usable intermediate results (M1 sandbox, M2 MVP); no dates promised. · Indicator: phases stalled without recorded blockers. · Owner role: Project owner · Phase: all · Status: Open

### RISK-025 — Distribution platform constraints
- Description: The chosen distribution platform may impose packaging, review, content, or dependency rules not yet known. · Likelihood: M · Impact: M
- Mitigation: Read-only research of publishing requirements before PHASE-13 (OPEN-017); [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md). · Indicator: requirements that conflict with design decisions. · Owner role: Coordinator · Phase: PHASE-13 · Status: Open

### RISK-026 — Limited debugging on the retail game
- Description: Runtime debugging tools may require special development builds that are unavailable for the commercial game. The community `unity-devtools` README states that it needs a Unity Mono development build and that "a retail build exposes no SDB port" (documented upstream, extraction-level, 2026-10-18). · Likelihood: M · Impact: M
- Mitigation: Strong logging and diagnostics ([DEBUGGING](../engineering/DEBUGGING.md)); verify tool requirements (OPEN-031). · Indicator: debugger cannot attach in TASK-0003. · Owner role: Implementer · Phase: PHASE-00, PHASE-01 · Status: Open

### RISK-027 — Historical planning inputs contain inaccurate claims
- Description: [chat.md](../archive/chat.md) is truncated at its start and its citations were stripped. Claims such as the base mod's license, its missing building collisions, and its target framework may be outdated or wrong. · Likelihood: M · Impact: M
- Mitigation: Treat all of it as **[Historical input]**; re-verify through [SOURCES](../research/SOURCES.md) and the audits. · Indicator: a contradiction found during audit. · Owner role: Coordinator · Phase: PHASE-00 · Status: Open

### RISK-028 — Damage to primary saves or the game installation during testing
- Description: Tests could overwrite or corrupt the owner's real saves or modify game files. · Likelihood: L · Impact: H
- Mitigation: Lab cities and copies only, never primary saves; no changes to game installation files; owner authorization for setup and runtime operations ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)). · Indicator: any task that requires touching primary saves. · Owner role: Project owner · Phase: PHASE-00 onwards · Status: Open

### RISK-029 — Accidental redistribution of proprietary game content
- Description: Packages, evidence, or the repository could include game DLLs, extracted assets, or other proprietary files. · Likelihood: L · Impact: H
- Mitigation: Ignore rules, packaging review, and asset provenance; never copy game binaries into the project. · Indicator: binary or asset files appearing in the project tree. · Owner role: Reviewer · Phase: all · Status: Open

### RISK-030 — Personal data in logs or evidence
- Description: Logs, screenshots, or diagnostics may contain user names, profile paths, account identifiers, or store IDs. · Likelihood: M · Impact: M
- Mitigation: Sanitization rules ([DEBUGGING](../engineering/DEBUGGING.md), [evidence](../quality/evidence/README.md), [SECURITY.md](../../SECURITY.md)). · Indicator: unsanitized paths in a committed evidence file. · Owner role: QA · Phase: all · Status: Open

## 3. Open questions

### OPEN-001 — Installed game edition, version, platform, and DLCs
- Question: Which CS2 edition, version, store platform, and DLCs are installed, and where are the game and its user data (mods, logs, saves) located?
- Impact: All compatibility, toolchain, and test decisions depend on it.
- Verification method: Read-only discovery on the owner's machine with owner permission (TASK-0001). Record the results in a local, git-ignored file and in sanitized form in [WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md).
- Answer (2026-10-18, TASK-0001, read-only): Steam, app 949230. Installed in the standard Steam library folder `C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II`. Game version **1.6.2f1** (`launcher-settings.json`), Unity 2022.3.71f1 with a Mono runtime. User data under `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II`, where saves and logs exist (not opened). Edition and DLCs are not determined (owner question). The in-game version string is to be confirmed in TASK-0003.
- Resolving phase/task: TASK-0001 (PHASE-00) · Status: Partly answered (2026-10-18)

### OPEN-002 — Official CS2 modding toolchain components and versions
- Question: What does the official modding toolchain consist of today, which versions of its components (Unity, .NET SDK, IDE, templates) does it require, and what does it install?
- Impact: Determines how to build both the base mod and our mod (RISK-021).
- Verification method: Read official documentation ([SOURCES](../research/SOURCES.md)); detect installed components read-only (TASK-0001); record confirmed versions in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
- Findings so far (2026-10-18, read-only, not verification): The official Paradox wiki toolchain page **[Documented upstream]** (verified only for game 1.1.12f1) lists Unity Editor 2022.3.7f1 with a Unity license, .NET SDK 6 or later (8 recommended), Node.js 18 or later for UI mods, and Visual Studio 2022 or Rider. It also lists `CSII_*` environment variables and a post-build processor. Whether these versions still apply to the installed game is unknown.
- Installed side (2026-10-18, TASK-0001): the official toolchain is **not installed** (no `CSII_*` variables, no Unity Editor or Hub, no `dotnet` SDK). Visual Studio Build Tools 2022 (17.14) and Visual Studio Community 2026 (18.10), with MSBuild, are present; Rider is not. See [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
- Resolving phase/task: TASK-0001, TASK-0002 · Status: Partly answered (installed side, 2026-10-18); required versions for 1.6.2f1 still open

### OPEN-003 — Mod runtime target framework
- Question: Which .NET target framework must a CS2 code mod use? The historical input reports `net48` for the base mod.
- Impact: Project configuration; testability of pure career logic; tool choice.
- Verification method: Inspect the base mod's project file and the official template documentation (TASK-0002); confirm by a successful isolated build (TASK-0003).
- Findings so far (2026-10-18, read-only, not verification):
  - The base mod's project file declares `net48` and C# 9 **[Source-inspected, raw text verified]**.
  - A community agent-plugin reference states that the toolchain's `Mod.props` fixes `net48` **[Documented upstream, community]**.
  - No official page consulted states the target framework. Local confirmation is still required.
- Source side (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): SDK-style project, `net48`, `LangVersion` 9.0, nullable and implicit usings disabled, no toolchain import **[Source-inspected]**. Build confirmation remains TASK-0003.
- Resolving phase/task: TASK-0002, TASK-0003 · Status: Open (source side answered 2026-10-18)

### OPEN-004 — Base mod license, revision, dependencies, and supported game version
- Question: What are the exact license and copyright holders, which revision should be pinned, what does it depend on, and which game version does it claim to support?
- Impact: Legal reuse (RISK-002) and technical reuse (RISK-001).
- Verification method: Read-only source audit ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), TASK-0002).
- Findings so far (2026-10-18, preliminary documentation-time review; TASK-0002 still required):
  - LICENSE is MIT, "Copyright (c) 2026 stoot" **[Source-inspected, raw text verified]**.
  - Default-branch head is `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (2026-06-17). It is a candidate audit revision only; it has not been adopted.
  - The project file references game assemblies only; no package references or toolchain imports were found.
  - The publish configuration declares `GameVersion 1.5.*`.
  - Open provenance questions remain: an excluded `decompiled_published_0317` folder, an empty assembly copyright field, and a relicensing commit whose earlier license was not inspected.
- Answer (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): MIT at the pin, "Copyright (c) 2026 stoot". The first 4 commits (2026-05-30) carried a restrictive "Source-Available Transparency License"; MIT applies from commit `af355236521636e987801e4c14019aaf187ec277` (2026-05-31). NOTICE excludes trademarks, game assets, and third-party content. There are no per-file headers, and `AssemblyCopyright` is empty. Dependencies: 18 game-assembly references only, with no packages, no mod dependencies, and no Harmony or BepInEx. Declared `GameVersion` is "1.5.*", with no runtime version check. Residual provenance items are in OPEN-404, OPEN-405, and OPEN-411. See [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9 and [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md).
- Resolving phase/task: TASK-0002 · Status: Answered (2026-10-18, TASK-0002)

### OPEN-005 — Does the base mod build and run, and does handback work?
- Question: Does the pinned base mod build reproducibly, load in the installed game, drive a vehicle, and return it to native AI cleanly?
- Impact: Decides between reuse and re-implementation of vehicle control.
- Verification method: Isolated baseline in a lab city with owner-authorized setup ([TASK-0003](tasks/TASK-0003-isolated-baseline-validation.md)).
- Resolving phase/task: TASK-0003 · Status: Open

### OPEN-006 — Persistence mechanism and save identity
- Question: Can a code mod store custom data inside the CS2 save, or is a revision-bound sidecar (or a hybrid) required? How can city identity and save revision be determined reliably?
- Impact: REQ-SAVE-001 to REQ-SAVE-006; the core no-leak guarantee.
- Verification method: Read the official documentation; inspect how existing open-source mods persist data (read-only); then run a PHASE-01 spike in a lab city comparing options against [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md); decide in [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md).
- Findings so far (2026-10-18, read-only, not verification):
  - Community references (not official; verified by their authors for game builds around 1.5.x–1.6.2f1) describe mod data being serialized into saves through component and system serialization interfaces. They recommend writing a mod-owned version number.
  - No official Paradox page on save serialization was found.
  - The base mod persists nothing in saves.
  - The in-save option therefore looks plausible but is **[Unverified]**.
- Resolving phase/task: PHASE-01 · Status: Open

### OPEN-007 — Surface information at runtime
- Question: What reliable ground, road, bridge, tunnel, and elevated-road surface information can the mod query at runtime (height, normal, surface type, level)?
- Impact: REQ-WORLD-001, REQ-PLAYER-002, REQ-DRIVE-002.
- Verification method: PHASE-03 laboratory with a fixed set of surface scenarios; record findings in [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md).
- Resolving phase/task: PHASE-03 · Status: Open

### OPEN-008 — Building and static-obstacle collision data
- Question: Do buildings and static props expose collision geometry usable at runtime? If not, can bounded local proxies be generated from available data?
- Impact: REQ-WORLD-002, REQ-WORLD-003, REQ-PLAYER-004 (RISK-003).
- Verification method: PHASE-03 laboratory spikes; measure the memory and time cost of any proxies.
- Resolving phase/task: PHASE-03 · Status: Open

### OPEN-009 — Physics approach
- Question: Which movement approach gives consistent semi-realistic handling: an enhanced kinematic controller with surface, grip, and collision queries, or a local physical controller?
- Impact: REQ-DRIVE-002, REQ-DRIVE-003, REQ-TIME-003.
- Verification method: Implement both minimally on one car in the PHASE-03 laboratory and compare them against the criteria in [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md). Only one movement authority may be kept.
- Resolving phase/task: PHASE-03 · Status: Open

### OPEN-010 — Avatar representation and animation
- Question: Can the avatar use own or authorized assets, or a runtime representation available in the game, and are suitable animations available?
- Impact: REQ-PLAYER-001, REQ-ART-001 (RISK-009).
- Verification method: PHASE-04 spike comparing options, with a license and provenance check for each ([ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md)).
- Resolving phase/task: PHASE-04 · Status: Open

### OPEN-011 — Clean takeover and handback of native vehicles
- Question: How can a native vehicle be taken over and returned so that its route is rebuilt and lane reservations and blocking state are cleared?
- Impact: REQ-ENTITY-001, REQ-CTRL-001 (RISK-004).
- Verification method: Inspect the base mod's approach (TASK-0002), observe its baseline (TASK-0003), then run PHASE-02 route tests with traffic observation.
- Source side (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): on takeover the mod clears navigation lanes and path elements and sets the path owner to pending. Every tick, a freeze system pins a zero-speed navigation target, and a control system writes position and velocity after the native move step while keeping the car in the nearest lane's object list. On release it removes its lane-object entries, snaps the current lane with an obsolete flag, sets a short navigation target, and marks the path owner obsolete. There is **no stop requirement, no verification, and no handback on save, load, or dispose**. Runtime behavior is still unobserved.
- Resolving phase/task: TASK-0002, TASK-0003, PHASE-02 · Status: Open (source side answered 2026-10-18; TASK-0003 observation, PHASE-02 tests)

### OPEN-012 — Safe spawning and removal of career vehicles
- Question: How can the mod spawn rental and owned vehicles that traffic respects, and remove them without residual entities?
- Impact: REQ-ENTITY-002, REQ-ENTITY-003, REQ-GARAGE-001.
- Verification method: PHASE-02 and PHASE-05 spikes with entity-count checks before and after.
- Resolving phase/task: PHASE-02, PHASE-05 · Status: Open

### OPEN-013 — Simulation speed and pause control
- Question: Can the mod read, set, and restore simulation speed and detect pause in a supported way?
- Impact: REQ-TIME-001, REQ-TIME-002.
- Verification method: Documentation review, then a PHASE-03 spike.
- Findings so far (2026-10-18, read-only, not verification): A community handbook describes reading and setting the user-selected simulation speed through a game system and warns that the paused value needs in-game verification **[Documented upstream, community]**. No official page was found.
- Resolving phase/task: PHASE-03 · Status: Open

### OPEN-014 — Input integration
- Question: How can the mod capture character-mode input without triggering city-building tools, and integrate with the game's key-binding or remapping system?
- Impact: REQ-CTRL-003, REQ-INPUT-001 (RISK-020).
- Verification method: Inspect the base mod's input handling (TASK-0002); PHASE-02 spike; PHASE-11 remapping work.
- Findings so far (2026-10-18, read-only, not verification):
  - The official wiki documents mod key bindings declared through the game's settings and binding system, built on the Unity Input System. It states that conflicting mod bindings are disabled **[Documented upstream]**, verified for an older game build.
  - The base mod instead reads legacy Unity input and stores key bindings in player preferences **[Source-inspected]**.
- Source side (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): the base mod polls legacy Unity input every frame in every context and stores `KeyCode` bindings in player preferences. It does not use the game's input system or settings bindings, does not suppress game input, handles no focus, and has no gamepad support. It offers nothing reusable toward REQ-CTRL-003 or REQ-INPUT-001.
- Resolving phase/task: PHASE-02, PHASE-11 · Status: Open (source side answered 2026-10-18; PHASE-02 spike)

### OPEN-015 — UI technology for HUD and menus
- Question: Which UI technology can a CS2 code mod use for its HUD and menus? The historical input mentioned `coherent-gameface` inspection tooling; this is unverified.
- Impact: REQ-UI-001 to REQ-UI-004, REQ-L10N-001.
- Verification method: Official documentation and community references (read-only); a PHASE-01 spike drawing one HUD element.
- Findings so far (2026-10-18, read-only, not verification): The official wiki documents UI modding with Coherent Gameface, React, TypeScript, and a module registry with insertion hooks **[Documented upstream]**, verified for game 1.5.7f1. The base mod uses that registry approach plus a fallback debug panel **[Source-inspected]**.
- Resolving phase/task: PHASE-01, PHASE-11 · Status: Open

### OPEN-016 — Melty capabilities and fit
- Question: How does Melty access project files, how is it invoked, how does it launch or package outputs, what permissions does it need, and does it conflict with the mod workflow?
- Impact: Whether Melty is used at all (RISK-010). It never blocks the project.
- Verification method: Documentation review first, then an owner-attended, observation-only session following [MELTY_EVALUATION](../research/MELTY_EVALUATION.md).
- Findings so far (2026-10-18, read-only, not verification):
  - melty.gg describes a free Windows launcher whose creator flow is "copy Melty's instructions" into Claude Code or Codex.
  - It documents no project-folder selector and no built-in agent.
  - It does **not** list Cities: Skylines II or Motor Town **[raw text verified]**.
  - Its Terms grant Melty an exclusive license for published content (CONF-012).
- Answer (2026-10-18, read-only Melty tools with owner authorization): the connection works. CS2 is in Melty's catalog with **BepInEx 5 as its only loader**, installed into the game folder. A native CS2 code mod cannot be installed one-click by Melty, because the `LocalLow` destination is rejected and manual mode is not publishable. Motor Town is not in the catalog, and a CS2-only listing is accepted. See [MELTY_EVALUATION](../research/MELTY_EVALUATION.md) §9.
- Resolving phase/task: PHASE-00 (separate evaluation task, not yet created) · Status: Answered for capabilities (2026-10-18); adoption decision pending (project owner)

### OPEN-017 — Distribution platform requirements
- Question: What packaging, metadata, review, and dependency rules apply to publishing a CS2 code mod on the intended platform?
- Impact: PHASE-13 (RISK-025).
- Verification method: Read-only research recorded in [SOURCES](../research/SOURCES.md) and [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md); a private or test publication only with owner authorization.
- Findings so far (2026-10-18, read-only, not verification): The official wiki documents publishing to Paradox Mods from the IDE through a publish configuration file and a Paradox account login. A first publication returns a mod ID; later updates publish new versions **[Documented upstream]**. Moderation and review rules were not found, because the Paradox Mods site returned only a script shell to the fetch tool.
- Resolving phase/task: PHASE-13 (research may start earlier) · Status: Open

### OPEN-018 — Determining reachable access points
- Question: How can the mod find valid curbside, parking, loading, or stop positions for a building, rather than its geometric center?
- Impact: REQ-CAREER-003, REQ-HOME-002.
- Verification method: PHASE-06 spike over a sample of building types in the lab city.
- Resolving phase/task: PHASE-06 · Status: Open

### OPEN-019 — Route and connectivity validation
- Question: Should mission validation query native pathfinding or a mod-side road-graph search, and what does each cost?
- Impact: REQ-CAREER-002, REQ-CHAL-003.
- Verification method: PHASE-06 spike comparing correctness on disconnected-road scenarios, and comparing cost.
- Resolving phase/task: PHASE-06 · Status: Open

### OPEN-020 — Reading transit lines and stops
- Question: Can the mod read the city's bus lines, stop order, and stop access positions reliably?
- Impact: REQ-BUS-001, REQ-BUS-002.
- Verification method: PHASE-08 spike in a lab city with modified lines.
- Resolving phase/task: PHASE-08 · Status: Open

### OPEN-021 — Detecting city edits
- Question: How can the mod detect demolitions, road changes, and line changes made in Building mode, so that it can revalidate?
- Impact: REQ-CITY-002, REQ-CAREER-006.
- Verification method: PHASE-10 spike; a fallback of full revalidation on every return from Building mode.
- Resolving phase/task: PHASE-10 · Status: Open

### OPEN-022 — Saves after the mod is disabled or removed
- Question: Does a save created with the mod load correctly after the mod is disabled or removed, and what mod data remains?
- Impact: REQ-SAVE-006, REQ-REL-002 (RISK-017).
- Verification method: Lab test in PHASE-01 (first skeleton) and again in PHASE-13 (release candidate).
- Findings so far (2026-10-18, read-only, not verification): A community reference states that uninstalling a mod loses that mod's serialized components and keeps the city **[Documented upstream, community]**. This is untested here, and no safe-uninstall claim may be based on it.
- Resolving phase/task: PHASE-01, PHASE-13 · Status: Open

### OPEN-023 — Conflicts with other mods
- Question: Which commonly used driving, camera, traffic, or UI mods conflict with ours, and can conflicts be detected at load time?
- Impact: REQ-COMPAT-001 (RISK-016).
- Verification method: A conflict check against the original base mod in PHASE-01; a compatibility sweep in PHASE-12.
- Resolving phase/task: PHASE-01, PHASE-12 · Status: Open

### OPEN-024 — Street-level rendering quality and cost
- Question: How do level-of-detail, culling, and lighting behave at walking and driving camera distances, and what does improving them cost?
- Impact: REQ-PERF-002, player experience (RISK-008).
- Verification method: Captures and measurements following [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md) in PHASE-03 and PHASE-04; options evaluated in PHASE-11.
- Resolving phase/task: PHASE-03, PHASE-04, PHASE-11 · Status: Open

### OPEN-025 — Gamepad support in the mod's input context
- Question: Can the mod read gamepad input for walking, driving, and menus through supported means?
- Impact: REQ-INPUT-002.
- Verification method: An early probe during PHASE-02; full support in PHASE-11.
- Resolving phase/task: PHASE-02, PHASE-11 · Status: Open

### OPEN-026 — Localization mechanism for mod strings
- Question: How do CS2 code mods supply localized strings, and can the mod switch between English and PT-BR with the game's language setting?
- Impact: REQ-L10N-001.
- Verification method: Documentation review; a PHASE-01 spike with one externalized string.
- Resolving phase/task: PHASE-01, PHASE-11 · Status: Open

### OPEN-027 — Project license
- Question: Under which license will Drive the City II be published, given any incorporated components?
- Impact: REQ-REL-003; contribution and reuse terms. The project is **not** assumed to be MIT.
- Verification method: The project owner decides after [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) and before incorporating third-party code or distributing anything.
- Answer (2026-10-20, owner decision): **MIT License**, copyright holder "michelbr84" ([LICENSE](../../LICENSE)), chosen when the owner decided to make the GitHub repository public. No third-party code is incorporated ([THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md)); contributions are accepted under the same license ([CONTRIBUTING](../../CONTRIBUTING.md) §8).
- Resolving phase/task: Project owner decision; before the first incorporation (PHASE-01) and confirmed in PHASE-13 · Status: Resolved (2026-10-20)

### OPEN-028 — Project location inside OneDrive
- Question: Should build outputs, caches, and local files stay inside the OneDrive-synced project root, or be redirected outside it? How does sync interact with Git?
- Impact: RISK-011; build reliability; private data exposure.
- Verification method: Owner decision informed by TASK-0001 observations (sync status, available disk space); document the result in [WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md).
- Observations (2026-10-18, TASK-0001): the project files carry OneDrive reparse-point attributes, and drive C: has about 1.5 TB free. The owner still decides where build outputs and local files live.
- Resolving phase/task: TASK-0001, then an owner decision before PHASE-01 code work · Status: Open (owner decision; observations recorded 2026-10-18)

### OPEN-029 — Version control initialization and hosting
- Question: When should the Git repository be initialized, and will a remote host be used?
- Impact: Commit-based evidence, upstream pinning, collaboration. Git `2.55.0.windows.5` is installed, but the project root is not a repository (observed 2026-10-18).
- Verification method: Project owner decision. Initializing the repository is a separate, authorized task; the `.gitignore` file prepared in the baseline is already in place.
- Answer: the repository was initialized under the owner brief of 2026-10-19 (first commit `09e80ac`, 2026-10-19). On 2026-10-20 the owner created <https://github.com/michelbr84/DriveTheCityII> (private at first), added it as `origin`, and pushed `main`; the owner then decided to make it public. Commits use the GitHub `noreply` address.
- Resolving phase/task: Project owner decision before PHASE-01 code work · Status: Resolved (2026-10-20)

### OPEN-030 — Need for a Motor Town installation
- Question: Is installing or running Motor Town needed at all, given that it is only a design reference and is never required at runtime?
- Impact: Development cost; the Melty evaluation (Melty may require selecting installed games).
- Verification method: Project owner decision after the Melty documentation review.
- Answer (2026-10-18, TASK-0001): Motor Town is already installed through Steam (app 1369670). No task or gate needs it, and it was not launched.
- Resolving phase/task: PHASE-00 · Status: Partly answered (2026-10-18): installed, not needed; owner decides on optional play notes

### OPEN-031 — Debugging capabilities on the retail game
- Question: Which debugging tools work against the installed retail game, and what do the tools named in the historical input (`unity-devtools`) actually require?
- Impact: Development speed (RISK-026).
- Verification method: Read the tool documentation ([SOURCES](../research/SOURCES.md)); confirm during TASK-0003 if authorized.
- Findings so far (2026-10-18, read-only, not verification): The community `unity-devtools` plugin README states that it requires a Unity Mono development build and that "a retail build exposes no SDB port". The `coherent-gameface` plugin needs a reachable browser-debugging endpoint, and how to enable that in CS2 is not documented there **[Documented upstream, community]**. Nothing was installed.
- Presence (2026-10-18, TASK-0001): `dnx` and a .NET 10 SDK, which `unity-devtools` needs, are absent. The retail game is not a Unity development build. Nothing was installed.
- Resolving phase/task: TASK-0002, TASK-0003 · Status: Partly answered (presence, 2026-10-18); runtime check in TASK-0003 if authorized

### OPEN-032 — Contact rule with native traffic and pedestrians
- Question: What happens when the controlled vehicle meets native vehicles or pedestrians (blocking, push, pass-through with feedback, avoidance)? Which rule is safe for native agents?
- Impact: REQ-WORLD-004; driving feel.
- Verification method: PHASE-03 laboratory comparing candidate rules; decided in [SPEC-CONTACT](../architecture/WORLD_CONTACT.md).
- Resolving phase/task: PHASE-03 · Status: Open

### OPEN-033 — Code-mod support of the installed store edition
- Question: Does the owner's store edition of CS2 support code mods through the documented toolchain and platform?
- Impact: Blocks PHASE-00 if unsupported.
- Verification method: Compare TASK-0001 observations with the official documentation.
- Answer (2026-10-18): the installed edition is the Steam PC version. Official sources document code mods for PC through the in-game toolchain and Paradox Mods **[Documented upstream]**. Runtime confirmation needs TASK-0003.
- Resolving phase/task: TASK-0001 · Status: Answered (documented upstream, 2026-10-18); runtime confirmation pending TASK-0003

### OPEN-034 — Vehicle models for career vehicles
- Question: May career vehicles reuse the game's own vehicle models at runtime (without redistributing them), or are new authorized models needed?
- Impact: REQ-DRIVE-003, REQ-GARAGE-002; [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md).
- Verification method: Check the platform's and publisher's mod rules (read-only); run a PHASE-02 spawn spike.
- Resolving phase/task: PHASE-02, PHASE-07 · Status: Open

### OPEN-035 — Isolated testing setup
- Question: How can mods be tested in isolation (a separate mod set or profile, lab city, copies of saves) without touching the owner's primary saves or active mods?
- Impact: RISK-028; a precondition for TASK-0003.
- Verification method: Read-only discovery of the game's mod and profile management (TASK-0001) and official documentation; an owner-approved setup procedure.
- Read-only findings (2026-10-18, TASK-0001): no mods are installed (`.cache\Mods\local` and `.cache\Mods\pdx_mods` are empty), and playset files exist. A proposed isolated setup is in [WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md) §8.
- Resolving phase/task: TASK-0001, TASK-0003 · Status: Open (proposed setup awaiting owner approval, 2026-10-18)

### OPEN-036 — Native local-mods folder on game 1.6.2f1
- Question: On game 1.6.2f1, where does the official toolchain deploy local mods? The official page (verified for 1.1.12f1) documents a root-level `Mods` folder in the user-data folder. On this machine that folder does not exist, while `.cache\Mods\local` does (empty).
- Impact: The isolated test setup (OPEN-035), TASK-0003 deployment, and the install documentation for players.
- Verification method: Read `CSII_LOCALMODSPATH` once the toolchain is installed (TASK-0003). Confirm by deploying the baseline build in the lab session and observing it load.
- Resolving phase/task: TASK-0003 · Status: Open

## 4. Recorded conflicts

### CONF-001 — Project root location
- Positions: The historical input suggested an example project folder on drive D:. The owner brief sets `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII` as the authoritative root.
- Resolution: The owner brief wins (2026-10-18). The OneDrive implications are tracked in OPEN-028 and RISK-011. · Status: Resolved

### CONF-002 — Documentation language
- Positions: The historical input allowed Portuguese document content. The owner brief requires English for all active documentation.
- Resolution: English (2026-10-18). [chat.md](../archive/chat.md) remains unchanged as a Portuguese historical input. The game interface remains bilingual (REQ-L10N-001). · Status: Resolved

### CONF-003 — Articulated trucks
- Positions: The historical roadmap gave articulated trucks "their own prototype" in phase 8 and also listed them under post-1.0 advanced logistics. The owner brief excludes articulated-truck physics from v1.0.
- Resolution: Articulated trucks are an expansion ([EXPANSIONS](../product/EXPANSIONS.md)). PHASE-08 may include an optional, non-gating exploratory spike only if the project owner authorizes it. · Status: Resolved

### CONF-004 — Documentation file lists
- Positions: The historical roadmap listed a root-level documentation set (including a single compatibility file). The later historical folder structure and the owner manifest place documents under `docs/` and split compatibility into a matrix (quality) and a policy (release).
- Resolution: The owner manifest wins ([INDEX](../INDEX.md)). · Status: Resolved

### CONF-005 — Documentation required before the first audit
- Positions: One historical answer required a full pre-audit document set. A later one placed only the roadmap and structure in the folder before running an audit prompt.
- Resolution: A full documentation baseline is created first (TASK-0000, 2026-10-18). Audits follow as separate tasks. · Status: Resolved

### CONF-006 — Tool installation order
- Positions: The historical preparation checklist installed Git and .NET tools "after the compatibility audit", while the same audit was asked to build the base mod.
- Resolution: TASK-0001 and TASK-0002 are read-only and need no installs. TASK-0003 (build and run) requires explicit owner authorization for any installation, based on TOOLCHAIN_MATRIX findings. · Status: Proposed resolution (awaiting owner approval of TASK-0003)

### CONF-007 — Rental and wallet needed before the progression phase
- Positions: The historical taxi loop (phase 6) rents a car and pays money and experience, but the rental, wallet, and experience systems were introduced in phase 7.
- Resolution: PHASE-06 includes a minimal slice: basic rental, wallet, ledger, and taxi experience. PHASE-07 expands it into full progression and ownership. · Status: Proposed resolution

### CONF-008 — Melty's "mashup" model versus a CS2-only runtime
- Positions: Melty is described as combining selected games, but the project keeps CS2 as the only runtime and imports nothing from Motor Town. The historical plan also assumed that CS2 and Motor Town could be selected in Melty. As of 2026-10-18, melty.gg's homepage lists neither game (raw text verified). Melty's connected catalog lists CS2 (BepInEx 5 only) but not Motor Town, and it accepts a CS2-only listing (MELTY_EVALUATION §9).
- Resolution: Evaluate Melty strictly as an optional workflow aid (OPEN-016). The project stays independent of it. · Status: Open

### CONF-009 — Installing and launching games and Melty during preparation
- Positions: The historical checklist asked to install and run Motor Town and Melty during preparation. The owner brief forbids launching games or Melty during the documentation task.
- Resolution: Nothing is launched in TASK-0000. Any later installation or launch needs explicit owner authorization (OPEN-030, OPEN-016). · Status: Resolved

### CONF-010 — Scope of the first agent session
- Positions: The historical phase-0 prompt asked one session to inspect the environment and also build and run a baseline.
- Resolution: The work is split into TASK-0001 (Windows discovery), TASK-0002 (source audit), and TASK-0003 (isolated baseline, which requires authorization). · Status: Resolved

### CONF-011 — Native bus passengers
- Positions: The historical plan described a second level of bus integration with native passengers, fares, and schedules.
- Resolution: v1.0 uses city lines and stops with career passengers only. Native integration is a separate experiment ([EXPANSIONS](../product/EXPANSIONS.md)). · Status: Resolved

### CONF-012 — Melty exclusivity versus multi-channel distribution
- Positions: The historical plan named Paradox Mods as the primary distribution channel to validate and Melty as an additional channel. Melty's Terms (last updated 2026-10-18, raw text verified) state that "Melty is the only marketplace for your Content" and that publishing grants Melty an exclusive license, lasting while the content is listed and for 12 months after removal.
- Resolution: Open. This is not a legal conclusion. The project owner (with legal advice if needed) must decide before anything is published on Melty. Until then, Melty is not a distribution channel, and Paradox Mods remains the candidate primary channel ([PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md), [MELTY_EVALUATION](../research/MELTY_EVALUATION.md)). See also OPEN-701. · Status: Open

### CONF-013 — Per-career tutorials as a PHASE-08 gate
- Positions: The historical phase-8 criterion required a tutorial for every career. The owner brief's PHASE-08 gate requires reachable contracts, valid failure paths, and persistence, and no PRD requirement covers per-career tutorials (REQ-ONBOARD-001 covers the first job only).
- Resolution: Per-career introductions are specified in [SPEC-ONBOARD](../experience/ONBOARDING.md) as design guidance and do not gate PHASE-08. The project owner may add a requirement through PRD change control (proposed as OPEN-509; candidate test TEST-ONBOARD-102). · Status: Proposed resolution

### CONF-014 — Melty's BepInEx-only CS2 route versus the native code-mod route
- Positions: Melty's generic creator prompt and its catalog give BepInEx 5 (installed into the game folder) as the CS2 loader. The official toolchain, the candidate base mod's entry point, and Melty's own modding-kit note use the game's native code-mod loading. The project forbids modifying the game installation.
- Resolution: Native route proposed in [ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md). No BepInEx is installed, and Melty is not a distribution channel unless it supports the native route and CONF-012 is resolved. Runtime confirmation of the native route on 1.6.2f1 is in TASK-0003. · Status: Proposed resolution (awaiting owner acceptance of ADR-0005)

## 5. Entries contributed by document groups (2026-10-18 baseline)

Ranges: architecture 101–129 · gameplay 201–229 · product and experience 301–329 · research, engineering, and legal
401–429 · quality 501–519 · planning 601–619 · agents, release, and user 701–719.

Entries below were contributed by the document groups on 2026-10-18 and merged by the Coordinator. Unless stated
otherwise, every entry has status **Open**. Duplicates are kept, marked as merged, and point to the primary entry.

**Architecture group (101–129)**

### OPEN-101 — Host lifecycle events observable by the mod
- Question: Which host lifecycle events can the mod observe, and early enough to act on: city load start and end, save start and end (including autosave), quit to main menu, game exit, and mod disable?
- Impact: Loading/Unloading overlays and orphan sweeps (SPEC-CONTROL, SPEC-ENTITY); revision-bound career writes and save-revision IDs (SPEC-SAVE, ADR-0003); handling a host save while a native vehicle is borrowed in sandbox mode.
- Verification method: Read official and community documentation read-only; inspect how the candidate base mod and other open-source mods react to load/save/unload (read-only, TASK-0002); then a PHASE-01 lab spike that logs every observable event during load, manual save, autosave, quit to menu, and disable, in a lab city.
- Resolving phase/task: PHASE-01 (input from TASK-0002)
- Raised in: docs/architecture/ARCHITECTURE.md (also SPEC-CONTROL, SPEC-ENTITY, SPEC-SAVE, ADR-0003)
- Status: Open

### OPEN-102 — Detecting window focus loss and host menus
- Question: Can the mod detect that the game window lost focus, and that a host menu (pause menu, options) is open, in a supported way?
- Impact: REQ-TIME-002 (inputs cleared on focus loss), Paused/Menu overlay semantics, auto-pause setting (SPEC-TIME, SPEC-CONTROL).
- Verification method: Documentation review; PHASE-02 early probe logging focus and menu changes; confirm in the PHASE-03 time/focus scenarios (alt-tab, pause menu, options menu).
- Resolving phase/task: PHASE-02 (probe), PHASE-03 (decision)
- Raised in: docs/architecture/GAME_TIME_AND_PAUSE.md
- Status: Open

### OPEN-103 — Stable references to native city objects across save/load
- Question: Do buildings, road segments, transit lines, and stops have identifiers that stay stable across save and reload (and across game updates)? If not, what descriptor (position, type, frontage, name) re-resolves them reliably?
- Impact: REQ-SAVE-003 (stable references), persisted access points, home base, service points, mission destinations, challenge routes (SPEC-SAVE, SPEC-CITY).
- Verification method: PHASE-01 lab spike: record candidate identifiers and descriptors for a sample of objects, save, reload (and restart the game), compare; repeat after a minor city edit; record findings in CS2_INTEGRATION_FINDINGS.
- Resolving phase/task: PHASE-01 (spike), confirmed in PHASE-06
- Raised in: docs/architecture/SAVE_FORMAT.md
- Status: Open

### OPEN-104 — Clock basis for career timers and company settlement
- Question: Should mission deadlines and company income/costs advance with simulation (in-game) time, or with career-active time (time spent in character mode, unpaused)? If simulation time, how are exploits from fast-forwarding in Building mode prevented without skipping periods?
- Impact: REQ-TIME-004, REQ-COMPANY-003, balancing and player expectations (SPEC-TIME, SPEC-COMPANY, SPEC-MISSION).
- Verification method: Design decision by the project owner informed by a logic-test prototype of both bases (no game needed) and, for simulation time, the OPEN-013 answer on reading in-game time; record in SPEC-TIME and SPEC-COMPANY.
- Resolving phase/task: PHASE-06 (mission timers), PHASE-09 (company); owner decision
- Raised in: docs/architecture/GAME_TIME_AND_PAUSE.md
- Status: Open

### OPEN-105 — Host behavior when mod code throws
- Question: What does the host do when an exception escapes mod update code (crash, disable the system, log and continue), and at which points can the mod place fault boundaries?
- Impact: REQ-RECOV-002 containment design, circuit breakers (SPEC-RECOVERY, SPEC-MODULES).
- Verification method: Documentation and community references (read-only); PHASE-01 lab experiment with a deliberately throwing test system in a development build of our own mod skeleton, in a lab city, with logs captured.
- Resolving phase/task: PHASE-01
- Raised in: docs/architecture/ERROR_RECOVERY.md
- Status: Open

### OPEN-106 — Detecting deletion or replacement of a referenced entity
- Question: How can the mod detect that an entity it references (career vehicle, avatar, borrowed vehicle, building, stop) has been deleted or replaced by the host (bulldozing, despawn, zoning replacement, disasters)?
- Impact: REQ-TRANS-003 (removal during transitions), mission invalidation, registry consistency (SPEC-ENTITY, SPEC-CONTROL).
- Verification method: PHASE-02 spike validating a referenced vehicle every frame while removing it by native means in a lab city; PHASE-05 transition-removal scenarios; record cost of per-frame validity checks.
- Resolving phase/task: PHASE-02, PHASE-05
- Raised in: docs/architecture/ENTITY_LIFECYCLE.md
- Status: Open

### OPEN-107 — Mod update timing and ordering
- Question: Can mod updates be ordered relative to native vehicle movement, camera update, and rendering, and do they run per rendered frame or per simulation tick (and what happens while paused)?
- Impact: Single movement authority (write after native movement or with native movement suspended), bounded per-frame motion and fixed substeps, camera smoothness (SPEC-CONTROL, SPEC-TIME, ADR-0004).
- Verification method: Read-only inspection of the candidate base mod's update registration (TASK-0002); official/community documentation; PHASE-02 timing instrumentation logging frame and tick counts per update.
- Resolving phase/task: TASK-0002 (input), PHASE-02, PHASE-03
- Raised in: docs/architecture/ARCHITECTURE.md
- Status: Open

### OPEN-108 — Sidecar retention without enumerating saves
- Question: If career payloads are stored in sidecar files (Sidecar or Hybrid option), how are old revisions retained and cleaned up when the mod may not be able to list which saves still exist?
- Impact: Disk growth versus the risk of deleting a revision that an existing save still needs (REQ-SAVE-001, REQ-SAVE-004).
- Verification method: Check whether the host exposes the list of saves (PHASE-01 spike, read-only); otherwise define a retention policy (count/age per city identity plus never deleting revisions loaded recently) and test it with SAVE_SAFETY_TESTS fixtures.
- Resolving phase/task: PHASE-01 (only if ADR-0003 selects Sidecar or Hybrid)
- Raised in: docs/architecture/SAVE_FORMAT.md
- Status: Open

### OPEN-109 — Operation for the optional native-economy write experiment
- Question: Should the gated native-economy write experiment run at all, and if so, which single operation type should it test (for example, one transfer to the municipal budget)?
- Impact: REQ-CITY-003 experiment scope; risk to lab cities only; informs post-1.0 expansions.
- Verification method: Project owner decision after PHASE-08; if approved, a lab-city experiment per SPEC-CITY section 9 with before/after logging.
- Resolving phase/task: PHASE-10; owner decision
- Raised in: docs/architecture/CITY_INTEGRATION.md
- Status: Open

### OPEN-110 — Sandbox mode exposure in player builds
- Question: Is sandbox mode (temporary control of native vehicles, no ownership) available to players in v1.0, available only in development builds, or available behind a clearly labeled option?
- Impact: SPEC-CONTROL and SPEC-ENTITY sandbox/career differences, support burden of handback defects, UI labeling, REQ-ENTITY-002 communication.
- Verification method: Project owner decision informed by PHASE-02 handback reliability evidence.
- Resolving phase/task: Owner decision before PHASE-05 (input from PHASE-02)
- Raised in: docs/architecture/CONTROL_AUTHORITY.md
- Status: Open

### OPEN-111 — Camera takeover and restoration
- Question: Can the mod take over the active game camera for first-person, third-person, and chase views, and restore the building camera exactly (position, mode, settings) afterwards?
- Impact: REQ-CTRL-002 single camera owner, REQ-CTRL-004 restoration, REQ-PLAYER-003 cameras.
- Verification method: Read-only inspection of the candidate base mod's camera handling (TASK-0002); PHASE-02 spike comparing camera state before entering and after leaving character mode; PHASE-04 first/third-person rigs.
- Resolving phase/task: TASK-0002 (input), PHASE-02, PHASE-04
- Raised in: docs/architecture/ARCHITECTURE.md
- Status: Open

### RISK-101 — Sidecar career data separated from its save
- Description: With Sidecar or Hybrid storage, copying a save to another machine, cloud synchronization, sharing a save, or restoring a backup may move the save without its career payload, so the career appears missing or falls back to an older revision. · Likelihood: M · Impact: M
- Mitigation: Prefer an identity anchor inside the save if possible (ADR-0003); explicit "career data not found for this save" message instead of guessing; document the limitation for players; never attach another revision's data automatically. · Indicator/trigger: reports of missing career after moving saves; load results "no career data" with an anchor present. · Owner role: Coordinator · Phase: PHASE-01, PHASE-13
- Status: Open

### RISK-102 — Recovery or fault-containment loops
- Description: A persistent fault (bad ground data, repeated authority conflict) may re-trigger Recovery or forced return to Building indefinitely, making character mode unusable or spamming logs. · Likelihood: M · Impact: M
- Mitigation: Escalation after three Recovery entries in 60 s; circuit breakers per module; second Critical fault disables character mode until next load with an explanation; log rate limiting (SPEC-RECOVERY). · Indicator/trigger: repeated Recovery entries at the same location in logs or playtests. · Owner role: Implementer · Phase: PHASE-04, PHASE-05
- Status: Open

### RISK-103 — Fixed-step motion cost spiral at low frame rates
- Description: Fixed-substep integration costs more per frame when frames get slower, which can slow frames further ("spiral"). · Likelihood: L · Impact: M
- Mitigation: Frame-delta clamp and maximum substeps per frame; discard excess time (SPEC-TIME section 7); measure in PHASE-03 B3/B4 configurations. · Indicator/trigger: mod update time rising with lower frame rates in benchmarks. · Owner role: Implementer · Phase: PHASE-03
- Status: Open

### RISK-104 — Host types leak into pure career logic
- Description: Convenience shortcuts may introduce host-game or engine types into Career, making it untestable without CS2 and exposing it to game updates. · Likelihood: M · Impact: M
- Mitigation: Ports owned by Career; only CityBridge references host types; Career built without host references; dependency check in every review touching Career (SPEC-MODULES). · Indicator/trigger: Career logic tests cannot run outside the game; a host type appears in a Career signature. · Owner role: Reviewer · Phase: PHASE-01 onward
- Status: Open

### RISK-105 — Career vehicles obstruct native traffic
- Description: Mod-spawned career vehicles parked or left on lanes (or captured in a host save) may block native traffic, act as invisible obstacles, or be destroyed by city edits. · Likelihood: M · Impact: M
- Mitigation: Stow policy on every return to Building; parking only at validated access points; orphan sweep after load; traffic observation in PHASE-05 and PHASE-06 tests (SPEC-ENTITY). · Indicator/trigger: queues forming behind parked career vehicles; career vehicles found after loading without the mod. · Owner role: QA · Phase: PHASE-05, PHASE-06
- Status: Open

### RISK-106 — Revalidation cost spikes in large cities
- Description: Full revalidation on every return from Building (the fallback if city edits cannot be detected, OPEN-021) may cause noticeable hitches in large cities with many missions, vehicles, and service points. · Likelihood: M · Impact: M
- Mitigation: Prioritize what is needed to spawn; amortize the rest over frames before affected interactions; cache keyed by city revision stamp; measure in PHASE-10 and PHASE-12 (SPEC-CITY, SPEC-PERF). · Indicator/trigger: entering character mode takes noticeably longer as the city grows; frame spikes after returning from Building. · Owner role: Implementer · Phase: PHASE-10, PHASE-12
- Status: Open

**Gameplay group (201–229)**

### OPEN-201 — Clock basis for career timers and company settlement periods
- Question: Which clock should measure mission timers (offer TTL, start window, passenger patience, delivery and freight deadlines, bus timetable, challenge times) and company settlement periods? Option A: a mod-owned "career clock" that advances only in stable character-mode states and freezes in Paused/Menu/Loading/Unloading/Building. Option B: the CS2 in-game clock (which runs during Building mode and at variable simulation speed). The gameplay specs currently propose A for missions and B (one in-game day) for company periods.
- Impact: REQ-TIME-002, REQ-TIME-004, REQ-COMPANY-003, REQ-DELIV-001 (deadlines), REQ-BUS-001 (timetable); fairness of deadlines at different simulation speeds; exploitability (freezing deadlines by switching modes); whether company income accrues during long Building sessions.
- Verification method: Decide in SPEC-TIME with a written comparison (fairness, exploit surface, determinism across save/load). Then run a Logic-layer simulation of both clocks across scripted mode switches, pauses, and save/load cycles. Confirm in-game readability of the in-game clock and simulation speed during the PHASE-03 time spike (related: OPEN-013).
- Resolving phase/task: PHASE-03 (SPEC-TIME decision), validated in PHASE-06 (missions) and PHASE-09 (company)
- Raised in: docs/gameplay/MISSION_FRAMEWORK.md (also TAXI.md, DELIVERIES.md, FREIGHT.md, COMPANIES_AND_FLEET.md)
- Status: Merged into OPEN-104 (2026-10-18). OPEN-104 is the primary entry; this entry is kept because gameplay documents reference it.

### OPEN-202 — Representation of mod-owned figures (career passengers, mission characters)
- Question: How can mod-owned career passengers and service-point mission characters be shown (model, animation, boarding/alighting, walking a few meters) with own or authorized assets, without creating native citizens? What is the minimum acceptable substitute (marker plus fade at the door), and how many figures can be visible at once (bus stops) within the performance budget?
- Impact: REQ-TAXI-002 (represented boarding and alighting), REQ-BUS-001, REQ-HOME-002 (optional mission characters), REQ-ART-001/002, REQ-PERF-001. Related to OPEN-010 (avatar representation), which may share the same solution.
- Verification method: PHASE-04 avatar spike results reused; PHASE-06 spike spawning 1–3 passenger figures at a curb with boarding substitute; PHASE-08 measurement with up to 6 figures per stop × several stops, following BENCHMARK_PROTOCOL. Provenance check in ASSET_PROVENANCE for every model/animation.
- Resolving phase/task: PHASE-06 (WP for taxi passengers), PHASE-08 (bus scale)
- Raised in: docs/gameplay/careers/TAXI.md (also BUS.md, HOME_AND_SERVICE_POINTS.md)
- Related: OPEN-010 (avatar representation); resolve together in PHASE-04.
- Status: Open

### OPEN-203 — Vehicle-class-aware access-point and route validation
- Question: Can access-point and route validation account for vehicle class (length, turning radius, and any road restrictions the city exposes for trucks or buses), so that a van, rigid truck, or bus is never offered a pickup, loading bay, or stop it cannot physically reach, stop at, or turn around from?
- Impact: REQ-CAREER-002, REQ-CAREER-003, REQ-FREIGHT-001, REQ-BUS-001, REQ-DELIV-001; risk of impossible contracts and players stuck in cul-de-sacs with large vehicles; recovery load.
- Verification method: PHASE-08 lab-city spike: generate offers for each class over a road set with dead ends, narrow streets, and small lots; drive every generated access point with each class and record failures; compare validation heuristics (minimum frontage length, turnaround availability, restriction flags if readable) against the drive results. Extends OPEN-018/OPEN-019.
- Resolving phase/task: PHASE-08 (with groundwork in PHASE-06 for cars)
- Raised in: docs/gameplay/MISSION_FRAMEWORK.md (also FREIGHT.md, DELIVERIES.md, VEHICLE_OWNERSHIP_AND_GARAGE.md, CHALLENGES_AND_DYNAMIC_CITY.md)
- Status: Open

### OPEN-204 — Economy balancing targets and calibration method
- Question: What concrete balancing targets does the project owner want (time to first payout, time to first owned car, earnings per hour per career, cost share of running an owned vehicle, cost of a failure), and how will they be calibrated (spreadsheet model of the pure economy rules first, then timed playtests)?
- Impact: REQ-ECON-003, REQ-ECON-004, player motivation (RISK-023), exploit risk (RISK-203). All values in SPEC-ECON §8 are illustrative until this is answered.
- Verification method: Owner decision on target ranges; Logic-layer simulation of the economy rules with scripted mission streams; timed playtests at M2 (taxi only) and M3 (all careers) recorded as evidence; adjust the balancing table, not the rules.
- Resolving phase/task: PHASE-07 (initial calibration), PHASE-12 (beta confirmation)
- Raised in: docs/gameplay/PROGRESSION_AND_ECONOMY.md
- Status: Open

### OPEN-205 — Parked career vehicles while the player is in Building mode
- Question: Should parked career vehicles (owned, rented, company-held) remain as physical entities in the world while the player is in Building mode and while the save is closed, or be converted to "stored at location" records and respawned on return to character mode?
- Impact: REQ-ENTITY-003 (no orphans), REQ-TRANS-004 (park outcome), REQ-CITY-002 (revalidation when roads under a parked vehicle are edited), native traffic and parking (invisible blockers), save size, and behavior after disabling the mod (REQ-SAVE-006).
- Verification method: Decide jointly with SPEC-ENTITY. PHASE-05 spike: park a career vehicle, return to Building, demolish/replace the road under it, save, disable the mod, reload; compare entity counts and traffic behavior for both options.
- Resolving phase/task: PHASE-05 (decision), PHASE-07 (owned vehicles at scale)
- Raised in: docs/gameplay/WALK_DRIVE_TRANSITIONS.md (also VEHICLE_OWNERSHIP_AND_GARAGE.md)
- Status: Open

### OPEN-206 — Stable identity of zoned buildings across level-up, abandonment, and rebuild
- Question: When a zoned building levels up, is abandoned, burns down, or is rebuilt by the native simulation, does it keep an identity the mod can follow, or must the mod treat it as a new building? What stable reference (lot/parcel, address, position plus category) survives these events?
- Impact: REQ-HOME-001/003 (home and garage links), REQ-SAVE-003 (stable references), mission destinations and service points; risk of homes silently "disappearing" without any player edit. May overlap with an architecture stable-reference entry; merge if so.
- Verification method: PHASE-09 lab-city observation: record building references for a sample of zoned buildings, run the simulation until level-ups/abandonment occur (or trigger them in a lab city), and check which reference resolves afterwards. Read-only inspection of documented community references first.
- Resolving phase/task: PHASE-09 (with input from the PHASE-01 stable-reference work)
- Raised in: docs/gameplay/HOME_AND_SERVICE_POINTS.md (also CHALLENGES_AND_DYNAMIC_CITY.md)
- Related: OPEN-103 (stable references to native objects).
- Status: Open

### OPEN-207 — Readable building and stop attributes for demand-informed job generation
- Question: Which per-building and per-stop attributes can be read reliably and cheaply for weighting jobs (zone/category such as residential, commercial, office, industrial and industry type, building size or occupancy, workplace counts, transport hubs, stop usage), without writing native state?
- Impact: REQ-CHAL-003, quality of taxi/delivery/freight/bus offers, bus stop passenger generation, freight cargo types. Without it, weighting falls back to category-only or distance-only generation.
- Verification method: Read-only review of documented sources, then a PHASE-10 (groundwork in PHASE-08) spike reading attributes for a sample of buildings and stops in a lab city; measure cost per refresh; record in CS2_INTEGRATION_FINDINGS.
- Resolving phase/task: PHASE-08 (stops, industry categories), PHASE-10 (full weighting)
- Raised in: docs/gameplay/CHALLENGES_AND_DYNAMIC_CITY.md (also DELIVERIES.md, FREIGHT.md, BUS.md)
- Status: Open

### OPEN-208 — Sandbox mode availability in release builds
- Question: Is Sandbox mode (temporary control of native vehicles, no rewards, mandatory handback) offered to players in release builds as a "free drive" option, or restricted to development and QA? If offered, how is it gated and labeled?
- Impact: Scope and messaging of v1.0; exposure of the riskiest integration path (BorrowedNativeVehicle handback, REQ-ENTITY-001) to players; compatibility claims; the PRD defines Sandbox only as development/testing. Proposed requirement if offered: "Sandbox mode is labeled, grants no career progress, and always hands vehicles back."
- Verification method: Project owner decision informed by PHASE-02 handback reliability evidence and PHASE-12 beta feedback; record as a PRD change or a confirmed non-goal.
- Resolving phase/task: Owner decision after PHASE-02 evidence; confirmed before PHASE-13
- Raised in: docs/gameplay/VEHICLE_DRIVING.md
- Status: Merged into OPEN-110 (2026-10-18). OPEN-110 is the primary entry; this entry is kept because gameplay documents reference it.

### RISK-201 — Career bus blocks native buses at shared stops
- Description: The player's career bus dwelling at a city stop may occupy the stop bay or lane so native buses queue behind it, disturbing the native transit line (or the reverse: native buses occupying the stop zone prevent servicing). · Likelihood: M · Impact: M
- Mitigation: Stop zone that accepts stopping behind a native bus; dwell cap; no lane reservation held after departure; PHASE-08 observation of stop queues under the OPEN-032 contact rule; adjust zone length per stop type. · Indicator/trigger: native buses waiting behind the career bus for longer than the dwell cap; native line throughput drop during lab shifts. · Owner role: QA · Phase: PHASE-08
- Raised in: docs/gameplay/careers/BUS.md
- Status: Open

### RISK-202 — Personal wallet confused with the municipal budget
- Description: Players may believe that career earnings or spending change the city's finances (or that city money can buy vehicles), because both are shown as money in the same game. · Likelihood: M · Impact: M
- Mitigation: Distinct name, icon, and placement for the wallet; economic tutorial hook on first wallet view; ledger descriptions that never mention city finances; localization review. · Indicator/trigger: playtest or tester questions about city money; support reports. · Owner role: Coordinator (UI copy) · Phase: PHASE-07, PHASE-11
- Raised in: docs/gameplay/PROGRESSION_AND_ECONOMY.md
- Status: Open

### RISK-203 — Economy exploits through cancellation loops and safety nets
- Description: Safety nets required by REQ-ECON-004 and REQ-GARAGE-006 (free basic rental, deferred charges, free relocation, recovery, mobile service, challenge rewards) or cancel/accept loops may be abused to earn money, fast-travel, or avoid costs. · Likelihood: M · Impact: M
- Mitigation: Cancellations never yield positive money; basic rental takes a revenue share; recovery moves only to the nearest safe point; towing is charged; sale below purchase price; challenge rewards once per tier and route version; Logic-layer exploit tests on the pure economy rules. · Indicator/trigger: earnings per hour in playtests far above the balancing target; repeated cancel/accept patterns in diagnostics. · Owner role: QA · Phase: PHASE-07, PHASE-12
- Raised in: docs/gameplay/PROGRESSION_AND_ECONOMY.md (also MISSION_FRAMEWORK.md, VEHICLE_OWNERSHIP_AND_GARAGE.md)
- Status: Open

### RISK-204 — Too few valid contracts in small or unusual cities
- Description: Small, early-stage, or unconventional cities (few curbside access points, no industry, no bus lines, no parking lots, disconnected districts) may produce few or no valid offers for some careers or challenges, making them unavailable or repetitive. · Likelihood: M · Impact: M
- Mitigation: Minimum-city checks with explained unavailability per career; virtual service points; widened distance bands when offers are scarce; starter job guaranteed whenever at least one valid access-point pair exists; PHASE-12 testing across small/medium/large cities. · Indicator/trigger: offer boards empty or showing the same pairs repeatedly in a test city. · Owner role: Coordinator · Phase: PHASE-08, PHASE-10, PHASE-12
- Raised in: docs/gameplay/MISSION_FRAMEWORK.md (also PROGRESSION_AND_ECONOMY.md, HOME_AND_SERVICE_POINTS.md, CHALLENGES_AND_DYNAMIC_CITY.md, careers/DELIVERIES.md, careers/FREIGHT.md, careers/BUS.md)
- Status: Open

### RISK-205 — Players expect a physical company fleet
- Description: Even with explicit labeling, players may expect hired drivers and company vehicles to appear in the city (as in other games), and perceive the managerial simulation as missing or broken. · Likelihood: M · Impact: L
- Mitigation: Binding wording rules in SPEC-COMPANY §4; "Managed operations (simulated)" headings; physical fleet listed as an expansion; tutorial hook at company founding. · Indicator/trigger: playtest feedback or reports asking where employee vehicles are. · Owner role: Coordinator · Phase: PHASE-09, PHASE-11
- Raised in: docs/gameplay/COMPANIES_AND_FLEET.md
- Status: Open

**Product and experience group (301–329)**

### OPEN-301 — Minimum city content for character mode and each career
- Question: What minimum city content does character mode, the tutorial fare, and each career need? For example: a connected road network, a number of eligible buildings with reachable access points, commercial or industrial loading points for deliveries and freight, and a bus line with at least two reachable stops for the bus career. How should the mod explain a missing requirement to the player?
- Impact: First-time use in new or small cities (JRN-001), career availability messages (REQ-UI-003), the tutorial fare (REQ-ONBOARD-001), and content completion criteria in GAME_DESIGN section 10. Without defined thresholds, a career could be silently empty (RISK-301).
- Verification method: Lab-city experiments with deliberately minimal cities: an empty map, a single road, one zone type, and no transit. Record which offers can be generated and validated (depends on OPEN-018 and OPEN-019). Define the thresholds in SPEC-MISSION and SPEC-CHALLENGE, and the messages in SPEC-UI.
- Resolving phase/task: PHASE-06 (taxi and tutorial fare), PHASE-08 (delivery, freight, bus), PHASE-10 (demand-informed jobs)
- Raised in: docs/product/GAME_DESIGN.md, docs/product/PLAYER_JOURNEYS.md, docs/experience/ONBOARDING.md
- Status: Open

### OPEN-302 — Discoverable entry point into character mode
- Question: How does a first-time player discover and launch character mode from Building mode? Options include a button inserted in the game's interface, a one-time notice, or a key binding alone. Which insertion points can a code mod reliably use?
- Impact: REQ-ONBOARD-001 (stage 0 of the tutorial), REQ-UI-001. If the entry point is hard to find, new players fail before the tutorial begins.
- Verification method: Read-only review of the official UI-modding documentation for documented insertion points (see SOURCES). Draw one entry element in the PHASE-01 UI spike (together with OPEN-015). Validate discoverability with the PHASE-11 fresh-tester protocol (SPEC-ONBOARD section 5).
- Resolving phase/task: PHASE-01 (technical feasibility), PHASE-11 (discoverability)
- Raised in: docs/experience/UI_AND_HUD.md, docs/experience/ONBOARDING.md, docs/product/PLAYER_JOURNEYS.md
- Status: Open

### OPEN-303 — Inheriting the game's interface and display settings
- Question: Can the mod's UI read and follow the game's interface scale, unit preference (metric or imperial), and other display or accessibility settings? Or must the mod provide its own equivalents?
- Impact: REQ-INPUT-003 (UI scaling); consistent units and number formatting (SPEC-L10N section 3.5). Duplicated settings that disagree with the game's settings confuse players.
- Verification method: Documentation review (read-only), then the PHASE-01 UI spike. Change the game's interface settings in a lab profile and observe whether mod UI elements follow. Record the result in CS2_INTEGRATION_FINDINGS.
- Resolving phase/task: PHASE-01 (spike), PHASE-11 (final settings design)
- Raised in: docs/experience/UI_AND_HUD.md, docs/experience/CONTROLS_AND_ACCESSIBILITY.md, docs/experience/LOCALIZATION.md
- Status: Open

### OPEN-304 — Audio playback and mixing integration
- Question: Can the mod play its own positional (3D) and interface sounds through supported means? Can it route them through the game's volume categories (master, effects, interface), and do they pause and resume with the game's pause and window-focus behavior?
- Impact: REQ-AUDIO-001, captions linked to cues (REQ-INPUT-003), and the mixing rules in SPEC-AUDIO section 3.3. If routing is not possible, the mod needs its own volume controls and conservative defaults.
- Verification method: Documentation and community-source review (read-only). Then an audio probe in a lab city: one looping engine placeholder on the driven vehicle and one interface sound. Check volume-setting response, pause, focus loss, and cleanup on exit and unload. Measure the voice count and cost following BENCHMARK_PROTOCOL.
- Resolving phase/task: PHASE-02 or PHASE-04 (probe), PHASE-11 (final mix)
- Raised in: docs/experience/AUDIO.md
- Status: Open

### OPEN-305 — PT-BR linguistic review and terminology alignment
- Question: Who performs the native Brazilian Portuguese (PT-BR) linguistic review of player-facing text? Should the project's PT-BR glossary follow the game's own PT-BR terminology for overlapping concepts (budget, district, transit line, and so on)?
- Impact: REQ-L10N-001 quality; consistency with the host game's interface; the translation workflow (SPEC-L10N section 3.8). Without a named reviewer, translations cannot be approved.
- Verification method: Project-owner decision on the reviewer. Read-only observation of the game's PT-BR interface terms in a lab profile, recorded in the future glossary alongside the translation resources (not in English documentation).
- Resolving phase/task: Owner decision before PHASE-11 translation work; glossary started in PHASE-01
- Raised in: docs/experience/LOCALIZATION.md
- Status: Open

### OPEN-306 — Wallet currency presentation
- Question: Should the personal wallet use the same currency sign and formatting as the city budget, or a distinct presentation, so that players never confuse career money with municipal money?
- Impact: REQ-ECON-001 (separate wallet), player trust, and localization formatting (SPEC-L10N section 3.5). Confusion could lead players to believe that the mod changes city finances.
- Verification method: Observe (read-only) how the game presents money in the lab profile, in English and PT-BR. Prototype both options in the PHASE-06 minimal wallet HUD. Decide with owner review and playtest feedback at M2. Record the decision in SPEC-ECON and SPEC-UI.
- Resolving phase/task: PHASE-06 (prototype), PHASE-07 (decision), PHASE-11 (final presentation)
- Raised in: docs/product/GAME_DESIGN.md, docs/experience/UI_AND_HUD.md, docs/experience/LOCALIZATION.md

## Risks
- Status: Open

### RISK-301 — Career variety depends on the player's city
- Description: Because jobs come from the player's own city (REQ-CHAL-003), small, young, or monotonous cities may produce repetitive work or make some careers unavailable (no bus lines, no industry). The product can then feel thin even though it works correctly. · Likelihood: M · Impact: M
- Mitigation: A target driving-time band for offers; contract variants per career; clear "what to build" messages; defined minimum content (OPEN-301); PHASE-12 beta in small, medium, and large cities. · Indicator/trigger: playtest notes reporting repetition, or careers unavailable in representative beta cities. · Owner role: Coordinator · Phase: PHASE-08, PHASE-10, PHASE-12
- Status: Open

### RISK-302 — Onboarding validation lacks genuinely fresh testers
- Description: REQ-ONBOARD-001 requires that a new player completes a first job without help. A small team may not find enough people who have never seen the mod, and testers stop being fresh after one session. The result could be that the gate is passed with biased evidence or cannot be evaluated. · Likelihood: M · Impact: M
- Mitigation: The owner decides early how testers are recruited; the protocol uses a minimum of five testers, including PT-BR and gamepad-only runs; each iteration uses new testers; the first-session evidence is kept. · Indicator/trigger: fewer than five fresh testers available when PHASE-11 reaches its gate. · Owner role: Project owner · Phase: PHASE-11, PHASE-13
- Status: Open

### RISK-303 — PT-BR localization lags or breaks layouts
- Description: Frequent string changes during development may leave PT-BR translations outdated or missing. Longer PT-BR text may truncate or overflow fixed layouts. Release builds could ship untranslated or cut-off strings. · Likelihood: M · Impact: M
- Mitigation: Externalization from PHASE-01; key status tracking (new, changed, approved); pseudo-localization on every UI change; expansion allowances; string freeze before the PHASE-12 beta; English fallback with logging. · Indicator/trigger: a growing count of unapproved keys; truncation found in pseudo-locale runs. · Owner role: QA · Phase: PHASE-01, PHASE-11, PHASE-12
- Status: Open

### RISK-304 — Authorized vehicle and footstep audio unavailable or poor
- Description: Engine, tire, impact, and footstep sounds from own or authorized sources (REQ-AUDIO-001) may be hard to obtain at acceptable quality, especially four distinct engine sets. This could leave placeholders or weak feedback that hurts the driving feel (RISK-023). · Likelihood: M · Impact: M
- Mitigation: Identify license-compatible sound sources early (after OPEN-027); plan simple synthesized engine layers as a fallback; label placeholders (REQ-ART-002); budget the work in PHASE-11. · Indicator/trigger: no approved source per vehicle class by the start of PHASE-11. · Owner role: Coordinator · Phase: PHASE-06 (placeholders), PHASE-11
- Status: Open

### RISK-305 — Economy balance built on illustrative values
- Description: All economic values are currently illustrative proposals. Without play data, the economy may be too grindy (the first car takes too long), too generous (progression trivialized), or exploitable (repeated short trips, timing around checkpoints), and the illustrative numbers may silently become final. · Likelihood: H · Impact: M
- Mitigation: Parameter tables kept in SPEC-ECON and SPEC-GARAGE, never hard-coded; balance reviews at M2 and M3 using ledger exports from playtests (local, sanitized); anti-exploit rules (minimum distance, pair cooldowns, no pay for pause or mode switches); PHASE-12 long sessions. · Indicator/trigger: playtest time-to-first-car far outside the proposed band; ledger patterns showing repeated micro-trips. · Owner role: Coordinator · Phase: PHASE-06, PHASE-07, PHASE-12
- Status: Open

### RISK-306 — Street-level cameras cause motion discomfort
- Description: First-person walking, camera shake, head bob, and fast camera transitions at street level may cause motion discomfort for some players. · Likelihood: M · Impact: L
- Mitigation: Head bob off by default; a camera-shake slider; a field-of-view slider; fade instead of swoop for transitions; options applied only to mod cameras (SPEC-INPUT section 3.7). · Indicator/trigger: tester reports of discomfort during PHASE-04 or PHASE-11 sessions. · Owner role: QA · Phase: PHASE-04, PHASE-11
- Status: Open

**Research, engineering, and legal group (401–429)**

### OPEN-401 — Melty exclusivity versus Paradox Mods distribution
- Question: If any Drive the City II content (or a version of it) were ever published on Melty, how would the Terms' exclusive licence ("Melty is the only marketplace for your Content"; covers modified/partial/renamed/ported versions and remixes; lasts 12 months after removal; content already on another marketplace "may stay there as it is, but new versions and releases follow this section") interact with distribution on Paradox Mods and with the licenses of any incorporated third-party code? Is Melty publication in scope at all?
- Impact: Distribution strategy (PHASE-13), packaging, project license choice (OPEN-027); could restrict publishing new versions on Paradox Mods. Melty Terms were "Last updated" on the access date (2026-10-18), so they change often.
- Verification method: Owner decision on whether Melty publication is ever wanted; if yes, legal review of the Terms version current at that time. Never publish to Melty without owner + legal sign-off. Raw Terms text read 2026-10-18 (V-09) is the starting point.
- Resolving phase/task: Owner decision before any Melty publication; at the latest PHASE-13 (release review)
- Raised in: docs/research/MELTY_EVALUATION.md (also docs/legal/REUSE_AND_LICENSE_AUDIT.md)
- Status: Open

### OPEN-402 — Base-mod control technique applied to mod-spawned career vehicles
- Question: Can the Beta Test Driving Mod's direct-control technique (freeze native navigation before the native car-move step, write the pose after it, keep lane presence) drive a vehicle spawned by our mod that has no native trip/path owner, or does a CareerVehicle need a different movement-authority path?
- Impact: REQ-ENTITY-002, REQ-DRIVE-001 in career mode, REQ-GARAGE-001; determines how much of the base mod's value carries beyond Sandbox mode (the base mod explicitly "does not spawn a fake player car").
- Verification method: TASK-0002 raw read of which native components the possession code depends on; then a PHASE-02 spike after the spawn spike of OPEN-012, comparing a borrowed native vehicle and a spawned vehicle under the same control code, with entity-count and traffic observations.
- Resolving phase/task: TASK-0002 (read-only input), PHASE-02
- Raised in: docs/research/BASE_MOD_AUDIT.md (also FEASIBILITY_MATRIX.md, UPSTREAM_STRATEGY.md)
- Partial (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): the base mod's control needs a native car archetype (car, navigation, current lane, transform, moving, prefab reference, transform-frame buffer, path owner and elements) processed by the native navigation and move systems, and it never touches the car's destination. **Inference:** mod-spawned career vehicles would need the same archetype and would still be processed natively, so a separate authority path for career vehicles is likely preferable.
- Status: Open

### OPEN-403 — How the base mod's published build was produced; need for the official post-processor
- Question: The base mod's csproj (raw-verified) imports no official Mod.props/Mod.targets, so the toolchain's ModPostProcessor (IL post-processing, Burst; documented for 1.1.12f1) is not wired in. Was the published Paradox Mods build (0.3.54-stable) produced with the post-processor? Does a plain SDK build of the repository project produce a loadable, equivalent mod? Does our own mod require post-processing?
- Impact: Reproducibility of the TASK-0003 baseline (RISK-403); how BUILD_AND_RUN structures our project; validity of baseline conclusions.
- Verification method: TASK-0002 read-only search of the source for code that depends on post-processing (e.g., Burst-compiled jobs); TASK-0003 isolated build with a ManagedPath override (proposed procedure), record whether it loads; optional separately authorized second build inside an official-template project; compare behavior, never redistribute outputs.
- Resolving phase/task: TASK-0002, TASK-0003
- Raised in: docs/engineering/BUILD_AND_RUN.md (also BASE_MOD_AUDIT.md, TOOLCHAIN_MATRIX.md)
- Partial (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): the source uses no Burst compilation, scheduled jobs, or code-generated ECS APIs, and its UI module is hand-written. **Inference:** a plain SDK build probably does not need the post-processor for this code. How the published package was produced remains unknown.
- Status: Open

### OPEN-404 — Provenance of the base mod's `decompiled_published_0317` folder
- Question: The base mod's csproj excludes `decompiled_published_0317\**` from compilation (raw-verified); the folder is not in the repository. What was decompiled (the author's own published build, another mod, or game code), and does any committed source derive from decompiled third-party or game code?
- Impact: Whether any upstream code can be reused without provenance taint (RISK-404); notices and attribution; REQ-REL-003.
- Verification method: TASK-0002 read-only: search commit history and CHANGELOG for references to the folder or to version 0.3.17; compare suspicious files' style/headers; treat any code of unclear provenance as not reusable (reference only). Contacting the upstream author only with owner authorization. We never decompile anything ourselves.
- Resolving phase/task: TASK-0002
- Raised in: docs/research/BASE_MOD_AUDIT.md (also REUSE_AND_LICENSE_AUDIT.md, CODING_STANDARDS.md)
- New facts (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): the exclusion was added in commit `2c5b83085290b9d578ae6eff30f9241d8b0299c5` (2026-06-04), together with the collision file, and no commit ever added the folder. The CHANGELOG mentions restoring a local cache "from the published 0.3.17 package" **[Documented upstream]**. The folder name is consistent with a decompiled published package (inference only). Until the owner decides (REUSE_AND_LICENSE_AUDIT Q-04), files touched in that commit are concept-only.
- Status: Open

### OPEN-405 — Base mod copyright holder identity and pre-relicense history
- Question: LICENSE says "Copyright (c) 2026 stoot" (raw); AssemblyInfo has an empty AssemblyCopyright; the GitHub account is `northernst11-bot`. What is the relationship between account and author, and under which license were revisions before commit `af355236…` ("Relicense mod source under MIT", 2026-05-31; extraction-level) published? Do any files carry other headers or notices?
- Impact: Correct attribution in notices and headers if code is incorporated; RISK-002.
- Verification method: TASK-0002 read-only: LICENSE/NOTICE at earlier revisions via the GitHub API, per-file headers at the pinned SHA, commit authorship metadata. If ambiguity remains, owner decides whether to contact upstream (authorization required) or to restrict reuse to reference only.
- Resolving phase/task: TASK-0002
- Raised in: docs/legal/REUSE_AND_LICENSE_AUDIT.md (also BASE_MOD_AUDIT.md)
- Partial answer (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): the license history is now known (see OPEN-004). All 8 commits are authored as "stoot", whom the GitHub API associates with the login `stoot`. The repository belongs to `northernst11-bot`, and their relationship is not stated. Remaining: owner decisions on attribution wording and on whether to contact upstream (Q-03).
- Status: Partly answered (2026-10-18)

### OPEN-406 — Unity Editor version and license prerequisite for the current game version
- Question: The official toolchain page (verified only for game 1.1.12f1) requires Unity Editor 2022.3.7f1 and a Unity personal or commercial license ("a legal requirement we can not automate"), and says the Unity version tracks game updates. Which Unity version does the toolchain require for the owner's installed game version, and does the owner hold or accept a suitable Unity license?
- Impact: Toolchain installation for TASK-0003 and all builds (RISK-021); an owner legal obligation that no agent can fulfil.
- Verification method: TASK-0001 read-only (presence of `CSII_UNITYVERSION` / toolchain, owner statement on Unity installation and license); reading the in-game Modding panel only when the owner authorizes launching the game; owner decision on the license before any install.
- Resolving phase/task: TASK-0001, then owner decision before TASK-0003
- Raised in: docs/engineering/TOOLCHAIN_MATRIX.md (also WINDOWS_SETUP.md, CS2_INTEGRATION_FINDINGS.md, BUILD_AND_RUN.md)
- Observation (2026-10-18, TASK-0001): the installed game runs Unity **2022.3.71f1**, while the official toolchain page documents Unity Editor 2022.3.7f1 for game 1.1.12f1. The required editor version for 1.6.2f1 is therefore unconfirmed. No Unity Editor is installed.
- Status: Open

### OPEN-407 — Game captures in project evidence and public material
- Question: Under which conditions may screenshots or recordings of Cities: Skylines II be committed as test evidence or used in public material (mod listing, documentation), and what sanitization is required? (Captures of Motor Town or Melty are not used.)
- Impact: Evidence recording from TASK-0003 onward, documentation images, PublishConfiguration screenshots (PHASE-13); RISK-015, RISK-030.
- Verification method: Read-only review of the publisher's published terms for user/creator content and of the distribution platform rules (OPEN-017); owner decision. Until resolved, keep captures in git-ignored local storage and reference them by description.
- Resolving phase/task: Before the first evidence run with captures (TASK-0003); public use in PHASE-13
- Raised in: docs/legal/ASSET_PROVENANCE.md (also DEBUGGING.md, MOTOR_TOWN_REFERENCE.md)
- Status: Open

### RISK-401 — Melty exclusive licence blocks or constrains Paradox Mods distribution
- Description: Publishing any version of the mod on Melty would grant Melty an exclusive licence (12-month tail after removal; covers derived versions), which on a plain reading conflicts with distributing the same content on Paradox Mods; it could also interact with third-party license obligations. Not a legal conclusion. · Likelihood: L (only if something is published on Melty) · Impact: H
- Mitigation: Nothing from the project is published on Melty without owner decision and legal review (OPEN-401); the Melty evaluation is observation-only; distribution planning assumes Paradox Mods subject to OPEN-017. · Indicator/trigger: any proposal to publish, test-publish, or connect the project repository to Melty. · Owner role: Project owner · Phase: PHASE-00 (evaluation), PHASE-13
- Status: Open

### RISK-402 — Security and privacy exposure from the Melty application
- Description: Per its Terms, the app finds installed games, downloads mod loaders/launchers/build tools, writes into game folders (with backups), may "look for that copy in your folders and drives", starts programs, and updates itself automatically; its privacy policy mentions session recordings for some users. A press article reports an unverified third-party claim about installer components (explicitly not independently verified by the article). Using it on the development machine could alter the game installation or expose data. · Likelihood: M · Impact: M
- Mitigation: No installation, launch, or agent connection during the baseline; owner-attended observation-first procedure (MELTY_EVALUATION §6); backups of saves and a recorded game-folder state before any trial; minimum-scope tokens; never let Melty modify the development game installation without verified backups. · Indicator/trigger: unexpected files in game folders, unexpected background processes, or new terms. · Owner role: Project owner · Phase: PHASE-00
- Status: Open

### RISK-403 — Base-mod baseline build diverges from the published build
- Description: The base mod's project file does not import the official Mod.props/Mod.targets (no post-processor); a plain SDK build may fail, fail to load, or behave differently from the published Paradox Mods build, making TASK-0003 conclusions unreliable. · Likelihood: M · Impact: M
- Mitigation: OPEN-403; record exact build command, SDK version, and output; optionally compare with an official-template build (separately authorized); do not draw reuse conclusions from a build whose equivalence is unknown. · Indicator/trigger: build errors about framework/reference assemblies, mod not loading, or behavior contradicting the README. · Owner role: Implementer · Phase: PHASE-00
- Note (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): keep M/M. The source shows no dependency on post-processing (inference), but the version mismatch adds a new way for the baseline to diverge. Indicator added: compile errors against the 1.6.2f1 assemblies.
- Status: Open

### RISK-404 — Provenance taint in reused base-mod code
- Description: If any base-mod source derives from decompiled game code or third-party builds (see the excluded `decompiled_published_0317` folder), incorporating it could bring material we have no right to use. · Likelihood: L · Impact: H
- Mitigation: OPEN-404 resolved before incorporation; reuse only files with clear provenance; reference-only or re-implementation for doubtful parts; never commit decompiled code; Reviewer check at incorporation. · Indicator/trigger: code that mirrors game internals verbatim, unexplained references to decompiled folders, missing headers. · Owner role: Reviewer · Phase: PHASE-00, PHASE-02
- Reassessment proposal (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): likelihood **L → M**, impact H. The decompiled-folder exclusion arrived together with the collision code, and the CHANGELOG mentions restoring from a published package and porting another project's collision layer.
- Status: Open

### RISK-405 — Community-sourced technical claims are wrong or stale
- Description: Key design inputs (in-save serialization interfaces and own-version rule, mod-removal behavior, simulation-speed fields, `net48` runtime target) come only from community sources derived from decompiled code/static analysis and pinned to specific game versions (1.6.2f1, ~1.5.10f1, 1.6.0f1); they may be inaccurate or change with game updates. · Likelihood: M · Impact: H (save design)
- Mitigation: Treat as hypotheses; record the verified-for game version per claim (CS2_INTEGRATION_FINDINGS); PHASE-01/PHASE-03 spikes before decisions (ADR-0003, SPEC-TIME); re-check after game updates (RISK-007). · Indicator/trigger: spike results contradict a community claim; a game update changes a referenced type. · Owner role: Coordinator · Phase: PHASE-01, PHASE-03
- Status: Open

### RISK-406 — Extraction errors in the 2026-10-18 research
- Description: Most source-inspected findings were obtained through a model-mediated fetch tool that summarizes content; identifiers, numbers, or quotations may be wrong. Only V-01..V-10 (and lines read from the same raw fetches) were verified byte-for-byte. Designs relying on an extraction-level identifier could be wrong. · Likelihood: M · Impact: M
- Mitigation: Label every claim with its level (SOURCES §2); TASK-0002 re-verifies identifiers against raw files at a pinned SHA before any design or code relies on them. · Indicator/trigger: an identifier not found in the raw file; contradictions between extraction and raw text. · Owner role: Coordinator · Phase: PHASE-00
- Status: Open

### OPEN-411 — Origin and license of the base mod's vehicle-collision layer
- Question: The base mod's CHANGELOG (0.3.17) says it "Ports the CS2 GTA Edition vehicle collision layer". What is that project, who holds its rights, and under which license was it published? Does the base mod's vehicle-collision system file (added 2026-06-04 in commit `2c5b830…`, with no header) derive from it?
- Impact: Any code-level reuse of the collision sweep; attribution and notices (REQ-REL-003); RISK-411.
- Verification method: Bounded read-only public search (data only); an owner decision on contacting upstream (authorization required). Until answered, collision stays **concept only**.
- Resolving phase/task: Owner decision before any incorporation (PHASE-02 at the earliest)
- Raised in: docs/research/BASE_MOD_AUDIT.md §9; docs/legal/REUSE_AND_LICENSE_AUDIT.md
- Status: Open

### OPEN-412 — A native vehicle saved while possessed
- Question: If the game saves while a native car is possessed, the save may contain the frozen native state (pending path owner, empty path, zero maximum speed, injected lane objects). After a reload with the mod, and without it, does the car recover, stay stuck, or block traffic?
- Impact: REQ-SAVE-006 and REQ-ENTITY-001 for any takeover technique we adopt; the "handback-ready at every frame boundary" rule in SPEC-CONTROL; input to OPEN-101.
- Verification method: TASK-0003 baseline check BL-21 on a **lab copy** only, then a PHASE-02 test of our own sandbox.
- Resolving phase/task: TASK-0003 (observation), PHASE-02
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### OPEN-413 — Unregistered mod systems
- Question: Does CS2 1.6.2f1 create or update system classes in a mod assembly that are **not** registered from the mod's load method? The base mod compiles a police-chase system, a traffic guard that writes other cars' velocity, and a legacy control stack, none of them registered.
- Impact: Whether the inactive base-mod code can run, and our own coding rule for unregistered code.
- Verification method: Read-only documentation review; TASK-0003 check BL-25 (search the log for those systems' message prefixes).
- Resolving phase/task: TASK-0003 (observation), PHASE-01 (coding standard)
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### OPEN-414 — Time base for a mod-driven vehicle
- Question: Should a mod-driven vehicle's control step use simulation ticks or real time? The base mod clamps real elapsed time to 1/60–1/30 s per simulation tick. How does that behave when paused, at faster simulation speeds, and at low frame rates, and what does SPEC-TIME require instead?
- Impact: REQ-TIME-001 to REQ-TIME-003; driving feel at different simulation speeds.
- Verification method: TASK-0003 check BL-20 (observation), then a PHASE-03 spike.
- Resolving phase/task: PHASE-03 (TASK-0003 input)
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### OPEN-415 — Borrowable vehicle classes from data
- Question: Which component or prefab data reliably identifies private cars and light delivery vehicles (borrowable in sandbox mode) versus emergency, public-transport, and city-service vehicles? The base mod accepts any native car and only ranks candidates by prefab-name keywords.
- Impact: SPEC-CONTROL sandbox rules and REQ-ENTITY-002 discipline; avoiding interference with dispatch.
- Verification method: Read-only community and official references; a PHASE-02 spike listing components per vehicle prefab in the lab city.
- Resolving phase/task: PHASE-02
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### RISK-411 — Collision code of unclear third-party origin
- Description: The base mod's collision layer is described upstream as a port of another project whose rights and license are unknown. Code-level reuse could import material we have no right to use. · Likelihood: M · Impact: H (code reuse) / L (concept only)
- Mitigation: OPEN-411; concept-only treatment; Reviewer check at incorporation. · Indicator/trigger: an identified source project with an incompatible license, or matching code structure. · Owner role: Reviewer · Phase: PHASE-00, PHASE-02
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### RISK-412 — Interference from a co-loaded base mod
- Description: If a player keeps the original base mod (or a fork) enabled alongside Drive the City II, it can possess our career or borrowed vehicles (it accepts any entity with native car components). It also clears selections while driving, polls common keys in every context, and drives the camera in the same phase. The result can be authority conflicts, broken UI flows, and corrupted mission state. · Likelihood: M · Impact: H
- Mitigation: Detection (OPEN-023) using the candidates in BASE_MOD_AUDIT §9; refusing to enable our controllers, with a message; write-verify authority checks (SPEC-CONTROL); user documentation; different default keys. · Indicator/trigger: authority-conflict log lines; base-mod types present at load. · Owner role: Implementer · Phase: PHASE-01, PHASE-12
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

### RISK-413 — Saving while a native vehicle is borrowed persists a frozen vehicle
- Description: A takeover technique that freezes native navigation can leave that state in a save if the player saves mid-drive. On reload, especially without the mod, the vehicle may stay stuck or block a lane. · Likelihood: M · Impact: M (sandbox only; never career data)
- Mitigation: Keep a borrowed vehicle "handback-ready" at every frame boundary (SPEC-CONTROL); hand back before save events if they are detectable (OPEN-101); test with OPEN-412. · Indicator/trigger: a stuck vehicle after reload; vehicles with a pending path and no request. · Owner role: Implementer · Phase: TASK-0003 (observe), PHASE-02
- Raised in: docs/research/BASE_MOD_AUDIT.md §9
- Status: Open

**Quality group (501–519)**

### OPEN-501 — Logic-layer test harness
- Question: Which test runner and framework will the future logic test project use, which target framework can it run under alongside the mod's runtime target (see OPEN-003), and can the pure Career and Persistence logic be referenced from it without loading game assemblies?
- Impact: Without it, Logic-layer tests (TEST-CAREER-101/103, TEST-ECON-101, TEST-SAVE-002, TEST-SAVE-104, and others) cannot be automated, and the separation of pure logic promised by the architecture cannot be verified. No production test code is created in the baseline.
- Verification method: After OPEN-003 is answered, confirm the toolchain in TOOLCHAIN_MATRIX; build a minimal logic assembly with no game references and a test project that references it, in a PHASE-01 spike (installation only with owner authorization); record the choice in TOOLCHAIN_MATRIX and TEST_STRATEGY.
- Resolving phase/task: PHASE-01 (module-boundary work); depends on TASK-0002/TASK-0003 for OPEN-003
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-502 — Diagnostic readouts needed by tests
- Question: Which diagnostic readouts must the Diagnostics module expose for Integration and save-safety tests — entity registry counts per category, a player-state and movement-authority trace, a mission state trace, a ledger dump, a persistence readout (save identity, schema version, validation result, last-valid-copy status, quarantine contents), a city snapshot for before/after comparisons, telemetry (speed, position, height, per-frame displacement), and development-only tools to force invalid positions, remove vehicles, and inject subsystem faults — and how are the development-only tools kept out of release builds?
- Impact: Most Integration tests (CTRL, TRANS, ENTITY, SAVE, RECOV, CITY) cannot be judged objectively without them; results would rest on visual impressions only.
- Verification method: Derive the list from TEST_SCENARIOS and SAVE_SAFETY_TESTS; agree it with the architecture owner in SPEC-MODULES / DEBUGGING; prototype the first readouts (registry, persistence) in PHASE-01; confirm release builds exclude development-only tools (TEST-REL-101).
- Resolving phase/task: PHASE-01 (first readouts), extended in PHASE-02 to PHASE-05
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-503 — Scripted in-game test driver
- Question: Can in-game scenarios (fixed inputs, camera paths, drive routes, transition cycles) be executed by a development-only scripted driver inside the mod, or must every Integration and In-game test be performed manually?
- Impact: Reproducibility of TEST-DRIVE-002 (identical inputs across frame-rate conditions), benchmark camera paths, and the cost of soak campaigns (TEST-TRANS-101, TEST-SAVE-109).
- Verification method: PHASE-02/03 spike: replay a recorded input sequence through the mod's own input layer in Sandbox mode on a lab city and compare two replays' telemetry; community tooling listed in SOURCES (SRC-005) may be evaluated read-only, without installation unless the owner authorizes it.
- Resolving phase/task: PHASE-03
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-504 — Tested frame-rate range and reproducible frame-rate conditions
- Question: What frame-rate range constitutes "the tested frame-rate range" in REQ-DRIVE-002, what tolerance defines "consistent", and how can sustained reduced frame rates, single stalls, and fluctuating frame rates be induced reproducibly (for example game settings, a development-only stall injector in the mod, or an external limiter)?
- Impact: TEST-DRIVE-002, TEST-TIME-003, TEST-WORLD-001, and TEST-PLAYER-004 cannot pass until the range, tolerance, and induction method are defined; ADR-0004's comparison depends on them.
- Verification method: Measure the frame rates the owner's hardware actually reaches in the lab city (BENCHMARK_PROTOCOL); propose the range and tolerance in SPEC-DRIVING / SPEC-TIME for owner approval; prototype a development-only stall injector in the PHASE-03 laboratory; any external tool needs owner authorization and a TOOLCHAIN_MATRIX entry.
- Resolving phase/task: PHASE-03
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-505 — Benchmark measurement tooling and repeatable camera path
- Question: Which tools will capture per-frame time, process memory, and simulation speed for BENCHMARK_PROTOCOL, what overhead do they add, and how will a Building-mode camera path and a driving route be repeated exactly?
- Impact: REQ-PERF-001 requires a repeatable protocol and a baseline; without tooling no baseline can be recorded and the 10% investigation trigger cannot be applied.
- Verification method: Read-only review of the game's own performance displays (if any) and of candidate tools; evaluate a sampler inside the Diagnostics module; measure each tool's overhead against the vanilla baseline; record the chosen tools and versions in TOOLCHAIN_MATRIX; installation only with owner authorization.
- Resolving phase/task: PHASE-03 (from PHASE-01 if the lab exists)
- Raised in: docs/quality/BENCHMARK_PROTOCOL.md
- Status: Open

### OPEN-506 — Safe fault-injection methods for save-safety tests
- Question: How can forced termination during a write, full storage, read-only storage, and corrupted career records be simulated safely and repeatably — affecting only lab copies on storage designated for testing, never the system drive's other data, the game installation, or primary saves — for each storage option (In-save, Sidecar, Hybrid)?
- Impact: TEST-SAVE-004, TEST-SAVE-102, TEST-SAVE-103, TEST-SAVE-104, and TEST-SAVE-111 cannot run without an approved method; REQ-SAVE-004 cannot be verified.
- Verification method: Propose candidate methods per storage option during the PHASE-01 persistence spike (for example a development-only write-interruption hook inside the mod, a dedicated lab storage location with restricted space or permissions); review each for side effects; obtain explicit owner approval before first use; document the approved methods in SAVE_SAFETY_TESTS.
- Resolving phase/task: PHASE-01
- Raised in: docs/quality/SAVE_SAFETY_TESTS.md
- Status: Open

### OPEN-507 — Independent testers and hardware for PHASE-11 to PHASE-13
- Question: Who will act as the new player (TEST-ONBOARD-001) and the independent tester (TEST-REL-001, PHASE-13 gate), and which additional machines, store editions, and DLC sets will be available for the PHASE-12 beta? How are testers briefed and their data kept out of evidence?
- Impact: The PHASE-11, PHASE-12, and PHASE-13 gates require people and configurations other than the developer's own; without them those gates cannot pass.
- Verification method: Project owner decision on tester recruitment and available hardware before PHASE-11; record tester aliases (not personal data) and sanitized hardware profiles in the run records. The same decision covers how pre-release builds reach testers privately (merged from OPEN-602).
- Resolving phase/task: Project owner decision before PHASE-11; confirmed in PHASE-12 planning
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-508 — Holding and reading the game version during a test campaign
- Question: Can the installed game version be held constant for the duration of a test campaign (for example by controlling automatic updates in the store client), and where does the game report its exact version so that every run can record it reliably?
- Impact: Comparability of runs, benchmark baselines, and COMPATIBILITY_MATRIX rows; a silent update in the middle of a campaign invalidates its evidence (RISK-007).
- Verification method: Read-only observation of the store client's update settings and of the game's version display during TASK-0001; document the procedure in WINDOWS_SETUP; any settings change requires owner authorization.
- Resolving phase/task: TASK-0001 (observation); procedure confirmed in TASK-0003
- Raised in: docs/quality/TEST_STRATEGY.md
- Status: Open

### OPEN-509 — Proposed requirement: introduction for each career
- Question: Should the PRD add a requirement that every career (taxi, deliveries, freight, bus) has its own first-use introduction or tutorial? The ROADMAP PHASE-08 exit gate requires "a tutorial" for each career, but no `REQ-*` covers it: REQ-ONBOARD-001 covers only the first job, and REQ-ECON-005 only the economic tutorial.
- Impact: A phase-gate criterion without a requirement cannot be traced (TRACEABILITY) and has no primary test; TEST-ONBOARD-102 currently maps to REQ-ONBOARD-001 provisionally.
- Verification method: Project owner decides whether to add a requirement (PRD change control) or to reword the PHASE-08 gate; QA then remaps TEST-ONBOARD-102.
- Resolving phase/task: Project owner decision before PHASE-08 (ideally during PRD review of the baseline)
- Raised in: docs/quality/TEST_SCENARIOS.md
- Status: Open

### RISK-501 — Insufficient tester independence
- Description: The project may have a single person or agent acting as implementer, tester, and reviewer, so defects are validated by the same mind that introduced them, and gate evidence lacks independent review. · Likelihood: H · Impact: M
- Mitigation: Separate Reviewer role with self-review labeled as such; owner acceptance of gates; recruit external testers for TEST-ONBOARD-001 and PHASE-13 (OPEN-507); evidence written so that a third party can repeat it. · Indicator/trigger: gate evidence where the Reviewer is the Implementer; no tester available when PHASE-11 starts. · Owner role: Project owner · Phase: PHASE-05 onwards (critical from PHASE-11)
- Related: RISK-302 (fresh testers for onboarding).
- Status: Open

### RISK-502 — Proposed acceptance quantities miss rare defects
- Description: 50 transition cycles, 20 rides, and 30 save/load cycles give limited statistical confidence; a defect occurring once in hundreds of cycles (for example a rare duplicated reward or a stuck exit) can pass every gate and surface only after release. · Likelihood: M · Impact: H
- Mitigation: Logic-layer randomized tests for settlement and ledger invariants (TEST-CAREER-103, TEST-ECON-101); long sessions and soaks (TEST-SAVE-114, TEST-ENTITY-101); automatic anomaly logging; treat any single critical failure as blocking regardless of the count. · Indicator/trigger: defects first observed in PHASE-12 or after release that earlier campaigns did not reveal. · Owner role: QA · Phase: PHASE-05, PHASE-06, PHASE-12
- Status: Open

### RISK-503 — Logic-test doubles diverge from host-game behavior
- Description: Logic tests run against test doubles of CityBridge and other ports; if a double assumes behavior the real game does not have (for example reachability, entity removal timing, save callbacks), logic tests pass while the game fails. · Likelihood: M · Impact: M
- Mitigation: Every port contract used by logic tests also has an Integration test in the lab; doubles are derived from recorded in-game observations (CS2_INTEGRATION_FINDINGS); re-check doubles after each game update. · Indicator/trigger: an Integration failure in an area whose logic tests pass. · Owner role: QA · Phase: PHASE-01 onwards
- Status: Open

**Planning group (601–619)**

### OPEN-601 — Location of the pinned upstream source copy for the baseline build
- Question: Where may the pinned base-mod source be placed for the TASK-0003 build (outside the OneDrive-synced project root, in a git-ignored folder inside it, or another location the owner designates), and in which form (a Git working copy checked out at the pinned commit, or a source archive of that commit)?
- Impact: TASK-0003 cannot obtain the source without it. Vendoring into the tracked project is not authorized. OneDrive sync may interfere with builds or upload third-party code (RISK-011). The provenance of what was built must be traceable to the pinned commit.
- Verification method: Owner decision informed by TASK-0001 observations (OPEN-028: sync status, disk space) and the options in UPSTREAM_STRATEGY; record the decision in UPSTREAM_STRATEGY and in the TASK-0003 authorization row.
- Resolving phase/task: TASK-0003 precondition (PHASE-00, WP-00-03)
- Raised in: docs/planning/tasks/TASK-0003-isolated-baseline-validation.md
- Status: Open

### OPEN-602 — Independent testers and pre-release build delivery
- Question: Who will act as independent testers for the PHASE-11 new-player sessions, the PHASE-12 beta (varied hardware, DLCs, mod sets), and the PHASE-13 acceptance run, and how will they receive pre-release builds privately, without a public publication?
- Impact: The PHASE-11, PHASE-12, and PHASE-13 gates require players other than the implementer; hardware variety depends on the tester pool; tester data must be sanitized (RISK-030); private distribution may be constrained by platform rules (OPEN-017).
- Verification method: Owner decision; read-only research of private or test distribution options on the intended platform (OPEN-017); record the result in PACKAGING_AND_PUBLISHING and the PHASE-12 beta plan.
- Resolving phase/task: PHASE-12 (WP-12-02); must be settled before PHASE-11 (WP-11-11)
- Raised in: docs/planning/phases/PHASE-12-beta-and-validation.md
- Status: Merged into OPEN-507 (2026-10-18), which now also covers private delivery of pre-release builds. This entry is kept because phase documents reference it.

### OPEN-603 — Source of laboratory and representative test cities
- Question: Where do the lab city and the representative beta cities come from (built by the owner or the team for testing, or third-party maps or saves), and what provenance and terms apply to third-party ones?
- Impact: PHASE-00 baseline lab city (WP-00-06); PHASE-03 laboratory scenarios that need bridges, tunnels, and stacked roads (WP-03-01); PHASE-12 small, medium, and large cities (WP-12-01). Primary saves must never be used (RISK-028). Third-party content may need provenance records (ASSET_PROVENANCE).
- Verification method: Owner decision; a minimal lab-city specification in TEST_SCENARIOS; a check of the terms of any third-party map before use; record the result in TEST_STRATEGY.
- Resolving phase/task: PHASE-00 (WP-00-06) for the first lab city; PHASE-03 (WP-03-01); PHASE-12 (WP-12-01)
- Raised in: docs/planning/phases/PHASE-03-surfaces-collisions-and-physics.md
- Status: Open

### OPEN-604 — Stable technical identifiers independent of the working title
- Question: Which stable technical identifiers (mod ID, assembly name, persistence namespace and keys, settings keys, log names) will the mod use, decoupled from the working title "Drive the City II", so that a later rename does not orphan saves, settings, or logs?
- Impact: REQ-SAVE-001 and REQ-SAVE-002 (persisted data must stay findable and migratable); REQ-COMPAT-001 (identity distinct from the original base mod); RISK-015 (player-facing naming is reviewed before release and may change).
- Verification method: Decide before the first persisted write in PHASE-01; record in SPEC-SAVE and SPEC-MODULES; check that the identifiers do not collide with the base mod's identifiers recorded in BASE_MOD_AUDIT.
- Resolving phase/task: PHASE-01 (WP-01-01)
- Raised in: docs/planning/phases/PHASE-01-foundation-and-save-safety.md
- Status: Open

### RISK-601 — Unmodified baseline build may require game-relative placement or project-file edits
- Description: If the pinned base-mod project resolves game assemblies through paths relative to the game installation (the preliminary read-only source check suggests it may) instead of the official toolchain mechanism, building it "as is" could require placing source or outputs near or inside the game installation (forbidden) or editing the project file (which makes the run less of a pure baseline). · Likelihood: M · Impact: M
- Mitigation: TASK-0002 records the exact reference-resolution mechanism and the expected deviations; TASK-0003 uses only a non-functional build-configuration override that keeps all files outside the game installation, lists every deviation in the RUN record, and changes no source behavior; the PHASE-00 recovery allows an owner-approved alternative gate if the build still fails. · Indicator/trigger: references cannot be resolved without moving files into or near the game installation or editing the project file. · Owner role: Implementer · Phase: PHASE-00
- Raised in: docs/planning/phases/PHASE-00-audit-and-windows.md
- Reassessment proposal (TASK-0002 (2026-10-18, pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`)): likelihood **M → L**. The relative managed-assembly path is a plain MSBuild property, so a command-line override should take precedence without editing the project file **[Unverified]**; TASK-0003 confirms it. Residual: the .NET SDK and the .NET Framework 4.8 reference assemblies are missing on this machine (TASK-0001), and installing them needs authorization.
- Status: Open

### RISK-602 — In-game validation depends on human-operated sessions
- Description: Agents cannot play or watch the game unless suitable tooling exists. Every in-game test, campaign (50 transition cycles, 20 taxi rides, 30 save/load cycles, two-hour sessions), new-player session, and the M2 fun checkpoint needs the owner or another human tester to operate the game and capture evidence. Gates may wait on unscheduled sessions. · Likelihood: H · Impact: M
- Mitigation: Step-by-step test procedures and checklists; diagnostics counters and structured logs that make outcomes objectively checkable (entity registry counts, active movement authority, ledger reconciliation); maximum coverage by logic tests that run without CS2; batching in-game tests into planned sessions per gate; recording sessions for later review. · Indicator/trigger: a phase in Gate review waiting on sessions; evidence records missing observed results. · Owner role: Coordinator · Phase: PHASE-00 onwards
- Raised in: docs/planning/phases/PHASE-00-audit-and-windows.md
- Status: Open

**Agents, release, and user group (701–719)**

### OPEN-701 — Distribution channel choice and exclusivity
- Question: Which channel or channels will distribute v1.0 (Paradox Mods, Melty, both, or another)? Do any channel's terms (for example, the exclusive license over published content described in Melty's terms, [Documented upstream], read 2026-10-18) prevent publishing the same mod on more than one channel, or conflict with the licenses of incorporated components?
- Impact: Package format, dependency declaration, update and hotfix flow, support channel (OPEN-702), and license compatibility. PACKAGING_AND_PUBLISHING, INSTALL_UPGRADE_UNINSTALL, and POST_RELEASE_MAINTENANCE cannot give channel-specific steps until this is decided.
- Verification method: Read-only research of each candidate's current publishing requirements and terms (OPEN-017; MELTY_EVALUATION under OPEN-016), recorded in SOURCES with access dates; a legal reading of any exclusivity clause if Melty remains a candidate; then a project-owner decision recorded in the register and in PACKAGING_AND_PUBLISHING.
- Resolving phase/task: PHASE-13 (owner decision before the release candidate; research may start earlier)
- Raised in: docs/release/PACKAGING_AND_PUBLISHING.md (also cited in INSTALL_UPGRADE_UNINSTALL.md, POST_RELEASE_MAINTENANCE.md, DEFINITION_OF_READY.md)
- Status: Open

### OPEN-702 — Support, issue-reporting, and security-reporting channels
- Question: Where will players report problems and receive notices (for example after a game update breaks the mod), and which private route will receive security reports?
- Impact: TROUBLESHOOTING, SECURITY.md, and POST_RELEASE_MAINTENANCE cannot name any contact; hotfix and game-update notices have no outlet; diagnostics bundles (REQ-DIAG-001) have no destination. Release tasks are not Ready without it (DEFINITION_OF_READY §6).
- Verification method: Project-owner decision informed by OPEN-029 (repository hosting) and OPEN-701 (channel); read-only check of the facilities the chosen channel documents; record the decision in SECURITY.md §6, TROUBLESHOOTING §5, and POST_RELEASE_MAINTENANCE §7.
- Resolving phase/task: PHASE-13 (before the first publication, including any test publication involving external players)
- Raised in: docs/release/POST_RELEASE_MAINTENANCE.md (also SECURITY.md, docs/user/TROUBLESHOOTING.md)
- Partial answer (2026-10-20): with the repository on GitHub, [SECURITY.md](../../SECURITY.md) §6 routes security reports to GitHub's private vulnerability reporting (the owner enables it in the repository settings) and bug reports to GitHub issues. Player support and game-update notices for a published mod remain open until a release channel exists (OPEN-701).
- Status: Partly answered (2026-10-20)

### RISK-701 — Work lost or overwritten without version control
- Description: Until a Git repository exists (OPEN-029), documentation changes (and later code) have no history. Concurrent or later sessions, or OneDrive synchronization conflicts (RISK-011), can overwrite or lose changes with no reliable way to see or restore what changed. Any sync-provider version history is unverified as a recovery mechanism. · Likelihood: M · Impact: M
- Mitigation: One writer per file; re-read each file immediately before editing; dated CHANGELOG and task status-history entries; report sync-conflict copies instead of resolving them silently; owner decides OPEN-029 before PHASE-01 code work (implementation tasks are not Ready without a repository, DEFINITION_OF_READY §5). · Indicator/trigger: unexplained file changes, sync-conflict copies, a document losing recently recorded content. · Owner role: Project owner · Phase: PHASE-00 (until OPEN-029 is resolved)
- Raised in: docs/agents/WORKFLOW_AND_HANDOFF.md
- Closed (2026-10-20): Git has been in use since 2026-10-19 and `main` is pushed to GitHub (OPEN-029).
- Status: Closed

