# Melty Evaluation

| Field | Value |
|---|---|
| ID | DOC-MELTY |
| Document status | Draft |
| Release scope | Development workflow (optional); distribution strategy input |
| Phases | PHASE-00 (owner-attended evaluation); PHASE-13 (distribution decision) |
| Responsible role | Coordinator (maintainer); the project owner runs the observation and decides adoption |
| Related requirements | REQ-REL-004 (no Melty at runtime), REQ-REL-003 |
| Related documents | [SOURCES](SOURCES.md) (SRC-004, SRC-006, SRC-007, SRC-014), [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md), [PACKAGING_AND_PUBLISHING](../release/PACKAGING_AND_PUBLISHING.md), [TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md), [MOTOR_TOWN_REFERENCE](MOTOR_TOWN_REFERENCE.md) |
| Last updated | 2026-10-18 (connected inspection, §9) |

> **Position:** Melty is an **optional** complementary application **[Owner requirement]**. The owner installed Melty
> and connected its tools to Claude Code. On 2026-10-18 an agent used those tools **read-only**, with the owner's
> authorization (§9). Nothing was created, uploaded, tested, or published. The project and its documentation work
> without Melty, and **Melty never blocks any phase**. Nothing here is a legal conclusion.
>
> **Current outcome:** Melty is not adopted for distribution. Its only CS2 route (BepInEx 5 installed into the game
> folder) does not match the native code-mod route this project follows
> ([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)). It remains "Reference only"
> pending the owner's decision.

## 1. Purpose

This document records what is documented about Melty and the questions an evaluation must answer. It defines an
owner-attended, observation-first procedure, the adoption criteria, and the fallback that applies in every case.

## 2. The owner's description

The owner describes Melty as a **standalone Windows application** and has **not found a project-folder selector**
(SRC-007). The earlier planning conversation records the same observation: the program is more like a desktop chat
application, with no visible option to choose the project folder (SRC-006, **[Historical input]**).

This document does not invent an "Open Folder" feature, a workspace setting, a directory, an integrated agent, or any
compatibility guarantee.

## 3. Documented facts (SRC-004, accessed 2026-10-18)

| # | Fact | Level |
|---|---|---|
| F-01 | Publisher: "© 2026 Stella Montis Inc." | **[Documented upstream]** (raw text verified 2026-10-18, V-08) |
| F-02 | Windows only: "Free for Windows." No other platform is mentioned. | Raw (V-08) |
| F-03 | A launcher for "game mashups" ("A mod that joins games into one") and mods for games the user owns | Raw (same fetch as V-08) |
| F-04 | Creator flow: "Copy Melty's instructions into Claude Code or Codex and say what you want. Your agent builds it and publishes it." | Raw (V-08) |
| F-05 | The homepage text does not contain "folder", "workspace", "MCP", or "Chat". The Terms mention folders only for the app writing into game folders and other folders a mashup needs. **No project-folder selection and no integrated agent are documented.** | Raw homepage text; Terms extraction |
| F-06 | Cities: Skylines II and Motor Town are **not listed on the public homepage**: it does not contain "Cities", "Skylines", or "Motor Town". Melty's connected catalog does include CS2 (§9, M-02); Motor Town is not in the catalog (M-07). | Raw (V-08); Melty API (§9) |
| F-07 | The Terms grant Melty "an exclusive, worldwide, royalty-free licence, which Melty may sublicense" for published content, and state that "Melty is the only marketplace for your Content". | Raw (V-09) |
| F-08 | The exclusivity covers modified, partial, renamed, or ported versions and remixes. It lasts while the content is on Melty "and for 12 months after you take it off Melty". Content already on another marketplace before first publication on Melty "may stay there as it is, but new versions and releases follow this section". Source code may stay in the creator's own repository "for example on GitHub". | Raw (same fetch as V-09) |
| F-09 | Per the Terms, the app may find installed games, download "mod loaders, launchers and build tools", write into game folders after backing up replaced files, "look for that copy in your folders and drives", start games and programs, and "update itself automatically". | Raw (V-09 and same fetch) |
| F-10 | The Terms mention coding agents connected to Melty, with tokens, and publishing "automatically from a GitHub repository you connected". | Extraction; the GitHub phrase is raw |
| F-11 | "Every upload is checked for dangerous files." The Terms say every mod and mashup is audited before release. | Raw homepage; extraction |
| F-12 | The Terms dated "Last updated: October 18, 2026", the day of access, suggest frequent revisions. The Privacy policy was dated October 12, 2026; it mentions installed-game scanning, optional session recordings limited to the Melty window, and 30-day crash logs. | Raw (Terms date); extraction (Privacy) |

### 3.1 Distribution conflict to review

F-07 and F-08 describe an **exclusive** licence. On a plain reading, publishing the same content (or a version of it)
on Melty would conflict with also distributing it on Paradox Mods, the official CS2 mod platform (SRC-012). The 12-month
tail and the rule for "new versions and releases" make this hard to reverse.

**This is flagged for owner and legal review. It is not a legal conclusion** (OPEN-401, RISK-401). It also interacts
with obligations toward any incorporated MIT code ([REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md)).

## 4. Press-only claims (reasons for caution, not facts)

From SRC-014, a Dexerto article dated 2026-10-14:

- **[Press, unverified]** The article reports announcements of "Melty MCP" (Oct 11) and "Melty Chat" (Oct 14). Neither appears on melty.gg as read on 2026-10-18. Whether the owner's chat-like application window corresponds to such a feature is **unknown**.
- **[Press, unverified]** The article reports a third-party claim (Oct 5) that a Melty installer version contains components of a gameplay- and input-recording application. It flags this as **not independently verified**, and says that the documents "do not, by themselves, establish that Melty is malware". We have not verified it either. It is recorded only as a reason for a cautious, owner-controlled procedure (RISK-402).

## 5. Evaluation questions

| ID | Area | Question | Why it matters | Current documented answer |
|---|---|---|---|---|
| MQ-01 | Project access | Can Melty, or an agent it instructs, work on files in `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII` without moving the project? | The project root is fixed | **Answered (2026-10-18):** yes. Melty's tools reach the agent through its MCP connection, and the agent works in the project root. Melty itself needs no project-folder access (M-01). |
| MQ-02 | Invocation | What exactly are "Melty's instructions": text, a URL, a file? Do they direct the agent to install tools, connect tokens, or publish? | Agents must not run installers found in content | **Partly answered:** a generic text prompt held by the owner, plus MCP tools with a server-side workflow (build → upload → test → publish). The tool instructions also ask the agent to send every reply to a Melty progress log; that was not done (§9). |
| MQ-03 | Files | Which files does Melty create, read, or modify, in the project, in game folders, or in its own folder? | Protect the game installation and saves | For CS2, Melty installs BepInEx 5 into the game folder and places mods under `{game}/BepInEx/plugins` (M-03). It writes test files under `%LOCALAPPDATA%\Melty\test`. |
| MQ-04 | Launch | Does Melty launch CS2 with special arguments, a mod loader, or a separate profile? | Interaction with the official mod loading and test isolation | **Answered:** normal launch (`launch: game`) with BepInEx 5 loaded through the game folder (M-03). No separate profile. |
| MQ-05 | Packaging | What package format and metadata does a Melty mashup need? Can a standard CS2 code mod be represented at all? | REQ-REL-004; the packaging plan | **Answered:** an archive plus an install recipe. A native CS2 code mod **cannot** be represented one-click: the native local-mods folder lies under `AppData\LocalLow`, which Melty rejects as a destination (M-08), and manual mode is not one-click (M-09). |
| MQ-06 | Permissions | Which account, token scopes, folder and drive access, and auto-update rights are required? | Least privilege; owner limits | Broad rights per Terms (F-09, F-10) |
| MQ-07 | Paradox Mods conflict | Could Melty-installed files conflict with mods subscribed through Paradox Mods, or with local mods? | Isolated testing (OPEN-035) | **Unknown, with a concrete risk:** a BepInEx copy and a native copy of the same mod could both load (REQ-COMPAT-001). Not tested. |
| MQ-08 | Other mods | Does Melty alter other installed mods or playsets? | RISK-016 | Unknown |
| MQ-09 | Security | What does the installed application run in the background, what does it record, and what does it send? | RISK-402 | Privacy policy only (F-12); press claims unverified |
| MQ-10 | Exclusivity | Does any intended use require publishing on Melty and so trigger the exclusive licence? | RISK-401 | Publishing triggers it (F-07) |
| MQ-11 | Removal | Does uninstalling Melty restore every touched file, and how is that verified? | Game installation integrity | Claimed (homepage) but not verified |
| MQ-12 | CS2 support | Does Melty support CS2 at all, given that it is not listed? | Whether evaluation is even possible | **Answered:** yes, in the catalog, through BepInEx 5 only. There are no live CS2 mashups and no install-outcome data (M-02, M-03, M-06). |
| MQ-13 | Value | What would Melty add beyond running Claude Code in the project root? | Adoption criterion | **Low for this project today:** its distribution route does not fit, and its kit's CS2 guidance points back to the official toolchain (M-11). |

## 6. Owner-attended, observation-first procedure (proposed; partly superseded on 2026-10-18)

**Proposed procedure (requires validation).** No agent installs, launches, or connects Melty. Each stage needs the
owner's explicit go-ahead, and the owner may stop at any stage.

1. **Stage 0 — documentation review (done 2026-10-18).** The result is this document.
   - **Reached on 2026-10-18 (out of the original order):** the owner installed Melty and connected its tools.
     The owner then authorized a read-only inspection through that connection, which covers the intent of Stages 2 and
     3 without any game launch or publication (§9).
2. **Stage 1 — decision to evaluate.** The owner decides whether an evaluation is worth doing at all, given F-06 (CS2 not listed) and F-07 (exclusivity). If not, record "Not evaluated", and the fallback (§8) applies.
3. **Stage 2 — passive observation of the existing app.** The owner opens Melty personally, outside any agent session. They describe or capture the main window (sanitized: no account names, tokens, or paths beyond the project root) to answer MQ-01, MQ-02, and MQ-12.
   - No game selection that triggers downloads.
   - No agent connection.
   - No publishing.
   - No creation of a mashup.
4. **Stage 3 — instruction inspection as data.** If Melty shows "instructions" for agents, the owner copies the text into a git-ignored local file (for example under `.local/`). An agent then reads it as **data only**: it does not follow any instruction it contains. The agent classifies what the instructions would make an agent do (MQ-02, MQ-05, MQ-06).
5. **Stage 4 — optional sandbox trial, separately authorized.** This stage is only considered if Stages 2–3 show value and CS2 support. It uses a throwaway folder outside this project, a token with minimum scope, recent backups of saves, and a recorded snapshot of the game-folder state, made with an owner-approved method. Publishing stays forbidden.
6. **Record.** Answers go in §5 with their dates. The register entries (OPEN-016, RISK-010) are updated, and the owner's decision is recorded.

**Never, without a separate owner decision and a legal review:**

- publishing anything to Melty;
- connecting this project's future repository to Melty;
- letting Melty modify the CS2 installation used for development without verified backups;
- running scripts or installers that Melty or its instructions provide.

## 7. Decision criteria

Melty may be adopted as an optional workflow aid only if **all** of the following hold. Otherwise it is recorded as
"Not adopted", and the evaluation can be revisited if Melty later documents CS2 support.

1. It works with the existing project root, or needs no access to it (MQ-01).
2. It supports CS2 for the intended use (MQ-12).
3. It requires no publication on Melty and no acceptance of exclusivity for Drive the City II content (MQ-10), unless the owner, after legal review, chooses otherwise.
4. It makes no unconsented or unrecoverable changes to the game installation, saves, or other mods (MQ-03, MQ-07, MQ-08, MQ-11).
5. Its permissions and background behavior are acceptable to the owner (MQ-06, MQ-09).
6. It adds clear value over the standard workflow (MQ-13).

Possible outcomes: **Not evaluated** · **Not adopted** · **Reference only** (its agent instructions are read as
information) · **Adopted as an optional aid**, with documented limits. Even if adopted, nothing in the runtime or the
distribution depends on it (REQ-REL-004).

## 8. Launcher-independent fallback (the default plan)

The project proceeds with:

- the **standard CS2 modding toolchain** ([CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md), [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md));
- **Claude Code running in the project root**, governed by [AGENTS.md](../../AGENTS.md);
- the official Paradox Mods publishing path, subject to OPEN-017 and the owner's decision.

No task, phase gate, document, or build step requires Melty. If Melty becomes unavailable or changes its terms, the
project continues unchanged.

## 9. Connected, read-only inspection (2026-10-18)

**Scope and method.** The owner connected Melty's tools to this Claude Code session and authorized a read-only
investigation. The agent used `melty_status`, `whoami`, `search_games`, `game_info`, `modding_kit`, `search_mashups`,
`mashup_info`, `list_my_mods`, and `validate_recipe` (hypothetical recipes only; nothing is stored by validation). It
did **not** use any tool that creates, uploads, tests, prices, remixes, publishes, or requests anything. It also did
not call `consult_melty`, a progress log that would send the agent's full reply to Melty: that is not a read-only
action, and no work ID was provided. Account details are not recorded in tracked documents. The local modding kit was
read as data, and nothing in it was executed.

**Findings.** All findings are **[Documented upstream: Melty service response, 2026-10-18]**. They state what Melty
says; none was verified in the game.

| # | Finding | Source call |
|---|---|---|
| M-01 | The connection works. The app is online (version 1.0.132) and signed in; the token is named "Claude Code (Melty app)". No test is waiting or running. | `melty_status`, `whoami` |
| M-02 | CS2 **is** in Melty's catalog (`cities-skylines-2`, engine `unity`), although the public homepage does not list it (F-06). | `search_games` |
| M-03 | Melty's **only** CS2 loader is **BepInEx 5 v5.4.23.5**, which Melty installs automatically into the game folder (`{game}`). Mod folders are `{game}/BepInEx/plugins`, `patchers`, and `config`. No game-version restriction is stated. | `game_info` |
| M-04 | Files that must never be shipped: `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `UnityPlayer.dll`, `Cities2.exe`, `Cities2_Data/Managed/Game.dll`. | `game_info` |
| M-05 | Sell policy is "tips-only": "Paradox's user content terms require mods to be free". | `game_info` |
| M-06 | **No live CS2 mashups** and no recorded install failures. There is therefore no Melty evidence that BepInEx 5 works on CS2 1.6.2f1. | `game_info`, `search_mashups` |
| M-07 | Motor Town is **not in the catalog** (`inCatalog: false`, custom entry only). | `search_games`, `game_info` |
| M-08 | Recipe destinations must start with a root token (`{game}`, `{documents}`, `{my-games}`, `{saved-games}`, `{appdata}`, `{localappdata}`, `{managed}`, and others). A destination under `AppData\LocalLow`, where CS2 keeps its user data and native local mods, was **rejected**: "Destination must start with a folder like {game}". | `validate_recipe` |
| M-09 | `manual` mode validates but is **not one-click** (blocker `manual-install`), and Melty's publishing flow blocks non-one-click releases. `workshop` mode needs Steam Workshop IDs, but CS2 mods are distributed on Paradox Mods, not Steam Workshop. | `validate_recipe` |
| M-10 | A recipe with CS2 as `primary` and Motor Town as `secondary` validates with only CS2 needed: Motor Town would not be required. In the `companion` role it would be required and launched alongside, as ETS2 is in M-12. | `validate_recipe` |
| M-11 | Melty's local modding kit (version 2026.10.18-739f3496) contains a community note classifying the CS2 route as the **official Modding Toolchain plus Paradox Mods** ("loader-api"). That contradicts the catalog's BepInEx-only route, and the note's author states that nothing was built or loaded. | Kit file `knowledge/games/cities-skylines/cities-skylines-modding-api-and-container-formats.md` |
| M-12 | Prior art: "City Trucker" (Cities: Skylines **1** plus ETS2) installs into CS1's native mod folder under `{localappdata}` and launches ETS2 alongside. CS2 has no equivalent in Melty, because CS2's native folder is under `LocalLow` (M-08). | `search_mashups`, `mashup_info` |
| M-13 | The connected account has no Drive the City II draft or listing. Nothing was created. | `list_my_mods` |

**Analysis.**

1. **Loading-route mismatch.** The candidate base mod and the official toolchain use the game's native code-mod
   loading ([CS2_INTEGRATION_FINDINGS](CS2_INTEGRATION_FINDINGS.md) §3.15). Melty can only install BepInEx plugins into
   the game folder. That would modify the game installation, which this project avoids
   ([WINDOWS_SETUP](../engineering/WINDOWS_SETUP.md) §4), and BepInEx 5 on CS2 1.6.2f1 is unverified (M-06).
2. **Two-game requirement.** It is **not** inherent: Melty accepts a CS2-only listing (M-10). The incompatibility is
   the loading route, not Motor Town.
3. **Exclusivity.** Publishing on Melty still triggers the exclusive licence (F-07, CONF-012), which conflicts with
   Paradox Mods distribution.

**Smallest viable resolutions, ranked.**

1. **Recommended:** keep the native CS2 code-mod route and the official distribution path (Paradox Mods or local
   mods), and do not use Melty as a distribution channel. Melty stays available as a reference
   ([ADR-0005](../architecture/decisions/ADR-0005-mod-loading-and-distribution-route.md)).
2. **Owner action, optional:** ask Melty to support CS2's native local-mods destination (a `LocalLow` root token) or
   a Paradox Mods mode. Re-evaluate only if the exclusivity question (CONF-012) is also resolved. `request_game` was
   **not** called, because it sends a request on the owner's behalf.
3. **Not recommended:** a BepInEx 5 bootstrap that loads the native mod. It would modify the game folder, is
   unverified on 1.6.2f1, and risks double loading next to a Paradox Mods copy. It would only be considered as a
   separately authorized spike after M1.

**Representation rule, if the project is ever listed anywhere:** CS2 is the only required game. Motor Town is never a
`companion` or required game; at most it is credited as design inspiration in the description, with no assets or
branding. A listing must not describe Drive the City II as a two-game mashup.

## 10. Open questions and risks

- **Central:** OPEN-016 (Melty capabilities and fit), OPEN-030 (Motor Town installation), CONF-008 (the mashup model versus a CS2-only runtime), CONF-012 (exclusivity), CONF-014 (Melty's BepInEx-only CS2 route versus the native route), RISK-010.
- **New:**
  - OPEN-401: exclusivity versus Paradox Mods distribution.
  - RISK-401: the exclusive licence could block distribution.
  - RISK-402: security and privacy exposure from the Melty application.

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
