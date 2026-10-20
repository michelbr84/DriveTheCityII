# ADR-0001: Cities: Skylines II as the Sole Host and Runtime

| Field | Value |
|---|---|
| ID | ADR-0001 |
| ADR status | Proposed |
| Date proposed | 2026-10-18 |
| Decision owner role | Project owner (product direction); Coordinator with Reviewer (technical runtime details) |
| Related | REQ-REL-004, REQ-REL-001, REQ-COMPAT-001, [PRD](../../product/PRD.md), [SPEC-ARCH](../ARCHITECTURE.md), [SPEC-MODULES](../MODULE_CONTRACTS.md), [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), OPEN-002, OPEN-003, OPEN-004, OPEN-005, OPEN-033 |

## Context

Drive the City II puts a walking and driving career inside the player's own city. Several components were discussed
during planning ([chat.md](../../archive/chat.md), **[Historical input]**): Cities: Skylines II (CS2) as the city, the Beta
Test Driving Mod as a possible vehicle-control foundation, Motor Town as a career reference, and Melty as a possible
workflow or "mashup" launcher. Without a clear runtime decision, the project could drift toward exporting the city,
running two simulations side by side, or depending on tools the player does not need.

The owner brief of 2026-10-18 states that CS2 is the host game and the sole authority for the city and its native
simulation, that the mod runs inside it, that the core distribution requires only CS2 and documented mod
dependencies, and that Motor Town and Melty are not runtime dependencies **[Owner requirement]**.

Official documentation describes code mods that add their own systems to the game's update loop and can access game
entities **[Documented upstream]** ([SOURCES](../../research/SOURCES.md)). The concrete toolchain components and versions
(OPEN-002), the mod's runtime target framework (OPEN-003), and whether the owner's store edition supports code mods
(OPEN-033) are not yet verified on the owner's machine.

## Decision drivers

1. Respect the owner's product direction: the city is played where it was built.
2. One world authority: no synchronization between two simulations.
3. Distributable to players who have only CS2 (REQ-REL-004).
4. Legal clarity: no import of Motor Town code, assets, or physics; base-mod reuse only after a license audit.
5. Survivability across game updates (RISK-007): host-specific code isolated.
6. Testability of career logic without the game.

## Options considered

| Option | Description | Assessment |
|---|---|---|
| A. CS2 code mod as the sole runtime | All gameplay runs inside CS2 as a code mod; external projects are references or audited source only | Matches every driver. Risk: depends on host capabilities still unverified (see [SPEC-ARCH](../ARCHITECTURE.md) section 8). |
| B. Export the city to a separate engine | Extract roads and buildings and drive in another engine | Rejected by the owner. Loses the living simulation, duplicates the world, raises asset and legal issues. |
| C. Run alongside Motor Town or bridge to it | Use Motor Town's driving or career at runtime | Rejected by the owner. Two games, no shared world, no transferable API or code assumed. |
| D. Melty mashup as the primary runtime or distribution | Package the experience as a Melty mashup | Not chosen. Melty is optional and under evaluation ([MELTY_EVALUATION](../../research/MELTY_EVALUATION.md), OPEN-016); the project must work without it. |

## Decision

**Product direction:** Option A, CS2 as the sole host and runtime, is **owner-confirmed (2026-10-18)**.

**Technical runtime details: Pending.** The target framework (OPEN-003), toolchain components and versions (OPEN-002),
store-edition support (OPEN-033), and the base-mod reuse decision (OPEN-004, OPEN-005) are resolved by TASK-0001,
TASK-0002, and TASK-0003 and recorded in [TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md) and
[BASE_MOD_AUDIT](../../research/BASE_MOD_AUDIT.md). The ADR remains Proposed until those results exist and the owner
accepts them; no target framework or tool version is chosen here to fill the gap. Historical and community sources
mention a specific framework for CS2 code mods; that is an input to OPEN-003, not a decision.

## Consequences

- All host access is concentrated in the CityBridge module; other modules use our own types
  ([SPEC-MODULES](../MODULE_CONTRACTS.md)).
- Motor Town informs design documents only ([MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md)); no code,
  assets, or physics are imported.
- The mod's feasibility depends on host capabilities; each one has an open question and a resolving phase. Discovered
  infeasibility leads to a scope review, not a runtime change.
- Compatibility must be declared per game version ([COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md)); game
  updates are a standing risk (RISK-007).
- This mod and the original Beta Test Driving Mod must never load conflicting controllers simultaneously
  (REQ-COMPAT-001).

## Validation / experiment

| Step | Evidence required | Task | Validation |
|---|---|---|---|
| Discover installed game edition, version, and toolchain read-only | Sanitized discovery record | [TASK-0001](../../planning/tasks/TASK-0001-windows-environment-audit.md) | Not run |
| Audit base-mod source, license, dependencies, framework | Audit record | [TASK-0002](../../planning/tasks/TASK-0002-base-mod-source-audit.md) | Not run |
| Build and run the base mod in an isolated lab setup (owner-authorized) | `RUN-*` evidence | [TASK-0003](../../planning/tasks/TASK-0003-isolated-baseline-validation.md) | Not run |
| Run the mod skeleton without Motor Town or Melty present | TEST-REL-004 | [PHASE-01](../../planning/phases/PHASE-01-foundation-and-save-safety.md) | Not run |

## Affected specifications

[SPEC-ARCH](../ARCHITECTURE.md), [SPEC-MODULES](../MODULE_CONTRACTS.md), [PACKAGING_AND_PUBLISHING](../../release/PACKAGING_AND_PUBLISHING.md),
[TOOLCHAIN_MATRIX](../../engineering/TOOLCHAIN_MATRIX.md), [COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md).

## Follow-ups

- After TASK-0003: record the confirmed runtime details here and submit the ADR for review; it becomes Accepted only
  when the project owner accepts it.
- If the owner's edition cannot run code mods (OPEN-033), record a CONF entry and stop PHASE-00 at its gate.
- Revisit only through a new ADR that supersedes this one ([archive policy](../../archive/README.md)).
