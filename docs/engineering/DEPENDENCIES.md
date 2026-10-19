# Dependencies

| Field | Value |
|---|---|
| ID | DOC-DEPENDENCIES |
| Document status | Draft |
| Release scope | v1.0 and later |
| Phases | PHASE-00 (inventory), PHASE-01 (first project), PHASE-13 (release audit) |
| Responsible role | Coordinator (maintainer); the project owner authorizes every installation and every runtime dependency |
| Related requirements | REQ-REL-001, REQ-REL-003, REQ-REL-004, REQ-COMPAT-001 |
| Related documents | [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md), [UPSTREAM_STRATEGY](UPSTREAM_STRATEGY.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md), [TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md) |
| Last updated | 2026-10-18 |

> **Current state (2026-10-18):** no dependency of any class has been added to the project. No package, library,
> plugin, or third-party code is incorporated. Nothing was installed during the documentation baseline.

## 1. Purpose

This document defines the classes of dependency the project can have, who is responsible for each, the candidates
known today and their status, and the pinning and update rules. Confirmed versions are recorded in
[TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md); licenses and notices are recorded in [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md)
and [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md).

## 2. Dependency classes

| Class | Examples | Shipped to players? | Responsibility | Rule |
|---|---|---|---|---|
| **D1 — Development tools** | Official modding toolchain, Unity Editor, .NET SDK, IDE, Node.js (for UI), Git | No | The project owner installs them; the Implementer records versions | Installed only with explicit owner authorization; versions pinned in TOOLCHAIN_MATRIX |
| **D2 — Build-time references to game assemblies** | Game, Colossal, Unity, and UnityEngine assemblies from the installed game | **Never** | CityBridge owns their use in code | Referenced from the owner's installation (through toolchain variables); never committed, copied into the project, or redistributed (RISK-029) |
| **D3 — Runtime mod dependencies** | Other Paradox Mods mods required at runtime | Declared in the publish configuration | The project owner approves each one | **None declared.** Adding one requires an ADR and an entry in the [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md). The core distribution needs only CS2 plus documented dependencies (REQ-REL-004). |
| **D4 — Libraries compiled into the mod** | NuGet packages; copied or adapted source | Yes (inside our assembly or package) | The Reviewer checks the audit and notice | None today. Each one needs a completed license audit, a notice with the full license text, and a pinned version or SHA. |
| **D5 — Optional tooling** | Community agent plugins, debugging tools, Melty | No | The project owner decides | Not installed. Never required by any phase gate; never auto-installed from documentation or web content. |

## 3. Candidate list

| Candidate | Class | Status | Evidence | Notes |
|---|---|---|---|---|
| Beta Test Driving Mod (source) | D4 if code is adapted; otherwise reference only | **Candidate, not incorporated** | SRC-001, **[Source-inspected]** raw at the pinned commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (TASK-0002, 2026-10-18). MIT at the pin; the first 4 commits used a restrictive license ([REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) §3.1). | The decision follows TASK-0003 ([UPSTREAM_STRATEGY](UPSTREAM_STRATEGY.md)). It is never a D3 runtime dependency: our mod must not require the original to be installed (REQ-COMPAT-001). TASK-0002 classifies only small, separable functions as reusable code; everything else is concept only or not reusable ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.10). |
| Base mod's own dependencies (facts, for the TASK-0003 baseline only) | — | Recorded | **[Source-inspected]** raw at pin | **Runtime:** none. There is no `Dependency` element and `mod.json` has `"dependencies": []`. No NuGet packages. **No Harmony, no BepInEx.** It is a native CS2 code mod (`IMod`, `OnLoad(UpdateSystem)`) loaded by the game's own mod manager. **Build:** the 18 game references below, `net48`, C# 9.0, and no official toolchain import. It needs no Unity Editor, ModPostProcessor, or Node.js (the UI module is hand-written). The build plan is in BASE_MOD_AUDIT §9.13. |
| Official modding toolchain (Unity Editor, .NET SDK, ModPostProcessor, ModPublisher) | D1 | **Not installed** on this machine (TASK-0001; [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md)) | SRC-008 (verified for 1.1.12f1) | OPEN-002, OPEN-406. The BTDM baseline build does not use it (BASE_MOD_AUDIT §9.5). |
| .NET Framework 4.8 reference assemblies | D1 | Not found on this machine (TOOLCHAIN_MATRIX) | Needed to compile any `net48` project, including the BTDM baseline | They may arrive as a targeting pack or developer pack, or as a NuGet reference-assembly package that SDK builds can add implicitly (**[Unverified]**). Any of these is an installation or download that needs owner authorization. |
| Game reference assemblies (the base mod references 18) | D2 | Not checked | SRC-001 project file (raw at pin): `Game`; `Colossal.Core`, `.Collections`, `.Mathematics`, `.Logging`, `.IO.AssetDatabase`, `.Localization`, `.UI`, `.UI.Binding`; `Unity.Collections`, `Unity.Entities`, `Unity.Mathematics`; `UnityEngine.CoreModule`, `.InputLegacyModule`, `.PhysicsModule`, `.IMGUIModule`, `.ImageConversionModule`; `System.Memory`. No source file uses the physics, localization, or image-conversion APIs. | The exact set our mod needs is decided in PHASE-01. `UnityEngine.InputLegacyModule` and `IMGUIModule` serve BTDM approaches we do not plan to use (REQ-INPUT-001; [SPEC-UI](../experience/UI_AND_HUD.md)). |
| UI build tooling (Node.js, UI template) | D1 | Node observed (`v24.19.0`); template not checked | SRC-008, SRC-009 | Only if the UI uses the official stack (OPEN-015) |
| Harmony (`Lib.Harmony`) | D4 / D3 | **Not planned** | SRC-002 (official): "Harmony can patch only managed code", so Burst-compiled code cannot be patched. SRC-005 (community): version 2.2.2 "pinned by community agreement". The base mod does not use Harmony (TASK-0002, raw at pin). | Considered only if a requirement cannot be met through supported extension points; requires an ADR |
| BepInEx | — | **Not a dependency** | Not referenced by the base mod (TASK-0002). CS2 code mods load through the game's own mod manager (`IMod`; SRC-002, SRC-008). | Not planned |
| `cs2-modding` agent knowledge plugin | D5 | Not installed | SRC-005 (community, MIT) | May be read online as reference; installing it requires owner authorization |
| `unity-devtools` agent plugin | D5 | Not installed | SRC-005 | Needs a Unity Mono development build of the game and the .NET 10 SDK; a retail build "exposes no SDB port" (OPEN-031) |
| `coherent-gameface` agent plugin | D5 | Not installed | SRC-005 | Needs a reachable CDP endpoint; enablement in CS2 is undocumented (OPEN-031) |
| Melty | D5 | Not used by agents | SRC-004 | Optional; never a runtime or build dependency ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)) |
| Motor Town | — | Not a dependency | SRC-003 | Design reference only ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)) |

## 4. Pinning policy

1. **Exact versions only.** No floating ranges for packages, and no "latest" for tools. Source dependencies are pinned to a full commit SHA, never a branch name.
2. **Single home.** Tool versions live in [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md). Incorporated code records its SHA in [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md) and in the file headers ([CODING_STANDARDS](CODING_STANDARDS.md) §9).
3. **The game version is a dependency too.** Every tested combination records the game version in [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md).
4. **No transitive surprises.** Any package added later must have its full dependency tree listed and audited before merge.
5. **The runtime target is not an upgrade lever.** Never change the mod's target framework to satisfy a tool (RISK-021).

## 5. Update review procedure

**Proposed procedure (requires validation).**

1. **Trigger:** a game update, a toolchain notification, an upstream commit of interest, or a security advisory.
2. **Read-only review:** read the change notes and diff (for source dependencies, compare the pinned SHA with the candidate SHA). Record what changed and why it matters.
3. **License re-check:** confirm that the license and notices have not changed (update [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md)).
4. **Isolated build and test:** in the lab setup, with owner authorization. Record the results as an evidence run.
5. **Decision:** adopt (update the pin, the matrix, the notices, and the CHANGELOG) or defer (record the reason).

## 6. Responsibilities summary

| Role | Responsibility |
|---|---|
| Project owner | Authorizes installs, runtime dependencies, and license-relevant incorporations |
| Coordinator | Keeps this list, the toolchain matrix, and the notices consistent |
| Implementer | Records observed versions; never adds a dependency without an approved task |
| Reviewer | Blocks merges that add unaudited code, unpinned versions, or redistributed game files |
| QA | Confirms that the release package contains no D2 files and that the notices are complete (PHASE-13) |

## 7. Open questions and risks

- **Central:** OPEN-002, OPEN-003, OPEN-015, OPEN-027, OPEN-031, RISK-002, RISK-021, RISK-029.
- **New:** OPEN-403 (the build path and post-processor), OPEN-406 (the Unity Editor version and license).
- **Proposed by TASK-0002** (pending register entries): OPEN-411 (the provenance of the base mod's collision code),
  RISK-412 (interference from a co-loaded base mod).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
