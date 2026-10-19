# Known Issues

| Field | Value |
|---|---|
| ID | DOC-KNOWN-ISSUES |
| Document status | Draft |
| Release scope | v1.0 and every later release |
| Phases | All phases with runtime work (PHASE-00 onwards) |
| Responsible role | QA (maintainer: records observed defects); Implementer (fixes); Reviewer (verifies fixes from evidence); project owner (approves "Won't fix" for Critical or High issues) |
| Related requirements | All `REQ-*` in the [PRD](../product/PRD.md); the PRD §9 release criterion "no known critical defects" |
| Related documents | [TEST_STRATEGY](TEST_STRATEGY.md), [evidence](evidence/README.md), [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [TROUBLESHOOTING](../user/TROUBLESHOOTING.md), [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md) |
| Last updated | 2026-10-19 |

## 1. Current state

> **Runtime testing started on 2026-10-19.** Defects observed in the lab sessions are listed in section 6 with their
> evidence (session folders under `%LOCALAPPDATA%\DriveTheCityII	est\sessions\` and the
> [WORKLOG](evidence/WORKLOG.md)). Testing is still partial: an empty severity class does not mean no defects exist.

## 2. What belongs here and what does not

| Situation | Where it goes |
|---|---|
| A defect **observed** in our mod during a recorded run or reported with reproducible steps | Here, as `ISSUE-NNN` |
| A feature that is not implemented yet | Not a defect. The related test stays **Not run**; the work is in the [ROADMAP](../../ROADMAP.md), phase documents, and [BACKLOG](../planning/BACKLOG.md). |
| An anticipated problem that has not been observed (for example save corruption as a possibility) | The [risk register](../planning/RISKS_AND_OPEN_QUESTIONS.md) as `RISK-*` |
| An unanswered technical question | The register as `OPEN-*` |
| A contradiction between documents | The register as `CONF-*`, or the documentation validation report for documentation-only checks |
| Behavior observed in the **candidate base mod** during baseline validation (TASK-0003), before any of its code is incorporated | [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) findings. It becomes an `ISSUE-NNN` only if the code is incorporated and the defect persists in our mod. |
| Third-party reports about other projects (for example issues filed upstream) | External claims recorded in [SOURCES](../research/SOURCES.md) or the relevant audit; never here unless we reproduce them in our mod |
| A host-game problem that affects our mod | Here, labeled "host game", with the game version, if it affects players of our mod; otherwise a note in the run |

## 3. Severity scale

| Severity | Definition | Examples |
|---|---|---|
| **Critical** | Breaks a release-blocking guarantee. Any open Critical issue blocks the PHASE-12 and PHASE-13 gates ([PRD](../product/PRD.md) §9). | Save corruption or loss of career data; money, progress, or vehicles leaking between save revisions; duplicated rewards or money; permanent loss of control (no way back to `Building` or `OnFoot` without restarting the game); mandatory content that cannot be completed; an unintended persistent change to the city; a city that no longer loads after the mod is disabled or removed. |
| **High** | Severe malfunction with a workaround, or a crash without data loss. | Game crash that loses only unsaved progress; recurring `Recovery` loops; a career that can only be continued through a documented workaround; a handback that leaves a temporary traffic blocker. |
| **Medium** | A feature misbehaves, but the player can continue normally. | Wrong HUD value; an exit point on the less preferred side; a missing PT-BR string; inconsistent challenge scoring. |
| **Low** | Cosmetic or minor. | Small camera jitter; a typo; a placeholder label in the wrong place. |

Severity is assigned by QA and confirmed by the Reviewer. Lowering the severity of a Critical issue requires the
project owner's approval and a recorded reason.

## 4. Issue status values

`Open` → `Investigating` → `Fixed – awaiting verification` → `Verified fixed` (with the verifying `RUN-NNNN`).
Other terminal values: `Duplicate` (with a link to the original), `Not reproducible` (after a documented attempt), and
`Won't fix` (requires a recorded reason; for Critical or High issues, the project owner's approval). A fix is never
marked verified because it compiles.

## 5. Entry format

Copy this block for each new issue. Number issues sequentially (`ISSUE-001`, `ISSUE-002`, …); never reuse a number.

```markdown
### ISSUE-NNN — <short, specific title>
- Severity: Critical / High / Medium / Low
- Status: Open
- Component: <internal module (CityBridge, Player, WorldContact, Vehicles, Career, Persistence, UI, Diagnostics) or "host game">
- First observed: RUN-NNNN on YYYY-MM-DD (or a reproducible report with its source)
- Environment: game version · store edition · DLCs · mod version/commit · other mods (see COMPATIBILITY_MATRIX)
- Affected requirements and tests: REQ-… / TEST-…
- Steps to reproduce: 1) … 2) … 3) …
- Expected: …
- Observed: …
- Frequency: always / intermittent (n of m attempts) / once
- Workaround: … (or "none known")
- Fix: task or change reference (once it exists)
- Verification: RUN-NNNN (once it exists)
- Notes: sanitized log excerpt references; no personal data
```

## 6. Issues

### ISSUE-001 — Game fails to start when launched directly without Steam app ids
- Severity: High (test tooling only; players start through Steam)
- Status: Verified fixed
- Component: test tooling (`tools	est-session.ps1`)
- First observed: session 20261019-015116 on 2026-10-19
- Steps to reproduce: start `Cities2.exe` directly while Steam runs. Observed: fatal NullReferenceException in
  `AssetDatabase.PopulateFromDataSource` (Steam platform not initialized), UI shows an error page.
- Fix: the launch helper sets `SteamAppId`/`SteamGameId` for the child process only. Verification: every later session.

### ISSUE-002 — Game process ends after saving while a mod actor exists
- Severity: Critical (crash on save)
- Status: Verified fixed
- Component: CityBridge (save handling)
- First observed: session 20261019-021355 on 2026-10-19 (save while driving a mod vehicle)
- Cause: mod actors were tagged `Deleted` inside the game's serialization step; the game destroyed them without its
  reference cleanup, leaving stale references for native jobs.
- Fix: actors are tagged `Temp` only during serialization (excluded from the save) and untagged right after
  (`ActorOpsSystem.PreSerialize` / `ActorPostSerializeSystem`).
- Verification: sessions 20261019-023754 and -024201: save while driving succeeded; reload clean; leak check 0.

### ISSUE-003 — Game process ends about one second after spawning a taxi model
- Severity: Critical (crash)
- Status: Verified fixed
- Component: CityBridge (vehicle spawning)
- First observed: session 20261019-022932 on 2026-10-19
- Cause (code-inspected): `TransportPathfindSetup` reads the `Owner` of every taxi without a check; mod vehicles have no
  owner. A bicycle spawn earlier ended the same way.
- Fix: career vehicles are role-neutral (service-role components stripped); bicycles and two-wheelers excluded.
- Verification: session 20261019-023754 (taxi model survived 10 s, driven, saved) and all later sessions.

### ISSUE-004 — Native traffic could push the player's stationary car
- Severity: Medium
- Status: Verified fixed
- Component: Vehicles (collision response)
- First observed: session 20261019-025728 (HUD showed 18 km/h with no input)
- Fix: other vehicles' velocity is no longer injected; moving obstacles only block a car that drives into them.
- Verification: session 20261019-030051 (car stayed at 0 km/h for 45 s while traffic queued behind it).

### ISSUE-005 — Severe frame-rate drop in character mode
- Severity: High (13 fps)
- Status: Verified fixed
- Component: CityBridge (road graph)
- First observed: session 20261019-032125 (`cycles 20`: average frame 79 ms)
- Cause: the road graph was rebuilt nearly every frame (2,589 rebuilds) because lanes receive transient tags.
- Fix: rebuild on real lane-count changes (or at most every 2 minutes). Verification: session 20261019-032643
  (driving 33 ms average vs 32 ms in plain city building).

### ISSUE-006 — HUD rendered at about 2 px tall after a unit change
- Severity: Medium
- Status: Verified fixed
- Component: UI
- Cause: the game defines `1rem = 1px at 1080p`; sizes were divided by 16. Verification: session 20261019-030051.

## 7. Release use

- Before each phase gate, QA lists the open issues by severity in the gate evidence.
- The PHASE-12 and PHASE-13 gates require **zero open Critical issues**.
- Player-facing workarounds for open issues are summarized in [TROUBLESHOOTING](../user/TROUBLESHOOTING.md) once a
  release exists.
