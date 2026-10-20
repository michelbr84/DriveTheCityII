# RUN-0006 — Release 0.1.0: archive install test, GitHub release, Paradox Mods publication

| Field | Value |
|---|---|
| Run ID | RUN-0006 |
| Phases | PHASE-13 (packaging and publication) |
| Date | 2026-10-20 |
| Executed by | Claude Code agent (owner request of 2026-10-20: GitHub Releases first, then Paradox Mods; no Melty); no human reviewer yet |
| Input | **Simulated**: test-bridge commands and the TEST AUTOPILOT (`career drive`) in a disposable lab city; career rules unchanged. No real keys. |
| Build | Commit `59399fd` (tag `v0.1.0`), package `DriveTheCityII-0.1.0-59399fd-20261020T153959Z`; 172 unit tests passed; 0 errors, 10 compiler warnings |
| Session | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261020-114051`; campaign report `test\campaigns\career-persist-20261020-114709.md` |

## 1. Release archive

`tools\release.ps1 -Version 0.1.0` checked the package against its manifest, refused nothing (clean commit), and wrote
`DriveTheCityII-0.1.0.zip` (302,818 bytes, SHA-256 `88014be9f57a14655028b03d72d7ee6e4b7b335afd5f9cad54f852ed5146f4e8`).
Entries: `DriveTheCityII/DriveTheCityII.dll`, `.pdb`, `.mjs`, `LICENSE.txt`, `README.txt`. Each mod file in the archive
was read back and matched the package byte for byte. No game files.

## 2. Install exactly as a player would

| Step | Result |
|---|---|
| Remove the managed install (`deploy.ps1 -Uninstall`) | **Passed**: only the files in the ownership record were removed; no other mod folder exists |
| Extract the zip into `<user data>\Mods\` (`Expand-Archive`) | **Passed**: `Mods\DriveTheCityII\` with the five files |
| Start a test session; main menu | **Passed**: log line `bridge online: mod 0.1.0+59399fd… build 59399fd-20261020T153959Z, game 1.6.2f1` |
| Load lab save `DTC2-LAB-CAREERS-H`; character mode; call and board the basic rental | **Passed** (messages in Portuguese, the configured mod language) |
| Career persistence campaign (`tools\campaign-career-persist.ps1`) | **Passed 11/11**: save A; taxi fare accepted; passenger aboard; save B while driving; reload B at the same stage with no mod entity in the save; finish the fare (one settlement); save and reload C (no second payout); reload A (older balance 732185 / 8 entries restored exactly) |
| Extra files (`LICENSE.txt`, `README.txt`) in the mod folder | No effect on loading (no errors) |

Observed and not a defect of the release: after the campaign, the game window lost focus (other windows were in use);
the mod then counts the game as paused and refuses character mode (by design, `!Application.isFocused`). Screenshots
for the listings were therefore taken from earlier test sessions (section 4).

Afterwards the hand-extracted copy was removed and the same package reinstalled with `deploy.ps1 -Install`
(`-Status`: all three files OK).

## 3. GitHub release

| Step | Result |
|---|---|
| Annotated tag `v0.1.0` on `59399fd`, pushed | **Passed** (`refs/tags/v0.1.0^{}` = `59399fd`) |
| Release created | The owner clicked Publish on the empty form opened in Chrome before it was filled; the agent then completed it through the edit page: title "Drive the City II 0.1.0", notes in English and Portuguese, archive attached, marked Latest |
| Public API check (no login) | **Passed**: one asset `DriveTheCityII-0.1.0.zip`, 302,818 bytes, GitHub digest `sha256:88014be9…46f4e8` (same as ours); `releases/latest` → `v0.1.0` |
| Anonymous download and comparison | **Passed**: downloaded archive SHA-256 identical; extracted `.dll`, `.pdb`, `.mjs` identical to the package |

Release page: <https://github.com/michelbr84/DriveTheCityII/releases/tag/v0.1.0>.

## 4. Paradox Mods

| Step | Result |
|---|---|
| Publisher | The game's own `ModPublisher` (`Cities2_Data\Content\Game\.ModdingToolchain\ModPublisher`, .NET 6) run on the private .NET 10 with `DOTNET_ROLL_FORWARD=Major` through `tools\publish-paradox.ps1` |
| Listing media | Thumbnail (1024 × 1024) and 8 screenshots (1920 × 1200, JPEG, under 2 MB each) made from real captures of earlier lab sessions (2026-10-19 and 2026-10-20) |
| Login | **Automatic login with the account the game is signed in to**; no email or password was passed or seen |
| `Publish` with `AccessLevel` Private | **Passed**: uploaded exactly `DriveTheCityII.dll`, `.mjs`, `.pdb` from the installed package (hashes checked against the package by the script first); "Mod published with Id=164157" |
| `Update` with `AccessLevel` Public | **Passed**: "Mod metadata Updated" |
| Public page, logged out | **Passed**: <https://mods.paradoxplaza.com/mods/164157/Windows> shows title, author, thumbnail, screenshots, the formatted description (including the Portuguese section), mod version 0.1.0, suggested game version 1.6.\*, size 297.82 KB, tag Code Mod, GitHub link |
| Install from Paradox Mods (subscribe in game, local copy removed) | **Not run.** Subscribing needs the owner's Paradox login in the game or on the website. The uploaded files are the ones tested in section 2, and the mod finds its own folder through the game's `TryGetExecutableAsset`, so the different install folder (`.cache\Mods\pdx_mods\164157_…`) is not expected to matter; this is unverified until a subscribed copy is loaded. |

The website's cookie banner was declined (no optional cookies).

## 5. Limits of this run

- Simulated input only; M1 with real keys was not repeated on this package.
- The subscribed Paradox Mods copy has not been loaded in game (section 4).
- Having both the local copy (`Mods\DriveTheCityII`) and a subscribed copy active at the same time is untested and
  should be avoided.
