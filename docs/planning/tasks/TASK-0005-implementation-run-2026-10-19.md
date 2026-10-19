# TASK-0005 — Implementation run of 2026-10-19 (private playable build)

| Field | Value |
|---|---|
| ID | TASK-0005 |
| Task status | In review (work done and evidenced; awaiting the project owner) |
| Task type | Implementation, integration, in-game validation, private packaging |
| Phases | PHASE-01 to PHASE-12; PHASE-13 limited to a **private** package (no publishing, per the owner brief) |
| Responsible role | Implementer and QA: Claude Code agent; Reviewer: project owner (pending) |
| Authorization | Owner brief "Drive the City II — autonomous implementation, Melty MCP, and private playable delivery" of 2026-10-19: local Git, user-scoped installs, builds, local deploy with manifest and rollback, disposable test sessions, Melty for permitted read-only checks. No publishing, no uploads, no paid resources, no primary saves. |
| Created | 2026-10-19 |
| Last updated | 2026-10-19 |

## 1. Objective

Turn the documented design into a usable private v1.0 build. Implement and validate it in playable increments,
without publishing anything.

## 2. Scope delivered

| Area | Delivered | Evidence |
|---|---|---|
| Foundation (PHASE-01) | Native mod, bounded log with build stamp, co-load guard, save envelope with CRC32 and sidecar backups, quarantine, settings page | RUN-0001; unit tests; WORKLOG |
| Driving, physics, on foot, transitions (PHASE-02 to PHASE-05) | Vehicle controller, ground solver, walking and running avatar, enter and exit, cameras, recovery, traffic presence | RUN-0002 (M1, real keys); transition cycles 33/33 |
| Taxi MVP (PHASE-06) | Offers, pickup, drop-off, passengers, patience, settlement, persistence | RUN-0003 §1–2 |
| Progression and garage (PHASE-07) | XP and levels per career, reputation, unlocks, rental, purchase, sale, fuel, wear, repair, upgrades, roadside help, tow | Unit tests; RUN-0003 §3 |
| Delivery, freight, bus (PHASE-08) | Multi-stop deliveries, rigid-truck freight with the shipper's truck, bus line runs with the operator's bus | Unit tests; RUN-0003 §3 |
| Home, service points, company (PHASE-09) | Home and garage base, service points (CS2 has no fuel stations), managed company with simulated operations | Unit tests; RUN-0003 §3 |
| Challenges and city change (PHASE-10) | Checkpoint, time-trial and parking challenges with records; revalidation when places, roads or lines change | Unit tests (city change); RUN-0003 §3 |
| Presentation and accessibility (PHASE-11) | HUD, panels, onboarding, EN/PT-BR, HUD scale, high contrast, remappable keys, vehicle sound | Screenshots; MORNING_REPORT |
| Validation (PHASE-12) | Campaign scripts, real-time soak, mod-less load check | MORNING_REPORT; WORKLOG |
| Private package (PHASE-13, private part only) | `tools\build.ps1` package, `tools\deploy.ps1` install, rollback and uninstall | BUILD_AND_RUN §0 |

## 3. Not done or not verified

See [MORNING_REPORT](../../../MORNING_REPORT.md) §4 and [KNOWN_ISSUES](../../quality/KNOWN_ISSUES.md). In short:

- no gamepad test (no device);
- enter and exit are a slide, not an animation;
- no human judgement of fun, feel, or sound quality;
- no publishing.

## 4. Handoff

The owner reviews START_HERE, MORNING_REPORT, and the evidence. The decisions recorded for the owner are in the
MORNING_REPORT "Actions for you" section.
