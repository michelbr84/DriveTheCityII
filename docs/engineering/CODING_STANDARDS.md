# Coding Standards

| Field | Value |
|---|---|
| ID | DOC-CODING |
| Document status | Draft (all rules are **[Proposal]** until PHASE-01 confirms the toolchain) |
| Release scope | v1.0 and later |
| Phases | PHASE-01 onward (first code); reviewed whenever the toolchain changes |
| Responsible role | Coordinator (maintainer); the Reviewer enforces the rules in code review |
| Related requirements | REQ-DIAG-001, REQ-RECOV-002, REQ-SAVE-002, REQ-SAVE-003, REQ-CAREER-004, REQ-L10N-001, REQ-REL-003 |
| Related documents | [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md), [SPEC-ARCH](../architecture/ARCHITECTURE.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [DEBUGGING](DEBUGGING.md), [DEPENDENCIES](DEPENDENCIES.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md) |
| Last updated | 2026-10-18 |

> No code exists yet. These standards are proposals for the first code in PHASE-01. Rules that depend on the
> confirmed language version or target framework are marked **pending OPEN-003**.

## 1. Purpose and scope

These standards keep the mod's code reviewable, testable without CS2 where possible, and safe for players' saves.
They cover C# code, the UI module code (once the UI technology is confirmed, OPEN-015), logging, error containment,
serialization, tests, and attribution. Module boundaries are defined in [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md);
this document only states how code respects them.

## 2. Module boundaries in code

- **One compilation unit per boundary that matters [Proposal]:**
  - **Career** (plus the serialization and migration logic of Persistence) is built **without any reference** to `Game`, `Colossal.*`, `Unity.*`, or `UnityEngine.*` assemblies, so that it can be tested without CS2.
  - The mod assembly holds CityBridge, Player, Vehicles, WorldContact, UI, Diagnostics, and the composition root.
- **Host types stay in CityBridge** ([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) rule 3). Other modules exchange our own value types: positions, identifiers, enumerations, and records. A `using` of a host or engine namespace outside CityBridge is a review failure.
- **Ports belong to the consumer.** Interfaces are declared in the consuming module and implemented in adapters wired by the composition root.
- **Single writer.** Code may mutate only state owned by its module. Changes to other state go through commands or ports.
- **No static mutable global state** outside the composition root. The candidate base mod keeps its whole runtime state in a static class (SRC-001); we deliberately do not follow that pattern.

## 3. Naming and layout

| Element | Convention [Proposal] |
|---|---|
| Namespaces | `<RootNamespace>.<Module>[.<Area>]`. The root namespace is chosen with the mod identity in PHASE-01; the working title is not assumed to be final. |
| Types, methods, properties | PascalCase |
| Locals, parameters | camelCase |
| Private fields | `_camelCase` |
| Constants | PascalCase |
| Interfaces (ports) | `I` + noun (for example `ICareerClock`, as sketched in SPEC-MODULES) |
| Host-game fields | Keep the host's own names when reading or writing host structures inside CityBridge; never rename them in our types. |
| Files | One public type per file; the file name equals the type name. |
| Identifiers in data | Stable string or GUID identifiers for anything persisted (REQ-SAVE-003); never raw runtime entity indices. |

Formatting is enforced by `.editorconfig` once code exists. Line length and brace style follow the IDE defaults of the
confirmed toolchain, to avoid churn.

## 4. Language features (pending OPEN-003)

- The language version and target framework come from the toolchain's Mod.props. Community sources and the base mod indicate C# 9 and `net48` (see [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md) §3). Code must not use features beyond the confirmed version.
- Nullable reference types: enable them in the Career compilation unit **if** the confirmed language version and target allow it. Annotate public ports either way. The base mod disables them; we do not inherit that choice.
- Avoid `dynamic`, reflection into host internals, and runtime code generation. Reflection on host types, if it is ever needed, requires an ADR (RISK-007).
- Prefer immutable records or `readonly struct` value types for messages between modules, as far as the confirmed language version allows.

## 5. Logging

Every module logs through Diagnostics ([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) §4.9). No module writes its own files.

| Level | Use |
|---|---|
| Error | A fault that disabled a feature or forced recovery. Logged once per occurrence with context, then rate-limited. |
| Warning | An unexpected but handled condition (for example a revalidation failure, or a port returning `Unknown`). |
| Info | Lifecycle events: load, unload, player state transitions, authority grants and revocations, persistence results. |
| Debug | Diagnostic detail for development builds; off by default for players. |
| Trace | Per-frame detail; development builds only, never enabled by default. |

Rules:

- **Header:** each session logs the mod version, the build identifier, the persisted schema version, and (if the host reports it) the game version (REQ-DIAG-001).
- **Rate limiting:** collapse repeated identical messages. Never log per frame at Info or above.
- **Sanitization:** no user-profile paths (replace them with placeholders such as `<USER_DATA>`), account names, store IDs, or machine names. Save names are hashed (RISK-030).
- **Structured messages:** `module=<Module> event=<Name> key=value …`, so that logs can be searched.

## 6. Error containment

- Every per-frame entry point of a module is called inside the Supervisor's fault boundary ([SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md)). Exceptions never propagate into the game's update loop.
- On a fault: log it once, classify it, move to a safe state (ultimately `Building`), and mark the module Degraded after repeated faults. Never retry in a tight loop.
- Host access in CityBridge validates that entities and components exist before every read or write. A missing entity is an expected result, not an exception.
- Code compiled by Burst (if any) may not support managed exception handling in the same way **[Unverified]**. Containment must therefore wrap job scheduling and completion in managed code. To be confirmed in PHASE-01.
- No `catch` blocks that swallow exceptions silently: every catch logs or returns an explicit failure result.

## 7. Serialization and versioning rules

These rules apply whichever storage option [ADR-0003](../architecture/decisions/ADR-0003-career-save-isolation.md)
selects (OPEN-006).

1. **Own leading version.** Every persisted record starts with **our own schema version integer**. Community guidance (SRC-005, SRC-013) notes that the game's reader context version is the *game* build version and is not usable as ours. That guidance is community-documented and not yet verified (RISK-405).
2. **Additive evolution.** New fields are appended. Removed fields are skipped by version-aware readers. Every schema bump has a migration function and a golden-file test ([SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md)).
3. **Refuse newer schemas** without modifying data (REQ-SAVE-002), and quarantine invalid data (REQ-SAVE-004).
4. **Stable references only** (REQ-SAVE-003); resolution happens after load, through CityBridge.
5. **No heavy work inside deserialization callbacks.** Restore and revalidation run after the load completes (community guidance SRC-013; verify in PHASE-01).
6. **Determinism for money.** Settlement uses a TransactionId derived from MissionId + outcome (REQ-CAREER-004). No floating-point currency: use integer minor units **[Proposal]**.

## 8. Tests for pure logic

- Career and serialization code have automated tests that run without CS2. The test framework is **not selected yet**; it is chosen in PHASE-01 together with the confirmed target framework (OPEN-003).
- Tests are deterministic: time comes from an injected clock, randomness from a seeded source, and no test touches the file system except through fakes or temporary folders.
- Required families ([SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) §7):
  - lifecycle transition tables;
  - settlement idempotence;
  - ledger invariants;
  - company settlement across pauses and reloads;
  - save round trips and migrations.
- Test names reference the `TEST-*` ID they implement where one exists (for example `TEST-CAREER-004`).
- A passing logic test never substitutes for an in-game test of host behavior ([TEST_STRATEGY](../quality/TEST_STRATEGY.md)).

## 9. Third-party code and attribution

- **No third-party code is incorporated today** ([THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md)).
- Before any MIT-licensed code (for example from the candidate base mod) is copied or adapted:
  - the [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) entry must be complete;
  - the owner decides the project license (OPEN-027);
  - the incorporating change adds the full license text to THIRD_PARTY_NOTICES.
- Each file that contains copied or adapted code carries a header comment that names:
  - the origin project;
  - the upstream file path and the commit SHA;
  - the upstream copyright line exactly as written upstream (for the base mod: "Copyright (c) 2026 stoot");
  - the license (MIT);
  - a note that the file was modified.

  The header wording is finalized by the audit.
- **Never** paste decompiled game code or code of unclear provenance, including anything traceable to the base mod's excluded `decompiled_published_0317` folder (OPEN-404).
- Game assemblies are referenced, never copied into the repository or the package ([DEPENDENCIES](DEPENDENCIES.md)).

## 10. Localization in code

All player-facing strings go through a localization key; no literal user-facing text in code (REQ-L10N-001). Keys
are stable identifiers, such as `career.taxi.offer.title`. The mechanism is pending OPEN-026.

## 11. Review checklist (per change)

- [ ] Host or engine types appear only in CityBridge.
- [ ] No new mutable static state; single writer respected.
- [ ] Faults are contained; no silent catches; logs are sanitized.
- [ ] Persisted data is versioned; there is a migration and test for each schema change.
- [ ] Logic changes have deterministic tests.
- [ ] Third-party code: audit entry, notice, and header present.
- [ ] Player-facing text is externalized.
- [ ] Changes to the critical areas (control, entities, saves) have a scoped Reviewer sign-off.

## 12. Open questions

- **Central:** OPEN-003, OPEN-006, OPEN-015, OPEN-026, OPEN-027.
- **New:** OPEN-404 (provenance of decompiled material), RISK-405 (community guidance may be wrong).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
