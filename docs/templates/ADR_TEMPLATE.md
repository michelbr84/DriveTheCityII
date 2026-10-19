# ADR Template

| Field | Value |
|---|---|
| ID | TPL-ADR |
| Document status | Draft |
| Used for | Architecture decision records `docs/architecture/decisions/ADR-NNNN-short-slug.md` |
| Responsible role | Coordinator (maintainer) |
| Related documents | [ADR index](../architecture/decisions/README.md), [ARCHITECTURE](../architecture/ARCHITECTURE.md), [archive policy](../archive/README.md) |
| Last updated | 2026-10-18 |

## How to use

- Use the next free `ADR-NNNN` number and add the ADR to the [ADR index](../architecture/decisions/README.md).
- A new ADR starts as **Proposed**. Only the project owner (or a review the owner explicitly delegates) makes it
  Accepted. An Accepted ADR must update every affected specification in the same change.
- Do not choose an option to fill a gap. If evidence is missing, write "Pending" with the experiment that will decide
  it (as ADR-0004 does for the physics approach).
- A superseded ADR keeps its ID, gets the status Superseded with a link to its replacement, and follows the
  [archive policy](../archive/README.md).

## Template

```markdown
# ADR-<NNNN> — <Decision title>

| Field | Value |
|---|---|
| ID | ADR-<NNNN> |
| ADR status | Proposed |
| Date proposed | <YYYY-MM-DD> |
| Decision owner role | <Project owner / Coordinator with owner acceptance> |
| Related | <REQ-*, SPEC-*, OPEN-*, RISK-*, other ADR-* IDs with relative links> |

## Context
<The problem, the constraints (owner requirements, host-game limits, evidence so far), and why a decision is needed now.>

## Decision drivers
- <Driver, e.g. "exactly one movement authority (REQ-CTRL-001)">
- <Driver, e.g. "testable without CS2">

## Options considered
| Option | Summary | Pros | Cons | Evidence level |
|---|---|---|---|---|
| <A> | <…> | <…> | <…> | <Documented upstream / Source-inspected / Unverified / RUN-NNNN> |
| <B> | <…> | <…> | <…> | <…> |

## Decision
<The chosen option and why, or "Pending — <experiment or evidence needed, owner phase/task>".>

## Consequences
<What becomes easier or harder; obligations created; what must not be done from now on.>

## Validation / experiment
<How the decision is or will be validated: TEST-* IDs, spike description, success criteria, RUN-NNNN once real.>

## Affected specifications
<Specs that must change when this ADR is accepted, with relative links.>

## Follow-ups
- <Task, OPEN-* entry, or review needed>
```
