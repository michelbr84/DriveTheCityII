# Upstream Strategy — Beta Test Driving Mod

| Field | Value |
|---|---|
| ID | DOC-UPSTREAM |
| Document status | Draft |
| Release scope | v1.0 foundation (vehicle control in Sandbox mode) |
| Phases | PHASE-00 (decision inputs), PHASE-02 (consumption), all later phases (tracking) |
| Responsible role | Coordinator (maintainer); the project owner approves the consumption option; the Reviewer checks every upstream-derived change |
| Related requirements | REQ-COMPAT-001, REQ-REL-003, REQ-ENTITY-001, REQ-DRIVE-001 |
| Related documents | [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [DEPENDENCIES](DEPENDENCIES.md), [CODING_STANDARDS](CODING_STANDARDS.md), [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md) |
| Last updated | 2026-10-18 |

> **Current state:** the base mod is **read-only reference material**. Nothing from it is in this project, and no
> revision is adopted for use.
>
> - **Audit pin:** TASK-0002 pinned and audited `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (2026-06-17). The remote
>   `HEAD` still pointed to it on 2026-10-18 (verified command `git ls-remote`, TASK-0002).
> - **Findings:** [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.
> - **License facts:** [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) §3.1.

## 1. Purpose

This document defines how the candidate base mod may be consumed, how upstream changes are tracked, and which
attribution and coexistence rules apply. The option itself is chosen after TASK-0002 (source and license audit) and
TASK-0003 (isolated baseline), and is recorded in an ADR. The ADR number is assigned when it is written.

## 2. Options

| Option | Description | Advantages | Disadvantages | Preconditions |
|---|---|---|---|---|
| **A — Fork** | Start from the pinned upstream source, keep its history, and refactor it into our module structure | Fastest path to a driving sandbox; attribution is straightforward; upstream fixes are easier to merge | Inherits:<br>• a static global-state design;<br>• legacy input and IMGUI;<br>• unregistered and dead code (the `ReliableDrive*` stack, the police chase, the traffic guard, the duplicate traffic-presence system, the uncalled turn-intent and mesh-bounds code);<br>• the relative build path;<br>• the decompiled-folder and "CS2 GTA Edition" provenance questions;<br>• tight coupling to native components;<br>• a declared `GameVersion` 1.5.* against an installed 1.6.2f1;<br>• no player-state machine, no on-foot mode, no career vehicles (TASK-0002) | OPEN-404 and proposed OPEN-411 resolved; MIT obligations accepted; project license decided (OPEN-027); TASK-0003 passed on the installed game version |
| **B — Selective re-implementation** | Write our own Vehicles and CityBridge code following our contracts, adapting specific techniques or snippets where useful | Clean architecture ([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md)); only reviewed parts enter; easier to keep host types in CityBridge | Slower; adapted snippets still carry MIT obligations; we own every bug | Per-snippet provenance recorded; headers and notices at incorporation. Snippet candidates are limited to the "reusable code" row of [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.10. Collision code stays concept only until proposed OPEN-411 is resolved. |
| **C — Reference only** | Use upstream only as documentation and as a behavioral baseline to compare against; no code copied | No license obligations from code; full design freedom | Slowest; each technique is rediscovered | None beyond TASK-0003 for comparison |

**Working assumption [Proposal]:** Option B is the likely fit, because career mode needs spawned career vehicles
that the base mod does not provide (OPEN-012, OPEN-402) and because SPEC-MODULES confines host types to CityBridge.

TASK-0002 (2026-10-18) **reinforces** this assumption. The base mod's architecture does not match SPEC-CONTROL and
SPEC-ENTITY: it has a single static driving flag and no state machine, legacy input without host-input suppression,
no save or load hooks, possession of any native car including service vehicles, and a handback without stop or
verification steps. The valuable parts are techniques: the freeze and control ordering, the handback flags, lane
presence, and the chase-camera ordering. Those techniques transfer as concepts. The suitability verdict is in
BASE_MOD_AUDIT §9.9.

**This is not a decision.** The ADR records the actual choice with the TASK-0003 evidence.

Reading MIT-licensed code before writing similar code does not remove attribution duties when substantial portions
are copied or closely adapted. When in doubt, attribute, and flag the case in the license audit. No legal conclusion
is drawn here.

## 3. Consumption rules (all options)

1. **Pin by full SHA.** Branch names are never used as a reference. The pinned SHA is recorded in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), in the ADR, and (if code is incorporated) in [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md) and the file headers.
2. **No vendoring during PHASE-00.** Source used for the baseline build stays in an owner-approved folder outside the project root ([BUILD_AND_RUN](BUILD_AND_RUN.md) §4).
3. **Incorporation is an explicit event.** The project license must be decided (OPEN-027). The audit must be complete. The change adds the license text and notice, and the Reviewer signs off.
4. **No out-of-scope code.** These are not carried over ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §5 and §9.10):
   - the police chase;
   - the traffic guard;
   - the duplicate traffic-presence system;
   - the `ReliableDrive*` stack;
   - the uncalled turn-intent and mesh-bounds code;
   - the IMGUI HUD;
   - the legacy input with `PlayerPrefs`.
5. **No unclear provenance.** Nothing traceable to decompiled material (OPEN-404) or to the "CS2 GTA Edition" collision layer (proposed OPEN-411) is incorporated until the owner resolves the question.
6. **Independent identity.** Our mod has its own name, assembly name, mod ID, settings keys, UI binding group, and log name, so the two mods cannot collide. It reuses none of the upstream identifiers:
   - `BetaTestDrivingMod` as assembly, namespace, `mod.json` id, or log name `BetaTestDrivingMod.Mod`;
   - `ModId` 145145;
   - the UI group `betatestdrivingmod`;
   - the `PlayerPrefs` prefix `BetaTestDrivingMod.DirectDrive.Key.`;
   - the HUD GameObject name;
   - the IMGUI window IDs 7114021 and 9425501.

   The list was verified by TASK-0002 (BASE_MOD_AUDIT §9.12).

## 4. Never co-load with the original

The original base mod and Drive the City II must **never** control vehicles in the same session (REQ-COMPAT-001).

- Lab test sets never contain both (OPEN-035).
- PHASE-01 investigates whether our mod can detect the original at load time and refuse to enable its controllers, with a clear message (OPEN-023). Detection must not depend on load order: the official dev diary states that there is "no such thing as a mod loading order" (SRC-002).
- The user documentation tells players to disable the original before using ours, until detection is proven.
- **Detection candidates (source facts, TASK-0002):**
  - a loaded assembly or type named `BetaTestDrivingMod` / `BetaTestDrivingMod.Mod`;
  - the UI binding group `betatestdrivingmod`;
  - the logger `BetaTestDrivingMod.Mod`.

  Forks may rename all of these. The fork announced in issue #1 (Paradox Mods id 162039) was not inspected.
- **Why co-loading is harmful (TASK-0002, BASE_MOD_AUDIT §9.12):**
  - The base mod accepts any entity with the native car components. It could possess our mod-spawned vehicles.
  - While it drives, it clears all selections on every UI update.
  - It polls V, WASD, the arrow keys, F7, F8, F9, and Numpad 0 and 9 in every context.
  - Both mods would drive the camera in the same update phase.

## 5. Tracking upstream changes

| What | How (read-only) | Frequency |
|---|---|---|
| New commits on the upstream default branch | Compare the latest SHA with the pinned SHA through the public repository page or API | Before each phase that touches vehicle control, and after each game update |
| License or notice changes | Re-read `LICENSE` and `NOTICE.md` at the new SHA | With every SHA comparison |
| Declared game version | Re-read `Properties/PublishConfiguration.xml` | With every SHA comparison |
| Issues reporting bugs in techniques we use | Read the open issues | With every SHA comparison |

**Community fork (information only):** upstream issue #1 (opened 2026-10-05 by a third party) announces an MIT fork
with Chinese localization and a handling rework. It reports two suspected upstream bugs: an unnecessary
vehicle-kind save-back, and a collision resolver that re-corrects the same entity. The fork was **not inspected**
and is **not a candidate**.

TASK-0002 checked both reports against the pinned code:

- **Save-back:** the method the issue names (`SetVehicleKind`) does not exist at the pin, so that report concerns
  the fork's code.
- **Collision re-correction:** plausible from the code (BASE_MOD_AUDIT §9.6.6) and listed as baseline check BL-18.

The GitHub API also lists 2 plain forks, which were not inspected.

**Baseline for tracking (TASK-0002):**

- 8 commits; no tags, releases, or pull requests.
- `GameVersion` "1.5.*" unchanged across all sampled revisions.
- `LICENSE` MIT since `af355236521636e987801e4c14019aaf187ec277`.

A future comparison starts from these facts.

## 6. Update review procedure

**Proposed procedure (requires validation).**

1. Record the candidate SHA and the reason for considering it.
2. Read the diff between the pinned and candidate SHAs, file by file, focusing on control, handback, lane presence, collision, and camera.
3. Re-check the license and notice, and the declared game version.
4. If code was incorporated, map each changed upstream file to our adapted files (from the file headers) and decide per change: port, ignore, or re-implement.
5. Build and test in the lab setup with owner authorization; record an evidence run.
6. Update the pin in every place listed in §3, the CHANGELOG, and the compatibility matrix.

## 7. Contribution back upstream

Reporting a reproducible upstream bug, or offering a fix, is a courtesy the project may extend. It requires owner
authorization, because it involves public accounts and publishing. It is never a prerequisite for our work.

## 8. Open questions and risks

- **Central:** OPEN-004, OPEN-005, OPEN-011, OPEN-012, OPEN-023, OPEN-027, RISK-001, RISK-002, RISK-016.
- **New:**
  - OPEN-402: the control technique for spawned vehicles.
  - OPEN-404: provenance of decompiled material.
  - OPEN-405: the copyright holder and license history.
  - RISK-404: provenance taint.
- **Proposed by TASK-0002** (pending register entries):
  - OPEN-411: the collision-code origin.
  - OPEN-413: unregistered systems.
  - OPEN-415: borrowable vehicle classes.
  - RISK-411: collision-code provenance.
  - RISK-412: co-loading interference.

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
