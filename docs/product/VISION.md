# Vision — Drive the City II

| Field | Value |
|---|---|
| ID | DOC-VISION |
| Document status | Draft |
| Release scope | v1.0 and long-term direction |
| Phases | Guides all phases (PHASE-00 – PHASE-13) |
| Responsible role | Coordinator (maintainer); project owner approves changes to the vision statement and pillars |
| Related requirements | REQ-PLAYER-001, REQ-CTRL-004, REQ-CITY-001, REQ-CITY-003, REQ-COMPANY-002, REQ-SAVE-001, REQ-ECON-004, REQ-REL-004 |
| Related documents | [PRD](PRD.md), [GAME_DESIGN](GAME_DESIGN.md), [PLAYER_JOURNEYS](PLAYER_JOURNEYS.md), [EXPANSIONS](EXPANSIONS.md), [ROADMAP](../../ROADMAP.md), [MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md) |
| Last updated | 2026-10-18 |

> **Status:** pre-development. Nothing described here exists yet. This document states what we are aiming for and
> why. The [PRD](PRD.md) holds the binding requirements, and [PROJECT_STATUS](../PROJECT_STATUS.md) holds the real state.

## 1. Vision statement

**Live and work in the city you built.**

Drive the City II (working title) is a Cities: Skylines II (CS2) code mod for Windows. It lets a city builder step
out of the planner's view and into the streets as a visible character. The player walks to a vehicle, drives on the
roads they laid out, and earns a living moving people and goods through their own city. Over time the player buys
vehicles and grows a small transport business. At any moment they can return to building, and the city they come
back to is the same city they left.

CS2 is the world. The mod is the playable experience inside it. **[Owner requirement]**

## 2. Target player

| Player | Why they come | What they need from us |
|---|---|---|
| **Primary:** a CS2 player who has built a city they care about | To see and use their city at street level and give it a second purpose | Safety for their saves, a quick start, and the freedom to go back to building at any time |
| **Secondary:** a driving-career fan (taxi, delivery, bus, freight) | Jobs, vehicles, progression, and a business inside a world that is unique to each player | Consistent driving, clear jobs, and a fair economy |
| **Supporting:** a returning player | To continue a career after days or weeks away | Career data that always matches the save they load, and clear reminders of where they were |

Assumptions to validate: players use Windows, keyboard and mouse or a gamepad, and a supported CS2 configuration
([COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md)). We do **not** target players who want simulator-grade
vehicle physics, combat, or multiplayer in v1.0 ([EXPANSIONS](EXPANSIONS.md)).

## 3. Pillars

Every feature must serve at least one pillar and must not break any of them.

| Pillar | What it means | What it rules out | Anchoring requirements |
|---|---|---|---|
| **1. Your city is the level** | Jobs, routes, challenges, and difficulty come from the player's own roads, buildings, and transit lines. Building better infrastructure changes the work. | Hand-made levels that ignore the city, and work that teleports between unrelated places | REQ-CHAL-003, REQ-CAREER-002, REQ-CAREER-003, REQ-BUS-001, REQ-CHAL-001 |
| **2. Honest systems** | What the player sees is what is simulated. Career passengers are called career passengers, the company is labeled a managerial simulation, placeholders are labeled, and every coin is in the ledger. | Presenting abstract fleets as physical traffic, or presenting mod passengers as native citizens. Hidden reward rules. | REQ-COMPANY-002, REQ-TAXI-002, REQ-ART-002, REQ-ECON-002, REQ-CITY-003 |
| **3. Safe to play** | The city and its saves are protected. Any failure state has a way out. The mod can be stopped and left cleanly. | Writing native economic state in v1.0, permanent softlocks, and claims of safe uninstallation before testing | REQ-CITY-001, REQ-CITY-004, REQ-SAVE-001, REQ-SAVE-004, REQ-RECOV-001, REQ-ECON-004, REQ-REL-002 |
| **4. Work that feels earned** | Driving is semi-realistic and consistent. Jobs have clear goals, fair pay, and visible consequences. Progress opens real choices. | Random or unexplained payouts, and grinding without choices | REQ-DRIVE-002, REQ-DRIVE-003, REQ-CAREER-004, REQ-ECON-003, REQ-GARAGE-005 |
| **5. Building stays first-class** | Character mode is a guest inside city building. Entering and leaving it is fast and lossless, and it restores the player's camera and simulation speed. | Character-mode input that touches building tools, and career systems that punish the player for building | REQ-CTRL-003, REQ-CTRL-004, REQ-TIME-001, REQ-CITY-002 |

## 4. What the product is and is not

**It is:**

- a mod that runs inside CS2 and reads the city the player built;
- a single-player career in transportation (taxi, urban delivery, rigid-truck freight, and bus) with a personal
  wallet, a ledger, progression, owned vehicles, a home base, and a small company;
- a set of driving challenges (checkpoints, time trials, parking) on the player's own roads;
- a product that remains usable at each milestone: a walking and driving sandbox at M1, and a taxi career at M2
  ([ROADMAP](../../ROADMAP.md)).

**It is not:**

- a port of the city into another engine, or a second synchronized simulation;
- an import of Motor Town systems, assets, physics, or code;
- a reproduction of Streets of SimCity content or branding;
- a game that requires Melty or a running Motor Town ([REQ-REL-004](PRD.md));
- an open-world combat game, a vehicle-damage simulator, or a multiplayer service in v1.0.

## 5. Why each component matters to the vision

The roles are binding in [PRD](PRD.md) section 3. This table explains how each one shapes the vision.

| Component | Role | What it means for the vision |
|---|---|---|
| Cities: Skylines II | Host game; sole authority for the world and its native simulation | Pillars 1 and 5: the player's city is the content, and the native simulation is never replaced. |
| Beta Test Driving Mod | Candidate source-code foundation for direct vehicle control | A possible shortcut to driving. It is used only if the license audit and baseline tests succeed ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). Its reported features are not our features until we verify them. |
| Motor Town: Behind The Wheel | Behavioral and design reference | Shows what a satisfying transport career can feel like. We design original systems that fit a player-built city ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)). |
| Streets of SimCity | Inspiration | The fantasy of driving through a city you built. No content or branding is reused. |
| Melty | Optional workflow, still to be evaluated | It never shapes the product or its requirements ([MELTY_EVALUATION](../research/MELTY_EVALUATION.md)). |

## 6. Long-term direction

1. **v1.0** proves the core experience: build, walk, drive, work, earn, park, save, and continue. It does this safely,
   in any supported city ([PRD](PRD.md)).
2. **After v1.0**, expansions deepen the same fantasy along four lines: urban services (towing, ambulance, fire,
   police missions, native passengers), immersion (own interiors, a dealership, vehicle-specific cockpits, steering
   wheels), advanced logistics (articulated trucks, physical cargo and fleets, municipal-economy integration), and
   larger systems (part-level maintenance, structural damage, multiplayer). Each one enters scope only through an
   owner-approved PRD change ([EXPANSIONS](EXPANSIONS.md)).
3. Throughout, pillar 2 holds: we only claim a deeper integration (for example, native passengers or a physical
   fleet) after it has been built and validated.

## 7. What success means

- **Release success** is defined in [PRD](PRD.md) section 9: every `Must` requirement is validated or has an
  owner-approved scope decision, an independent tester completes the core experience without help, there are no
  known critical save or reward defects, and the legal audits are complete.
- **Experience success** is judged at two checkpoints:
  - the M2 fun checkpoint, where the project owner decides whether walking and driving are enjoyable before more
    content is added (RISK-023);
  - the onboarding test, where a new player completes a first job without developer help (REQ-ONBOARD-001,
    [SPEC-ONBOARD](../experience/ONBOARDING.md)).
- **Trust success:** players who load an older save, edit their city mid-career, or disable the mod find that the
  documented behavior is what actually happens ([PLAYER_JOURNEYS](PLAYER_JOURNEYS.md)).

No player counts, ratings, or dates are promised.

## 8. Provenance

- The vision statement and component roles come from the owner brief of 2026-10-18 **[Owner requirement]**. The
  owner brief confirms the earlier product definition in [chat.md](../archive/chat.md) **[Historical input]**: "you build
  your city normally, then take on a character, walk to a vehicle, start working, earn money, buy cars, develop a
  company, and keep exploring the city you created" (translated summary).
- The pillars are our synthesis of those requirements **[Proposal]**.
- "Drive the City II" is a working title. Player-facing naming is reviewed before release (RISK-015,
  [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md)).
