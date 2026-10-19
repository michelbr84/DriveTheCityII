# Compatibility Policy

| Field | Value |
|---|---|
| ID | DOC-COMPAT-POLICY |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 and maintenance releases |
| Phases | PHASE-00 (baseline), PHASE-01 (co-load check), PHASE-12 (compatibility sweep), PHASE-13 (declaration) |
| Responsible role | Coordinator (policy maintainer); QA (evidence and matrix); Reviewer (checks evidence); project owner (approves the published declaration) |
| Related requirements | REQ-COMPAT-001, REQ-REL-001, REQ-SAVE-002, REQ-PERF-001 |
| Related documents | [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md), [POST_RELEASE_MAINTENANCE](POST_RELEASE_MAINTENANCE.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md) |
| Last updated | 2026-10-18 |

> **No combination is supported today.** Nothing has been built or tested, so every game version, DLC set, and mod
> combination is **Untested**. This policy defines how that can change; the evidence lives only in
> [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md).

## 1. Purpose

Separates the **rules** (this document) from the **evidence** (the matrix). A compatibility statement anywhere in the
project, in release notes, or on a channel listing must be traceable to a matrix row with a `RUN-NNNN` record.

## 2. What a combination is

A combination is identified by: the exact game version string as reported by the game; the store edition; Windows as
the platform; the DLC set; the mod version and build ID (and commit, once Git exists); and every other active mod with
its version. Changing any element creates a different combination.

## 3. Support levels

| Level | Meaning | Required evidence |
|---|---|---|
| **Supported** | Expected to work; defects are fixed in maintenance releases. | Passed runs of the compatibility test set (section 4) on this exact combination, reviewed. |
| **Supported with known issues** | Works with documented limitations or workarounds. | Same runs, plus `ISSUE-*` entries in [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) linked from the matrix. |
| **Untested** | No evidence. It may or may not work; no claim is made. | None. Default for everything. |
| **Incompatible** | Known not to work, or conflicts by design. | A failed run or a documented design conflict. |
| **Must not co-load** | The combination is prevented or explicitly warned against. | Design rule plus a verified detection or warning behavior. |

## 4. How a combination becomes Supported

1. QA defines the combination in a new matrix row (status Untested).
2. In a lab environment with an isolated mod set, QA runs the compatibility test set: the smoke suite in
   [TEST_STRATEGY](../quality/TEST_STRATEGY.md) §11, at least one complete career mission, a save/load cycle, and the
   clean-exit check ([INSTALL_UPGRADE_UNINSTALL](INSTALL_UPGRADE_UNINSTALL.md)).
3. Each run is recorded as `RUN-NNNN` with [TPL-TEST-REPORT](../templates/TEST_REPORT_TEMPLATE.md).
4. A Reviewer who did not run the tests checks the evidence and sets the level in the matrix.
5. For a release, the project owner approves the published declaration (section 8).

**Evidence does not transfer.** A level applies to the exact combination tested. A new game version, mod version, or
dependency version starts as Untested. **[Proposal]:** a channel declaration covers only the game versions that were
actually tested, even if the channel accepts version wildcards.

## 5. Game-update policy

CS2 updates may change the internals the mod relies on (RISK-007).

- On a game update, the new version is **Untested**. Earlier rows keep their results.
- QA runs the smoke suite on the new version (TEST-COMPAT-103) before any further feature testing, then the full
  compatibility test set.
- The toolchain is re-verified: the official documentation says the toolchain's Unity version must track game updates
  **[Documented upstream]**; confirmed versions go to [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
- If tests fail, the response follows [POST_RELEASE_MAINTENANCE](POST_RELEASE_MAINTENANCE.md): a notice, a
  recommendation to use the safe-disable switch or keep the mod disabled, and a hotfix.
- During development, the lab holds one game version for the length of a test campaign (OPEN-508), and a change of
  version is recorded before testing continues.
- Career data stays bound to its save identity and schema version across game updates (REQ-SAVE-001, REQ-SAVE-002).

## 6. Conflicting mods

- **Original Beta Test Driving Mod — Must not co-load.** Drive the City II and the original mod must never run
  conflicting vehicle controllers at the same time (REQ-COMPAT-001). **[Proposal]:** detect the original mod at load and
  refuse to enter character mode, with a clear message; if detection is not feasible (OPEN-023), warn prominently in the
  documentation and listing. The PHASE-01 co-load check and TEST-COMPAT-001 verify the behavior. If upstream code is
  incorporated, our mod keeps its own identity so the two are never confused ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)).
- **Other driving, camera, or vehicle-control mods:** Untested by default and treated as likely conflicts until tested
  in PHASE-12.
- **Traffic and AI mods:** may affect handback and lane state ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md));
  tested before any Supported claim.
- **UI mods:** may overlap the HUD or capture input; tested for layout and input routing.
- **Load order is not a resolution strategy.** Paradox documents that CS2 has no mod loading order **[Documented
  upstream]**, so conflicts must be detected or documented, not "fixed" by ordering.
- Input conflicts with game bindings are handled through remapping ([SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md)).

## 7. DLC, platform, and hardware

- **Platform:** Windows only **[Owner requirement]**. Other platforms are out of scope for v1.0.
- **DLC:** representative DLC sets are tested in PHASE-12. DLC-specific content (vehicles, transit types, map features)
  is Untested until covered by a run.
- **Hardware:** no minimum specification or frame rate is promised. Performance is measured with
  [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md) (REQ-PERF-001).

## 8. The published compatibility declaration

Each release includes a declaration with: the date; Supported and Supported-with-known-issues combinations, copied from
the matrix with their run IDs; the Incompatible and Must-not-co-load list; and a sentence stating that every other
combination is Untested. The declaration never says "compatible with the latest version" without a run on that version.

## 9. Open questions and risks

OPEN-001 (installed version and DLCs), OPEN-023 (conflict detection), OPEN-033 (edition support), OPEN-508 (holding the
game version constant); RISK-001 (base mod incompatible with the installed version), RISK-007 (game updates), RISK-016
(conflicts with other mods).
