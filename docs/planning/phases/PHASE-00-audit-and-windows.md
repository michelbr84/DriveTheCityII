# PHASE-00 — Audit and Windows environment

| Field | Value |
|---|---|
| ID | PHASE-00 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M0 — Verified foundation |
| Responsible role | Coordinator (phase lead); Implementer (discovery, audit, build); QA (evidence runs); Reviewer (independent gate check); project owner (authorizations, gate acceptance) |
| Related requirements | [REQ-REL-003](../../product/PRD.md) (license and provenance audit of the candidate base mod). This phase also produces inputs for REQ-COMPAT-001 and REQ-REL-004. |
| Last updated | 2026-10-18 |

> **Documentation alone cannot pass this phase.** The documentation baseline (TASK-0000) defines the questions and
> procedures; it answers none of them. The gate needs discovery results, an audited and pinned source revision, a
> reproducible build, and a recorded runtime baseline.

## 1. Objective

Establish verified facts before anything is built for Drive the City II:

1. What is installed on the owner's Windows machine: Cities: Skylines II (CS2) edition, version, store platform, DLCs,
   game and user-data locations, and the development toolchain. Whether this edition supports code mods.
2. What the candidate base mod ([Beta Test Driving Mod](../../research/BASE_MOD_AUDIT.md), SRC-001) is, legally and
   technically: license, copyright holders, pinned revision, dependencies, target framework, declared game version,
   and how it takes over and hands back vehicles.
3. How to test in isolation (separate mod set, disposable lab city, copies only) without touching the owner's primary
   saves, active mods, or game installation.
4. With explicit owner authorization only: a reproducible build and a recorded runtime baseline of the **unmodified**
   base mod at the pinned revision, with a list of its limitations.

The phase ends with a recommendation for PHASE-01 and PHASE-02: **reuse**, **partial reuse**, or **re-implementation**
of vehicle control, supported by evidence.

## 2. Entry conditions

- This is the first phase. There is no previous gate.
- The documentation baseline and [HANDOFF_TO_PHASE_00](../../agents/HANDOFF_TO_PHASE_00.md) are available and have been
  read by whoever starts the work.
- Authorizations, recorded in each task's status history with date and scope (never assumed):
  - [TASK-0001](../tasks/TASK-0001-windows-environment-audit.md): owner go-ahead before any read-only discovery outside
    the project root.
  - [TASK-0002](../tasks/TASK-0002-base-mod-source-audit.md): none beyond read-only reading of public sources.
  - [TASK-0003](../tasks/TASK-0003-isolated-baseline-validation.md): explicit owner authorization for each installation,
    source download, build, mod installation, game launch, and mod enablement.
  - WP-00-04 (Melty) and WP-00-05 (Motor Town): a separate owner decision before installing or launching anything.

## 3. Dependencies

- **Phases:** none.
- **Open questions answered here:** OPEN-001, OPEN-002, OPEN-003, OPEN-004, OPEN-005, OPEN-028 (observations), OPEN-031,
  OPEN-033, OPEN-035. Partly answered: OPEN-011 and OPEN-014 (source reading and observation), OPEN-022 (initial
  observation with the base mod). Optional: OPEN-016 (Melty), OPEN-030 (Motor Town). New: OPEN-601.
- **Recorded conflicts that shape this phase:** CONF-006 (no installs before the audits), CONF-008 and CONF-009 (Melty
  and games are not launched without authorization), CONF-010 (discovery, audit, and baseline are separate tasks).
- **People:** owner availability for consent and for attended runtime sessions (RISK-602).

## 4. Required reading

- [AGENTS.md](../../../AGENTS.md), [DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md), [PROJECT_STATUS](../../PROJECT_STATUS.md),
  [TOOLS_AND_PERMISSIONS](../../agents/TOOLS_AND_PERMISSIONS.md), [DEFINITION_OF_READY](../../agents/DEFINITION_OF_READY.md),
  [DEFINITION_OF_DONE](../../agents/DEFINITION_OF_DONE.md), [SECURITY.md](../../../SECURITY.md).
- Environment: [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md),
  [LOCAL_ENVIRONMENT.example](../../engineering/LOCAL_ENVIRONMENT.example.md), [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md),
  [DEBUGGING](../../engineering/DEBUGGING.md).
- Source and license: [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md), [SOURCES](../../research/SOURCES.md),
  [REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md), [DEPENDENCIES](../../engineering/DEPENDENCIES.md),
  [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md), [FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md).
- Runtime observation criteria: [SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md),
  [ADR-0001](../../architecture/decisions/ADR-0001-host-game-and-runtime.md), [TEST_STRATEGY](../../quality/TEST_STRATEGY.md),
  [evidence README](../../quality/evidence/README.md).
- Optional work packages: [MELTY_EVALUATION](../../research/MELTY_EVALUATION.md), [MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md).

## 5. Deliverables

| Deliverable | Produced by | Where it is recorded |
|---|---|---|
| Sanitized environment description; confirmed installed tool versions; private machine values kept local | WP-00-01 | [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), git-ignored local file per [LOCAL_ENVIRONMENT.example](../../engineering/LOCAL_ENVIRONMENT.example.md) |
| Completed source, license, and dependency audit at a pinned full commit hash | WP-00-02 | [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md), [REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md), [SOURCES](../../research/SOURCES.md), [DEPENDENCIES](../../engineering/DEPENDENCIES.md), [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md) |
| Build evidence (two clean builds) and runtime baseline evidence | WP-00-03 | `RUN-NNNN` records under [evidence](../../quality/evidence/README.md); [BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md) commands relabeled as verified with their date |
| Base-mod limitations list | WP-00-03 | [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md) |
| Owner-approved isolated test setup procedure | WP-00-06 | [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md), referenced by [TEST_STRATEGY](../../quality/TEST_STRATEGY.md) |
| Optional Melty findings; optional Motor Town loop study | WP-00-04, WP-00-05 | [MELTY_EVALUATION](../../research/MELTY_EVALUATION.md), [MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md) |
| Phase gate report with the reuse recommendation | WP-00-07 | This document (section 12), [PROJECT_STATUS](../../PROJECT_STATUS.md), [FEASIBILITY_MATRIX](../../research/FEASIBILITY_MATRIX.md), [register](../RISKS_AND_OPEN_QUESTIONS.md) |

## 6. Proposed work packages

| WP | Title | Task | Notes |
|---|---|---|---|
| WP-00-01 | Windows, game, and toolchain discovery | [TASK-0001](../tasks/TASK-0001-windows-environment-audit.md) | Read-only. Answers OPEN-001, OPEN-002 (installed side), OPEN-028 (observations), OPEN-033, OPEN-031 (presence), and the read-only part of OPEN-035. |
| WP-00-02 | Base mod source, license, and dependency audit | [TASK-0002](../tasks/TASK-0002-base-mod-source-audit.md) | Read-only online inspection; no cloning, building, or executing. Can run in parallel with WP-00-01. |
| WP-00-03 | Isolated baseline build and runtime validation | [TASK-0003](../tasks/TASK-0003-isolated-baseline-validation.md) | Needs WP-00-01, WP-00-02, WP-00-06, and explicit owner authorization. Lab city and copies only. |
| WP-00-04 | Melty evaluation (optional) | No task file yet | Documentation review first ([MELTY_EVALUATION](../../research/MELTY_EVALUATION.md)), then, only if the owner authorizes it, an owner-attended, observation-first session. Answers OPEN-016 and CONF-008. **Never blocks the gate.** The workspace stays independent of Melty. |
| WP-00-05 | Motor Town loop study as design reference (optional) | No task file yet | Map observable loops (accept → collect → transport → deliver → get paid → invest) to original CS2 adaptations in [MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md). No code, assets, or physics are imported. Whether an installation is needed at all is OPEN-030 (owner decision). Never blocks the gate. |
| WP-00-06 | Isolated test setup definition | Read-only part inside TASK-0001; owner approval before TASK-0003 | Define the separate mod set or profile, the disposable lab city (OPEN-603), save copies, and rollback (OPEN-035, RISK-028). Written as a proposed procedure until the owner approves it. |
| WP-00-07 | Phase gate report | Created when WP-00-03 is done | Consolidate evidence, limitations, recorded versions, and the reuse recommendation. Update the feasibility matrix and register. Request the Reviewer check and owner acceptance. |

Order: WP-00-01 and WP-00-02 in parallel → WP-00-06 → WP-00-03 → WP-00-07. WP-00-04 and WP-00-05 are independent.

## 7. Out of scope

- Creating Drive the City II's own code project, solution, or `src/` folder (PHASE-01).
- Changing the base mod's behavior. Only minimal, recorded build-configuration deviations are allowed (RISK-601). A
  functional change would mean the run is no longer a baseline.
- Forking, vendoring, or committing upstream source into the project; publishing anything.
- Initializing Git (OPEN-029) and choosing the project license (OPEN-027). These are owner decisions before PHASE-01
  code work.
- The persistence spike (PHASE-01), the physics decision ADR-0004 (PHASE-03), and any gameplay.
- Installing or launching Melty or Motor Town without an owner decision; using primary saves; altering the owner's
  normal mod set.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| [TEST-REL-003](../../quality/TEST_SCENARIOS.md) | Review | License and provenance audit of the candidate base mod. Nothing is incorporated yet; the full audit repeats per incorporation (PHASE-01) and before release (PHASE-13). | Not run |

Baseline observations (TASK-0003) are not product tests. They observe the third-party base mod against the criteria of
[SPEC-ENTITY](../../architecture/ENTITY_LIFECYCLE.md) (handback, lane state) and
[SPEC-CONTROL](../../architecture/CONTROL_AUTHORITY.md) (input and camera separation) to inform the reuse decision.
They **never count as validation of our requirements**. Proposed baseline steps: two clean builds; load the lab city;
take over a vehicle; drive a short route (straight, curve, junction, slope); hand it back; observe traffic for a
pre-declared observation window; disable the mod; reload a copy of the lab save.

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md) exit gate: *reproducible base-mod build and runtime baseline
recorded with evidence, plus a list of limitations; documentation alone cannot pass.* All of the following:

1. **Reproducible build.** The pinned revision builds from a clean state twice, with the toolchain versions recorded in
   [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md) and every build-configuration deviation listed. **[Proposal]**
   "Reproducible" means the same inputs produce a successful build with the same output file set; byte-identical
   output is recorded but not required.
2. **Recorded versions.** Game version and edition, sanitized DLC list, toolchain versions, and the full base-mod commit
   hash are recorded.
3. **Documented run.** At least one `RUN-NNNN` record of the runtime baseline in the lab city with the isolated mod set
   (load, take over, drive, hand back, observe traffic, disable, reload).
4. **Limitations list** in [BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md), and a reuse recommendation.
5. **License audit entry** complete for the candidate (OPEN-004 answered; TEST-REL-003 recorded).
6. **Isolated setup** documented and approved by the owner (OPEN-035).
7. **No collateral change.** The evidence states that no primary save, game installation file, or normal mod set was
   altered.
8. **Review and acceptance.** An independent Reviewer check and owner acceptance, per the [gate process](README.md).

Melty (WP-00-04) and Motor Town (WP-00-05) results are **not** gate criteria.

## 10. Risks and open questions

- Risks: RISK-001 (game-version incompatibility), RISK-002 (license), RISK-007 (game updates), RISK-010 (Melty), RISK-011
  (OneDrive), RISK-021 (toolchain mismatch), RISK-026 (retail debugging), RISK-027 (historical claims), RISK-028 (damage
  to primary saves), RISK-029 (redistribution of game content), RISK-030 (personal data in evidence).
- New in this group: RISK-601 (the unmodified baseline may need placement relative to the game installation or
  project-file edits), RISK-602 (in-game validation depends on human-operated sessions).
- Open questions: listed in section 3. New: OPEN-601 (location of the pinned upstream source copy), OPEN-603 (source of
  test cities).
- All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **The edition does not support code mods (OPEN-033).** Stop. Record the blocker in [PROJECT_STATUS](../../PROJECT_STATUS.md)
  and ask the owner for a decision. Do not work around platform rules.
- **The license forbids or constrains reuse (RISK-002).** Incorporate nothing. Keep the base mod as a behavioral
  reference only, within what its terms allow, and plan a clean-room implementation of vehicle control in PHASE-02.
- **The build fails at the pinned revision.** Align toolchain versions with the documented requirements; try one other
  pinned revision with a recorded rationale; allow only minimal, recorded build-configuration deviations. If it still
  fails, recommend re-implementing vehicle control. The owner may then approve an **alternative gate** (for example, a
  minimal mod from the official template that builds reproducibly and loads in the lab city), recorded as a `CONF-*`
  entry and a ROADMAP change. The gate is never declared passed silently.
- **The runtime baseline shows defects** (crashes, stuck vehicles, invisible blockers after handback). Record them as
  limitations. A documented negative result still satisfies "runtime baseline recorded". It informs the PHASE-02
  design and the reuse recommendation.
- **The game updates mid-phase.** Record the new version and repeat the affected builds and runs.
- **A lab operation touches anything outside the approved scope.** Stop, inform the owner, follow the TASK-0003
  rollback, and record the incident. Never repair primary data silently.

## 12. Evidence record

None yet. No discovery, audit, build, or runtime run has happened for this phase. Evidence will be recorded as
`RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md) with
[TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md). Read-only discovery results are recorded as dated, verified
commands in the task outputs. The gate decision (Reviewer, date, owner acceptance) will be added here.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
