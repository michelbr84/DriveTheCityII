# Changelog

| Field | Value |
|---|---|
| ID | DOC-CHANGELOG |
| Document status | Draft (living document) |
| Responsible role | Coordinator (maintainer); every completed task adds its real changes |
| Last updated | 2026-10-19 |

This file records changes that were **actually made**. It never lists planned features as done, and no version of the
mod has been released. Version headings will appear only when real releases exist
([PACKAGING_AND_PUBLISHING](docs/release/PACKAGING_AND_PUBLISHING.md)).

## [Unreleased]

### Added — 2026-10-19 — Native mod implementation, in-game validation, private build (TASK-0003, TASK-0004 and later)

Everything below was built and exercised on this machine (CS2 1.6.2f1). The "Validation" lines name the kind of
evidence; full records are in [WORKLOG](docs/quality/evidence/WORKLOG.md) and the RUN reports.

- **Toolchain and helpers** (user-scoped, no game files changed):
  - private .NET SDK; build against the game's own managed assemblies ([ADR-0005](docs/architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md));
  - `tools\build.ps1` (tests, build stamp, package, `LATEST.txt`), `tools\deploy.ps1` (install, status, rollback,
    uninstall, ownership manifest), `tools\test-session.ps1` (isolated session that restores the Continue pointer),
    `tools\bridge.ps1` (file-based test bridge), `tools\realinput.ps1` (real keyboard input to the verified game
    window), `tools\campaign-m1.ps1`, `tools\baseline.ps1`.
- **Engine-free Core library with unit tests:** geometry, level-aware ground solver, on-foot motor, vehicle controller
  (gears, reverse delay, per-wheel ground prediction, obstacle response), control state machine (TR-01..TR-13,
  timeouts, recovery loop detection), JSON, save envelope with CRC32, sidecar revision store with quarantine, EN/PT-BR
  text tables.
- **Mod runtime (native IMod):**
  - character mode: avatar spawned from a city resident prefab, walking/running with animation, enter/exit, driving,
    recovery, return to city building with camera, tools and input restored;
  - drivable vehicles from the city's own car prefabs with roles stripped (no AI takeover), registered in lane
    buffers so AI traffic queues behind the player;
  - save safety: mod entities are tagged temporary for every save and never persisted; autosave is only disabled in
    test sessions;
  - persistence envelope in every city save plus sidecar backups; load outcomes with quarantine;
  - road router, place index, world markers, mission objects;
  - HUD and Tab panel (React module), onboarding hints, HUD scale and high-contrast options;
  - Options page: gameplay, camera, 12 remappable key bindings, accessibility, safe-disable;
  - co-load guard against the original Beta Test Driving Mod.
- **Validation:** in-game runs in disposable `DTC2-LAB` cities with simulated and real keyboard input: transition
  cycles 20/20, 10/10 and 3/3 with 0 failures, save while driving without a crash, leak check 0, AI traffic queueing,
  30-37 fps in the benchmark city. Fixed defects are listed in [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md)
  (ISSUE-001 to ISSUE-006).
- **Docs:** ADR-0005 Accepted; TASK-0003 In review; [START_HERE](START_HERE.md); PROJECT_STATUS §0; COMPATIBILITY_MATRIX
  tested rows; MELTY_EVALUATION §9a.

### Added — 2026-10-19 (later the same day) — Careers in game, M1 acceptance, audio, language option

- **Careers (Core, 93 new unit tests; 143 in total):** taxi, delivery, freight, bus, garage (rent, buy, sell,
  refuel, repair, upgrades, roadside help, tow), progression and unlocks, home and garage base, a managed company
  (hire, fleet, contracts, period settlement), and challenges. Money moves only through an append-only ledger with
  operation IDs, so a repeated action or a reload never pays or charges twice.
- **Careers in the mod:** job board, garage, company, challenges and profile tabs; objective card; markers and route
  guidance for missions and running challenges; mod-owned taxi passengers; employer-provided trucks and buses.
- **Fixes from in-game runs:**
  - a 1.1 s freeze when building taxi offers (ISSUE-007);
  - running challenges were invisible (ISSUE-008);
  - freight and bus jobs were refused while the player drove a car;
  - route time estimates assumed free-flow driving, so delivery deadlines expired too early;
  - the job board regenerated offers on every HUD refresh when none were available;
  - exits from a car stopped mid-lane now prefer the nearest sidewalk within 8 m (ISSUE-010).
- **Presentation:** procedural vehicle sound (engine, rolling noise, impact thump) routed through the game's mixer,
  with an Options switch; "Mod language" option (game / English / Português do Brasil).
- **Validation:** M1 acceptance with real keyboard input, 17/17 ([RUN-0002](docs/quality/evidence/RUN-0002/REPORT.md)).
  Career persistence campaign: mid-fare save and reload, no double payout, older save restored exactly. Career
  coverage campaign: see the WORKLOG and MORNING_REPORT.
- **Test tooling:** `track` measurement op, test autopilot for missions and challenges (simulated input, labelled in
  every result), campaign scripts, a mod-less load check, and a real-time soak script.

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
