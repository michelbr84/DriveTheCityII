# Phase Template

| Field | Value |
|---|---|
| ID | TPL-PHASE |
| Document status | Draft |
| Used for | Phase documents `docs/planning/phases/PHASE-NN-short-name.md` |
| Responsible role | Coordinator (maintainer) |
| Related documents | [ROADMAP.md](../../ROADMAP.md), [phases](../planning/phases/README.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md), [DEFINITION_OF_DONE](../agents/DEFINITION_OF_DONE.md) |
| Last updated | 2026-10-18 |

## How to use

- Phase order, titles, dependencies, and milestones come from [ROADMAP.md](../../ROADMAP.md); a phase document never
  changes them. The sections below are the standard phase structure and must all be present.
- Replace every `<…>` marker. Quantities in gates are labeled as proposed acceptance criteria, never as results.
- Phase status changes only after the gate review and the owner's acceptance (ROADMAP §6).

## Template

```markdown
# PHASE-<NN> — <Phase title exactly as in ROADMAP.md>

| Field | Value |
|---|---|
| ID | PHASE-<NN> |
| Document status | Draft |
| Phase status | Not started |
| Milestone | <M0–M5 and its name, e.g. "M1 — Playable walking/driving sandbox"> |
| Responsible role | <Coordinator (planning); Implementer; QA (evidence); Reviewer (gate check); project owner (gate acceptance)> |
| Related requirements | <REQ-AREA-NNN, REQ-AREA-NNN> |
| Last updated | <YYYY-MM-DD> |

## 1. Objective
<What this phase proves, consistent with the ROADMAP exit gate.>

## 2. Entry conditions
- <Predecessor gate accepted by the project owner, with date and evidence reference>
- <Owner authorizations needed before work starts>

## 3. Dependencies
<Phases (per ROADMAP), ADRs, OPEN-* answers, tools confirmed in TOOLCHAIN_MATRIX.>

## 4. Required reading
<Ordered list of documents with relative links; tasks in this phase copy and extend it.>

## 5. Deliverables
<Code modules, specification updates, ADR decisions, tests, evidence runs.>

## 6. Proposed work packages
| WP | Title | Objective | Depends on | Critical area |
|---|---|---|---|---|
| WP-<NN>-01 | <…> | <one objective> | <…> | <SPEC-CONTROL / SPEC-ENTITY / SPEC-SAVE / None> |

## 7. Out of scope
<Items explicitly excluded, with the phase or expansion that handles them.>

## 8. Tests
| Test | Layer | Requirement | Validation |
|---|---|---|---|
| <TEST-AREA-NNN> | <Logic / Integration / In-game> | <REQ-AREA-NNN> | Not run |

## 9. Acceptance gate
<Measurable criteria. Label quantities "[Proposal]". Name the evidence required, the independent Reviewer check,
and the owner's acceptance.>

## 10. Risks and open questions
- <RISK-NNN / OPEN-NNN — one-line summary> (register: RISKS_AND_OPEN_QUESTIONS)

## 11. Recovery (if the phase fails)
<What to do if the gate fails: fallbacks, spikes, scope review (never a silent removal of a requirement).>

## 12. Evidence record
<None yet. Runs are recorded as RUN-NNNN under docs/quality/evidence/ with TPL-TEST-REPORT and listed here
with date, scope, and result as they actually occur.>
```
