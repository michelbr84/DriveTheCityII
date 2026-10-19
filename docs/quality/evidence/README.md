# Evidence Records

| Field | Value |
|---|---|
| ID | DOC-EVIDENCE |
| Document status | Draft |
| Scope | Every real execution of a test, benchmark, baseline, or playtest for Drive the City II |
| Responsible role | QA (creates and maintains run records); Reviewer (reviews and signs them); Coordinator (propagates results to status documents) |
| Related documents | [TEST_STRATEGY](../TEST_STRATEGY.md), [TEST_SCENARIOS](../TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](../SAVE_SAFETY_TESTS.md), [BENCHMARK_PROTOCOL](../BENCHMARK_PROTOCOL.md), [TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md), [COMPATIBILITY_MATRIX](../COMPATIBILITY_MATRIX.md), [TRACEABILITY](../../planning/TRACEABILITY.md), [DEBUGGING](../../engineering/DEBUGGING.md) |
| Last updated | 2026-10-18 |

> **No runs exist.** As of 2026-10-18 nothing has been built or executed, so there are no `RUN-NNNN` folders. Creating
> a run folder for a planned, imagined, or simulated execution is forbidden. The first run folder appears only when a
> real execution starts (expected with [TASK-0003](../../planning/tasks/TASK-0003-isolated-baseline-validation.md),
> which needs owner authorization).

## 1. Purpose

This folder holds the evidence that turns a test's validation status from **Not run** into **Passed**, **Failed**, or
**Inconclusive**. Phase gates, [PROJECT_STATUS](../../PROJECT_STATUS.md), the [COMPATIBILITY_MATRIX](../COMPATIBILITY_MATRIX.md),
and the evidence column of [TRACEABILITY](../../planning/TRACEABILITY.md) may cite only run records stored here.

**Documentation checks are not runs.** Validation of the documentation itself (links, IDs, language) is recorded as
`DV-NNN` entries in [DOCUMENTATION_VALIDATION_REPORT](../DOCUMENTATION_VALIDATION_REPORT.md), not in this folder.

## 2. What a run is

- A **run** (`RUN-NNNN`) is one execution session of one or more tests on **one fixed configuration**: game version,
  store edition, DLC set, mod build, mod set, lab city master, and machine. If any of these change, a new run starts.
- Numbers are four digits, sequential across the whole project, assigned when the run starts, and never reused. An
  aborted run keeps its number and records the reason, with the result **Inconclusive**.
- Several tests may share a run (for example a campaign such as TEST-TRANS-101). Each test gets its own result inside
  the run.
- Baseline observations of the candidate base mod (PHASE-00) are runs too. Their "mod commit" field cites the pinned
  upstream revision.

## 3. Folder layout

```text
docs/quality/evidence/                (versioned once a repository exists; OPEN-029)
├── README.md                         this file
└── RUN-NNNN/                         one folder per real execution (none exist yet)
    ├── REPORT.md                     the run record, created from TPL-TEST-REPORT
    ├── logs/                         short, sanitized log excerpts only
    ├── data/                         small sanitized text data: counts, ledger extracts, benchmark summaries
    └── images/                       a few sanitized screenshots, only when essential

artifacts/evidence/RUN-NNNN/          git-ignored; never versioned (location: OPEN-028)
├── recordings/                       screen recordings
├── logs/                             full logs and diagnostics bundles
├── captures/                         raw benchmark captures, dumps
└── fixtures/                         lab save copies kept as evidence, quarantined or corrupted data
```

The `artifacts/` area sits at the project root and is git-ignored by the root `.gitignore` created in the
documentation baseline (2026-10-18). No Git repository exists yet (OPEN-029), so confirm that the rule is still present
before storing anything there. Because the project root is inside a OneDrive-synced folder, the project owner may relocate it outside the synced area (OPEN-028, RISK-011). The
run record then names the relocated root by a placeholder, never by a personal path.

## 4. Required content of `REPORT.md`

Create `REPORT.md` from [TPL-TEST-REPORT](../../templates/TEST_REPORT_TEMPLATE.md). It must contain:

| Field | Content |
|---|---|
| Run | `RUN-NNNN` |
| Date | The actual date of execution (ISO format) |
| Tests | Every TEST ID covered, with the requirement(s) and phase |
| Game version | Exactly as reported by the game; store edition; DLC set |
| Mod commit / version | Our mod's commit and version, or the candidate base mod's pinned revision for baselines |
| Environment | Sanitized hardware profile, Windows version, GPU driver version, relevant settings; mod set with versions; lab city master name, version, and hash |
| Authorization | Reference to the owner's authorization for the runtime operations performed |
| Scenario | Setup and scenario-matrix cells executed |
| Procedure | The procedure followed, with any deviation stated explicitly |
| Expected | The expected result copied from the test definition |
| Observed | What actually happened, factually, including anomalies |
| Result | Passed / Failed / Inconclusive for each test, with the scope of the result |
| Logs and recordings | References to versioned excerpts and to artifacts (see §6) |
| Issues | `ISSUE-NNN` entries opened or updated |
| Executed by | Role (QA, Implementer, …) and an alias agreed with the project owner; real names only with the person's consent |
| Reviewer | Name or alias and role of the reviewer, the review date, and whether the review is independent or a self-review |

**Before a repository exists (OPEN-029):** a run of our own mod cannot cite a commit. It records a build identifier and
a reference to the source snapshot, and is marked "not reproducible from version control". **[Proposal]** Such runs can
inform development but should not serve as phase-gate evidence.

## 5. What is versioned and what is not

| Category | Where | Examples |
|---|---|---|
| Versioned (once a repository exists) | `docs/quality/evidence/RUN-NNNN/` | `REPORT.md`; short sanitized log excerpts; small text data (counts, ledger extracts, benchmark summaries); a few sanitized screenshots |
| Stored, not versioned | `artifacts/evidence/RUN-NNNN/` | Videos; full logs; crash dumps; raw benchmark captures; diagnostics bundles; lab save copies; quarantined or corrupted data |
| Never stored in the project | Nowhere in the project | The owner's primary saves; game binaries or extracted game assets (RISK-029); credentials; unsanitized personal data |

A maximum size for versioned evidence files is set when the repository is initialized (OPEN-029). Until then: if a file
is not plain text, or if in doubt, store it as an artifact.

## 6. Durable references to artifacts

Artifacts are not versioned, so the run record must identify them unambiguously. For each artifact, `REPORT.md` lists:

| Artifact | Path relative to the artifacts root | SHA-256 | Size | Sanitized | Retention |
|---|---|---|---|---|---|

- The path is relative (for example `evidence/RUN-NNNN/recordings/<file>`), never an absolute personal path.
- The hash lets a reviewer confirm that the artifact is the one referenced.
- **Retention [Proposal]:** keep artifacts at least until the phase gate that relies on them is accepted; keep
  artifacts that support a release until that release is superseded. If an artifact is lost, the run record says so,
  and the result stands only on the remaining evidence.

## 7. Sanitization rules

Apply these before anything is versioned or shared. They complement [DEBUGGING](../../engineering/DEBUGGING.md) and
[SECURITY.md](../../../SECURITY.md) (RISK-030).

1. **Paths:** replace user-profile paths with placeholders such as `<USERPROFILE>`, the game installation with
   `<GAME_DIR>`, the game's user-data folder with `<GAME_USERDATA>`, and the project root with `<PROJECT_ROOT>`.
2. **Identities:** remove Windows user names, account and display names, e-mail addresses, store and platform account
   IDs, machine names, IP addresses, and hardware serial numbers.
3. **Screens:** before recording, close or hide overlays that show accounts, friends, or notifications. Record without
   microphone audio. Crop or blur anything identifying before versioning a screenshot.
4. **Cities:** lab cities use neutral names. A copy of an owner city used as a lab master is renamed if its name is
   personal.
5. **Logs:** never version a raw log. Version only reviewed excerpts, preferably from the mod's sanitized diagnostics
   bundle (REQ-DIAG-001).
6. **Saves:** never version a save file.
7. **Machine-specific values** belong in the git-ignored local environment file described in
   [LOCAL_ENVIRONMENT.example](../../engineering/LOCAL_ENVIRONMENT.example.md), not in run records.
8. **When in doubt,** keep the item as an artifact only and ask the project owner.

`REPORT.md` includes a short sanitization checklist confirming rules 1–6 were applied.

## 8. Workflow

1. Check the entry criteria in [TEST_STRATEGY](../TEST_STRATEGY.md) §9. Assign the next `RUN-NNNN` and create its
   folder only now, when the real execution starts.
2. Fill in the environment fields before executing.
3. Execute the procedure and record observations as they happen, including deviations.
4. Store artifacts and compute their hashes.
5. Sanitize and complete the checklist.
6. Record a result for each test. File `ISSUE-NNN` entries in [KNOWN_ISSUES](../KNOWN_ISSUES.md) for defects.
7. Request review. Gate evidence needs a Reviewer who did not implement the change; a self-review is labeled as such
   ([ROLES_AND_OWNERSHIP](../../agents/ROLES_AND_OWNERSHIP.md)).
8. After review, propagate the result: the run index below, the [TRACEABILITY](../../planning/TRACEABILITY.md) evidence
   cell, [COMPATIBILITY_MATRIX](../COMPATIBILITY_MATRIX.md) rows, [BENCHMARK_PROTOCOL](../BENCHMARK_PROTOCOL.md) results,
   the phase document's evidence record, and [PROJECT_STATUS](../../PROJECT_STATUS.md).

**Integrity:** after review, `REPORT.md` is not rewritten. Corrections are appended with their date and reason. Failed
and inconclusive runs are kept; they are evidence too.

## 9. Run index

| RUN | Date | Tests | Phase | Result | Reviewer |
|---|---|---|---|---|---|
| — | — | — | — | No runs recorded | — |
