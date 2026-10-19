# PHASE-11 — Presentation and accessibility

| Field | Value |
|---|---|
| ID | PHASE-11 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M4 — Refined and validated beta |
| Responsible role | Coordinator (phase lead); Implementer; QA (new-player sessions, accessibility checks); Reviewer (asset provenance, localization); project owner (PT-BR review or delegate, gate acceptance) |
| Related requirements | REQ-UI-001 – REQ-UI-004, REQ-ONBOARD-001, REQ-INPUT-001 – REQ-INPUT-003, REQ-L10N-001, REQ-ECON-005, REQ-DRIVE-004, REQ-ART-001, REQ-ART-002, REQ-AUDIO-001, REQ-PERF-002 — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Turn the working systems into one coherent experience. Consolidate the HUD, objectives, route and map view, wallet,
garage, company, ledger, and settings menus. Deliver the tutorial that lets a new player start exploring, rent a
vehicle, and complete a first job without the console or developer help. Add full remapping, gamepad support,
accessibility options, and the **English and Brazilian Portuguese (PT-BR)** interface. Finish animations and audio
using only own or authorized assets. Offer a hood or driver-position camera with documented limitations; a detailed
cockpit appears only for vehicle models that have a suitable interior. Street-level visual enhancements are optional,
measured, and off by default if they exceed the agreed budget.

## 2. Entry conditions

- The content being presented has passed its gates (PHASE-07 to PHASE-10). UI layout prototypes and string
  externalization may start earlier behind stable contracts ([ROADMAP](../../../ROADMAP.md) §5), but this phase's gate
  is judged only after PHASE-10 has passed.
- Asset provenance records exist for every asset to be shipped.
- A PT-BR reviewer is identified (the owner or a delegate the owner names).

## 3. Dependencies

- **Phases:** PHASE-07, PHASE-08, PHASE-09, PHASE-10.
- **Open questions:** OPEN-014 (input integration and remapping), OPEN-015 (UI technology), OPEN-025 (gamepad), OPEN-026
  (localization mechanism), OPEN-024 (street-level rendering options), OPEN-602 (independent new-player testers).

## 4. Required reading

[SPEC-UI](../../experience/UI_AND_HUD.md), [SPEC-ONBOARD](../../experience/ONBOARDING.md),
[SPEC-INPUT](../../experience/CONTROLS_AND_ACCESSIBILITY.md), [SPEC-L10N](../../experience/LOCALIZATION.md),
[SPEC-ART](../../experience/ART_AND_ANIMATION.md), [SPEC-AUDIO](../../experience/AUDIO.md),
[SPEC-DRIVING](../../gameplay/VEHICLE_DRIVING.md) (cameras), [SPEC-PERF](../../architecture/PERFORMANCE_BUDGET.md),
[SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md) (economic tutorial), [ASSET_PROVENANCE](../../legal/ASSET_PROVENANCE.md),
[BENCHMARK_PROTOCOL](../../quality/BENCHMARK_PROTOCOL.md), [VISION](../../product/VISION.md).

## 5. Deliverables

- A consolidated HUD, menus, feedback for blocked or failed actions, and a map or route view.
- The onboarding tutorial and the final economic tutorial.
- Remapping of every mod action; gamepad support for walking, driving, interactions, and menus.
- Accessibility options: UI scaling, captions for important audio cues, indicators that do not rely on color alone,
  hold and toggle alternatives.
- Complete English and PT-BR text, reviewed.
- Final animation set and audio, with provenance; remaining placeholders labeled.
- Hood or driver-position camera with documented limitations.
- Optional, measured street-level enhancements, off by default when over budget.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-11-01 | HUD consolidation | REQ-UI-001 | Speed, objective, route guidance, wallet, state-appropriate prompts. |
| WP-11-02 | Menus and feedback | REQ-UI-002, REQ-UI-003 | Career and jobs, garage, company, ledger, settings; blocked or failed actions always show a next step. |
| WP-11-03 | Map and route view | REQ-UI-004 | Destination and route on the city's roads. |
| WP-11-04 | Onboarding and economic tutorial | REQ-ONBOARD-001, REQ-ECON-005 | First exploration, rental, and first job; wallet, costs, income, recovery. |
| WP-11-05 | Remapping and gamepad | REQ-INPUT-001, REQ-INPUT-002, OPEN-014, OPEN-025 | Through the game's supported binding mechanism where possible; conflicts with game shortcuts handled per SPEC-INPUT. |
| WP-11-06 | Accessibility options | REQ-INPUT-003 | Checked against the SPEC-INPUT checklist. |
| WP-11-07 | English and PT-BR localization | REQ-L10N-001, OPEN-026 | All player-facing text externalized and translated; reviewed by the PT-BR reviewer; layout checked for longer strings. |
| WP-11-08 | Animation, audio, and placeholders | REQ-ART-001, REQ-ART-002, REQ-AUDIO-001 | Own or authorized sources only; provenance recorded in ASSET_PROVENANCE. |
| WP-11-09 | Hood or driver-position camera | REQ-DRIVE-004 | Documented limitations; cockpit only where a suitable interior exists. |
| WP-11-10 | Street-level visual options | REQ-PERF-002, OPEN-024 | Measured per BENCHMARK_PROTOCOL; off by default if over budget. |
| WP-11-11 | New-player sessions and gate report | Gate | Players who did not build the mod attempt the first job unaided (OPEN-602). |

## 7. Out of scope

Languages other than English and PT-BR. Detailed cockpits for every vehicle. Steering-wheel hardware support
(expansion). Interiors. New gameplay systems; only presentation, input, accessibility, and polish of existing ones.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-UI-001 | In-game | Full HUD | Not run |
| TEST-UI-002 | In-game | All menus | Not run |
| TEST-UI-003 | In-game | Feedback with next step for blocked or failed actions | Not run |
| TEST-UI-004 | In-game | Map or route view | Not run |
| TEST-ONBOARD-001 | In-game | New player completes the first job unaided | Not run |
| TEST-INPUT-001 | In-game | Every mod action remappable | Not run |
| TEST-INPUT-002 | In-game | Gamepad for walking, driving, interactions, menus | Not run |
| TEST-INPUT-003 | In-game, Review | Accessibility options | Not run |
| TEST-L10N-001 | Review, In-game | Complete EN and PT-BR text | Not run |
| TEST-ECON-005 | In-game | Final economic tutorial | Not run |
| TEST-DRIVE-004 | In-game | Hood or driver camera with documented limits | Not run |
| TEST-ART-001 | In-game | Final animation set or documented substitutes | Not run |
| TEST-ART-002 | Review | Remaining placeholders labeled | Not run |
| TEST-AUDIO-001 | Review | Audio sources own or authorized | Not run |
| TEST-PERF-002 | In-game | Visual options measured; off by default if over budget | Not run |

Definitions: [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md).

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *a new player completes their first work without console or
developer help.* Checked by recorded new-player sessions with players who did not implement the mod, in English and in
PT-BR. In addition: every shipped asset has a provenance record; remapping, gamepad, and accessibility checks pass; a
Reviewer and the project owner must accept the gate.

## 10. Risks and open questions

RISK-020 (input conflicts), RISK-015 (asset provenance and branding), RISK-008 (performance of visual options),
RISK-012 (scope creep toward cockpits and interiors), RISK-602 (human-operated sessions). Open questions: section 3.
All entries: [RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **New players get stuck.** Record where, revise the tutorial or feedback, and repeat with a new player (a player who
  has already seen the tutorial is no longer a new player).
- **Gamepad input cannot be read in the mod's context** (OPEN-025). Open a scope review of REQ-INPUT-002; do not
  remove it silently.
- **An asset lacks provenance.** Replace it or label it as a placeholder; it cannot ship without a record.
- **PT-BR text breaks layouts.** Fix the layouts, not by truncating meaning; re-review.

## 12. Evidence record

None yet. Runs will be recorded as `RUN-NNNN` under [docs/quality/evidence/](../../quality/evidence/README.md). New-player
session notes are sanitized before they are recorded (RISK-030).

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
