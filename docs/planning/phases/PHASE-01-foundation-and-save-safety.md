# PHASE-01 — Foundation and save safety

| Field | Value |
|---|---|
| ID | PHASE-01 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M0 — Verified foundation |
| Responsible role | Coordinator (phase lead); Implementer; QA (evidence); Reviewer scoped to the save format (critical area: [SPEC-SAVE](../../architecture/SAVE_FORMAT.md)); project owner (decisions, gate acceptance) |
| Related requirements | REQ-SAVE-001, REQ-SAVE-002, REQ-SAVE-003, REQ-SAVE-004, REQ-SAVE-006 (first pass), REQ-CITY-001, REQ-CITY-004, REQ-CTRL-002 (skeleton), REQ-ENTITY-003 (registry), REQ-RECOV-002, REQ-DIAG-001, REQ-DIAG-002, REQ-L10N-001 (externalization), REQ-COMPAT-001 (base-mod co-load), REQ-REL-003 (per incorporation) — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Create Drive the City II as an **independent, reversible mod** with its own identity, clear module boundaries,
diagnostics, safe activation and unload, and a **save foundation** that binds career data to the correct save. No
gameplay is added. The phase proves that the mod can be installed, used, saved, reloaded, and disabled without changing
the city, and that invalid data never replaces the last valid copy.

## 2. Entry conditions

- PHASE-00 gate **Passed** with evidence and owner acceptance. Its reuse recommendation is known.
- Owner decisions recorded before any code is written:
  - project license (OPEN-027), if any third-party code will be incorporated;
  - version control initialization and hosting (OPEN-029);
  - location of build outputs and local files relative to OneDrive (OPEN-028).
- Owner authorization to create the code project, build, install into the isolated mod set, and run lab-city sessions.

## 3. Dependencies

- **Phases:** PHASE-00 (toolchain, isolated setup, base-mod findings).
- **Open questions:** OPEN-006 (persistence mechanism, decided here through ADR-0003), OPEN-022 (saves after disable or
  removal; first pass), OPEN-023 (conflict check against the original base mod), OPEN-015 (UI technology; one-element
  spike), OPEN-026 (localization mechanism; one-string spike), OPEN-003 (confirmed target framework), OPEN-027,
  OPEN-028, OPEN-029. New: OPEN-604 (stable technical identifiers).
- **Decisions:** [ADR-0003](../../architecture/decisions/ADR-0003-career-save-isolation.md) moves from Proposed to a
  decision in this phase. [ADR-0001](../../architecture/decisions/ADR-0001-host-game-and-runtime.md) and
  [ADR-0002](../../architecture/decisions/ADR-0002-control-authority.md) constrain the skeleton.

## 4. Required reading

- Architecture: [SPEC-ARCH](../../architecture/ARCHITECTURE.md), [SPEC-MODULES](../../architecture/MODULE_CONTRACTS.md),
  [SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
  [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md), [SPEC-RECOVERY](../../architecture/ERROR_RECOVERY.md),
  [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), the [ADR index](../../architecture/decisions/README.md).
- Engineering: [CODING_STANDARDS](../../engineering/CODING_STANDARDS.md), [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md),
  [DEBUGGING](../../engineering/DEBUGGING.md), [DEPENDENCIES](../../engineering/DEPENDENCIES.md),
  [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md).
- Legal and compatibility: [REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md),
  [THIRD_PARTY_NOTICES](../../../THIRD_PARTY_NOTICES.md), [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md),
  [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) (limitations list).
- Quality: [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md), [TEST_STRATEGY](../../quality/TEST_STRATEGY.md),
  [SPEC-L10N](../../experience/LOCALIZATION.md).

## 5. Deliverables

- A buildable mod skeleton in the owner-approved repository, with its own name, identifiers, and version, and with the
  notices of any incorporated code preserved.
- The module skeleton from [SPEC-MODULES](../../architecture/MODULE_CONTRACTS.md): CityBridge, Player, WorldContact,
  Vehicles, Career, Persistence, UI, Diagnostics, and a composition root, with the documented dependency direction.
  Career is a pure-logic component that is testable without CS2.
- Diagnostics: size-bounded logs that state the mod version and build, and an exportable, sanitized diagnostics
  bundle. Settings with a safe-disable switch.
- A control-state skeleton: `Building` plus the `Loading` and `Unloading` overlays, ready for later states. An entity
  registry that proves zero orphans on unload and disable.
- The persistence foundation chosen through ADR-0003: save identity (city identity, career profile ID, save revision
  ID), schema version, stable references, atomic writes where the storage allows, quarantine, and the last valid copy.
- A first clean-exit observation (OPEN-022) and a draft procedure for [INSTALL_UPGRADE_UNINSTALL](../../release/INSTALL_UPGRADE_UNINSTALL.md).
- Externalized player-facing strings (English source plus a PT-BR path) and one drawn HUD element.
- A co-load guard: this mod never activates conflicting controllers while the original Beta Test Driving Mod is loaded.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-01-01 | Pre-code decisions | OPEN-027, OPEN-028, OPEN-029, OPEN-604 | Owner decides the license (before incorporating code), version control, and output locations. Fix stable technical identifiers that are independent of the working title. |
| WP-01-02 | Independent identity, credits, and co-load guard | REQ-COMPAT-001, REQ-REL-003, OPEN-023 | Own mod ID and assembly name. Preserve upstream notices in [THIRD_PARTY_NOTICES](../../../THIRD_PARTY_NOTICES.md). Detect the original base mod and refuse to run conflicting controllers at the same time; document it in [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md). |
| WP-01-03 | Module skeleton and dependency direction | SPEC-MODULES | Ports and adapters. A logic-test harness for Career that runs without CS2. A check that forbidden dependencies (for example, Career → CityBridge) do not exist. |
| WP-01-04 | Diagnostics, logging, settings, safe-disable | REQ-DIAG-001, REQ-DIAG-002, REQ-RECOV-002 | Bounded log size, version stamp, sanitized bundle (RISK-030). Failure containment: a failing subsystem degrades to `Building` and logs the error. |
| WP-01-05 | Activation, unload, and no-change guarantee | REQ-CTRL-002, REQ-ENTITY-003, REQ-CITY-001, REQ-CITY-004 | State skeleton with guards. Entity registry. A before/after comparison method for city state (buildings, roads, budget, population) defined with QA. |
| WP-01-06 | Save foundation spike and decision | OPEN-006, ADR-0003, REQ-SAVE-001 – REQ-SAVE-004 | Compare In-save, Sidecar (revision-bound), and Hybrid storage in the lab city against [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md). The owner decides. Then implement the chosen foundation. Critical area: one coordinated task at a time. |
| WP-01-07 | Clean-exit check (first pass) | OPEN-022, REQ-SAVE-006, RISK-017 | Save a lab city with the mod → disable → load → record what remains; repeat after removal. No safe-uninstall claim follows from this. |
| WP-01-08 | String externalization and UI spike | REQ-L10N-001, OPEN-026, OPEN-015 | One externalized string switching between English and PT-BR; one HUD element drawn through the chosen UI technology. |
| WP-01-09 | Phase gate report | — | Evidence summary; SPEC-SAVE updated to the accepted ADR-0003; Reviewer (save format) and owner acceptance. |

## 7. Out of scope

Driving, the avatar, missions, economy, gameplay UI beyond one HUD element, physics, any write to native city state,
publishing, and performance tuning. Base-mod code is incorporated only after the license decision and the
[REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md) entry for that code.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-SAVE-001 | In-game | Older save never shows newer-session data | Not run |
| TEST-SAVE-002 | Logic, Integration | Schema version, forward migration path, refusal of unknown newer schema | Not run |
| TEST-SAVE-003 | Integration | Stable references revalidated on load | Not run |
| TEST-SAVE-004 | Integration | Atomic write, quarantine, last valid copy kept under injected corruption | Not run |
| TEST-SAVE-006 | In-game | First clean-exit pass (disable and removal) | Not run |
| TEST-CITY-001 | Integration | No native economic or service writes | Not run |
| TEST-CITY-004 | In-game | Activate, use, deactivate: no unintended city change | Not run |
| TEST-CTRL-002 | Integration | `Building` and overlays skeleton only | Not run |
| TEST-ENTITY-003 | Integration | Registry; zero orphans on unload and disable | Not run |
| TEST-RECOV-002 | Integration | Injected subsystem failure degrades safely | Not run |
| TEST-DIAG-001 | Integration | Bounded, version-stamped logs; sanitized bundle | Not run |
| TEST-DIAG-002 | In-game | Safe-disable returns to `Building` without data loss | Not run |
| TEST-L10N-001 | Integration | Externalization only (full EN/PT-BR in PHASE-11) | Not run |
| TEST-COMPAT-001 | In-game | Co-load guard against the original base mod | Not run |
| TEST-REL-003 | Review | Per incorporated component | Not run |

TEST-SAVE-* are defined in [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md); the others in
[TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *install, open, save, close, reopen, and disable with no unwanted
city change; invalid data never overwrites the last valid copy.* Specifically:

1. A recorded run installs the skeleton into the isolated mod set, opens the lab city, saves, closes, reopens, and
   disables the mod, with the before/after comparison showing no unintended city change.
2. Injected invalid or corrupt career data is quarantined, and the last valid copy survives (TEST-SAVE-004).
3. Loading an older lab save shows no data from a newer session (TEST-SAVE-001).
4. ADR-0003 is decided and accepted by the owner, and SPEC-SAVE is updated to match it.
5. Logs are bounded and sanitized; safe-disable works; the co-load guard is demonstrated.
6. Review by a Reviewer scoped to the save format, and owner acceptance.

## 10. Risks and open questions

RISK-005 (save corruption or leakage), RISK-016 (mod conflicts), RISK-017 (unloadable saves after removal), RISK-021
(toolchain mismatch), RISK-007 (game updates), RISK-011 (OneDrive), RISK-014 (concurrent critical edits), RISK-026
(debugging), RISK-029 (game content in the repository), RISK-030 (personal data in logs), RISK-602 (human-operated
sessions). Open questions: section 3, including the new OPEN-604. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **In-save storage is unsupported or unsafe.** Use the revision-bound sidecar option from ADR-0003.
- **Neither option can bind data to a save revision reliably.** This threatens the core no-leak guarantee
  (REQ-SAVE-001). Stop persistence-dependent work and open a scope review with the owner. Do not weaken the guarantee
  silently.
- **A save made with the mod does not load after the mod is disabled.** Block career-persistence design until the
  cause is known. Prefer storage that keeps the city save free of mod data, and re-test.
- **The original base mod cannot be detected at load time.** Document the conflict and the manual check in
  [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md) and the user guides, and keep the gate open until a
  run shows that both mods are never active together in a supported setup.
- **A failed gate** keeps the phase In progress. Fixes are re-verified with new runs, not by editing old evidence.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md) with
[TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md). The ADR-0003 decision and the gate decision will be
referenced here.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
