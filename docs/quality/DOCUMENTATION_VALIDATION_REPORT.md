# Documentation Validation Report

| Field | Value |
|---|---|
| ID | DOC-DOCVAL |
| Document status | Draft (validation record of the 2026-10-18 baseline) |
| Scope | The documentation baseline produced by [TASK-0000](../planning/tasks/TASK-0000-documentation-bootstrap.md) |
| Responsible role | Coordinator (performed the checks; this is self-validation, not independent review) |
| Related documents | [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md), [INDEX](../INDEX.md), [TRACEABILITY](../planning/TRACEABILITY.md), [PROJECT_STATUS](../PROJECT_STATUS.md) |
| Last updated | 2026-10-18 |

> **This is documentation evidence, not gameplay QA.** It shows that the documents exist, link correctly, use
> consistent identifiers, and make no unsupported claims. It says nothing about whether the mod works. No mod exists,
> and runtime validation is **Not run**.

## 1. Method and tools

- **Automated checks:** a Python 3.13.15 validation script run against the project root. It is read-only and never
  modifies files. The script, and the generator used for [TRACEABILITY](../planning/TRACEABILITY.md), were kept in the
  session scratchpad **outside the project**. The brief does not authorize adding tooling to the project; a future
  task may propose a `tools/` validator with owner approval.
- **Targeted text scans:** `grep` and short Python scans for overclaim phrases, scope semantics, non-ASCII characters,
  and external URLs.
- **Manual review:** the Coordinator read all backbone documents and the writers' completion reports. It spot-read the
  most sensitive outputs (toolchain matrix, third-party notices, Melty evaluation, phase gates, transition and company
  semantics) and reconciled every inconsistency the writers reported.
- **Provenance checks:** ten key external facts were re-fetched as raw text with `curl` (read-only; nothing executed)
  and recorded as "raw text verified" in [SOURCES](../research/SOURCES.md).
- **Writers' own checks:** each of the seven writing agents ran its own link and ID check on its files before
  reporting. Those checks are superseded by the global run below.

**Snapshot validated (final run, 2026-10-18):**

- 122 files under the project root: 118 manifest documents, the three configuration files `.gitignore`,
  `.gitattributes`, and `.editorconfig`, and the unchanged historical input `chat.md`.
- 19,426 lines (1.51 MB) of active Markdown.

## 2. Results

| Check | What was checked | Method | Result |
|---|---|---|---|
| DV-001 | Every manifest file exists at its exact path, has an H1 title, and has more than minimal content (at least 1,800 bytes; 900 bytes for templates and the archive policy) | Script; manual sampling | **Passed.** 118 of 118 present; none flagged as too small. Limitation: substance is judged by size plus sampling, not by a full human read of every line. |
| DV-002 | Every relative Markdown link resolves inside the project | Script (code spans and blocks excluded) | **Passed.** 3,418 local links resolved; 0 broken; 0 escape the project root. |
| DV-003 | Heading anchors used in links exist | Script (GitHub-style slug approximation) | **Passed.** 19 anchors checked, 0 missing. Cross-file anchors are avoided by rule ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §2). Rendering differences between Markdown viewers were not tested. |
| DV-004 | Requirement IDs are unique, and every `REQ-*` reference is defined in the [PRD](../product/PRD.md) | Script | **Passed.** 93 unique requirements (90 Must, 3 Should); 0 undefined references. |
| DV-005 | Each `REQ-X-NNN` has its primary test `TEST-X-NNN`; every referenced test is defined; no test is defined twice | Script | **Passed.** 151 test IDs defined: 121 in TEST_SCENARIOS, 21 in SAVE_SAFETY_TESTS, 9 in BENCHMARK_PROTOCOL. 93 of 93 requirements have a primary test. 0 undefined references. |
| DV-006 | Register, source, feasibility, journey, expansion, task, ADR, and metadata IDs are unique and every reference resolves | Script | **Passed.** Register: 59 risks, 82 open questions (3 merged into primaries), 13 conflicts. Also 14 sources, 38 feasibility items, 12 journeys, 22 expansions, 4 task files, 4 ADRs; metadata IDs unique; only PHASE-00 to PHASE-13 referenced. |
| DV-007 | All 14 phases and 6 milestones are present and consistent | Script; manual comparison of gates with the ROADMAP | **Passed.** 14 phase documents, all with phase status "Not started"; M0–M5 in the ROADMAP. TRACEABILITY: 93 of 93 with specification, phase, and planned test; 0 with evidence. 126 work packages defined inside the phase documents. |
| DV-008 | Active documentation is in English | Script (Portuguese keyword heuristic); non-ASCII character scan | **Passed.** 0 Portuguese keyword hits. The only non-ASCII characters are typographic symbols (dashes, arrows, mathematical signs, box drawing); there are no accented letters. `chat.md` is excluded as a historical input. Limitation: heuristic check. |
| DV-009 | The canonical Windows path is used, and no conflicting project-path instruction remains | Script; scan for other absolute paths | **Passed.** Only the canonical root (and its Git Bash form) appears. Neither the legacy drive-D path nor its folder name appears. Registry key names in TASK-0001's not-yet-run discovery commands are not file paths. |
| DV-010 | Scope and semantics are consistent: bus passengers, company simulation, save contracts, phase gates, Melty's optional role | Targeted scans; manual reconciliation of writer reports | **Passed after fixes.** See §3. No document states that Melty is required, that native citizens ride as career passengers in v1.0, or that company fleets circulate physically. |
| DV-011 | Unknowns have register entries; there are no empty placeholders | Script; manual check | **Passed.** "TBD" appears only in rule statements that forbid it (5 occurrences, including this row). `<…>` placeholders appear only in templates, the local-environment example, and commands labeled as not-yet-run procedures. Each open-question entry states its question, impact, verification method, and resolving phase or task. |
| DV-012 | No proposed feature is reported as implemented; no fictitious evidence; no unverified command presented as proven | Script (claim labels, "Passed" cells, `RUN-` IDs, overclaim phrases) | **Passed.** Runtime claim labels appear only in their definitions and rules. No requirement validation is "Passed". No `RUN-*` record or folder exists. The 3 overclaim-phrase hits are negations or conditions. Commands are labeled verified, read-only discovery, or proposed procedure. |
| DV-013 | No unauthorized operation took place; the changed files match the brief | File inventory against the manifest; `chat.md` hash; operations log | **Passed.** No extra files and no `.git`, `src/`, `tests/`, `assets/`, `tools/`, `artifacts/`, or `.local/`. `chat.md` SHA-256 `e5b154df783bb6f7acbef356f6558bae82edd081e372cd247b6318a640b284ca` is unchanged, along with its size (56,022 bytes) and timestamp. See §4 for operations. |
| DV-014 | Private data and generated artifacts are excluded from prospective version control without blanket rules | Review of `.gitignore` | **Passed.** Ignores: `.local/`, `artifacts/`, `bin/`, `obj/`, IDE state, `node_modules/`, `__pycache__/`, and OS and Office temporary files. It does not ignore `docs/`, `*.md`, or any source folder; there are no blanket `*` or `*.dll` rules. No save, game binary, or extracted asset exists in the project. |

## 3. Problems found and fixed during validation

| Problem | Fix |
|---|---|
| The ROADMAP and PHASE-08 gate required a "tutorial" per career; this came from the historical input, is not in the owner brief, and has no requirement behind it. | Aligned the gate with the owner brief. Recorded CONF-013 and the proposed requirement OPEN-509. |
| The ten later phase documents phrased gate acceptance in the past tense ("the owner accepted the gate"), which read like approvals that never happened. | Rewritten as requirements ("must accept"). |
| REQ-REL-003's phase column implied incorporation in PHASE-00; REQ-CTRL-002 omitted PHASE-04. | Phase columns corrected; TRACEABILITY regenerated. |
| "Service vehicle" was ambiguous (native city service versus mod-provided). Mission reachability on foot was undefined. | Wording of REQ-ENTITY-002 and REQ-CAREER-002 clarified. SPEC-ENTITY now distinguishes company-owned vehicles from Service vehicles. |
| A save during an enter/exit transition was "deferred" in the gameplay spec but "persists the last stable state" in the control spec. | Gameplay spec aligned with INV-CTRL-08; deferral kept only as a proposal pending OPEN-101. |
| Overlay semantics and TransactionId derivation in the rules were too narrow for the architecture. | [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §12 updated. |
| Duplicate open questions from different writers (OPEN-201/OPEN-104, OPEN-208/OPEN-110, OPEN-602/OPEN-507). | Marked as merged, pointing to the primary entry. |
| Stale "historical input" wording after the source research (RISK-003, RISK-026, OPEN-003), and the misleading term "clean-room re-implementation". | Updated with the evidence level; renamed to "selective re-implementation with attribution". |
| Melty findings: the games are not listed, and the terms claim exclusivity. | Recorded as CONF-012, OPEN-401, RISK-401, and RISK-402. Melty stays optional and is not a distribution channel. |
| Validator false positives (`REQ-L10N` digits; URL schemes read as drive letters; an example ID in a code block). | Script corrected; example changed to a pattern. |

## 4. Operations performed during TASK-0000

- **Read-only checks:** `git --version`, `git rev-parse --show-toplevel` (not a repository), `python --version`,
  `node --version`, a directory listing of the project root and its parent folder, and the `chat.md` hash.
- **Read-only public research:**
  - web fetch and search by a research subagent;
  - `curl` GET of raw public text into the session scratchpad (base-mod LICENSE, commits, contents, project file,
    README, publish configuration, NOTICE; melty.gg home and terms; agent-tooling LICENSE and commits).
  - Nothing fetched was executed or copied into the project.
- **Writes:** only the 118 manifest documents and three configuration files under the project root, plus scratchpad
  files outside the project.
- **Not performed:** installs; game, Melty, or launcher launches; mod enablement; builds; cloning; Git initialization,
  commits, or pushes; publishing; access to saves, credentials, or unrelated folders.

## 5. Limitations and unresolved items

- **No independent review yet.** These checks were run by the Coordinator who produced the baseline with its writing
  agents. That is self-validation ([ROLES_AND_OWNERSHIP](../agents/ROLES_AND_OWNERSHIP.md)). TASK-0000 is therefore
  **In review**.
- **Mermaid diagrams were not rendered,** because no renderer is installed and installing one is not authorized. Their
  syntax was checked by eye only.
- **External links were not re-fetched at validation time.** Their status as of 2026-10-18 is in
  [SOURCES](../research/SOURCES.md). Many upstream facts are extraction-level only (RISK-406) and must be re-checked by
  TASK-0002.
- **Semantic consistency across about 19,400 lines** was checked through writer reports, targeted scans, and sampling.
  Subtle contradictions may remain. Report them as `CONF-*` entries when found.
- **Heuristic checks** (English, substance, anchors) can miss edge cases.
- All numeric gameplay values are illustrative proposals and are not balanced (OPEN-204, RISK-305).
- Decisions awaiting the project owner are listed in [PROJECT_STATUS](../PROJECT_STATUS.md) §5 and
  [HANDOFF_TO_PHASE_00](../agents/HANDOFF_TO_PHASE_00.md).

## 6. Later validation runs

| Date | Trigger | Result | Notes |
|---|---|---|---|
| 2026-10-18 | TASK-0001 and TASK-0002 execution, the Melty inspection, ADR-0005, TASK-0004, and the M1 refinement | **0 errors.** 120 of 120 manifest documents (ADR-0005 and TASK-0004 added); 3,534 local links and 19 anchors resolve; IDs are consistent; the register has 164 entries (62 risks, 88 open questions, 14 conflicts); 151 tests | 7 warnings, all expected: 5 occurrences of the forbidden placeholder word inside the rules that forbid it, and 2 hits on the observed Steam installation path of the game, which is not a personal path. The git-ignored `.local/LOCAL_ENVIRONMENT.md` is the only file outside the manifest, and it is expected. Sanitization checks found no account names, IDs, or user-profile paths in tracked documents. `chat.md` is unchanged (same SHA-256). |
