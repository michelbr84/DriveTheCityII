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
> evidence (session folders under `%LOCALAPPDATA%\DriveTheCityII\test\sessions\` and the
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
- Component: test tooling (`tools\test-session.ps1`)
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

### ISSUE-007 — One-second freeze when the job board builds taxi offers
- Severity: Medium
- Status: Verified fixed
- Component: Careers (world adapter)
- Cause: every place query refined the curb-side access point of every candidate place (two lane searches each), and
  route estimates had only a count budget. Measured: 1,138 ms in place queries for one refresh.
- Fix: place queries return the road-edge point; refinement happens only for places that are actually route-searched or
  shown; route searches have a 40 ms per-frame budget (estimates beyond it are retried later, never offered as
  reachable). Verification: refresh cost 55–80 ms (sessions 20261019-041354, -042215).

### ISSUE-008 — Running challenges were invisible in the world
- Severity: High
- Status: Fixed (in-game verification in the career coverage campaign)
- Component: Careers (presentation)
- Cause: the challenge rules ran in Core, but the mod drew no checkpoint markers, route, or HUD card for a running
  challenge.
- Fix: next and following checkpoints are marked, the route is drawn, and the HUD shows checkpoint N of M, time with
  penalties, and the best record.

### ISSUE-009 — Router and place index rebuild cause occasional 50–130 ms hitches
- Severity: Low
- Status: Open
- Component: City bridge
- Detail: when the number of car lanes changes (the live city adds and removes temporary lanes), the road graph
  (about 50 ms) and the place index (about 110 ms) are rebuilt on the main thread, at most every 2 minutes for
  version-only changes. Observed in the 2026-10-19 logs. Workaround: none needed; noticeable only as a rare stutter.
- Next step: incremental updates or spreading the rebuild over several frames.

### ISSUE-010 — Exiting a car stopped mid-lane places the character on the road
- Severity: Low
- Status: Open
- Component: Player (transitions)
- Detail: exits search for a sidewalk point beside the car; when the car stops in the middle of a wide road the
  character is placed beside the door on the carriageway (1 of 10 exits reached a sidewalk in the 10-cycle run where the
  autopilot stopped mid-lane; real-key exits at the curb landed on the sidewalk). AI traffic treats the character like a
  pedestrian.
- Workaround: park at the curb before pressing E.

### ISSUE-011 — Career time stops while the game window is not focused
- Severity: Low (by design, documented)
- Status: Open (design decision)
- Component: Player / Careers
- Detail: an unfocused game window counts as paused, so mission timers and company time stop (protects players who
  Alt-Tab). Test runs keep the game window in front.

### ISSUE-012 — Not implemented or not testable in this build
- Severity: Medium
- Status: Open (limitation)
- Component: Presentation / input
- Detail: no footstep sound (a procedural vehicle sound exists since 2026-10-19); entering and leaving a car is a
  short slide, not an animation; gamepad bindings exist but were never tested (no device); night lighting of the
  character and car not checked; nobody has listened to the vehicle sound (verified only by measured output).

### ISSUE-013 — Freight and bus jobs refused while driving a car
- Severity: High
- Status: Verified fixed (session 20261019-050655: the truck was provided and boarded)
- Component: Careers (accept)
- Cause: the vehicle being driven was passed to the job even when the job provides its own truck or bus, so the
  career refused it ("This job needs a RigidTruck").

### ISSUE-014 — The employer's truck or bus never appeared after accepting
- Severity: Critical (freight and bus unplayable)
- Status: Verified fixed (sessions 20261019-050655, -052326)
- Component: Careers (event handling)
- Cause: career commands return their events in the command result; the mod handled only events from the frame
  update, so "service vehicle provided" from Accept was dropped. All command results now go through the same handler.

### ISSUE-015 — Delivery deadlines expired before an on-route driver could finish
- Severity: High
- Status: Verified fixed (delivery completed in session 20261019-044815)
- Component: City bridge (route estimates)
- Cause: route times assumed free flow at the speed limit. They are now calibrated (x1.3 plus 3 s per junction).

### ISSUE-016 — Challenge runs voided as "route changed" in a live city
- Severity: High
- Status: Verified fixed (final package: no route change during runs; one expected "route changed" notice per
  challenge on the first load after the update, because the stored points moved to the new stable access points)
- Component: Careers (place access, challenge signature)
- Cause: the place access point the career saw changed when markers or route searches refined it, and the challenge
  signature included the computed route length, which varies as the city adds and removes temporary lanes. The
  career now sees a stable base access point, and route length is not part of the signature.

### ISSUE-017 — Freight and bus runs not completed in game by the test autopilot
- Severity: Medium (test coverage, not a player defect as far as known)
- Status: Open
- Component: Test tooling
- Detail: the simulated-input autopilot drove the truck into traffic right after loading (load lost: the correct rule
  outcome) and got the bus stuck before the first stop. Complete freight and bus runs are covered by unit tests only.
  A human playthrough is needed.

### ISSUE-018 — After a failed employer job you still sit in the released truck or bus
- Severity: Low
- Status: Open (by design; wording could be clearer)
- Component: Careers
- Detail: when a freight or bus job ends, the employer's vehicle is released ("the operator collects the service
  vehicle when you leave it"), but you remain in it until you get out. Jobs and challenges that need your own career
  vehicle are refused meanwhile ("Challenges need one of your career vehicles"). Workaround: get out and use your own
  vehicle (Tab → Garage → "Bring here").

### ISSUE-019 — Test teleport helper can silently fail
- Severity: Low (test tooling only)
- Status: Open
- Component: Test bridge
- Detail: `career goplace` reports success even when the avatar could not be placed at the target (invalid ground or
  inside a footprint). It caused one refused repair during the soak; the next maintenance visit succeeded.

## 7. Release use

- Before each phase gate, QA lists the open issues by severity in the gate evidence.
- The PHASE-12 and PHASE-13 gates require **zero open Critical issues**.
- Player-facing workarounds for open issues are summarized in [TROUBLESHOOTING](../user/TROUBLESHOOTING.md) once a
  release exists.
