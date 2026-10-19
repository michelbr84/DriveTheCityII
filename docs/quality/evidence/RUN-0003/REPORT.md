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

PENDING: filled in from the career coverage reports at the end of the run.
