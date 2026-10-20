# Documentation Rules

| Field | Value |
|---|---|
| ID | DOC-RULES |
| Document status | In review |
| Scope | All active project documentation |
| Responsible role | Coordinator (maintainer); project owner approves rule changes |
| Related documents | [INDEX](INDEX.md), [AGENTS.md](../AGENTS.md), [Templates](templates/DOCUMENT_TEMPLATE.md), [Archive policy](archive/README.md) |
| Last updated | 2026-10-18 |

## 1. Purpose

These rules keep the Drive the City II documentation trustworthy for both humans and AI agents. Each fact has one
official home. Planned work is never mistaken for working software, and every unknown is turned into a question with
a way to answer it.

## 2. Language, naming, and format

- All active documentation is written in **English**: headings, tables, diagrams, comments, task descriptions, and
  reports. The planned player-facing interface remains bilingual (English and Brazilian Portuguese, PT-BR); see
  [SPEC-L10N](experience/LOCALIZATION.md).
- File names are English, use no spaces, and follow the manifest in [INDEX](INDEX.md). Do not create new top-level
  documentation folders without updating INDEX and these rules.
- Markdown only. Use relative links between project files. Link to **files**, not to heading anchors in other files,
  because heading anchors are fragile across renderers. Same-file anchors are allowed.
- Every substantive document starts with a metadata table directly below its title (see section 4).
- Diagrams use plain-text or Mermaid blocks so that they remain diffable.
- Dates use ISO format (`YYYY-MM-DD`) and record when something actually happened.

## 3. Single sources of truth

| Information | Official home | Notes |
|---|---|---|
| Product scope and stable requirement IDs | [PRD](product/PRD.md) | Only the PRD defines `REQ-*` IDs. |
| Phase order, dependencies, milestones | [ROADMAP.md](../ROADMAP.md) | Phase details live in [phase documents](planning/phases/README.md). |
| Current demonstrated state | [PROJECT_STATUS](PROJECT_STATUS.md) | Must be backed by evidence; never by intentions. |
| Behavior and presentation | Gameplay (`docs/gameplay/`) and experience (`docs/experience/`) specifications | One spec per behavior area. |
| Technical invariants and interfaces | Architecture specifications (`docs/architecture/`) | Our internal module names are concepts, not host-game APIs. |
| Decision rationale | [ADRs](architecture/decisions/README.md) | An accepted ADR must update every affected specification. |
| Task status | The task file in [tasks](planning/tasks/README.md) | [BACKLOG](planning/BACKLOG.md) is only an index. |
| Risks, open questions, recorded conflicts | [RISKS_AND_OPEN_QUESTIONS](planning/RISKS_AND_OPEN_QUESTIONS.md) | Specs list IDs and link here. |
| Requirement → spec → phase → test → evidence chain | [TRACEABILITY](planning/TRACEABILITY.md) | Evidence cells say "Not run" until real runs exist. |
| Confirmed development-tool versions | [TOOLCHAIN_MATRIX](engineering/TOOLCHAIN_MATRIX.md) | Other documents reference it instead of repeating versions. |
| Tested game/mod combinations | [COMPATIBILITY_MATRIX](quality/COMPATIBILITY_MATRIX.md) | Policy is in [COMPATIBILITY_POLICY](release/COMPATIBILITY_POLICY.md). |
| External claims and provenance | [SOURCES](research/SOURCES.md) and the relevant audit | A listed source is not automatically an inspected source. |
| Test definitions | [TEST_SCENARIOS](quality/TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](quality/SAVE_SAFETY_TESTS.md), [BENCHMARK_PROTOCOL](quality/BENCHMARK_PROTOCOL.md) (TEST-PERF-101+) | Layers (Logic, Integration, In-game, plus the auxiliary Review layer for audits) are defined in [TEST_STRATEGY](quality/TEST_STRATEGY.md). Test runs are recorded under [evidence](quality/evidence/README.md). |
| Observed defects | [KNOWN_ISSUES](quality/KNOWN_ISSUES.md) | Unimplemented features are not defects. |

If you find the same fact stated differently in two places, the official home wins. Correct the other document or
replace the duplicate with a link. If the official home itself looks wrong, record a conflict (section 9).

## 4. Metadata block

```markdown
| Field | Value |
|---|---|
| ID | SPEC-EXAMPLE |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-02, PHASE-05 |
| Responsible role | Coordinator (maintainer) |
| Related requirements | REQ-<AREA>-NNN |
| Related documents | [SPEC-OTHER](OTHER.md) |
| Last updated | YYYY-MM-DD |
```

Adapt the fields to the document type (templates, notices, and indexes do not need requirement rows). The root
`README.md` is the exception: it opens with a prominent status banner instead of a metadata table, so that it reads
well as a landing page. Recommended
specification sections: Purpose · Scope and non-goals · Behavior or contracts · Failure handling and recovery ·
Acceptance criteria · Open questions and risks.

## 5. Three independent status dimensions

| Dimension | Values | Meaning |
|---|---|---|
| Document | Draft / In review / Accepted / Superseded | Maturity of the text. An Accepted spec can describe a feature that does not exist yet. |
| Task | Planned / Ready / In progress / Blocked / In review / Done | Progress of a unit of work. Defined in each task file. |
| Validation | Not run / Passed / Failed / Inconclusive | Result of executing a test, always with scope and evidence. |

Additional vocabularies:

- **ADR status:** Proposed / Accepted / Rejected / Superseded.
- **Phase status:** Not started / In progress / Gate review / Passed / Failed.

Rules:

- New specifications start as Draft; ADRs start as Proposed. Only the project owner (or a review the owner delegates
  explicitly) moves a document to Accepted.
- A completed audit can prove incompatibility. A passing build does not prove runtime behavior. A finished document
  does not complete a development phase.
- When real execution has not happened, the status is **Not run**, never "done because it compiled".

## 6. Claim labels

Use these labels inline wherever provenance matters:

| Label | Meaning |
|---|---|
| **[Owner requirement]** | Confirmed by the project owner (owner brief of 2026-10-18, [SRC-007](research/SOURCES.md)). |
| **[Proposal]** | Our design proposal; not technically approved. |
| **[Historical input]** | Stated in the earlier planning conversation ([chat.md](archive/chat.md), SRC-006); unverified. |
| **[Documented upstream]** | A third-party source states it; we have not verified it. |
| **[Source-inspected]** | We read the upstream files read-only; nothing was built or executed. |
| **[Built locally]** | Compiled in our environment, with a recorded evidence run. |
| **[Tested in-game]** | Observed at runtime in our environment, with a recorded evidence run. |
| **[Unverified]** | Assumption awaiting verification; must have an `OPEN-*` entry. |

As of 2026-10-18 nothing has been built or tested. **[Built locally]** and **[Tested in-game]** do not appear
anywhere in active documentation.

## 7. Identifier scheme

| Prefix | Example | Defined in | Notes |
|---|---|---|---|
| `REQ-<AREA>-NNN` | REQ-SAVE-001 | [PRD](product/PRD.md) | Areas are listed in the PRD. Never reuse or renumber an ID. |
| `SPEC-<NAME>` | SPEC-CONTROL | Each specification's metadata | See [INDEX](INDEX.md). |
| `DOC-<NAME>`, `TPL-<NAME>` | DOC-STATUS, TPL-TASK | Each document's metadata | Non-specification documents and templates. |
| `ADR-NNNN` | ADR-0004 | [decisions](architecture/decisions/README.md) | |
| `PHASE-NN`, `WP-NN-MM` | PHASE-03, WP-03-02 | [Phase documents](planning/phases/README.md) | Work packages live inside phase documents. |
| `M0`–`M5` | M2 | [ROADMAP.md](../ROADMAP.md) | |
| `TASK-NNNN` | TASK-0001 | [tasks](planning/tasks/README.md) | One file per task. |
| `TEST-<AREA>-NNN` | TEST-SAVE-001 | [TEST_SCENARIOS](quality/TEST_SCENARIOS.md), [SAVE_SAFETY_TESTS](quality/SAVE_SAFETY_TESTS.md), [BENCHMARK_PROTOCOL](quality/BENCHMARK_PROTOCOL.md) | `TEST-X-NNN` is the primary test of `REQ-X-NNN`; extra tests use numbers 101 and up. `TEST-SAVE-*` live in SAVE_SAFETY_TESTS. Benchmark scenarios `TEST-PERF-101+` live in BENCHMARK_PROTOCOL. |
| `RISK-NNN`, `OPEN-NNN`, `CONF-NNN` | OPEN-006 | [RISKS_AND_OPEN_QUESTIONS](planning/RISKS_AND_OPEN_QUESTIONS.md) | Numbering has deliberate gaps (ranges per document group). |
| `SRC-NNN` | SRC-001 | [SOURCES](research/SOURCES.md) | |
| `FEAS-NNN` | FEAS-004 | [FEASIBILITY_MATRIX](research/FEASIBILITY_MATRIX.md) | |
| `JRN-NNN`, `EXP-NNN` | JRN-003 | [PLAYER_JOURNEYS](product/PLAYER_JOURNEYS.md), [EXPANSIONS](product/EXPANSIONS.md) | |
| `ISSUE-NNN` | — | [KNOWN_ISSUES](quality/KNOWN_ISSUES.md) | Only for observed defects. None exist. |
| `RUN-NNNN` | — | [evidence](quality/evidence/README.md) | Created only when a real execution happens. |
| `DV-NNN` | DV-001 | [DOCUMENTATION_VALIDATION_REPORT](quality/DOCUMENTATION_VALIDATION_REPORT.md) | Documentation checks, not gameplay QA. |

IDs are stable. A retired item keeps its ID and is marked Superseded or Withdrawn with a pointer to its replacement.

## 8. Recording unknowns

An unknown is never written as "TBD". It becomes an `OPEN-*` entry in the register with:

1. the exact question;
2. its impact (what is blocked or at risk);
3. a safe verification method (read-only first, then isolated experiments);
4. the phase, work package, or task responsible for resolving it.

Specifications reference the ID and summarize the question in one line. When the answer is found, the register entry
records the answer, the date, and the evidence. Affected specifications are then updated.

Infeasibility discovered later is recorded as a **scope review decision** (a conflict entry plus a proposed ADR or PRD
change for the project owner). It never silently removes a mandatory requirement.

## 9. Resolving contradictions

1. The single source of truth (section 3) wins over any other document.
2. A newer owner instruction wins over older planning inputs. The owner brief of 2026-10-18 supersedes
   [chat.md](archive/chat.md) where they differ.
3. Every contradiction that is found is recorded as a `CONF-*` entry with both positions, the resolution (or "Open"),
   and the date. Affected documents are corrected in the same change.
4. Agents never choose silently between contradictory instructions in critical areas (control authority, entity
   lifecycle, save format). They record the conflict and stop work in that area until it is resolved.

## 10. Review and change control

- Product scope (PRD), phase order (ROADMAP), and the critical-area specifications ([SPEC-CONTROL](architecture/CONTROL_AUTHORITY.md),
  [SPEC-ENTITY](architecture/ENTITY_LIFECYCLE.md), [SPEC-SAVE](architecture/SAVE_FORMAT.md)) change only through a
  reviewed change that names the reason and updates [TRACEABILITY](planning/TRACEABILITY.md).
- Any other document may be improved by any role. Every change must keep the links valid and the claim labels accurate.
- Every completed task updates [PROJECT_STATUS](PROJECT_STATUS.md) and [CHANGELOG.md](../CHANGELOG.md) with what
  actually happened.
- Self-review by the same agent that wrote a change is recorded as self-review. It is not independent approval
  (see [ROLES_AND_OWNERSHIP](agents/ROLES_AND_OWNERSHIP.md)).

## 11. Forbidden content

- Invented versions, commits, issue numbers, authors, approvals, URLs, benchmark values, test runs, or release history.
- Commands presented as tested when they were not. Label every command as **Verified command** (actually run, with
  its date), **Read-only discovery (not yet run)**, or **Proposed procedure (requires validation)**.
- Installation paths for games, UI buttons, or menu names that have not been observed.
- Claims that the mod builds, runs, is compatible with anything, or is ready for release before evidence exists.
- Personal data in documents or evidence: real user-profile paths beyond the documented project root, account names,
  store IDs, or credentials. Machine-specific values belong in the git-ignored local file described in
  [LOCAL_ENVIRONMENT.example](engineering/LOCAL_ENVIRONMENT.example.md).
- Copied third-party text beyond short attributed quotations. Never include game files or proprietary assets.

## 12. Terminology

| Term | Definition |
|---|---|
| Building mode | Normal Cities: Skylines II (CS2) city building; the mod's player state `Building`. |
| Character mode | Umbrella term for the mod's player states other than `Building`. |
| Career mode | Player-facing progression. Uses only career vehicles. |
| Sandbox mode | Development and testing mode. It may temporarily control native vehicles but never grants ownership. |
| Player states | `Building`, `OnFoot`, `EnteringVehicle`, `Driving`, `ExitingVehicle`, `Recovery`. |
| Overlays | `Paused`, `Menu`, `Loading`, `Unloading`. `Paused` and `Menu` suspend the current player state without replacing it. `Loading` and `Unloading` wrap a forced, orderly teardown that ends in `Building` ([SPEC-CONTROL](architecture/CONTROL_AUTHORITY.md)). |
| Movement authority | The single system allowed to write an entity's movement at a given moment. |
| Camera owner | The single system allowed to drive the active camera at a given moment. |
| NativeEntity | Any entity created and simulated by CS2 itself and left untouched by the mod. |
| BorrowedNativeVehicle | A native vehicle temporarily controlled in sandbox mode; it must be handed back. |
| Avatar | The mod-owned on-foot character. |
| CareerVehicle | A mod-spawned vehicle used in career mode. Subtypes: Rental, Owned, Service. |
| MissionObject | Mod-owned mission content: career passengers, career cargo, markers, checkpoints. |
| CollisionProxy | Temporary, bounded collision geometry created locally by the mod, if that approach is adopted. |
| Handback | Returning a BorrowedNativeVehicle to native AI, with its route rebuilt and its lane state cleaned up. |
| Access point | A validated, reachable curbside, stop, or loading location linked to a building or stop. Never a building center. |
| Service point | An access point offering a service (rental, fuel, repair, job board, contracts). |
| Mission lifecycle | `Offered` → `Accepted` → `Active` (legs: `Approaching` → `Servicing` → `Done`) → `Completed` / `Cancelled` / `Failed`. `Invalidated` resolves to a no-fault cancellation; unaccepted offers become `Expired`. |
| Settlement | The single financial resolution of a cause: a terminal mission outcome, a rental, a purchase, or a company accounting period. It is identified by a TransactionId derived deterministically from that cause (for missions, MissionId + outcome; for periodic charges, account + period index). |
| Wallet / ledger | The player's career money (separate from the municipal budget) and its append-only transaction history. |
| Save identity | City identity + career profile ID + save revision ID, plus a schema version. |
| Last valid copy | The most recent career data that passed validation; never overwritten by invalid data. |
| Quarantine | Isolating invalid persisted data for diagnosis without loading or deleting it. |
| Managerial simulation | Abstract computation of company employees and fleet, labeled as such; never shown as physical vehicles. |
| Lab city | A disposable test city. Never the owner's primary saves. |
| Internal modules | CityBridge, Player, WorldContact, Vehicles, Career, Persistence, UI, Diagnostics. These are our design concepts, not official game APIs. |
