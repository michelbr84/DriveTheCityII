# Third-Party Notices

| Field | Value |
|---|---|
| ID | DOC-NOTICES |
| Document status | Draft |
| Scope | Third-party code and assets incorporated in Drive the City II |
| Responsible role | Coordinator (maintainer); the Reviewer verifies each notice at incorporation; QA verifies it again at release |
| Related documents | [REUSE_AND_LICENSE_AUDIT](docs/legal/REUSE_AND_LICENSE_AUDIT.md), [ASSET_PROVENANCE](docs/legal/ASSET_PROVENANCE.md), [DEPENDENCIES](docs/engineering/DEPENDENCIES.md), [SOURCES](docs/research/SOURCES.md) |
| Last updated | 2026-10-18 |

## Incorporated third-party material

**None.** As of 2026-10-18, Drive the City II contains no third-party source code, libraries, or assets. No build
or package exists.

## Candidates under evaluation (NOT incorporated)

The following component is being evaluated. It is **not** part of this project, and no notice obligation has been
triggered:

| Component | Upstream | License as found | Status |
|---|---|---|---|
| Beta Test Driving Mod | [github.com/northernst11-bot/BetaTestDrivingMod](https://github.com/northernst11-bot/BetaTestDrivingMod), candidate audit revision `eefd6bb4fe2dcc479df837a1678f3529ac6e0f76` | MIT License, "Copyright (c) 2026 stoot" (read 2026-10-18) | Candidate. Source and license audit pending (TASK-0002); open provenance questions are listed in the [audit](docs/legal/REUSE_AND_LICENSE_AUDIT.md). |

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
