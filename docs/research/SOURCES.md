# Sources and Provenance Register

| Field | Value |
|---|---|
| ID | DOC-SOURCES |
| Document status | Draft |
| Scope | Every external or historical source cited by the documentation baseline (SRC-001 … SRC-014) |
| Phases | PHASE-00 (created); maintained in every phase |
| Responsible role | Coordinator (maintainer); any role may add a source or re-verify one |
| Related documents | [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md), [CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md), [FEASIBILITY_MATRIX](FEASIBILITY_MATRIX.md), [MELTY_EVALUATION](MELTY_EVALUATION.md), [MOTOR_TOWN_REFERENCE](MOTOR_TOWN_REFERENCE.md), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md) |
| Last updated | 2026-10-18 |

## 1. Purpose

This register is the single home for **where an external claim comes from**, how strongly it was checked, and what
remains uncertain. Other documents cite a source by its `SRC-NNN` ID and link here. A source being listed here is
**not** the same as it being inspected, and a claim being inspected is not the same as it being built or tested by us.

All research recorded here was **read-only**. On 2026-10-18 nothing was cloned, downloaded as an archive, installed,
built, or executed. Instructions found inside fetched pages were treated as data, never as instructions.

## 2. Verification levels (legend)

| Level | Meaning | How it was obtained | How far you may rely on it |
|---|---|---|---|
| **Raw-verified** | Exact text confirmed byte-for-byte. | Read-only HTTP GET with `curl`, then a text search of the raw response. Items V-01 … V-10 were checked by the coordinator on 2026-10-18. A few additional lines were read by the research writer from **the same fetched raw files**; they are marked "(same raw fetch)". For SRC-001, TASK-0002 (2026-10-18) read **every text file** raw, streamed to the terminal from SHA-pinned URLs and not saved; these are marked "(raw at pin)". | Strongest level available today. It proves what the upstream text says, not that the behavior works. |
| **Extracted** | Content obtained through a summarizing fetch tool (WebFetch). | The tool returns text through a language model that summarizes or extracts. It declined to reproduce some files verbatim. | Identifiers, numbers, and quotations may contain extraction errors. Re-check against raw text before relying on a specific symbol (RISK-406). |
| **Partial** | The page answered, but only its title was extractable (JavaScript-rendered shell). | WebFetch. | Confirms that the page exists, nothing more. |
| **Snippet only** | Seen only in web-search result summaries; the page was not fetched. | WebSearch. | Not used as authority anywhere. |
| **Listed only** | Named or linked by another source but not read. | — | No claim may rest on it. |
| **Historical** | Read from the local historical input `chat.md`. | Local file read. | Unverified; see [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §6. |
| **Owner** | Stated by the project owner. | Owner brief of 2026-10-18. | Binding for scope; not a technical fact about third-party software. |

Claim labels used below follow [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §6. **[Source-inspected]** means we
read the upstream source files read-only. In this register it is qualified as "(raw text verified 2026-10-18)" for
Raw-verified items and "(extraction)" for Extracted items. **[Press, unverified]** marks claims reported only by
secondary journalism. **[Built locally]** and **[Tested in-game]** do not appear: nothing has been built or run.

## 3. Source index

| ID | Source | Kind | Access on 2026-10-18 | Highest level reached |
|---|---|---|---|---|
| SRC-001 | Beta Test Driving Mod repository (GitHub) and its Paradox Mods listing | Community open-source repository (candidate base) | OK (listing: partial) | Raw-verified at the pinned commit `eefd6bb…` for every text file (TASK-0002); V-01 … V-07 earlier |
| SRC-002 | Paradox "Modding Development Diary #3: Code Modding" | Official developer dev diary | OK | Extracted |
| SRC-003 | Motor Town: Behind The Wheel, Steam store page | Developer store page | OK | Extracted |
| SRC-004 | Melty website, download page, Terms, Privacy | Official product site and legal terms | OK (one guessed URL 404) | Raw-verified (V-08, V-09) |
| SRC-005 | CitiesSkylinesModding/agents-plugins (GitHub) | Community tooling and knowledge repository | OK (tree truncated) | Raw-verified (V-10) |
| SRC-006 | `docs/archive/chat.md` (in the project root until 2026-10-20) | Historical input (Portuguese) | Read locally | Historical |
| SRC-007 | Owner brief of 2026-10-18 | Owner instruction | Received in the documentation session | Owner |
| SRC-008 | Paradox CS2 Wiki: "Modding Toolchain" | Official wiki page | OK | Extracted |
| SRC-009 | Paradox CS2 Wiki: "UI Modding" | Official wiki page | OK | Extracted |
| SRC-010 | Paradox CS2 Wiki: "Mod Key Binding" | Official wiki page | OK | Extracted |
| SRC-011 | Paradox CS2 Wiki: Modding hub, plus community pages "Systems" and "ECS" | Official hub and community wiki pages | OK | Extracted |
| SRC-012 | Paradox dev diary #1 "Paradox Mods in Cities: Skylines II", plus the Paradox Mods site | Official dev diary; platform site | OK (site: partial) | Extracted |
| SRC-013 | BrokeAssSoftware/cs2-modding-guide (GitHub) | Community handbook | OK | Extracted |
| SRC-014 | Dexerto article on Melty | Press (secondary) | OK | Extracted |

## 4. Source entries

### SRC-001 — Beta Test Driving Mod (candidate base mod)

| Field | Value |
|---|---|
| URL(s) | [github.com/northernst11-bot/BetaTestDrivingMod](https://github.com/northernst11-bot/BetaTestDrivingMod); GitHub REST API endpoints for the repository, commits (including per-commit file lists and two diffs), tree, tags, releases, pulls, issues, and forks; raw files under `https://raw.githubusercontent.com/northernst11-bot/BetaTestDrivingMod/eefd6bb4fe2dcc479df837a1678f3529ac6e0f76/` (TASK-0002) and earlier under `…/main/` (bootstrap); Paradox Mods listing [mods.paradoxplaza.com/mods/145145/Windows](https://mods.paradoxplaza.com/mods/145145/Windows) |
| Publisher / project | GitHub account `northernst11-bot` (repository owner). The LICENSE copyright line, `mod.json` author, and `AssemblyCompany` are "stoot". All 8 commits are authored as "stoot", and GitHub associates them with the account login `stoot`. The mod is titled "Beta Test Driving Mod (Stable)". The relationship between the two accounts is not stated (OPEN-405). |
| Kind | Community open-source repository; candidate source foundation **[Owner requirement]** |
| Access status | GitHub: OK. Paradox Mods listing: Partial (title "Beta Test Driving Mod (Stable) - Paradox Mods" only). |
| Access date | 2026-10-18 (bootstrap review and TASK-0002 audit) |
| Revision | **Pinned by TASK-0002: `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76`**, 2026-06-17T10:19:57Z, "Publish 0.3.54 stable smoother driving update". *Verified command (2026-10-18, TASK-0002 auditor):* `git ls-remote https://github.com/northernst11-bot/BetaTestDrivingMod` returned this hash for `HEAD` and `refs/heads/main` and no other references. This is unchanged from the bootstrap candidate (V-02). The API lists 8 commits, no tags, no releases, no pull requests, 1 open issue (#1), and 2 forks (not inspected). |
| Verification level | **Raw-verified at the pinned commit** for every text file in the tree (TASK-0002; coverage table in [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §9.2). Binary images: not read. Earlier: V-01 … V-07 raw from `main`; extraction for the rest, now superseded. |

**Claims supported**

- **[Source-inspected] (raw at pin, TASK-0002, 2026-10-18).** Summary; full detail in [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §9:
  - `Mod.cs` implements `IMod` (`OnLoad(UpdateSystem)`/`OnDispose()`). It registers five systems: UI and input in `UIUpdate`; freeze and control around `CarNavigationSystem`/`CarMoveSystem` in `GameSimulation`; camera after `CameraUpdateSystem` in `PreCulling`.
  - It is a native CS2 code mod. There is no Harmony, BepInEx, or reflection patching, and no Burst, jobs, or `SystemAPI`.
  - The csproj is SDK-style `net48`, C# 9.0, with relative `ManagedPath` `..\..\Cities2_Data\Managed` and 18 game references (`Private=false`). It has no `Import`, `PackageReference`, or post-build step.
  - `GameVersion` "1.5.*" appears at every sampled revision. There is no runtime version check. No dependencies are declared.
  - `LICENSE` is MIT at the pin. The first 4 commits (2026-05-30) carried a restrictive "Source-Available Transparency License"; MIT applies from `af355236521636e987801e4c14019aaf187ec277` (2026-05-31). No file has a license header.
  - The `decompiled_published_0317` exclusion was added in `2c5b83085290b9d578ae6eff30f9241d8b0299c5` (2026-06-04). No commit ever contained the folder.
  - Input is legacy `UnityEngine.Input`; key bindings are kept in `PlayerPrefs` under the prefix `BetaTestDrivingMod.DirectDrive.Key.`. Nothing is persisted in saves.
  - The "road intent assist" turn-queue code is never called (dead code).
  - The traffic-presence, traffic-guard, police-chase, and `ReliableDrive*` systems are compiled but not registered.
  - The pinned tree has no committed binaries other than 6 PNG/JPG images.
- **[Documented upstream] (TASK-0002, raw at pin):**
  - The CHANGELOG entry `0.3.17-collision-tuning-public` "Ports the CS2 GTA Edition vehicle collision layer"; entry `0.3.18-crashfix-local` "Restores the local test cache from the published `0.3.17-collision-tuning-public` package".
  - Issue #1 (open, 2026-10-05, third party) announces a fork, Paradox Mods id 162039. Its first "finding" names a method (`SetVehicleKind`) that does not exist at the pin.
- **[Source-inspected] (raw text verified 2026-10-18, bootstrap)**:
  - `LICENSE` reads "MIT License" / "Copyright (c) 2026 stoot" (V-01).
  - The repository root contains `.gitignore`, `BetaTestDrivingMod.csproj`, `BetaTestDrivingMod.mjs`, `CHANGELOG.md`, `LICENSE`, `Mod.cs`, `NOTICE.md`, `Properties`, `README.md`, `Resources`, `Systems`, `images`, `mod.json` (V-03). No `decompiled_published_0317` folder is present at the root.
  - The project file targets `net48` with `LangVersion` 9.0, uses the relative `ManagedPath` `..\..\Cities2_Data\Managed`, excludes `decompiled_published_0317\**\*.cs` from compilation, and references game, Colossal, Unity, and UnityEngine assemblies plus `System.Memory` through HintPaths. It contains no `PackageReference` or `Import` lines (V-04).
  - `Properties/PublishConfiguration.xml` declares `ModId` 145145, `ModVersion` "0.3.54-stable", and `GameVersion` "1.5.*" (V-06). It contains no `Dependency` element (same raw fetch).
  - `NOTICE.md` says the project is "open source under the MIT License" and that the license "does not grant rights to Cities: Skylines II, Paradox or Colossal Order trademarks, game assets, or third-party content not owned by this project" (V-07).
- **[Documented upstream] (README, raw text verified 2026-10-18, V-05 and same raw fetch)**:
  - Line 19: "It does not spawn a fake player car. It takes over a real live vehicle already created by Cities: Skylines II, freezes the possessed car's vanilla physical path movement, then applies direct player movement after the normal car move step."
  - Line 111: "Building collision and crash fire effects are not added in this build."
  - Line 17: the police chase is "hidden and disabled in this public build".
  - Line 96: vehicle collision targets on different road heights are skipped "so cars below/above bridges do not create invisible barriers."
  - Line 34: the 0.3.53 release "Rebuilds vanilla pathfinding from the car's current lane and position when you release control."
  - Line 68: the build "Purges the possessed car from every lane-buffer marker touched during the drive so lanes you left should not keep an invisible queue blocker."
  - Line 106: collision checks no longer push or rewrite "other live traffic vehicles on impact".
  - Line 55: "Use the arrow keys if WASD moves your game camera or screen."
  - The README states no supported game version, required mods, build steps, or installation steps.
- **[Source-inspected] (extraction, bootstrap; superseded by the TASK-0002 raw reading above).** Corrections are in [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §9.3.
  - `Mod.cs` implements `IMod` with `OnLoad(UpdateSystem)`/`OnDispose()` and registers five systems (UI, input, freeze, control, camera).
  - Input uses legacy `UnityEngine.Input` with key bindings stored in `PlayerPrefs`.
  - There is no save-game serialization.
  - The UI uses a `UISystemBase` binding system, a hand-written `.mjs` module, and an IMGUI fallback.
  - The traffic-presence, traffic-guard, and police-chase systems and the older `ReliableDrive*` stack are present but not registered in `Mod.cs`.
  - `CHANGELOG.md` stops at `0.3.27-stable` while the README and manifest say 0.3.54.
  - `Properties/AssemblyInfo.cs` has an empty `AssemblyCopyright`. `mod.json` declares `dependencies: []`.
  - Commit `af355236…` (2026-05-31) is titled "Relicense mod source under MIT". Only the abbreviated SHA was captured.
  - Issue #1 (open, 2026-10-05) announces a community MIT fork with Chinese localization and a handling rework, and reports two suspected bugs.
  - Details: [BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §2.

**Remaining uncertainty:**

- whether the mod builds or runs against the owner's game version, 1.6.2f1 versus the declared 1.5.* (OPEN-005);
- how the published build was produced (OPEN-403);
- the provenance of `decompiled_published_0317` (OPEN-404) and of the "CS2 GTA Edition" collision layer (proposed OPEN-411);
- the relationship between the accounts `stoot` and `northernst11-bot` (OPEN-405). The license history itself is now answered;
- every runtime behavior, which is not observable from source.

**Listed but not inspected:**

- the binary image files (identities taken from the tree only);
- the contents of the two forks and of the issue #1 fork;
- the forum thread linked in the publish description;
- the Paradox Mods listing content (subscribers, displayed version, author).

The earlier gaps (the tail of `DirectDriveControlSystem.cs`, the `ReliableDrive*` files, and the history before
`eefd6bb…`) were closed by TASK-0002. Per-commit file lists were read for all 8 commits. Full diffs were read only for
`af35523…` (CHANGELOG, `PublishConfiguration.xml`, README, `mod.json`) and `2c5b830…` (csproj, `Mod.cs`, NOTICE).

### SRC-002 — Paradox dev diary #3: Code Modding

| Field | Value |
|---|---|
| URL(s) | [paradoxinteractive.com/…/dev-diary-3-code-modding](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding) |
| Publisher / project | Paradox Interactive / Colossal Order; the page names the author "Sergey" (MacSergey), a Colossal Order developer (extraction). |
| Kind | Official developer dev diary |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | Page date 2024-03-21 (before game 1.1.x). Not tagged with a verified game version. |
| Verification level | Extracted |

**Claims supported (all [Documented upstream])**

- A one-click installer sets up external tools such as the Unity engine, Burst compiler, and ECS.
- Projects use the .NET templates mechanism; the IDE is Visual Studio or Rider.
- Mods add features by creating their own system and registering it "in the updating loop", and they can access game entities and components.
- Mods are published to Paradox Mods from the IDE.
- Settings UI is generated automatically from properties marked with attributes.
- "Harmony can patch only managed code": Burst-compiled code cannot be patched.
- Unity API calls without the provided setup throw a "NotImplemented" exception.
- There is "no such thing as a mod loading order".

**Remaining uncertainty:** the page predates current game builds. It does not state the Unity version, the .NET target, entry-point names, the UI stack, save serialization, or debugging.

### SRC-003 — Motor Town: Behind The Wheel (Steam store page)

| Field | Value |
|---|---|
| URL(s) | [store.steampowered.com/app/1369670/Motor_Town_Behind_The_Wheel/](https://store.steampowered.com/app/1369670/Motor_Town_Behind_The_Wheel/) |
| Publisher / project | P3 Games (developer and publisher, per the page) |
| Kind | Developer store page |
| Access status | OK (no age gate) |
| Access date | 2026-10-18 |
| Revision | None shown (live page). The Early Access text and feature list may have changed since they were written. |
| Verification level | Extracted |

**Claims supported (all [Documented upstream]):** the release date (Dec 17, 2021) and Early Access label; the listed
job types (taxi, bus, trucks, hitchhikers); buying, renting, and part replacement; company creation with
bus or truck routes and hired drivers; racing and time attack; online modes; and an open world of 24 x 16 km. Some items are
written in the future tense ("will be added"). Full detail: [MOTOR_TOWN_REFERENCE](MOTOR_TOWN_REFERENCE.md).

**Remaining uncertainty:** whether future-tense items exist today; any progression system (not described). Nothing
about the game's code, assets, or physics is known or assumed.

### SRC-004 — Melty (melty.gg)

| Field | Value |
|---|---|
| URL(s) | [melty.gg](https://melty.gg/), [melty.gg/download](https://melty.gg/download), [melty.gg/terms](https://melty.gg/terms), [melty.gg/privacy](https://melty.gg/privacy) |
| Publisher / project | Stella Montis Inc. (homepage footer, raw V-08; Terms and Privacy) |
| Kind | Official product site and legal terms |
| Access status | OK. A guessed URL `melty.gg/llms.txt` returned HTTP 404. |
| Access date | 2026-10-18 |
| Revision | Terms "Last updated: October 18, 2026" (V-09), the same day as access, which suggests frequent changes. Privacy "Last updated: October 12, 2026" (extraction). Homepage undated. |
| Verification level | Raw-verified (V-08, V-09); Extracted for Privacy and the download page |

**Claims supported**

- **[Documented upstream] (raw text verified 2026-10-18)**:
  - The homepage says "Copy Melty's instructions into Claude Code or Codex and say what you want. Your agent builds it and publishes it." and "Free for Windows." (V-08).
  - The homepage text does not contain "Cities", "Skylines", or "Motor Town" (V-08).
  - The homepage also says "Every upload is checked for dangerous files" (same raw fetch).
  - The Terms say "Melty is the only marketplace for your Content" and that publishing gives Melty "an exclusive, worldwide, royalty-free licence, which Melty may sublicense" (V-09).
  - The Terms say the exclusive licence lasts while the content is on Melty "and for 12 months after you take it off Melty", and covers modified, partial, renamed, or ported versions and remixes (same raw fetch).
  - The Terms say content shared on another marketplace *before* first publication on Melty "may stay there as it is, but new versions and releases follow this section", and that creators may keep source code in their own repository, "for example on GitHub" (same raw fetch).
  - The Terms say the app may find installed games, download "mod loaders, launchers and build tools", "write files into your game folders and other folders a mashup needs, after keeping a copy of any file it replaces", "look for that copy in your folders and drives", start games and programs, and "update itself automatically" (V-09 and same raw fetch).
  - The Terms mention publishing "automatically from a GitHub repository you connected" (same raw fetch).
- **[Documented upstream] (extraction)**:
  - Melty is a Windows launcher for "game mashups".
  - Agent tokens and "If you connect a coding agent to Melty, it acts for you".
  - Uploads are audited before release.
  - Using mashups or Melty data "to train or improve AI models" is prohibited.
  - Governing law is Delaware.
  - Privacy: installed-game scanning, optional session recordings limited to the Melty window, and 30-day sanitized crash logs.
- **Not documented** on the pages read: a project-folder or workspace selector, an integrated agent, "MCP", or "Melty Chat".

**Remaining uncertainty:** the form of "Melty's instructions"; whether games that are not listed (CS2, Motor Town) can
be targeted; the app's actual behavior (not installed). Legal interpretation of the Terms is **not** made here
(OPEN-401). Evaluation: [MELTY_EVALUATION](MELTY_EVALUATION.md).

### SRC-005 — CitiesSkylinesModding/agents-plugins

| Field | Value |
|---|---|
| URL(s) | [github.com/CitiesSkylinesModding/agents-plugins](https://github.com/CitiesSkylinesModding/agents-plugins); API endpoints for the repository, commits, contents, and tree; raw `LICENSE`, `.claude-plugin/marketplace.json`, the `unity-devtools` and `coherent-gameface` READMEs, the `cs2-modding` and `cs2-mod-project` `SKILL.md` files, and `…/references/technique/save-serialization/save-serialization.md` |
| Publisher / project | GitHub organization "CitiesSkylinesModding" (marketplace owner "Cities: Skylines Modding") |
| Kind | Community agent-tooling and knowledge repository (not Paradox-official) |
| Access status | OK. One wrong raw path returned 404 and the corrected path returned OK. The recursive tree response was truncated. |
| Access date | 2026-10-18 |
| Revision | Latest commit `e966c0192e57357db99c2471ae3420c87c60ccdb`, 2026-10-17T10:16:41Z (V-10). The `cs2-modding` knowledge states that it was verified against the decompiled game, version 1.6.2f1 (extraction). |
| Verification level | Raw-verified for the license and commit (V-10); Extracted otherwise |

**Claims supported**

- **[Source-inspected] (raw text verified 2026-10-18):** "MIT License" / "Copyright (c) 2026 Morgan Touverey Quilling" (V-10).
- **[Documented upstream] (community, extraction)**:
  - Three plugins exist: `coherent-gameface` (drives a running Gameface UI over the Chrome DevTools Protocol; needs a reachable CDP endpoint, default `http://localhost:9444`; how to enable it in CS2 is not explained), `unity-devtools` (needs "a running Unity Mono development build"; "a retail build exposes no SDB port"; "Writes are live"), and `cs2-modding` (knowledge only).
  - "`Mod.props` fixes `net48` to match the game's runtime" and pins C# 9.
  - The C# template is `dotnet new csiimod`.
  - Harmony `Lib.Harmony` 2.2.2 is "pinned by community agreement".
  - Save serialization uses `ISerializable` on components and `IDefaultSerializable` on systems, with an own version int.
  - "uninstalling a mod loses that mod's components and keeps the city".
  - The plugin installation commands are documented upstream and were **not executed**.

**Remaining uncertainty:** all technical claims are community-authored, derived from decompiled code and static
analysis, and pinned to specific game versions (RISK-405).

**Listed but not inspected:** the `cs2-modding-setup` and `cs2-modding-ui` skills, the plugin source code, and the rest
of the truncated tree.

### SRC-006 — `chat.md` (historical input)

| Field | Value |
|---|---|
| URL(s) | Local file [chat.md](../archive/chat.md) in `docs/archive/` (in the project root until 2026-10-20; content unchanged) |
| Publisher / project | The project owner's earlier planning conversation with an AI assistant, exported to Markdown |
| Kind | Historical input (Portuguese). Kept unchanged; never edited or translated. |
| Access status | Read locally |
| Access date | 2026-10-18 |
| Revision | 56,022 bytes, 1,012 lines, as observed by the coordinator on 2026-10-18. The beginning is truncated ("Loading older messages…") and its web citations were stripped. |
| Verification level | Historical |

**Claims it contains (all [Historical input])**

Corroborated later:

- the base mod is MIT-licensed (now V-01);
- the base mod does not implement building collisions (now V-05);
- the base project uses `net48` and should not be migrated just because a tool needs a newer SDK (`net48` now V-04);
- `unity-devtools` needs a Unity Mono development build (now SRC-005, extraction).

Not confirmed:

- Melty's creator flow consists of selecting the games, copying generated instructions, and giving them to Claude Code or Codex. The homepage text read on 2026-10-18 describes copying instructions but no game selection.
- An earlier checklist installed and ran Motor Town and Melty; this is superseded by the owner brief (CONF-009).

The owner's observation recorded there is that Melty is a standalone program, more like a desktop chat application,
with no visible option to choose the project folder.

**Remaining uncertainty:** anything not corroborated by a newer source (RISK-027).

### SRC-007 — Owner brief of 2026-10-18

| Field | Value |
|---|---|
| URL(s) | None (delivered to the documentation session) |
| Publisher / project | Project owner |
| Kind | Owner instruction |
| Access status | Received and read in full |
| Access date | 2026-10-18 |
| Revision | Single version of 2026-10-18; its requirements are captured in the [PRD](../product/PRD.md), the [ROADMAP](../../ROADMAP.md), and [TASK-0000](../planning/tasks/TASK-0000-documentation-bootstrap.md). |
| Verification level | Owner |

**Claims supported ([Owner requirement])**

- the roles of CS2 (sole host and world authority), the Beta Test Driving Mod (candidate, conditional on audit and baseline), Motor Town (design reference only), Streets of SimCity (inspiration only), and Melty (optional, to be evaluated);
- the v1.0 scope and explicit exclusions;
- the permissions and prohibitions for this baseline;
- the seed URLs used for SRC-001 … SRC-005;
- the source-verification policy;
- the owner's description of Melty as a standalone application in which no project-folder selector was found.

### SRC-008 — Paradox CS2 Wiki: "Modding Toolchain" (official)

| Field | Value |
|---|---|
| URL(s) | [cs2.paradoxwikis.com/Modding_Toolchain](https://cs2.paradoxwikis.com/Modding_Toolchain) |
| Publisher / project | Paradox Interactive (official wiki page; "Beta Documentation") |
| Kind | Official documentation |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | Revision 6258, "last edited on 25 April 2026, at 08:41"; banner "verified by Paradox Interactive for version 1.1.12 f1". |
| Verification level | Extracted |

**Claims supported (all [Documented upstream], verified for game 1.1.12f1 only)**

- Installation:
  - The toolchain is installed from in-game **Options → Modding**.
  - Components: Unity Editor 2022.3.7f1 plus a Unity personal or commercial license; .NET SDK minimum 6, "8 is recommended"; Node.js minimum 18 (installs 20.11, UI mods only); Visual Studio 2022 (17.8) or Rider (2021.3.3); VS Code (1.86) for UI.
  - The C# template is a NuGet package used via `dotnet new`; the UI template is `npx create-csii-ui-mod`.
- Build and environment:
  - `CSII_*` environment variables, and Mod.props/Mod.targets added automatically.
  - An AfterBuild step runs ModPostProcessor.exe (IL post-processing, Burst).
- Local mods:
  - Output goes to `…\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\<ModName>`, and the game loads local mods automatically.
  - A folder whose name starts with "." is disabled.
  - Mods are delay-loaded until login is viable.
- Publishing:
  - ModPublisher profiles `PublishNewMod`, `PublishNewVersion`, and `UpdatePublishedConfiguration`.
  - A Paradox account login in the game is required.

**Remaining uncertainty:** the verification tag is far older than current game builds, and the page says the Unity
version tracks game updates (OPEN-002, OPEN-406). The page does not state the target framework.

### SRC-009 — Paradox CS2 Wiki: "UI Modding" (official)

| Field | Value |
|---|---|
| URL(s) | [cs2.paradoxwikis.com/UI_Modding](https://cs2.paradoxwikis.com/UI_Modding) |
| Publisher / project | Paradox Interactive (official; "Beta Documentation") |
| Kind | Official documentation |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | "last edited on 25 April 2026, at 11:41"; verified for game 1.5.7f1 |
| Verification level | Extracted |

**Claims supported ([Documented upstream]):**

- The UI stack is Coherent Gameface, React, SCSS, TypeScript, and Webpack. Only flex layout is supported; "grid layouts are not supported".
- A Module Registry offers Append hooks including `Game`, `GameTopLeft`, and `UniversalModMenu` (from 1.5.7).
- Bindings use `cs2/api` (`bindValue`, `trigger`, `useValue`).
- `cs2/input` is "not yet available".

The page does not name the C#-side binding classes or a debugger port.

### SRC-010 — Paradox CS2 Wiki: "Mod Key Binding" (official)

| Field | Value |
|---|---|
| URL(s) | [cs2.paradoxwikis.com/Mod_Key_Binding](https://cs2.paradoxwikis.com/Mod_Key_Binding) |
| Publisher / project | Paradox Interactive (official) |
| Kind | Official documentation |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | Revision 6263, "last edited on 25 April 2026, at 14:14"; verified for game 1.3.6f1 |
| Verification level | Extracted |

**Claims supported ([Documented upstream]):**

- Mods declare `ProxyBinding` properties on their `ModSetting` subclass, with keyboard, mouse, and gamepad binding attributes.
- `RegisterKeyBindings()` is called in `OnLoad`; actions are obtained with `GetAction`.
- The game input system is based on the Unity Input System.
- Conflicts with built-in bindings disable "all conflicted mod bindings".

The page does not say where rebinds persist and does not discuss legacy `UnityEngine.Input`.

### SRC-011 — Paradox CS2 Wiki: Modding hub, "Systems", and "ECS"

| Field | Value |
|---|---|
| URL(s) | [cs2.paradoxwikis.com/Modding](https://cs2.paradoxwikis.com/Modding), [cs2.paradoxwikis.com/Systems](https://cs2.paradoxwikis.com/Systems), [cs2.paradoxwikis.com/ECS_-_Entity_Component_System](https://cs2.paradoxwikis.com/ECS_-_Entity_Component_System) |
| Publisher / project | Paradox wiki. The hub is official; "Systems" and "ECS" are community-made guides. |
| Kind | Official hub; community wiki pages |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | Hub last edited 25 June 2026, 04:56. Systems last edited 23 March 2025 ("Potentially outdated"; some sections verified for 1.0). ECS last edited 7 June 2024. |
| Verification level | Extracted |

**Claims supported:**

- **[Documented upstream]** The hub separates Paradox-reviewed official pages from community guides.
- **[Documented upstream] (community)**:
  - The update phases include `GameSimulation`, `UIUpdate`, and `Serialize`, among others.
  - Systems are registered with `UpdateAt`/`UpdateBefore`/`UpdateAfter`.
  - `GetUpdateInterval` "must be a power of 2".

No official page on save serialization or on simulation speed and pause was found.

**Listed but not inspected:** the hub's links to "Options UI", the publishing pages, "Creating a Settings File",
"Logging", "Debugging", and "Mod security".

### SRC-012 — Paradox dev diary #1 and the Paradox Mods site

| Field | Value |
|---|---|
| URL(s) | [paradoxinteractive.com/…/dev-diary-1-paradox-mods](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-1-paradox-mods); [mods.paradoxplaza.com/games/cities_skylines_2](https://mods.paradoxplaza.com/games/cities_skylines_2) |
| Publisher / project | Paradox Interactive |
| Kind | Official dev diary; platform site |
| Access status | Dev diary OK. Site: Partial (title only). |
| Access date | 2026-10-18 |
| Revision | Dev diary page date 2024-03-19; no author named |
| Verification level | Extracted (dev diary); Partial (site) |

**Claims supported ([Documented upstream]):**

- Paradox Mods is the official CS2 mod backend for PC and consoles.
- Metadata is editable on the website.
- Playsets sync across devices; local mods can join playsets but are not cloud-synced.
- "we do resolve the entire tree of dependencies".
- Creators can list supported game versions.

The claims that a Paradox account is required and that mods are free were seen as **snippets only** and are not
relied on.

### SRC-013 — BrokeAssSoftware/cs2-modding-guide

| Field | Value |
|---|---|
| URL(s) | [github.com/BrokeAssSoftware/cs2-modding-guide](https://github.com/BrokeAssSoftware/cs2-modding-guide); raw `explanation/serialization.md`, `reference/sdk-and-toolchain.md`, `how-to/recipes/simulation-speed-control.md` (branch `integration`) |
| Publisher / project | GitHub account BrokeAssSoftware |
| Kind | Community handbook; MIT according to the page (extraction) |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | `1903c032ccb3b005eef83901ba3918f2f5652258` (2026-07-06T06:43:52Z). Serialization pinned to game "~1.5.10f1"; the SDK reference is pinned to 1.6.0f1 and was reverified 2026-07-04. |
| Verification level | Extracted |

**Claims supported ([Documented upstream], community):**

- Serialization:
  - Saves are written in `SystemUpdatePhase.Serialize` and loaded in `Deserialize`.
  - Data should carry an explicit leading version int.
  - Restore work should be deferred out of `Deserialize`.
- Simulation speed:
  - `SimulationSystem.selectedSpeed` (user-picked) versus `smoothSpeed` (the eased running value).
  - The value while paused is marked "Needs Verification (in-game)".
- SDK:
  - The target framework is not mandated (one cited mod uses `net472`).
  - Mod.props/Mod.targets are imported from `CSII_TOOLPATH`.

**Listed but not inspected:** all other files in the guide.

### SRC-014 — Dexerto article on Melty

| Field | Value |
|---|---|
| URL(s) | [dexerto.com/gaming/melty-game-launcher-…-3419011/](https://www.dexerto.com/gaming/melty-game-launcher-lets-you-play-ai-game-mashups-in-one-click-but-theres-a-catch-3419011/) |
| Publisher / project | Dexerto; byline Zackerie Fairfax |
| Kind | Press (secondary) |
| Access status | OK |
| Access date | 2026-10-18 |
| Revision | Dated Oct 14, 2026 |
| Verification level | Extracted |

**Claims supported ([Press, unverified]):**

- The article names a founder.
- It reports announcements of "Melty MCP" (Oct 11) and "Melty Chat" (Oct 14). Neither appears on melty.gg as read on 2026-10-18.
- It reports two claims and flags both as **not independently verified**: Melty's pre-publication audit, and a third-party claim (Oct 5) that installer v1.0.33 contains components of a gameplay- and input-recording app. The article states that the documents "do not, by themselves, establish that Melty is malware".

These claims are reasons for caution only (RISK-402), not facts.

**Listed but not inspected:** the third-party documents that the article refers to.

## 5. Attempted URLs that did not yield content

| URL (not linked) | Result | Note |
|---|---|---|
| `https://cs2.paradoxwikis.com/Mod_Development_Environment` | HTTP 404 | Guessed URL; the page does not exist. |
| `https://melty.gg/llms.txt` | HTTP 404 | Guessed URL. |
| `…/agents-plugins/…/references/technique/save-serialization.md` | HTTP 404 | Wrong path; the corrected path (SRC-005) returned OK. |

## 6. Adding or re-verifying a source

1. Assign the next free `SRC-NNN`. Record the URL, publisher, kind, access status, the actual access date, the revision (commit SHA, page revision, "last edited", or verified-for game version), and the verification level.
2. Write each claim with its label. Quote short phrases only, with attribution. Never paste whole third-party files.
3. For any identifier that design or code will rely on, reach **Raw-verified** first. For repositories, use SHA-pinned URLs.
4. When a newer source contradicts an older one, keep both entries, record the contradiction, and update the affected documents.

## 7. Related open questions and risks

All of these are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md):

- **Central:** OPEN-002, OPEN-003, OPEN-004, OPEN-016, OPEN-027, OPEN-031, RISK-027.
- **New in this baseline:** OPEN-401 to OPEN-407, RISK-401 to RISK-406.
- **Proposed by TASK-0002** (pending register entries by the Coordinator): OPEN-411 to OPEN-415, RISK-411 to RISK-413 ([BASE_MOD_AUDIT](BASE_MOD_AUDIT.md) §8).
