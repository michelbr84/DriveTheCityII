# ADR-0002: Single Movement Authority and an Explicit Player State Machine

| Field | Value |
|---|---|
| ID | ADR-0002 |
| ADR status | Proposed |
| Date proposed | 2026-10-18 |
| Decision owner role | Coordinator with Reviewer (critical area); project owner accepts |
| Related | REQ-CTRL-001 to REQ-CTRL-004, REQ-TRANS-001 to REQ-TRANS-004, REQ-ENTITY-001, REQ-ENTITY-002, [SPEC-CONTROL](../CONTROL_AUTHORITY.md), [SPEC-ENTITY](../ENTITY_LIFECYCLE.md), OPEN-011, OPEN-014, OPEN-107, OPEN-111, RISK-004, RISK-020 |

## Context

The mod adds a player who walks, enters vehicles, drives, and returns to city building, inside a game whose native AI
already moves vehicles and whose building camera and tools already consume input. The classic failure modes are known
from the planning history and from the candidate base mod's change notes: two systems writing the same vehicle (jitter,
stuck traffic, stale lane reservations, crashes), the building camera and the character camera fighting, and keys
that both drive and trigger city tools **[Historical input]**, **[Source-inspected, pending re-check in TASK-0002]**
([BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md)).

The owner brief requires one movement authority; explicit `Building`, `OnFoot`, `EnteringVehicle`, `Driving`,
`ExitingVehicle`, and `Recovery` states; overlays for pause, menus, loading, and unloading; entry and exit guards;
controller and camera ownership; and cleanup **[Owner requirement]**.

## Decision drivers

1. Native AI and the player controller never write the same vehicle simultaneously (REQ-CTRL-001).
2. Every state and transition is explicit, testable, and reviewable (REQ-CTRL-002).
3. Input and camera separation between character mode and building mode (REQ-CTRL-003).
4. Return to `Building` is always possible and restores everything (REQ-CTRL-004).
5. Safe abort of transitions when a vehicle disappears (REQ-TRANS-003).
6. A design that survives faults: every exit path cleans up.

## Options considered

| Option | Description | Assessment |
|---|---|---|
| A. Explicit state machine with authority grants | One state machine owns player state; per-entity movement-authority grants and one camera owner are issued and revoked only by it; transitions are step lists with compensations | Meets all drivers; more upfront structure; requires discipline that controllers write only under a grant |
| B. Implicit flags per subsystem | Each subsystem keeps its own "is driving" or "camera active" flags | Simple at first; produces contradictory states and missed cleanup as features grow; hard to test |
| C. Co-control | Player input is blended into native AI steering, or both write and the last write wins | Conflicts by design; native state corruption and jitter (RISK-004); violates REQ-CTRL-001 |
| D. Steer through native navigation targets | Player input sets navigation targets that native AI then follows | Keeps one writer (native), but handling is limited by native behavior; remains a candidate implementation inside option A for some vehicle behaviors, decided with ADR-0004 |

## Decision

**Proposed:** Option A. The ControlAuthority state machine in the Player module is the only issuer of movement-authority
grants and camera ownership; controllers write only while holding a grant; a write-verify check detects any other
writer; every state registers its resources and releases them on every exit. Details, transition table, and invariants
INV-CTRL-01 to INV-CTRL-14 are in [SPEC-CONTROL](../CONTROL_AUTHORITY.md). Option D is not excluded as a movement
technique within a grant; the choice of driving model belongs to [ADR-0004](ADR-0004-physics-strategy.md).

## Consequences

- Positive: one place to reason about who moves what; invariants are checkable at runtime and in tests; fault
  containment can always return to `Building` ([SPEC-RECOVERY](../ERROR_RECOVERY.md)).
- Negative: every new interaction must be modeled as a state or transition; controllers cannot take shortcuts.
- Native vehicles are touched only in sandbox mode and only through the handback protocol in
  [SPEC-ENTITY](../ENTITY_LIFECYCLE.md); career mode uses only career vehicles (REQ-ENTITY-002).
- Host capabilities for suspending native movement, update ordering, input suppression, and camera takeover remain
  unverified (OPEN-011, OPEN-107, OPEN-014, OPEN-111); if one is unavailable, the affected feature goes to scope review.

## Validation / experiment

| Step | Evidence | Phase | Validation |
|---|---|---|---|
| State skeleton with overlays; illegal transitions rejected | TEST-CTRL-002 | [PHASE-01](../../planning/phases/PHASE-01-foundation-and-save-safety.md) | Not run |
| One-car takeover, drive, handback without dual writes; write-verify active | TEST-CTRL-001, TEST-ENTITY-001 | [PHASE-02](../../planning/phases/PHASE-02-basic-driving.md) | Not run |
| Input and camera separation | TEST-CTRL-003 | PHASE-02 | Not run |
| Return to `Building` from every stable state | TEST-CTRL-004 | [PHASE-05](../../planning/phases/PHASE-05-walk-drive-integration.md) | Not run |
| Proposed 50 transition cycles across the scenario matrix | TEST-TRANS-001 to TEST-TRANS-004 | PHASE-05 | Not run |

The owner may accept this ADR after PHASE-02 evidence shows a clean takeover and handback.

## Affected specifications

[SPEC-CONTROL](../CONTROL_AUTHORITY.md), [SPEC-ENTITY](../ENTITY_LIFECYCLE.md), [SPEC-TRANSITION](../../gameplay/WALK_DRIVE_TRANSITIONS.md),
[SPEC-PLAYER](../../gameplay/PLAYER_AND_CAMERAS.md), [SPEC-INPUT](../../experience/CONTROLS_AND_ACCESSIBILITY.md),
[SPEC-MODULES](../MODULE_CONTRACTS.md).

## Follow-ups

- PHASE-02: record the observed takeover, ordering, and handback mechanisms in
  [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md) and update SPEC-CONTROL.
- Owner decision on sandbox exposure in player builds (OPEN-110).
- Any change to this decision requires a superseding ADR ([archive policy](../../archive/README.md)).
