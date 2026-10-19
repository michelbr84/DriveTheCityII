# Bus Career

| Field | Value |
|---|---|
| ID | SPEC-BUS |
| Document status | Draft |
| Release scope | v1.0 (city lines and stops with **career passengers only**; native passengers are a separate experiment, CONF-011) |
| Phases | PHASE-08 (primary), PHASE-10 (line-change reaction hardening, demand weighting), PHASE-11 (presentation) |
| Responsible role | Coordinator (maintainer); Implementer of the Career module and the CityBridge transit-line reader |
| Related requirements | REQ-BUS-001, REQ-BUS-002, REQ-CAREER-001 to REQ-CAREER-006, REQ-DRIVE-003, REQ-SAVE-005, REQ-CITY-001, REQ-CITY-003 |
| Related documents | [SPEC-MISSION](../MISSION_FRAMEWORK.md), [SPEC-CITY](../../architecture/CITY_INTEGRATION.md), [SPEC-DRIVING](../VEHICLE_DRIVING.md), [SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md), [SPEC-HOME](../HOME_AND_SERVICE_POINTS.md), [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md), [SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md), [SPEC-SAVE](../../architecture/SAVE_FORMAT.md), [EXPANSIONS](../../product/EXPANSIONS.md) |
| Last updated | 2026-10-18 |

> **Status:** design specification. Nothing is implemented or tested. Whether lines, stop order, and stop positions can
> be read reliably is open (OPEN-020). Values are **[Proposal]**; pay is in [SPEC-ECON](../PROGRESSION_AND_ECONOMY.md)
> §8. Validation: **Not run**. **Bus passengers in v1.0 are mod-owned career passengers. Native citizens are never
> carried, removed from stops, or counted [Owner requirement].**

## 1. Purpose

The bus career puts the player behind the wheel of the transit network they designed. The player drives a real line of
the city, serving its stops in order, managing capacity and dwell time, and keeping passengers comfortable and on
schedule (REQ-BUS-001). When the player redesigns that line, the shift adapts safely (REQ-BUS-002).

## 2. Scope and non-goals

**In scope:** shifts on existing city **bus** lines; stops in line order; boarding and alighting of career passengers;
capacity; schedule adherence; comfort; trip evaluation; replanning or no-fault cancellation after line or stop changes.

**Non-goals for v1.0:** native passengers, native fares, or native schedules (a separate experiment, CONF-011,
[EXPANSIONS](../../product/EXPANSIONS.md)); trams, trains, or other transit modes; creating or editing lines from
character mode; ticket sales; articulated buses. The career never changes the native line's ridership, income, or
vehicles (REQ-CITY-001).

## 3. Prerequisites and vehicle

- **Unlock:** SPEC-ECON §5.3.
- **City requirement:** at least one bus line whose route has at least 3 stops with validated stop access points and
  validated road connectivity between consecutive stops. If no line qualifies, the career shows "Unavailable: your city
  has no usable bus line" with the reason per line (RISK-204).
- **Vehicle:** a **service bus** from a **bus depot** service point ([SPEC-HOME](../HOME_AND_SERVICE_POINTS.md)): free to
  use, returned at the end of the shift ([SPEC-GARAGE](../VEHICLE_OWNERSHIP_AND_GARAGE.md) §8). A company-owned bus may
  also be driven by the player (PHASE-09). Class parameters: [SPEC-DRIVING](../VEHICLE_DRIVING.md) §3.4. **Capacity:**
  60 career passengers (30 seated, 30 standing) **[Proposal]**.

## 4. The shift

1. At the depot (or job board), choose a **line**, a **direction**, and a **shift length**: one full run of the line or
   a segment of at least 4 consecutive stops **[Proposal]**. The offer shows stops, estimated duration, and estimated pay.
2. **Accept.** The mission takes a **snapshot** of the stop sequence (stable stop references) and the line's **version
   signature** ([SPEC-SAVE](../../architecture/SAVE_FORMAT.md)). Each stop becomes a `Stop` leg, in order.
3. Pick up the service bus, then drive to the first stop.
4. At each stop: **stop, open doors, let passengers alight, let passengers board, close doors, depart.**
5. After the last stop's alighting, the mission is `Completed` and settles once. Return the bus to the depot, or choose
   **End shift here** at any valid parking access point (the operator collects the bus).

**First-time guidance** (needed for the PHASE-08 gate): the first shift is a guided 4-stop segment. Hints cover the stop
zone, the door action and interlock, dwell and capacity, and the on-time window. Presentation belongs to
[SPEC-ONBOARD](../../experience/ONBOARDING.md).

## 5. Stops, doors, boarding, and alighting

- **Stop zone:** within ±8 m along the curb or bay of the stop access point, at under 0.5 m/s **[Proposal]**.
- **Doors:** a dedicated action, or automatic if enabled in the accessibility settings. While the doors are open,
  throttle is disabled by a **door interlock**, so the bus cannot drive off with doors open.
- **Order:** passengers whose destination is this stop alight first, then waiting passengers board up to capacity.
- **Rate:** about 1.2 s per passenger, with dwell capped at 40 s per stop **[Proposal]**. Passengers who do not fit or
  do not board in time are **left behind**. That is neutral for the evaluation unless capacity was free.
- **Passenger groups:** passengers are modeled as **groups by destination stop**, with counts rather than individuals,
  to keep the entity count low. Up to 6 figures per stop are visibly represented (OPEN-202). The rest are shown as a
  count on the stop marker and the HUD. Inside the bus, passengers are shown only as an occupancy counter.
- **Generation:** each stop's waiting count is drawn when the bus approaches it (within 300 m). It comes from a
  stop-demand weight based on surrounding building density and the in-game time of day (OPEN-207;
  [SPEC-CHALLENGE](../CHALLENGES_AND_DYNAMIC_CITY.md) §4). Counts are capped at 0–15 per stop **[Proposal]**, and
  destinations are spread over the downstream stops. Native citizens waiting at the same stop are untouched and never
  counted.

## 6. Schedule, comfort, and evaluation

- **Timetable:** the target arrival per stop = cumulative route estimate + dwell allowance (15 s per stop). The on-time
  window is 1 min early to 3 min late **[Proposal]**. **Departing early** from a stop is penalized more than arriving late.
- **Comfort:** standing passengers are sensitive to harsh longitudinal acceleration above 0.3 g and lateral acceleration
  above 0.35 g. Each event costs comfort points. Contacts cost as in the taxi table ([SPEC-TAXI](TAXI.md) §6).
- **Missed stop:** passing a stop with waiting or alighting passengers without servicing it marks the stop `Missed`. Its
  alighting passengers get off at the next stop and record a complaint.
- **Trip evaluation** 0–100 = 35% punctuality + 25% comfort + 25% stops served + 15% driving cleanliness. The pay
  multiplier and reputation follow the table in [SPEC-DELIVERY](DELIVERIES.md) §7.
- **Pay:** per stop served plus per career passenger delivered, plus a punctuality bonus (SPEC-ECON §8), times the
  evaluation multiplier. One settlement per shift.

## 7. Cancellation, failure, and recovery

| Situation | Result **[Proposal]** |
|---|---|
| Player ends the shift early | Passengers aboard alight at the next valid stop, or immediately if the bus is stopped at a stop. `Cancelled`: stops already served are paid at 50%, reputation −2, no fee. |
| Player leaves the bus with passengers aboard for more than 3 career-clock minutes | Passengers alight. `Failed`, reputation −5. |
| Three Severe contacts in one shift | Passengers alight at the next stop. `Failed`. |
| Lateness | Never a failure by itself. It only lowers punctuality. |
| Bus immobilized | The operator replaces the service bus. Passengers transfer; no fault, and the schedule targets shift by the replacement time. |
| `Recovery` | The bus moves to a safe lane point near its position. If triggered by the system, the schedule targets shift by its duration. |

## 8. Line and stop changes (REQ-BUS-002)

When revalidation runs ([SPEC-MISSION](../MISSION_FRAMEWORK.md) §7.3, on return from `Building` or on load), the current
line is read again and compared with the shift's version signature. Replanning:

1. **Line unchanged:** continue.
2. **Stops added or moved:** rebuild the remaining stop sequence from the current line data, starting from the next
   stop downstream of the bus's position. New stops become new legs. Moved stops use their new access points.
3. **Stops removed:** passenger groups headed to a removed stop are reassigned to the nearest remaining downstream stop.
   If the next stop was removed, the following valid stop becomes next.
4. **Line deleted, direction no longer served, or fewer than 2 valid stops remaining downstream:** the shift is
   `Invalidated`. Passengers alight at the next valid stop, or immediately if the bus is stopped at a curb. No-fault
   cancellation: stops served are paid at 100%, with no reputation change.
5. **Route between stops disconnected:** reroute. If impossible, skip to the next reachable stop with no fault, and
   reassign that stop's passengers as in step 3.

The player always sees what changed and why, for example "Line 4 was modified: 2 stops removed. Passengers were moved
to stop 'Elm Street'."

## 9. Interaction with native buses

The career bus shares stops with native buses. To avoid blocking them (RISK-201) **[Proposal]**:
- the stop zone accepts stopping behind a native bus;
- dwell is capped;
- the career bus holds no lane reservation at the stop after departing;
- PHASE-08 observes stop-queue behavior in a lab city, under the contact rule chosen in OPEN-032.

## 10. Data that must persist

Format in [SPEC-SAVE](../../architecture/SAVE_FORMAT.md): the shift record (line stable reference, direction, the line
version signature at acceptance, the stop-sequence snapshot as stable references, the current stop index, the outcome
per stop, punctuality and comfort accumulators, and career-clock targets); passenger groups by destination stop; and the
service bus VehicleRecordId. On load: the bus is respawned, passenger counts are restored, any dwell in progress
restarts with the doors closed, and revalidation runs before the clock resumes.

## 11. Acceptance criteria

| # | Criterion | Primary test | Validation |
|---|---|---|---|
| 1 | A shift on a lab-city line serves stops in order with career passengers boarding and alighting, respects capacity, and produces a trip evaluation. No native citizen is carried or changed. | TEST-BUS-001 | Not run |
| 2 | Adding, moving, or removing stops and deleting the line during a shift leads to replanning or a no-fault cancellation as in §8. | TEST-BUS-002 | Not run |
| 3 | The door interlock prevents driving with doors open. Dwell is capped. | TEST-BUS-001 (procedure step) | Not run |
| 4 | Lifecycle, cancellation, and exactly-once settlement behave as in SPEC-MISSION. | TEST-CAREER-001, TEST-CAREER-004, TEST-CAREER-005 | Not run |
| 5 | Save/load mid-shift restores passengers, stop progress, and the schedule, with no duplicate pay. | TEST-SAVE-005 | Not run |
| 6 | The bus class meets its distinctness and capacity rules. | TEST-DRIVE-003 | Not run |

## 12. Open questions and risks

Central ([register](../../planning/RISKS_AND_OPEN_QUESTIONS.md)): OPEN-020 (reading lines and stops), OPEN-021
(detecting line edits), OPEN-032, CONF-011. New: **OPEN-202** (representation of visible career passengers),
**OPEN-207** (stop-demand inputs), **RISK-201** (the career bus blocking native buses at shared stops), **RISK-204**
(cities with no usable bus line).

## 13. Provenance

Following city lines and stops in order with career passengers, capacity, boarding and alighting, trip evaluation,
replanning or no-fault cancellation, and the exclusion of native passengers in v1.0 are **[Owner requirement]**. The
historical plan in [chat.md](../../../chat.md) **[Historical input]** described a second level with native passengers,
which is now a separate experiment (CONF-011). The door interlock, passenger groups, timetable window, and replanning
procedure are original proposals. Motor Town's bus job is a behavioral reference only
([MOTOR_TOWN_REFERENCE](../../research/MOTOR_TOWN_REFERENCE.md)).
