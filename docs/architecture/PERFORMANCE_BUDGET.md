# Performance Budget

| Field | Value |
|---|---|
| ID | SPEC-PERF |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-03 (first baselines and proposed budgets), PHASE-04 and PHASE-05 (re-measure), PHASE-11 (optional enhancements), PHASE-12 (validation across cities and hardware) |
| Responsible role | QA (measurements); Coordinator (maintainer); project owner accepts budgets |
| Related requirements | REQ-PERF-001, REQ-PERF-002, REQ-WORLD-003, REQ-TIME-003 |
| Related documents | [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md), [SPEC-CONTACT](WORLD_CONTACT.md), [SPEC-TIME](GAME_TIME_AND_PAUSE.md), [SPEC-MODULES](MODULE_CONTRACTS.md), [ADR-0004](decisions/ADR-0004-physics-strategy.md), [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md), [evidence](../quality/evidence/README.md) |
| Last updated | 2026-10-18 |

> **No performance number in this document is a measurement or a promise.** No frame rate is promised. Budgets are
> set only from recorded baselines. The 10% figure is an investigation trigger, not a budget **[Owner requirement]**.
> Validation status: **Not run**; no baseline exists yet.

## 1. Purpose

Define what is measured, how baselines come first, when a regression triggers an investigation, and how budgets will
be decided from evidence, so that street-level play does not quietly degrade the player's city.

## 2. Scope and non-goals

In scope: metrics, baseline configurations, the investigation trigger, the budget decision procedure, design rules
that keep mod cost bounded, optional street-level enhancements.

Not in scope: the step-by-step benchmark procedure, scenarios, hardware sheet, and capture tools, which are defined in
[BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md). Non-goals: fixed frame-rate targets, comparisons with other
mods, optimizing the host game itself.

## 3. Metrics

| Metric | Definition | Why it matters |
|---|---|---|
| Frame time | Per-frame duration: mean, median, 95th and 99th percentile, maximum; count of stalls longer than 0.25 s | Player-visible smoothness; stalls drive the bounded-motion rules ([SPEC-TIME](GAME_TIME_AND_PAUSE.md)) |
| Mod update time | Time spent inside each module's update (instrumented in development builds through Diagnostics) | Attributes cost to modules; distinguishes mod cost from rendering cost |
| Memory | Process memory at start, after 10 minutes, and at the end of a session; managed memory if measurable | Leak detection over sessions of two hours or more (PHASE-12) |
| Simulation speed | In-game time advanced per real minute at the selected speed, compared with the same city without the mod (OPEN-013) | The mod must not slow the city simulation |
| Entity counts | Registry counts by category; host census of mod entities where countable (OPEN-012); native vehicle count for context | Orphans and leaks show up as growth |
| Proxy counts | Active proxies, proxy data memory, proxies created per frame | Verifies the caps of [SPEC-CONTACT](WORLD_CONTACT.md) |
| Revalidation duration | Time from the "enter character mode" command to control, and total amortized revalidation time | City-size scaling (RISK-106) |
| Persistence cost | Snapshot, validation, and write time; payload size; load time | Save hitches; size growth of the ledger |
| Log volume | Bytes per hour at default settings | Bounded logs (REQ-DIAG-001) |

## 4. Baseline first

Every comparison is against a recorded baseline on the **same** hardware, game version, settings, lab city, mod set,
and camera path or driving route ([BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md)). Each configuration runs at
least three times.

| Configuration | Content | Comparison |
|---|---|---|
| B0 | Game without the mod; Building mode on a fixed camera path | Reference |
| B1 | Mod installed, idle in `Building` | Idle overhead versus B0 |
| B2 | `OnFoot` circuit (sidewalks, ramp, bridge) | Versus a street-level reference with mod controllers disabled through a diagnostic setting |
| B3 | `Driving` route (junctions, slopes, bridge, tunnel) | Same street-level reference approach |
| B4 | `Driving` through the densest block of the lab city with traffic | Proxy and contact load |
| B5 | Two-hour session following the PHASE-12 script | Memory and entity growth |

The street-level reference separates what the mod's systems cost from what rendering the city at street level costs.
The latter belongs to the host and is addressed only through optional enhancements (section 8) and OPEN-024.

## 5. Investigation trigger (REQ-PERF-001)

A **recurring frame-time regression above 10%** triggers an investigation **[Owner requirement, initial proposal]**:

- "Recurring" means the regression of mean or 95th-percentile frame time appears in at least two of three repeated
  runs of the same configuration **[Proposal]**.
- The comparison is against the latest accepted baseline for that configuration, or against the previous mod build.
- An investigation produces a note in the evidence record: cause, module attribution, and a decision (fix, accept with
  owner approval, or make the feature optional). It does not automatically fail a phase.

## 6. Budgets

| Budget | Value |
|---|---|
| Idle overhead in `Building` (B1 versus B0) | To be set from measured baseline in PHASE-03, confirmed in PHASE-12 |
| Mod update time per frame in `OnFoot` and `Driving` | To be set from measured baseline in PHASE-03, confirmed in PHASE-12 |
| Collision proxy memory and count | To be set from measured baseline in PHASE-03 (laboratory caps in [SPEC-CONTACT](WORLD_CONTACT.md) apply until then) |
| Memory growth over two hours | To be set from measured baseline in PHASE-12 |
| Simulation speed loss | To be set from measured baseline in PHASE-03, confirmed in PHASE-12 |
| Revalidation time on return from `Building` | To be set from measured baseline in PHASE-10, confirmed in PHASE-12 |
| Save hitch and payload size | To be set from measured baseline in PHASE-06, confirmed in PHASE-12 |

**Decision procedure:**

1. QA records baselines B0–B4 on the reference machine described in [WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md)
   (after TASK-0001), following [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md), with `RUN-*` evidence.
2. The Coordinator proposes each budget as a share of the baseline or an absolute cap, citing the runs and leaving a
   stated margin, and writes it into the table as **[Proposal]**.
3. A Reviewer checks the calculation against the evidence; the project owner accepts or adjusts the values.
4. PHASE-12 re-measures on representative cities and the additional hardware listed in
   [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md) and updates the budgets the same way.
5. Accepted budgets are cited, never copied, by other documents.

## 7. Design rules that keep cost bounded **[Proposal]**

- Collision proxies are created only within a radius, with per-frame creation limits and eviction
  ([SPEC-CONTACT](WORLD_CONTACT.md)).
- Fixed-step motion has a maximum substep count per frame (RISK-103).
- Revalidation after `Building` is prioritized and amortized across frames (RISK-106).
- Hot paths (controllers, contact queries) avoid per-frame allocations; caches are keyed by `CityRevisionStamp`.
- Debug overlays and verbose logging are off by default; logs are rate-limited.
- Settlement catch-up is processed in batches per frame ([SPEC-TIME](GAME_TIME_AND_PAUSE.md)).
- Nothing runs per frame in `Building` beyond event listening and Diagnostics.

## 8. Optional street-level enhancements (REQ-PERF-002)

Street-level visual improvements (for example, level-of-detail bias near the player, extra lighting, ground detail)
are optional, each behind its own setting, and measured individually against the B2/B3 configuration. An enhancement
whose measured cost exceeds the agreed budget is off by default and labeled with its cost in the settings. What these
enhancements can be, and whether they are possible at all, is unverified (OPEN-024).

## 9. Failure handling

- A budget overrun observed at runtime (for example the proxy cap) degrades the feature (eviction, speed limiting) and
  is logged; it never disables collision silently.
- A recurring regression found in a phase gate follows section 5; if it cannot be fixed, the owner decides between
  accepting it, making the feature optional, or a scope review.

## 10. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| Frame time, memory, and simulation speed measured against a recorded baseline with a repeatable protocol; regressions above 10% investigated. | TEST-PERF-001 | Not run |
| Street-level enhancements are optional, individually measured, and off by default above budget. | TEST-PERF-002 | Not run |
| Proxy counts and memory stay within caps under the B4 configuration. | TEST-WORLD-003 | Not run |

## 11. Open questions and risks

- OPEN-012 (entity counting), OPEN-013 (simulation speed reading), OPEN-024 (street-level rendering cost).
- RISK-008 (street-level performance), RISK-103 (substep cost spiral), RISK-106 (revalidation cost spikes).

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
