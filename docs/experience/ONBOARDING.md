# Onboarding

| Field | Value |
|---|---|
| ID | SPEC-ONBOARD |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-06 (raw first-job loop), PHASE-07 (economic tutorial), PHASE-08 (per-career introductions), PHASE-11 (complete tutorial; fresh-tester validation), PHASE-13 (independent-tester check) |
| Responsible role | Coordinator (maintainer); Implementer (UI and Career modules); QA (fresh-tester protocol) |
| Related requirements | REQ-ONBOARD-001, REQ-ECON-005, REQ-ECON-004, REQ-GARAGE-001, REQ-TAXI-001, REQ-RECOV-001, REQ-UI-003, REQ-L10N-001, REQ-INPUT-002, REQ-SAVE-001 |
| Related documents | [PLAYER_JOURNEYS](../product/PLAYER_JOURNEYS.md) (JRN-001), [GAME_DESIGN](../product/GAME_DESIGN.md), [SPEC-UI](UI_AND_HUD.md), [SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md), [SPEC-ECON](../gameplay/PROGRESSION_AND_ECONOMY.md), [SPEC-TAXI](../gameplay/careers/TAXI.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [PHASE-11](../planning/phases/PHASE-11-presentation-and-accessibility.md) |
| Last updated | 2026-10-18 |

> **Status:** design and test protocol only. No tutorial exists, and no tester session has taken place.

## 1. Purpose

This spec defines how a new player goes from the first entry into character mode to a first completed taxi job
without console commands or developer help (REQ-ONBOARD-001). It also covers the economic tutorial (REQ-ECON-005),
contextual hints, explanations of recovery, skip and replay, and how the outcome is tested with fresh testers.

## 2. Scope and non-goals

**In scope:** the first-session tutorial, the economic tutorial, per-career first-job introductions, contextual
hints, and the fresh-tester protocol.

**Non-goals:**

- Teaching city building, which is the game's own domain.
- Long text manuals in the game. Documentation for players is in [QUICK_START](../user/QUICK_START.md).
- Voice-over (see [SPEC-AUDIO](AUDIO.md)).

## 3. Behavior and design

### 3.1 Tutorial flow (first session)

Each stage completes when the player performs the action, not when they read the text. Every stage shows one
objective line and the binding for the active device.

| Stage | Trigger | Player does | Teaches | Completes when |
|---|---|---|---|---|
| 0. Discover | First load of a city with the mod active | Reads a one-time notice and enters character mode | Where the entry point is (OPEN-302); that the city is safe | Character mode is entered, or the notice is dismissed (it can be reopened from Help) |
| 1. Arrive | First entry into character mode for this profile | Chooses **Start tutorial** or **Skip** | That the tutorial can be replayed | A choice is made |
| 2. Move | Tutorial start | Walks, looks, runs, switches camera | `OnFoot` controls ([SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md)) | Each action has been performed once |
| 3. Go to rental | Stage 2 done | Follows on-foot guidance to the rental service point | Route view and markers | Arrives in interaction range |
| 4. Money basics | Arrival at the rental point | Opens the rental offer | Economic tutorial, part 1 (section 3.2) | Explanation acknowledged |
| 5. Rent | Stage 4 done | Rents the basic car | Interaction prompts; a ledger entry is created | Rental confirmed (REQ-GARAGE-001) |
| 6. Enter and drive | Rental done | Enters the car, drives a short guided segment, uses the parking brake | `Driving` controls and the camera; stopping is required to exit | Segment done |
| 7. First job | Stage 6 done | Opens the job board and accepts the tutorial fare | Offer cards, fare estimate, accepting | Fare accepted |
| 8. Pick up | Fare accepted | Drives to the pickup access point; the career passenger boards | Access points; career passengers are not citizens (REQ-TAXI-002) | Passenger aboard |
| 9. Deliver | Passenger aboard | Drives to the destination using the route view | Route guidance and smooth driving | Passenger alights |
| 10. Get paid | Drop-off | Reads the settlement summary | Pay breakdown; paid once; ledger reference | Summary closed |
| 11. Park and exit | Summary closed | Parks, exits, and is reminded that career progress is saved with the city save | Exit rules; saving | Avatar on foot; tutorial marked complete |
| 12. Optional: back to building | Tutorial complete | Holds Return to Building, or continues playing | Leaving and returning is free | Either |

**Tutorial fare constraints [Proposal]:**

- It is chosen deterministically: the nearest validated, reachable pair of access points whose route is longer than
  a minimum distance and avoids elevated or complex segments where possible.
- It has no deadline. It cannot fail through poor driving; poor driving only lowers the quality component.
- Its pay comfortably covers the basic rental.

If no valid tutorial fare exists, the tutorial explains what the city lacks (OPEN-301) and offers free exploration
instead.

### 3.2 Economic tutorial (REQ-ECON-005)

Delivered in two short parts, never as one wall of text:

1. **At the first rental (stage 4):**
   - the wallet is personal career money, separate from the city budget, and career transactions never change city
     finances (REQ-ECON-001);
   - renting costs money, and if the wallet is short, the rental is paid from the next earnings (REQ-ECON-004; the
     exact mechanism is in SPEC-ECON);
   - every transaction appears in the ledger.
2. **At the first moment of financial pressure** (the first fuel purchase, the first repair, or the wallet falling
   below the cost of a basic rental):
   - running costs (fuel, wear, maintenance);
   - the rent-or-own decision;
   - the recovery options: the always-available starter taxi job, deferred rental, and selling a vehicle (see
     [JRN-007](../product/PLAYER_JOURNEYS.md)).

Both parts can be re-read in Help and tips.

### 3.3 Contextual hints

Hints are shown **once per profile**, when the situation first happens. They stay available in Help and tips
afterward, and they can be turned off in Settings.

| First occurrence | Hint (English source; illustrative) |
|---|---|
| Exit blocked | "Exits need free space beside the vehicle. Move it, or hold Recover." |
| Low fuel | "Fuel is running low. Fuel points are marked on the route view." |
| Player cancels a job | "Cancelling costs a little reputation, but never money." |
| Mission invalidated by a city edit | "The city changed, so this job was cancelled. No penalty." |
| `Recovery` used | "Recovery moves you to the nearest safe spot. It never moves you closer to your destination." |
| First career unlock | "New career unlocked. Its first job includes a short introduction." |
| Wallet below the rental cost | Part 2 of the economic tutorial (section 3.2) |
| First visit to the Company menu | "Your company is a managerial simulation. Employees and fleet are calculated, not driven in the city." |
| Career unavailable in this city | Explains what to build (for example, a bus line) |
| Idle for a while during an objective **[Proposal]** | Restates the objective and points to the route view |

### 3.4 Recovery explanations

The first time each recovery type happens, a short explanation shows **what happened, what it cost (if anything),
and what to do next**: stuck or wrong level (free `Recovery`), out of fuel, immobilized (tow or rental), and
insolvency (starter path). The rules come from [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md) and
[SPEC-GARAGE](../gameplay/VEHICLE_OWNERSHIP_AND_GARAGE.md). This spec only defines how they are presented.

### 3.5 Per-career introductions (PHASE-08 gate)

The first job in each of delivery, freight, and bus starts with a two- or three-step introduction:

- **Delivery:** stop order, deadlines, and fragility.
- **Freight:** mass, capacity, and loading bays.
- **Bus:** stop order, capacity, and trip evaluation, carrying career passengers only.

The first company session gets a similar introduction (contracts, hiring, checkpoints, the managerial-simulation
label). Each introduction uses a validated job with relaxed constraints, by the same principles as the tutorial
fare.

### 3.6 Skip, resume, and replay

- **Skip:** offered at stage 1 and in the tutorial panel at any time. Skipping still grants the starter rental path,
  and part 1 of the economic tutorial appears at the first rental anyway.
- **Resume:** tutorial progress is career data bound to the save (REQ-SAVE-001). Loading a save resumes from that
  save's tutorial stage. Returning to Building mode suspends the tutorial, and it resumes on re-entry.
- **Replay:** the tutorial can be replayed from the Career menu. A replay never pays twice for the tutorial fare
  beyond a normal fare (REQ-CAREER-004).
- **Invalidation:** if a tutorial target becomes invalid (a city edit, or a removed rental point), the stage picks a
  new valid target without penalty.

### 3.7 Presentation rules

- One instruction at a time, with the binding shown for the active device.
- Text in English and PT-BR ([SPEC-L10N](LOCALIZATION.md)), with layouts that tolerate longer PT-BR text.
- No time pressure during the tutorial, and the game can be paused at any stage.
- Instructions never refer to console commands, debug keys, or developer tools.

## 4. Failure handling and recovery

| Problem | Handling |
|---|---|
| The player wanders off or gets stuck | Guidance stays; hint after idling; `Recovery` is available; the stage does not reset |
| The rental point becomes unreachable | A new rental point is chosen, or the starter car is delivered to the player **[Proposal]** |
| A tutorial step cannot complete because of a mod error | The tutorial marks the step skipped, logs the error, and continues. A broken tutorial never blocks play (REQ-RECOV-002). |
| The player quits mid-tutorial without saving | Resumes from the last saved stage |

## 5. Acceptance criteria and fresh-tester protocol

| Test | Criterion | Validation |
|---|---|---|
| TEST-ONBOARD-001 | Fresh testers complete the first-session tutorial up to the first paid taxi job without console use or developer assistance (protocol below) | Not run |
| TEST-ECON-005 | Both economic tutorial parts trigger at the right moments and can be re-read | Not run |
| TEST-UI-003 | Every hint situation in section 3.3 shows its hint once, and it appears in the history | Not run |
| TEST-L10N-001 | The tutorial is complete in English and PT-BR | Not run |

**Fresh-tester protocol [Proposal]** (PHASE-11; evidence recorded under [evidence](../quality/evidence/README.md)):

1. **Testers:** at least five people who have never used this mod. At least two of them run the session in PT-BR,
   and at least one plays only with a gamepad. Ideally at least one has little CS2 experience. Who recruits the
   testers, and how, is an owner decision (RISK-302).
2. **Setup:** a supported configuration. A copy of a lab city with a connected road network and the minimum content
   (OPEN-301). The mod is installed by QA following the current draft of
   [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md). Testers never use the owner's primary saves.
3. **Briefing:** only "This mod lets you walk and drive in your city. Please try it and think aloud." No controls are
   explained.
4. **Observation:** the observer does not help. Any help, console use, or developer intervention makes that tester's
   run a **fail** for REQ-ONBOARD-001. The observer records it and the reason.
5. **Measures:** time to enter character mode, time to the first paid job, points of confusion (with the stage),
   hints that were triggered, recoveries used, and any defects found (recorded in
   [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) only after they are observed).
6. **Pass threshold:** at least four of five testers complete the session unaided, and no confusion point recurs for
   three or more testers. A soft target is completion within about 20 minutes; it is not a pass condition.
7. **Privacy:** testers consent to notes and any recording. Evidence is sanitized: no names, account names, or
   personal paths (RISK-030).
8. **Iteration:** fixes are re-tested with **new** fresh testers. A tester who has seen the tutorial is no longer
   fresh.

PHASE-13 repeats an end-to-end check with an independent tester, who installs the mod and completes the core
experience ([PRD](../product/PRD.md) section 9).

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-301 (new) | Minimum city content needed for character mode, the tutorial fare, and each career. |
| OPEN-302 (new) | How a first-time player discovers the entry point into character mode. |
| OPEN-015 | UI technology for the tutorial panels. |
| RISK-302 (new) | Onboarding validation may lack genuinely fresh testers. |
| RISK-019 | Economic softlock; the tutorial must teach the recovery paths. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
