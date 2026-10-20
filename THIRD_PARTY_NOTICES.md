# Third-Party Notices

| Field | Value |
|---|---|
| ID | DOC-NOTICES |
| Document status | Draft |
| Scope | Third-party code and assets incorporated in Drive the City II |
| Responsible role | Coordinator (maintainer); the Reviewer verifies each notice at incorporation; QA verifies it again at release |
| Related documents | [REUSE_AND_LICENSE_AUDIT](docs/legal/REUSE_AND_LICENSE_AUDIT.md), [ASSET_PROVENANCE](docs/legal/ASSET_PROVENANCE.md), [DEPENDENCIES](docs/engineering/DEPENDENCIES.md), [SOURCES](docs/research/SOURCES.md) |
| Last updated | 2026-10-20 |

Drive the City II itself is licensed under the [MIT License](LICENSE).

## Incorporated third-party material

**None.** As of 2026-10-20, the mod package (`DriveTheCityII.dll`, `DriveTheCityII.pdb`, `DriveTheCityII.mjs`) contains
only this project's own code. It is compiled against the game's managed assemblies, which are read from the local game
installation at build time and are never committed or redistributed.

## Development and test tools (not distributed)

These NuGet packages are used only by the unit-test project (`tests/DriveTheCityII.Core.Tests`). They are restored at
build time, are not committed, and are not part of the mod package. Licenses as declared in each package's metadata
(read 2026-10-20):

| Package | Version | License |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |

## Game content in the documentation

Screenshots of Cities: Skylines II in the documentation (for example
`docs/quality/evidence/RUN-0001/images/upstream-possessed-car.jpg`) show game content that belongs to its owners. They
are included only as test evidence and are not covered by this project's license.

## Reference-only material (NOT incorporated)

| Component | Upstream | License as found | Status |
|---|---|---|---|
| Beta Test Driving Mod | [github.com/northernst11-bot/BetaTestDrivingMod](https://github.com/northernst11-bot/BetaTestDrivingMod), audit revision `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` | MIT License, "Copyright (c) 2026 stoot" (read 2026-10-18) | Reference only: audited, built, and run outside this project as a baseline ([RUN-0001](docs/quality/evidence/RUN-0001/REPORT.md)). No upstream code is incorporated. Drive the City II only detects its assembly name so that it stays inactive while both are loaded. Open provenance questions are in the [audit](docs/legal/REUSE_AND_LICENSE_AUDIT.md). |

Reference-only material (documentation, community tooling that is not installed, and games used as design references)
is listed in [SOURCES](docs/research/SOURCES.md) and is not incorporated.

## Rule for adding notices

A notice is added **in the same change that incorporates the material**, never before and never later. Each notice
contains:

1. the component name and upstream location;
2. the exact pinned revision (full commit SHA) or package version;
3. the copyright line(s) exactly as stated upstream;
4. the **full license text**;
5. any upstream NOTICE content that the license or the upstream project asks to be preserved;
6. the files or assets in this project that contain the material.

Prerequisites: the component's row in the [REUSE_AND_LICENSE_AUDIT](docs/legal/REUSE_AND_LICENSE_AUDIT.md) is
complete, the project owner has approved the incorporation, and the project license has been decided.

## Trademarks

The following product and company names appear in this project only to identify the products concerned:

- Cities: Skylines II, Paradox Interactive, and Colossal Order;
- Motor Town: Behind The Wheel and P3 Games;
- Melty and Stella Montis;
- SimCity.

Any trademarks in these names belong to their respective owners. Drive the City II is not affiliated with, endorsed
by, or sponsored by any of them. No third-party logos or branding are used.
