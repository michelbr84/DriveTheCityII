# RUN-0003 — Careers in game: taxi, persistence, and career coverage

| Field | Value |
|---|---|
| Run ID | RUN-0003 |
| Phases | PHASE-06 to PHASE-10 (careers, garage, home, company, challenges) |
| Date | 2026-10-19 |
| Executed by | Claude Code agent (owner-authorized autonomous run of 2026-10-19); no human reviewer yet |
| Input | **Simulated.** Driving uses the TEST AUTOPILOT (`career drive` / `career chdrive`). It sends throttle, brake and steering through the same input path as the keyboard, and every result line says `[simulated input via test autopilot]`. Career rules (arrival, boarding, settlement, deadlines) run unchanged. Panel actions are invoked through the test bridge (`career ui <action>`), which calls the same handlers as the panel buttons. |
| Test seeding | Where progression gates or money were needed, `career xp/rep/grant/fuel` were used. They mark the career profile **test-seeded** permanently. Seeded steps are labelled. |
| Sessions | `%LOCALAPPDATA%\DriveTheCityII\test\sessions\20261019-035427` … (see the WORKLOG for the full list) |

## 1. Taxi fares end to end

| Session | Result | Detail |
|---|---|---|
| 20261019-035427 | Failed (career logic correct) | The autopilot pinned the car on a lamppost after a U-turn start; the passenger's patience ran out; settled 0 cr, reputation -1. The career failure path worked as specified. |
| 20261019-040328 | Failed (career logic correct) | Autopilot deadlocked behind a stopped vehicle after a recovery; patience failure again, settled 0 cr. |
| 20261019-042215 | **Passed** | Pickup at 1221 Elm Street after 116 s (servicing 4 s, "passengers boarded"), drop-off at 9 Highland Street, **settled 49.01 cr, +12 XP**; basic-rental share **-12.25 cr** (25 %). Wallet 300.00 → 336.76 cr. Two autopilot recoveries on the way (test tool). |
| 20261019-043433 (persistence campaign) | **Passed** | A second fare completed after a save and reload mid-trip: settled 29.13 cr, rental share -7.28 cr. |

Autopilot defects were fixed in the test tool (three-point turns, corner speed, vehicles ahead, stall recovery). They
do not affect players.

## 2. Persistence and exactly-once payouts (`tools\campaign-career-persist.ps1`)

Report: `%LOCALAPPDATA%\DriveTheCityII\test\campaigns\career-persist-20261019-044017.md`.

| Step | Result |
|---|---|
| Save A (before the fare) | Passed |
| Accept the fare, drive to the pickup, passenger boards | Passed (`Taxi/Active leg=1 Dropoff`, `pax=Aboard`) |
| Save B mid-mission while driving with the passenger aboard | Passed |
| Reload B: the mission resumes at the same stage, same wallet | Passed (`leg=1`, `pax=Aboard`, wallet 300.00 cr) |
| Reload B: no mod entity was written into the save | Passed (`excluded-signature cars=0 actors=0`) |
| Bring the mission car back and finish the fare | **Passed on the data; the script's check was wrong.** One `MissionSettlement` (+29.13 cr) and one `RentalCharge` (-7.28 cr) were added. The check expected one ledger entry instead of two and has been corrected. |
| Save C and reload C: same balance, no second payout | Passed (wallet 321.85 cr, 3 entries, no mission) |
| Reload the older save A: its balance is restored exactly | Passed (300.00 cr, 1 entry) |

## 3. Career coverage (`tools\campaign-careers.ps1`)

Reports and raw outputs: `%LOCALAPPDATA%\DriveTheCityII\test\campaigns\careers-*` (several runs; defects found in a run
were fixed and the affected sections re-run on the next build).

| Area | Result | Evidence and detail |
|---|---|---|
| Career clock while paused | **Passed** | Game paused for 8 s of real time: career clock 238 → 238 |
| Service point, repair | **Passed** | Rental called to service point '390 Robin Street'; repaired 90 % → 100 % |
| Buy a vehicle | **Passed** | `car.used` bought (test-seeded money) |
| Refuel, exactly once | **Passed** | Tank 30 % → 100 % (test-seeded level), one ledger entry (-42.00 cr). The second, simultaneous click was refused: "This refuel was already paid." |
| Sell a vehicle | **Passed** | +351.00 cr |
| Delivery | **Passed** | Load at 1215 Elm Street, drop-off at 156 Birdsong Street; the last parcel was destroyed in a collision, so the job completed with pay for the delivered parcel (22.22 cr, +25 XP). A first attempt failed its hard deadline after 3 minutes; this exposed the route-time defect (ISSUE-015), which is now fixed. |
| Freight | **Partly verified** | Offer (test-seeded unlock), accept, **shipper's truck provided** (after the ISSUE-013/014 fixes), boarded, cargo loaded ("Payload affects braking and acceleration"). In two runs the test autopilot then hit a car at 9.7 m/s and the load was lost, so the job failed with 0 cr and reputation -5, the correct rule outcome. A completed delivery of freight was **not observed in game**. |
| Bus | **Partly verified** | Bus shift on line 1 (14 stops) accepted, **operator's bus provided** and boarded. The test autopilot got stuck before the first stop. A served stop was **not observed in game**. |
| Challenges | **Partly verified** | Challenges generated (time trial, checkpoint route, parking). Start arms the run; checkpoint markers, route and HUD card are drawn. A completed run was **not observed**: the runs were voided first by the route-change defect (ISSUE-016, fixed), then by an autopilot recovery (a recovery voids a run by design). |
| Home | **Passed** | Home set at the nearest house; "Go home" worked |
| Company | **Passed** (setup) | Company founded (test-seeded gates), driver hired, fleet rigid truck bought, freight contract accepted, driver assigned, state saved. Period settlements are observed in the soak (see RUN-0004). |
| Save with all careers' state | **Passed** | `DTC2-LAB-CAREERS`, `-F`, `-G` |

Test-seeded profile: yes for this city (XP, reputation, money and one fuel level were seeded).

### Retries on the final package `0.1.0-3eaf045` (session 20261019-083020)

- **Freight:** truck provided, boarded, loaded at 87 Sunset Street; the test autopilot then tried to pass a stopped
  vehicle and hit it, so the load was lost (0 cr, reputation -5). The career rules again behaved as specified; a
  completed freight run is still **not observed in game**.
- **Challenges:** after the failed freight job the player was still sitting in the released shipper truck, so starting a
  challenge was refused with "Challenges need one of your career vehicles" (correct rule; the test ran freight before
  challenges). Earlier challenge runs on the final package were armed and then voided by recoveries while reaching the
  start. In those runs the route no longer flipped to "changed" during the run (ISSUE-016 fix confirmed). A finished run
  with a time and medal is still **not observed in game**.
