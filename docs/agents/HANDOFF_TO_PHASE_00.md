# Handoff — TASK-0000 → PHASE-00 — 2026-10-18

| Field | Value |
|---|---|
| ID | DOC-HANDOFF-P00 |
| Document status | Draft (handoff record) |
| Date | 2026-10-18 |
| From (role) | Coordinator (documentation baseline; executed and self-validated in one session) |
| To (role) | Project owner (decision needed), then the Implementer or Coordinator who runs TASK-0001 and TASK-0002 |
| Task | [TASK-0000 — Documentation bootstrap](../planning/tasks/TASK-0000-documentation-bootstrap.md) |
| Task status at handoff | In review |
| Last updated | 2026-10-18 |

> **Update (2026-10-18, later the same day):** TASK-0001 and TASK-0002 have been executed read-only with the
> owner's authorization and are **In review**. The next step is the owner's review and the TASK-0003 authorization.
> See [PROJECT_STATUS](../PROJECT_STATUS.md) §5–§6 and the handoff sections in
> [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md) and
> [TASK-0002](../planning/tasks/TASK-0002-base-mod-source-audit.md).

> This is a **handoff record**, not an authorization. Nothing here starts PHASE-00 or any implementation. PHASE-00 is
> **Not started**, and no build or gameplay exists.

## Documentation result (what actually exists)

- The full manifest of 118 English Markdown documents exists, plus `.gitignore`, `.gitattributes`, and `.editorconfig`.
  [INDEX](../INDEX.md) lists every document with its purpose and reading priority.
- The scope is fixed as 93 requirements (90 Must, 3 Should) in the [PRD](../product/PRD.md). Each is traced to a
  specification, a phase, and a planned test in [TRACEABILITY](../planning/TRACEABILITY.md). Evidence is **Not run**
  for all of them.
- All 14 phase plans and the 6 milestones exist. Every phase is **Not started**.
- ADR-0001 to ADR-0004 are **Proposed**. The physics strategy (ADR-0004) remains undecided until the PHASE-03
  experiment.
- The register holds 154 entries (risks, open questions, conflicts) in
  [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
- `chat.md` (historical input) is unchanged.

## Documents read (by the Coordinator in this session)

| Document | Last updated (as seen) | Depth |
|---|---|---|
| Owner brief of 2026-10-18 (SRC-007) | 2026-10-18 | Full |
| [chat.md](../archive/chat.md) (SRC-006) | Pre-existing (56,022 bytes) | Full |
| [SOURCES](../research/SOURCES.md) and the research findings behind it | 2026-10-18 | Full (findings); spot-checked raw text for 10 facts |
| Writer reports for every document group | 2026-10-18 | Full |

## Work done

See [TASK-0000](../planning/tasks/TASK-0000-documentation-bootstrap.md) §6. Only documentation and the three
configuration files were created, under the owner brief of 2026-10-18. No pre-existing file was modified.

## Evidence

- Documentation checks DV-001 to DV-014: [DOCUMENTATION_VALIDATION_REPORT](../quality/DOCUMENTATION_VALIDATION_REPORT.md).
- Runtime evidence: **none, Not run**. No `RUN-*` record exists ([evidence](../quality/evidence/README.md)).
- Source facts checked against raw text on 2026-10-18 are marked as such in [SOURCES](../research/SOURCES.md).

## Open issues

**Decisions needed from the project owner, before or during PHASE-00:**

| Decision | Reference |
|---|---|
| Go-ahead for TASK-0001 (read-only discovery outside the project root, scoped as in the task file) | [TASK-0001](../planning/tasks/TASK-0001-windows-environment-audit.md) |
| Go-ahead for TASK-0002 (read-only online audit; no cloning into the project) | [TASK-0002](../planning/tasks/TASK-0002-base-mod-source-audit.md) |
| Whether and when to initialize Git, and whether to use a remote host | OPEN-029, RISK-701 |
| Where build outputs and local files live, given OneDrive sync | OPEN-028, RISK-011 |
| The project license (before incorporating any third-party code) | OPEN-027 |
| Whether to evaluate Melty at all: melty.gg lists neither CS2 nor Motor Town, and its terms claim exclusivity for published content | OPEN-016, CONF-008, CONF-012, OPEN-401 |
| Whether Motor Town needs to be installed | OPEN-030 |

**Technical unknowns that PHASE-00 starts answering:**

- OPEN-001 and OPEN-033: installed edition, version, and code-mod support.
- OPEN-002 and OPEN-406: toolchain components; the Unity version for the current game.
- OPEN-003: runtime target framework.
- OPEN-004 and OPEN-403 to OPEN-405: base-mod license history, build path, and provenance.
- OPEN-005 and OPEN-011: baseline build, run, and handback.
- OPEN-031: debugging on the retail game.
- OPEN-035: isolated test setup.

**Already known from the read-only source review (not runtime facts):**

- The base mod takes over live native vehicles and does not spawn a player car. Its README says building collision is
  not added (RISK-003; OPEN-012 and OPEN-402 remain ours to solve).
- Its project file targets `net48` and references game assemblies by a relative path, with no toolchain imports
  (RISK-601, RISK-403).

## Next step

**Project owner:** decide on the go-ahead for TASK-0001 and TASK-0002, which can run in parallel. The executing agent
then reads, in this order:

1. [AGENTS.md](../../AGENTS.md)
2. [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md)
3. [PROJECT_STATUS](../PROJECT_STATUS.md)
4. The task file
5. [PHASE-00](../planning/phases/PHASE-00-audit-and-windows.md)
6. [TOOLS_AND_PERMISSIONS](TOOLS_AND_PERMISSIONS.md)
7. For TASK-0001: [WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md) and [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md)
8. For TASK-0002: [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), and [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md)

[TASK-0003](../planning/tasks/TASK-0003-isolated-baseline-validation.md) (build and runtime baseline) comes only
after both audits and a separate, explicit authorization for setup and runtime operations.

## Authorization boundaries

- **May do without further approval:**
  - read project files;
  - improve documentation within the scope of an assigned task;
  - do bounded, read-only public research ([TOOLS_AND_PERMISSIONS](TOOLS_AND_PERMISSIONS.md)).
- **May not do:**
  - start TASK-0001 discovery outside the project root without the recorded go-ahead;
  - install anything;
  - launch CS2, Motor Town, or Melty;
  - enable mods;
  - build;
  - clone into the project;
  - initialize Git, commit, push, or publish;
  - touch saves or game files;
  - choose a license.
- **Needs owner authorization for the next steps:**
  - TASK-0001's scoped read-only discovery (and separately, registry reads);
  - every TASK-0003 operation (installs, obtaining the source outside the project, build, lab city, mod enablement,
    game launch);
  - any Melty session.
