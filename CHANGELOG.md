# Changelog

| Field | Value |
|---|---|
| ID | DOC-CHANGELOG |
| Document status | Draft (living document) |
| Responsible role | Coordinator (maintainer); every completed task adds its real changes |
| Last updated | 2026-10-20 |

This file records changes that were **actually made**. It never lists planned features as done. Version headings
mark real releases ([PACKAGING_AND_PUBLISHING](docs/release/PACKAGING_AND_PUBLISHING.md)).

## [Unreleased]

## [0.2.0] — 2026-10-20

Released from commit `2994688` on [GitHub](https://github.com/michelbr84/DriveTheCityII/releases/tag/v0.2.0) and as a new version of [Paradox Mods 164157](https://mods.paradoxplaza.com/mods/164157/Windows).
Evidence: [RUN-0007](docs/quality/evidence/RUN-0007/REPORT.md). Driving in these tests is simulated input.

### Fixed

- **Parked city cars can be entered in sandbox mode (ISSUE-035).** Only cars stopped in traffic could be borrowed;
  168 presses of E failed in the owner's first session. A parked household car is now unparked the way the game does
  when a resident drives off, in the same step that takes it out of the traffic AI. When the player gets out, the game's
  own car AI parks it on a valid space. Verified: entered, driven and left in a parking space (parked again 3 s later)
  and on the road (parked on the kerbside lane 4 s later); `leakcheck` 0.
- **Buses and trucks no longer stop on small street props (ISSUE-040).** Poles, signs, lamps, posts, hydrants and
  furniture placed by the road (up to 3 m, for example a stop bench) no longer block vehicles of 7 m or more, as the game's
  own buses and trucks sweep over them when they follow the lanes; cars, people, buildings, lot furniture and parked
  vehicles are unchanged. One rule (`VehicleController.Blocks`) serves collisions, placement checks and the test
  autopilot. Same lamp, switch off and on: the bus stopped at it / drove past it; a car still stops at it.
- **A borrowed car saved while driven no longer stays frozen in the road after loading (ISSUE-041).** The saved copy
  kept the mod's AI exclusion, which the game never releases; it is now saved as ordinary traffic and the player keeps
  driving. Cars left that way by older saves are released when the city loads ("handed back (ISSUE-041)" in the log).
  `leakcheck` now counts any such car.
- **Invisible vehicles in garages are no longer obstacles (ISSUE-043).** Vehicles stored in a garage are not in the street,
  but the probe still treated them as solid; a motorbike in a garage beside the road stopped a bus at 12 m/s on a
  straight road. Unspawned objects are now ignored.

### Added

- Test bridge: `parked`, `gotoparked`, `cars`, `carinfo`, `props` (with model and owner), `softprops on|off`.
- Unit tests: 176 (small props for buses and cars, the switch).

## [0.1.0] — 2026-10-20

First public release, 2026-10-20: a manual-install archive on
[GitHub Releases](https://github.com/michelbr84/DriveTheCityII/releases/tag/v0.1.0) and the listing
[Paradox Mods 164157](https://mods.paradoxplaza.com/mods/164157/Windows), both from commit `59399fd`
([RUN-0006](docs/quality/evidence/RUN-0006/REPORT.md): archive installed as a player would, career persistence 11/11).
Everything below this heading is part of 0.1.0. Release tooling: `tools\release.ps1` (archive with install notes,
checksum, manifest) and `tools\publish-paradox.ps1` (the game's ModPublisher with its automatic login).

### Changed — 2026-10-20 — Repository prepared for public visibility

At the owner's request, before the GitHub repository `michelbr84/DriveTheCityII` is switched from private to public.

- **Audit (every commit, not just the current files):** 486 file versions of 252 paths in 60 commits were scanned for
  passwords, tokens and keys, email addresses, user-profile paths, machine names, account and store IDs, and IP
  addresses. None were found; the only personal detail is the documented project root path on the owner's machine.
  Commit identities use the GitHub `noreply` address. The repository holds no game files, decompiled code, saves,
  build output, or third-party code; its only image is a test screenshot of the game. The mod's source makes no
  network or process-launch calls, and the test bridge works only while a local marker file exists.
- **License:** MIT, copyright holder "michelbr84" (owner decision; OPEN-027 resolved). [LICENSE](LICENSE) added;
  [CONTRIBUTING](CONTRIBUTING.md) §8 and [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md) updated (test-only NuGet
  packages and their licenses listed; game screenshots excluded from the license).
- **README rewritten.** It still described pre-development ("no code, no build"), and the GitHub "first commit"
  `d91bd26` had appended a UTF-16 line to it, so Git and GitHub treated it as a binary file.
- **`chat.md` moved** from the root to [docs/archive/chat.md](docs/archive/chat.md), unchanged (owner decision; SHA-256
  still `e5b154df…284ca`). 37 links updated.
- **Entry documents brought up to date:** AGENTS.md §1 and §4, CLAUDE.md (Git and next-step lines), CONTRIBUTING
  (state, pull-request conventions), SECURITY (state, source check, how to report a vulnerability through GitHub),
  START_HERE (build before install on a fresh copy), `.gitignore` header, INDEX, PROJECT_STATUS, register entries
  OPEN-027, OPEN-029, OPEN-702, RISK-701.

### Fixed — 2026-10-20 — Standing queues and bus turn-rounds (ISSUE-033, ISSUE-034)

Evidence: [RUN-0005](docs/quality/evidence/RUN-0005/REPORT.md). Driving in these tests is simulated input.

- **A parked vehicle of the player's no longer holds traffic indefinitely (ISSUE-033).** It was registered in its lane
  as traffic standing still, and native cars wait behind standing traffic without limit: a rental left in the street
  held a queue that grew from 12 to 81 cars in four minutes. A parked vehicle now drops the game's motion data and is
  handled like any stationary obstacle (traffic changes lane or edges past); the data returns when the player drives
  it. Same spot with the fix switched off and on: 6 and 1 cars stopped behind it without the fix, 0 with it.
- **The driven vehicle reports what it waits behind (ISSUE-033, ISSUE-025).** The game clears a traffic deadlock only
  when a chain of "blocked by" links loops back; the player's vehicle never filled in its own link, so a loop through it
  stood until the player moved. After 5 s stopped behind a vehicle (including one already touching it), that vehicle is
  written as its blocker. Bus stopped mid-junction, three rounds: without the fix a car stood across its front waiting
  for it and 18–23 queued cars' chains ended at the bus (still 20 after a further 30 s); 20 s after switching the fix on,
  that car was gone and 0–2 remained. A first version skipped a vehicle overlapping the bus; the test exposed it and it
  was corrected.
- **Borrowing a native car removes its extra lane entries** (lane-change lane, lanes its body overlapped), which only the
  game's navigation system removes and which would otherwise stay behind as obstacles.
- **Buses go round the block instead of making U-turns at junctions (ISSUE-034).** Only a route starting against the
  heading was penalised; U-turn movements at junctions cost nothing. For vehicles over 10.5 m both now cost 400 s
  (`TurnPenalties` in Core; trucks and cars unchanged). On the test pose the route changed from 426 m with a junction
  U-turn to 433 m round the block, and the bus served the stop with no turn-round.
- **Diagnostics (test bridge):** `jams`, `ghosts`, `routeinfo`, `routetest`, `trafficfix`, `parkfix`, `career abtest`,
  `trafficwatch … parked`; `blockchain` and `blockers` now follow trailer-to-tractor links as the game does (earlier
  readings that "the chain does not involve the player's vehicle" had stopped at trailers).
- **Unit tests:** 172 (new: turn penalties by length, choice of the vehicle waited behind).
- **Recorded:** a 12.7 m bus clips corner props in narrow junctions because vehicles turn about the body centre
  (ISSUE-040); the test autopilot could not finish a downtown bus shift on the new build without help.
- **Package:** `DriveTheCityII-0.1.0-36ec810-20261020T065957Z`, installed. On it: career persistence 11/11 and a
  save/reload with a parked vehicle passed; M1 with real keys was not run (a Steam window held the foreground).
- **Recorded from the owner's first session** (not fixed): parked cars cannot be entered (ISSUE-035), three recoveries
  in a minute end character mode (ISSUE-036), the garage's "starter job" option does nothing (ISSUE-037), character
  mode refused while the camera looks at a blocked spot (ISSUE-038), no bus lines found in the owner's city
  (ISSUE-039).

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

### Fixed — 2026-10-19 (evening) — Freight and bus in game (part B)

Found by the freight and bus campaigns ([RUN-0003 §4](docs/quality/evidence/RUN-0003/REPORT.md)); each fix was
rerun in game. Details and evidence in [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md) ISSUE-020 to ISSUE-033.

- Employer trucks and buses are placed where their whole footprint is clear (ISSUE-020).
- Place access points sit on the curb-side lane; guidance ends within 6 m of a dock or stop when possible (ISSUE-021).
- Harsh-driving acceleration is bounded and smoothed; contacts are scored separately (ISSUE-022).
- Ground following: wheel outliers rejected, pitch and roll capped (ISSUE-023); overlapping decks keep each wheel on
  its own surface or the followed lane's level (ISSUE-029); a stale tilt no longer blocks a vehicle (ISSUE-030).
- Routes run on the lanes vehicles drive in, chosen from the destination backwards; lane changes are smoothed
  (ISSUE-024). Contacts use body widths without mirrors (ISSUE-024).
- Contacts with moving vehicles score only the player's own share of the closing speed; traffic overtaking through the
  player's vehicle is no impact (ISSUE-027).
- Guidance: long-vehicle U-turn penalty 120 s (ISSUE-026); start lanes on the vehicle's own level (ISSUE-028);
  standing queues near the start cost extra so the route goes round them (ISSUE-033).
- Recovery keeps the direction of travel, prefers its own level, faces along the guidance route and resets the tilt
  (ISSUE-025, ISSUE-030, ISSUE-031).
- The operator's bus is a line bus, never a prison van (ISSUE-032).
- Test tooling: autopilot deadlock limits, bumper-zone creeping, curve speed, comfortable braking and relative
  following; campaign resume after a return to city building, `-NoDrive`, `-WarpMinutes`; bridge diagnostics
  `traffic [radius]`, `probegrid`, `netsat`, `lanesat`, `routepts`, `tpveh`, `tpav`, `blockers`, `blockchain`.
- Validation: a freight job and a whole 14-stop bus shift completed and paid in game (simulated input); 161 unit
  tests.

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
- **More fixes from the career runs:**
  - events returned by career commands were dropped, so the employer's truck or bus never appeared (ISSUE-014);
  - challenge runs were voided as "route changed" in a live city (ISSUE-016);
  - the mod never reported recoveries to the career, so deadlines did not account for them.
- **Portuguese:** 174 of 222 career message keys translated (8 new unit tests; 151 in total); payout lines and job
  titles formatted in Portuguese by the mod.
- **Validation:** presentation checks and the mod-less load check passed ([RUN-0004](docs/quality/evidence/RUN-0004/REPORT.md));
  career coverage in [RUN-0003](docs/quality/evidence/RUN-0003/REPORT.md); final package `0.1.0-3eaf045`; M1 repeated 17/17 with real keys on it.

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
