# Document Template

| Field | Value |
|---|---|
| ID | TPL-DOCUMENT |
| Document status | Draft |
| Used for | New specifications (`SPEC-*`) and other substantive documents (`DOC-*`) |
| Responsible role | Coordinator (maintainer) |
| Related documents | [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md), [INDEX](../INDEX.md), [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md) |
| Last updated | 2026-10-18 |

## How to use

1. Copy the block below into a new file in the right folder, with an English name and no spaces.
2. Replace every `<…>` marker. Delete guidance lines that start with `> Guidance:`. **No marker may remain** in an
   active document; markers belong only in templates.
3. Compute relative links from the new file's location. Link to files, never to headings in other files.
4. Add the document to [INDEX](../INDEX.md) and, if it defines behavior for requirements, to
   [TRACEABILITY](../planning/TRACEABILITY.md). Requirement IDs are created only in the [PRD](../product/PRD.md).
5. Adapt sections sensibly for indexes, notices, or guides; do not add boilerplate that says nothing.

## Template

```markdown
# <Document title in English>

| Field | Value |
|---|---|
| ID | <SPEC-NAME or DOC-NAME from the INDEX manifest> |
| Document status | Draft |
| Release scope | <v1.0 / Expansion / Project-wide> |
| Phases | <PHASE-NN, PHASE-NN> |
| Responsible role | <Coordinator (maintainer); add "Reviewer sign-off required for changes (critical area)" only for SPEC-CONTROL, SPEC-ENTITY, SPEC-SAVE> |
| Related requirements | <REQ-AREA-NNN, REQ-AREA-NNN> |
| Related documents | <links to related specs, ADRs, tests, e.g. [SPEC-ENTITY](relative/path/ENTITY_LIFECYCLE.md)> |
| Last updated | <YYYY-MM-DD: the date of this change> |

> <Optional status reminder, e.g. "Nothing in this document is implemented or tested.">

## 1. Purpose
<Why this document exists and which decisions or behavior it owns. One paragraph.>

## 2. Scope and non-goals
- In scope: <…>
- Not in scope: <… with a link to where it is handled, or to EXPANSIONS if it is post-1.0>

## 3. Behavior / contracts
<States, rules, invariants, inputs and outputs, write ownership. Label provenance where it matters:
[Owner requirement], [Proposal], [Documented upstream], [Source-inspected], [Historical input], [Unverified].
Illustrative numbers are [Proposal] values, not measurements.>

## 4. Failure handling and recovery
<What can go wrong, how it is detected, the safe state, what the player sees, what is logged.>

## 5. Acceptance criteria
| Criterion | Requirement | Test | Validation |
|---|---|---|---|
| <observable, measurable criterion> | <REQ-AREA-NNN> | <TEST-AREA-NNN> | Not run |

## 6. Open questions and risks
- <OPEN-NNN — one-line question> (register: RISKS_AND_OPEN_QUESTIONS)
- <RISK-NNN — one-line risk>

## 7. Notes on provenance (optional)
<Sources by SRC-NNN with what each supports; what remains unverified.>
```

## Claim labels reminder

| Label | Use when |
|---|---|
| **[Owner requirement]** | The owner brief of 2026-10-18 (SRC-007) states it. |
| **[Proposal]** | It is our design proposal, not technically approved. |
| **[Historical input]** | It comes from [chat.md](../archive/chat.md) (SRC-006) and is unverified. |
| **[Documented upstream]** | A third-party source states it; we have not verified it. |
| **[Source-inspected]** | We read upstream files read-only; nothing was built. |
| **[Built locally]** / **[Tested in-game]** | Only with a recorded `RUN-NNNN`. Not usable as of 2026-10-18. |
| **[Unverified]** | An assumption awaiting verification; it needs an `OPEN-*` entry. |
