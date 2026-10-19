# Definition of Done

| Field | Value |
|---|---|
| ID | DOC-DOD |
| Document status | Draft |
| Scope | Every task and every kind of result, all phases |
| Responsible role | Reviewer (applies it); Coordinator (maintainer); project owner approves changes |
| Related requirements | All `REQ-*` (a requirement is met only through Passed evidence, [PRD](../product/PRD.md) §8) |
| Related documents | [DEFINITION_OF_READY](DEFINITION_OF_READY.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md), [QA_CHECKLIST](../quality/QA_CHECKLIST.md), [evidence](../quality/evidence/README.md), [ROLES_AND_OWNERSHIP](ROLES_AND_OWNERSHIP.md) |
| Last updated | 2026-10-18 |

## 1. Principles

- **Done is specific.** Documentation, implementation, a build, runtime behavior, and a release each have their own
  criteria. Meeting one never implies another.
- **"Compiled" is not "done".** A successful build proves the code compiles, nothing more. **"Documented" is not
  "implemented"**, and a task Done is not a phase Passed.
- **No evidence, no claim.** Anything not executed is **Not run**. Labels such as **[Built locally]** and
  **[Tested in-game]** require a recorded `RUN-NNNN`.
- **Status updates are part of the work.** A task whose results are not reflected in the status documents is not Done.

## 2. Every task

- [ ] Each acceptance criterion is met with a reference to its evidence, or is explicitly recorded as not met (in
      which case the task is not Done).
- [ ] All listed outputs were updated; no file outside the allowed scope was changed.
- [ ] No forbidden or unauthorized operation was performed.
- [ ] Review is recorded: independent review where required, otherwise self-review labeled as such.
- [ ] Task file: status history row with the date; handoff notes written ([TPL-HANDOFF](../templates/HANDOFF_TEMPLATE.md)).
- [ ] [PROJECT_STATUS](../PROJECT_STATUS.md) and [CHANGELOG.md](../../CHANGELOG.md) describe what actually happened;
      [BACKLOG](../planning/BACKLOG.md) matches the task file.
- [ ] Answered `OPEN-*` entries carry the answer, date, and evidence; new unknowns are proposed as entries.

## 3. Documentation

- [ ] Metadata block present and accurate; document status no higher than Draft or In review unless the owner accepted it.
- [ ] Claim labels match provenance; nothing is presented as built, tested, compatible, or released without evidence.
- [ ] No template fill-in markers (angle-bracket placeholders) remain; they are allowed only in
      [templates](../templates/DOCUMENT_TEMPLATE.md).
- [ ] Every unknown is an `OPEN-*` entry with impact, verification method, and resolving phase or task. No "TBD".
- [ ] Links are relative, point to files that exist, and use no cross-file heading anchors. IDs referenced exist.
- [ ] English only; terminology matches [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §12.
- [ ] [INDEX](../INDEX.md) lists any new document; [TRACEABILITY](../planning/TRACEABILITY.md) reflects changed
      requirement links; superseded documents follow the [archive policy](../archive/README.md).
- [ ] Changes to the PRD, ROADMAP, or a critical-area specification have the required approval or scoped review.

## 4. Research

- [ ] The question is answered with evidence, or recorded as **Inconclusive** with what was tried and the next method.
- [ ] Every source is in [SOURCES](../research/SOURCES.md) with access status, access date, and revision.
- [ ] Findings keep **[Documented upstream]**, **[Source-inspected]**, and local observations separate.
- [ ] Commands are labeled as verified (with date), read-only discovery, or proposed procedure.

## 5. Implementation

- [ ] The change implements the specification's contract; behavior changes are reflected in the specification in the
      same task, with review.
- [ ] It follows [CODING_STANDARDS](../engineering/CODING_STANDARDS.md) and the module boundaries in
      [MODULE_CONTRACTS](../architecture/MODULE_CONTRACTS.md); host-game access stays inside CityBridge.
- [ ] Logic-layer tests exist for new pure logic and pass in a recorded run.
- [ ] The build criteria (section 6) and runtime criteria (section 7) are met for the targeted tests.
- [ ] Errors are contained and logged ([ERROR_RECOVERY](../architecture/ERROR_RECOVERY.md),
      [DEBUGGING](../engineering/DEBUGGING.md)); every mod-created entity has an owner and a cleanup path.
- [ ] Placeholder art and abstract simulations are labeled (REQ-ART-002, REQ-COMPANY-002).
- [ ] Incorporated third-party code or assets are recorded in the audit, notices, and provenance register.

## 6. Build

- [ ] The build was produced from a clean state with the documented procedure in
      [BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md), by a session authorized to build.
- [ ] Tool versions match [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md); warnings are recorded, not hidden.
- [ ] The output is identified by mod version and build ID (and commit, once Git exists).
- [ ] Neither the outputs nor the project contain game binaries, extracted game assets, saves, or local environment
      files (RISK-029).
- [ ] The build record is stored with the evidence. A Done build says nothing about runtime behavior.

## 7. Runtime behavior

- [ ] The targeted `TEST-*` entries were executed at their declared layer in a lab city with an isolated mod set,
      never on primary saves ([TEST_STRATEGY](../quality/TEST_STRATEGY.md) §5).
- [ ] Each run has a `RUN-NNNN` report ([TPL-TEST-REPORT](../templates/TEST_REPORT_TEMPLATE.md)) with game version,
      mod version and build, environment, procedure, expected and observed results, and sanitized logs.
- [ ] Every targeted test is **Passed**; failures have `ISSUE-NNN` entries in
      [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) and keep the task open.
- [ ] Changes touching persistence ran the relevant [SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md); changes
      that can affect frame time followed [BENCHMARK_PROTOCOL](../quality/BENCHMARK_PROTOCOL.md).
- [ ] [TRACEABILITY](../planning/TRACEABILITY.md) evidence cells link the run.

## 8. Critical-area changes (control, entities, save)

In addition to sections 5–7:

- [ ] A Reviewer who did not author or direct the change performed a **scoped review** of the affected invariants of
      [SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md), [SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md), or
      [SPEC-SAVE](../architecture/SAVE_FORMAT.md), for example: exactly one movement authority and one camera owner;
      cleanup and handback on every exit path, with no orphans or stale lane state; no career data leaking between save
      revisions, and no overwrite of the last valid copy.
- [ ] The critical-area regression suite ([TEST_STRATEGY](../quality/TEST_STRATEGY.md) §11) passed on this build.
- [ ] Self-review alone never closes a critical-area task.

## 9. Release acceptance

- [ ] The [PHASE-13](../planning/phases/PHASE-13-release-and-maintenance.md) gate items have evidence: license audit and
      notices complete; package verified per [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md); install,
      update, and clean exit tested per [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md);
      compatibility declaration drawn from [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md) evidence.
- [ ] An independent tester completed the intended experience on a supported configuration without development tools.
- [ ] No known critical defect involves save corruption, duplicated rewards, permanent loss of control, or
      uncompletable mandatory content.
- [ ] The project owner approved publication. Only then does [CHANGELOG.md](../../CHANGELOG.md) receive a version
      entry and the release documents drop their pre-release banners.

## 10. Task Done versus phase Passed

All tasks of a phase can be Done while the phase gate is still open. A phase passes only through the gate policy in
[ROADMAP.md](../../ROADMAP.md) §6: evidence against the gate, an independent Reviewer, and owner acceptance.
