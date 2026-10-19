# Packaging and Publishing

| Field | Value |
|---|---|
| ID | DOC-PACKAGING |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 |
| Phases | PHASE-01 (mod identity), PHASE-13 (packaging and publication) |
| Responsible role | Coordinator (maintainer); QA verifies packages; project owner chooses channels and approves every publication |
| Related requirements | REQ-REL-001, REQ-REL-003, REQ-REL-004, REQ-COMPAT-001, REQ-DIAG-001, REQ-ART-002 |
| Related documents | [ADR-0001](../architecture/decisions/ADR-0001-host-game-and-runtime.md), [INSTALL_UPGRADE_UNINSTALL](INSTALL_UPGRADE_UNINSTALL.md), [COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md), [DEPENDENCIES](../engineering/DEPENDENCIES.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), [MELTY_EVALUATION](../research/MELTY_EVALUATION.md), [SOURCES](../research/SOURCES.md) |
| Last updated | 2026-10-18 |

> **Pre-release draft.** Nothing has been built, no package exists, **no tested installer exists, and nothing is
> published**. No distribution channel has been chosen. This document describes the intended packaging and the checks a
> package must pass before any publication.

## 1. Purpose

Defines what a Drive the City II package will contain, how its dependencies and notices are declared, which channels
are candidates, and the verification checklist that must pass before the owner authorizes publication.

## 2. Scope and non-goals

- In scope: package contents, dependency declaration, notices, versioning, candidate channels, release verification.
- Not in scope now: choosing a channel, creating listings or publication identifiers, automated or CI-based publishing
  (not authorized), and any packaging specific to Melty (assessed separately under OPEN-016).
- Never in scope: bundling Cities: Skylines II files, Motor Town content, or Melty components.

## 3. Intended package contents [Proposal]

| Item | Notes |
|---|---|
| Mod assembly and the build outputs the official toolchain requires | The exact file set is confirmed only by a validated build ([BUILD_AND_RUN](../engineering/BUILD_AND_RUN.md)). The official toolchain documents a post-build processing step that adds files to the output **[Documented upstream]**; this is unverified locally. |
| UI module files, if the chosen UI technology needs them | OPEN-015. |
| Localization resources (English and PT-BR) | REQ-L10N-001; mechanism OPEN-026. |
| Own or authorized assets only | Each listed in [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md); placeholders labeled (REQ-ART-002). |
| Third-party notices and the project license text | [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md); license pending OPEN-027. |
| Release notes | Derived from [CHANGELOG.md](../../CHANGELOG.md); only validated changes. |

**Must never be included:** game DLLs or other files from the game installation, decompiled code, extracted game
assets, saves (including lab saves), Motor Town or Melty material, development tools, the local environment file,
evidence artifacts, logs, or anything containing personal data (RISK-029, RISK-030).

## 4. Versioning and identity [Proposal]

- The mod has its own identity, independent of the Beta Test Driving Mod, established in PHASE-01. Incorporating
  upstream code never reuses the upstream mod's identity.
- Mod versions use `MAJOR.MINOR.PATCH`. PATCH is for fixes without save-schema changes; MINOR for compatible features;
  MAJOR for incompatible changes. Pre-release builds carry a pre-release suffix.
- The save **schema version** is independent of the mod version ([SPEC-SAVE](../architecture/SAVE_FORMAT.md)).
- The version and build ID appear in logs and in the diagnostics bundle (REQ-DIAG-001), so reports can be matched to a
  package.

## 5. Dependency declaration

- **Runtime requirements:** Cities: Skylines II at a version declared supported in
  [COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md), plus explicitly declared mod dependencies. None has been chosen. Every
  dependency is recorded with version pin and license in [DEPENDENCIES](../engineering/DEPENDENCIES.md) and declared
  through the channel's own mechanism. Paradox's documentation states that its mod platform resolves dependency trees
  **[Documented upstream]**; how to declare them is part of OPEN-017.
- **Not runtime requirements (REQ-REL-004):** Motor Town, Melty, .NET SDKs, Unity, IDEs, Node.js, or any development
  tool. The core distribution must work with only CS2 and the declared dependencies.
- **The original Beta Test Driving Mod is not a dependency.** If any of its code is incorporated, it is incorporated with
  its notices. The two mods must never load conflicting controllers together (REQ-COMPAT-001).

## 6. Notices, licensing, and naming

- Before any distribution, [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) is complete for every
  incorporated component and asset, and [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md) lists them (REQ-REL-003).
- The project license is an owner decision (OPEN-027). No package is published without it.
- Names of other games and companies appear only as factual description, never as branding, and the listing must not
  imply endorsement (RISK-015). The unofficial status follows the affiliation statement in
  [README.md](../../README.md).

## 7. Candidate channels

| Channel | Status | What is known | Must be validated (OPEN-017, OPEN-701) |
|---|---|---|---|
| Paradox Mods | **Candidate primary channel to validate.** The earlier planning conversation assumed it **[Historical input]**. | Paradox describes it as its official mod platform for CS2. The official toolchain documents publishing from the IDE using a publish configuration file and a publisher tool, after logging in to a Paradox account in-game; supported game versions can be listed in metadata **[Documented upstream]** ([SOURCES](../research/SOURCES.md)). | Metadata and content rules, review or moderation, dependency declaration, version update flow, account requirements, whether a private or test publication is possible. |
| Melty | **Not a channel for this project** under the proposed [ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md) (owner acceptance pending). | Melty's connected catalog lists CS2 with **BepInEx 5 as its only loader**, installed into the game folder. A native CS2 code mod cannot be installed one-click by Melty, because its native folder under `AppData\LocalLow` is not an allowed destination, and Melty's publishing flow requires one-click. Melty's terms also describe an exclusive license over content published there **[Documented upstream]** ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md) §9). | Whether CS2 content is eligible; packaging format; whether exclusivity conflicts with any other channel or with incorporated licenses (needs legal reading, CONF-008, RISK-025). |
| Source hosting | Not a distribution channel yet. | No repository exists (OPEN-029); no license is chosen (OPEN-027). | Decided with OPEN-027 and OPEN-029. |

No listing URL, publication identifier, or channel-specific procedure is recorded here until it exists. Credentials for
any channel are used only by the project owner ([SECURITY.md](../../SECURITY.md)).

## 8. Proposed release procedure (requires validation)

1. Freeze the release candidate: version, build ID, commit (once Git exists), and pinned dependencies.
2. Run the PHASE-12 and PHASE-13 evidence set, including the checklist in section 9 and
   [INSTALL_UPGRADE_UNINSTALL](INSTALL_UPGRADE_UNINSTALL.md).
3. Assemble the package from a clean build; review its file list against section 3.
4. Prepare release notes and the compatibility declaration ([COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md)).
5. Obtain and record the owner's authorization; the owner performs or supervises the publication.
6. Install the published package from the channel on a clean configuration and repeat the smoke checks.
7. Update [CHANGELOG.md](../../CHANGELOG.md), [PROJECT_STATUS](../PROJECT_STATUS.md), and
   [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md).

## 9. Verification checklist (validation: Not run)

- [ ] File list matches section 3; no forbidden item is present.
- [ ] Every incorporated component and asset has a notice and a provenance entry; the project license is included.
- [ ] Installed and run on a configuration **without** SDKs, Unity, IDEs, or Node.js (REQ-REL-001, TEST-REL-001;
      clean configuration and tester availability: OPEN-507).
- [ ] Runs with only CS2 and the declared dependencies; Motor Town and Melty are absent (TEST-REL-004).
- [ ] Version and build ID in logs match the release notes.
- [ ] Declared supported game versions match Passed rows in [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md).
- [ ] Behavior with the original Beta Test Driving Mod present is verified and documented (TEST-COMPAT-001).
- [ ] Install, upgrade, and clean-exit checks passed ([INSTALL_UPGRADE_UNINSTALL](INSTALL_UPGRADE_UNINSTALL.md)).
- [ ] Release notes claim nothing that lacks evidence.
- [ ] Channel requirements from OPEN-017 are met; owner authorization is recorded.

## 10. Open questions and risks

- OPEN-017 — distribution platform requirements. OPEN-016 — Melty fit. OPEN-027 — project license. OPEN-507 — clean
  configuration and independent tester.
- [OPEN-701](../planning/RISKS_AND_OPEN_QUESTIONS.md) (new) — which channel or channels will distribute v1.0, given
  possible exclusivity terms.
- RISK-025 — platform constraints. RISK-029 — accidental redistribution of proprietary content. RISK-015 — IP and
  branding. CONF-008 — Melty's "mashup" model versus a CS2-only runtime.
