# CS2 Integration Findings

| Field | Value |
|---|---|
| ID | DOC-CS2-FINDINGS |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-00 (research), PHASE-01, PHASE-02, PHASE-03, PHASE-11, PHASE-13 (verification spikes) |
| Responsible role | Coordinator (maintainer); the Implementer adds verified findings with evidence runs |
| Related requirements | REQ-SAVE-001 … REQ-SAVE-006, REQ-TIME-001, REQ-TIME-002, REQ-CTRL-003, REQ-INPUT-001, REQ-INPUT-002, REQ-UI-001, REQ-L10N-001, REQ-DIAG-001, REQ-REL-001 |
| Related documents | [SOURCES](SOURCES.md), [FEASIBILITY_MATRIX](FEASIBILITY_MATRIX.md), [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md), [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md), [BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md) |
| Last updated | 2026-10-18 (TASK-0001 observations; §3.15 added) |

> **None of the findings below has been verified in our environment.** They are what official or community sources
> state, each pinned to the game version that source was verified for. The official toolchain page is verified only
> for game **1.1.12f1**, which is older than every other version cited here. The owner's installed game is
> **1.6.2f1** on Steam, running Unity 2022.3.71f1 (TASK-0001, 2026-10-18). Sources verified for older versions may not
> apply to it.

## 1. How to read this document

Each finding lists its **source** (see [SOURCES](SOURCES.md)), the **kind** of source (official Paradox page or
community), the **game version** the source says it was verified for, the **evidence level**, and the **open question**
it informs. "Extraction" means that the content was obtained through a summarizing fetch tool, so identifiers need a
raw re-check before code relies on them (RISK-406).

## 2. Summary

| # | Topic | Source | Kind | Verified for game | Evidence level | Informs |
|---|---|---|---|---|---|---|
| 3.1 | Official modding toolchain | SRC-008, SRC-002 | Official | 1.1.12f1 (SRC-008); none (SRC-002, 2024-03) | Documented upstream (extraction) | OPEN-002, OPEN-406 |
| 3.2 | Mod runtime target framework and C# version | SRC-005, SRC-013, SRC-001 | Community + base-mod source | 1.6.2f1 (SRC-005); 1.6.0f1 (SRC-013 SDK page) | Community (extraction) + source-inspected (raw) | OPEN-003 |
| 3.3 | Mod structure, entry point, system registration | SRC-002, SRC-010, SRC-011, SRC-001 | Official + community + source | 1.3.6f1 (SRC-010); partly 1.0 (SRC-011) | Documented upstream + source-inspected (extraction) | OPEN-003 |
| 3.4 | UI modding stack | SRC-009 | Official | 1.5.7f1 | Documented upstream (extraction) | OPEN-015 |
| 3.5 | Settings and key binding | SRC-010, SRC-002 | Official | 1.3.6f1 | Documented upstream (extraction) | OPEN-014, OPEN-025 |
| 3.6 | Localization of mod strings | — | — | — | Not documented in what we read | OPEN-026 |
| 3.7 | Save serialization | SRC-005, SRC-013 | **Community only** | 1.6.2f1; ~1.5.10f1 | Community (extraction) | OPEN-006 |
| 3.8 | Behavior after mod removal | SRC-005 | Community | 1.6.2f1 | Community claim (extraction) | OPEN-022 |
| 3.9 | Simulation speed and pause | SRC-013 | Community | ~1.5.x (the recipe cites a mod commit) | Community (extraction) | OPEN-013 |
| 3.10 | Local mods, loading, disabling | SRC-008 | Official | 1.1.12f1 | Documented upstream (extraction) | OPEN-035 |
| 3.11 | Publishing | SRC-008, SRC-012, SRC-001 | Official + base-mod example | 1.1.12f1; none (SRC-012) | Documented upstream + source-inspected (raw) | OPEN-017 |
| 3.12 | Harmony, Burst, and Unity API limits | SRC-002 | Official | None stated (2024-03) | Documented upstream (extraction) | Engineering policy |
| 3.13 | Debugging tools | SRC-005 | Community | — | Community (extraction) | OPEN-031 |
| 3.14 | Native systems and components used by the base mod | SRC-001 | Base-mod source | `GameVersion` 1.5.* | Source-inspected (extraction) | OPEN-007, OPEN-011 |
| 3.15 | Loading routes compared: native code mod versus BepInEx 5 | SRC-002, SRC-008, SRC-001, Melty service (2026-10-18) | Official + base-mod source + Melty | 1.1.12f1 (SRC-008); 1.6.2f1 observed locally | Documented upstream + local observation (no runtime) | ADR-0005, CONF-014 |

## 3. Findings

### 3.1 Official modding toolchain (OPEN-002, OPEN-406)

From SRC-008, an official wiki page verified for 1.1.12f1 and last edited 2026-04-25:

- **Installation:** the toolchain is installed from in-game **Options → Modding**, with install, uninstall, upgrade, and repair per component. Missing or outdated items are flagged in the main-menu notifications.
- **Components:**
  - Unity Editor 2022.3.7f1, "required to be installed", plus a Unity personal or commercial license ("a legal requirement we can not automate");
  - .NET SDK minimum 6, "8 is recommended";
  - Node.js minimum 18 (installs 20.11), for UI mods only;
  - Visual Studio 2022 (17.8) or Rider (2021.3.3); VS Code (1.86) for UI.
- **Templates:** the C# template is a NuGet package used through `dotnet new`. The page does not give its name; the community (SRC-005) says `csiimod`. The UI template is `npx create-csii-ui-mod`.
- **Build:** Roslyn compilation followed by an AfterBuild step that runs **ModPostProcessor.exe** (IL post-processors, source-generation call patching, Burst). Mod.props/Mod.targets are added automatically and "should never need" manual edits.
- **Environment variables:** `CSII_INSTALLATIONPATH`, `CSII_USERDATAPATH`, `CSII_TOOLPATH`, `CSII_LOCALMODSPATH`, `CSII_UNITYMODPROJECTPATH`, `CSII_MANAGEDPATH`, `CSII_ASSEMBLYSEARCHPATH` (optional), `CSII_UNITYVERSION`, `CSII_ENTITIESVERSION`, `CSII_MODPOSTPROCESSORPATH`, `CSII_MODPUBLISHERPATH`, `CSII_PATHSET`.

From SRC-002, the official dev diary of 2024-03-21: a one-click installer sets up Unity, Burst, and ECS, and prompts
updates when dependencies change.

**Caveat:** the page itself says the Unity version must track game updates. The Unity version required for the
installed game is therefore **not confirmed** by any current official page. Confirmed values go only into
[TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).

### 3.2 Runtime target framework and language version (OPEN-003)

- No official page we read states the target framework (SRC-002 and SRC-008 do not).
- Community (SRC-005): "`Mod.props` fixes `net48` to match the game's runtime" and pins C# 9.
- Community (SRC-013): the target framework is not mandated; one cited mod uses `net472`.
- Base mod (SRC-001, raw V-04): `net48`, `LangVersion` 9.0.
- **Best-supported reading: `net48`**, set by the toolchain's Mod.props. This is not officially confirmed. It can be checked read-only by opening Mod.props in the toolchain folder once TASK-0001 has located it, and confirmed by an isolated build (TASK-0003).

### 3.3 Mod structure, entry point, system registration (OPEN-003)

- Official (SRC-002): mods add features by creating their own systems and registering them "in the updating loop", and they can access game entities and components. There is "no such thing as a mod loading order".
- Official (SRC-010, verified for 1.3.6f1): key bindings are registered in the mod's `OnLoad`.
- Community (SRC-011, "Systems" page; potentially outdated, parts verified for 1.0):
  - update phases include `GameSimulation`, `PreSimulation`, `PostSimulation`, `LoadSimulation`, `Serialize`, `Modification1`–`4`, `ToolUpdate`, `UIUpdate`, and `UITooltip`;
  - systems are ordered with `UpdateAt`, `UpdateBefore`, and `UpdateAfter`;
  - `GetUpdateInterval` "must be a power of 2".
- Base mod (SRC-001, extraction): an `IMod` class with `OnLoad(UpdateSystem)` and `OnDispose()`. It registers its systems in `UIUpdate`, `GameSimulation` (ordered around native car navigation and movement), and `PreCulling` (after native camera update).

### 3.4 UI modding stack (OPEN-015)

From SRC-009 (official, verified for 1.5.7f1):

- **Stack:** Coherent Gameface with React, SCSS, TypeScript, and Webpack. Only flex layout is available; "grid layouts are not supported".
- **Registration:** a default `ModRegistrar` export registers UI through a module registry, with Append hooks such as `Menu`, `Game`, `Editor`, `GameTopLeft`, `GameTopRight`, `GameBottomRight`, and (from 1.5.7) `UniversalModMenu`.
- **Bindings:** data flows through `cs2/api` (`bindValue`, `call`, `trigger`, `useValue`). The `cs2/*` packages are injected at runtime, and `cs2/input` is "not yet available".
- **Not covered:** the page does not name the C#-side binding classes. The base mod uses a `UISystemBase` subclass with value and trigger bindings (extraction).

### 3.5 Settings and key binding (OPEN-014, OPEN-025)

- **Settings (SRC-002, official):** an automatic system builds the settings UI from properties marked with attributes.
- **Key binding (SRC-010, official, verified for 1.3.6f1):**
  - a `ModSetting` subclass declares `ProxyBinding` properties with keyboard, mouse, or gamepad binding attributes;
  - `OnLoad` calls `RegisterKeyBindings()`, then obtains actions with `GetAction`, enables them, and reacts to interactions or polls them;
  - the game input system "is based on Unity Input System";
  - a conflict with built-in bindings disables "all conflicted mod bindings";
  - built-in actions can be mirrored, not used directly.
- **Not stated:** where user rebinds are stored, how analog axes are mapped for vehicle control, and how to suppress building-mode input while character mode is active. The base mod uses legacy `UnityEngine.Input` instead ([BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §2.10).

### 3.6 Localization of mod strings (OPEN-026)

No source we read documents the mechanism. The base mod references a `Colossal.Localization` assembly (raw V-04), but
how it uses it was not inspected. The official hub links pages that were listed but not inspected (SRC-011).
Resolution: read-only documentation review, then a PHASE-01 spike with one externalized string in English and PT-BR.

### 3.7 Save serialization (OPEN-006) — community only

No official Paradox page on save serialization was found (SRC-011). Community sources describe it as follows
(SRC-005, verified for 1.6.2f1 against the decompiled game; SRC-013, pinned to ~1.5.10f1, static source only):

- **Components:**
  - components implementing `ISerializable` (`Serialize<TWriter>` / `Deserialize<TReader>` with `IWriter`/`IReader` from `Colossal.Serialization.Entities`) are picked up automatically: "Nothing is registered and there is no opt-in list";
  - related variants are `IEmptySerializable`, `ISerializeAsEnabled`, and `IStrideSerializable`.
- **Systems:** systems can implement `IDefaultSerializable` (with `SetDefaults`) or `IJobSerializable`.
- **Phases:** saving runs in the `Serialize` phase and loading in `Deserialize`.
- **Versioning:** the game's `reader.context.version` is the **game** build version, so mods must write their own version int, as the leading field. Mods cannot add format tags.
- **Restore:** heavy restore work should be deferred out of `Deserialize`.

**Implication for [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md):** in-save storage is a
plausible candidate, not a verified one. A PHASE-01 spike must compare it with a revision-bound sidecar against
[SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md). Save identity (city identity, save revision) is not addressed
by any source we read.

### 3.8 Behavior after mod removal (OPEN-022)

Community (SRC-005): "uninstalling a mod loses that mod's components and keeps the city". This is unverified. It says
nothing about sidecar files or about systems that implement `IDefaultSerializable`. It must be tested in a lab city in
PHASE-01 and again in PHASE-13 before any clean-exit claim is made (REQ-SAVE-006, REQ-REL-002).

### 3.9 Simulation speed and pause (OPEN-013)

Community (SRC-013):

- Read and write `SimulationSystem.selectedSpeed` (the speed the user picked). `smoothSpeed` is the eased running value.
- Relative stepping is clamped to `[0, 8]`.
- Avoid the `time.simulationSpeed$` UI binding for a live readout: "while the game is paused that value reflects the pre-pause speed".
- The exact value while paused is marked "Needs Verification (in-game)".
- The cited source is another mod at a stated commit.

No official page was found. A PHASE-03 spike must confirm how to read, set, and restore speed and how to detect pause
(REQ-TIME-001, REQ-TIME-002).

### 3.10 Local mods, loading, and disabling (OPEN-035)

Official (SRC-008, 1.1.12f1):

- Build output goes to a local mods folder described as `…\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\<ModName>`, and the game "will load local mods automatically".
- A folder whose name starts with "." is disabled.
- Mods are delay-loaded until login is viable, so early-initialization injection is not supported.

How this interacts with playsets (SRC-012: local mods can join playsets but are not cloud-synced) and with an isolated
test set is unknown.

**Observed on this machine (TASK-0001, 2026-10-18, game 1.6.2f1):**

- The user-data root has **no** `Mods` folder.
- `.cache\Mods\local` and `.cache\Mods\pdx_mods` exist and are empty, next to the files `mod_directory.json`,
  `playset_config.json`, and `playset_sync.json` (not read).
- No mods are installed.

Whether the toolchain deploys to the documented `Mods` folder or to `.cache\Mods\local` on 1.6.2f1 is OPEN-036. The
answer comes from `CSII_LOCALMODSPATH` once the toolchain is installed (TASK-0003).

### 3.11 Publishing (OPEN-017)

- **Official flow (SRC-008, 1.1.12f1):**
  1. Fill in `Properties/PublishConfiguration.xml`.
  2. Log in to a Paradox account in the game.
  3. Publish from the IDE through ModPublisher with the `PublishNewMod` profile. It returns a ModID ("typically 6 digit number") to paste into the XML.
  4. Use `PublishNewVersion` (new version and change log) for updates, or `UpdatePublishedConfiguration` for metadata.
- **Platform (SRC-012):**
  - Paradox Mods hosts CS2 mods for PC and consoles, and metadata is editable on the website;
  - dependency trees are resolved;
  - creators can list supported game versions.
- **Example:** the base mod's configuration declares a ModId, a version, a "Code Mod" tag, and `GameVersion` 1.5.* (raw V-06).
- **Not confirmed:** review or moderation rules, and account requirements (the site was a JavaScript shell). Publishing anything requires the project owner's authorization.

### 3.12 Harmony, Burst, and Unity API limits

Official (SRC-002):

- "Harmony can patch only managed code", so Burst-compiled code cannot be patched.
- Unity API calls made without the provided setup "won't work and will throw a 'NotImplemented' exception".

Our policy ([DEPENDENCIES](../engineering/DEPENDENCIES.md)) does not plan Harmony unless an ADR justifies it.

### 3.13 Debugging tools (OPEN-031)

Community (SRC-005), with nothing installed:

- **`unity-devtools`** attaches through the Mono Soft Debugger and requires "a running Unity Mono development build"; "a retail build exposes no SDB port and cannot be driven". It needs the .NET 10 SDK. Its writes are live, with no undo.
- **`coherent-gameface`** requires a reachable Chrome DevTools Protocol endpoint (default `http://localhost:9444`). How to enable it in CS2 is not documented in what we read.

Neither tool is assumed to work against the owner's retail game ([DEBUGGING](../engineering/DEBUGGING.md)).

### 3.14 Native systems and components used by the base mod (extraction; version-coupled)

The base mod (SRC-001) orders itself against, or writes to, native items named in the extraction as:

- systems: `CarNavigationSystem`, `CarMoveSystem`, `CameraUpdateSystem`, `SelectedInfoUISystem`, `Game.Net.SearchSystem`;
- components and buffers: `Game.Objects.Transform`, `Moving`, `TransformFrame`, `CarNavigation`, `CarNavigationLane`, `CarCurrentLane`, `PathOwner`, `PathElement`, `Game.Net.LaneObject` (through `NetUtils.UpdateLaneObject`).

These names are **extraction-level** and are coupled to the base mod's declared `GameVersion` 1.5.*. They must be
raw-verified (TASK-0002) and then checked against the installed game before any design depends on them (RISK-007,
RISK-406). Inside our architecture, such access belongs behind the CityBridge, Vehicles, and WorldContact modules
([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md)).

### 3.15 Loading routes compared (ADR-0005, CONF-014)

| Route | How a mod is loaded | Evidence | Fit with this project |
|---|---|---|---|
| **Native CS2 code mod** (official toolchain) | An assembly with the game's mod entry point is loaded by the game's own mod system, from the local-mods location or from Paradox Mods subscriptions. The mod registers its systems in the game's update loop. | **[Documented upstream]** SRC-002, SRC-008. The candidate base mod uses this entry point, with no BepInEx, Harmony, or patching (**[Source-inspected]**, TASK-0002 at the pinned commit; [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §9.5). Melty's own kit note names this route (MELTY_EVALUATION M-11). | Matches the candidate base mod and the architecture. No change to the game installation. Distributable on Paradox Mods (OPEN-017). |
| **BepInEx 5 plugin** (Melty's only CS2 loader, v5.4.23.5) | A proxy loader placed in the game folder starts BepInEx, which loads plugins from `{game}/BepInEx/plugins`. | **[Documented upstream]** Melty `game_info` (MELTY_EVALUATION M-03). No live CS2 mashups exist (M-06). | Requires changing the game installation folder. Uses a different plugin entry point from the native route. Unverified on 1.6.2f1. A BepInEx copy and a native copy of the same mod could both load (REQ-COMPAT-001). |

**Local facts (TASK-0001, 2026-10-18):**

- game 1.6.2f1 on Steam, Unity 2022.3.71f1 with a Mono runtime;
- no BepInEx, Doorstop, or MelonLoader footprint in the game folder;
- no mods installed;
- the official toolchain is not installed.

**Conclusion:** the native route is proposed for this project
([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md), **[Proposal]**). Still unverified:

- that a toolchain-built native mod loads on 1.6.2f1 (TASK-0003);
- whether BepInEx 5 runs on 1.6.2f1 at all (not tested and not planned).

## 4. Not documented in any source read (2026-10-18)

| Capability | Open question | Resolving phase |
|---|---|---|
| Ground, bridge, tunnel, and elevated-road surface queries | OPEN-007 | PHASE-03 |
| Building and static-obstacle collision data | OPEN-008 | PHASE-03 |
| Spawning and removing mod-owned vehicles | OPEN-012, OPEN-402 | PHASE-02, PHASE-05 |
| Building access points and curbside positions | OPEN-018 | PHASE-06 |
| Transit lines, stop order, and stop access | OPEN-020 | PHASE-08 |
| City-edit events | OPEN-021 | PHASE-10 |
| Save identity (city and revision) | OPEN-006 | PHASE-01 |
| Window focus events and input clearing | OPEN-014 | PHASE-03 |
| Localization mechanism | OPEN-026 | PHASE-01 |

## 5. Turning findings into verified facts

1. **Read-only, local (TASK-0001):** record the game version and whether the toolchain and `CSII_*` variables exist. If the toolchain folder exists, read its Mod.props to confirm the target framework and C# version. No installation is part of this step.
2. **Raw source re-check (TASK-0002):** turn every extraction-level identifier used by a design into a raw-verified one.
3. **Isolated build and run (TASK-0003, owner-authorized):** confirm the toolchain path and the load behavior in a lab city.
4. **Phase spikes:** persistence (PHASE-01), localization (PHASE-01), UI (PHASE-01), input and gamepad probes (PHASE-02), simulation speed, pause, and focus (PHASE-03), publishing rules (before PHASE-13).

Each verified finding is recorded here with its evidence run and the game version it was observed on. The
[FEASIBILITY_MATRIX](FEASIBILITY_MATRIX.md) row is updated in the same change.

## 6. Open questions and risks

- **Central:** OPEN-002, OPEN-003, OPEN-006, OPEN-013, OPEN-014, OPEN-015, OPEN-017, OPEN-022, OPEN-026, OPEN-031, RISK-007, RISK-021, RISK-026.
- **New:**
  - OPEN-406: which Unity Editor version and license the current toolchain needs.
  - RISK-405: community-sourced technical claims may be wrong or stale.
  - RISK-406: extraction errors.

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
