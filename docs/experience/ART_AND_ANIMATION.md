# Art and Animation

| Field | Value |
|---|---|
| ID | SPEC-ART |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-02 (vehicle model question, OPEN-034), PHASE-04 (avatar representation, OPEN-010), PHASE-05 (transitions), PHASE-06 (passenger representation), PHASE-11 (refinement), PHASE-13 (provenance audit) |
| Responsible role | Coordinator (maintainer); Implementer; project owner approves any asset source; Reviewer checks provenance |
| Related requirements | REQ-ART-001, REQ-ART-002, REQ-DRIVE-004, REQ-PLAYER-001, REQ-PLAYER-003, REQ-TAXI-002, REQ-HOME-002, REQ-PERF-002, REQ-REL-003 |
| Related documents | [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md), [SPEC-TRANSITION](../gameplay/WALK_DRIVE_TRANSITIONS.md), [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md), [SPEC-UI](UI_AND_HUD.md) |
| Last updated | 2026-10-18 |

> **Status:** policy and plan. No asset has been created, licensed, or incorporated. The avatar representation is
> undecided (OPEN-010), and so is the source of vehicle models (OPEN-034).

## 1. Purpose

This spec defines which visual assets the mod may use and where they may come from, how the avatar and its
animations are represented, how placeholders are labeled, when a cockpit view is allowed, and how street-level
visual options are evaluated.

## 2. Scope and non-goals

**In scope:** avatar model and animations, transition visuals, career passenger and mission character
representation, mission markers, career vehicle appearance, camera-related visual rules, and optional street-level
visual adjustments.

**Non-goals for v1.0:**

- Building interiors (EXP-001, EXP-016).
- Detailed cockpits for every vehicle (EXP-010).
- Structural damage visuals (EXP-008).
- An expanded animation set (EXP-017).
- Any change to the game's own assets or rendering settings outside character mode.

## 3. Behavior and contracts

### 3.1 Asset policy

| Source | Allowed? | Conditions |
|---|---|---|
| Created by the project | Yes | A provenance entry (author, date, tool, source files location) in [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) |
| Third-party asset with a license that allows modification and redistribution in a game mod | Yes, after review | License text kept; attribution in [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md); compatible with the project license (OPEN-027) |
| Assets produced with generative tools | Only with project-owner approval | The tool and its terms are recorded like any other source; same review as third-party assets |
| The game's own models used **at runtime** (referenced by the running game, never copied into the project) | Only if platform and publisher rules permit it | Decided per asset type: avatar (OPEN-010) and vehicles (OPEN-034); never extracted, converted, or redistributed |
| Files extracted from CS2, Motor Town, SimCity products, or other games | **No** | Never, including for "temporary" use (RISK-029) |
| Assets of unknown origin (for example, downloaded without a clear license) | **No** | — |

Every asset in a build must have a provenance entry before distribution (REQ-REL-003). Source art files and large
binaries stay out of the documentation tree. Where they live is decided with the version-control setup (OPEN-029).

### 3.2 Avatar representation (pending OPEN-010)

The avatar is mod-owned and never a native citizen with a household, job, or schedule (REQ-PLAYER-001).

| Option | Description | Main risks |
|---|---|---|
| A. Own or authorized rigged model | A humanoid model and animations that the project owns or licenses | Production cost; whether the game can load and animate it (RISK-009) |
| B. Runtime representation of a native character model | Reuse a character appearance the running game already provides, without taking over a citizen and without redistributing anything | Platform rules; whether the animation can be controlled; coupling to game versions (RISK-007) |
| C. Labeled placeholder | A simple mannequin, clearly labeled | Low appeal; acceptable for M1 and M2 only if labeled (REQ-ART-002) |

**PHASE-04 evaluation criteria:** legal clearance, animation control, visual fit at street level, first-person body
visibility (whether hands or body are shown), frame-time cost, and behavior across save/load. The decision and its
evidence are recorded in [CS2_INTEGRATION_FINDINGS](../research/CS2_INTEGRATION_FINDINGS.md) and in SPEC-PLAYER.

### 3.3 Animation list

| Animation | State | v1.0 status | Substitute if unavailable |
|---|---|---|---|
| Idle | `OnFoot` | Required (REQ-ART-001) | None: a static pose is acceptable only as a labeled placeholder |
| Walk | `OnFoot` | Required | None |
| Run | `OnFoot` | Required | Faster walk cycle, labeled placeholder |
| Turn in place | `OnFoot` | Desirable | Body rotates with the walk blend |
| Enter vehicle | `EnteringVehicle` | Required, or a documented substitute (REQ-ART-001) | Substitute transition (section 3.4) |
| Exit vehicle | `ExitingVehicle` | Required, or a documented substitute | Substitute transition |
| Recovery | `Recovery` | Substitute by design | Fade out, move, fade in |
| Career passenger board and alight | `Servicing` leg (taxi, bus) | Represented (REQ-TAXI-002, REQ-BUS-001) | The passenger walks to the door point, then a short fade |
| Mission character at a service point | `OnFoot` near a service point | Optional (REQ-HOME-002) | A marker and a name label instead of a character |

### 3.4 Substitute transition **[Proposal]**

Used when an entry or exit animation is unavailable, or when the vehicle model has no usable door positions.

1. The avatar walks to the validated door-side point from SPEC-TRANSITION.
2. A short camera blend runs (about 0.4 s), and the avatar is hidden at the door point. The camera owner changes as
   defined in SPEC-CONTROL.
3. For exit, the same steps run in reverse, at the validated exit point only.
4. If the transition aborts (REQ-TRANS-003), the visuals reverse and the avatar reappears at the last validated
   position.

The avatar must never be visible intersecting the vehicle body or a wall for more than a few frames. A substitute
transition is documented as such in release notes. It is a design choice, not a hidden defect.

### 3.5 Placeholder labeling (REQ-ART-002)

- A placeholder is any visual (or audio, see [SPEC-AUDIO](AUDIO.md)) intended to be replaced before release.
- Every placeholder is listed in [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) with the status "Placeholder".
- **Builds:** development and beta builds that contain placeholders show the persistent "Placeholder" tag defined in
  [SPEC-UI](UI_AND_HUD.md). The mod's About page lists them.
- **Documentation:** screenshots or videos that show placeholders say so in their caption.
- A release build may contain a placeholder only if the owner accepts it explicitly and it stays listed.

### 3.6 Cockpit and driver camera policy (REQ-DRIVE-004)

- A **detailed cockpit** is shown only for a vehicle model that has a suitable, authorized interior. These models are
  listed per vehicle in SPEC-DRIVING.
- All other vehicles get a **hood or driver-position camera** with documented limitations. It hides geometry that
  would show an empty or broken interior, keeps the HUD speed display (instead of a fake dashboard), and is labeled
  "Driver view (no cockpit model)" ([SPEC-UI](UI_AND_HUD.md)).
- Camera clipping rules for first person belong to [SPEC-PLAYER](../gameplay/PLAYER_AND_CAMERAS.md).

### 3.7 Mission markers and world indicators

- Markers for access points, checkpoints, and service points are mod-owned assets.
- Marker types differ by **shape** as well as color (pickup, drop-off, checkpoint, service), and each has an icon in
  the HUD legend (REQ-INPUT-003).
- Markers stay readable at driving distance, never hide traffic at close range, and fade as the player gets near.
- Markers are `MissionObject` entities with a lifecycle ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)). None
  may remain after a mission ends or the mod unloads.

### 3.8 Career vehicles

- Visual source follows OPEN-034. Each of the four classes must be visually distinct (REQ-DRIVE-003).
- Taxi signs, company names, or liveries are added only if they are authorized assets and technically feasible. They
  are not promised for v1.0.
- Wear may be shown in the HUD and garage UI. It is not shown as mesh damage (EXP-008).

### 3.9 Street-level visual considerations (REQ-PERF-002, OPEN-024)

- First, measure how level of detail, culling, and lighting look at walking and driving camera distances (PHASE-03
  and PHASE-04 captures following [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md)).
- Any improvement is an **optional, measured** setting that applies only in character mode and only near the
  player. It is off by default if it exceeds the agreed budget ([SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md)).
- The mod never raises detail across the whole city, and never changes the player's graphics settings permanently.
  Anything it adjusts is restored when the player returns to Building mode.
- Which adjustments are possible at all is unknown until OPEN-024 is investigated. None is promised here.

## 4. Failure handling and recovery

- **Missing or failed asset:** fall back to a labeled placeholder, log the failure, and continue. An asset error never
  crashes the game or blocks returning to Building mode (REQ-RECOV-002).
- **Avatar model fails to load:** the labeled placeholder avatar is used. Gameplay continues.
- **Provenance gap found late:** the asset is removed from the build or replaced before distribution. The gap is
  recorded in the audit (REQ-REL-003).

## 5. Acceptance criteria

| Test | Criterion | Validation |
|---|---|---|
| TEST-ART-001 | Idle, walk, run, and entry/exit animations (or the documented substitute) play correctly in their states, using only assets with provenance entries | Not run |
| TEST-ART-002 | Every placeholder is labeled in builds and documentation | Not run |
| TEST-DRIVE-004 | The driver view works with documented limitations; cockpits appear only on listed models | Not run |
| TEST-TAXI-002 | Career passengers visibly board and alight, and are never presented as citizens | Not run |
| TEST-PERF-002 | Street-level options are measured and off by default if they exceed the budget | Not run |
| TEST-REL-003 | Every distributed asset has a provenance entry and any required notice | Not run |

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-010 | Avatar representation and animation sources. |
| OPEN-034 | Whether career vehicles may reuse the game's vehicle models at runtime. |
| OPEN-024 | Street-level rendering quality and its cost. |
| OPEN-027 | Project license, which affects which third-party asset licenses are compatible. |
| RISK-009 | Avatar visuals or animation may be infeasible. |
| RISK-015, RISK-029 | IP or branding misuse; accidental redistribution of proprietary content. |
| RISK-008 | Performance degradation at street level. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
