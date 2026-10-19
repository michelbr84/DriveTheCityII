# Reuse and License Audit

| Field | Value |
|---|---|
| ID | DOC-LICENSE-AUDIT |
| Document status | Draft |
| Release scope | v1.0 (required before any incorporation and before distribution) |
| Phases | PHASE-00 (TASK-0002), every incorporation event, PHASE-13 (release audit) |
| Responsible role | Coordinator (maintainer); the project owner decides the license and incorporations, with legal review where needed; the Reviewer verifies notices |
| Related requirements | REQ-REL-003, REQ-REL-004, REQ-ART-001, REQ-AUDIO-001 |
| Related documents | [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md), [ASSET_PROVENANCE](ASSET_PROVENANCE.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [SOURCES](../research/SOURCES.md), [DEPENDENCIES](../engineering/DEPENDENCIES.md), [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md), [MELTY_EVALUATION](../research/MELTY_EVALUATION.md) |
| Last updated | 2026-10-18 |

> **As of 2026-10-18, no third-party code or asset is incorporated in this project.** This document records what
> was found about candidate and referenced components. It contains **no legal conclusions**: every item that could
> have legal consequences is flagged for the project owner and, where needed, for legal review.

## 1. Purpose

Before anything is incorporated or distributed, this audit establishes:

- each component's role and license as found;
- the evidence level;
- the obligations that would apply if it were incorporated;
- the questions that remain open.

Incorporation is allowed only after the component's row is complete and the owner approves it. Notices are added at
that moment ([THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md)).

## 2. Project license

- **Undecided** (OPEN-027). No `LICENSE` file has been created for Drive the City II.
- The project is **not** assumed to be MIT because a candidate dependency is MIT **[Owner requirement]**.
- The owner decides before the first incorporation of third-party code (PHASE-01 at the earliest) and confirms the decision in PHASE-13. Inputs to that decision:
  - the obligations in §4;
  - the Melty terms (§5), if relevant;
  - the intended distribution platform (OPEN-017).

## 3. Component table

| Component | Role in this project | License as found | Evidence | Status | Observations and open items |
|---|---|---|---|---|---|
| Beta Test Driving Mod (SRC-001) | Candidate source for vehicle control | MIT at the pinned commit; "Copyright (c) 2026 stoot". The first 4 commits used a restrictive source-available license (§3.1). | **[Source-inspected]**: TASK-0002 read every text file raw at the pinned commit `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (2026-10-18), plus the license and notice texts at earlier revisions | **Candidate — not incorporated** | See §3.1 and §3.2 |
| CitiesSkylinesModding/agents-plugins (SRC-005) | Tooling and knowledge reference | MIT; "Copyright (c) 2026 Morgan Touverey Quilling" | Raw (V-10) | Reference only; **not installed** | Installing any plugin needs owner authorization. Plugin dependencies (for example the .NET 10 SDK, NuGet packages) were not audited. |
| BrokeAssSoftware/cs2-modding-guide (SRC-013) | Community reading material | MIT according to the page | Extraction | Reference only | No code copied |
| Paradox wiki and dev diaries (SRC-002, SRC-008 … SRC-012) | Documentation | Not audited | — | Reference only | Only short attributed quotations in our docs |
| Cities: Skylines II assemblies | Build-time references | Proprietary (Paradox Interactive / Colossal Order) | — | Referenced at build time; **never redistributed** | Copies never enter the project or the packages (RISK-029) |
| Cities: Skylines II assets (models, textures, audio) | None today | Proprietary | — | **Not extracted, not redistributed** | Runtime reuse of in-game vehicle models is an open question (OPEN-034) |
| Motor Town: Behind The Wheel (SRC-003) | Design reference | Proprietary (P3 Games) | — | **Reference only; no reuse** of code, assets, physics, or branding | [MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md) |
| Streets of SimCity | Inspiration only | Proprietary | — | **No reuse** of content or branding **[Owner requirement]** | — |
| Melty Terms of Service (SRC-004) | Possible distribution channel | Terms with an exclusivity clause | Raw (V-09) | Not used | See §5 |
| Unity Editor (official toolchain prerequisite) | Development tool | Personal or commercial Unity license required, per SRC-008 | Extraction | Not checked | Owner obligation (OPEN-406); not part of our distribution |

### 3.1 Beta Test Driving Mod — license findings and questions

**Status: Candidate — not incorporated.** TASK-0002 read-only audit, 2026-10-18, at the pinned commit
`eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`. Technical detail is in [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)
§9.4. These are facts and questions only; no legal conclusion is drawn.

| Item | Finding | Level | Open item |
|---|---|---|---|
| License text at the pin | `LICENSE` is the standard MIT text, "Copyright (c) 2026 stoot" | **[Source-inspected]** raw at pin | — |
| License history | See the table below. The first 4 commits (2026-05-30) carried a "Beta Test Driving Mod Source-Available Transparency License". The relicensing commit `af355236521636e987801e4c14019aaf187ec277` (2026-05-31) replaced it with MIT. The CHANGELOG says it "Re-licenses the repository under the MIT License". | **[Source-inspected]** raw `LICENSE` and `NOTICE.md` at `9955221…`, `af35523…`, and the pin; per-commit file lists | Q-01, Q-02 |
| Copyright holder | "stoot" in `LICENSE` (all revisions), `mod.json` "author", and `AssemblyCompany`. **`AssemblyCopyright` is empty.** All 8 commits are authored as "stoot", and GitHub associates them with the account login `stoot`. The repository owner is the account `northernst11-bot`. The relationship is **not stated**. | **[Source-inspected]**; GitHub API commit metadata | OPEN-405; Q-03 |
| Per-file headers | **None.** No `.cs`, `.mjs`, or `.svg` file carries a copyright or license header. | **[Source-inspected]**, every text file | Our headers at incorporation would cite `LICENSE` and the pin (CODING_STANDARDS §9) |
| Notice terms | `NOTICE.md` at the pin asks users to "keep the MIT copyright and license notice with substantial copies". It grants no rights to "Cities: Skylines II, Paradox or Colossal Order trademarks, game assets, or third-party content not owned by this project". | **[Source-inspected]** | Carry the exclusions into our notices if anything is incorporated |
| Decompiled material | The csproj line `Compile Remove="decompiled_published_0317\**\*.cs"` was added on 2026-06-04 (commit `2c5b83085290b9d578ae6eff30f9241d8b0299c5`). No commit ever contained the folder. The CHANGELOG entry `0.3.18-crashfix-local` says it "Restores the local test cache from the published `0.3.17-collision-tuning-public` package". What was decompiled, and by whom, is **not stated**. | **[Source-inspected]**; **[Documented upstream]** (CHANGELOG) | OPEN-404; Q-04 |
| Third-party code claim | The CHANGELOG entry `0.3.17-collision-tuning-public`: "Ports the CS2 GTA Edition vehicle collision layer into Beta Test Driving Mod". The repository does not identify that project, its author, or its license. `DirectDriveVehicleCollisionSystem.cs` (added 2026-06-04) has no attribution. | **[Documented upstream]** (CHANGELOG); **[Source-inspected]** (no header) | Proposed OPEN-411; Q-05 |
| Trademarked words | Car brand words appear only as prefab-name match strings in the unregistered `ReliableDriveSystem.cs` | **[Source-inspected]** (assistant reader) | Not reuse candidates |
| Images | 6 PNG/JPG files (game-rendered screenshots, thumbnail, icon; 2 pairs are byte-identical) and one hand-drawn SVG | Tree listing; SVG raw | Not reuse candidates |
| Binaries | None committed | Pinned tree (not truncated) | — |
| Runtime dependencies | None declared (`mod.json` `"dependencies": []`; no `Dependency` element). No packages, no Harmony, no BepInEx. | **[Source-inspected]** | — |

**License history by commit** (all authored by "stoot"):

| Commit | Date (UTC) | Message | `LICENSE` state |
|---|---|---|---|
| `9955221a6364630ca4a7209456488c8443f23ce2` | 2026-05-30 | Release source available snapshot | **Source-Available Transparency License** ("not open source"; all rights reserved) |
| `5f59b2c4691f8741c10960be97e9c99b26c3c68c` | 2026-05-30 | Add source transparency link to mod bio | unchanged (restrictive) |
| `8a9018f65bea4dde238f1d57119b0568709552e3` | 2026-05-30 | Refresh published package after checksum failure | unchanged (restrictive) |
| `7173e9e5392470f07899f52c000ac8e98c00bd6e` | 2026-05-30 | Hide advanced controls in public UI | unchanged (restrictive) |
| `af355236521636e987801e4c14019aaf187ec277` | 2026-05-31 | Relicense mod source under MIT | **MIT** |
| `2c5b83085290b9d578ae6eff30f9241d8b0299c5` | 2026-06-04 | Publish stable crashguard driving update | MIT (`NOTICE.md` wording updated) |
| `28c140737f42abf8ae87490bf6f8b037dd668019` | 2026-06-17 | Publish 0.3.53 stable source update | MIT |
| `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` (pin) | 2026-06-17 | Publish 0.3.54 stable smoother driving update | MIT |

These files have been **unchanged since the restrictive first commit** and are still in the MIT-licensed pinned tree:
`DirectDriveInputSystem.cs`, `DirectDrivePoliceChaseSystem.cs`, and all eight `ReliableDrive*` files. None of them is
a reuse candidate ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) §9.10).

### 3.2 Open legal questions for the owner (input to OPEN-027)

No answer is assumed. Contacting the upstream author or seeking legal review requires the owner's decision.

| # | Question | Why it matters |
|---|---|---|
| Q-01 | Is the owner satisfied that the MIT `LICENSE` at the pinned commit governs **all** files in the pinned tree, including files first published under the restrictive text and never modified since? | Any code-level reuse; the plain reading of the relicensing commit and NOTICE is "yes", but no legal conclusion is drawn here |
| Q-02 | The old text forbade use "to train, fine-tune, benchmark, or evaluate machine-learning systems or code-generation tools". Does the owner want a policy that AI agents only read the MIT-licensed revisions? TASK-0002 read only the license and notice texts at the old revisions, plus the pinned (MIT) tree. | Process hygiene for future audits |
| Q-03 | How should attribution name the holder: exactly "Copyright (c) 2026 stoot" as written? Does the owner want to confirm the relationship between "stoot" and the repository account `northernst11-bot`? | THIRD_PARTY_NOTICES accuracy (OPEN-405) |
| Q-04 | Does the owner want an upstream statement on what `decompiled_published_0317` contained? Or should every file touched in commit `2c5b830…` be treated as **concept only** without asking? | Provenance taint (OPEN-404, RISK-404) |
| Q-05 | Should the collision code be treated as concept only until "CS2 GTA Edition" and its license are identified? | Third-party code inside an MIT-labeled file (proposed OPEN-411, RISK-411) |
| Q-06 | Which project license will Drive the City II use? It must allow the MIT notice for any adapted snippets. | OPEN-027 |
| Q-07 | If the owner chooses Option B, should every adapted snippet carry an attribution header even for small functions ("when in doubt, attribute")? | [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md) §2 |

## 4. Obligations if MIT-licensed code is incorporated

This section summarizes what the license and notice texts say. It is not legal advice.

1. **Upstream requirements:**
   - The MIT text requires the copyright notice and permission notice to be included in all copies or substantial portions of the software.
   - The base mod's NOTICE repeats this request and limits the grant to the project's own code and original assets.
2. **Our process at incorporation:**
   - Add the component, the pinned SHA, the copyright line exactly as written upstream, and the full license text to [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md).
   - Add file headers per [CODING_STANDARDS](../engineering/CODING_STANDARDS.md) §9.
   - Record the incorporation in the [CHANGELOG](../../CHANGELOG.md).
3. **Scope check:** confirm that the incorporated files do not derive from decompiled or third-party material (OPEN-404; proposed OPEN-411) and that the copyright holder is correctly identified (OPEN-405).
4. **Packaging:** the notices travel with every distributed package ([PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md)).
5. **Trademarks:** the base mod's license and notice do not grant rights to game trademarks or assets, and no such rights are assumed.

## 5. Melty Terms: distribution-relevant clause

The Melty Terms (raw text verified 2026-10-18, V-09):

- say that publishing gives Melty an exclusive, worldwide, royalty-free licence it may sublicense;
- state that "Melty is the only marketplace for your Content";
- extend the exclusivity to modified, partial, renamed, or ported versions and remixes;
- keep it for 12 months after removal from Melty.

**Flag for owner and legal review:** this appears to conflict with distributing the same content (or new versions of
it) on Paradox Mods. It may also interact with the licenses of any incorporated third-party code. **No conclusion is
drawn here** (OPEN-401, RISK-401). Until the owner decides otherwise, nothing from this project is published on
Melty ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)).

## 6. Trademarks and branding

"Cities: Skylines", "Paradox", and "Colossal Order"; "Motor Town" and "P3 Games"; "Melty" and "Stella Montis"; and
"SimCity" are named in the documentation only to identify the products concerned. No logos, trade dress, or branding
are used. Player-facing names are reviewed before release so that they do not imply affiliation (RISK-015).

## 7. Audit procedure for a new component

**Proposed procedure (requires validation).**

1. Add a row to §3 with the role, the license as found, the evidence level (read the raw license at a pinned revision), and the status "Candidate".
2. Check: copyright holder, license history, per-file headers, embedded third-party material, binaries, assets, and declared dependencies (transitively).
3. Record open items as `OPEN-*` entries. Do not incorporate while any item blocks.
4. Obtain the owner's approval (and legal review if the owner requests it).
5. At incorporation, add the notice and headers in the same change, and set the status to "Incorporated (date, SHA)".
6. Before each release (PHASE-13), re-verify every incorporated row against the shipped files.

## 8. Open questions and risks

- **Central:** OPEN-004, OPEN-010, OPEN-017, OPEN-027, OPEN-034, RISK-002, RISK-015, RISK-029.
- **New:**
  - OPEN-401: Melty exclusivity.
  - OPEN-404: provenance of decompiled material.
  - OPEN-405: the copyright holder and license history.
  - OPEN-406: the Unity license.
  - RISK-401: Melty exclusivity could block distribution.
  - RISK-404: provenance taint.
- **Proposed by TASK-0002** (pending register entries by the Coordinator):
  - OPEN-411: the "CS2 GTA Edition" collision-code origin and license.
  - RISK-411: collision code of unclear third-party origin.

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
