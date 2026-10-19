# Changelog

| Field | Value |
|---|---|
| ID | DOC-CHANGELOG |
| Document status | Draft (living document) |
| Responsible role | Coordinator (maintainer); every completed task adds its real changes |
| Last updated | 2026-10-18 |

This file records changes that were **actually made**. It never lists planned features as done, and no version of the
mod has been released. Version headings will appear only when real releases exist
([PACKAGING_AND_PUBLISHING](docs/release/PACKAGING_AND_PUBLISHING.md)).

## [Unreleased]

### Changed — 2026-10-18 — PHASE-00 audits (TASK-0001, TASK-0002) and Melty check

- **TASK-0001 (read-only Windows discovery), now In review.**
  - Recorded Steam CS2 **1.6.2f1** (Unity 2022.3.71f1, Mono), the installation and user-data locations, **no
    installed mods and no loader** in the game folder, the **missing** official toolchain (no Unity Editor, no .NET
    SDK), and the installed Visual Studio products.
  - Results are in [WINDOWS_SETUP](docs/engineering/WINDOWS_SETUP.md) and
    [TOOLCHAIN_MATRIX](docs/engineering/TOOLCHAIN_MATRIX.md). Private values are in the git-ignored
    `.local/LOCAL_ENVIRONMENT.md`.
  - Proposed an isolated test setup for owner approval.
- **TASK-0002 (read-only base-mod source audit at pinned `eefd6bb…`), now In review.**
  - Recorded a native code mod with no BepInEx or Harmony.
  - Recorded the license history: restrictive before `af35523…`, MIT after.
  - Recorded collision-provenance questions, a declared game version of 1.5.\*, and no on-foot or entry/exit features.
  - Recommendation **[Proposal]**: baseline and technique source, not a fork.
  - Updated [BASE_MOD_AUDIT](docs/research/BASE_MOD_AUDIT.md), [REUSE_AND_LICENSE_AUDIT](docs/legal/REUSE_AND_LICENSE_AUDIT.md),
    SOURCES, DEPENDENCIES, UPSTREAM_STRATEGY, and FEASIBILITY_MATRIX.
- **Melty inspected read-only** through its connection to Claude Code.
  - CS2 is supported only through BepInEx 5 installed into the game folder.
  - Native CS2 mods cannot be installed one-click by Melty.
  - Motor Town is not required.
  - See [MELTY_EVALUATION](docs/research/MELTY_EVALUATION.md) §9. Nothing was created, uploaded, tested, or published.
- **Added:**
  - [ADR-0005](docs/architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md) (Proposed): native mod route,
    no BepInEx, Melty not a distribution channel;
  - [TASK-0004](docs/planning/tasks/TASK-0004-native-mod-skeleton.md) (Planned): the first implementation task;
  - the refined M1 milestone in [ROADMAP](ROADMAP.md) §3.1 and
    [PHASE-05](docs/planning/phases/PHASE-05-walk-drive-integration.md) §13.
- **Register:** 164 entries, including CONF-014, OPEN-036, OPEN-411 to OPEN-415, and RISK-411 to RISK-413. Answers
  were recorded for OPEN-001, OPEN-004, OPEN-016, OPEN-030, OPEN-033, and others.
- **Backfilled** the campaign test IDs in the phase plans.
- **Not done:** no installs, builds, game, Melty, or Motor Town launches, registry reads, or save access. PHASE-00 is
  not completed.

### Added — 2026-10-18 — Documentation baseline (TASK-0000)

- Created the complete English documentation baseline defined by the owner brief of 2026-10-18:
  - root entry points (README, AGENTS, CLAUDE, ROADMAP, CONTRIBUTING, SECURITY, THIRD_PARTY_NOTICES, this changelog);
  - `docs/` with product, research, engineering, architecture (including ADR-0001 to ADR-0004 as Proposed), gameplay,
    experience, planning (14 phase plans and task files TASK-0000 to TASK-0003), quality, agent-operation, release
    (pre-release drafts), legal, user (pre-release drafts), templates, and archive policy.
  - See [TASK-0000](docs/planning/tasks/TASK-0000-documentation-bootstrap.md) for the file counts.
- Defined 93 stable requirement IDs in the [PRD](docs/product/PRD.md) and generated the
  [traceability matrix](docs/planning/TRACEABILITY.md). Every requirement's evidence is "Not run".
- Created the central [risk, open-question, and conflict register](docs/planning/RISKS_AND_OPEN_QUESTIONS.md). It
  consolidates the historical Portuguese planning conversation (`chat.md`, kept unchanged) with the owner brief.
- Recorded a bounded, read-only review of public sources (base mod repository, official CS2 modding pages, Motor Town
  store page, Melty site and terms, community agent tooling) in [SOURCES](docs/research/SOURCES.md), with key facts
  checked against raw text. No code was cloned, built, installed, or executed.
- Added the prospective version-control hygiene files `.gitignore`, `.gitattributes`, and `.editorconfig`. They are
  scoped to private local files, generated artifacts, and Markdown line endings. No Git repository was initialized.
- Reconciled the document groups:
  - merged 76 contributed register entries, for 154 in total;
  - recorded the CONF-012 and CONF-013 conflicts;
  - aligned the phase gates, PRD wording, and the transition-save and company-vehicle semantics.
- Validated the documentation with 0 errors across 118 documents and 3,418 local links; 151 tests are defined for 93
  requirements. See the [DOCUMENTATION_VALIDATION_REPORT](docs/quality/DOCUMENTATION_VALIDATION_REPORT.md). This
  validated the documentation only; runtime validation is Not run.

### Not changed

- `chat.md` (historical input) is unchanged: SHA-256 `e5b154df783bb6f7acbef356f6558bae82edd081e372cd247b6318a640b284ca`,
  56,022 bytes.
- No game files, saves, mods, launchers, SDKs, or settings were touched. No build or runtime work happened.
