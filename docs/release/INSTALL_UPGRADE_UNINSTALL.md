# Install, Upgrade, and Uninstall

| Field | Value |
|---|---|
| ID | DOC-INSTALL |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 |
| Phases | PHASE-01 (first clean-exit test), PHASE-12, PHASE-13 |
| Responsible role | Coordinator (maintainer); QA executes the checklist; project owner authorizes every install and runtime test |
| Related requirements | REQ-REL-001, REQ-REL-002, REQ-SAVE-002, REQ-SAVE-004, REQ-SAVE-006, REQ-CITY-004, REQ-ENTITY-003, REQ-TRANS-004, REQ-DIAG-002, REQ-COMPAT-001 |
| Related documents | [PACKAGING_AND_PUBLISHING](PACKAGING_AND_PUBLISHING.md), [COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md), [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md), [QUICK_START](../user/QUICK_START.md), [TROUBLESHOOTING](../user/TROUBLESHOOTING.md) |
| Last updated | 2026-10-18 |

> **Pre-release draft — do not follow these steps with real saves.** No build or package exists, **no tested installer
> exists, and nothing is published**. The flows below are intended designs. **No claim is made that uninstalling the mod
> is harmless** until the clean-exit procedure has passed its tests (REQ-REL-002, REQ-SAVE-006, OPEN-022).

## 1. Purpose

Describes the intended install, upgrade, and clean-exit flows for players, and the checklist that must pass before any
of them is published as player guidance.

## 2. Principles

- A player needs no development tools (REQ-REL-001). Installation never modifies game installation files.
- The mod writes only to its own data locations ([SECURITY.md](../../SECURITY.md) §5); their exact locations are decided
  with the persistence mechanism (OPEN-006, [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md)) and
  logging design ([DEBUGGING](../engineering/DEBUGGING.md)), and will be listed here.
- Channel-specific steps (where to obtain, how to subscribe or enable) are written only after the channel is chosen
  (OPEN-017, [OPEN-701](../planning/RISKS_AND_OPEN_QUESTIONS.md)) and observed. No menu names or paths are given now.
- Every flow advises keeping a separate copy of the city save before changing the mod.

## 3. Intended install flow [Proposal]

1. Check that the installed game version is declared supported ([COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md)).
2. Make sure the original Beta Test Driving Mod is not active at the same time (REQ-COMPAT-001).
3. Obtain the mod and its declared dependencies from the chosen channel.
4. Enable the mod with the game's own mod management.
5. Start the game, open a copy of a city, and confirm that the mod reports its version (in its settings or its log).
6. Continue with [QUICK_START](../user/QUICK_START.md).

**Developer installs are not player installs.** The official toolchain documents that locally built mods are deployed
to a local mods folder that the game loads automatically **[Documented upstream]**. That path is for development, needs
owner authorization ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)), and is verified in TASK-0003, not here.

## 4. Intended upgrade flow [Proposal]

1. Read the release notes and the compatibility declaration for the new version.
2. In the game: complete or cancel active missions, return to Building mode, and save to a **new** copy.
3. Install the new version through the channel; start the game; load the copy.
4. On load, older career data migrates forward through the tested migration path (REQ-SAVE-002). If migration fails,
   the data is quarantined, the last valid copy is preserved, and the mod falls back to a safe state
   (REQ-SAVE-004, [ERROR_RECOVERY](../architecture/ERROR_RECOVERY.md)).
5. Verify the wallet, ledger, vehicles, and progress before continuing.

**Downgrades are not a supported flow.** An older mod version must refuse career data with a newer schema without
damaging it (REQ-SAVE-002).

## 5. Clean-exit procedure (draft, untested)

The clean-exit procedure is how a player stops using the mod while keeping the city loadable (REQ-SAVE-006).

1. **End control.** Return to Building mode. If the mod misbehaves, use the planned safe-disable switch, which returns
   control to Building mode and stops career systems without data loss (REQ-DIAG-002).
2. **Park and return entities.** Ending character mode parks career vehicles, hands back any borrowed native vehicle
   (sandbox mode only), and removes temporary mission objects (REQ-TRANS-004, REQ-ENTITY-003). Check that no
   mod vehicle or marker remains.
3. **Resolve missions.** Complete or cancel active missions so that no settlement is pending.
4. **Save to a new copy.** Never overwrite the main save.
5. **Exit the game, then disable or remove the mod** through the game's or channel's normal mechanism.
6. **Verify the copy opens without the mod.** Load it; confirm that the city loads, and that buildings, roads, budget,
   and citizens show no unintended change (REQ-CITY-004). Record the result.

What remains after removal is **unknown**. It depends on the storage option chosen in
[ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md) (in-save, sidecar, or hybrid). A community
source states that removing a mod drops that mod's saved components and keeps the city **[Documented upstream]**
(community, unverified by us). That statement is an input to OPEN-022, not a guarantee. If sidecar data exists, this
section will list its location so players can keep or delete it; the mod never deletes anything outside its own data.

## 6. Verification checklist (validation: Not run)

Run in a lab environment and, for release, on a clean configuration without development tools (OPEN-507).

| Check | Test | Validation |
|---|---|---|
| Fresh install on a supported configuration without SDKs, Unity, IDEs, or Node.js; first run reaches character mode | TEST-REL-001 | Not run |
| Install, update, and clean exit tested before any safe-uninstall wording is published | TEST-REL-002 | Not run |
| Clean exit: the saved copy loads without the mod, with no unintended city change | TEST-SAVE-006, TEST-CITY-004 | Not run |
| Upgrade from the previous released version with an active career profile migrates correctly | TEST-SAVE-002 | Not run |
| Older mod version refuses newer-schema data without loss | TEST-SAVE-002 | Not run |
| Failed migration quarantines data and keeps the last valid copy | TEST-SAVE-004 | Not run |
| Reinstalling after removal restores or explicitly reports career data, per the chosen storage option | TEST-SAVE-006 | Not run |
| Original Beta Test Driving Mod present: conflict detected or documented behavior observed | TEST-COMPAT-001 | Not run |
| Game installation files unchanged after install and removal (owner-authorized comparison) | TEST-REL-002 | Not run |

The first clean-exit check runs in PHASE-01 on the earliest skeleton; it is repeated on the release candidate in
PHASE-13.

## 7. Open questions and risks

OPEN-006 (persistence mechanism), OPEN-017 (platform requirements), OPEN-022 (saves after removal), OPEN-507 (clean
configuration and tester), [OPEN-701](../planning/RISKS_AND_OPEN_QUESTIONS.md) (channel choice); RISK-005 (save
corruption or leakage), RISK-017 (unloadable saves after removal), RISK-028 (damage to primary saves).
