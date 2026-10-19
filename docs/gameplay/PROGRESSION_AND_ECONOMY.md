# Progression and Economy

| Field | Value |
|---|---|
| ID | SPEC-ECON |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-06 (minimal slice), PHASE-07 (full), PHASE-09 (company transfers), PHASE-10 (challenge rewards), PHASE-11 (tutorial presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module; Reviewer review required for ledger or settlement changes |
| Related requirements | REQ-ECON-001 to REQ-ECON-005, REQ-CAREER-004, REQ-GARAGE-001, REQ-GARAGE-006, REQ-SAVE-001, REQ-SAVE-004, REQ-COMPANY-003 |
| Related documents | [SPEC-MISSION](MISSION_FRAMEWORK.md), [SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-COMPANY](COMPANIES_AND_FLEET.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [SPEC-TIME](../architecture/GAME_TIME_AND_PAUSE.md), [SPEC-ONBOARD](../experience/ONBOARDING.md), [SPEC-UI](../experience/UI_AND_HUD.md), [GAME_DESIGN](../product/GAME_DESIGN.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or balanced. **All numbers in this document are
> illustrative [Proposal] values in a placeholder unit "cr" (credits).** They are not measured, final, or tested. This
> document is the single home for illustrative economy values; other gameplay specifications refer to §8 instead of
> repeating numbers. Validation: **Not run**.

## 1. Purpose

This document defines the player's personal economy and long-term progression: the wallet, the append-only ledger,
XP per career, reputation, unlocks, the principles for balancing them, a safe beginner economy, and recovery from
insolvency. The aim is that work always leads to meaningful choices and that no sequence of events leaves the career
unrecoverable.

## 2. Scope and non-goals

**In scope:** the personal wallet and ledger, transaction types, XP and levels per career, global reputation, unlocks,
balancing principles, insolvency recovery, and economic tutorial hooks.

**Non-goals for v1.0 [Owner requirement]:** any write to the municipal budget or native economy (REQ-ECON-001,
REQ-CITY-001). Also out of scope **[Proposal]**: loans with interest, a stock market, taxes on the player, and real-money
anything. Company finances are a separate account ([SPEC-COMPANY](COMPANIES_AND_FLEET.md)).

## 3. Wallet

- One wallet per career profile, per city save. It is **completely separate from the municipal budget**: no career
  transaction reads from or writes to city finances (REQ-ECON-001).
- Amounts are integers in minor units **[Proposal]**, so no rounding drift accumulates. The wallet balance is a
  **derived cache** of the ledger. The ledger is the source of truth.
- The wallet cannot go negative through player purchases: a purchase that cannot be paid is refused. Mandatory charges
  that cannot be paid become **deferred charges** (§7).
- In the player's eyes the wallet must not look like city money. It uses a distinct name and icon, explained in the
  tutorial (RISK-202). The final player-facing currency name is a UI and localization decision
  ([SPEC-L10N](../experience/LOCALIZATION.md)).

## 4. Ledger

Append-only and persistent (REQ-ECON-002). The player can view it as transaction history with filters by type and
career ([SPEC-UI](../experience/UI_AND_HUD.md)).

**Entry fields:** TransactionId (unique), type, signed amount, balance after, career-clock timestamp, in-game date (for
display), source reference (MissionId, VehicleRecordId, RentalId, ServiceVisitId, CompanyId plus period), itemized
breakdown lines, and a localized description key.

**Invariants:**
1. A duplicate TransactionId is rejected with `AlreadyApplied` and changes nothing (idempotence).
2. At most one settlement exists per MissionId ([SPEC-MISSION](MISSION_FRAMEWORK.md)).
3. Wallet balance equals the sum of all entry amounts. On load, a mismatch causes the wallet to be recomputed from the
   ledger, with a diagnostics entry. A ledger that fails validation is quarantined and the last valid copy is kept
   (REQ-SAVE-004, [SPEC-SAVE](../architecture/SAVE_FORMAT.md)).
4. Entries are never edited or deleted. A correction is a new `Correction` entry.

**Operation IDs for player-initiated actions [Proposal]:** when the player confirms a purchase, sale, refuel, repair,
upgrade, rental, or transfer, an operation ID is generated **once** and stored with the pending operation before it is
applied. The TransactionId derives from it. A replayed confirmation, a double click, or a reload therefore cannot
charge twice.

| Type | Sign | TransactionId shape **[Proposal]** | First phase |
|---|---|---|---|
| InitialBalance | + | `INIT-<ProfileId>` | PHASE-06 |
| MissionSettlement | ± or 0 | `MS-<MissionId>-<Outcome>` | PHASE-06 |
| RentalCharge | − | `RN-<RentalId>-<ChargeIndex>` | PHASE-06 (basic share), PHASE-07 |
| RentalDeposit / DepositRefund | − / + | `RD-<RentalId>` / `RR-<RentalId>` | PHASE-07 |
| VehiclePurchase / VehicleSale | − / + | `VP-<VehicleRecordId>` / `VS-<VehicleRecordId>` | PHASE-07 |
| Fuel / Maintenance / Upgrade | − | `FU-<ServiceVisitId>` / `MR-<ServiceVisitId>` / `UP-<OperationId>` | PHASE-07 |
| RecoveryService (tow, roadside fuel) | − | `RS-<IncidentId>` | PHASE-07 |
| DeferredChargeRepayment | − | `DR-<SettlementTransactionId>` | PHASE-07 |
| GarageExpansion | − | `GE-<OperationId>` | PHASE-09 |
| CompanyFounding / CompanyTransfer | − / ± | `CF-<CompanyId>` / `CT-<TransferId>-W` (wallet side) | PHASE-09 |
| ChallengeReward | + | `CH-<ChallengeId>-<RouteVersion>-<Tier>` | PHASE-10 |
| Correction | ± | `CX-<SchemaVersion>-<Sequence>` | as needed (migration) |

**Growth [Proposal]:** after 5,000 entries, the oldest entries beyond the most recent 1,000 may be compacted into
per-type carry-forward entries. Duplicate detection for compacted mission settlements is kept through the mission
sequence watermark and the set of settled MissionIds. The format is owned by [SPEC-SAVE](../architecture/SAVE_FORMAT.md).

## 5. Experience, reputation, and unlocks

### 5.1 XP per career (REQ-ECON-003)

- Separate XP and level (1–10) for Taxi, Deliveries, Freight, and Bus. Challenges grant XP to the career of the vehicle
  class used. XP never decreases.
- Mission XP = base + per-km component of the **planned** route distance, times a quality multiplier (0.5–1.25) from
  the career's evaluation. Using planned distance prevents detour farming.
- XP is applied in the settlement commit, keyed by the same TransactionId, so it is never granted twice.

### 5.2 Reputation

One global **reliability** score from 0 to 100, starting at 50 **[Proposal]**. It affects the quality and pay of
offered contracts, access to premium contracts, and company founding. It never removes starter jobs.

| Event | Change **[Proposal]** |
|---|---|
| Completed, evaluation ≥ 80% | +2 |
| Completed, evaluation 50–79% | +1 |
| Completed, evaluation < 50% | 0 |
| Player cancellation | −1 to −3 (by stage, [SPEC-MISSION](MISSION_FRAMEWORK.md)) |
| Failed | −5 |
| No-fault cancellation, Expired offer | 0 |

Career specifications may set **lighter** penalties for specific causes (for example, an expired taxi passenger
patience costs −1, [SPEC-TAXI](careers/TAXI.md)). They never set heavier ones.

### 5.3 Unlocks [Proposal]

| Unlock | Condition |
|---|---|
| Taxi, Deliveries tier 1 (car), basic rental, car checkpoint and time-trial challenges | Available from the start |
| Light delivery vehicle rental | Deliveries level 2 |
| Light delivery vehicle purchase; fragile and express contracts | Deliveries level 3 |
| Freight (starter contracts with a service truck) | Deliveries level 3 **or** total level across careers ≥ 6 |
| Rigid truck rental and purchase | Freight level 2 |
| Bus shifts | Taxi level 3 **or** reputation ≥ 60 |
| Company founding | Total level ≥ 10, reputation ≥ 60, and the founding cost (§8) |

QA and lab testing may seed a profile through a diagnostics-only option. Such profiles are marked "test-seeded"
permanently, and the option is never offered in normal career play **[Proposal]**.

## 6. Balancing principles [Proposal]

1. **Fast first loop.** A new player earns their first payout within about 10 minutes of entering character mode.
2. **Reachable ownership.** The first owned car is affordable after about 1–3 hours of active play. Exact targets are
   OPEN-204.
3. **Owning pays off.** An owned vehicle's running costs (fuel plus maintenance) stay below half of the basic rental's
   revenue share at the same workload.
4. **Bounded setbacks.** A failure or cancellation never costs more than about one average mission's pay.
5. **No routine debt.** Routine costs never push the wallet below zero. Only mandatory recovery services can create
   capped deferred charges.
6. **Progress improves the rate, not the access.** Better vehicles and levels raise earnings per hour. The basic loop is
   never locked behind them.
7. **Tunable data.** All coefficients live in one balancing table that can be adjusted without changing the rules.
8. **The city matters.** Longer, harder, or rarer routes pay more per minute than repetitive short hops.

## 7. Safe beginner economy and insolvency recovery (REQ-ECON-004)

**Starter guarantees:**
- A new profile starts with the initial balance in §8 and a guaranteed **basic rental**. It needs no upfront payment and
  includes fuel; the rental takes a revenue share of positive settlements ([SPEC-GARAGE](VEHICLE_OWNERSHIP_AND_GARAGE.md)).
- The job board always offers at least one **starter job** (Taxi or Deliveries tier 1) that is usable with the basic
  rental, whenever the city has at least one valid access-point pair (RISK-204).

**Insolvency** is the state where the wallet cannot pay the cheapest action needed to keep working (refuel, repair, or
rent) **or** the player has no usable vehicle and the wallet is at or below zero.

**Recovery path:** always shown in a single "Get back to work" panel, with explanations:
1. Take the basic rental (always available; no money needed).
2. Accept a starter job.
3. Mandatory charges beyond the balance (towing, roadside fuel) become **deferred charges**, capped as in §8. They are
   repaid automatically at a fixed share of each positive settlement. While a deferred charge is open, vehicle purchases
   and upgrades are blocked, but rentals, fuel, and repairs are not.
4. Selling an owned vehicle is offered as an option. It is never forced.

**Invariant (testable):** from any reachable profile state there is a sequence of actions requiring **zero** credits
that leads to a paying job. It never permanently blocks progression.

**Exploit safeguards (RISK-203):**
- Cancellations never yield positive money.
- Recovery relocates only to the nearest safe point, not toward a destination.
- Towing is charged.
- Sale prices are below purchase prices.
- Challenge rewards are paid once per tier and route version.
- The basic rental's share makes it a fallback, not the best way to earn.

## 8. Illustrative values (single home) [Proposal]

| Item | Value |
|---|---|
| Initial balance | 300 cr |
| Basic rental | No deposit; 25% of each positive settlement while in use; fuel included |
| Premium rental (van / rigid truck) | 60 / 150 cr per shift, plus 100 / 300 cr deposit |
| Taxi fare | 20 cr flag + 6 cr per planned km; tip 0–25% by ride rating |
| Delivery pay | 12 cr per parcel + 4 cr per planned km; fragile ×1.3; express ×1.4 |
| Freight pay | 40 cr per load + 3 cr per tonne-km; heavy cargo ×1.2; service-truck contracts pay 60% |
| Bus shift pay | 8 cr per stop served + 1.5 cr per career passenger delivered; punctuality bonus up to +20% |
| Mission XP | 10 + 3 per planned km, × quality 0.5–1.25 |
| Level thresholds (2…10) | 100, 300, 650, 1,150, 1,800, 2,600, 3,600, 4,800, 6,200 XP |
| Fuel (full tank; gameplay-scaled range) | Car 60 cr / 120 km · Van 70 cr / 100 km · Rigid truck 120 cr / 150 km |
| Maintenance | Car 1.2 · van 1.8 · truck 3.5 cr per condition point restored |
| Towing / roadside fuel | 40 cr + 2 cr per km towed / 25 cr + fuel at cost |
| Vehicle prices | Used car 650 · new car 1,600 · van 2,400 · rigid truck 6,500 · bus (company only) 9,000 cr |
| Sale value | 60% of purchase price × (0.5 + 0.5 × condition) |
| Upgrades | 150–600 cr each |
| Garage capacity expansion (2 → 4 → 6 stored vehicles) | 800 / 1,600 cr |
| Cancellation fee cap | 40 cr |
| Deferred-charge cap / repayment share | 1,000 cr / 25% of each positive settlement |
| Company founding / hiring fee / salary | 3,000 cr / 150 cr / 120 cr per settlement period |

## 9. Economic tutorial hooks (REQ-ECON-005)

Contextual explanations are triggered by the first occurrence of each event. The presentation and sequencing are owned
by [SPEC-ONBOARD](../experience/ONBOARDING.md):

- opening the wallet (and how it differs from the city budget);
- taking the basic rental (revenue share);
- the first settlement breakdown (pay, tip, XP, reputation);
- the first cost (fuel or repair);
- the first purchase becoming affordable;
- the first cancellation or failure (consequences);
- the first insolvency ("Get back to work");
- founding a company (separate account).

## 10. PHASE-06 slice versus PHASE-07 (CONF-007)

| Capability | PHASE-06 (minimal slice) | PHASE-07 (full) |
|---|---|---|
| Wallet and ledger | InitialBalance, MissionSettlement, RentalCharge (basic share); a simple history list | All types; filters; compaction |
| XP | Taxi XP accrues and is displayed | All careers, levels, and unlock effects |
| Reputation, unlocks | Not active (data fields reserved) | Active |
| Rental | Basic rental only | Premium rentals, deposits |
| Ownership, fuel, maintenance, upgrades | — | Per SPEC-GARAGE |
| Insolvency recovery | Basic rental plus starter taxi job | Full §7, including deferred charges |

The PHASE-06 persisted schema must already contain the PHASE-07 fields, or migrate to them through the tested path in
[SPEC-SAVE](../architecture/SAVE_FORMAT.md) (REQ-SAVE-002).

## 11. Failure handling

- A settlement that would violate a ledger invariant is rejected. The mission stays *settlement pending* and the error
  is logged (SPEC-MISSION §12).
- A corrupt economy record on load is quarantined and the last valid copy is used (SPEC-SAVE). The player sees an
  explained notice.
- A missing balancing-table value falls back to built-in defaults with a diagnostics warning. It never produces a zero
  price or a free purchase.

## 12. Data that must persist

Per career profile, bound to the save identity (REQ-SAVE-001; format in [SPEC-SAVE](../architecture/SAVE_FORMAT.md)):
the ledger, the wallet cache, XP per career, reputation, unlocks, open deferred charges, pending operations with their
operation IDs, and the test-seeded flag. An older save never shows a newer session's money or progress.

## 13. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | No career transaction changes the city budget (compared before and after a scripted session). | TEST-ECON-001 | Not run |
| 2 | Every money movement creates exactly one ledger entry. Duplicates are rejected; balance equals the sum of the entries. | TEST-ECON-002, TEST-CAREER-004 | Not run |
| 3 | XP per career, reputation, and unlocks change as specified and persist across save/load. | TEST-ECON-003 | Not run |
| 4 | From scripted insolvency states (no money, no vehicle, immobilized vehicle, open deferred charges), a zero-cost path to paid work exists. | TEST-ECON-004 | Not run |
| 5 | Tutorial hooks fire once per trigger and explain wallet, costs, income, and recovery. | TEST-ECON-005 | Not run |
| 6 | Loading an older save shows only that save's wallet and ledger. | TEST-SAVE-001 | Not run |

## 14. Open questions and risks

Central ([register](../planning/RISKS_AND_OPEN_QUESTIONS.md)): RISK-006, RISK-019, RISK-022, CONF-007. New:
**OPEN-204** (balancing targets and calibration method), **RISK-202** (wallet confused with the city budget),
**RISK-203** (economy exploits through safety nets), **RISK-204** (too few contracts in small or unusual cities).

## 15. Provenance

Wallet separation, the ledger, XP, reputation, unlocks, the economic tutorial, and "no permanent block" come from the
owner brief **[Owner requirement]** and the historical plan in [chat.md](../../chat.md) **[Historical input]**. The
revenue-share basic rental, deferred charges, the zero-cost-path invariant, and all values are original proposals.
Motor Town's buy/rent progression is a behavioral reference only ([MOTOR_TOWN_REFERENCE](../research/MOTOR_TOWN_REFERENCE.md)).
