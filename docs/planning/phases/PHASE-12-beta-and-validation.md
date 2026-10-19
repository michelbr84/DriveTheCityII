# PHASE-12 — Beta and validation

| Field | Value |
|---|---|
| ID | PHASE-12 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M4 — Refined and validated beta |
| Responsible role | Coordinator (beta lead); QA (test campaigns, triage); Implementer (fixes); Reviewer (save format, release readiness); project owner (beta distribution authorization, gate and M4 acceptance) |
| Related requirements | REQ-SAVE-002 (migration path), REQ-PERF-001, REQ-COMPAT-001, plus regression of every requirement — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Prove that the mod works beyond the laboratory city. Test across **representative cities** (small, medium, and large;
dense districts; bridges and tunnels; heavy traffic), **hardware**, **DLCs**, and the **mod combinations declared as
supported**. Re-run every test in all three layers: logic (payments, progression, contracts, ownership, data
migration), integration (control transitions, entities, camera, persistence), and in-game (driveability, collisions,
traffic, interface, fun). Exercise update, disable, old-data, and write-failure scenarios. Measure frame time, memory,
and simulation speed against the recorded baseline.

## 2. Entry conditions

- PHASE-10 and PHASE-11 gates **Passed**.
- A beta plan approved by the owner (WP-12-01), including the city set, hardware set, DLC set, and supported mod list.
- Owner authorization for distributing pre-release builds to testers, through a private channel decided in OPEN-602.
  No public publication happens in this phase.

## 3. Dependencies

- **Phases:** PHASE-10, PHASE-11 (and, through them, every earlier phase).
- **Open questions:** OPEN-023 (conflicts with other mods), OPEN-602 (testers and pre-release delivery), OPEN-603
  (representative test cities), OPEN-017 (platform rules that affect private test builds), OPEN-022 (clean exit,
  re-checked before PHASE-13).

## 4. Required reading

[TEST_STRATEGY](../../quality/TEST_STRATEGY.md), [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md),
[SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md), [BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md),
[COMPATIBILITY_MATRIX](../../quality/COMPATIBILITY_MATRIX.md), [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md),
[KNOWN_ISSUES](../../quality/KNOWN_ISSUES.md), [QA_CHECKLIST](../../quality/QA_CHECKLIST.md),
[SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [SPEC-PERF](../../architecture/PERFORMANCE_BUDGET.md),
[TRACEABILITY](../TRACEABILITY.md), [SECURITY.md](../../../SECURITY.md).

## 5. Deliverables

- An approved beta plan and tester pool.
- A full regression pass recorded against every `TEST-*` in TEST_SCENARIOS and SAVE_SAFETY_TESTS.
- The save campaign and long-session results.
- Performance measurements compared with the baseline.
- A compatibility sweep, with tested combinations recorded in COMPATIBILITY_MATRIX.
- A triaged KNOWN_ISSUES list with no open critical defects of the classes named in the gate.
- TRACEABILITY evidence cells filled with real `RUN-*` links for every requirement.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-12-01 | Beta plan | OPEN-603 | City set (small, medium, large; dense districts; bridges; tunnels; heavy traffic), hardware set, DLC set, supported mod list. Third-party cities only with recorded provenance and terms. |
| WP-12-02 | Testers and pre-release delivery | OPEN-602, OPEN-017 | Who tests, on what hardware, and how they receive builds privately. Owner authorization required. Testers' data is sanitized in evidence. |
| WP-12-03 | Full regression | All requirements | Logic, integration, and in-game layers per TEST_STRATEGY. |
| WP-12-04 | Save campaign | REQ-SAVE-001 – REQ-SAVE-006, RISK-005 | **[Proposal]** 30 save/load cycles across careers and modes; older-save migration (REQ-SAVE-002); unknown newer schema refused; injected write failure; mod update; mod disabled; old data. |
| WP-12-05 | Long sessions | RISK-006, RISK-022 | **[Proposal]** sessions of at least two hours; orphan, ledger, and timer reconciliation at the end of each session. |
| WP-12-06 | Performance comparison | REQ-PERF-001, RISK-008 | Same conditions as the baseline. A recurring frame-time regression above 10% triggers an investigation. No fixed frame rate is promised. |
| WP-12-07 | Compatibility sweep | REQ-COMPAT-001, OPEN-023, RISK-016 | Supported game version, DLCs, and mods; known conflicting driving or camera mods detected or documented. |
| WP-12-08 | Triage, fixes, and gate report | Gate | Every fix is re-verified with a new run; Reviewer and owner acceptance of PHASE-12 and milestone M4. |

## 7. Out of scope

New features (the scope is frozen except for fixes). Public publication (PHASE-13). Performance budgets that have no
measured baseline. Support for game versions, DLCs, or mods not in the approved beta plan.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-SAVE-002 | Logic, In-game | Forward migration from older schemas; refusal of unknown newer schema | Not run |
| TEST-PERF-001 | In-game | Comparison with the recorded baseline | Not run |
| TEST-COMPAT-001 | In-game | Supported combinations and conflicting-mod handling | Not run |
| TEST-SAVE-109 (save campaign) | In-game | Proposed 30 save/load cycles; defined in [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md) | Not run |
| TEST-SAVE-114 and TEST-PERF-107 (long-session campaign) | In-game | Proposed sessions of at least two hours; defined in [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md) and [BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md) | Not run |
| Full regression | All layers | Every `TEST-*` defined for v1.0 | Not run |

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): **[Proposal]** *30 save/load cycles and sessions of at least two
hours; no known critical save corruption, duplicated rewards, permanent loss of control, or uncompletable mandatory
content.* In addition: every `Must` requirement has a Passed validation with a `RUN-*` link, or an owner-approved scope
decision recorded in the [register](../RISKS_AND_OPEN_QUESTIONS.md) ([PRD](../../product/PRD.md) §9); the
compatibility matrix lists every tested combination; a Reviewer and the project owner must accept the gate and milestone M4.

## 10. Risks and open questions

RISK-005 (save corruption), RISK-006 (duplicated rewards), RISK-007 (a game update during the beta), RISK-008
(performance), RISK-016 (mod conflicts), RISK-013 (unverified work reported as complete), RISK-030 (tester personal
data), RISK-602 (human-operated sessions). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **A critical defect is found** (save corruption, duplicated reward, permanent loss of control, uncompletable
  mandatory content). Fix it, add a regression test, and re-run the affected campaign from the start. The gate cannot
  pass with an open critical defect.
- **A game update arrives during the beta.** Record it, re-run the smoke and save tests on the new version, and update
  COMPATIBILITY_MATRIX. Decide with the owner which version the release will support.
- **Performance regresses beyond the trigger.** Investigate per BENCHMARK_PROTOCOL; disable optional visual features by
  default if they are the cause.
- **A supported mod conflicts.** Detect it or document it as unsupported in COMPATIBILITY_POLICY; never claim
  compatibility without a recorded run.

## 12. Evidence record

None yet. Beta runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md). Large
logs and recordings stay outside the repository with durable references, as described there.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
