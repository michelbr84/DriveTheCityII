# Architecture Decision Records

| Field | Value |
|---|---|
| ID | DOC-ADR-INDEX |
| Document status | Draft |
| Scope | All architecture decisions for Drive the City II |
| Responsible role | Coordinator (maintainer); project owner accepts or rejects decisions |
| Related documents | [SPEC-ARCH](../ARCHITECTURE.md), [ADR template](../../templates/ADR_TEMPLATE.md), [Archive policy](../../archive/README.md), [DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md), [RISKS_AND_OPEN_QUESTIONS](../../planning/RISKS_AND_OPEN_QUESTIONS.md) |
| Last updated | 2026-10-18 |

## 1. Purpose

An Architecture Decision Record (ADR) captures **why** a significant technical choice was made: its context, the
options compared, the evidence, and the consequences. Specifications describe the resulting contracts; ADRs keep the
reasoning. As of 2026-10-18 every ADR is **Proposed**: none has been accepted, and nothing has been built or tested.

## 2. Index

| ADR | Title | Status | Decision summary | Pending on |
|---|---|---|---|---|
| [ADR-0001](ADR-0001-host-game-and-runtime.md) | Cities: Skylines II as the sole host and runtime | Proposed | Product direction owner-confirmed (2026-10-18); runtime technical details pending | OPEN-002, OPEN-003, OPEN-033, TASK-0001 to TASK-0003 |
| [ADR-0002](ADR-0002-control-authority.md) | Single movement authority and an explicit player state machine | Proposed | Explicit state machine with authority grants and one camera owner | PHASE-02 evidence (OPEN-011, OPEN-014, OPEN-107, OPEN-111) |
| [ADR-0003](ADR-0003-career-save-isolation.md) | Career isolation and revision-bound career saves | Proposed | Economic separation and revision-bound identity are owner requirements; storage mechanism pending | PHASE-01 spike (OPEN-006, OPEN-022, OPEN-101) |
| [ADR-0004](ADR-0004-physics-strategy.md) | Physics strategy for controlled vehicles and the avatar | Proposed | Pending — PHASE-03 experiment; no option pre-selected | PHASE-03 laboratory (OPEN-007, OPEN-008, OPEN-009, OPEN-032) |
| [ADR-0005](ADR-0005-mod-loading-and-distribution-route.md) | Mod loading and distribution route | Proposed | Native CS2 code mod with the official toolchain and the official distribution route; no BepInEx; Melty not a distribution channel | Owner acceptance; TASK-0003 evidence on game 1.6.2f1 (OPEN-017, OPEN-036) |

## 3. Status values

| ADR status | Meaning |
|---|---|
| Proposed | Written and open for review. May contain owner-confirmed requirements, but the decision as a whole is not accepted. |
| Accepted | The project owner accepted it, with the evidence cited in the ADR. Affected specifications have been updated. |
| Rejected | Considered and declined; kept for the record with the reason. |
| Superseded | Replaced by a later ADR, which it links to. Moved to the archive. |

ADR status is independent of document status and of validation status ([DOCUMENTATION_RULES](../../DOCUMENTATION_RULES.md)
section 5). An Accepted ADR can describe something not yet implemented.

## 4. Process

1. **Propose.** Copy the [ADR template](../../templates/ADR_TEMPLATE.md) to `ADR-NNNN-short-title.md` with the next
   free number. Fill in Context, Decision drivers, Options considered, Decision (or "Pending — …" with the experiment
   that will decide), Consequences, Validation / experiment, Affected specifications, and Follow-ups. Add a row to the
   index above. Link every open question it depends on to the
   [register](../../planning/RISKS_AND_OPEN_QUESTIONS.md).
2. **Review.** A Reviewer who did not write the ADR checks that the options are fairly described, that claims carry
   the right labels (**[Owner requirement]**, **[Documented upstream]**, **[Source-inspected]**, **[Proposal]**), and
   that any evidence cited exists as a recorded `RUN-*` ([evidence](../../quality/evidence/README.md)). ADRs touching
   control authority, the entity lifecycle, or the save format need Reviewer sign-off and no concurrent edits
   ([ROLES_AND_OWNERSHIP](../../agents/ROLES_AND_OWNERSHIP.md)).
3. **Owner acceptance.** Only the project owner (or a review the owner explicitly delegates) moves an ADR to Accepted.
   No acceptance is ever recorded without that decision.
4. **Update affected specifications** in the same change: each spec listed under "Affected specifications" is updated
   to the decided contract, the related `OPEN-*` entries are marked answered with date and evidence, and
   [TRACEABILITY](../../planning/TRACEABILITY.md) is updated if requirement coverage changes.
5. **Supersede and archive.** A decision is never edited into a different decision. A new ADR supersedes it; the old
   ADR's status becomes Superseded with a link to its replacement, and it is moved according to the
   [archive policy](../../archive/README.md). The index keeps a row for it.

## 5. When an ADR is required

- Choosing between technical approaches with lasting consequences (runtime, physics, storage, UI technology).
- Changing an invariant in [SPEC-CONTROL](../CONTROL_AUTHORITY.md), [SPEC-ENTITY](../ENTITY_LIFECYCLE.md), or
  [SPEC-SAVE](../SAVE_FORMAT.md).
- Adding a dependency on a third-party component or a new host capability.
- Any proposed write into native city state (see [SPEC-CITY](../CITY_INTEGRATION.md)).

Routine implementation choices inside an existing contract do not need an ADR.

## 6. Candidate future ADRs

These are expected but not yet written; each depends on open questions:

- UI technology for the HUD and menus (OPEN-015).
- Route and connectivity validation method (OPEN-019).
- Contact rule with native traffic and pedestrians (OPEN-032), if PHASE-03 shows it deserves its own record.
- Base-mod reuse versus independent re-implementation of vehicle control (OPEN-004, OPEN-005).
