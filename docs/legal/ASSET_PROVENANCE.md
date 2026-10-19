# Asset Provenance Register

| Field | Value |
|---|---|
| ID | DOC-ASSET-PROVENANCE |
| Document status | Draft |
| Release scope | v1.0 (every shipped asset) and project evidence |
| Phases | PHASE-04 (avatar), PHASE-07 (vehicles), PHASE-11 (art, audio, UI), PHASE-13 (release audit); evidence rules apply from TASK-0003 |
| Responsible role | Coordinator (maintainer); the contributor adding an asset fills in its row; the Reviewer verifies it; the project owner approves third-party assets |
| Related requirements | REQ-ART-001, REQ-ART-002, REQ-AUDIO-001, REQ-REL-003 |
| Related documents | [REUSE_AND_LICENSE_AUDIT](REUSE_AND_LICENSE_AUDIT.md), [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), [SPEC-ART](../experience/ART_AND_ANIMATION.md), [SPEC-AUDIO](../experience/AUDIO.md), [evidence](../quality/evidence/README.md) |
| Last updated | 2026-10-18 |

> **As of 2026-10-18, no assets are incorporated in this project:** no models, textures, animations, audio, fonts,
> icons, or images. The register in §3 therefore has no entries.

## 1. Purpose

Every asset that ships with the mod, appears in its listing, or is committed to this project must have a traceable
origin and a right to use it. This register records that provenance. Code provenance is handled in
[REUSE_AND_LICENSE_AUDIT](REUSE_AND_LICENSE_AUDIT.md).

## 2. Rules

1. **Own or authorized only.** Assets are created by project contributors (who agree to the project license once it is chosen, OPEN-027) or are third-party assets whose license explicitly allows our use and distribution.
2. **No extraction or redistribution of game assets.** Cities: Skylines II models, textures, animations, audio, and UI art are never copied out of the game into the project or a package. Using the game's own assets **at runtime, without redistributing them**, is an open question per asset type (OPEN-034 for vehicles, OPEN-010 for the avatar), and needs an answer before any design depends on it.
3. **No third-party game content.** Nothing from Motor Town, SimCity, or other games: no assets, sounds, logos, or recognizable trade dress (RISK-015).
4. **Placeholders are labeled** in builds and documentation until they are replaced (REQ-ART-002). Placeholders follow the same provenance rules.
5. **Generated assets:** if a tool generates an asset, the row records the tool, its version, the terms of use that applied at creation, and the date. Prompts and inputs must not imitate protected works or brands. The owner approves each use.
6. **Screenshots and recordings:**
   - **Evidence:** captures of CS2 used as test evidence are kept local until OPEN-407 settles what may be committed. Captures must be sanitized (no account names, overlays with personal data, or user paths).
   - **Public material** (a mod listing, documentation images): requires OPEN-407 to be resolved and owner approval. Screenshots of Motor Town or Melty are not used.
   - The base mod's listing screenshots are not reused.
7. **One row per asset or coherent asset pack,** added in the same change that adds the asset.

## 3. Register

Format (one row per asset or pack). There are **no entries yet**.

| Asset ID | Asset (file or pack) | Origin (created / third-party / generated) | Author or rights holder | License or permission | Proof (link, commit, receipt, terms version) | Date added | Used in | Reviewer |
|---|---|---|---|---|---|---|---|---|

Asset IDs use the form `AST-NNNN`, assigned when the first asset is added. When a third-party asset is added, its
notice goes into [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md) in the same change.

## 4. Candidate questions to resolve before assets are needed

| Question | Why | Register | Resolving phase |
|---|---|---|---|
| Can the avatar use a runtime representation available in the game, or does it need own or authorized models and animations? | REQ-PLAYER-001, REQ-ART-001 | OPEN-010 | PHASE-04 |
| May career vehicles use the game's own vehicle models at runtime without redistribution? | REQ-DRIVE-003, REQ-GARAGE-002 | OPEN-034 | PHASE-02, PHASE-07 |
| Which CS2 captures may be committed as evidence or used publicly? | Evidence and listing images | OPEN-407 | TASK-0003 (evidence), PHASE-13 (public) |
| Which fonts and icons does the UI stack provide for mod use, and under which terms? | Fonts and icons are assets too | OPEN-015 (UI technology) | PHASE-11 |
| Who provides the PT-BR translations, and under which terms? | Translations are contributed text assets | OPEN-026 (mechanism) | PHASE-11 |
| Which audio sources are authorized for engine, tire, footstep, and impact sounds? | REQ-AUDIO-001 | — (tracked in [SPEC-AUDIO](../experience/AUDIO.md)) | PHASE-11 |

## 5. Review procedure

**Proposed procedure (requires validation).**

1. The contributor adds the asset and its register row in one change.
2. The Reviewer checks that the proof supports the stated license, that the license allows modification and distribution in a mod, and that no game or third-party branding is visible.
3. Third-party assets need owner approval. When required, they are also checked against the [REUSE_AND_LICENSE_AUDIT](REUSE_AND_LICENSE_AUDIT.md) procedure.
4. Before release (PHASE-13), QA reconciles the package contents with this register: every shipped asset has a row, and every row's asset is either shipped or marked as removed.

## 6. Open questions and risks

- **Central:** OPEN-010, OPEN-015, OPEN-026, OPEN-027, OPEN-034, RISK-009, RISK-015, RISK-029, RISK-030.
- **New:** OPEN-407 (captures in evidence and public material).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
