# ADR-0003: Career Isolation from the Municipal Economy and Revision-Bound Career Saves

| Field | Value |
|---|---|
| ID | ADR-0003 |
| ADR status | Proposed |
| Date proposed | 2026-10-18 |
| Decision owner role | Project owner (requirements already confirmed); Coordinator with Reviewer (storage mechanism, critical area) |
| Related | REQ-ECON-001, REQ-ECON-002, REQ-CITY-001, REQ-CITY-003, REQ-SAVE-001 to REQ-SAVE-006, [SPEC-SAVE](../SAVE_FORMAT.md), [SPEC-CITY](../CITY_INTEGRATION.md), [SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md), OPEN-006, OPEN-022, OPEN-101, OPEN-103, OPEN-108, RISK-005, RISK-017, RISK-101 |

## Context

The career adds money, vehicles, missions, and a company. Two questions follow. First, does career money live in the
city's economy? Second, where and how is career data stored so that it belongs to the right save? The earlier plan
already preferred data tied to the save itself and, failing that, a file tied to the exact identity and revision of
the save, and warned that loading an old save must never bring back money from a future session
**[Historical input]** ([chat.md](../../archive/chat.md)).

The owner brief requires: a personal wallet separate from the municipal budget; no native economic writes in v1.0
except a gated, off-by-default experiment; city/profile/save-revision identity; schema versions; stable references;
atomic writes where applicable; quarantine; protection of the last valid copy; and a comparison of in-save and
revision-bound sidecar storage **without asserting an unverified hook** **[Owner requirement]**.

Community documentation describes a mechanism for code mods to serialize their own data with the save and states that
removing such a mod drops that data and keeps the city **[Documented upstream, community; unverified]**
([SOURCES](../../research/SOURCES.md)). No official documentation of save serialization for mods was identified in the
2026-10-18 research pass. Nothing has been tested.

## Decision drivers

1. No leak: an older save never shows newer career data (REQ-SAVE-001).
2. No corruption of the city or its save; the city stays loadable without the mod (REQ-SAVE-006, REQ-CITY-004).
3. A failure in a career feature cannot damage the native simulation (separation of economies).
4. Atomic, verifiable writes and recoverable invalid data (REQ-SAVE-004).
5. Forward migration across mod versions (REQ-SAVE-002).
6. Robustness to players copying, renaming, or syncing saves (RISK-101).
7. Implementation and maintenance cost; exposure to game updates (RISK-007).

## Options considered

**Part 1: economic separation**

| Option | Assessment |
|---|---|
| 1A. Separate wallet and ledger owned by the mod | Required by the owner; contains career defects; simple to test without the game |
| 1B. Career money flows through the municipal budget | Excluded from v1.0 by the owner; only a gated experiment is allowed ([SPEC-CITY](../CITY_INTEGRATION.md) section 9) |

**Part 2: storage mechanism** (full comparison in [SPEC-SAVE](../SAVE_FORMAT.md) section 6)

| Option | Strength | Weakness |
|---|---|---|
| 2A. In-save | Data travels with the save; revision binding is natural | Depends on an unverified host capability; atomicity is the host's; behavior on mod removal unknown (OPEN-022) |
| 2B. Sidecar, revision-bound | Fully under our control; atomic temp-and-rename writes; easy quarantine | Needs a reliable save fingerprint and save-event detection (OPEN-006, OPEN-101); separated from the save on copy or sync (RISK-101); retention problem (OPEN-108) |
| 2C. Hybrid: small anchor in the save, payload in a sidecar | Identity travels with the save; payload writes stay atomic; small in-save footprint | Combines both dependencies; payload still separable from the save |

## Decision

**Part 1:** Option 1A, adopted as an owner requirement (2026-10-18). The wallet and the append-only ledger are separate
from the municipal budget; v1.0 performs no native economic writes.

**Revision-bound identity:** adopted as an owner requirement. Career data is bound to (city identity, career profile
ID, save revision ID) plus a schema version, with the no-leak rule and ancestor-only fallback of
[SPEC-SAVE](../SAVE_FORMAT.md).

**Part 2, storage mechanism: Pending — PHASE-01 spike (OPEN-006).** No option is selected before evidence exists.

## Consequences

- Career logic and persistence can be built and tested before the mechanism is chosen, behind the Persistence storage
  port ([SPEC-MODULES](../MODULE_CONTRACTS.md)).
- Every option must satisfy the same write and load protocols, validation, quarantine, and last-valid-copy rules.
- Career data is written only together with host saves, so career progress and city progress stay consistent.
- If no option can bind data to a save revision reliably, REQ-SAVE-001 is at risk and a scope review is recorded.

## Validation / experiment

PHASE-01 spike in a lab city (never the owner's primary saves):

| Check | Applies to | Evidence |
|---|---|---|
| Can custom data be stored with the save and read back after reload? | 2A, 2C | Lab run with a test record |
| Can the mod observe save and load events, including autosave, in time to write? | 2B, 2C | OPEN-101 lab run |
| Can a save be identified unambiguously on load (anchor or fingerprint)? | All | Save, copy, rename, overwrite, and reload sequence |
| Does the no-leak sequence of SPEC-SAVE section 9 pass? | All | TEST-SAVE-001 |
| Interrupted write, corrupt payload, newer schema | All | TEST-SAVE-002, TEST-SAVE-004 |
| Save loads with the mod disabled and removed; what remains? | All | TEST-SAVE-006 (OPEN-022) |

Gate: the chosen option passes every check that applies to it; the Reviewer confirms the evidence; the owner accepts.
Validation status of every check: **Not run**.

## Affected specifications

[SPEC-SAVE](../SAVE_FORMAT.md), [SPEC-CITY](../CITY_INTEGRATION.md), [SPEC-ECON](../../gameplay/PROGRESSION_AND_ECONOMY.md),
[SPEC-ENTITY](../ENTITY_LIFECYCLE.md), [INSTALL_UPGRADE_UNINSTALL](../../release/INSTALL_UPGRADE_UNINSTALL.md),
[SAVE_SAFETY_TESTS](../../quality/SAVE_SAFETY_TESTS.md).

## Follow-ups

- Record the spike result and the chosen mechanism here and in SPEC-SAVE; answer OPEN-006 in the register.
- If Sidecar or Hybrid is chosen: define retention (OPEN-108) and a player-facing note about copying saves (RISK-101).
- Re-run TEST-SAVE-006 with the release candidate in PHASE-13.
