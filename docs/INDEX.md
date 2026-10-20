# Documentation Index

| Field | Value |
|---|---|
| ID | DOC-INDEX |
| Document status | Draft |
| Scope | Every active document in the project |
| Responsible role | Coordinator (maintainer) |
| Related documents | [DOCUMENTATION_RULES](DOCUMENTATION_RULES.md), [READING_GUIDE](agents/READING_GUIDE.md), [PROJECT_STATUS](PROJECT_STATUS.md) |
| Last updated | 2026-10-20 (LICENSE, START_HERE, MORNING_REPORT added; chat.md moved to archive/) |

This index is the entry point to the documentation. It lists every active document, what it is for, and when to read
it. Statuses shown here are document statuses at the time of the baseline. The file's own metadata block is
authoritative. Runtime truth lives only in [PROJECT_STATUS](PROJECT_STATUS.md).

**Reading priority**

- **P1** — read in every first session, in the order shown in [READING_GUIDE](agents/READING_GUIDE.md).
- **P2** — read when your task touches this area. Task files list them explicitly.
- **P3** — reference material. Consult it when needed.
- **T** — template. Copy it; do not read it for facts.

Excluded from the default reading path:

- [archive/](archive/README.md): superseded material. No superseded document is archived yet.
- The historical input [chat.md](archive/chat.md) (moved from the root on 2026-10-20): the original Portuguese planning conversation, kept unchanged
  ([SRC-006](research/SOURCES.md)).

## Root documents

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [README.md](../README.md) | DOC-README | Project entry point: what the mod does, current status, build and install. | P1 |
| [LICENSE](../LICENSE) | — | MIT License (owner decision, 2026-10-20). | P3 |
| [START_HERE.md](../START_HERE.md) | DOC-START-HERE | Build, install, controls, and a first walkthrough. | P2 |
| [MORNING_REPORT.md](../MORNING_REPORT.md) | DOC-MORNING-REPORT | Latest status report: what is verified in game and what is not. | P2 |
| [AGENTS.md](../AGENTS.md) | DOC-AGENTS | Common rules for every agent and contributor: protected areas, permissions, evidence. | P1 |
| [CLAUDE.md](../CLAUDE.md) | DOC-CLAUDE | Claude Code entry point; routes to AGENTS.md. | P1 (Claude) |
| [ROADMAP.md](../ROADMAP.md) | DOC-ROADMAP | Official phase order, dependencies, milestones, gate policy. | P1 |
| [CHANGELOG.md](../CHANGELOG.md) | DOC-CHANGELOG | Changes that were actually made. | P3 |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | DOC-CONTRIBUTING | How to contribute, review requirements, Git and pull-request conventions. | P2 |
| [SECURITY.md](../SECURITY.md) | DOC-SECURITY | Data protection, sensitive operations, supply-chain caution, how to report a vulnerability. | P2 |
| [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) | DOC-NOTICES | Third-party notices: none in the mod; test-only tools; reference-only material. | P3 |

## Navigation and status (`docs/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [INDEX.md](INDEX.md) | DOC-INDEX | This index. | P1 |
| [PROJECT_STATUS.md](PROJECT_STATUS.md) | DOC-STATUS | Demonstrated state: documentation, source review, environment, runtime, blockers, next work. | P1 |
| [DOCUMENTATION_RULES.md](DOCUMENTATION_RULES.md) | DOC-RULES | Language, sources of truth, statuses, claim labels, IDs, conflicts, terminology. | P1 |

## Product (`docs/product/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [VISION.md](product/VISION.md) | DOC-VISION | Why the product exists, pillars, what it is and is not. | P1 |
| [PRD.md](product/PRD.md) | DOC-PRD | v1.0 requirements with stable `REQ-*` IDs; exclusions; success criteria. | P1 |
| [GAME_DESIGN.md](product/GAME_DESIGN.md) | DOC-GDD | Core loops, original design choices, provisional balancing principles. | P2 |
| [PLAYER_JOURNEYS.md](product/PLAYER_JOURNEYS.md) | DOC-JOURNEYS | End-to-end journeys, including failure and recovery paths. | P2 |
| [EXPANSIONS.md](product/EXPANSIONS.md) | DOC-EXPANSIONS | Post-1.0 ideas kept out of v1.0 scope. | P3 |

## Research (`docs/research/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [SOURCES.md](research/SOURCES.md) | DOC-SOURCES | Every external source, its access status, revision, supported claims, uncertainty. | P2 |
| [FEASIBILITY_MATRIX.md](research/FEASIBILITY_MATRIX.md) | DOC-FEASIBILITY | Evidence level reached for each needed capability. | P2 |
| [BASE_MOD_AUDIT.md](research/BASE_MOD_AUDIT.md) | DOC-BASE-AUDIT | Beta Test Driving Mod audit: license, dependencies, inspection plan, reuse candidates. | P2 |
| [MOTOR_TOWN_REFERENCE.md](research/MOTOR_TOWN_REFERENCE.md) | DOC-MOTORTOWN | Observable Motor Town loops mapped to original CS2 adaptations. | P3 |
| [CS2_INTEGRATION_FINDINGS.md](research/CS2_INTEGRATION_FINDINGS.md) | DOC-CS2-FINDINGS | What is documented about CS2 modding and what remains to verify. | P2 |
| [MELTY_EVALUATION.md](research/MELTY_EVALUATION.md) | DOC-MELTY | Melty facts, evaluation questions, fallback without Melty. | P3 |

## Engineering (`docs/engineering/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [WINDOWS_SETUP.md](engineering/WINDOWS_SETUP.md) | DOC-WINDOWS-SETUP | Observed workspace facts, discovery procedure, safe testing prerequisites. | P2 |
| [TOOLCHAIN_MATRIX.md](engineering/TOOLCHAIN_MATRIX.md) | DOC-TOOLCHAIN | Single home of confirmed tool versions (none confirmed for the mod yet). | P2 |
| [LOCAL_ENVIRONMENT.example.md](engineering/LOCAL_ENVIRONMENT.example.md) | DOC-LOCAL-ENV | Template for the git-ignored local environment file. | T |
| [BUILD_AND_RUN.md](engineering/BUILD_AND_RUN.md) | DOC-BUILD-RUN | Intended build and run procedure (unvalidated). | P2 |
| [CODING_STANDARDS.md](engineering/CODING_STANDARDS.md) | DOC-CODING | Code conventions and module-boundary rules for future code. | P2 |
| [DEPENDENCIES.md](engineering/DEPENDENCIES.md) | DOC-DEPENDENCIES | Dependency classes, responsibilities, pinning policy. | P3 |
| [UPSTREAM_STRATEGY.md](engineering/UPSTREAM_STRATEGY.md) | DOC-UPSTREAM | How the base mod would be pinned, forked or re-implemented, and reviewed. | P3 |
| [DEBUGGING.md](engineering/DEBUGGING.md) | DOC-DEBUGGING | Logging, diagnostics bundle, sanitization, tool availability. | P2 |

## Architecture (`docs/architecture/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [ARCHITECTURE.md](architecture/ARCHITECTURE.md) | SPEC-ARCH | System context, modules, core-loop walkthrough, principles. | P2 |
| [MODULE_CONTRACTS.md](architecture/MODULE_CONTRACTS.md) | SPEC-MODULES | Module responsibilities, write ownership, dependency direction, ports. | P2 |
| [CONTROL_AUTHORITY.md](architecture/CONTROL_AUTHORITY.md) | SPEC-CONTROL | **Critical.** Player states, overlays, movement and camera ownership. | P2 |
| [ENTITY_LIFECYCLE.md](architecture/ENTITY_LIFECYCLE.md) | SPEC-ENTITY | **Critical.** Entity categories, handback, spawning, cleanup. | P2 |
| [GAME_TIME_AND_PAUSE.md](architecture/GAME_TIME_AND_PAUSE.md) | SPEC-TIME | Simulation speed, pause, focus, timers, time-based settlement. | P2 |
| [WORLD_CONTACT.md](architecture/WORLD_CONTACT.md) | SPEC-CONTACT | Surfaces, levels, collisions, bounded proxies, traffic contact. | P2 |
| [CITY_INTEGRATION.md](architecture/CITY_INTEGRATION.md) | SPEC-CITY | Read-before-write boundary, access points, revalidation after edits. | P2 |
| [SAVE_FORMAT.md](architecture/SAVE_FORMAT.md) | SPEC-SAVE | **Critical.** Save identity, schema, storage options, write and load protocols. | P2 |
| [ERROR_RECOVERY.md](architecture/ERROR_RECOVERY.md) | SPEC-RECOVERY | Failure containment, Recovery state, safe degradation. | P2 |
| [PERFORMANCE_BUDGET.md](architecture/PERFORMANCE_BUDGET.md) | SPEC-PERF | Metrics, baseline-first budgets, regression trigger. | P3 |
| [decisions/README.md](architecture/decisions/README.md) | DOC-ADR-INDEX | ADR index and process. | P2 |
| [ADR-0001](architecture/decisions/ADR-0001-host-game-and-runtime.md) | ADR-0001 | CS2 as the sole host and runtime. | P2 |
| [ADR-0002](architecture/decisions/ADR-0002-control-authority.md) | ADR-0002 | Single movement authority and explicit state machine. | P2 |
| [ADR-0003](architecture/decisions/ADR-0003-career-save-isolation.md) | ADR-0003 | Career and save isolation; storage mechanism pending. | P2 |
| [ADR-0004](architecture/decisions/ADR-0004-physics-strategy.md) | ADR-0004 | Physics strategy; decision pending the PHASE-03 experiment. | P2 |
| [ADR-0005](architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md) | ADR-0005 | Mod loading and distribution route: native code mod, no BepInEx, Melty not a channel (Proposed). | P2 |

## Gameplay (`docs/gameplay/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [PLAYER_AND_CAMERAS.md](gameplay/PLAYER_AND_CAMERAS.md) | SPEC-PLAYER | Avatar, walking and running, cameras. | P2 |
| [VEHICLE_DRIVING.md](gameplay/VEHICLE_DRIVING.md) | SPEC-DRIVING | Driving controls, handling, vehicle classes, vehicle cameras. | P2 |
| [WALK_DRIVE_TRANSITIONS.md](gameplay/WALK_DRIVE_TRANSITIONS.md) | SPEC-TRANSITION | Entering, exiting, parking, blocked exits. | P2 |
| [MISSION_FRAMEWORK.md](gameplay/MISSION_FRAMEWORK.md) | SPEC-MISSION | Shared mission lifecycle, validation, settlement. | P2 |
| [PROGRESSION_AND_ECONOMY.md](gameplay/PROGRESSION_AND_ECONOMY.md) | SPEC-ECON | Wallet, ledger, XP, reputation, unlocks, insolvency recovery. | P2 |
| [VEHICLE_OWNERSHIP_AND_GARAGE.md](gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md) | SPEC-GARAGE | Rental, purchase, sale, fuel, maintenance, upgrades, immobilization. | P2 |
| [HOME_AND_SERVICE_POINTS.md](gameplay/HOME_AND_SERVICE_POINTS.md) | SPEC-HOME | Home base, garage location, service points, fallbacks. | P2 |
| [COMPANIES_AND_FLEET.md](gameplay/COMPANIES_AND_FLEET.md) | SPEC-COMPANY | Basic company as a labeled managerial simulation. | P2 |
| [CHALLENGES_AND_DYNAMIC_CITY.md](gameplay/CHALLENGES_AND_DYNAMIC_CITY.md) | SPEC-CHALLENGE | Checkpoints, time trials, parking, demand-informed jobs, city edits. | P2 |
| [careers/TAXI.md](gameplay/careers/TAXI.md) | SPEC-TAXI | Taxi career (the PHASE-06 MVP loop). | P2 |
| [careers/DELIVERIES.md](gameplay/careers/DELIVERIES.md) | SPEC-DELIVERY | Urban delivery career. | P2 |
| [careers/FREIGHT.md](gameplay/careers/FREIGHT.md) | SPEC-FREIGHT | Rigid-truck freight career. | P2 |
| [careers/BUS.md](gameplay/careers/BUS.md) | SPEC-BUS | Bus career on city lines with career passengers. | P2 |

## Experience (`docs/experience/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [UI_AND_HUD.md](experience/UI_AND_HUD.md) | SPEC-UI | HUD, menus, map, feedback, honesty labels. | P2 |
| [CONTROLS_AND_ACCESSIBILITY.md](experience/CONTROLS_AND_ACCESSIBILITY.md) | SPEC-INPUT | Actions, proposed bindings, remapping, gamepad, accessibility. | P2 |
| [ART_AND_ANIMATION.md](experience/ART_AND_ANIMATION.md) | SPEC-ART | Asset policy, animations, placeholders, cockpit policy. | P3 |
| [AUDIO.md](experience/AUDIO.md) | SPEC-AUDIO | Sound design scope and sourcing. | P3 |
| [LOCALIZATION.md](experience/LOCALIZATION.md) | SPEC-L10N | English and PT-BR interface localization. | P3 |
| [ONBOARDING.md](experience/ONBOARDING.md) | SPEC-ONBOARD | Tutorial and first-job flow. | P3 |

## Planning (`docs/planning/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [BACKLOG.md](planning/BACKLOG.md) | DOC-BACKLOG | Index of task files (not a second tracker). | P1 |
| [RISKS_AND_OPEN_QUESTIONS.md](planning/RISKS_AND_OPEN_QUESTIONS.md) | DOC-RISKS | Risks, open questions, recorded conflicts. | P1 |
| [TRACEABILITY.md](planning/TRACEABILITY.md) | DOC-TRACE | Requirement → specification → phase → test → evidence. | P2 |
| [phases/README.md](planning/phases/README.md) | DOC-PHASES | Phase index and gate process. | P2 |
| [PHASE-00](planning/phases/PHASE-00-audit-and-windows.md) | PHASE-00 | Audit and Windows environment. | P1 (current) |
| [PHASE-01](planning/phases/PHASE-01-foundation-and-save-safety.md) | PHASE-01 | Foundation and save safety. | P2 |
| [PHASE-02](planning/phases/PHASE-02-basic-driving.md) | PHASE-02 | Basic driving. | P3 |
| [PHASE-03](planning/phases/PHASE-03-surfaces-collisions-and-physics.md) | PHASE-03 | Surfaces, collisions, and physics. | P3 |
| [PHASE-04](planning/phases/PHASE-04-on-foot-character.md) | PHASE-04 | On-foot character. | P3 |
| [PHASE-05](planning/phases/PHASE-05-walk-drive-integration.md) | PHASE-05 | Walk–drive integration. | P3 |
| [PHASE-06](planning/phases/PHASE-06-taxi-career-mvp.md) | PHASE-06 | Taxi career MVP. | P3 |
| [PHASE-07](planning/phases/PHASE-07-progression-and-garage.md) | PHASE-07 | Progression and garage. | P3 |
| [PHASE-08](planning/phases/PHASE-08-delivery-freight-and-bus.md) | PHASE-08 | Deliveries, freight, and bus. | P3 |
| [PHASE-09](planning/phases/PHASE-09-home-services-and-company.md) | PHASE-09 | Home, services, and company. | P3 |
| [PHASE-10](planning/phases/PHASE-10-challenges-and-dynamic-city.md) | PHASE-10 | Challenges and dynamic city. | P3 |
| [PHASE-11](planning/phases/PHASE-11-presentation-and-accessibility.md) | PHASE-11 | Presentation and accessibility. | P3 |
| [PHASE-12](planning/phases/PHASE-12-beta-and-validation.md) | PHASE-12 | Beta and validation. | P3 |
| [PHASE-13](planning/phases/PHASE-13-release-and-maintenance.md) | PHASE-13 | Release and maintenance. | P3 |
| [tasks/README.md](planning/tasks/README.md) | DOC-TASKS | Task lifecycle and conventions. | P2 |
| [TASK-0000](planning/tasks/TASK-0000-documentation-bootstrap.md) | TASK-0000 | This documentation baseline. | P3 |
| [TASK-0001](planning/tasks/TASK-0001-windows-environment-audit.md) | TASK-0001 | Windows environment discovery. | P2 |
| [TASK-0002](planning/tasks/TASK-0002-base-mod-source-audit.md) | TASK-0002 | Base-mod source, license, and dependency audit. | P2 |
| [TASK-0003](planning/tasks/TASK-0003-isolated-baseline-validation.md) | TASK-0003 | Isolated baseline build and runtime validation (next; needs authorization). | P1 (next) |
| [TASK-0004](planning/tasks/TASK-0004-native-mod-skeleton.md) | TASK-0004 | Native mod skeleton: the first implementation task (Planned). | P2 |

## Quality (`docs/quality/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [TEST_STRATEGY.md](quality/TEST_STRATEGY.md) | DOC-TEST-STRATEGY | Test layers, lab-city policy, evidence approach. | P2 |
| [QA_CHECKLIST.md](quality/QA_CHECKLIST.md) | DOC-QA-CHECKLIST | Checklists per change type and per phase gate. | P2 |
| [TEST_SCENARIOS.md](quality/TEST_SCENARIOS.md) | DOC-TEST-SCENARIOS | Test definitions for every requirement (except save tests). | P2 |
| [SAVE_SAFETY_TESTS.md](quality/SAVE_SAFETY_TESTS.md) | DOC-SAVE-TESTS | Save-safety test definitions. | P2 |
| [BENCHMARK_PROTOCOL.md](quality/BENCHMARK_PROTOCOL.md) | DOC-BENCHMARK | Repeatable performance measurement protocol. | P3 |
| [COMPATIBILITY_MATRIX.md](quality/COMPATIBILITY_MATRIX.md) | DOC-COMPAT-MATRIX | Tested combinations (none yet). | P3 |
| [KNOWN_ISSUES.md](quality/KNOWN_ISSUES.md) | DOC-KNOWN-ISSUES | Observed defects (none; no runtime assessment exists). | P3 |
| [DOCUMENTATION_VALIDATION_REPORT.md](quality/DOCUMENTATION_VALIDATION_REPORT.md) | DOC-DOCVAL | Checks performed on this documentation baseline. | P3 |
| [evidence/README.md](quality/evidence/README.md) | DOC-EVIDENCE | How real execution evidence will be recorded. | P2 |

## Agent operations (`docs/agents/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [READING_GUIDE.md](agents/READING_GUIDE.md) | DOC-READING | Explicit reading sequences per session and task type. | P1 |
| [ROLES_AND_OWNERSHIP.md](agents/ROLES_AND_OWNERSHIP.md) | DOC-ROLES | Coordinator, Implementer, Reviewer, QA, project owner; critical-area ownership. | P2 |
| [WORKFLOW_AND_HANDOFF.md](agents/WORKFLOW_AND_HANDOFF.md) | DOC-WORKFLOW | Batch workflow and handoff contents. | P2 |
| [DEFINITION_OF_READY.md](agents/DEFINITION_OF_READY.md) | DOC-DOR | Readiness criteria per task type. | P2 |
| [DEFINITION_OF_DONE.md](agents/DEFINITION_OF_DONE.md) | DOC-DOD | Completion criteria per work type. | P2 |
| [TOOLS_AND_PERMISSIONS.md](agents/TOOLS_AND_PERMISSIONS.md) | DOC-TOOLS | Observed tools and permission tiers. | P1 |
| [HANDOFF_TO_PHASE_00.md](agents/HANDOFF_TO_PHASE_00.md) | DOC-HANDOFF-P00 | Handoff from the documentation baseline to PHASE-00. | P1 (next) |

## Release (`docs/release/`) — pre-release drafts

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [PACKAGING_AND_PUBLISHING.md](release/PACKAGING_AND_PUBLISHING.md) | DOC-PACKAGING | Intended packaging, channels to validate, checklist. | P3 |
| [INSTALL_UPGRADE_UNINSTALL.md](release/INSTALL_UPGRADE_UNINSTALL.md) | DOC-INSTALL | Intended install, update, and clean-exit flows (untested). | P3 |
| [COMPATIBILITY_POLICY.md](release/COMPATIBILITY_POLICY.md) | DOC-COMPAT-POLICY | Support levels and how combinations become supported. | P3 |
| [POST_RELEASE_MAINTENANCE.md](release/POST_RELEASE_MAINTENANCE.md) | DOC-MAINTENANCE | Responding to game updates, hotfixes, migration. | P3 |

## Legal (`docs/legal/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [REUSE_AND_LICENSE_AUDIT.md](legal/REUSE_AND_LICENSE_AUDIT.md) | DOC-LICENSE-AUDIT | Licenses and obligations of candidate components; project license pending. | P2 |
| [ASSET_PROVENANCE.md](legal/ASSET_PROVENANCE.md) | DOC-ASSET-PROVENANCE | Provenance register for assets (none incorporated). | P3 |

## User guides (`docs/user/`) — pre-release drafts

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [QUICK_START.md](user/QUICK_START.md) | DOC-QUICK-START | Intended first-play flow; not yet installable. | P3 |
| [TROUBLESHOOTING.md](user/TROUBLESHOOTING.md) | DOC-TROUBLESHOOTING | Planned recovery actions and diagnostics. | P3 |

## Templates (`docs/templates/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [DOCUMENT_TEMPLATE.md](templates/DOCUMENT_TEMPLATE.md) | TPL-DOCUMENT | New specification or document. | T |
| [PHASE_TEMPLATE.md](templates/PHASE_TEMPLATE.md) | TPL-PHASE | Phase document structure. | T |
| [TASK_TEMPLATE.md](templates/TASK_TEMPLATE.md) | TPL-TASK | Task file structure. | T |
| [ADR_TEMPLATE.md](templates/ADR_TEMPLATE.md) | TPL-ADR | Architecture decision record. | T |
| [TEST_REPORT_TEMPLATE.md](templates/TEST_REPORT_TEMPLATE.md) | TPL-TEST-REPORT | Evidence run report (`RUN-NNNN`). | T |
| [HANDOFF_TEMPLATE.md](templates/HANDOFF_TEMPLATE.md) | TPL-HANDOFF | Session or task handoff. | T |

## Archive (`docs/archive/`)

| Document | ID | Purpose | Priority |
|---|---|---|---|
| [archive/README.md](archive/README.md) | DOC-ARCHIVE | When and how superseded material is archived. Nothing is archived yet. | P3 |
