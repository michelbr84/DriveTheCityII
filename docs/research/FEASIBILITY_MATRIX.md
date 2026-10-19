# Feasibility Matrix

| Field | Value |
|---|---|
| ID | DOC-FEASIBILITY |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-00 – PHASE-13 (each row names its resolving phase or task) |
| Responsible role | Coordinator (maintainer); the Implementer and QA update rows when evidence runs exist |
| Related requirements | All `REQ-*` in the [PRD](../product/PRD.md) that depend on host-game capabilities |
| Related documents | [SOURCES](SOURCES.md), [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md), [CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md), [TRACEABILITY](../planning/TRACEABILITY.md), [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md) |
| Last updated | 2026-10-18 |

## 1. Purpose and reading rules

This matrix records, for each host-game capability the PRD depends on, **how far the evidence has actually reached**.
It does not decide designs. Those decisions live in the specifications and ADRs.

Evidence levels are cumulative and kept separate:

1. **Documented upstream:** a third-party page or README says it.
2. **Source-inspected:** we read upstream source files read-only. "Raw" means byte-level verified; "Extraction" means the text came through a summarizing fetch tool and may contain errors ([SOURCES](SOURCES.md) §2).
3. **Built locally:** compiled by us, with a recorded evidence run.
4. **Tested in-game:** observed by us at runtime in a lab city, with a recorded evidence run.

**A feature documented or implemented by another project is not a feature compiled or tested by us.** As of
2026-10-18, the *Built locally* and *Tested in-game* columns read **Not run** in every row. The "Documented
upstream" cell names the source and the game version that source was verified for, when one is stated.

**TASK-0002 update (2026-10-18).** Every "Source-inspected" cell that concerns BTDM now reflects a raw reading of
the pinned commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`, marked "Raw at pin". Details are in
[BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §9. "Built locally" and "Tested in-game" remain **Not run** in every row.

Abbreviations: "BTDM" is the Beta Test Driving Mod (SRC-001). "Official" means a Paradox page; "Community" means a
non-official source.

## 2. Matrix

### 2.1 Vehicle control and entity lifecycle

| ID | Capability | Documented upstream | Source-inspected | Built locally | Tested in-game | What we know | Gaps | REQ / OPEN / RISK | Resolving |
|---|---|---|---|---|---|---|---|---|---|
| FEAS-001 | Direct control of a live native vehicle (sandbox, `BorrowedNativeVehicle`) | BTDM README (raw): takes over a live vehicle, freezes vanilla path movement, applies player movement after the car move step. Declared `GameVersion` 1.5.* | Raw at pin `eefd6bb…` (TASK-0002): the freeze system (between `CarNavigationSystem` and `CarMoveSystem`) sets a 0.05 m navigation target, maximum speed 0, and a pending path owner. The control system (after `CarMoveSystem`) writes `Transform`, `Moving`, `CarNavigation`, `PathOwner`, and `TransformFrame`. Movement is kinematic. The native systems still process the car every tick. | Not run | Not run | One concrete technique exists in an MIT-licensed project and matches our sandbox concept. Any native `Car` with the navigation components can be possessed, including service and public-transport vehicles. | Not built. Declared `GameVersion` 1.5.* versus the installed 1.6.2f1. Exclusivity comes from update ordering only, with no write-verify check. The CHANGELOG records native/Burst crashes from such writes. | REQ-DRIVE-001, REQ-CTRL-001 · OPEN-005, OPEN-011 · RISK-001 | TASK-0003, PHASE-02 |
| FEAS-002 | Handback to native AI with route rebuild and lane cleanup | BTDM README (raw): rebuilds vanilla pathfinding from the current lane on release; purges lane-buffer markers | Raw at pin `eefd6bb…` (TASK-0002): on release, `CarCurrentLane` is snapped to the nearest lane pose and flagged `Obsolete`. A 6–22 m navigation target is set, the `PathElement` and `CarNavigationLane` buffers are cleared, `PathOwner` is set to `Obsolete`, and every mod-written lane-object entry is removed. There is no stop requirement and no verification, and no handback on save, load, or dispose. | Not run | Not run | A handback approach is described and partly visible in the source | No observation of traffic after release. Stale reservations not checked. A save during possession may persist a frozen vehicle (proposed OPEN-412). | REQ-ENTITY-001 · OPEN-011 · RISK-004 | TASK-0003 (observation), PHASE-02 |
| FEAS-003 | Controlled vehicle visible to native traffic (lane presence) | BTDM README (raw): lane presence so traffic sees the car; "one writer" for lane buffers | Raw at pin `eefd6bb…` (TASK-0002): the control system writes the possessed car into the nearest lane's `Game.Net.LaneObject` buffer through `NetUtils.UpdateLaneObject`, covering about 1.7 m behind to 2.0 m ahead of the car. It removes the car from lanes it left, and from all drivable lanes within 34 m every 24 frames. The separate presence system is an unregistered duplicate. | Not run | Not run | Native traffic awareness is attempted by writing native lane data | Correctness and cost unknown; duplicate logic in unregistered code | REQ-WORLD-004, REQ-ENTITY-001 · OPEN-011, OPEN-032 | PHASE-02, PHASE-03 |
| FEAS-004 | Spawning and removing career vehicles (Rental, Owned, Service) | None found. BTDM README (raw): "It does not spawn a fake player car." | Raw at pin `eefd6bb…` (TASK-0002): no spawn code exists in any file, registered or not. The possession code requires a native car archetype (`Car`, `CarNavigation`, `CarCurrentLane`, `Moving`, `TransformFrame`, a path owner) plus the game's navigation and movement systems. This partly answers OPEN-402. | Not run | Not run | The candidate base does **not** cover career vehicles | Spawning, traffic acceptance, removal, models, and whether the control technique applies to spawned vehicles | REQ-ENTITY-002, REQ-ENTITY-003, REQ-GARAGE-001 · OPEN-012, OPEN-402, OPEN-034 | PHASE-02, PHASE-05 |
| FEAS-005 | Distinct handling for car, light delivery vehicle, rigid truck, bus | None found. BTDM README (raw): tuning for speed, launch, brake, reverse, steering | Raw at pin `eefd6bb…` (TASK-0002): one global tuning set for every vehicle. Collision hitboxes come from prefab-name keywords: car 2.2 × 1.7 × 4.8 m; bus or truck 2.8 × 2.7 × 8.8 m. | Not run | Not run | Global tuning values exist in BTDM | Per-class parameters; large-vehicle behavior | REQ-DRIVE-003 | PHASE-03, PHASE-07, PHASE-08 |

### 2.2 World contact, physics, and time

| ID | Capability | Documented upstream | Source-inspected | Built locally | Tested in-game | What we know | Gaps | REQ / OPEN / RISK | Resolving |
|---|---|---|---|---|---|---|---|---|---|
| FEAS-006 | Contact with native vehicles | BTDM README (raw): vehicle collision kept; does not push or rewrite hit cars; skips targets on other road heights | Raw at pin `eefd6bb…` (TASK-0002): swept probes at the leading bumper (the rear bumper when reversing), tested against moving cars only. Parked cars, bicycles, trams, and pedestrians are excluded. A 0.75 m height filter applies. The hit car is never written, and the possessed car keeps at least 35% of its speed. | Not run | Not run | Vehicle-to-vehicle contact exists in BTDM | Pedestrians are not covered, and our contact rule is not chosen. Issue #1's stutter report is plausible from the code (BASE_MOD_AUDIT BL-18). | REQ-WORLD-004 · OPEN-032 | PHASE-03 |
| FEAS-007 | Collision with buildings and static obstacles | BTDM README (raw): "Building collision and crash fire effects are not added in this build." | Raw at pin `eefd6bb…` (TASK-0002): no building, prop, or terrain collision exists in any file. | Not run | Not run | No source shows usable building collision | Data source for building volumes; proxy generation cost | REQ-WORLD-002, REQ-WORLD-003, REQ-PLAYER-004 · OPEN-008 · RISK-003 | PHASE-03 |
| FEAS-008 | Valid surface on roads, slopes, elevated roads, bridges, tunnels | BTDM README (raw): road-height assist; pitch aligned to road grade on elevated roads | Raw at pin `eefd6bb…` (TASK-0002): height comes from the nearest drivable lane: the current lane, or a quad-tree search within 26 m and ±3.2 m. A lane counts only within 20 m. Pitch follows the lane grade. No terrain or water data is read. | Not run | Not run | Road-lane-based height is used by BTDM | Off-road terrain, sidewalks, tunnels, and walking surfaces are not covered | REQ-WORLD-001, REQ-PLAYER-002 · OPEN-007 | PHASE-03 |
| FEAS-009 | Physics approach (enhanced kinematic or local physical controller) | None official | Raw at pin `eefd6bb…` (TASK-0002): the movement model is purely kinematic. `UnityEngine.PhysicsModule` is referenced, but no file uses a physics API. | Not run | Not run | A kinematic precedent exists | No physical-controller evidence; no WheelCollider compatibility assumed | REQ-DRIVE-002, REQ-DRIVE-003 · OPEN-009 · [ADR-0004](../architecture/decisions/ADR-0004-physics-strategy.md) | PHASE-03 |
| FEAS-010 | Bounded motion under low or unstable frame rates | BTDM README (raw): "capped real-time catch-up" for FPS drops | Raw at pin `eefd6bb…` (TASK-0002): each control tick uses the real elapsed time, clamped to 1/60–1/30 s. The camera uses unscaled time capped at 0.05 s. | Not run | Not run | One clamping approach is visible | Behavior under stalls, pause, and focus loss is not measured. The interaction with simulation speeds 2–3 is unknown (proposed OPEN-414). | REQ-TIME-003 | PHASE-03 |
| FEAS-011 | Read, set, and restore simulation speed; detect pause | Community (SRC-013, game ~1.5.x): selected versus eased speed values; the value while paused "Needs Verification (in-game)" | Raw at pin `eefd6bb…` (TASK-0002): BTDM neither reads nor changes simulation speed or pause; `SimulationSystem` is used only for `frameIndex`. | Not run | Not run | A community recipe exists | No official page; pause semantics unverified | REQ-TIME-001, REQ-TIME-002 · OPEN-013 · RISK-405 | PHASE-03 |
| FEAS-012 | Clear held input when the game window loses focus | None found | Raw at pin `eefd6bb…` (TASK-0002): BTDM has no focus handling. Input older than 0.35 s is zeroed. | Not run | Not run | Nothing yet | Everything | REQ-TIME-002 | PHASE-03 |

### 2.3 Avatar, cameras, input, and interface

| ID | Capability | Documented upstream | Source-inspected | Built locally | Tested in-game | What we know | Gaps | REQ / OPEN / RISK | Resolving |
|---|---|---|---|---|---|---|---|---|---|
| FEAS-013 | Visible, animated, mod-owned avatar (walk, run) | None found in any source | Raw at pin `eefd6bb…` (TASK-0002): no avatar, pedestrian, on-foot, or enter/exit code exists in any file. | Not run | Not run | Nothing; this is the least-evidenced v1.0 capability | Representation, animation, assets, provenance | REQ-PLAYER-001 … REQ-PLAYER-004, REQ-ART-001 · OPEN-010 · RISK-009 | PHASE-04 |
| FEAS-014 | Camera ownership (chase, first and third person) and restore to the building camera | BTDM README (raw): attached chase camera | Raw at pin `eefd6bb…` (TASK-0002): the camera system runs after `CameraUpdateSystem` in `PreCulling`. It activates the orbit controller without a follow target, overwrites the camera transform every frame, and restores the previous or gameplay controller on standby. There is no restore on dispose. | Not run | Not run | A vehicle chase camera with restore exists | On-foot cameras; clipping avoidance | REQ-DRIVE-001, REQ-PLAYER-003, REQ-CTRL-002, REQ-CTRL-004 | PHASE-02, PHASE-04 |
| FEAS-015 | Character-mode input isolated from building tools and camera | Official (SRC-010, verified for 1.3.6f1): mod actions through the game's Unity-Input-System-based bindings; conflicts disable mod bindings. BTDM README (raw): "Use the arrow keys if WASD moves your game camera or screen." | Raw at pin `eefd6bb…` (TASK-0002): BTDM polls legacy `UnityEngine.Input` every frame in every context. It has no suppression of game input, no focus handling, and no gamepad support. | Not run | Not run | BTDM does not isolate input; an official binding mechanism exists | How to suppress building input while in character mode | REQ-CTRL-003 · OPEN-014 · RISK-020 | PHASE-02 |
| FEAS-016 | Remappable actions | Official (SRC-010, 1.3.6f1): `ProxyBinding` on a `ModSetting` subclass | Raw at pin `eefd6bb…` (TASK-0002): BTDM stores `KeyCode` names in `PlayerPrefs` (prefix `BetaTestDrivingMod.DirectDrive.Key.`), written on rebind and on "Reset". The arrow keys are hard-wired. | Not run | Not run | An official remapping path is documented | Where rebinds persist; not tried | REQ-INPUT-001 · OPEN-014 | PHASE-02 (probe), PHASE-11 |
| FEAS-017 | Gamepad for walking, driving, and menus | Official (SRC-010, 1.3.6f1): a gamepad binding attribute exists | — | Not run | Not run | Gamepad bindings are documented for mod actions | Analog steering and throttle; menu navigation | REQ-INPUT-002 · OPEN-025 | PHASE-02 (probe), PHASE-11 |
| FEAS-018 | HUD and menus | Official (SRC-009, verified for 1.5.7f1): Coherent Gameface, React, TypeScript; module registry; flex layout only | Raw at pin `eefd6bb…` (TASK-0002): a `UISystemBase` with 50 value and 39 trigger bindings in the group `betatestdrivingmod`. A hand-written `.mjs` (no build step) appends to `GameTopLeft` and `Game` using `cs2/api` and `cs2/ui`. IMGUI is used only for the debug overlays; its window is unreachable. | Not run | Not run | Both the official stack and a working-looking example exist | Our UI build pipeline is not set up | REQ-UI-001 … REQ-UI-004 · OPEN-015 | PHASE-01 (spike), PHASE-11 |
| FEAS-019 | English and PT-BR localization of mod strings | None read (no official localization page fetched) | Raw at pin `eefd6bb…` (TASK-0002): `Colossal.Localization` is referenced but unused. All strings are hard-coded English. | Not run | Not run | Nothing usable yet | The mechanism and language switching | REQ-L10N-001 · OPEN-026 | PHASE-01, PHASE-11 |
| FEAS-020 | Settings with a safe-disable switch | Official (SRC-002): settings UI generated from attributed properties; SRC-010: `ModSetting` | Raw at pin `eefd6bb…` (TASK-0002): there is no `ModSetting`. Settings live in static fields and reset every session; only key bindings persist. | Not run | Not run | An official settings mechanism is documented | Not tried | REQ-DIAG-002 | PHASE-01 |

### 2.4 Persistence and city integration

| ID | Capability | Documented upstream | Source-inspected | Built locally | Tested in-game | What we know | Gaps | REQ / OPEN / RISK | Resolving |
|---|---|---|---|---|---|---|---|---|---|
| FEAS-021 | Career data stored inside the save | Community only (SRC-005, verified for 1.6.2f1; SRC-013, ~1.5.10f1): serializable components and systems with an own version int | Raw at pin `eefd6bb…` (TASK-0002): no serialization exists in any file; nothing is persisted in saves. | Not run | Not run | A community-described mechanism exists | No official documentation; no spike | REQ-SAVE-001 … REQ-SAVE-005 · OPEN-006 · [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md) · RISK-405 | PHASE-01 |
| FEAS-022 | Save identity (city identity, save revision) | None found | — | Not run | Not run | Nothing | Everything | REQ-SAVE-001 · OPEN-006 | PHASE-01 |
| FEAS-023 | City remains loadable after the mod is removed | Community (SRC-005): "uninstalling a mod loses that mod's components and keeps the city" | — | Not run | Not run | One community claim | Never tested; sidecar behavior unknown | REQ-SAVE-006 · OPEN-022 · RISK-017 | PHASE-01, PHASE-13 |
| FEAS-024 | Read roads and lanes near the player | Official (SRC-002): mods can access entities and components | Raw at pin `eefd6bb…` (TASK-0002): `Game.Net.SearchSystem.GetLaneSearchTree` is iterated within a 26 m box, with a current-lane shortcut and a 4-frame cache. | Not run | Not run | Lane lookup is possible in principle | Read-only discipline; cost | REQ-CITY-001, REQ-WORLD-001 · OPEN-007 | PHASE-02, PHASE-03 |
| FEAS-025 | Validated access points (curbside, stop, loading bay) | None found | — | Not run | Not run | Nothing | Everything | REQ-CAREER-003, REQ-HOME-001, REQ-HOME-002 · OPEN-018 | PHASE-06 |
| FEAS-026 | Route and connectivity validation before offering a mission | None found | Raw at pin `eefd6bb…` (TASK-0002): BTDM only invalidates the path owner on release. An uncalled turn-intent routine scans all connection lanes. | Not run | Not run | Nothing directly usable | Native pathfinding versus a mod-side graph | REQ-CAREER-002, REQ-CHAL-003 · OPEN-019 | PHASE-06 |
| FEAS-027 | Read transit lines, stop order, and stop access | None found | — | Not run | Not run | Nothing | Everything | REQ-BUS-001, REQ-BUS-002 · OPEN-020 | PHASE-08 |
| FEAS-028 | Detect city edits made in Building mode | None found | — | Not run | Not run | Nothing; the fallback is full revalidation on return | Event or diff mechanism | REQ-CITY-002, REQ-CAREER-006 · OPEN-021 | PHASE-10 |
| FEAS-029 | No unintended writes to native city state | Official (SRC-002): mods can access components, which includes writing them | Raw at pin `eefd6bb…` (TASK-0002): the registered code writes only the possessed car and the lane-object buffers, and it clears the global selection while driving. The unregistered code would write other cars' velocity (traffic guard) and police, crime, and dispatch state (police chase). | Not run | Not run | Writing native state is easy, so discipline is needed | Proof that activation and deactivation leave no change | REQ-CITY-001, REQ-CITY-004 | PHASE-01, PHASE-10 |

### 2.5 Engineering, release, and workflow

| ID | Capability | Documented upstream | Source-inspected | Built locally | Tested in-game | What we know | Gaps | REQ / OPEN / RISK | Resolving |
|---|---|---|---|---|---|---|---|---|---|
| FEAS-030 | Build toolchain and runtime target | Official (SRC-008, verified for 1.1.12f1 only): toolchain components. Community (SRC-005): `net48`, C# 9 | Raw at pin `eefd6bb…` (TASK-0002): `net48`, C# 9.0, a relative `ManagedPath`, and no Mod.props/Mod.targets. The source uses no Burst, jobs, or `SystemAPI`. | Not run | Not run | The best-supported runtime target is `net48` (not officially confirmed) | Current versions for the installed game; post-processor need | OPEN-002, OPEN-003, OPEN-403, OPEN-406 · RISK-021 | TASK-0003 |
| FEAS-031 | Mod entry point, system registration, update ordering | Official (SRC-002): register your own system in the update loop. Community (SRC-011): update phases | Raw at pin `eefd6bb…` (TASK-0002): `IMod.OnLoad(UpdateSystem)` with `UpdateAt`, `UpdateAfter`, and `UpdateBefore` in `UIUpdate`, `GameSimulation`, and `PreCulling`. Unregistered systems are compiled into the assembly (proposed OPEN-413). | Not run | Not run | A consistent pattern across sources | Unregistered systems in BTDM; ordering against the current game | REQ-RECOV-002 · OPEN-003 | PHASE-01 |
| FEAS-032 | Bounded, sanitized logging and diagnostics bundle | Community "Logging" page listed, not read | Raw at pin `eefd6bb…` (TASK-0002): `LogManager.GetLogger("BetaTestDrivingMod.Mod").SetShowsErrorsInUI(false)`, with rate-limited warnings. | Not run | Not run | A logger exists in the game libraries | Log location, size bounds, export | REQ-DIAG-001 | PHASE-01 |
| FEAS-033 | Runtime debugging on the retail game | Community (SRC-005): `unity-devtools` needs a Unity Mono development build ("a retail build exposes no SDB port"); `coherent-gameface` needs a CDP endpoint whose enablement in CS2 is undocumented | — | Not run | Not run | Neither tool is known to work against the retail game | Everything | OPEN-031 · RISK-026 | TASK-0003 |
| FEAS-034 | Local deployment and isolated test mod set | Official (SRC-008, 1.1.12f1): the local mods folder is auto-loaded; a "."-prefixed folder is disabled | — | Not run | Not run | A documented local deploy path exists | Profile or playset isolation; owner's setup | OPEN-035 · RISK-028 | TASK-0001, TASK-0003 |
| FEAS-035 | Never co-load conflicting controllers with the original BTDM | Official (SRC-002): no mod load order | Raw at pin `eefd6bb…` (TASK-0002): stable detection candidates exist: the type `BetaTestDrivingMod.Mod`, the UI group `betatestdrivingmod`, and the logger name. The conflict list is in BASE_MOD_AUDIT §9.12. | Not run | Not run | Load order cannot be relied on | A detection mechanism | REQ-COMPAT-001 · OPEN-023 · RISK-016 | PHASE-01, PHASE-12 |
| FEAS-036 | Publishing on Paradox Mods | Official (SRC-008, 1.1.12f1; SRC-012) | Raw at pin `eefd6bb…` (TASK-0002): `PublishConfiguration.xml` declares `ModId` 145145, `0.3.54-stable`, `GameVersion` 1.5.*, the tag "Code Mod", and `AccessLevel` Public. There is no `Dependency` element. | Not run | Not run | The flow is documented, and a real example exists | Current rules and review; account requirements | REQ-REL-001, REQ-REL-004 · OPEN-017 · RISK-025 | PHASE-13 |
| FEAS-037 | Melty as an optional workflow aid | SRC-004 (raw): copy instructions into Claude Code or Codex; CS2 and Motor Town not listed; exclusive licence for published content | — | Not run | Not run | No folder selector or integrated agent is documented | Everything about the app's real behavior | OPEN-016, OPEN-401 · RISK-010, RISK-401, RISK-402 | PHASE-00 (owner-attended evaluation) |
| FEAS-038 | Repeatable performance measurement | None found | — | Not run | Not run | Nothing | A baseline protocol run | REQ-PERF-001 · RISK-008 | PHASE-03, PHASE-12 |

## 3. Reading the matrix

- **No row has reached "Built locally" or "Tested in-game".**
- Rows with **no upstream evidence at all** carry the highest scope risk: FEAS-004, FEAS-012, FEAS-013, FEAS-022, FEAS-025, FEAS-026, FEAS-027, FEAS-028, FEAS-038. These need early spikes in their phases. If a spike shows infeasibility, a scope review is recorded ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §8); requirements are not silently dropped.
- Rows that rest only on **community** sources (FEAS-011, FEAS-021, FEAS-023) are hypotheses pinned to the game versions those sources name (RISK-405).
- BTDM evidence covers **sandbox driving of native vehicles** only. Career-mode needs (FEAS-004, FEAS-025 … FEAS-028) are ours to solve.

## 4. Updating a row

1. A cell moves to "Built locally" or "Tested in-game" only with a link to a real `RUN-NNNN` record under [evidence](../quality/evidence/README.md), plus the game version and mod revision.
2. Update the related `OPEN-*` entry in the [register](../planning/RISKS_AND_OPEN_QUESTIONS.md) and the [TRACEABILITY](../planning/TRACEABILITY.md) evidence cell in the same change.
3. A negative result is recorded as clearly as a positive one: it is evidence too.

## 5. New entries raised by this document

New entries are summarized here; the full entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).

- **OPEN-402:** Can the BTDM control technique drive vehicles spawned by our mod?
- **OPEN-403:** How was the BTDM build produced, and is the post-processor needed?
- **OPEN-406:** Which Unity Editor version and license does the current toolchain need?
- **RISK-405:** Community-sourced technical claims may be wrong or stale.

Proposed by TASK-0002 (pending register entries by the Coordinator; full text in BASE_MOD_AUDIT §8 and the TASK-0002
handoff):

- **OPEN-412:** the effect of saving while a vehicle is possessed (FEAS-002).
- **OPEN-413:** whether unregistered systems ever run (FEAS-029, FEAS-031).
- **OPEN-414:** the control time base under pause and simulation speed (FEAS-010, FEAS-011).
- **OPEN-415:** identifying borrowable vehicle classes from data (FEAS-001).
