# Benchmark Protocol

| Field | Value |
|---|---|
| ID | DOC-BENCHMARK |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-03 (first measurements; earlier from PHASE-01 if the lab exists), PHASE-04, PHASE-11, PHASE-12, and after every game update |
| Responsible role | QA (maintainer and executor); Reviewer checks that compared runs share the same conditions |
| Related requirements | REQ-PERF-001, REQ-PERF-002, REQ-WORLD-003, REQ-TIME-001 |
| Related documents | [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md), [TEST_STRATEGY](TEST_STRATEGY.md), [TEST_SCENARIOS](TEST_SCENARIOS.md), [SPEC-CONTACT](../architecture/WORLD_CONTACT.md), [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md), [evidence](evidence/README.md) |
| Last updated | 2026-10-18 |

> **No benchmark has been run.** There is no baseline, no measured value, and no performance budget. Every scenario
> in this file has the validation status **Not run**. Nothing here promises a frame rate.

## 1. Purpose

This protocol makes performance measurements repeatable and comparable, so that a change in frame time, memory, or
simulation speed can be attributed to the mod rather than to noise, a game update, or different settings. It defines
the fixed conditions, the procedure, the metrics, the benchmark scenarios (`TEST-PERF-101` and up), and the rule that
turns a measured regression into an investigation.

The primary tests TEST-PERF-001 and TEST-PERF-002 in [TEST_SCENARIOS](TEST_SCENARIOS.md) are satisfied by following
this protocol.

## 2. Principles

1. **Baseline first.** The vanilla reference (TEST-PERF-101) is measured before anything with the mod. Every comparison
   cites the `RUN-NNNN` of the baseline it is compared with.
2. **Same conditions or no comparison.** Two measurements are comparable only if every fixed condition in §3 matches.
   Otherwise, the comparison result is **Inconclusive**.
3. **Investigation trigger, not budget.** A recurring frame-time regression above 10% opens an investigation (§7). It
   is not a pass/fail budget and not a frame-rate promise. Final budgets are set in
   [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) only after measured baselines exist.
4. **Frame time over FPS.** Results are reported as frame-time distributions. An average frames-per-second figure hides
   stutter and is not used as a target.
5. **Measure the measurer.** Any in-mod sampling has its own cost. That overhead is measured once per tool version and
   recorded.
6. **No invented numbers.** Durations, repetitions, and thresholds are set by calibration (§4.1) and recorded. They are
   not assumed in advance.

## 3. Fixed conditions (recorded in every run)

| Category | What to record and hold constant |
|---|---|
| Hardware | CPU model, GPU model, installed memory, storage type for the game, display resolution and refresh rate. No serial numbers or machine names. |
| System | Windows version and build, power mode, GPU driver version as observed, background applications. Note OneDrive sync activity; pause it only if the owner agrees (RISK-011). |
| Game | Game version, store edition, DLC set (OPEN-001), graphics preset and every changed graphics setting, resolution, window mode, frame limiter and vertical sync state. |
| Mod set | Mod commit or version, mod settings, and every other enabled mod with its version. Benchmarks use an isolated mod set, ideally containing only the mod under test (OPEN-035). |
| Lab city | Benchmark lab city name, master version, and content hash; simulation speed setting; time of day and weather if the game lets them be fixed (**[Unverified]**). |
| Scenario | Scenario ID, camera path or drive route version, warm-up, duration, repetitions (§4.1). |
| Tooling | Name and version of every measurement tool used (OPEN-505), and its measured overhead. |

## 4. Procedure

### 4.1 Calibration (once per benchmark lab city and hardware profile)

1. Run the vanilla reference scenario several times, from a fresh game start each time.
2. Determine the **warm-up** W: the time after loading until frame time and memory stop trending.
3. Determine the **duration** D: long enough that repeating the run gives stable percentiles.
4. Determine the **repetitions** R: enough to know the run-to-run variation of each metric. A single run is never
   sufficient.
5. Record W, D, R, and the observed run-to-run variation in the parameter table (§9). Re-calibrate after a hardware,
   driver, game-version, or lab-city change.

### 4.2 A benchmark run

1. Confirm the entry criteria in [TEST_STRATEGY](TEST_STRATEGY.md) §9 and record the fixed conditions (§3).
2. Start the game fresh. Load a fresh working copy of the benchmark lab city.
3. Wait for the warm-up W without moving the camera.
4. Execute the scenario's camera path or route for the duration D while capturing the metrics.
5. Repeat R times, restarting the game between repetitions unless the scenario says otherwise.
6. Compute the metrics (§5) per repetition and across repetitions.
7. Compare with the cited baseline run (§7) and record the result in a `RUN-NNNN` ([evidence](evidence/README.md)).
   Raw captures go to the git-ignored `artifacts/` area; the run keeps a summary and references.

### 4.3 Camera paths and routes

A camera path is only useful if it can be repeated exactly (OPEN-505):

- **Preferred:** a development-only, scripted camera path or drive inside our Diagnostics module, if OPEN-503 / OPEN-505
  show that it is feasible. Its overhead is measured.
- **Fallback for Building mode:** a fixed list of camera viewpoints, held for equal times, reached in the same order.
- **Fallback for driving and walking:** a fixed route driven or walked manually. Route completion time is recorded,
  and the larger run-to-run variation is accepted and documented.

Each path or route has a version number. Changing it invalidates comparisons with earlier versions.

## 5. Metrics

| Metric | Definition | Notes |
|---|---|---|
| Frame-time distribution | Per-frame time over the capture: median, 95th and 99th percentiles, maximum, and the share of frames above a stall threshold | The stall threshold is derived from the calibrated vanilla baseline and recorded in §9. |
| Memory | Game process memory at the start and end of the capture, peak, and growth per unit of time | Exact counters depend on the chosen tool (OPEN-505). |
| Simulation speed | In-game time advanced per unit of real time at a recorded simulation speed setting | Shows whether character mode slows the city simulation (TEST-PERF-106). Reading it depends on OPEN-013. |
| Mod entity and proxy counts | Counts per entity category from the mod's registry, and CollisionProxy count and memory if proxies are used | Planned readouts (OPEN-502). |
| Native counts (context) | Population, vehicles, or other counts as the game displays them | Context only, to confirm the lab city state matches. |
| Load and save time | Time to load and to save the lab city | TEST-PERF-109. |

## 6. Tooling

**To be confirmed (OPEN-505).** No measurement tool is selected or installed, and no tool version is claimed. Candidate
categories:

- A sampler inside our own Diagnostics module (engine frame delta, registry counts). It requires our mod to exist, and
  its overhead must be measured.
- Performance displays built into the game, if any exist. **[Unverified]**
- External capture tools at the operating-system or GPU-vendor level. Installing any of them needs owner authorization
  and an entry in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).

The chosen tools and their versions are recorded in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md) and cited in
each run.

## 7. Regression trigger **[Proposal]**

- **Comparison:** median and 95th-percentile frame time of a scenario, against the latest valid baseline or the last
  accepted measurement of the same scenario, under identical fixed conditions.
- **Regression:** either value is more than 10% higher than the reference, beyond the run-to-run variation recorded in
  calibration.
- **Recurring:** the regression appears again in a second series of repetitions started from a fresh game start. A
  single outlier is not a trigger.
- **Action:** open an investigation (a task, or an `ISSUE-NNN` in [KNOWN_ISSUES](KNOWN_ISSUES.md) if a defect is
  identified) and link it from the run. The trigger does not fail a phase gate automatically, and staying under it does
  not prove that performance is acceptable.
- **Re-baseline:** after a game update, GPU driver change, hardware change, or lab city rebuild, measure the baseline
  again before comparing.

## 8. Benchmark scenarios

#### TEST-PERF-101 — Vanilla reference baseline in Building mode
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-03 (from PHASE-01 if the lab exists); again after every game update · **Validation status:** Not run
- **Preconditions:** Benchmark lab city; isolated mod set with no mods; calibration done (§4.1).
- **Procedure:** §4.2 with the Building-mode camera path.
- **Expected result:** A recorded baseline with all fixed conditions and the observed run-to-run variation. There is no target value.

#### TEST-PERF-102 — Mod loaded and idle in Building mode
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-01, PHASE-03, every phase gate · **Validation status:** Not run
- **Preconditions:** As TEST-PERF-101, with the mod enabled but character mode never entered.
- **Procedure:** §4.2 with the same camera path as TEST-PERF-101.
- **Expected result:** The difference from TEST-PERF-101 is recorded. A recurring regression above 10% opens an investigation (§7).

#### TEST-PERF-103 — Street-level driving route
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-03, PHASE-12 · **Validation status:** Not run
- **Preconditions:** Benchmark lab city with a route through dense frontage, junctions, a bridge, and a tunnel at moderate and congested traffic; a car.
- **Procedure:** §4.2, driving the fixed route.
- **Expected result:** Metrics recorded. The first accepted measurement becomes this scenario's reference. Later builds are compared with it (§7). A comparison with a vanilla street-level view is added only if the game offers an equivalent view **[Unverified]**.

#### TEST-PERF-104 — Street-level on-foot circuit
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-04, PHASE-12 · **Validation status:** Not run
- **Preconditions:** Benchmark lab city; the PHASE-04 representative circuit or a fixed part of it.
- **Procedure:** §4.2, walking the fixed circuit in third-person view, then in first-person view.
- **Expected result:** Metrics recorded per camera view; comparisons per §7.

#### TEST-PERF-105 — Collision-proxy stress
- **Layer:** In-game (benchmark) · **Requirement:** REQ-WORLD-003, REQ-PERF-001 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Applies only if the CollisionProxy approach is adopted; otherwise "not applicable" with the reason. The densest district of the benchmark lab city; proxy readouts.
- **Procedure:** §4.2 on a route that maximizes nearby static geometry; then leave the district and stay outside it for a further capture.
- **Expected result:** Proxy count and memory stay within the [SPEC-CONTACT](../architecture/WORLD_CONTACT.md) bounds, and their frame-time cost is recorded. Counts fall back after leaving the district.

#### TEST-PERF-106 — Simulation throughput in character mode
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001, REQ-TIME-001 · **Phase:** PHASE-03 · **Validation status:** Not run
- **Preconditions:** Benchmark lab city; a way to read in-game time and the simulation speed setting (OPEN-013).
- **Procedure:** Measure in-game time advanced per unit of real time at normal speed, first in `Building` and then while driving the TEST-PERF-103 route.
- **Expected result:** Both values are recorded. A recurring reduction beyond calibration variation opens an investigation, because it changes the city simulation the player experiences.

#### TEST-PERF-107 — Long-session drift (at least two hours)
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-12 · **Validation status:** Not run
- **Preconditions:** A representative city; may run together with TEST-SAVE-114.
- **Procedure:** Sample frame time, memory, mod entity and proxy counts, and log size at regular intervals throughout a session of at least two hours of mixed play.
- **Expected result:** **[Proposal]** The trends are recorded. Memory, entity counts, or log size that keep growing without levelling off open an investigation.

#### TEST-PERF-108 — Street-level visual enhancements on and off
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-002 · **Phase:** PHASE-11 · **Validation status:** Not run
- **Preconditions:** A build with the optional enhancements; the budget agreed in SPEC-PERF.
- **Procedure:** Run TEST-PERF-103 and TEST-PERF-104 with each enhancement off and on.
- **Expected result:** The cost of each enhancement is recorded. Any enhancement exceeding the agreed budget is off by default (TEST-PERF-002).

#### TEST-PERF-109 — Save and load time with career data
- **Layer:** In-game (benchmark) · **Requirement:** REQ-PERF-001 · **Phase:** PHASE-06, PHASE-12 · **Validation status:** Not run
- **Preconditions:** Lab copies of the benchmark city without career data, with a short career, and with a long career (for example from TEST-SAVE-114).
- **Procedure:** Measure save time and load time for each copy, with repetitions.
- **Expected result:** Times are recorded per career-data size. A recurring increase above 10% over the copy without career data opens an investigation.

## 9. Calibration parameters

| Parameter | Value | Status |
|---|---|---|
| Warm-up W | — | Not set (no calibration run) |
| Duration D | — | Not set |
| Repetitions R | — | Not set |
| Run-to-run variation per metric | — | Not set |
| Stall threshold (derived from the vanilla baseline) | — | Not set |
| Building-mode camera path (version) | — | Not defined |
| Driving route (version) | — | Not defined |
| On-foot circuit (version) | — | Not defined |

## 10. Results

No benchmark has been run. The table stays empty until real `RUN-NNNN` records exist.

| RUN | Scenario | Date | Game version | Mod commit | Hardware profile | Baseline RUN | Median frame time | 95th pct. | 99th pct. | Peak memory | Simulation speed | Result |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — | — | — | — | Not run |

## 11. Open questions and risks

- **OPEN-505** (new) — Measurement tooling for frame time, memory, and simulation speed, and a repeatable camera path or
  drive.
- **OPEN-503** (new) — Feasibility of a scripted, development-only in-game driver (also used for camera paths).
- **OPEN-502** (new) — Registry and proxy readouts.
- **OPEN-013** — Reading simulation speed and pause state.
- **OPEN-024** — Street-level rendering quality and cost.
- **RISK-008** — Performance degradation at street level.
- **RISK-011** — OneDrive synchronization interfering with captures or storing large artifacts.

All entries are in the [register](../planning/RISKS_AND_OPEN_QUESTIONS.md).
