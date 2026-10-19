# ADR-0005: Mod Loading and Distribution Route

| Field | Value |
|---|---|
| ID | ADR-0005 |
| ADR status | Proposed |
| Date proposed | 2026-10-18 |
| Decision owner role | Project owner (distribution and installation policy); Coordinator with Reviewer (technical route) |
| Related | REQ-REL-001, REQ-REL-004, REQ-COMPAT-001, REQ-SAVE-006, [ADR-0001](ADR-0001-host-game-and-runtime.md), [CS2_INTEGRATION_FINDINGS](../../research/CS2_INTEGRATION_FINDINGS.md) §3.15, [MELTY_EVALUATION](../../research/MELTY_EVALUATION.md) §9, [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md), [PACKAGING_AND_PUBLISHING](../../release/PACKAGING_AND_PUBLISHING.md), CONF-012, CONF-014, OPEN-017, OPEN-036 |

## Context

A CS2 code mod can reach the game in two ways relevant to this project:

1. **The native route.** An assembly with the game's own mod entry point is loaded by the game's mod system from the
   local-mods location or from Paradox Mods subscriptions. It is built with the official modding toolchain
   (**[Documented upstream]** SRC-002, SRC-008). The candidate base mod uses this entry point, with no BepInEx, Harmony,
   or patching (**[Source-inspected]**, TASK-0002 at the pinned commit `eefd6bb…`).
2. **The BepInEx route.** A third-party loader installed into the game folder loads plugins from
   `{game}/BepInEx/plugins`. It is the **only** CS2 loader that Melty supports, BepInEx 5 v5.4.23.5, which Melty
   installs automatically (**[Documented upstream]** Melty service response, 2026-10-18; MELTY_EVALUATION M-03).

Melty's generic creator prompt mentions BepInEx 5 for CS2. The owner asked for that claim to be verified against the
installed game, the project architecture, the candidate mod's entry point, and Melty's loading routes before it is
adopted. Facts gathered on 2026-10-18:

- The installed game is 1.6.2f1 on Steam (Unity 2022.3.71f1, Mono runtime).
- No BepInEx, Doorstop, or MelonLoader files are in the game folder, and no mods are installed (TASK-0001).
- Melty cannot place files in CS2's native user-data location under `AppData\LocalLow` (validator rejection, M-08).
  Its manual mode is not one-click and therefore cannot be published through Melty (M-09).
- Melty has no live CS2 mashup (M-06).
- Melty's own kit note names the official toolchain plus Paradox Mods as the CS2 route (M-11).

## Decision drivers

1. **CS2 is the only required runtime, and the project modifies no game files** (ADR-0001; owner brief;
   [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) §4).
2. **Compatibility with the candidate base mod and the official toolchain**, so that reuse, debugging, and
   documentation apply directly.
3. **Distribution through the platform where CS2 players get mods** (Paradox Mods), with free distribution (Paradox
   terms, MELTY_EVALUATION M-05).
4. **No double loading** of conflicting controllers (REQ-COMPAT-001).
5. **Evidence over convenience:** nothing is adopted only because a tool's template suggests it.

## Options considered

| Option | Summary | Assessment |
|---|---|---|
| A. Native code mod, official toolchain, distributed through Paradox Mods (and local deployment for testing) | The route documented by Paradox and used by the base mod | Meets all drivers. Still needs a TASK-0003 build and run on 1.6.2f1. |
| B. BepInEx 5 plugin distributed through Melty | Melty's only CS2 route | Changes the game folder (driver 1), uses a different entry point from the base mod (driver 2), conflicts with Melty's exclusive licence (CONF-012), and is unverified on 1.6.2f1. |
| C. Native mod plus a BepInEx 5 bootstrap so that Melty can install it | A thin plugin that loads the native assembly | Combines B's costs with a double-loading risk next to a Paradox Mods copy (driver 4). Feasibility unverified. |
| D. Native mod listed on Melty in manual mode | Melty page only; players install by hand | Not one-click, so Melty's publishing flow rejects it (M-09). Exclusivity still applies. |

## Decision

**Proposed:** Option A. Drive the City II is built as a native CS2 code mod with the official modding toolchain. It is
deployed locally for tests and distributed through the official route (Paradox Mods), subject to OPEN-017 and the
owner's publishing decision in PHASE-13.

- **No third-party loader** (BepInEx, Doorstop, MelonLoader) is installed for this project, and the game's loading
  configuration is not modified.
- **Melty is not a distribution channel** for this project unless it later supports the native route and the
  exclusivity question (CONF-012) is resolved. Melty stays available as a reference tool.
- **Motor Town is never declared** as a required or companion game anywhere (ADR-0001, MELTY_EVALUATION §9
  representation rule).

The ADR stays **Proposed** until the project owner accepts it. Technical confirmation needs the TASK-0003 evidence.

## Consequences

- **Positive:**
  - no game-folder changes;
  - matches the base mod and the official documentation;
  - one distribution path;
  - no Melty licence lock-in.
- **Negative:**
  - no one-click Melty installation;
  - development needs the official toolchain, which is not installed yet: Unity Editor, .NET SDK, and possibly a
    Unity license (TOOLCHAIN_MATRIX; OPEN-406);
  - publishing needs a Paradox account (OPEN-017).
- **Neutral:** the generic Melty creator workflow (build → upload → test → publish) does not apply. Its test harness
  ideas may still inform our own test runs.

## Validation / experiment

1. **TASK-0003** (owner authorization required): build the pinned base mod with the official toolchain, deploy it to
   the native local-mods location, and observe it loading on 1.6.2f1 in a lab city. This confirms that the native
   route works on the installed version.
2. **PHASE-01:** the project's own skeleton loads the same way and passes the co-load guard against the original
   base mod.
3. **Re-evaluation trigger:** Melty adds a native CS2 destination or a Paradox Mods mode, **and** the exclusivity
   question is resolved. Then options B to D are re-assessed in a new ADR.

## Affected specifications

[PACKAGING_AND_PUBLISHING](../../release/PACKAGING_AND_PUBLISHING.md) (channels),
[INSTALL_UPGRADE_UNINSTALL](../../release/INSTALL_UPGRADE_UNINSTALL.md) (native install flow),
[BUILD_AND_RUN](../../engineering/BUILD_AND_RUN.md), [WINDOWS_SETUP](../../engineering/WINDOWS_SETUP.md) §8,
[COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md) (no BepInEx variant).

## Follow-ups

- Owner: accept or amend this ADR. Decide whether to contact Melty about a native CS2 route (optional; not done by
  agents).
- TASK-0003: native-route evidence; record `CSII_LOCALMODSPATH` (OPEN-036).
- PHASE-13: confirm Paradox Mods requirements (OPEN-017).
