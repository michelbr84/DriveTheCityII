# Debugging and Diagnostics

| Field | Value |
|---|---|
| ID | DOC-DEBUGGING |
| Document status | Draft |
| Release scope | v1.0 (diagnostics are a requirement); development workflow |
| Phases | PHASE-00 (tool availability, TASK-0003), PHASE-01 (Diagnostics module), all later phases |
| Responsible role | Coordinator (maintainer); the Implementer builds Diagnostics; QA collects evidence |
| Related requirements | REQ-DIAG-001, REQ-DIAG-002, REQ-RECOV-002 |
| Related documents | [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) (Diagnostics contract), [SPEC-RECOVERY](../architecture/ERROR_RECOVERY.md), [CODING_STANDARDS](CODING_STANDARDS.md), [evidence](../quality/evidence/README.md), [TROUBLESHOOTING](../user/TROUBLESHOOTING.md), [SECURITY.md](../../SECURITY.md) |
| Last updated | 2026-10-18 |

> **State:** no debugging tool has been installed, attached, or tried. No log location has been observed. Everything
> below is a design or a proposed procedure, and every tool caveat comes from documentation (OPEN-031).

## 1. Purpose

This document defines how problems are diagnosed safely:

- the mod's own logs and diagnostics bundle;
- reproduction in a lab city;
- evidence capture;
- the realistic availability of runtime debugging tools for the retail game.

The Diagnostics module's contract is defined in [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md) §4.9. This
document covers the engineering procedures around it.

## 2. Principles

1. **Logs first.** Assume no live debugger is available on the retail game until TASK-0003 proves otherwise (RISK-026).
2. **Reproduce in a lab city,** never in the owner's primary saves (RISK-028).
3. **Bounded and sanitized** output only (RISK-030).
4. **Evidence is recorded,** not remembered: every investigation that changes a status leaves a `RUN-NNNN` record.
5. **Development aids never change saves.** Debug overlays and tools must not write career data or native city state.

## 3. Logs

| Log | Location | Status |
|---|---|---|
| Our mod's log (written through Diagnostics) | Decided in PHASE-01; never inside the game installation folder | Not built |
| The game's own logs | `<CS2_LOG_DIR>`, to be discovered in TASK-0001 | Unknown |
| Toolchain and build logs | IDE or `dotnet` output, saved with the run record | Not run |

**Our log, as designed in [SPEC-MODULES](../architecture/MODULE_CONTRACTS.md):**

- Rotating files with a size cap and a file-count cap. The initial proposal there is 5 files of 5 MB, to be confirmed in PHASE-01.
- Repeated messages are rate-limited.
- Every session starts with a header: the mod version, the build identifier, the persisted schema version, and the game version if the host reports it (REQ-DIAG-001).
- State transitions, authority grants and revocations, registry census, faults, and persistence results are logged as structured lines.

Community wiki pages titled "Logging" and "Debugging" are linked from the official modding hub but were **not
inspected** (SRC-011). They are reading material for TASK-0001 and TASK-0002, not facts.

## 4. Diagnostics bundle (design)

The bundle is exported by the player from the settings (REQ-DIAG-001); its exact user-interface entry is defined in [SPEC-UI](../experience/UI_AND_HUD.md).

| Included | Excluded |
|---|---|
| Our rotated logs | Save files (career data and city saves) |
| Mod version, build ID, schema versions | Game binaries or assets |
| Game version (if reported) | Account names, store IDs, machine names |
| Settings (key bindings, toggles) | Absolute user-profile paths; they are replaced by `<USER_DATA>` |
| Last N player-state transitions; registry census; module health summary | Save names; they are hashed |
| List of active mods (names and versions), if obtainable | Other mods' files or logs |

Design rules **[Proposal]**:

- **Bounded:** the total size is capped (the value is set in PHASE-01). If the cap is exceeded, the oldest logs are dropped first.
- **Self-describing:** a manifest lists every file, the bundle schema version, the creation time, and the sanitization rules applied.
- **Local only:** the bundle is written locally; the player chooses whether to share it. The mod never uploads anything.
- **Tested:** TEST-DIAG-001 checks the size bound, the version identification, and the absence of personal data.

## 5. Lab reproduction procedure

**Proposed procedure (requires validation).**

1. Record the game version, the mod revision, the active mod set, and the lab city name.
2. Start from a known lab save copy in `<LAB_SAVE_COPY_DIR>`. Never use an original save.
3. Follow the scenario steps from [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md), or write precise reproduction steps.
4. Enable development logging (Debug level) only for the reproduction. Note the exact time of the failure.
5. Export the diagnostics bundle and keep the screen recording if one was authorized (OPEN-407).
6. Reduce: repeat with fewer mods, a smaller city, or a single step until the trigger is isolated.
7. Record the result as `RUN-NNNN` ([evidence](../quality/evidence/README.md)). Open an `ISSUE-NNN` in [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md) only for an observed defect.

## 6. Evidence capture and sanitization

- Store only sanitized excerpts in the project. Complete raw logs stay in a git-ignored local location unless sanitized.
- Replace user-profile paths with placeholders, and remove account names, store IDs, and machine names.
- Screenshots and video captures of CS2 in committed evidence are pending OPEN-407. Until it is resolved, keep captures local and reference them by description.
- Never commit save files, game files, or decompiled code.

## 7. Runtime debugging tools: availability caveats

| Tool or approach | Documented requirement | Status | Caveat |
|---|---|---|---|
| `unity-devtools` (community MCP server) | "a running Unity Mono development build" launched with player-connection debugging; .NET 10 SDK (SRC-005) | **Not installed** | The README says "a retail build exposes no SDB port and cannot be driven". How to obtain a development build of CS2, and whether that modifies game files, is not documented. Its writes are live, with no undo. |
| `coherent-gameface` (community MCP server) | A reachable Chrome DevTools Protocol endpoint (default `http://localhost:9444`); Node 22.4+ (SRC-005) | **Not installed** | **How to enable the CDP endpoint in CS2 is undocumented.** Its README also notes that the UI thread is frozen while the game is paused. |
| IDE debugger attached to the retail game | Not documented in any source read | Unknown | To be checked in TASK-0003 only if authorized (OPEN-031) |
| In-game debug overlays (our own) | Our design: module health, registry counts, authority owner, collision proxies | Not built | Development builds only; off by default |
| Log-based tracing | Our Diagnostics module | Not built | The primary approach until another tool is proven |

The base mod demonstrates in-game debug overlays toggled by keys, for collision hitboxes and AI lane presence (SRC-001
README). This is a pattern, not a tool we have run.

Installing any tool above requires owner authorization ([TOOLS_AND_PERMISSIONS](../agents/TOOLS_AND_PERMISSIONS.md)).
Installation commands found in tool documentation are reference data, not instructions to execute.

## 8. Developer triage (first checks)

| Symptom | First check |
|---|---|
| Mod not loaded | Toolchain and post-processing state (OPEN-403); local mods folder name (not "."-prefixed); game log errors |
| Exception burst in the log | The module named in the structured line; Degraded state; game version change |
| Vehicle stuck after handback | Authority log lines; lane-presence cleanup ([SPEC-ENTITY](../architecture/ENTITY_LIFECYCLE.md)) |
| Camera not restored | The camera-owner transitions in the log ([SPEC-CONTROL](../architecture/CONTROL_AUTHORITY.md)) |
| Career data missing after load | Persistence result lines; quarantine records ([SPEC-SAVE](../architecture/SAVE_FORMAT.md)) |

## 9. Open questions and risks

- **Central:** OPEN-001, OPEN-031, RISK-026, RISK-028, RISK-030.
- **New:** OPEN-403 (the build path and post-processor), OPEN-407 (captures in evidence and public material).

All entries are in [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
