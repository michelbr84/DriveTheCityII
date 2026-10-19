# Drive the City II (working title)

> **Status: PRE-DEVELOPMENT.** Documentation baseline only. There is **no code, no build, and nothing to install or
> play**. No feature described in this repository has been implemented or tested. PHASE-00 is in progress: the read-only
> environment and source audits are done and in review, and the runtime baseline has not started. See [PROJECT_STATUS](docs/PROJECT_STATUS.md).

## What it is meant to become

Drive the City II is a planned **Cities: Skylines II (CS2) code mod for Windows** that lets you live and work inside the
city you built. You build your city as usual, then switch to a visible character, walk to a vehicle, drive your own
road network, and take on transportation careers: taxi, urban delivery, rigid-truck freight, and bus. You earn money
and experience, rent or buy vehicles, and grow a small transport company. You can return to city building at any time,
and saving must never corrupt the city or duplicate rewards.

- **CS2** is the host game and the only runtime.
- The **Beta Test Driving Mod** is a *candidate* foundation for vehicle control, pending license and baseline audits.
- **Motor Town: Behind The Wheel** is a design reference only. **Streets of SimCity** is inspiration only.
- **Melty** is an optional tool still under evaluation. The project does not depend on it.

Full scope: [VISION](docs/product/VISION.md) · [PRD](docs/product/PRD.md) · [ROADMAP](ROADMAP.md).

## Workspace

- Authoritative project root (Windows): `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII`
- The folder is synced by OneDrive and is not yet a Git repository (see OPEN-028 and OPEN-029 in the
  [register](docs/planning/RISKS_AND_OPEN_QUESTIONS.md)).
- `chat.md` is the original Portuguese planning conversation. It is kept unchanged as a historical input and is not
  active documentation.

## Where to start

| You are… | Read |
|---|---|
| New to the project | [VISION](docs/product/VISION.md) → [PRD](docs/product/PRD.md) → [ROADMAP](ROADMAP.md) → [PROJECT_STATUS](docs/PROJECT_STATUS.md) |
| An AI agent or contributor | [AGENTS.md](AGENTS.md) (Claude Code: [CLAUDE.md](CLAUDE.md)) → [DOCUMENTATION_RULES](docs/DOCUMENTATION_RULES.md) → your task file |
| Looking for a specific document | [docs/INDEX.md](docs/INDEX.md) |
| Picking up the next task | [PROJECT_STATUS](docs/PROJECT_STATUS.md) §6 → [TASK-0003](docs/planning/tasks/TASK-0003-isolated-baseline-validation.md) (needs owner authorization) |

## Layout

```text
README.md, ROADMAP.md, AGENTS.md, CLAUDE.md, CHANGELOG.md, CONTRIBUTING.md, SECURITY.md, THIRD_PARTY_NOTICES.md
docs/
  INDEX.md, PROJECT_STATUS.md, DOCUMENTATION_RULES.md
  product/       vision, requirements (PRD), game design, player journeys, expansions
  research/      sources, feasibility, base-mod audit, Motor Town reference, CS2 findings, Melty evaluation
  engineering/   Windows setup, toolchain, build/run, coding standards, dependencies, upstream, debugging
  architecture/  architecture, module contracts, control, entities, time, world contact, city, save, recovery, performance, ADRs
  gameplay/      player, driving, transitions, missions, economy, garage, home, company, challenges, careers/
  experience/    UI/HUD, controls and accessibility, art, audio, localization, onboarding
  planning/      backlog, risks and open questions, traceability, phases/, tasks/
  quality/       test strategy, checklists, scenarios, save safety, benchmarks, compatibility, known issues, evidence/
  agents/        reading guide, roles, workflow, definitions of ready/done, tools and permissions, handoff
  release/       packaging, install/upgrade/uninstall, compatibility policy, maintenance (pre-release drafts)
  legal/         reuse and license audit, asset provenance
  user/          quick start, troubleshooting (pre-release drafts)
  templates/     document, phase, task, ADR, test report, handoff
  archive/       archival policy (nothing archived yet)
```

The `src/`, `tests/`, `assets/`, `tools/`, `artifacts/`, and `.local/` folders will be created only when authorized
work needs them.

## License and affiliation

- **Project license:** not yet chosen. It is an owner decision after the reuse audit (OPEN-027), and the project is
  not assumed to be MIT.
- **Third-party material:** none has been incorporated ([THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md)).
- **Affiliation:** this is an unofficial fan project, not affiliated with or endorsed by Paradox Interactive, Colossal
  Order, P3 Games, the authors of the Beta Test Driving Mod, or Stella Montis Inc. (Melty). All trademarks belong to
  their owners.
