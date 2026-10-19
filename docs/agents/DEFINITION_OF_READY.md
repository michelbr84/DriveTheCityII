# Definition of Ready

| Field | Value |
|---|---|
| ID | DOC-DOR |
| Document status | Draft |
| Scope | Every task file, all phases |
| Responsible role | Coordinator (applies it when moving a task from Planned to Ready); project owner approves changes |
| Related documents | [TPL-TASK](../templates/TASK_TEMPLATE.md), [tasks](../planning/tasks/README.md), [BACKLOG](../planning/BACKLOG.md), [DEFINITION_OF_DONE](DEFINITION_OF_DONE.md), [TOOLS_AND_PERMISSIONS](TOOLS_AND_PERMISSIONS.md) |
| Last updated | 2026-10-18 |

## 1. Purpose

A task may move from **Planned** to **Ready** only when someone other than its author could start it without guessing
scope, permissions, or what "finished" means. Readiness depends on the task type: a research task is Ready with an
unanswered question, while an implementation task needs stable contracts and a prepared environment.

## 2. Common criteria (every task type)

- [ ] The task file was created from [TPL-TASK](../templates/TASK_TEMPLATE.md) and contains no fill-in markers.
- [ ] The metadata names the task type (Research, Documentation, Implementation, Release), phase or work package,
      responsible role, dependencies, and the authorization required.
- [ ] There is **one** objective, stated so that a reviewer can tell whether it was met.
- [ ] Every dependency is Done, or the task file explains why it does not block.
- [ ] "Required reading" lists the specific documents and register entries ([READING_GUIDE](READING_GUIDE.md)).
- [ ] "Allowed scope" lists the files, areas, and operations; "Forbidden actions" lists the protected contracts.
- [ ] Acceptance criteria are verifiable, and the outputs (documents to update) are listed.
- [ ] Any owner authorization the task needs is either recorded in the task file with its date and scope, or the
      task stays Planned or Blocked. An agent cannot authorize itself.
- [ ] No file in "Allowed scope" is owned by another In progress task, and no other active task holds the same
      critical area ([ROLES_AND_OWNERSHIP](ROLES_AND_OWNERSHIP.md) §5).
- [ ] No open `CONF-*` entry affects the task's critical area.
- [ ] A recovery or rollback note exists for anything that is not purely additive.

## 3. Research tasks

A research task answers a question. **It does not need the answer in advance.** It is Ready when:

- [ ] The question is precise and tied to `OPEN-*` IDs in the [register](../planning/RISKS_AND_OPEN_QUESTIONS.md).
- [ ] A **safe verification method** is defined: read-only steps first (documentation, source reading, version
      checks), then any isolated experiment, each with its required permission.
- [ ] Permissions are explicit: which locations may be read (project root only, or named locations outside it with
      owner authorization), whether web reading is in scope, and that nothing is installed, cloned into the
      project, launched, or executed unless authorized.
- [ ] The output location is named: [SOURCES](../research/SOURCES.md), the relevant audit, register entries, or
      [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
- [ ] A stopping rule is defined: what counts as an answer, and how an unanswered question is recorded
      (Inconclusive, with what was tried and the next method).

Examples: [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md) and
[TASK-0002](../planning/tasks/TASK-0002-base-mod-source-audit.md).

## 4. Documentation tasks

- [ ] The target documents are listed with their IDs, and each file has exactly one owner for the task.
- [ ] The source of truth for the content is identified (PRD row, specification, ADR, evidence run, or owner message).
      Documentation never creates facts.
- [ ] The template to use is named, if the document is new.
- [ ] Changes to the PRD, ROADMAP, or a critical-area specification are flagged as requiring owner approval or a
      scoped Reviewer review.
- [ ] The validation method is stated (link check, ID check, language check), recorded later as `DV-*` entries where
      the Coordinator maintains them.

## 5. Implementation tasks

- [ ] The phase has started: its entry conditions are met and its predecessor gate was accepted by the owner
      ([ROADMAP.md](../../ROADMAP.md) §6). Exceptions are only the early parallel work listed in ROADMAP §5.
- [ ] The governing specification exists and covers the contract being implemented. If the relevant ADR is still
      Proposed, the task is explicitly a spike or experiment with a throwaway or decision-only outcome.
- [ ] Requirement IDs and `TEST-*` IDs are listed, with the test layer and the expected evidence.
- [ ] The environment is ready: tool versions confirmed in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md),
      a validated build procedure in [BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md), and owner authorization for
      building and any runtime operation.
- [ ] Version control exists (OPEN-029 resolved by the owner). Code changes without a repository are not Ready.
- [ ] For in-game tests: a lab city and an isolated mod set are available (OPEN-035,
      [TEST_STRATEGY](../quality/TEST_STRATEGY.md) §5).
- [ ] For critical areas: the Reviewer is named and the area is not held by another task.
- [ ] Any third-party code to be incorporated has passed the license audit
      ([REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md)) and the project license question (OPEN-027) has
      been decided by the owner.

## 6. Release tasks

- [ ] The release candidate is identified by version and build (and commit, once Git exists), and its evidence runs
      exist.
- [ ] For a public release: the [PHASE-13](../planning/phases/PHASE-13-release-and-maintenance.md) entry conditions
      are met (PHASE-12 gate accepted). A rehearsal on a private or test basis is labeled as such.
- [ ] The distribution channel's requirements were researched (OPEN-017) and the owner chose the channel or channels
      ([OPEN-701](../planning/RISKS_AND_OPEN_QUESTIONS.md)).
- [ ] The project license is decided (OPEN-027), and [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md) and
      [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) are complete for the candidate.
- [ ] A clean verification configuration without development tools and an independent tester are available
      (OPEN-507).
- [ ] The owner's authorization for publication, including test or private publications, is recorded.
- [ ] A support channel exists or the release notes say how problems will be handled
      ([OPEN-702](../planning/RISKS_AND_OPEN_QUESTIONS.md)).

## 7. Signals that a task is not Ready

- The objective contains "and" joining two deliverables, or words such as "investigate everything".
- It depends on an unanswered `OPEN-*` that is not itself the research subject.
- It needs an owner decision or authorization that is not recorded.
- It would edit a file or critical area already held by another task.
- Its acceptance criteria are "it works" or "compiles".
