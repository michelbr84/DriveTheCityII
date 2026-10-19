# Base Mod Audit — Beta Test Driving Mod

| Field | Value |
|---|---|
| ID | DOC-BASE-AUDIT |
| Document status | Draft |
| Release scope | v1.0 (foundation decision) |
| Phases | PHASE-00 (TASK-0002 source audit, TASK-0003 baseline), PHASE-02 (consumption) |
| Responsible role | Coordinator (maintainer); the Implementer executes TASK-0002 and TASK-0003; a Reviewer checks the conclusions |
| Related requirements | REQ-DRIVE-001, REQ-CTRL-001, REQ-ENTITY-001, REQ-ENTITY-002, REQ-COMPAT-001, REQ-REL-003 |
| Related documents | [SOURCES](SOURCES.md) (SRC-001), [FEASIBILITY_MATRIX](FEASIBILITY_MATRIX.md), [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [TASK-0002](../planning/tasks/TASK-0002-base-mod-source-audit.md), [TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md), [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md) |
| Last updated | 2026-10-18 |

> **Status:** nothing has been cloned, built, installed, or run.
>
> - **TASK-0002 read-only source audit:** completed on 2026-10-18 at the pinned commit
>   `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`. The results are in **§9**. Its validation status is **Not run**,
>   because no build or runtime check is part of a source audit.
> - **Section 2:** the **preliminary documentation-time review** from the documentation bootstrap (TASK-0000), kept
>   for history. Where §2 and §9 differ, **§9 wins**.
> - **No revision has been adopted.** Reuse is conditional on the license audit and the baseline tests
>   **[Owner requirement]**.

## 1. Purpose

The Beta Test Driving Mod ("BTDM") is the owner-designated **candidate** foundation for direct vehicle control. This
document records:

- what is known about its license, dependencies, design, and limits;
- what TASK-0002 must still read;
- which parts look reusable and which are tightly coupled to game internals;
- what TASK-0003 must demonstrate before any reuse decision.

The decision itself (fork, selective re-implementation, or reference only) is made later, as described in
[UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md).

## 2. Preliminary documentation-time source review (2026-10-18, read-only, not TASK-0002)

> **Superseded where it differs.** TASK-0002 re-read every file at the pinned commit. It confirmed most statements
> below and corrected several of them. The corrections are listed in §9.3, and §9 is the authoritative record.

### 2.1 Method and evidence levels

- **Raw:** the coordinator fetched selected files and API responses with `curl` (read-only) and checked the exact text (V-01 … V-07 in [SOURCES](SOURCES.md)). The research writer read a few more lines from the same raw files; those are marked "(raw)".
- **Extraction:** all other files were read through a summarizing fetch tool. Symbols and numbers may contain extraction errors (RISK-406).
- Two large files were read only partly: the first 100,000 characters of `Systems/DirectDriveControlSystem.cs` (102,017 characters) and of `Systems/ReliableDriveSystem.cs` (144,230 characters).
- Raw files were fetched from branch `main`, not from SHA-pinned URLs.

### 2.2 Identity and candidate revision

| Item | Value | Level |
|---|---|---|
| Repository | [github.com/northernst11-bot/BetaTestDrivingMod](https://github.com/northernst11-bot/BetaTestDrivingMod) | — |
| Candidate audit revision | `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, 2026-06-17T10:19:57Z, "Publish 0.3.54 stable smoother driving update". This is a **candidate for audit, not adopted**. | Raw (V-02) |
| Repository history | Created 2026-05-30; 8 commits on `main`; no tags; no releases | Extraction |
| Mod version | `0.3.54-stable` (README line 3, PublishConfiguration, and `mod.json`) | Raw (README, V-06); Extraction (`mod.json`) |
| Paradox Mods | `ModId` 145145. The listing exists, but only its title was readable. | Raw (V-06); Partial |
| Declared game version | `GameVersion` "1.5.*" in `Properties/PublishConfiguration.xml`. The README states no game version. | Raw (V-06, V-05) |
| Declared mod dependencies | None: no `Dependency` element; `mod.json` has `dependencies: []` | Raw (V-06 fetch); Extraction |
| Harmony | Not used | Extraction |

### 2.3 License and copyright

- `LICENSE`: "MIT License", "Copyright (c) 2026 stoot" (raw, V-01). The GitHub API also reports MIT (extraction).
- `NOTICE.md` (raw, V-07):
  - The project is "open source under the MIT License" and asks users to "keep the MIT copyright and license notice with substantial copies".
  - It grants no rights to "Cities: Skylines II, Paradox or Colossal Order trademarks, game assets, or third-party content not owned by this project".
- `Properties/AssemblyInfo.cs` names Company "stoot" and has an **empty `AssemblyCopyright`** (extraction).
- The GitHub account (`northernst11-bot`) differs from the author name ("stoot"). The relationship is not explained (OPEN-405).
- Commit `af355236…` (2026-05-31) is titled "Relicense mod source under MIT" (extraction). The license of the earlier history was **not inspected** (OPEN-405).
- No legal conclusion is drawn here. See [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md).

### 2.4 Repository contents and apparent responsibilities

Root entries are raw-verified (V-03). Responsibilities come from extraction.

| Path | Size (bytes) | Apparent responsibility | Registered in `Mod.cs`? |
|---|---|---|---|
| `Mod.cs` | 1,975 | `IMod` entry point (`OnLoad`/`OnDispose`); registers systems; creates the HUD; applies safe defaults | — |
| `Systems/DirectDriveRuntime.cs` | 39,249 | Static global state (driving flag, possessed entity, pose, speed), legacy input sampling, tuning, toggles, `PlayerPrefs` key bindings, HUD object creation | Not a system |
| `Systems/DirectDriveControlSystem.cs` | 102,017 | Possession, per-tick kinematic control, road pose, turn intent, traffic presence, low-FPS smoothing, release and handback. **Last 2,017 characters unread.** | Yes (after the native car-move system) |
| `Systems/DirectDriveVehicleCollisionSystem.cs` | 40,180 | Despite its name, a `partial class` of the control system: swept-box collision against other cars | Part of the control system |
| `Systems/DirectDriveFreezeSystem.cs` | 2,887 | While driving, parks native navigation just ahead of the car at zero speed and clears the path buffer | Yes (between native navigation and car move) |
| `Systems/DirectDriveCameraSystem.cs` | 8,580 | Chase camera through the game's camera update system; restores the previous controller | Yes (pre-culling) |
| `Systems/DirectDriveInputSystem.cs` | 244 | Calls input sampling | Yes (UI update) |
| `Systems/DirectDriveUiSystem.cs` | 28,174 | C#↔UI value and trigger bindings (about 52 and 39), including key-bind strings | Yes (UI update) |
| `Systems/DirectDriveHudBehaviour.cs` | 15,072 | IMGUI fallback panel | Created by `Mod.cs` |
| `Systems/DirectDriveTrafficPresenceSystem.cs` | 11,853 | Lane-presence insertion; equivalent logic exists inside the control system | **No** |
| `Systems/DirectDriveTrafficGuardSystem.cs` | 16,580 | Slows AI cars approaching the player; the README says it was removed from the update order | **No** |
| `Systems/DirectDrivePoliceChaseSystem.cs` | 28,385 | Retargets police cars and writes crime and dispatch state; gated off by default | **No** |
| `Systems/ReliableDrive*.cs` (8 files) | 144,230 for the largest | Older control stack, including watercraft and train navigation; replaced in 0.3.0 per the CHANGELOG. **Only 2 of 8 files read; the tail of the largest is unread.** | **No** |
| `BetaTestDrivingMod.mjs` | 28,756 | Hand-written UI module appended to `GameTopLeft` and `Game` | Loaded by the game UI |
| `Properties/PublishConfiguration.xml` | 9,928 | Paradox Mods metadata | — |
| `mod.json`, `Properties/AssemblyInfo.cs` | 308, 587 | Manifest; assembly attributes | — |
| `CHANGELOG.md`, `README.md`, `NOTICE.md`, `LICENSE` | 31,607; 12,252; 548; 1,062 | Documentation and license | — |
| `Resources/`, `images/`, `Properties/*.png` and `*.jpg` | ~1–1.65 MB each | Screenshots, thumbnail, icon | — |

No committed binary DLLs were seen in the tree (extraction).

### 2.5 Project file facts (raw, V-04)

- `Sdk="Microsoft.NET.Sdk"`, `TargetFramework` **net48**, `LangVersion` **9.0**, `Nullable` disable, `ImplicitUsings` disable, `GenerateAssemblyInfo` false, `OutputPath` `bin\`.
- `ManagedPath` is the **relative** path `..\..\Cities2_Data\Managed`. This implies that the project expects to sit two folders below a directory containing `Cities2_Data`, presumably the game installation (an inference, not verified). The project does not use the `CSII_*` variables that the official toolchain defines ([CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md)).
- **No `Import` of the official Mod.props/Mod.targets, no `PackageReference`, no custom `Target`.** The official ModPostProcessor step (SRC-008) is therefore not wired in by this file. Whether the published build used the post-processor is **unknown** (OPEN-403, RISK-403).
- 18 assembly references, all `Private=false` (not copied to the output):
  - game: `Game`;
  - Colossal: `Colossal.Core`, `.Collections`, `.Mathematics`, `.Logging`, `.IO.AssetDatabase`, `.Localization`, `.UI`, `.UI.Binding`;
  - Unity: `Unity.Collections`, `Unity.Entities`, `Unity.Mathematics`;
  - UnityEngine: `UnityEngine.CoreModule`, `.InputLegacyModule`, `.PhysicsModule`, `.IMGUIModule`, `.ImageConversionModule`;
  - other: `System.Memory`.
- Compilation excludes `publish\**` and **`decompiled_published_0317\**`**; both folders are also listed as non-compiled items. Neither folder exists at the repository root (V-03). According to the extraction, `.gitignore` covers `publish/` but not the decompiled folder. The folder name suggests a local decompilation of some published build; which build, and whose, is not stated (OPEN-404, RISK-404).
- Files copied to the output: the icon PNG, `BetaTestDrivingMod.mjs`, and two `images/` files.

### 2.6 Design: possession of live native vehicles

README line 19 (raw): "It does not spawn a fake player car. It takes over a real live vehicle already created by
Cities: Skylines II, freezes the possessed car's vanilla physical path movement, then applies direct player movement
after the normal car move step." The possession target is the selected entity or, failing that, the nearest car
(extraction).

- This **matches our `BorrowedNativeVehicle` concept in Sandbox mode** ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)).
- It does **not** provide career vehicles. REQ-ENTITY-002 forbids career mode from appropriating a citizen's or service vehicle, so spawning and removing rental and owned vehicles remains our problem (**OPEN-012**). So does the question whether the same control technique can drive a mod-spawned vehicle (**OPEN-402**).

### 2.7 Control, freeze, and handback

- **Freeze** (extraction): while driving, the native navigation target is pinned just ahead of the car with zero maximum speed, the path buffer is cleared, and the path owner is set to a pending state ("park live pathfinding").
- **Control** (extraction):
  - writes the object's transform, its moving state, and the predicted transform-frame buffer after the native car-move system;
  - computes speed and steering kinematically;
  - finds the road pose through a nearby-lane search and aligns pitch to the road grade;
  - can queue AI turn intent by writing navigation-lane data.
- Whether the "road intent assist" function is actually called could not be confirmed in the part that was read.
- **Handback:**
  - README (raw): the 0.3.53 release "Rebuilds vanilla pathfinding from the car's current lane and position when you release control" (line 34) and "Clears the parked route state on release so vanilla traffic rebuilds a fresh path from the exact place you let go" (line 65).
  - Extraction: on release, the current-lane and path-owner state are marked obsolete and the path buffer is cleared.
  - This is a candidate technique for REQ-ENTITY-001, **not** a verified one (OPEN-011).
- **Traffic presence:**
  - README (raw, line 68): the car is purged "from every lane-buffer marker touched during the drive so lanes you left should not keep an invisible queue blocker".
  - Extraction: lane-object buffers are written through a game utility.
- **Stability history** (CHANGELOG, extraction): several entries mention native or Burst crashes after writes to traffic vehicles, and road-entry current-lane writes "disabled". These writes are fragile.

### 2.8 Collision and world contact

- Contact with **vehicles only**: swept oriented boxes in the horizontal plane, a candidate cache refreshed about every 12 frames, and fixed keyword-sized hitboxes. There is no damage, impact component, or event, and no pedestrian, building, or prop contact (extraction).
- **No building collision:** "Building collision and crash fire effects are not added in this build." (README line 111, raw, V-05)
- Targets at different road heights are skipped "so cars below/above bridges do not create invisible barriers" (line 96, raw).
- Hit cars are not pushed or rewritten (line 106, raw).
- No use of terrain or water systems was seen in the part that was read (extraction).

### 2.9 Camera

The chase camera works through the game's camera update system (active, orbit, and gameplay controllers), with the
main camera as a fallback. Distance and height scale with speed, and the previous controller is restored on release
(extraction). Restore behavior on unload or exception is not yet confirmed.

### 2.10 Input and settings

- Input uses legacy `UnityEngine.Input` key polling (extraction), which is consistent with the `UnityEngine.InputLegacyModule` reference (raw).
- Key bindings persist in `PlayerPrefs` under a mod-specific prefix. Tuning values are not persisted (extraction).
- This differs from the official mechanism (`ModSetting` with `ProxyBinding`, SRC-010).
- The README acknowledges an input overlap: "Use the arrow keys if WASD moves your game camera or screen." (line 55, raw). This is relevant to REQ-CTRL-003 and RISK-020.

### 2.11 User interface

- A `UISystemBase`-derived system exposes value and trigger bindings in group `"betatestdrivingmod"` (extraction).
- A hand-written `.mjs` module reads the runtime globals `cs2/api`, `cs2/ui`, and `React`, and appends panels to `GameTopLeft` and `Game` (extraction). This is consistent with the official UI hooks (SRC-009).
- An IMGUI `MonoBehaviour` panel is kept as a fallback (extraction; `UnityEngine.IMGUIModule` is referenced, raw).

### 2.12 Persistence

There is no save-game serialization (extraction). Nothing career-like exists. The only persisted data is key
bindings in `PlayerPrefs`.

### 2.13 Documentation consistency

- The CHANGELOG's newest version heading is `0.3.27-stable`, behind the 0.3.54 of the README and manifest (extraction).
- The README states no supported game version (raw). The only version signal is `GameVersion` "1.5.*" (raw).
- Issue #1 (open, 2026-10-05) is by a third party who announces an MIT fork with Chinese localization and a handling rework. It reports two suspected bugs: an unnecessary vehicle-kind save-back, and a collision resolver that re-corrects the same entity and causes stutter (extraction; unverified).

## 3. License and dependency checklist

Statuses updated by TASK-0002 on 2026-10-18. Every "Raw (pin)" entry was read at commit
`eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` ([Source-inspected]; details in §9).

| # | Item | Status (2026-10-18, TASK-0002) | Evidence | Next step |
|---|---|---|---|---|
| L-01 | License file present and identified | **Confirmed:** standard MIT text, "Copyright (c) 2026 stoot" | Raw (pin): `LICENSE` | Owner decision on reuse (OPEN-027) |
| L-02 | Copyright holder identity | **Partly answered:** "stoot" in `LICENSE`, `mod.json` author, and `AssemblyCompany`; `AssemblyCopyright` empty; all 8 commits authored as "stoot". The repository owner account is `northernst11-bot`; the relationship is not stated. | Raw (pin); GitHub API commit metadata | OPEN-405 (owner question, §9.4) |
| L-03 | License of history before the relicensing commit | **Answered:** the first 4 commits carried a restrictive "Source-Available Transparency License" ("not open source", all rights reserved). MIT applies from `af355236521636e987801e4c14019aaf187ec277` (2026-05-31). | Raw `LICENSE` and `NOTICE.md` at `9955221…` and `af35523…` | Legal questions Q-01 and Q-02 in [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) |
| L-04 | Per-file headers and other notices | **None:** no copyright or license header in any `.cs`, `.mjs`, or `.svg` file | Raw (pin), every file | — |
| L-05 | NOTICE obligations | **Confirmed:** keep the MIT notice with substantial copies; no rights to game trademarks, game assets, or third-party content | Raw (pin): `NOTICE.md` | Carried into REUSE_AND_LICENSE_AUDIT |
| L-06 | Embedded third-party or decompiled material | **Open, with new facts:** the `decompiled_published_0317` exclusion was added on 2026-06-04 and the folder was never committed. The CHANGELOG says the collision layer was "ported" from a "CS2 GTA Edition" project. | Raw (pin): csproj, CHANGELOG; commit file lists | OPEN-404; proposed OPEN-411 (§9.4) |
| L-07 | Committed binaries | **Confirmed none:** the only binary files are 6 PNG/JPG images (2 pairs are byte-identical blobs) | Pinned tree listing (API, not truncated) | — |
| L-08 | Images and screenshots | Present (game-rendered screenshots and an icon); not reuse candidates | Pinned tree | — |
| D-01 | Declared runtime mod dependencies | **Confirmed none:** no `Dependency` element; `mod.json` has `"dependencies": []` | Raw (pin) | — |
| D-02 | NuGet packages | **Confirmed none** | Raw (pin): csproj | Implicit framework reference-assembly package possible at build time (§9.13) |
| D-03 | Harmony / BepInEx | **Confirmed not used:** no Harmony, BepInEx, `BaseUnityPlugin`, or reflection-based patching in any file | Raw (pin), every file | — |
| D-04 | Build-time game assembly references | **Confirmed:** 18 HintPath references, all `Private=false` (list in §9.5) | Raw (pin): csproj | §9.13 build plan |
| D-05 | Official toolchain integration | **Confirmed not imported:** no Mod.props/Mod.targets, no post-build step. The source uses no Burst, jobs, or `SystemAPI`. | Raw (pin) | OPEN-403 (TASK-0003) |
| D-06 | Target framework and language version | **Confirmed:** `net48` / C# 9.0 | Raw (pin): csproj | OPEN-003 (build confirmation in TASK-0003) |
| D-07 | Declared game version compatibility | **Confirmed:** `1.5.*` in every revision. The installed game is 1.6.2f1 ([TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md)). There is no runtime version check in the code. | Raw at 5 revisions | RISK-001 (proposed: likelihood H) |
| D-08 | Paradox Mods listing data | Not read (JavaScript shell) | Partial | Only if needed, in a browser with owner permission |

## 4. Inspection plan for TASK-0002 (raw, read-only)

> **Executed on 2026-10-18.** Every file in the plan was read in full at the pinned commit (coverage in §9.2). The
> answers to the questions in the table are in §9.6–§9.12. The open question whether the game creates unregistered
> systems automatically could not be settled from source; it is proposed as OPEN-413.

TASK-0002 reads every file below **at the pinned SHA**, through SHA-pinned raw URLs or the GitHub API. It records the
blob identifiers and turns every extraction-level statement in §2 into either a raw-verified statement or a
correction.

| Area | Files | Questions to answer |
|---|---|---|
| Control | `DirectDriveControlSystem.cs` (complete, **including the unread last 2,017 characters**), `DirectDriveFreezeSystem.cs`, `DirectDriveRuntime.cs`, `Mod.cs` | Which native components and buffers are written, and in which update phase? What is the exact system ordering? How is the target selected? Is the road-intent function called? What happens on unload, on load, or when the possessed entity is deleted? |
| Handback | Same files | The exact release sequence. Which lane and path state is reset? Is anything left registered in lane buffers? |
| Camera | `DirectDriveCameraSystem.cs` | Which controllers are touched, and is the restore path guaranteed on release, unload, and exception? |
| Input | `DirectDriveInputSystem.cs`, `DirectDriveRuntime.cs`, `DirectDriveUiSystem.cs` | Key polling, focus-loss handling, suppression (or not) of game input, `PlayerPrefs` keys |
| Traffic | Control system presence code; `DirectDriveTrafficPresenceSystem.cs`; `DirectDriveTrafficGuardSystem.cs` | Writes to lane-object buffers and AI velocities. Can systems **not** registered in `Mod.cs` still be created by the game (for example through automatic system creation)? This is unknown and must be answered before stating that they never run. |
| Collision | `DirectDriveVehicleCollisionSystem.cs` | Hitbox sizing, candidate queries, any writes to other entities; the issue #1 "re-correction" claim |
| Out-of-scope code | `DirectDrivePoliceChaseSystem.cs`; all 8 `ReliableDrive*.cs` files (**including the unread last 44,230 characters** of `ReliableDriveSystem.cs` and the six files that were only listed) | Are they compiled into the assembly (they are not excluded)? Does anything reference them? Which native state could they write? |
| UI | `DirectDriveUiSystem.cs`, `BetaTestDrivingMod.mjs`, `DirectDriveHudBehaviour.cs` | Binding names and groups; global objects used; IMGUI lifetime |
| Project and legal | `BetaTestDrivingMod.csproj`, `mod.json`, `AssemblyInfo.cs`, `.gitignore`, `CHANGELOG.md` (all), `LICENSE` and `NOTICE.md` at earlier revisions | License history, file headers, decompiled-folder history, build assumptions |

TASK-0002 records its results in §2 (upgrading the levels), in [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md),
in [CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md) (the native systems and components used), and in the register.

## 5. Reuse candidates and version-coupled parts (assessment only)

This is a reading-based assessment. Nothing here is tested, and nothing is incorporated.

> **Superseded by §9.10:** the final TASK-0002 classification (reusable code, concept only, not reusable). §9.11
> lists the version-coupled parts and §9.12 lists the co-loading conflicts. The table below is the preliminary view,
> kept for history.

| Element | Assessment | Coupling to game internals | Notes |
|---|---|---|---|
| Possession flow (freeze before the native car move, control after it) | Pattern for Sandbox mode | High: depends on native system order and component layout | Would sit behind the Vehicles and CityBridge boundaries ([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md)) |
| Handback by invalidating path state and purging lane markers | Pattern to validate | High | PHASE-02 route and traffic tests decide (OPEN-011) |
| Lane-presence writes | Pattern to validate | High | Risk of invisible blockers (RISK-004) |
| Kinematic speed and steering math | Possible code candidate if separable | Low for the pure math | Subject to ADR-0004; only one movement authority may be kept |
| Delta-time clamping under low FPS | Pattern | Low | Relevant to REQ-TIME-003 |
| Swept-box vehicle contact | Pattern | Medium (reads other vehicles) | Pedestrians are not covered; our rule comes from OPEN-032 |
| Chase camera with controller restore | Pattern | Medium–high | Camera ownership in [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md) |
| C# UI bindings plus `.mjs` registry append | Pattern consistent with the official UI stack | Low–medium | Our UI follows [SPEC-UI](../experience/UI_AND_HUD.md) |
| IMGUI fallback panel | Not proposed for reuse | Low | Not part of the official UI stack |
| Legacy input with `PlayerPrefs` key bindings | Not proposed for reuse | Low | An official remapping path exists (REQ-INPUT-001) |
| Police chase | Not proposed for reuse | High; writes crime and dispatch state | Excluded from v1.0 scope; would violate REQ-CITY-001 |
| Traffic guard (forces AI velocities) | Not proposed for reuse | High; writes native agents | Removed upstream from the update order |
| `ReliableDrive*` stack (including watercraft and train) | Not proposed for reuse | High | Legacy and unregistered |
| Screenshots and images | Not proposed for reuse | — | Game-rendered content |

Version-coupled facts to re-check after every game update: the native systems the mod orders itself against
(navigation, car move, camera update), the written components (transform, moving state, transform frames, navigation
lanes, path owner and path elements, lane objects, current lane), and the declared `GameVersion`.

## 6. Upstream strategy

Options (fork, selective re-implementation, reference only), pinning, attribution, tracking of upstream changes and the
community fork, and the rule against co-loading are described in [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md).
Until a decision is recorded, BTDM is **read-only reference material**. Nothing from it is in this project.

## 7. Baseline validation requirements for TASK-0003

TASK-0003 ([task file](../planning/tasks/TASK-0003-isolated-baseline-validation.md)) may start only after these
preconditions hold:

- TASK-0001 recorded the game version, platform, and toolchain state;
- TASK-0002 is complete;
- the project owner has authorized the needed installs, the build, and the game launch;
- a lab city with copies of saves and an isolated mod set exists (OPEN-035).

All items below are **proposed** checks. Their validation status is **Not run**.

| # | Check | Expected per upstream claims | Evidence to record |
|---|---|---|---|
| BL-01 | The source at the pinned SHA builds without modifying the game folder | Unknown (OPEN-403) | SDK version, the exact command used, the output file list, and warnings |
| BL-02 | The mod loads in the lab city, alone | Unknown (`GameVersion` 1.5.* versus installed) | Game version and log excerpt (sanitized) |
| BL-03 | Possess a car and drive a fixed route (curves, junction, slope) | Driving works | Recording and notes |
| BL-04 | Release the car; observe it and the traffic behind it for a fixed period | Path rebuilt; no queue | Recording and entity observations |
| BL-05 | Residual lane blockers after several possess/release cycles | None (README line 68) | Observation method and result |
| BL-06 | Chase camera on and off, then back to the building camera | Previous controller restored | Recording |
| BL-07 | Input overlap with game camera and tools | WASD may move the game camera (README line 55) | Which keys conflict |
| BL-08 | Contact with another car; drive into a building | Car contact present; buildings passed through (README line 111) | Recording |
| BL-09 | Elevated road or bridge over another road | No barriers from other levels (README line 96) | Recording |
| BL-10 | Low frame rate (limited by an owner-approved method) | Capped catch-up | Observations |
| BL-11 | Save while not driving, reload, disable the mod, reload | City loads; nothing persisted by BTDM | Load results |
| BL-12 | Errors and exceptions in the game log during BL-02 … BL-11 | — | Sanitized excerpts |
| BL-13 | Possession target: press V with a selected car, then with nothing selected | Selected car first; otherwise the nearest live car within 180 m of a point 35 m ahead of the camera (§9.6.2) | Vehicle name from the panel; log line "Direct Drive possessed …" |
| BL-14 | Select a service vehicle (bus, police, ambulance) and press V. **Release immediately.** Do this only if the owner approves it for the lab city. | Allowed by the code; service vehicles are only deprioritized in the nearest-car search (§9.6.2) | Whether possession happens; native dispatch effects after release |
| BL-15 | Driving model at default tuning | Top speed about 42 mph; brake first, reverse only on a second press when stopped; steering at standstill rotates the car in place (§9.6.4, inference from code) | Recording and notes |
| BL-16 | Leave the road (more than 20 m from any lane), then return | No height correction off-road; traffic presence cleared off-road (§9.6.4, §9.6.5) | Recording |
| BL-17 | Contact with a parked car, a bicycle, a tram, a pedestrian, and a building | Pass-through expected for all five; only moving cars collide (§9.6.6) | Recording |
| BL-18 | Hold throttle against a moving AI car for 10 s | Repeated position corrections ("collision resolved against" every ≥ 45 frames) with visible stutter, as reported in upstream issue #1 | Log line count and recording |
| BL-19 | Release while moving at speed, and release while stopped mid-lane | Release is allowed at any speed. The car is snapped to the nearest lane pose and repaths toward its unchanged destination (§9.6.7, inference). | Time until it moves on its own; route continuity; queue behind |
| BL-20 | Pause the game while driving; then drive at simulation speeds 1, 2, and 3 | Paused: the car freezes while the chase camera stays live. Higher speeds: the car moves faster in real time. (Inference from the update phases and the real-time step clamp, §9.6.4.) | Recording; perceived speed versus the speed setting |
| BL-21 | Save a **lab copy** while a car is possessed, then reload it with BTDM and again without BTDM | Unknown: the frozen native state (pending path owner, cleared path, maximum speed 0) is in the save (§9.6.9). Possible stuck car. | Whether the car moves after the reload (proposed OPEN-412) |
| BL-22 | Load another lab save while a car is possessed; separately, exit to the main menu while possessed | No handback on load, unload, or dispose (§9.6.9); stale-entity behavior is unknown | Log lines; camera state; any exception |
| BL-23 | Input focus: alt-tab while holding W; type in a text field (for example a save name) while not driving | Legacy key polling continues regardless of UI focus; the input is treated as stale after 0.35 s (§9.6.8) | Unintended possession or hotkey actions |
| BL-24 | Selection while driving | Any selection is cleared on every UI update while driving (§9.6.10) | Whether buildings or citizens can be inspected while driving |
| BL-25 | Unregistered systems | No log lines from the police-chase, traffic-guard, or Reliable Drive code (OPEN-413) | Log search |
| BL-26 | PlayerPrefs writes | Only the Keybinds panel and "Reset" write PlayerPrefs. Avoid both, or record their use (§9.6.8). | Note whether they were used |

The build plan, the controls documented upstream, the defaults to keep, and the log lines to collect are in §9.13 and
§9.14.

The exit is a recorded run (`RUN-NNNN`, created only when it actually happens), a list of limitations, and an input to
the reuse decision (OPEN-005). Build and runtime steps are described in [BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md).
BTDM and Drive the City II must never be loaded together (REQ-COMPAT-001).

## 8. Open questions and risks

Central entries: OPEN-001, OPEN-003, OPEN-004, OPEN-005, OPEN-011, OPEN-012, OPEN-023, OPEN-035, RISK-001, RISK-002,
RISK-004, RISK-027.

New entries raised by this document (full text in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md)):

- **OPEN-402:** Can the possession technique drive vehicles spawned by our mod?
- **OPEN-403:** How was the published build produced without the official Mod.props/Mod.targets? Is post-processing needed?
- **OPEN-404:** What is the provenance of `decompiled_published_0317`?
- **OPEN-405:** Who is the copyright holder, and under which license was the history before the relicensing commit published?
- **RISK-403:** The baseline build may diverge from the published build.
- **RISK-404:** Reused code could carry provenance taint from decompiled material.
- **RISK-406:** Extraction errors in the preliminary review. The extraction-level statements in §2 are now superseded by the raw reading in §9.

Entries **proposed** by TASK-0002 are listed below. They are not yet in the register; the Coordinator adds them and
may renumber them.

- **OPEN-411:** What is the "CS2 GTA Edition" collision layer that the CHANGELOG says was ported into BTDM, and under which license and rights?
- **OPEN-412:** What happens to a native vehicle that is saved while possessed, when the save is reloaded with and without the mod?
- **OPEN-413:** Does CS2 create or run mod `GameSystemBase` classes that are not registered through `UpdateSystem`?
- **OPEN-414:** Which time base should a mod-driven vehicle's control step use (simulation ticks or real time), across pause, simulation speeds, and low FPS?
- **OPEN-415:** How can borrowable native vehicle classes be identified from prefab or component data rather than prefab names?
- **RISK-411:** Collision code of unclear third-party origin.
- **RISK-412:** A co-loaded BTDM interferes with Drive the City II: it can possess our vehicles, clear selections, and react to shared keys.
- **RISK-413:** Saving while a native vehicle is borrowed persists a frozen vehicle.

## 9. TASK-0002 audit (pinned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, 2026-10-18)

### 9.1 Method, pin, and evidence level

- **Pin.** A read-only remote query returned `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` for both `HEAD` and
  `refs/heads/main`, and no other references (no tags). This is the same commit as the preliminary candidate. **No
  change since the preliminary review.**
  - *Verified command (2026-10-18, auditor):* `git ls-remote https://github.com/northernst11-bot/BetaTestDrivingMod`
  - Commit date: 2026-06-17T10:19:57Z. Message: "Publish 0.3.54 stable smoother driving update".
  - The GitHub API lists 8 commits, no tags, no releases, no pull requests, 1 open issue, and 2 forks. The forks were
    not inspected.
- **Reading method.**
  - Every file was read by streaming its raw text at the SHA-pinned URL
    `https://raw.githubusercontent.com/northernst11-bot/BetaTestDrivingMod/eefd6bb4fe2dcc479df837a1678f3529ac6e0f76/<path>`
    to the terminal, in line ranges, with `grep` for cross-references.
  - Metadata came from the GitHub REST API: the recursive tree (not truncated), commits, per-commit file lists, two
    commit diffs, the issues, and the forks. License and notice texts at earlier revisions were also streamed.
  - **Upstream content was never cloned, downloaded as an archive, or deliberately saved.** One exception, made by
    the tool harness rather than by command, is described below.
  - **Nothing was built, executed, or installed.**
  - **Exception: harness-saved tool output.** During the reading of the legacy files, the tool harness automatically
    saved one oversized grep output, about 46 KB of quoted upstream lines, to the session's temporary tool-results
    folder outside the project root. This is recorded in the TASK-0002 handoff for the Coordinator's decision.
- **Readers.**
  - The auditor read the registered systems, the traffic systems, the project and legal files, the UI module, and
    the documentation.
  - The unregistered `ReliableDrive*` files and `DirectDrivePoliceChaseSystem.cs` were read in full by two assistant
    readers. They are subagents working under the same read-only method. The auditor spot-checked their key claims
    (registration, cross-references, police-chase gating, input lines, entity creation) by `grep` at the pin.
- **Labels and line references.**
  - Every statement in §9 is **[Source-inspected] at the pinned commit** unless it is labeled **[Documented upstream]**
    (README, CHANGELOG, issue text: claims, not verified behavior) or *inference* (our reading of the code's
    behavior, unverified at runtime).
  - Line numbers refer to the pinned files.
  - Nothing in §9 is built locally or tested in-game.

### 9.2 Coverage table

| File | Bytes / lines | Registered in `Mod.cs`? | Read | Reader / notes |
|---|---|---|---|---|
| `Mod.cs` | 1,975 / 41 | Entry point | Fully | Auditor |
| `BetaTestDrivingMod.csproj` | 4,264 / 111 | — | Fully | Auditor; plus the diff that added the decompiled-folder exclusion (`2c5b830…`) |
| `Properties/PublishConfiguration.xml` | 9,928 / 126 | — | Fully | Auditor; plus `GameVersion` and `ModVersion` at 5 revisions |
| `Properties/AssemblyInfo.cs` | 587 / 14 | — | Fully | Auditor |
| `mod.json` | 308 / 9 | — | Fully | Auditor |
| `.gitignore` | 235 / 23 | — | Fully | Auditor |
| `LICENSE` | 1,062 | — | Fully | Auditor; also at `9955221…` (first commit) |
| `NOTICE.md` | 548 | — | Fully | Auditor; also at `9955221…` and `af35523…` |
| `README.md` | 12,252 / 131 | — | Fully | Auditor |
| `CHANGELOG.md` | 31,607 / 460 | — | Fully | Auditor |
| `Systems/DirectDriveRuntime.cs` | 39,249 / 856 | Static class used by all systems | Fully | Auditor |
| `Systems/DirectDriveControlSystem.cs` | 102,017 / 2,186 | **Yes** (GameSimulation, after `CarMoveSystem`) | Fully, in 6 ranges | Auditor; the tail left unread by the preliminary review is covered |
| `Systems/DirectDriveVehicleCollisionSystem.cs` | 40,180 / 885 | Partial class of the control system | Fully | Auditor |
| `Systems/DirectDriveFreezeSystem.cs` | 2,887 / 75 | **Yes** (GameSimulation, between `CarNavigationSystem` and `CarMoveSystem`) | Fully | Auditor |
| `Systems/DirectDriveCameraSystem.cs` | 8,580 / 203 | **Yes** (PreCulling, after `CameraUpdateSystem`) | Fully | Auditor |
| `Systems/DirectDriveInputSystem.cs` | 244 / 12 | **Yes** (UIUpdate) | Fully | Auditor |
| `Systems/DirectDriveUiSystem.cs` | 28,174 / 499 | **Yes** (UIUpdate) | Fully | Auditor. Lines 447–496 were verified by pattern to be 50 one-line binding updates. |
| `Systems/DirectDriveHudBehaviour.cs` | 15,072 / 311 | MonoBehaviour created by `Mod.cs` | Fully | Auditor |
| `BetaTestDrivingMod.mjs` | 28,756 / 724 | Loaded by the game UI (inference) | Fully | Auditor |
| `Systems/DirectDriveTrafficPresenceSystem.cs` | 11,853 / 295 | **No** | Fully | Auditor |
| `Systems/DirectDriveTrafficGuardSystem.cs` | 16,580 / 436 | **No** | Fully | Auditor |
| `Systems/DirectDrivePoliceChaseSystem.cs` | 28,385 / 706 | **No** | Fully | Assistant reader; spot-checked |
| `Systems/ReliableDriveSystem.cs` | 144,230 / 2,809 | **No** | Fully | Assistant reader; spot-checked |
| `Systems/ReliableDriveRuntime.cs` | 24,177 / 533 | **No** | Fully | Assistant reader; spot-checked |
| `Systems/ReliableDriveHudBehaviour.cs` | 20,978 / 343 | **No** | Fully | Assistant reader |
| `Systems/ReliableDriveUiSystem.cs` | 6,659 / 123 | **No** | Fully | Assistant reader |
| `Systems/ReliableDriveLaneSyncSystem.cs` | 3,321 / 81 | **No** | Fully | Assistant reader |
| `Systems/ReliableDriveFollowSystem.cs` | 1,676 / 50 | **No** | Fully | Assistant reader |
| `Systems/ReliableDriveCameraSystem.cs` | 1,368 / 45 | **No** | Fully | Assistant reader |
| `Systems/ReliableDriveInputSystem.cs` | 267 / 13 | **No** | Fully | Assistant reader |
| `images/beta-test-driving-mod.svg` | 1,624 / 33 | Copied to output | Fully | Auditor; a vector drawing with the word "TAXI" and no attribution |
| `Properties/1.png`, `2.png`, `3.jpg`, `4.jpg`, `Thumbnail.png`; `Resources/BetaTestDrivingModIcon.png`; `images/beta-test-driving-mod.png` | 0.33–1.65 MB each | — | **Not read** (binary) | Identity from the tree only: `1.png` and `Thumbnail.png` are the same blob, and so are the two icon PNGs |

Also read: the repository metadata, the metadata of all 8 commits, the changed-file list of every commit, the forks
list (names and dates only), and the full text of issue #1. Not read: the forks' contents, the Paradox Mods listing,
and the forum thread linked in the publish description.

### 9.3 Preliminary claims confirmed or corrected

| Preliminary statement (§2) | TASK-0002 result |
|---|---|
| Registration of five systems; native ordering | **Confirmed.** `Mod.cs` lines 26–31 register the UI and input systems in `UIUpdate`; the freeze system after `CarNavigationSystem` and before `CarMoveSystem`; the control system after `CarMoveSystem` (all in `GameSimulation`); and the camera system after `CameraUpdateSystem` in `PreCulling`. |
| "Road intent assist" queues AI turn intent (README lines 19, 47–48) | **Corrected: dead code.** `ApplyRoadIntent` (control system line 666) has **no call site**, so `TryQueueTurnIntent` and `WriteTurnIntentNavigation` never run. The `RoadIntentAssist` flag only gates traffic presence (lines 509, 530). The README claim does not match the code. |
| Unread tail of the control file | **Read.** It contains helpers, the lane iterators, lane-object upsert and remove, selection cleanup, the car search, and conversion utilities. No additional behavior. |
| Freeze parks pathfinding | **Confirmed**, with exact fields (§9.6.3). |
| Release marks state obsolete and clears the path | **Confirmed and refined.** `CarCurrentLane` is also snapped to the nearest lane pose, and a short navigation target is set (§9.6.7). |
| IMGUI fallback panel | **Corrected.** The IMGUI window is unreachable, because `HudVisible` is never set to true. IMGUI only draws the debug overlays (`DirectDriveHudBehaviour.cs` lines 18–30; `DirectDriveRuntime.cs` lines 106 and 842). |
| Input via legacy `UnityEngine.Input`; `PlayerPrefs` bindings | **Confirmed.** Input is also sampled by the HUD MonoBehaviour every rendered frame. F9 is hard-wired to a request that only re-applies safe defaults (§9.6.8). |
| Collision with vehicles only | **Confirmed and refined.** Parked cars, bicycles, trams, trains, pedestrians, buildings, and props are not collided with. The mesh-bounds helpers are unused (§9.6.6). |
| No save-game persistence | **Confirmed.** `PlayerPrefs` is written only on key rebinding and on "Reset". |
| No committed binaries; no Harmony | **Confirmed** (§3, D-03 and L-07). |
| CHANGELOG stops at `0.3.27-stable` | **Confirmed.** The 0.3.28–0.3.54 changes sit under "Unreleased". |
| Commit `af355236…` relicensed the source | **Confirmed.** Full hash `af355236521636e987801e4c14019aaf187ec277`. The earlier license text is now known (§9.4). |
| Issue #1 reports an unnecessary vehicle-kind save-back | **Corrected.** The method named in the issue (`DirectDriveRuntime.SetVehicleKind`) **does not exist at the pin**, so the finding concerns the fork's code. The collision-stutter report is plausible from the code (§9.6.6) but unverified. |

### 9.4 License and provenance (summary; detail in REUSE_AND_LICENSE_AUDIT)

- **License at the pin:** the standard MIT text with "Copyright (c) 2026 stoot". `NOTICE.md` asks users to keep the
  MIT notice with substantial copies and grants no rights to game trademarks, game assets, or third-party content.
  `AssemblyCopyright` is empty, and no source file has a header.
- **License history** (raw at earlier revisions):
  - Commits `9955221…` (2026-05-30) through `7173e9e…` (2026-05-30) carried a "Beta Test Driving Mod
    Source-Available Transparency License". It reads "not open source" and reserves all rights, forbidding copying,
    forking, compiling, reuse of "algorithms, UI implementation, driving implementation" and similar, and use for
    training or evaluating code-generation tools.
  - Commit `af355236…` (2026-05-31) replaced it with MIT. The CHANGELOG says it "Re-licenses the repository under the
    MIT License".
  - The `ReliableDrive*` files, `DirectDrivePoliceChaseSystem.cs`, and `DirectDriveInputSystem.cs` are **unchanged
    since the first commit**, so they were first published under the restrictive text. The pinned tree as a whole
    sits under the MIT `LICENSE`. Whether that settles the matter is a question for the owner (no conclusion drawn).
- **Copyright holder:**
  - "stoot" appears in `LICENSE` (all revisions), in `mod.json` "author", and in `AssemblyCompany`.
  - All 8 commits are authored and committed as "stoot", and GitHub associates them with the account login `stoot`.
  - The repository belongs to the account `northernst11-bot`. The relationship between the two accounts is not stated
    in the repository (OPEN-405).
- **Decompiled-folder exclusion (OPEN-404):**
  - `Compile Remove="decompiled_published_0317\**\*.cs"` was added in commit `2c5b83085290b9d578ae6eff30f9241d8b0299c5`
    (2026-06-04), together with the new `DirectDriveVehicleCollisionSystem.cs` (+726 lines).
  - No commit ever added that folder.
  - The CHANGELOG entry `0.3.18-crashfix-local` says it "Restores the local test cache from the published
    `0.3.17-collision-tuning-public` package". The folder name is *consistent with* a decompilation of a published
    0.3.17 package. That is an inference; the repository does not say what was decompiled, or by whom.
- **Third-party collision code (proposed OPEN-411):**
  - The CHANGELOG entry `0.3.17-collision-tuning-public` reads "Ports the CS2 GTA Edition vehicle collision layer
    into Beta Test Driving Mod" **[Documented upstream]**.
  - The repository does not identify "CS2 GTA Edition", its author, or its license.
  - `DirectDriveVehicleCollisionSystem.cs` has no attribution header.
  - Until this is resolved, the collision code is classified as **concept only** (§9.10).
- **Other third-party material:**
  - Screenshots show game-rendered content.
  - The legacy `ReliableDriveSystem.cs` contains car brand words only as prefab-name match strings.
  - No embedded third-party code is identified beyond the collision question.

### 9.5 Build configuration, game version, dependencies, loading mechanism

- **Project file** (`BetaTestDrivingMod.csproj`):
  - SDK-style (`Microsoft.NET.Sdk`), `net48`, `LangVersion` 9.0, `Nullable` and `ImplicitUsings` disabled,
    `GenerateAssemblyInfo` false, `AppendTargetFrameworkToOutputPath` false, `OutputPath` `bin\`.
  - `ManagedPath` is `..\..\Cities2_Data\Managed`, a path relative to the project folder.
  - There is no `Import`, `PackageReference`, `Target`, post-build step, or deployment step.
- **Game assembly references** (18, all `Private=false`):
  - `Game`;
  - `Colossal.Core`, `Colossal.Collections`, `Colossal.Mathematics`, `Colossal.Logging`,
    `Colossal.IO.AssetDatabase`, `Colossal.Localization`, `Colossal.UI`, `Colossal.UI.Binding`;
  - `Unity.Collections`, `Unity.Entities`, `Unity.Mathematics`;
  - `UnityEngine.CoreModule`, `UnityEngine.InputLegacyModule`, `UnityEngine.PhysicsModule`,
    `UnityEngine.IMGUIModule`, `UnityEngine.ImageConversionModule`;
  - `System.Memory`.

  No source file uses physics, localization, or image-conversion APIs (by `grep`), so at least three references
  appear unused.
- **Files copied to the output:** `BetaTestDrivingMod.mjs`, `Resources\BetaTestDrivingModIcon.png`, and
  `images\beta-test-driving-mod.png` and `.svg`. `mod.json` and the publish metadata are **not** copied.
- **Declared game version:** `GameVersion` "1.5.*" in `PublishConfiguration.xml`, identical at all 5 sampled
  revisions. The README and `mod.json` state none. **No runtime version check exists** in any code file. The game
  installed on the owner's machine is 1.6.2f1 (TASK-0001, [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md)).
- **Dependencies:**
  - None at runtime: no `Dependency` element, and `mod.json` has `"dependencies": []`.
  - No NuGet packages.
  - **No Harmony, no BepInEx, no reflection patching.**
- **Loading mechanism:**
  - BTDM is a **native CS2 code mod**: `public sealed class Mod : IMod` (`Game.Modding`), with `OnLoad(UpdateSystem)`
    and `OnDispose()`. Systems are registered through `updateSystem.UpdateAt`, `UpdateAfter`, and `UpdateBefore`.
  - It also calls `GameManager.instance.modManager.TryGetExecutableAsset(this, out asset)` to log its own location.
  - It is loaded by the game's own mod manager. Nothing in it requires BepInEx.
  - Distribution is through Paradox Mods (`ModId` 145145, `AccessLevel` Public, tag "Code Mod"). For local
    development, the official flow deploys to the local mods folder (SRC-008); the project has no deployment step.
  - The consumer of `mod.json` is not documented in any source we have read (proposed note to OPEN-403).
- **Post-processing (OPEN-403, source side):**
  - The code uses no `[BurstCompile]`, no scheduled jobs, no `SystemAPI`, no `Entities.ForEach`, and no
    `IJobEntity`. All ECS access goes through `EntityManager`, `GetComponentLookup`, and `GetEntityQuery` on the main
    thread. Lane searches use the game's quad tree with a managed iterator struct.
  - Systems are declared `partial` but use no generated members.
  - *Inference:* this code probably does not depend on ModPostProcessor. TASK-0003 must confirm it.
  - How the published package was built remains unknown.

### 9.6 Runtime architecture

#### 9.6.1 Entry point, update order, and global state

- **`OnLoad`** (`Mod.cs`):
  1. creates a DontDestroyOnLoad GameObject "Beta Test Driving Mod (Stable) HUD" with `DirectDriveHudBehaviour`;
  2. applies the "public safe defaults";
  3. registers the systems (§9.3).
- **`OnDispose`** only resets static state (`DirectDriveRuntime.Reset`, lines 813–854) and destroys the HUD object.
  **It does not hand back a possessed vehicle.**
- **Global state:** all shared state is in the static class `DirectDriveRuntime`: the driving flag, the possessed
  entity, the pose, the tuning values, the key bindings, and the debug data. The control system keeps the
  authoritative `m_PossessedCar` field.
- **Player state:** there is no state machine. Possession is a single boolean, `IsDriving`.

#### 9.6.2 Vehicle selection and takeover

- **Candidates** (`IsDriveableCar`, control system lines 2028–2045):
  - The entity must have `Car`, `CarNavigation`, `CarCurrentLane`, `Game.Objects.Transform`, `Moving`, `PrefabRef`,
    and a `TransformFrame` buffer.
  - It must not be `Deleted`, `Temp`, `TripSource`, `ParkedCar`, `Unspawned`, or `Bicycle`.
- **Target choice** (lines 212–234):
  - First the entity selected in `SelectedInfoUISystem`, resolved through `Controller` or `Owner`.
  - Otherwise the best-scoring live car within 180 m of a point 35 m in front of `Camera.main`.
  - The score adds a name-based penalty (+60) for prefab names containing bus, truck, ambulance, fire, police,
    garbage, and similar, and a bonus for taxi or car (lines 2101–2109).
  - **Service and public-transport vehicles are therefore possessable, especially when selected.**
- **Takeover** (`Possess`, lines 236–274):
  1. removes the car from the selection, the tool selection, and the orbit-camera follow target, where it is
     present;
  2. clears the `CarNavigationLane` and `PathElement` buffers;
  3. "parks" the path owner (§9.6.3);
  4. writes every `TransformFrame` to the current pose;
  5. records the pose in the runtime.
- No registry, ownership, class filter, or confirmation exists.

#### 9.6.3 Freezing native navigation

- **`DirectDriveFreezeSystem`** (whole file) runs every simulation tick between the game's car navigation and car
  movement systems. For the possessed car it:
  - sets `CarNavigation.m_TargetPosition` to 0.05 m ahead of the car, `m_TargetRotation` to default, and
    `m_MaxSpeed` to 0;
  - clears `PathElement`;
  - sets `PathOwner.m_ElementIndex` to 0, clears Failed, Stuck, Scheduled, Append, Updated, Obsolete, Divert, and the
    cached flags, and sets **`PathFlags.Pending`**.
- **Native systems keep running.** The game's navigation and movement systems still process the car every tick;
  exclusivity comes only from ordering and overwriting. *Inference:* the pending flag keeps the game from requesting
  a new path while driving.

#### 9.6.4 Per-tick movement and the driving model

The control system runs after the game's car movement system and calls `ApplyDirectControl` (lines 445–567). It
writes:

- `Transform` (position and rotation);
- `Moving` (velocity, and yaw angular velocity);
- `CarNavigation` (the same frozen target);
- `PathOwner` (pending);
- the two interpolation `TransformFrame` entries (computed with `ObjectInterpolateSystem.CalculateUpdateFrames`), or
  all entries after an FPS stall or drift. The frames also carry light flags: rear lights, braking, reversing, and
  turning (lines 965–1066).

**`CarCurrentLane` is never written while driving.** The CHANGELOG ties earlier writes to native crashes.

The driving model is kinematic: no mass, grip, slip, or suspension.

- **Time step** (lines 569–584): each control tick uses the real time since the previous tick, clamped to 1/60–1/30
  s. *Inference:* when the game runs several simulation ticks per frame (simulation speed 2 or 3, or low FPS), the car
  advances more than real time would suggest. While paused, the simulation phase does not run, so the car stops.
  Both need a runtime check (BL-20, proposed OPEN-414).
- **Speed** (lines 595–651):
  - The target is 42 mph by default.
  - Rates are 19 m/s² to accelerate, 42 m/s² to brake, and 12 m/s² both to coast and to accelerate in reverse.
  - There is a "launch bite" to 1.25 m/s.
  - Braking comes first. Reverse (9 mph) is armed once stopped and becomes active on a new brake press.
- **Steering** (lines 653–664):
  - Yaw rate = input × 148°/s × a blend from 0.58 (low speed) to 1 (at 14 m/s and above).
  - The rate does not fall to zero at standstill. *Inference:* the car can turn in place (BL-15).
  - Movement is always along the car's facing direction.
- **Road contact** (lines 477–488, 1078–1161):
  - The car's height follows the nearest drivable lane: the current lane if within 2 m; otherwise a search of the
    game's lane quad tree within 26 m and ±3.2 m height.
  - A lane counts only within 20 m horizontally.
  - Height is blended by "stickiness" 0.45, at most 0.08 m per tick. Pitch follows the lane grade (slope capped at
    0.42).
  - Off-road there is no height correction. No terrain, water, or building data is read.

#### 9.6.5 Traffic presence (lane objects)

- **Condition:** traffic presence runs only while `RoadIntentAssist` is on (forced on). With "road-only" on (also
  forced on), the car must also be within 3.25 m laterally and 2.8 m vertically of the lane, within 6 m of it
  horizontally, and its heading must be compatible (lines 506–519, 1344–1352).
- **Writes:** the possessed car is written into the `Game.Net.LaneObject` buffer of the nearest lane through
  `Game.Net.NetUtils.UpdateLaneObject`. The entry covers about 1.7 m behind to 2.0 m ahead of the car (lines 45–49,
  1459–1477, 1743–1756). An optional "halo" second lane is off by default.
- **Cleanup:**
  - The car's entries are removed from every lane it touched.
  - On lane change, and every 24 simulation frames, the car's entries are removed from all drivable lanes within
    34 m. Removal is a direct `RemoveAt` loop (lines 1559–1683, 1758–1777).
  - Off-road or on release, all tracked entries are removed (lines 387–443).
- **Scope:** only the possessed car's own entries are touched. Other vehicles' components are never written by the
  registered code.

#### 9.6.6 Collision

`DirectDriveVehicleCollisionSystem.cs` is a partial class of the control system.

- **Targets:** cars with the components of §9.6.2, excluding `Destroyed`, parked cars, bicycles, and
  unspawned or temporary entities. *Not covered:* parked cars, bicycles, trams, trains, pedestrians, buildings,
  props, and terrain.
- **Candidate cache:** refreshed every 12 simulation frames or after 10 m of movement. Poses come from the targets'
  interpolated `TransformFrame` entries (lines 249–394).
- **Hitboxes:** fixed sizes chosen from prefab-name keywords. The default is 2.2 × 1.7 × 4.8 m; bus and truck boxes
  are 2.8 × 2.7 × 8.8 m (lines 421–458).
- **Detection:** three swept probes at the leading bumper (the rear bumper when reversing), tested against target boxes
  in the horizontal plane. Targets more than 0.75 m apart vertically are skipped (lines 666–700).
- **Response:**
  - The car is placed back at the contact point.
  - Its velocity into the obstacle is reduced by 75%.
  - Its speed becomes at least 35% of the pre-impact speed (lines 194–222). *Inference:* with throttle held against a
    car, this repeats tick after tick, which matches the stutter reported in issue #1.
- **No writes to the other vehicle**, no damage, and no events.
- **Dead code:** the mesh-bounds helpers (lines 460–648) are unused.

#### 9.6.7 Release and handback

Release triggers: V, the UI "Release" button, the car becoming ineligible, or any exception during control (a
"safety release"). Steps (`Release` and `PrepareReleasePathfinding`, lines 276–385):

1. Remove every lane-object entry the mod wrote (§9.6.5).
2. If a lane pose is found:
   - write `CarCurrentLane` once: lane, curve position, direction, no change lane, `m_LanePosition` 0;
   - clear the turn flags and set **`CarLaneFlags.Obsolete`**.

   Otherwise, clear only the change lane and set the obsolete flag.
3. Set `CarNavigation` to a target 6–22 m ahead (1.25 s at the current speed), with maximum speed = max(speed,
   0.5 m/s).
4. Clear `CarNavigationLane` and `PathElement`. On `PathOwner`, set the element index to 0, clear the pending, failed,
   stuck, and similar flags, and set **`PathFlags.Obsolete`**. *Inference:* the game's pathfinding then rebuilds a
   path from the current position.
5. Clear the selection and the orbit-camera follow target, and switch the orbit controller back to the gameplay
   controller.

**Not done:**

- no requirement to stop first;
- no check that the release position is legal;
- no verification that the car rejoins traffic;
- no handback on save, load, exit, or mod disposal (§9.6.9).

The car's destination (`Target`) is never modified. *Inference:* the car resumes its original trip.

#### 9.6.8 Input

- **Legacy polling:** `UnityEngine.Input.GetKey` and `GetKeyDown` (`DirectDriveRuntime.cs` lines 267–326). Sampled:
  - once per frame by the HUD MonoBehaviour `Update`, in every game mode, including menus;
  - by `DirectDriveInputSystem` in `UIUpdate`;
  - by the control system.
- **Keys:** primary keys are rebindable. The arrow keys are hard-wired fallbacks, and F9 is hard-wired.
- **Not present:**
  - suppression of game input;
  - a check for window focus or for text-field focus;
  - gamepad support;
  - use of the game's input system (`ProxyBinding` / `ModSetting`).
- **Stale input:** input older than 0.35 s counts as stale and is zeroed.
- **Key-binding storage:** stored as Unity `KeyCode` names in `PlayerPrefs`, with the prefix
  `"BetaTestDrivingMod.DirectDrive.Key."` and 9 actions.
  - Written by every rebind (lines 366–378), and by "Reset" through `ResetKeyBindings` and `SaveDefaultKeyBindings`
    (lines 404–440). Otherwise only read.
  - The UI captures keys with a JavaScript `keydown` listener (`BetaTestDrivingMod.mjs` lines 361–390).

#### 9.6.9 Lifecycle, save, and load

- **Persistence:** none in any file. There is no serialization interface and no save or load hook.
- **Save while possessing:**
  - Nothing prevents a save while a car is possessed.
  - *Inference:* the save then contains the frozen native state (pending path owner, empty path, maximum speed 0,
    and the mod's lane-object entries).
  - Whether that car recovers after a reload, with or without the mod, is unknown (proposed OPEN-412, BL-21).
- **Loading another save, or exiting:** nothing releases the car.
  - The system's `m_PossessedCar` survives in the system instance. On the next tick the control system checks it with
    `IsDriveableCar` and releases it if it no longer qualifies.
  - The behavior with a stale entity handle in a new world is unknown (BL-22).
- **Camera:** restored only by the camera system's own update, when it sees that driving stopped. It is not restored
  on dispose.

#### 9.6.10 Camera, UI, and city-simulation side effects

- **Camera** (`DirectDriveCameraSystem.cs`; PreCulling, after the game's camera update):
  - On activation, stores the previous controller, sets the orbit controller active with no followed entity, and
    then overwrites the camera's position and rotation every frame.
  - The overwrite is a smoothed chase pose: 10.5 m behind and 3.25 m up, with 12 m look-ahead. Each grows with speed.
  - On standby or exception, restores the previous controller, or the gameplay controller.
  - The chase camera is on by default, F7 toggles it, and the UI toggle is ignored (it re-applies the defaults).
- **UI:**
  - `DirectDriveUiSystem` (a `UISystemBase`; game mode Game) exposes 50 value bindings and 39 trigger bindings in the
    group `"betatestdrivingmod"`.
  - The hand-written `.mjs` module (no build step) uses `window["cs2/api"]`, `window["cs2/ui"]`, and `window.React`.
    It appends a top-left button and a panel ("Drive" and "Tuning" tabs, keybind capture) to `GameTopLeft` and
    `Game`.
  - The icon is loaded from `coui://ui-mods/images/beta-test-driving-mod.svg`.
  - The setters for road intent, road height, freeze, chase camera, and police chase ignore the requested value
    (lines 236–303).
- **Selection:** while driving, **any** selection in `SelectedInfoUISystem` and the `ToolSystem` is cleared on every
  UI update (lines 180–216).
- **City simulation:**
  - The registered code does not read or write simulation speed or pause; `SimulationSystem` is used only for
    `frameIndex`.
  - It does not write other vehicles, citizens, buildings, or services. Its only shared writes are the lane-object
    buffers (§9.6.5).
  - **The police chase is not scheduled.** Commit `2c5b830…` removed it from `Mod.cs`, and its enable flag is only
    ever set to false.
  - The traffic guard is not scheduled either.

#### 9.6.11 Unregistered compiled code

All of the files below compile into the assembly. None is referenced by registered code, and none carries an ordering
or auto-creation attribute. Whether the game ever creates such systems by itself cannot be settled from source
(proposed OPEN-413).

- **`DirectDriveTrafficPresenceSystem`:** a duplicate lane-object writer, superseded by the control system's
  single-writer logic.
- **`DirectDriveTrafficGuardSystem`:** **writes `Moving.m_Velocity` of nearby AI cars** to brake them (lines
  262–277).
- **`DirectDrivePoliceChaseSystem`**, gated by `PoliceChaseEnabled`. If run, it would:
  - create service-request entities;
  - add or overwrite `CrimeProducer` on the possessed car;
  - retarget up to 3 police cars (`Target`, an obsolete `PathOwner`, emergency and warning flags);
  - emit `PathTargetMoved` events.
- **The `ReliableDrive*` stack** (the pre-0.3.0 controller): it overwrites `CarNavigation` and clears
  `CarNavigationLane` every frame for cars, watercraft, and trains (trains get speed only). It has optional
  service-vehicle and bicycle possession, a bus-stop assist, and **no path rebuild on release**.
  `ReliableDriveLaneSyncSystem` writes `CarNavigationLane`, `CarCurrentLane` flags, and `Blocker`.
- **No unregistered file spawns or destroys vehicles.** Only the police chase creates or destroys request and event
  entities. Spawned "player car" code from versions 0.1.x is gone **[Documented upstream: CHANGELOG 0.1.16, 0.3.0]**.

### 9.7 Features: README claims versus source

| README / publish claim **[Documented upstream]** | Source at the pin |
|---|---|
| Possess a live vehicle with V; direct movement after the car move step | Implemented (§9.6.2–§9.6.4) |
| Freezes vanilla path movement | Implemented through navigation overwrite plus a pending path owner. Native systems still run (§9.6.3). |
| Road intent assist queues AI turn intent (lines 19, 47–48, 99) | **Not active:** dead code (§9.3) |
| Rebuilds vanilla pathfinding from the current lane on release | Implemented as flag invalidation plus a lane snap (§9.6.7); runtime unverified |
| Green AI presence; stale-lane purge; nearby sweep; halo off by default | Implemented (§9.6.5) |
| Vehicle collision without pushing or rewriting hit cars | Implemented; vehicles only (§9.6.6) |
| Building collision and crash effects not included | Confirmed: none |
| Capped real-time catch-up for FPS drops | Implemented as a 1/60–1/30 s clamp (§9.6.4) |
| Configurable keybinds; F7, F8, Numpad 0, Numpad 9 | Implemented through `PlayerPrefs` (§9.6.8) |
| Police chase "hidden and disabled" | Confirmed: unscheduled and gated off |
| Bus, train, and watercraft controls "not active" | Confirmed: only in the unregistered `ReliableDrive*` stack |

### 9.8 On-foot movement and vehicle entry or exit

**None exists.** No file references citizens, humans, creatures, pedestrians, or walking (by `grep`). There is no
avatar, no on-foot controller, no on-foot camera, no enter or exit transition, no exit-point validation, and no
Recovery. Possession is instantaneous on V, and release leaves the camera in building mode. Everything in
`OnFoot`, `EnteringVehicle`, `ExitingVehicle`, and `Recovery` ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md))
and the `Avatar` category ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)) must be built (FEAS-013).

### 9.9 Fit with the Drive the City II architecture

| Our concept | BTDM at the pin | Gap or conflict |
|---|---|---|
| Player states and overlays (SPEC-CONTROL §4) | One static `IsDriving` boolean | No state machine, overlays, transitions, timeouts, or Recovery |
| Single movement authority; INV-CTRL-03 | Freeze before the game's car movement system, control after it | Native navigation and movement still process the car every tick. Exclusivity comes from ordering and overwriting only. There is no grant table and no write-verify check. |
| Camera owner (INV-CTRL-04) | The chase camera overrides the camera transform; restore runs on standby | Not restored on dispose or unload. Not tied to a state. |
| Input context (IR-01 … IR-05; REQ-INPUT-001) | Legacy polling, always on, `PlayerPrefs` | No host-input suppression, no clearing on focus loss, no official remapping. **Not compatible with our input rules.** |
| `BorrowedNativeVehicle` (sandbox) | The possessed car | Matches the concept, but there is no registry and no class filter: service and public-transport vehicles can be possessed, against our sandbox proposal (SPEC-CONTROL §11; proposed OPEN-415). |
| Handback H1–H6 (SPEC-ENTITY §6) | H2, H3 (tracked entries only), H4 (flags), and H5 (implicit) | No H1 stop or legal-position requirement and no H6 verification. Not run on save, load, or dispose. |
| `CareerVehicle` (mod-spawned) | Absent; "does not spawn a fake player car" | The whole spawn, stow, and persistence lifecycle (OPEN-012). For OPEN-402, the control code needs a native car archetype (§9.6.2) plus the game's navigation and movement systems. A career vehicle likely needs a different authority path. |
| Save safety (SPEC-SAVE; INV-CTRL-08) | No persistence and no save hooks | A save during possession may persist a frozen vehicle (proposed OPEN-412, RISK-413) |
| Fault containment (SPEC-RECOVERY) | `try`/`catch` around control, freeze, and camera, with a safety release | A useful pattern, but logs are rate-limited and there is no circuit breaker |
| Module boundaries (SPEC-MODULES) | Static global runtime; host types used everywhere | Our CityBridge, Vehicles, and Player separation must be designed from scratch |

**Suitability verdict [Proposal]:**

- BTDM is suitable as a **runtime behavioral baseline** (TASK-0003).
- It is a **source of concepts** for sandbox takeover, lane presence, handback, the chase camera, UI bindings, and
  crash guards.
- It is **not** suitable as a code foundation to fork for our roadmap: static global state, legacy input, no state
  machine, dead and unregistered code, open provenance questions (OPEN-404, proposed OPEN-411), the declared
  `GameVersion` 1.5.* versus the installed 1.6.2f1, and no career vehicles.
- This supports Option B (selective re-implementation, with attribution where code is adapted) or Option C
  ([UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md)). The owner decides after TASK-0003.

### 9.10 Reuse classification (final, assessment only)

The license permits code reuse only if the owner accepts the MIT obligations and resolves the provenance questions.
Nothing is incorporated.

| Class | Components |
|---|---|
| **Reusable code** (small, separable, license permitting; attribute if adapted) | Kinematic speed, brake, and reverse functions (`UpdateReverseState`, `BuildTargetSpeed`, `MoveSpeed`); lane-pose helpers (`TryGetCurvePose`, `GetLaneForwardClosestTo`, `AlignRotationToRoadGrade`); the lane quad-tree iterator pattern; lane-object upsert and remove helpers; camera smoothing math; the finite-value and clamp guards. All are version-coupled where they touch game types (§9.11). |
| **Reusable concept only** | Freeze before and control after the game's car movement system; handback by obsolete flags plus a lane snap plus a short navigation target; lane presence with a touched-lane list and a periodic sweep; priming and resyncing `TransformFrame`s for rendering; render-pose prediction for the camera; a chase camera after the game's camera update with controller restore; C# value and trigger bindings with a module-registry append; debug overlays for hitboxes and lane presence; `try`/`catch` safety release; the real-time step clamp; **the vehicle-collision sweep (concept only until proposed OPEN-411 is resolved)** |
| **Not reusable** | Legacy `UnityEngine.Input` and `PlayerPrefs` bindings; the IMGUI HUD; the static global runtime; the police chase; the traffic guard (writes other agents); the duplicate traffic-presence system; the `ReliableDrive*` stack; the dead turn-intent and mesh-bounds code; global selection clearing; prefab-name-based vehicle classes and hitboxes; all BTDM identifiers; screenshots and icons |

### 9.11 Version-coupled internals (re-check after every game update)

- **Systems and APIs:**
  - `CarNavigationSystem`, `CarMoveSystem`;
  - `CameraUpdateSystem` (`activeCamera`, `activeCameraController`, `orbitCameraController.followedEntity`,
    `gamePlayController`, `TryMatchPosition`);
  - `SelectedInfoUISystem` (`selectedEntity`, `SetSelection`); `ToolSystem` (`selected`, `selectedIndex`);
  - `PrefabSystem.GetPrefabName`; `SimulationSystem.frameIndex`; `Game.Net.SearchSystem.GetLaneSearchTree`;
  - `ObjectInterpolateSystem.CalculateUpdateFrames`;
  - `Game.Net.NetUtils.UpdateLaneObject` and `RemoveLaneObject`;
  - `GameManager.instance.modManager`; `UpdateSystem`; `SystemUpdatePhase` (`UIUpdate`, `GameSimulation`,
    `PreCulling`).
- **Components and fields:**
  - `Transform`; `Moving` (`m_Velocity`, `m_AngularVelocity`);
  - `CarNavigation` (`m_TargetPosition`, `m_TargetRotation`, `m_MaxSpeed`);
  - `CarCurrentLane` (`m_Lane`, `m_ChangeLane`, `m_ChangeProgress`, `m_CurvePosition`, `m_LanePosition`,
    `m_LaneFlags`) with `CarLaneFlags`;
  - the `CarNavigationLane` buffer; `PathOwner` (`m_ElementIndex`, `m_State`) with `PathFlags`; the `PathElement`
    buffer;
  - `TransformFrame` (`m_Position`, `m_Rotation`, `m_Velocity`, `m_Flags`) with `TransformFlags`; the `UpdateFrame`
    shared component;
  - the `Game.Net.LaneObject` buffer; `Game.Net.Curve`; `CarLane` (`m_Flags`, `m_CarriagewayGroup`);
    `ConnectionLane`; `LaneConnection`; `SubLane`; `Owner`; `Controller`; `PrefabRef`;
  - the tags `Car`, `Bicycle`, `TripSource`, `ParkedCar`, `Unspawned`, `Deleted`, `Temp`, `Destroyed`.
- **UI:**
  - `UISystemBase`, `ValueBinding`, and `TriggerBinding`;
  - the JavaScript globals `cs2/api` and `cs2/ui` (`Button`, `Tooltip`);
  - the registry slots `GameTopLeft` and `Game`;
  - the `coui://ui-mods/` path.
- **Declared version:** `GameVersion` "1.5.*", while the installed game is 1.6.2f1.

### 9.12 Conflicts if BTDM and Drive the City II were both loaded (REQ-COMPAT-001)

- **Double movement authority:** both mods would write `Transform`, `Moving`, `CarNavigation`, `PathOwner`, and
  `PathElement` on the same car.
- **BTDM possessing our vehicles:** BTDM accepts **any** entity with the native car components (§9.6.2). It could
  possess one of our mod-spawned `CareerVehicle`s, or a vehicle we borrowed (proposed RISK-412).
- **Shared lane buffers:** both mods would write `LaneObject` buffers. BTDM's sweep removes only its own car's
  entries, so it collides with us only if both target the same car.
- **Camera:** both mods would set the active camera controller and the camera transform in `PreCulling`.
- **Selection:** while driving, BTDM clears every selection on every UI update. That would break any feature of ours
  that relies on selection.
- **Keys:** BTDM polls V, W, A, S, D, the arrow keys, F7, F8, F9, Numpad 0, and Numpad 9 in every context. Our
  bindings on the same keys would trigger both mods.
- **Identifiers our mod must not reuse:**
  - assembly, namespace, and log name `BetaTestDrivingMod` / `BetaTestDrivingMod.Mod`;
  - UI binding group `"betatestdrivingmod"`;
  - the `PlayerPrefs` prefix `"BetaTestDrivingMod.DirectDrive.Key."`;
  - the GameObject "Beta Test Driving Mod (Stable) HUD";
  - IMGUI window IDs 7114021 and 9425501;
  - `ModId` 145145; the `mod.json` id `"BetaTestDrivingMod"`.

  These identifiers are also detection candidates for OPEN-023, for example the presence of the type
  `BetaTestDrivingMod.Mod`. The community fork with `ModId` 162039 (issue #1, **[Documented upstream]**) was not
  inspected; its identifiers are unknown.

### 9.13 TASK-0003 build plan (inputs and expected deviations)

All steps are **Proposed procedure (requires validation)**. They refine [BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md)
§4.

**Inputs**

| Input | Value or source | State on this machine (TOOLCHAIN_MATRIX, TASK-0001) |
|---|---|---|
| Source | Commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, obtained into `<AUTHORIZED_SOURCE_DIR>` per TASK-0003 step 3 | Not obtained; owner authorization needed |
| .NET SDK able to build SDK-style `net48` projects | Any SDK that the owner approves (TOOLCHAIN_MATRIX records the documented minimum) | **Not found.** MSBuild from Visual Studio is present but needs the SDK resolver. |
| .NET Framework 4.8 reference assemblies | The targeting pack or developer pack, or the reference-assembly NuGet package that SDK builds can add implicitly ([Unverified]) | **Not found.** An implicit package restore would be a network download that needs owner approval. |
| Game reference assemblies (read-only) | `<CS2_MANAGED_DIR>` of the installed 1.6.2f1 game: the 18 DLLs in §9.5 | Present presumably; check each name read-only before building |
| Not needed for this project | Unity Editor, ModPostProcessor, `CSII_*` variables, Node.js or a UI template (the `.mjs` is hand-written) | — |

**Command:**

```powershell
# Proposed procedure (requires validation) — not run
dotnet build "<AUTHORIZED_SOURCE_DIR>\BetaTestDrivingMod.csproj" -c Release -p:ManagedPath="<CS2_MANAGED_DIR>"
```

**Expected output:** `bin\BetaTestDrivingMod.dll` and `.pdb`; `bin\BetaTestDrivingMod.mjs`;
`bin\Resources\BetaTestDrivingModIcon.png`; `bin\images\beta-test-driving-mod.png` and `.svg`. No game DLLs should be
copied. `mod.json` is not part of the output.

**Expected deviations (record each one in the RUN record):**

| # | Deviation | Why / consequence |
|---|---|---|
| BD-01 | `ManagedPath` overridden on the command line; the project file is not edited | The relative default would require placing the source under the game folder (RISK-601). That the command-line value takes precedence is standard MSBuild behavior, **[Unverified]** here: confirm the reference paths in the build log. |
| BD-02 | No Mod.props/Mod.targets, no ModPostProcessor, no Burst | The source needs none of the post-processed features (§9.5, inference). If the mod fails to load with errors about missing types, `NotImplemented`, or system creation, record it against OPEN-403 and RISK-403. |
| BD-03 | Game version mismatch: built against 1.6.2f1 while the code declares 1.5.* | Compile errors are possible wherever a member in §9.11 changed. **Do not patch the source**; a patched build is no longer a baseline. Record the errors and stop for an owner decision. |
| BD-04 | Reference-assembly sourcing | Possible implicit NuGet restore; possible conflict warnings (for example MSB3277) between framework reference assemblies and the game-supplied `System.Memory`. Record all warnings. |
| BD-05 | Deployment layout | Copy the whole `bin\` content into `<ISOLATED_MOD_SET>\BetaTestDrivingMod\`. Whether the game loads the `.mjs` from there and serves images under `coui://ui-mods/` is an observation (BL-02), not an assumption. |
| BD-06 | Comparison with the published build | Not possible without a separately authorized Paradox Mods subscription to 0.3.54-stable. Never decompile. Make sure no subscribed copy is active together with the local build. |
| BD-07 | `PlayerPrefs` | Loading only reads bindings. Avoid "Reset" and rebinding unless recorded (BL-26). |

### 9.14 TASK-0003 observation inputs: controls, defaults, logs

- **Controls documented upstream** (README lines 41–55, **[Documented upstream]**; matched by source):
  - V possesses or releases;
  - W / Up accelerates;
  - S / Down brakes, then reverses on a second press at standstill;
  - A / Left and D / Right steer;
  - F8 shows the panel; F7 toggles the chase camera;
  - Numpad 0 shows the collision overlay; Numpad 9 shows the AI-presence overlay;
  - the top-left button opens the panel ("Possess car", "Release", "Reset", and the Tuning and Keybinds sections).

  Source-only behavior: the arrow keys always work as fallbacks, and F9 only re-applies the safe defaults.
- **Defaults to keep** (do not change during the baseline; `DirectDriveRuntime.cs` lines 78–99, 631–644):
  - speed 42 mph; reverse 9 mph;
  - acceleration 19, brake 42, coast 12, reverse acceleration 12 (all m/s²);
  - turn rate 148°/s; low-speed boost 0.58; road stickiness 0.45;
  - road intent, road height, and freeze **on**; vehicle collision **on**; road-only presence **on**; halo **off**;
  - overlays **off**; retained speed 0.35;
  - chase camera **on**, at 10.5 m, 3.25 m, and 12 m look-ahead;
  - police chase **off** (forced).
- **Handback and traffic observations:** BL-04, BL-05, BL-13 … BL-24 in §7.
- **Log lines to collect** (logger `BetaTestDrivingMod.Mod`; log folder `<CS2_LOG_DIR>` from TASK-0001; exact
  prefixes from the source):
  - "BetaTestDrivingMod 0.3.54-stable loading"; "Current mod asset at"; "Beta Test Driving Mod systems registered:";
  - "Direct Drive possessed"; "Direct Drive released";
  - "Direct Drive release handoff rebuilt vanilla path" or "…requested vanilla repath";
  - "Direct Drive stable traffic presence restored"; "Direct Drive vehicle collision resolved against";
  - "Direct Drive chase camera attached" and "…first custom pose applied";
  - every line containing "safety", "skipped after", "failed", or "rejected";
  - any game or UI log errors, exceptions, or invalid-style warnings during the session.

### 9.15 Debugging tools (OPEN-031, documentation side)

No new tool documentation was read: [DEBUGGING](../engineering/DEBUGGING.md) §7 already records the documented
requirements of `unity-devtools` and `coherent-gameface`. BTDM itself relies only on the game logger (errors hidden
from the in-game UI by `SetShowsErrorsInUI(false)`), on status text in its panel, and on IMGUI world-space overlays.
This supports the "logs first" principle. It is not a debugger.

### 9.16 What remains unverified (needs TASK-0003 or later runtime evidence)

- Whether the source builds against 1.6.2f1, and with which deviations (BD-01 … BD-04).
- Whether the plain build loads, and whether the UI module and icon resolve (OPEN-403, RISK-403).
- Every runtime behavior in §9.6:
  - takeover;
  - freeze effectiveness (whether native movement still nudges the car);
  - the driving feel;
  - lane-presence effects on traffic;
  - collision and stutter;
  - handback route continuity and residual blockers;
  - camera restore.
- Behavior on pause, at simulation speeds 2 and 3, and at low FPS (proposed OPEN-414).
- Save, reload, load, and exit while possessing (proposed OPEN-412, OPEN-101 input).
- Whether unregistered systems ever run (proposed OPEN-413).
- Input conflicts with the game's own bindings (OPEN-014; FEAS-015).
- Provenance: OPEN-404, OPEN-405, and proposed OPEN-411. These cannot be resolved by runtime tests; they need the
  owner's decision or contact with upstream, with the owner's authorization.
