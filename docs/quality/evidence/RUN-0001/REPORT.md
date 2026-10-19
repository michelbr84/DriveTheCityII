# RUN-0001 — Upstream base-mod runtime baseline (TASK-0003)

| Field | Value |
|---|---|
| Run ID | RUN-0001 |
| Task | [TASK-0003](../../../planning/tasks/TASK-0003-isolated-baseline-validation.md) |
| Date | 2026-10-19 |
| Executed by | Claude Code agent (owner-authorized autonomous run of 2026-10-19); no human reviewer yet |
| Result | **Passed for load, takeover, drive, collision and handback; reload not exercised** (see table) |

## Configuration

| Item | Value |
|---|---|
| Game | Cities: Skylines II **1.6.2f1** (Steam build 25127643), Unity 2022.3.71f1, Mono; version string reported in game by `Application.version` |
| Edition / DLCs | Not determined (owner question; not needed for this baseline) |
| Machine | Windows 11 Pro 10.0.26300, RTX 5090 Laptop GPU, 3840x2400 |
| Upstream mod | Beta Test Driving Mod 0.3.54-stable, commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (clean checkout outside the project) |
| Toolchain | .NET SDK 10.0.402 (private, user-scoped), Microsoft.NETFramework.ReferenceAssemblies 1.0.3, game `Managed` assemblies; no official modding toolchain, no Unity Editor |
| Mod set | Upstream mod plus Drive the City II (to test the co-load guard), both in `<user data>\Mods\` |
| City | `DTC2-LAB-Bench-S1`, a lab copy of the game's own built-in benchmark city (not a player save) |
| Session | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261019-031603` |

## Steps and results

| # | Step | Result |
|---|---|---|
| 1 | Build the pinned commit (twice, clean output folders) | **Passed**: 0 warnings, 0 errors both times (`build\upstream-baseline`, `build\upstream-baseline-2`). Only deviation: `ManagedPath` passed on the command line instead of the relative default. Builds are not byte-identical (upstream project is not deterministic). |
| 2 | Install into the local mods folder (`tools\baseline.ps1 -Install`, ownership record) | **Passed** |
| 3 | Launch and load the lab city | **Passed**: Modding.log `Loaded BetaTestDrivingMod, Version=0.3.54.0`; its systems registered |
| 4 | Co-load guard: Drive the City II with the upstream mod present | **Passed**: our status `conflict=True`; entering character mode refused with the conflict message |
| 5 | Takeover with real key V | **Passed**: `Direct Drive possessed Entity(...) 'Car09' by nearest car`; chase camera attached |
| 6 | Drive with real Up arrow, 3 s | **Passed** (car in a queue): upstream collision resolved repeatedly against the car ahead |
| 7 | Release with real key V (handback) | **Passed**: `released ... Released by toggle`; `release handoff rebuilt vanilla path from current lane` |
| 8 | Remove (`tools\baseline.ps1 -Remove`) | **Passed**: folder removed; only Drive the City II remains |
| 9 | Reload with the mod disabled | Not run |

Evidence: [log excerpt](logs/upstream-excerpt.log) (sanitized) and [screenshot](images/upstream-possessed-car.jpg).

## Observations

- The upstream mod is runtime-compatible with 1.6.2f1 for the steps above, despite declaring 1.5.\*.
- Upstream's default F8 (tuning panel) collides with Drive the City II's default F8 (character mode); the co-load
  guard makes our mod inert, so the collision has no effect, but both mods should not be installed together.
- This run does not change the reuse decision: Drive the City II implements its own controller
  ([BASE_MOD_AUDIT](../../../research/BASE_MOD_AUDIT.md) recommendation stands; no upstream code is incorporated).

## Rollback

Upstream files removed by the ownership record; `UserState.coc` restored by `tools\test-session.ps1 -Stop`; no
player save, game file, or other mod touched.
