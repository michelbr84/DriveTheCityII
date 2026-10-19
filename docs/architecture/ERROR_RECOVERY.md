# Error Containment and Recovery

| Field | Value |
|---|---|
| ID | SPEC-RECOVERY |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 (containment, persistence recovery, logging), PHASE-04 (avatar Recovery), PHASE-05 (vehicle and transition recovery), PHASE-07 (immobilization), PHASE-09 and PHASE-10 (inaccessible services) |
| Responsible role | Coordinator (maintainer); Reviewer checks changes to containment policy and to recovery paths touching control, entities, or saves |
| Related requirements | REQ-RECOV-001, REQ-RECOV-002, REQ-GARAGE-006, REQ-HOME-003, REQ-ECON-004, REQ-CAREER-005, REQ-CAREER-006, REQ-TRANS-002, REQ-TRANS-003, REQ-SAVE-004, REQ-DIAG-001 |
| Related documents | [SPEC-CONTROL](CONTROL_AUTHORITY.md), [SPEC-ENTITY](ENTITY_LIFECYCLE.md), [SPEC-SAVE](SAVE_FORMAT.md), [SPEC-CONTACT](WORLD_CONTACT.md), [SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), [DEBUGGING](../engineering/DEBUGGING.md), [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) |
| Last updated | 2026-10-18 |

> Containment, the player `Recovery` state, and "no permanent block" are **[Owner requirement]**. Fault classes,
> thresholds, and search orders are **[Proposal]**. How the host reacts to an exception thrown by mod code is
> unknown (OPEN-105). Validation status: **Not run**.

## 1. Purpose

Every failure the player can reach must have a way out that keeps the city, the save, and the career intact. This
spec defines the fault taxonomy, how faults are contained so that one subsystem cannot crash the game or corrupt a
save, the player-facing `Recovery` state, the recovery hooks for immobilized vehicles and inaccessible services, and
what must be logged.

## 2. Scope and non-goals

In scope: fault classes and severities; the Supervisor's containment and circuit breakers; `Recovery` (REQ-RECOV-001)
and its safe-point search; vehicle immobilization and inaccessible-service hooks; mission invalidation; persistence
recovery; logging.

Not in scope: the gameplay content of recovery options (prices, tow rules, starter jobs), owned by
[SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), and
[SPEC-HOME](../gameplay/HOME_AND_SERVICE_POINTS.md). Observed defects go to [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md);
none exist yet because nothing has run.

## 3. Fault taxonomy

| Class | Examples | Typical source |
|---|---|---|
| F1 Control | Authority conflict, stuck entity, invalid ground, out of bounds, lost camera, stuck input, transition timeout | Player, Vehicles |
| F2 Entity | Referenced entity deleted, registry or census mismatch, orphan found, handback verification failed | CityBridge, Vehicles |
| F3 World contact | No surface, level ambiguity, proxy budget exceeded, penetration detected | WorldContact |
| F4 Career logic | Invariant violation (wallet ≠ ledger sum), duplicate settlement attempt, mission without valid leg | Career |
| F5 City integration | Access point invalid, route unreachable, line or stop removed, home base demolished | CityBridge, Career |
| F6 Persistence | Identity mismatch, newer schema, corrupt payload, migration failure, write failure, disk full | Persistence |
| F7 Host integration | Required capability missing after a game update, unexpected exception from a host call, conflicting mod detected | CityBridge, Supervisor |
| F8 Resource | Log, entity, or proxy budgets exceeded | Any |

| Severity | Meaning | Default action |
|---|---|---|
| Info | Expected condition handled by the normal flow (blocked exit, unaffordable item) | Feedback to the player, no fault count |
| Recoverable | Handled locally (retry, reroute, `Recovery`) | Log; continue |
| Degraded | A module is unreliable for this session | Circuit breaker opens; dependent features disabled; player informed |
| Critical | Control or data safety at risk | Forced `Building` (TR-13); career systems stopped; persistence write-protected; player informed |

## 4. Containment (REQ-RECOV-002)

- **Fault boundary per module update.** The Supervisor ([SPEC-MODULES](MODULE_CONTRACTS.md)) calls each module inside a
  boundary that catches exceptions, classifies them, and prevents them from reaching the host. Whether an exception
  that escapes would crash the game, disable the update, or only be logged is unverified (OPEN-105), so the design
  assumes the worst case: nothing may escape.
- **Circuit breaker.** A module that faults 3 times within 60 s **[Proposal]** is marked Degraded for the session and is
  no longer called; features that depend on it are disabled with a message.
- **Safe state.** If Player, Vehicles, or WorldContact is Degraded, or any Critical fault occurs, the Supervisor forces
  `Building` through the cleanup of [SPEC-CONTROL](CONTROL_AUTHORITY.md) section 7. The city remains fully playable.
- **Data protection.** If Career or Persistence is Degraded, or a Career invariant fails, persistence becomes
  write-protected: the last valid copy is kept, and the current snapshot is quarantined at the next save
  ([SPEC-SAVE](SAVE_FORMAT.md) section 7).
- **No loops.** Recovery and forced `Building` are rate-limited: three `Recovery` entries within 60 s escalate to forced
  `Building` (TR-12); a second Critical fault in the same session keeps character mode disabled until the next load,
  with an explanation (RISK-102).
- **Capability loss (F7).** A missing host capability disables only the features that need it and logs the game
  version; it never blocks `Building`.

## 5. Player `Recovery` state (REQ-RECOV-001)

Entered by player request (from stable states) or automatically (stuck for 3 s with input, invalid or `Unknown`
ground for 1 s, out of bounds, authority conflict, transition failure without fallback). Movement writes stop, input
is limited to confirm and menu, and the camera holds or fades.

**Safe-point search order [Proposal].** Each candidate is validated in the same frame ([SPEC-CONTACT](WORLD_CONTACT.md):
valid ground for the class, correct level, clearance, not inside traffic) before use.

| Order | Avatar | Vehicle |
|---|---|---|
| 1 | Most recent entry of the validated-position history (last 10 positions sampled every 2 s while valid) that is still valid now and at least 2 m from the failure point | Same history, on a lane in the direction of travel |
| 2 | Nearest walkable point (sidewalk, path) in the same level context within 50 m | Nearest lane position at the same level within 100 m |
| 3 | Nearest validated access point within 200 m | Nearest validated parking or curb access point within 300 m |
| 4 | Home base access point (REQ-HOME-001) | Owned vehicle: stowed at the garage; rental: returned to the rental point (no fee for a system-caused recovery) |
| 5 | Rental or job-board service point | — |
| 6 | No candidate: forced `Building` (TR-12) with a message | Same |

Recovery requested by the player is free; repeated player-requested recovery during a timed challenge invalidates
that attempt rather than charging money ([SPEC-CHALLENGE](../gameplay/CHALLENGES_AND_DYNAMIC_CITY.md)).

## 6. Vehicle immobilization and inaccessible services

These hooks guarantee that no repair, part, upgrade, or service can permanently block driving (REQ-GARAGE-006) and that
an invalid home base or service point always has a fallback (REQ-HOME-003). The options offered and their prices
belong to the gameplay specs; this spec defines the guarantees.

| Situation | Detection | Hook raised | Guaranteed fallback |
|---|---|---|---|
| Out of fuel | Fuel reaches zero (Vehicles) | `VehicleImmobilized(fuel)` | Roadside refuel or tow to the nearest reachable fuel service point; always also the basic rental path (REQ-GARAGE-001) |
| Breakdown or excessive wear | Wear threshold (Career) | `VehicleImmobilized(wear)` | Tow to the nearest reachable repair service point; basic rental |
| Required service unavailable | No valid service point of that type (city edits, OPEN-021) | `ServiceUnavailable(type)` | Nearest valid alternative; if none, the service is performed through the menu with an explanation **[Proposal]** |
| Insufficient money for any recovery | Wallet check | `InsolvencyRisk` | Starter job and basic rental always available (REQ-ECON-004); recovery never requires money the player cannot obtain |
| Home base invalid | Revalidation ([SPEC-CITY](CITY_INTEGRATION.md)) | `HomeInvalid` | Notification; temporary spawn at the rental or job-board service point; prompt to choose a new base |
| Vehicle stuck in geometry or traffic | Stuck detector | `Recovery` (section 5) | Section 5 order |

## 7. Mission invalidation

- F5 faults and entity losses that make a mission impossible lead Career to `Invalidated`, which resolves to a no-fault
  cancellation ([SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md)). Its settlement uses a deterministic TransactionId
  like any other terminal outcome, so a reload cannot pay or charge twice.
- Mission objects are removed through the registry; the player is told why and what to do next (REQ-UI-003).
- A mission may never remain impossible indefinitely ([SPEC-CITY](CITY_INTEGRATION.md) section 6).

## 8. Persistence recovery

| Failure | Recovery |
|---|---|
| Write failure (interrupted, disk full) | Keep the previous revision; tell the player; retry at the next save |
| Invalid snapshot at save time | Write protection; quarantine; last valid copy kept |
| Corrupt or missing payload at load | Quarantine; fall back to an ancestor revision with an explicit message; otherwise no career data for this save |
| Newer schema | Refuse read-only; nothing written |
| Migration failure | Quarantine; ancestor fallback |

Full protocols: [SPEC-SAVE](SAVE_FORMAT.md). No recovery path ever loads data from a newer revision.

## 9. Logging requirements (REQ-DIAG-001)

Every fault record contains: real time and simulation time; mod version, build identifier, schema version, and game
version if known; player state and overlays; fault class, severity, module; registry IDs involved (never persisted
host handles); the last 20 state transitions; the action taken (retry, `Recovery`, Degraded, forced `Building`).
Identical faults are rate-limited and counted. Logs are size-bounded and sanitized as specified in
[SPEC-MODULES](MODULE_CONTRACTS.md) section 4.9 and [DEBUGGING](../engineering/DEBUGGING.md).

## 10. Invariants

| ID | Invariant | Test |
|---|---|---|
| INV-REC-01 | No mod exception reaches the host. | TEST-RECOV-002 |
| INV-REC-02 | Every Critical fault ends in `Building` with cleanup completed and the career untouched. | TEST-RECOV-002 |
| INV-REC-03 | `Recovery` places entities only at validated points or falls back to `Building`. | TEST-RECOV-001 |
| INV-REC-04 | Recovery cannot loop: escalation after three entries in 60 s. | TEST-RECOV-001 |
| INV-REC-05 | Every immobilization or unavailable service has an option the player can afford or that is free. | TEST-GARAGE-006, TEST-HOME-003 |

## 11. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| Player-triggered and automatic `Recovery` move the avatar or vehicle to a validated safe point. | TEST-RECOV-001 | Not run |
| Faults injected into each module are contained, logged, and degrade to a safe state without crash or save damage. | TEST-RECOV-002 | Not run |
| Immobilized vehicles and inaccessible services never permanently block driving. | TEST-GARAGE-006 | Not run |
| An invalid home base or service point produces a notification and a fallback. | TEST-HOME-003 | Not run |
| Insolvency never blocks progression. | TEST-ECON-004 | Not run |

## 12. Open questions and risks

- OPEN-105 (host behavior on mod exceptions), OPEN-106 (entity invalidation detection), OPEN-021 (edit detection),
  OPEN-006 (persistence mechanism).
- RISK-102 (recovery or fault loops), RISK-019 (economic softlock), RISK-005 (save corruption).

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
