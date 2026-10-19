# Test Report Template

| Field | Value |
|---|---|
| ID | TPL-TEST-REPORT |
| Document status | Draft |
| Used for | Evidence run reports (`RUN-NNNN`) for Logic, Integration, In-game, Review, and benchmark runs |
| Responsible role | QA (maintainer) |
| Related documents | [evidence](../quality/evidence/README.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md), [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md), [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) |
| Last updated | 2026-10-18 |

## How to use

- Create a report **only when a run actually happens**. Never create a report in advance or for a run that did not
  take place. Store it where the [evidence README](../quality/evidence/README.md) specifies.
- One report per run; a run may cover several tests. Assign the next free `RUN-NNNN` number.
- Record failed and inconclusive results as faithfully as passes. A defect gets an `ISSUE-NNN` entry.
- Sanitize everything before storing it: no personal data, credentials, or user-folder paths
  ([SECURITY.md](../../SECURITY.md) §2). Raw captures stay in git-ignored `artifacts/`.
- Runtime runs use lab cities and isolated mod sets only, never primary saves.

## Template

```markdown
# RUN-<NNNN> — <Short scenario name>

| Field | Value |
|---|---|
| Run ID | RUN-<NNNN> |
| Date | <YYYY-MM-DD: the actual date of execution> |
| Game version | <exact version string as reported by the game, plus store edition and DLC set> |
| Mod commit / version | <mod version and build ID; commit hash once Git exists; for PHASE-00 the pinned upstream revision> |
| Environment | <sanitized: Windows version, hardware class, other active mods with versions, lab city name/version/hash> |
| Tests covered | <TEST-AREA-NNN, …> |
| Requirements | <REQ-AREA-NNN, …> |
| Layer | <Logic / Integration / In-game / Review / Benchmark> |
| Executed by (role) | <QA / Implementer> |
| Owner authorization | <reference to the task status-history row that authorized the runtime operations> |
| Result | <Passed / Failed / Inconclusive> |
| Reviewer | <role and identity of a reviewer who did not implement the change; "Not reviewed yet" until done> |

## Scenario
<What is being tested and why; scenario-matrix cells covered (surface, traffic, frame-rate condition, vehicle class,
exit condition, time/overlay state); cells not covered and why.>

## Procedure
1. <Exact steps as executed, including starting state (fresh working copy of the lab city master)>

## Expected result
<From the test definition, quoted or referenced by TEST ID.>

## Observed result
<What actually happened, step by step where it differs. Measurements with units. No interpretation here.>

## Result and scope
<Passed / Failed / Inconclusive, and exactly what the result covers and does not cover.>

## Logs and recordings
| Artifact | Location | Sanitized |
|---|---|---|
| <log, recording, ledger dump, screenshot> | <path under artifacts/ or evidence folder> | <Yes / Not applicable> |

## Deviations and issues
<Deviations from the procedure; ISSUE-NNN entries filed; suspended steps and the reason.>

## Review
<Reviewer's verdict, date, and notes. Self-review is labeled "Self-review" and is not independent approval.>
```
