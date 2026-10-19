# Module Contracts

| Field | Value |
|---|---|
| ID | SPEC-MODULES |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 (boundaries, Diagnostics, Persistence skeleton), PHASE-02 to PHASE-05 (Player, Vehicles, WorldContact), PHASE-06 onward (Career) |
| Responsible role | Coordinator (maintainer); Reviewer checks every port or ownership change |
| Related requirements | REQ-DIAG-001, REQ-DIAG-002, REQ-CTRL-001, REQ-CTRL-003, REQ-ENTITY-003, REQ-CITY-001, REQ-ECON-001, REQ-ECON-002, REQ-CAREER-004, REQ-RECOV-002, REQ-L10N-001 |
| Related documents | [SPEC-ARCH](ARCHITECTURE.md), [SPEC-CONTROL](CONTROL_AUTHORITY.md), [SPEC-ENTITY](ENTITY_LIFECYCLE.md), [SPEC-SAVE](SAVE_FORMAT.md), [CODING_STANDARDS](../engineering/CODING_STANDARDS.md), [DEBUGGING](../engineering/DEBUGGING.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md) |
| Last updated | 2026-10-18 |

> All module and port names are **our internal concepts**, not host-game APIs. Interface sketches are **[Proposal]**,
> illustrative only, uncompiled, and contain no host-game types. Validation status of every contract: **Not run**.

## 1. Purpose

Define, for each internal module, its responsibilities, inputs and outputs, the state it alone may write, the
dependencies it must not take, and the ports through which it talks to other modules. The goal is twofold: keep the
three critical areas (control, entities, saves) under single ownership, and keep career rules testable without
running Cities: Skylines II (CS2).

## 2. Scope and non-goals

In scope: the composition root and the eight modules named in [SPEC-ARCH](ARCHITECTURE.md), the write-ownership
matrix, the Career ports, the testability approach, and the Diagnostics contract (REQ-DIAG-001, REQ-DIAG-002).

Not in scope: folder layout, assembly names, language features, and the target framework (OPEN-003). Coding rules
are in [CODING_STANDARDS](../engineering/CODING_STANDARDS.md).

## 3. Rules common to all modules

1. **Ports belong to the consumer.** A module that needs something declares a port (interface). The provider
   implements it. The composition root wires implementations to ports at start-up.
2. **Single writer.** Every piece of mutable state has exactly one owning module (section 6). Other modules request
   changes through commands or ports; they never write directly.
3. **Host types stay in CityBridge.** Only CityBridge may reference host-game or engine types. Other modules exchange
   plain data: positions, IDs, enumerations, and value records defined by us.
4. **Events flow up, commands flow down.** Lower modules (CityBridge, WorldContact) publish events; the composition
   root routes them. Lower modules never call into Player, Career, or UI logic.
5. **Every module update is contained.** The Supervisor calls each module inside a fault boundary
   ([SPEC-RECOVERY](ERROR_RECOVERY.md)).
6. **Every module logs through Diagnostics** and never writes its own files.

## 4. Module contracts

### 4.1 Composition root and Supervisor

- **Responsibilities:** be the mod's single entry point; construct modules in dependency order; wire adapters to
  ports; register per-frame updates through CityBridge in a fixed order (input → Player → Vehicles → WorldContact
  queries → Career tick → UI); route events; own the fault boundary, the circuit breakers, and ordered shutdown
  (reverse construction order).
- **Inputs:** host load/unload notifications (via CityBridge, OPEN-101); settings (safe-disable switch).
- **Outputs:** module lifecycle calls; forced `Building` requests to Player on critical faults.
- **May write:** module lifecycle state, fault counters, the session's Degraded or Disabled flags.
- **Must not:** contain gameplay rules; hold references to host types except the registration handle it receives
  from CityBridge.
- **Unknowns:** where a mod entry point and update registration exist and in what order updates run (OPEN-002,
  OPEN-107). Mods may be activated late in start-up; the design assumes no early-initialization hook.

### 4.2 CityBridge

- **Responsibilities:** the only translation layer between host data and our types. Groups of capability adapters
  **[Proposal]**: city query, route planning, entity access (spawn, despawn, pose read/write for authorized
  entities, native takeover and handback primitives), simulation clock and speed, camera, input, host lifecycle
  events, save hook, surface and collision sampling, edit detection. It also owns the **entity registry**
  ([SPEC-ENTITY](ENTITY_LIFECYCLE.md)): every spawn request must name an owner module and a lifecycle, and the
  registry record is created in the same operation as the spawn.
- **Inputs:** requests from Player, Vehicles, WorldContact, Persistence; host state.
- **Outputs:** plain-data answers; events (`CityEdited`, `EntityInvalidated`, `HostSaving`, `HostLoading`,
  `HostUnloading`, `FocusChanged`). Event names are ours.
- **May write:** registry records; host entities **only** on behalf of an authorized owner in the registry; nothing
  in the native economy (except the gated experiment in [SPEC-CITY](CITY_INTEGRATION.md), off by default).
- **Must not:** decide gameplay; keep player state; persist anything itself; let a raw host entity reference
  escape into persisted data (REQ-SAVE-003).
- **Ports implemented:** Career's `ICityQuery`, `IRoutePlanner`, `ICareerClock`, `IMissionWorld`; WorldContact's
  surface and collision sampling port; Persistence's save-hook port; Player's camera, input, and clock ports.

### 4.3 Player

- **Responsibilities:** the ControlAuthority state machine (states, overlays, guards, transitions, cleanup);
  movement-authority grants and revocations for the avatar and the controlled vehicle; camera ownership and camera
  rigs (first-person, third-person, chase); input routing by state; the avatar controller; transition orchestration
  for entering and exiting vehicles; Recovery orchestration; the simulation-speed policy (record, set, restore).
- **Inputs:** input actions (via CityBridge input adapter), WorldContact answers, Vehicles status, Career commands
  results, host lifecycle events.
- **Outputs:** state-change events consumed by UI, Vehicles, and Career (`StateChanged`, `ArrivedNear(point)`);
  movement-authority grants to Vehicles.
- **May write:** player state and overlays; avatar pose (only while it holds avatar authority); active camera (only
  while camera owner); simulation speed (only on entering and leaving character mode and on restore,
  [SPEC-TIME](GAME_TIME_AND_PAUSE.md)).
- **Must not:** write vehicle poses (Vehicles does); change career records (it sends commands); read persisted data.
- Detailed contract: [SPEC-CONTROL](CONTROL_AUTHORITY.md).

### 4.4 WorldContact

- **Responsibilities:** surface determination and level tracking; ground validity; collision queries against
  available static geometry and bounded collision proxies; camera clip probes; safe-point search used by exit
  placement and Recovery.
- **Inputs:** query requests (position, shape, level context, entity class); sampling through CityBridge.
- **Outputs:** answers with explicit confidence (`Valid`, `Invalid`, `Unknown`); proxy budget events.
- **May write:** its own collision proxies only, within the budgets of [SPEC-CONTACT](WORLD_CONTACT.md).
- **Must not:** move entities; change player state; hold movement authority.

### 4.5 Vehicles

- **Responsibilities:** the driving controller (per [ADR-0004](decisions/ADR-0004-physics-strategy.md), pending);
  vehicle class parameters; spawning, parking, stowing, and despawning CareerVehicles (Rental, Owned, Service);
  sandbox borrowing and the handback protocol for native vehicles; applying fuel, wear, and upgrade effects supplied
  by Career.
- **Inputs:** driving input (routed by Player), authority grants from Player, WorldContact answers, vehicle-service
  calls from Career.
- **Outputs:** vehicle status (speed, position, fuel use, impacts) as plain data; `VehicleInvalidated` events.
- **May write:** the controlled vehicle's pose while it holds authority; career vehicle entities via the registry;
  handback cleanup on a borrowed vehicle.
- **Must not:** take a native vehicle in career mode (REQ-ENTITY-002); write a vehicle without a current authority
  grant; decide prices or ownership (Career does).
- **Ports implemented:** Career's `IVehicleService`.

### 4.6 Career (pure logic)

- **Responsibilities:** the shared mission lifecycle and legs ([SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md));
  settlement with deterministic TransactionIds; wallet and append-only ledger; garage records (rental contracts,
  ownership, fuel and wear accounting, upgrades); XP, reputation, unlocks; company managerial simulation with
  settled-until checkpoints; challenge definitions and results.
- **Inputs:** commands (from UI and Player), observations (arrival, vehicle status), port answers, clock readings.
- **Outputs:** domain events (`MissionStateChanged`, `Settled`, `Notification`) and an immutable snapshot for
  Persistence.
- **May write:** its in-memory career state only.
- **Must not:** reference host, engine, or UI types; read wall-clock time or random sources directly (it uses the
  clock and random ports so that tests are deterministic); write files.

### 4.7 Persistence

- **Responsibilities:** save identity; schema versioning; serialization; validation; migration; write and load
  protocols; quarantine; last valid copy; storage strategy adapter (In-save, Sidecar, or Hybrid, decided in
  [ADR-0003](decisions/ADR-0003-career-save-isolation.md), OPEN-006).
- **Inputs:** Career snapshots; host save and load events (OPEN-101); stable-reference resolution through CityBridge.
- **Outputs:** restored Career state or an explicit failure result (`NoCareerData`, `NewerSchemaRefused`,
  `Quarantined`, `RestoredFromEarlierRevision`).
- **May write:** persisted career data and quarantine records; nothing else.
- **Must not:** touch the player's primary saves outside the host's own save flow; delete quarantined data
  automatically; write data that failed validation over the last valid copy (REQ-SAVE-004).
- **Ports implemented:** Career's `ILedgerStore` (and the snapshot store). Detailed contract: [SPEC-SAVE](SAVE_FORMAT.md).

### 4.8 UI

- **Responsibilities:** HUD, prompts, menus (career and jobs, garage, company, ledger, settings), notifications,
  blocked-action feedback with a next step, localization keys for all player-facing text (REQ-L10N-001).
- **Inputs:** read-only view models from Player, Career, Vehicles, Diagnostics.
- **Outputs:** commands (`RequestEnterCharacterMode`, `AcceptOffer(id)`, `RentVehicle(...)`), never direct writes.
- **May write:** its own view state.
- **Must not:** change entities, career records, or settings except through the owning module's command.
- Behavior and presentation: [SPEC-UI](../experience/UI_AND_HUD.md); technology unknown (OPEN-015).

### 4.9 Diagnostics

Covers REQ-DIAG-001 and REQ-DIAG-002.

| Function | Contract **[Proposal]** |
|---|---|
| Bounded logs | Rotating log files with a size cap per file and a maximum file count (initial proposal: 5 files of 5 MB; to be confirmed in PHASE-01). Rate limiting collapses repeated identical messages into a count. |
| Version identification | Every log header and diagnostics bundle states the mod version, the build identifier (commit or build stamp once version control exists, OPEN-029), the persisted schema version, and the game version if the host reports it. |
| Structured events | Player state transitions, authority grants and revocations, registry census, faults, and persistence results are logged as structured, greppable lines. |
| Sanitized export | A player-triggered bundle containing logs, settings, schema versions, the last N state transitions, and the registry census. User-profile paths are replaced with placeholders; account names, store IDs, and machine names are removed; save names are hashed. No save files are included (RISK-030). |
| Settings | Per-user settings (not per save): key bindings (OPEN-014), accessibility, auto-pause on focus loss, debug overlays (off by default), safe-disable switch, the native-economy experiment toggle (off by default, hidden outside development builds). Settings UI technology is unverified (OPEN-015). |
| Safe-disable switch | When turned on, the Supervisor forces `Building` through normal cleanup, asks Persistence to persist valid career data, then stops Player, Vehicles, WorldContact, and Career updates for the session. The mod stays loaded but inert; nothing is deleted. Turning it off takes effect only in `Building`. |
| Health summary | Counts of faults per module, Degraded modules, proxy and registry counts; shown in the debug overlay and the export. |

Log location and collection steps are in [DEBUGGING](../engineering/DEBUGGING.md).

## 5. Career ports (illustrative sketch)

**[Proposal]** — pseudo-interfaces showing intent. Not compiled, not an API commitment, no host types. Names such as
`PlaceRef` and `AccessPointRef` are our value records ([SPEC-SAVE](SAVE_FORMAT.md) defines their persisted form).

```text
port ICityQuery {
  describe(place: PlaceRef) -> PlaceInfo | Missing          // building or stop still present?
  accessPoints(place: PlaceRef, kind: AccessKind) -> list<AccessPointCandidate>
  transitLines(mode: TransitMode) -> list<LineInfo>        // ordered stops (OPEN-020)
  cityRevision() -> CityRevisionStamp                       // changes when edits are detected (OPEN-021)
}
port IRoutePlanner {
  plan(from: MapPoint, to: AccessPointRef, cls: VehicleClass) -> Reachable(route) | Unreachable | Unknown
}
port ICareerClock {
  careerNow() -> CareerInstant        // career-active time (SPEC-TIME)
  simulationNow() -> SimInstant?      // in-game time, when available (OPEN-013, OPEN-104)
}
port IVehicleService {
  spawnCareerVehicle(id: CareerVehicleId, spec: VehicleSpec, at: AccessPointRef) -> Ok | Failed(reason)
  stow(id: CareerVehicleId) -> Ok | Failed(reason)
  status(id: CareerVehicleId) -> Present(pose) | Stowed(at) | Missing
}
port ILedgerStore {
  append(entry: LedgerEntry) -> Appended | DuplicateRejected  // keyed by TransactionId
  read(query: LedgerQuery) -> list<LedgerEntry>
}
port IMissionWorld {
  present(obj: MissionObjectSpec, at: AccessPointRef) -> MissionObjectHandle | Failed(reason)
  remove(handle: MissionObjectHandle)
}
port IRandomSource { next(bound: int) -> int }             // seeded; seed persisted with the mission
port INotifier     { publish(event: CareerEvent) }
```

Rules: a port answer of `Unknown` is never treated as `Reachable` or `Valid`; Career must not offer a mission whose
validation returned `Unknown` (REQ-CAREER-002).

## 6. Write-ownership matrix

| State | Sole writer | Others may |
|---|---|---|
| Player state, overlays, authority grants, camera owner | Player | Read via events |
| Avatar pose | Player (avatar or transition controller holding authority) | Query |
| Controlled vehicle pose | Vehicles (driving controller holding authority) | Query |
| Native vehicles | Nobody, except Vehicles during sandbox borrow and handback | Read through CityBridge |
| Native citizens, buildings, roads, economy | Nobody in v1.0 (REQ-CITY-001) | Read through CityBridge |
| Entity registry | CityBridge (on owner request) | Read census |
| Collision proxies | WorldContact | Query |
| Simulation speed | Player (TimePolicy) at mode boundaries only | Read |
| Career state (missions, wallet, ledger, garage, company, challenges) | Career | Read snapshots; send commands |
| Persisted career data and quarantine | Persistence | — |
| Settings, logs, export bundles | Diagnostics | Read settings |
| UI view state | UI | — |

## 7. Testing Career without CS2

1. **Separate compilation unit.** Career (and the serialization and migration parts of Persistence) is built as code
   with no reference to host or engine assemblies **[Proposal]**. Whether a test runner can target the same framework
   as the mod is part of OPEN-003 and [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
2. **Fakes for every port.** An in-memory city (places, access points, reachability table), a controllable clock, a
   seeded random source, an in-memory ledger store, and a vehicle service that records calls.
3. **Deterministic scenarios.** Mission lifecycle tables (every legal and illegal transition), settlement idempotence
   (replaying the same terminal event N times yields one ledger entry), company settlement across simulated pauses,
   mode switches, and reloads (REQ-TIME-004, REQ-COMPANY-003).
4. **Save round trips.** Snapshot → serialize → deserialize → compare, plus golden files for every schema version to
   test migrations ([SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md)).
5. **Invariant checks.** Wallet balance equals the sum of ledger entries; no TransactionId appears twice; no mission
   is `Active` without a valid leg.

In-game tests remain necessary for everything that touches the host; logic tests never replace them
([TEST_STRATEGY](../quality/TEST_STRATEGY.md)).

## 8. Failure handling

- Port failures return explicit results; exceptions crossing a module boundary are caught by the Supervisor and
  classified ([SPEC-RECOVERY](ERROR_RECOVERY.md)).
- A module that faults repeatedly is marked Degraded for the session (circuit breaker); features depending on it are
  disabled with a message. Career Degraded → no new offers and no persistence writes of new data; Player Degraded →
  forced `Building`.
- A missing capability at start-up (for example after a game update) disables only the dependent features, logs the
  reason with the game version, and leaves `Building` mode untouched.

## 9. Acceptance criteria

| Criterion | Test | Validation |
|---|---|---|
| Logs are size-bounded, carry version identification, and the export contains no personal data. | TEST-DIAG-001 | Not run |
| The safe-disable switch returns control to `Building` and stops career systems without data loss. | TEST-DIAG-002 | Not run |
| Career logic tests run without CS2 (lifecycle, idempotent settlement). | TEST-CAREER-001, TEST-CAREER-004 | Not run |
| Every mod entity has a registry owner; none remains after unload. | TEST-ENTITY-003 | Not run |
| A fault injected in each module is contained without a crash or save damage. | TEST-RECOV-002 | Not run |

## 10. Open questions and risks

- OPEN-002, OPEN-003 (entry point, framework), OPEN-014, OPEN-015, OPEN-026, OPEN-029 (build identifier), OPEN-101,
  OPEN-105, OPEN-107 (see [SPEC-ARCH](ARCHITECTURE.md) section 8).
- RISK-104 (new) — host types leak into Career and erode testability. Mitigation: dependency rule in review, Career
  built without host references, logic tests in every phase that touches Career.
- RISK-030 — personal data in logs or exports.

Register: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
