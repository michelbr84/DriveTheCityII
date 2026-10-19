# Game Time and Pause

| Field | Value |
|---|---|
| ID | SPEC-TIME |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-03 (speed policy, pause, focus, bounded motion), PHASE-05 (return to Building), PHASE-06 (mission timers), PHASE-09 (company settlement) |
| Responsible role | Coordinator (maintainer); Reviewer checks changes to settlement or clock rules (they affect save safety) |
| Related requirements | REQ-TIME-001, REQ-TIME-002, REQ-TIME-003, REQ-TIME-004, REQ-CTRL-004, REQ-COMPANY-003, REQ-CAREER-004, REQ-SAVE-001, REQ-SAVE-005 |
| Related documents | [SPEC-CONTROL](CONTROL_AUTHORITY.md), [SPEC-CONTACT](WORLD_CONTACT.md), [SPEC-SAVE](SAVE_FORMAT.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), [ADR-0004](decisions/ADR-0004-physics-strategy.md) |
| Last updated | 2026-10-18 |

> The requirements are **[Owner requirement]**. Clock definitions, numbers, and algorithms are **[Proposal]**. Whether
> the mod can read or set simulation speed, detect pause, or detect focus loss is unverified (OPEN-013, OPEN-102).
> Validation status of everything here: **Not run**.

## 1. Purpose

Define which clock drives what, how simulation speed is changed and restored around character mode, what pause and
focus loss freeze, how per-frame motion stays bounded, and how career timers and company earnings advance so that
pauses, mode switches, and save/load can neither duplicate nor skip money or penalties.

## 2. Scope and non-goals

In scope: clocks, speed policy, pause semantics per overlay, focus loss, bounded motion, mission timers, time-based
settlement, behavior when an older save is loaded.

Not in scope: mission content and deadlines per career ([SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md)), company
balancing ([SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md)), the choice of physics model
([ADR-0004](decisions/ADR-0004-physics-strategy.md)).

## 3. Clocks

| Clock | Definition | Advances when | Used for | Persisted |
|---|---|---|---|---|
| Simulation time | The host's in-game time, read through CityBridge (OPEN-013) | The host simulation runs; scaled by host speed; frozen when the host is paused | Native world, in-game time of day shown in the HUD, company periods if OPEN-104 chooses it | By the host save; the mod stores the value it saw at its last save for consistency checks |
| Frame time | Unscaled real time between rendered frames, clamped (section 7) | Every frame | Player motion stepping, camera smoothing, UI animation, timeouts of transitional states | No |
| Career-active time | A mod-maintained monotonic counter per career profile | In character mode only; not `Paused`; no full-screen menu; window focused; not `Loading`/`Unloading` | Mission deadlines and timers, rental durations, challenge timing | Yes, with the career snapshot |

Every ledger entry records both the career-active instant and the simulation time (if readable) for auditing.

## 4. Simulation speed policy (REQ-TIME-001, REQ-CTRL-004)

1. **Record.** On TR-01 (entering character mode) TimePolicy records the current speed setting, including "paused".
2. **Normal speed during active exploration [Proposal].** After the avatar is in place, the speed is set to the
   host's normal speed. While in character mode, the only speed changes the mod accepts are pause and unpause; any
   other change it observes is reverted to normal with a short notice.
3. **Restore.** On every path back to `Building` (TR-02, TR-09, TR-12, TR-13 except when a new city is loading), the
   recorded value is restored exactly once (INV-CTRL-09).
4. **Failure.** If reading or setting speed is not possible (OPEN-013), character mode still works with whatever
   speed the host runs, and the HUD warns that speed control is unavailable. A failed restore is logged and the
   player is told which speed was expected.
5. Whether native traffic at normal speed looks consistent with real-time player motion is part of the PHASE-03
   measurement (OPEN-013).

## 5. Pause semantics per overlay (REQ-TIME-002)

| Overlay or condition | Simulation time | Player motion and transitions | Career-active time and mission timers | Input | Camera |
|---|---|---|---|---|---|
| `Paused` (host pause or mod pause) | Frozen | Frozen; authority retained, no writes | Frozen | Held inputs cleared; only unpause and menu | Static (owner retained) |
| `Menu`: host pause menu | Frozen (treated as `Paused`) | Frozen | Frozen | UI | Static |
| `Menu`: mod full-screen career menu (garage, ledger, company) **[Proposal]** | Paused by the mod if possible | Frozen | Frozen | UI | Static |
| `Menu`: mod HUD panel or prompt | Runs | Runs | Runs | Shared with the current context | Unchanged |
| Window focus lost | Runs unless auto-pause applies | Inputs released; vehicle brakes to a stop; avatar stops | Frozen | Cleared | Unchanged |
| `Loading` / `Unloading` | Not applicable | Torn down | Frozen; value at the last save is authoritative | None | Host |
| `Building` | Host speed | No mod motion | Frozen | Host | Host |

## 6. Focus loss (REQ-TIME-002)

- On focus loss: every held action is treated as released; the vehicle applies brake to a stop and then the parking
  brake; the avatar stops; the career-active clock freezes even if the host keeps simulating.
- Auto-pause on focus loss is a setting, on by default **[Proposal]**, effective only if the mod can pause the host
  (OPEN-013).
- On refocus: actions require a fresh press; nothing resumes from a key state captured before focus was lost.
- Detection of focus loss and of host menus is unverified (OPEN-102).

## 7. Bounded per-frame motion (REQ-TIME-003)

**[Proposal]**, starting values to be tuned in PHASE-03 and shared with the [ADR-0004](decisions/ADR-0004-physics-strategy.md)
experiment:

| Parameter | Proposed value | Purpose |
|---|---|---|
| Frame delta clamp | 1/30 s (about 33 ms) | Time beyond this in a single frame is discarded, so a stall slows motion instead of producing a jump |
| Fixed motion substep | 1/120 s | Same inputs on the same surface produce the same motion at any frame rate (REQ-DRIVE-002) |
| Maximum substeps per frame | 4 (= clamp / substep) | Prevents a cost spiral when frames are slow (RISK-103) |
| Stall threshold | 0.25 s | A frame longer than this is a discontinuity: discard it, re-validate ground and level before the next step |
| Contact test | Swept shape per substep, not point samples | Prevents tunneling through thin geometry (at 40 m/s a substep covers about 0.33 m) |

Rules: both the avatar and the vehicle controllers use the same stepping policy; the camera smooths with frame time
but never moves the controlled entity; any substep whose contact answer is `Unknown` holds the previous valid pose.
How mod updates are ordered relative to native movement and rendering is unverified (OPEN-107). The candidate base mod
clamps its control delta to a range of 1/60 to 1/30 s **[Source-inspected, pending re-check in TASK-0002]**; this
informs the starting values, nothing more.

## 8. Mission timers and deadlines

- Deadlines and timers are stored as **remaining career-active time**, not as absolute instants of another clock, so
  pause, focus loss, menus, `Building`, and save/load cannot consume them.
- While the player is in `Building`, active missions are suspended: timers frozen, mission objects removed, records
  kept. On return they are revalidated ([SPEC-CITY](CITY_INTEGRATION.md)) and resume or are invalidated without fault.
- A save during an active mission persists the mission record with its remaining time; after load it resumes
  suspended until the player re-enters character mode, or resolves safely if revalidation fails (REQ-SAVE-005).
- Whether deadlines should instead use simulation time for flavor (for example "deliver by 14:00") is part of
  OPEN-104.

## 9. Time-based settlement (REQ-TIME-004, REQ-COMPANY-003)

Applies to company income and costs, and to any other periodic charge (for example rental by duration).

**Settled-until checkpoint.** Each periodic account stores `settledUntil`, expressed in the units of its clock
(simulation time or career-active time; OPEN-104). Periods have a fixed length per account.

```text
// [Proposal] illustrative algorithm, not code
onSettlementTrigger(now):
  while settledUntil + periodLength <= now and batchCount < maxPerFrame:
    p    = periodIndexStartingAt(settledUntil)
    txId = TransactionId(accountKind, accountId, p)        // deterministic
    ledger.append(entry(txId, computePeriod(account, p)))  // DuplicateRejected counts as success
    settledUntil = settledUntil + periodLength
  // remaining periods continue next frame: amortized, never dropped
```

- **Triggers:** a period boundary is crossed; after a successful load and validation; on entering character mode;
  immediately before the career snapshot is taken for a save.
- **Atomicity:** the ledger entries and the new `settledUntil` are part of the same snapshot. If the game stops between
  appending and advancing, the reload recomputes the same period, produces the same TransactionId, the ledger rejects
  it, and the checkpoint advances. Nothing is paid twice.
- **No skipping:** catch-up is processed in batches, never discarded. Exploit limits (for example running the city fast
  in `Building`) are a design decision for [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md) and OPEN-104; they must be
  explicit rules shown to the player, not silent skips.
- **Pause and mode switches** change only whether the chosen clock advances; settlement itself is stateless apart from
  the checkpoint.
- **Clock regression:** if `now < settledUntil` (for example, inconsistent data), no settlement runs, `ClockRegression`
  is logged, and the save identity check in [SPEC-SAVE](SAVE_FORMAT.md) is consulted.

## 10. Loading an older save

Example: save A (company `settledUntil` = day 10, wallet 1,000) and later save B (day 20, wallet 1,800, made from A).
Loading A restores A's career snapshot: `settledUntil` = day 10 and A's ledger. Simulation time comes from A. As play
continues from A, periods 11 onward settle again **in A's timeline** with TransactionIds that A's ledger has never
seen. This is correct: B's ledger is a different revision and is never consulted (REQ-SAVE-001). Career-active time
also restarts from A's persisted value, so A's mission timers are unaffected by anything that happened in B.

## 11. Invariants

| ID | Invariant | Test |
|---|---|---|
| INV-TIME-01 | The recorded speed is restored exactly once per character-mode session. | TEST-TIME-001 |
| INV-TIME-02 | No player motion, mission timer, or career-active time advances while `Paused`. | TEST-TIME-002 |
| INV-TIME-03 | No single frame moves a controlled entity farther than the clamp allows. | TEST-TIME-003 |
| INV-TIME-04 | Each period of each account settles exactly once per save timeline. | TEST-TIME-004, TEST-COMPANY-003 |
| INV-TIME-05 | Focus loss leaves no held input active after refocus. | TEST-TIME-002 |

## 12. Failure handling

- Speed control unavailable: warn and continue (section 4).
- Settlement fault (exception, invariant violation): settlement pauses for that account, Career is flagged, the last
  valid copy is protected ([SPEC-RECOVERY](ERROR_RECOVERY.md)).
- Very long stall (for example after alt-tab): treated as a discontinuity (section 7).

## 13. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| Entering and leaving character mode records and restores speed. | TEST-TIME-001, TEST-CTRL-004 | Not run |
| Pause freezes motion, timers, and career clocks; focus loss clears inputs. | TEST-TIME-002 | Not run |
| Motion stays bounded under frame caps and injected stalls. | TEST-TIME-003 | Not run |
| Repeated pause, mode switches, and save/load never duplicate or skip timed income. | TEST-TIME-004, TEST-COMPANY-003 | Not run |

## 14. Open questions and risks

- OPEN-013 (speed and pause control), OPEN-102 (focus and host menus), OPEN-104 (clock basis for timers and company
  settlement; owner decision with [SPEC-COMPANY](../gameplay/COMPANIES_AND_FLEET.md)), OPEN-107 (update ordering).
- RISK-022 (time inconsistencies), RISK-006 (duplicated money), RISK-103 (substep cost spiral).

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
