# Drive the City II

An unofficial **Cities: Skylines II** code mod for Windows: leave city building, walk around the city you built, take a
car, drive it through your own traffic, and work as a taxi, delivery, freight, or bus driver. Earn money and
experience, rent or buy vehicles, found a small transport company, and go back to building at any time.

| | |
|---|---|
| Version | 0.1.0 (development build) |
| Game | Cities: Skylines II **1.6.2f1** (Steam, Windows 11). Other game versions are untested. |
| Download | A ready-to-install archive is attached to each [GitHub release](https://github.com/michelbr84/DriveTheCityII/releases/latest). The mod is not on any mod platform yet; Paradox Mods is planned. |
| Status | Playable in disposable test cities. Validation is in progress: see [PROJECT_STATUS](docs/PROJECT_STATUS.md) and [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md). |
| License | [MIT](LICENSE) |

## What works

Verified in game on the development machine, mostly with simulated input from the project's test harness. The core
loop was also run with real keyboard input. The per-feature table is in [MORNING_REPORT](MORNING_REPORT.md).

- **Character mode:** walk and run on the sidewalks of your own city, with third- and first-person cameras.
- **Driving:** enter a rental car, one of your own vehicles, or (in sandbox mode) a city car stopped in traffic. City
  traffic, buildings, and poles are solid; traffic stops behind you and steers around a vehicle you leave parked.
- **Careers:** taxi fares, urban deliveries, rigid-truck freight, and bus shifts on the city's own bus lines, with pay,
  XP, reputation, and unlocks.
- **Garage and life:** rent, buy, and sell vehicles; fuel and wear; roadside help and towing; a home and a garage;
  timed checkpoint and parking challenges; a simulated transport company.
- **Saves:** career data is stored inside each city save. Your character and vehicles are never written into the city,
  and a save still loads with the mod removed.
- **Languages:** English and Português (Brasil).

Known limitations include: parked city cars cannot be entered yet, long buses clip corner props in narrow junctions,
and gamepad bindings exist but are untested. See [KNOWN_ISSUES](docs/quality/KNOWN_ISSUES.md).

## Install a release

1. Close the game and back up your saves.
2. Download `DriveTheCityII-<version>.zip` from the [latest release](https://github.com/michelbr84/DriveTheCityII/releases/latest).
3. Extract it into `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\`, so that you get
   `Mods\DriveTheCityII\DriveTheCityII.dll`. Create the `Mods` folder if it does not exist.
4. Start the game from Steam, load a city, and press **F8**.

To uninstall, close the game and delete the `Mods\DriveTheCityII` folder. Cities keep loading without the mod. Controls
and a first walkthrough are in [START_HERE](START_HERE.md).

## Build from source

Requirements: Windows, Cities: Skylines II installed through Steam, and the .NET SDK 10. The scripts use a private,
user-scoped copy of the SDK and change nothing system-wide; the one-time setup is in
[BUILD_AND_RUN](docs/engineering/BUILD_AND_RUN.md). The build compiles against the game's own assemblies, read from your
game installation; no game files are included in this repository or in the package.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\build.ps1            # unit tests, build, local package
powershell -NoProfile -ExecutionPolicy Bypass -File tools\deploy.ps1 -Install  # copy it into the game's local Mods folder
```

Build output goes to `%LOCALAPPDATA%\DriveTheCityII\`, and the mod is installed into
`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DriveTheCityII\`. `tools\deploy.ps1 -Status`,
`-Rollback`, and `-Uninstall` check, undo, and remove it. The deploy script refuses to run while the game is open and
never touches other mods or saves. `tools\release.ps1 -Version <version>` turns the built package into the release
archive.

Then start the game from Steam, load a city, and press **F8**. Controls and a first walkthrough are in
[START_HERE](START_HERE.md). Back up your saves before trying any development build. Do not run the Beta Test Driving
Mod at the same time: Drive the City II detects it and stays inactive.

## Repository layout

```text
src/DriveTheCityII.Core/        engine-free rules: careers, economy, routing, motion, persistence (no game references)
src/DriveTheCityII.Mod/         the CS2 mod: player and vehicle control, city bridge, career systems, UI, test bridge
tests/DriveTheCityII.Core.Tests/ xUnit tests for Core (run without the game)
tools/                          build, deploy, test-session, and in-game test campaign scripts (PowerShell)
docs/                           product, architecture, gameplay, quality and evidence, planning, agent rules
```

Start with [docs/INDEX.md](docs/INDEX.md) for the full documentation map. Useful entry points:
[ARCHITECTURE](docs/architecture/ARCHITECTURE.md), [BUILD_AND_RUN](docs/engineering/BUILD_AND_RUN.md),
[TEST_STRATEGY](docs/quality/TEST_STRATEGY.md), the in-game evidence runs in [evidence](docs/quality/evidence/README.md),
and the [CHANGELOG](CHANGELOG.md).

## How this project is developed

Most of the code and documentation were written by AI coding agents (Claude Code) working in the owner's sessions,
under the rules in [AGENTS.md](AGENTS.md) and [CLAUDE.md](CLAUDE.md). Those rules require that every claim of
"built", "tested", or "verified" points to a recorded evidence run, and that anything not run is labelled "Not run".
The original planning conversation (Portuguese) is kept unchanged as a historical input in
[docs/archive/chat.md](docs/archive/chat.md).

Contributions, bug reports, and security reports: see [CONTRIBUTING](CONTRIBUTING.md) and [SECURITY](SECURITY.md).

## License and affiliation

- The code and documentation are released under the [MIT License](LICENSE).
- **Third-party material:** the mod contains no third-party code or assets; test-only tools and their licenses are
  listed in [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md). Screenshots of Cities: Skylines II in the documentation show
  game content that belongs to its owners and is not covered by the MIT License.
- This is an unofficial fan project. It is not affiliated with or endorsed by Paradox Interactive, Colossal Order,
  P3 Games, the authors of the Beta Test Driving Mod, or Stella Montis Inc. (Melty). All trademarks belong to their
  owners.
